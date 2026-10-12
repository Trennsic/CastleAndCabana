using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using static HeroTeamUiAddon;
using static MonsterTeam;

/// <summary>
/// Represents a party of up to four <see cref="HeroUnit"/>s moving through the tower together.
/// Manages team initialization, floor traversal, slot assignment, combat actions,
/// death handling, and the team UI overlay. A team progresses floor by floor,
/// entering combat when a <see cref="MonsterTeam"/> is present, and escaping or
/// being destroyed when all heroes are dead.
/// </summary>
public class HeroTeam : MonoBehaviour
{
    #region // Variables

    #region // Enums

    /// <summary>
    /// High-level states describing where the team is in the floor traversal loop.
    /// </summary>
    public enum HeroTeamStates
    {
        /// <summary>Team exists but has not yet been placed in the tower.</summary>
        Inactive,
        /// <summary>Team is positioned at the start waypoint of its current floor.</summary>
        AtStartPoint,
        /// <summary>Team has reached the end waypoint of its current floor.</summary>
        AtEndPoint,
        /// <summary>Team is occupying combat slots and fighting a <see cref="MonsterTeam"/>.</summary>
        InCombat,
        /// <summary>Team is currently moving along a path or into slots.</summary>
        Moving,
        /// <summary>All heroes are dead; team is being cleaned up.</summary>
        Destroyed,
    }

    #endregion

    #region // References

    [Header("References")]
    [Tooltip("All living hero units that belong to this team.")]
    [SerializeField] private List<HeroUnit> heroes;

    [Tooltip("Ordered list of waypoints the team follows from the floor start to end point.")]
    public List<GameObject> floorPath;

    [Tooltip("Reference to the central TowerDefenseManager.")]
    [SerializeField] private TowerDefenseManager GameManager;

    [Tooltip("The floor GameObject this team is currently occupying.")]
    [SerializeField] private GameObject floorObject;

    [Tooltip("The TowerSectionAddon of the floor this team is currently on.")]
    [SerializeField] private TowerSectionAddon floorAddon;

    [Tooltip("The HeroSlotsAddon for the current floor, assigned when the team enters combat slots.")]
    [SerializeField] private HeroSlotsAddon slotsAddon;

    [Tooltip("The CoreUnit on the current floor, linked when the team enters combat slots.")]
    [SerializeField] private CoreUnit floorCore;

    [Tooltip("The MonsterTeam this team is currently fighting, if any.")]
    [SerializeField] private MonsterTeam monsterTeamInCombatWith;

    #endregion

    #region // Team Information

    [Header("Team Information")]
    [Tooltip("Current high-level state of this team in the floor traversal loop.")]
    [SerializeField] private HeroTeamStates heroTeamState = HeroTeamStates.Inactive;

    [Tooltip("Index of the tower floor this team is currently on.")]
    [SerializeField] private int currentFloor;

    [Tooltip("Unique ID for this hero team.")]
    [SerializeField] private int htID;

    [Tooltip("Display name of this team.")]
    [SerializeField] private string teamName;

    [Tooltip("Spawn order index used to differentiate multiple teams on the same floor.")]
    [SerializeField] private int spawnNumber;

    [Tooltip("True when the team is locked on its current floor by an active core lock timer.")]
    [SerializeField] private bool isLocked;

    [Tooltip("True once the team has successfully escaped the tower.")]
    [SerializeField] private bool escapedTower;

    [Tooltip("The instantiated team UI GameObject that follows the team's average position.")]
    [SerializeField] private GameObject teamUi;

    [Tooltip("The HeroTeamUiAddon component on the team UI, used to update the UI state.")]
    [SerializeField] private HeroTeamUiAddon teamUiAddon;

    /// <summary>Maximum number of heroes allowed in one team.</summary>
    private const int MaxTeamSize = 4;

    /// <summary>Whether <see cref="InitializeHeroTeam"/> has completed successfully.</summary>
    private bool isInitalized = false;

    #endregion

    #region // Debugging

    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;

    #endregion

    #region // Public Getters

    [Header("Public Getters")]
    public int CurrentFloor => currentFloor;
    public int SpawnNumber => spawnNumber;
    public HeroTeamStates HeroTeamState => heroTeamState;

    #endregion

    #endregion

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake() { }

    private void Start() { }

    /// <summary>
    /// Each frame: repositions the team UI to the heroes' average world position,
    /// checks whether all heroes have been destroyed, and refreshes debug watches.
    /// </summary>
    private void Update()
    {
        UpdateTeamUiPosition();
        CheckIfAllUnitsAreDestroyed();
        UpdateDebugWatches();
    }

    // -------------------------------------------------------------------------
    // Debug Watches
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes all Inspector watch entries each frame with the team's current
    /// runtime state. Called from <see cref="Update"/>.
    /// </summary>
    private void UpdateDebugWatches()
    {
        debug.SetWatch("State", heroTeamState.ToString());
        debug.SetWatch("Floor", currentFloor.ToString());
        debug.SetWatch("Heroes", heroes != null ? heroes.Count.ToString() : "null");
        debug.SetWatch("Locked", isLocked.ToString());
        debug.SetWatch("Escaped", escapedTower.ToString());
        debug.SetWatch("In Combat", GetIsInCombat().ToString());
        debug.SetWatch("Monster Team", monsterTeamInCombatWith != null ? monsterTeamInCombatWith.name : "none");
        debug.SetWatch("Has Slots", slotsAddon != null ? "yes" : "no");
        debug.SetWatch("Core", floorCore != null ? floorCore.name : "none");
        debug.SetWatch("Team ID", htID.ToString());
    }

    // -------------------------------------------------------------------------
    // Overrides
    // -------------------------------------------------------------------------

    /// <summary>
    /// Two <see cref="HeroTeam"/> instances are equal if they share the same <see cref="htID"/>.
    /// </summary>
    public override bool Equals(System.Object obj)
    {
        if (obj == null) return false;
        if (GetType() != obj.GetType()) return false;

        HeroTeam a = obj as HeroTeam;
        if ((System.Object)a == null) return false;

        return htID == a.htID;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => htID.GetHashCode();

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fully initializes the hero team: links the manager, creates the hero list,
    /// sets the starting floor, spawns heroes, builds the team UI, and sets the
    /// initial state to <see cref="HeroTeamStates.AtStartPoint"/>.
    /// </summary>
    /// <param name="manager">The central <see cref="TowerDefenseManager"/>.</param>
    /// <param name="newHeroes">Descriptors for each hero to spawn on the team.</param>
    /// <param name="htID">Unique hero team ID.</param>
    /// <param name="floorToSpawnOn">The floor index to spawn on (default 0).</param>
    public void InitializeHeroTeam(TowerDefenseManager manager, List<NewHero> newHeroes, int htID, int floorToSpawnOn = 0)
    {
        InitalizeDebug();
        SetGameManager(manager);
        InitalizeList();
        SetCurrentFloor(floorToSpawnOn);
        SetHtID(htID);
        SetIsLockedOnFloor(false);
        SetupHeroTeam(newHeroes, floorToSpawnOn);
        InitializeTeamUi();
        SetState(HeroTeamStates.AtStartPoint);
        SetIsInitalized(true);
    }

    /// <summary>Resets the heroes and floor path lists to empty instances.</summary>
    private void InitalizeList() { SetHeroes(new List<HeroUnit>()); SetFloorPath(new List<GameObject>()); }

    /// <summary>Creates a fresh <see cref="DebugUtility"/> for this team.</summary>
    private void InitalizeDebug() { debug = new DebugUtility(false); }

    /// <summary>
    /// Instantiates the team UI prefab, parents it to this transform, links the
    /// <see cref="HeroTeamUiAddon"/>, and positions the UI at the team's location.
    /// </summary>
    private void InitializeTeamUi()
    {
        GameObject teamUiPrefab = GetGameManager().GetHeroManager().GetHeroTeamUiPrefabl();
        GameObject teamUiObject = Instantiate(teamUiPrefab);
        teamUiObject.transform.SetParent(gameObject.transform);
        SetTeamUi(teamUiObject);
        SetTeamUiAddon(teamUiObject.GetComponent<HeroTeamUiAddon>());
        UpdateTeamUiAddon(HeroTeamUiStates.Waiting);
        UpdateTeamUiPosition();
    }

    /// <summary>
    /// Spawns each hero described in <paramref name="newHeroes"/>, parents them to
    /// this transform, and snaps the whole team to the floor start point.
    /// </summary>
    /// <param name="newHeroes">Descriptors for the heroes to create.</param>
    /// <param name="floorToSpawnOn">Floor index used to set the initial path.</param>
    private void SetupHeroTeam(List<NewHero> newHeroes, int floorToSpawnOn = 0)
    {
        List<GameObject> floorPath = GetFloorPath();
        if (floorPath == null || floorPath.Count == 0)
        {
            Debug.LogError("Floor path is empty or invalid.");
            return;
        }

        for (int i = 0; i < newHeroes.Count; i++)
        {
            int numOfHeroes = GetHeroes().Count;
            SetName("Hero " + numOfHeroes.ToString());

            HeroUnit prefab = GetGameManager().GetHeroManager().GetHeroPrefab(newHeroes[i].heroClass);
            if (prefab == null)
            {
                Debug.LogError($"No prefab registered for HeroClass.{newHeroes[i].heroClass}. Skipping hero.");
                continue;
            }

            HeroUnit newHeroUnit = Instantiate(prefab, this.transform);
            newHeroUnit.name = GetName();
            newHeroUnit.InitializeHero(GetGameManager(), this, numOfHeroes, newHeroes[i].heroClass, newHeroes[i].level);

            GetHeroes().Add(newHeroUnit);
        }

        SnapTeamToStartPoint();

        if (debug.IsDebugging) Debug.Log("Hero team setup complete.");
    }

    // -------------------------------------------------------------------------
    // Floor Information
    // -------------------------------------------------------------------------

    /// <summary>
    /// Rebuilds the floor path by querying the current floor's
    /// <see cref="TowerSectionAddon"/> for its waypoint list.
    /// </summary>
    public void UpdateFloorPath()
    {
        if (debug.IsDebugging) Debug.Log("Starting UpdateFloorPath...");

        int currentFloor = GetCurrentFloor();
        TowerManager towerManager = GetTowerManager();

        if (towerManager == null)
        {
            Debug.LogError("TowerManager is null. Cannot update floor path.");
            return;
        }

        GameObject floorObject = towerManager.GetFloor(currentFloor);
        if (floorObject == null)
        {
            Debug.LogError($"Floor object for floor {currentFloor} is null.");
            return;
        }

        TowerSectionAddon towerSection = towerManager.GetTowerSectionAddon(floorObject);
        if (towerSection == null)
        {
            Debug.LogError($"TowerSectionAddon for floor object {floorObject.name} is null.");
            return;
        }

        List<GameObject> newPath = towerSection.GetPath();
        if (newPath == null || newPath.Count == 0)
            Debug.LogWarning("New path retrieved is null or empty.");
        else if (debug.IsDebugging)
            Debug.Log($"New path retrieved with {newPath.Count} nodes.");

        SetFloorPath(newPath);
        if (debug.IsDebugging) Debug.Log("Floor path updated successfully.");
    }

    /// <summary>
    /// Refreshes all floor-specific references (floor GameObject, section addon,
    /// hero slots, and floor path) based on <see cref="currentFloor"/>.
    /// Called automatically whenever <see cref="SetCurrentFloor"/> is invoked.
    /// </summary>
    private void UpdateFloorInformation()
    {
        int currentFloor = GetCurrentFloor();
        GameObject newFloor = GetTowerManager().GetFloor(currentFloor);
        TowerSectionAddon addon = GetTowerManager().GetTowerSectionAddon(newFloor);
        HeroSlotsAddon slots = addon.GetHeroSlots();

        SetFloorObject(newFloor);
        SetFloorAddon(addon);
        SetSlotsAddon(slots);
        UpdateFloorPath();
    }

    /// <summary>
    /// Checks the lock timer on the current floor's <see cref="CoreUnit"/> and
    /// updates <see cref="isLocked"/> accordingly. Sets unlocked if any reference
    /// in the chain is missing.
    /// </summary>
    public void UpdateIsLocked()
    {
        TowerSectionAddon floorAddon = GetFloorAddon();
        if (floorAddon == null) { SetIsLockedOnFloor(false); return; }

        CoreSlotAddon coreSlot = floorAddon.GetCoreSlot();
        if (coreSlot == null) { SetIsLockedOnFloor(false); return; }

        CoreUnit core = coreSlot.GetCoreUnit();
        if (core == null) { SetIsLockedOnFloor(false); return; }

        bool locked = (core.GetCoreInfo()?.GetLockTime() > 0);
        SetIsLockedOnFloor(locked);
    }

    /// <summary>Pushes a new UI state to the <see cref="HeroTeamUiAddon"/>, if present.</summary>
    /// <param name="newUiState">The UI state to apply.</param>
    private void UpdateTeamUiAddon(HeroTeamUiStates newUiState)
    {
        HeroTeamUiAddon uiAddon = GetTeamUiAddon();
        if (uiAddon == null) return;
        uiAddon.SetUiState(newUiState);
    }

    // -------------------------------------------------------------------------
    // Team Removal / Death
    // -------------------------------------------------------------------------

    /// <summary>
    /// Removes a specific hero by index from the active list and checks whether
    /// all remaining heroes are disabled, logging a notice if the team is wiped.
    /// </summary>
    /// <param name="whichUnit">Index of the hero to remove from the team.</param>
    public void RemoveUnitFromTeam(int whichUnit)
    {
        HeroUnit hero = heroes[whichUnit];
        GetHeroes().Remove(hero);

        bool allDisabled = true;
        foreach (HeroUnit unit in heroes)
        {
            if (unit.GetIsActive()) { allDisabled = false; break; }
        }

        if (allDisabled)
        {
            Debug.Log("ALL UNITS ARE DEAD");
        }
    }

    /// <summary>
    /// Checks each frame whether the hero list is empty, and calls
    /// <see cref="RemoveTeam"/> if so.
    /// </summary>
    public void CheckIfAllUnitsAreDestroyed()
    {
        if (GetHeroes().Count > 0) return;
        RemoveTeam();
    }

    /// <summary>
    /// Full team teardown: destroys all remaining hero GameObjects, removes the
    /// team UI, unlinks combat and slot references, deregisters from
    /// <see cref="HeroManager"/>, and destroys this GameObject.
    /// </summary>
    public void RemoveTeam()
    {
        foreach (HeroUnit unit in GetHeroes()) { Destroy(unit); }
        DestroyHeroTeamUi();
        UnlinkTeam();
        GetHeroManager().RemoveHeroTeam(GetHtID());
        Destroy(gameObject);
    }

    /// <summary>Destroys the team UI GameObject if one is assigned.</summary>
    private void DestroyHeroTeamUi()
    {
        if (GetTeamUi() == null) return;
        Destroy(GetTeamUi());
    }

    /// <summary>
    /// Severs this team's links to its combat slot addon and any
    /// <see cref="MonsterTeam"/> it was fighting, without destroying anything.
    /// </summary>
    public void UnlinkTeam()
    {
        if (GetSlotsAddon() != null) { GetSlotsAddon().ClearHeroSlots(); }
        if (GetMonsterTeamInCombatWith() != null) { GetMonsterTeamInCombatWith().SetHeroTeamInCombatWith(null); }
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    /// <summary>
    /// Instantly teleports all heroes to the start waypoint of the current floor,
    /// applying per-index spawn offsets on floor 0. Registers the team with the
    /// floor's non-combat team list and sets state to
    /// <see cref="HeroTeamStates.AtStartPoint"/>.
    /// </summary>
    public void SnapTeamToStartPoint()
    {
        if (debug.IsDebugging)
            Debug.Log("Starting SnapTeamToStartPoint: Moving all heroes to the start position.");

        TowerSectionAddon towerAddon = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
        if (towerAddon != null) { towerAddon.AddHeroTeamToNonCombatTeams(this); }

        // Per-hero spawn offsets applied only on floor 0.
        Vector3[] spawnOffsets = new Vector3[]
        {
            Vector3.zero,
            new Vector3(-1f,  0f,  0f),
            new Vector3(-2f,  0f, -1f),
            new Vector3( 1f,  0f, -1f),
        };

        foreach (HeroUnit hero in GetHeroes())
        {
            GameObject heroObject = hero.gameObject;
            if (heroObject == null)
            {
                if (debug.IsDebugging) Debug.LogError("Hero model is null. Cannot snap this hero.");
                continue;
            }

            List<GameObject> floorPath = GetFloorPath();
            if (floorPath == null || floorPath.Count == 0)
            {
                if (debug.IsDebugging) Debug.LogError("Floor path is null or empty. Cannot set start position.");
                continue;
            }

            Vector3 startPosition = floorPath[0].transform.position;
            Quaternion startRotation = floorPath[0].transform.rotation;

            if (GetCurrentFloor() == 0)
            {
                int heroIndex = heroes.IndexOf(hero);
                Vector3 offset = spawnOffsets.Length > heroIndex ? spawnOffsets[heroIndex] : Vector3.zero;
                startPosition += offset;

                if (debug.IsDebugging)
                    Debug.Log($"Applying offset for Hero '{hero.name}': {offset}");
            }

            heroObject.transform.position = startPosition;
            heroObject.transform.rotation = startRotation;
            hero.SetAnimationState(HeroUnit.HeroAnimations.Idle);

            if (debug.IsDebugging)
                Debug.Log($"Hero '{hero.name}' snapped to position: {startPosition}, rotation: {startRotation}");
        }

        if (debug.IsDebugging) Debug.Log("SnapTeamToStartPoint completed for all heroes.");

        SetState(HeroTeamStates.AtStartPoint);
    }

    /// <summary>
    /// Advances the team to the next floor by incrementing <see cref="currentFloor"/>,
    /// deregistering from the old floor's non-combat list, and snapping to the new
    /// floor's start point. Does nothing if already on the top floor.
    /// </summary>
    public void GotoNextFloor()
    {
        Debug.Log("Starting GotoNextFloor...");

        int currentFloor = GetCurrentFloor();
        TowerManager towerManager = GetTowerManager();

        if (towerManager == null)
        {
            Debug.LogError("TowerManager is null. Cannot move to the next floor.");
            return;
        }

        int floorCount = towerManager.Floors.Count;
        Debug.Log($"Total floors in tower: {floorCount}");

        if (currentFloor < floorCount - 1)
        {
            TowerSectionAddon oldAddon = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
            if (oldAddon != null) { oldAddon.RemoveHeroTeamToNonCombatTeams(this); }

            SetCurrentFloor(currentFloor + 1);
            Debug.Log($"Current floor updated to: {GetCurrentFloor()}");

            SnapTeamToStartPoint();

            TowerSectionAddon newAddon = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
            if (newAddon != null) { newAddon.AddHeroTeamToNonCombatTeams(this); }
        }
        else
        {
            Debug.LogWarning($"Cannot move to the next floor. Current floor ({currentFloor}) is the last floor.");
        }

        Debug.Log("GotoNextFloor completed.");
    }

    /// <summary>
    /// Moves all heroes along the current floor path simultaneously. If the team
    /// is already in combat, the path is shortened to only the final waypoint.
    /// Sets state to <see cref="HeroTeamStates.AtEndPoint"/> on completion.
    /// </summary>
    public IEnumerator MoveHeroesAlongPath()
    {
        List<GameObject> floorPath = GetFloorPath();
        if (floorPath == null || floorPath.Count == 0)
        {
            if (debug.IsDebugging) Debug.LogError("Floor path is null or empty. Cannot move heroes.");
            yield break;
        }

        if (GetState() == HeroTeamStates.InCombat)
        {
            if (debug.IsDebugging) Debug.Log("In combat: Modifying path to only include the final floorPath game object.");
            UnlinkTeam();
            floorPath = new List<GameObject> { floorPath[floorPath.Count - 1] };
        }

        SetState(HeroTeamStates.Moving);
        UpdateTeamUiAddon(HeroTeamUiStates.Moving);

        TowerSectionAddon towerAddon = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
        if (towerAddon != null) { towerAddon.AddHeroTeamToNonCombatTeams(this); }

        foreach (HeroUnit hero in GetHeroes())
        {
            StartCoroutine(hero.MoveHeroAlongPath(floorPath));
        }

        yield return new WaitUntil(() =>
            GetHeroes().All(h => Vector3.Distance(h.gameObject.transform.position, floorPath[floorPath.Count - 1].transform.position) < 0.1f)
        );

        SetState(HeroTeamStates.AtEndPoint);
        if (debug.IsDebugging) Debug.Log("All heroes have reached the endpoint.");
    }

    /// <summary>
    /// Moves all heroes to their individual offset positions around the end crystal,
    /// using <see cref="HeroUnit.MoveHeroToEndCrystal"/>. Sets state to
    /// <see cref="HeroTeamStates.AtEndPoint"/> and registers this team as the
    /// crystal team in <see cref="HeroManager"/> on completion.
    /// </summary>
    public IEnumerator MoveHeroesToEndCrystal()
    {
        List<GameObject> floorPath = GetFloorPath();
        if (floorPath == null || floorPath.Count == 0)
        {
            if (debug.IsDebugging) Debug.LogError("Floor path is null or empty. Cannot move heroes.");
            yield break;
        }

        UnlinkTeam();

        GameObject crystalPositionObject = floorPath[floorPath.Count - 1];

        SetState(HeroTeamStates.Moving);

        TowerSectionAddon towerAddon = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
        if (towerAddon != null) { towerAddon.AddHeroTeamToNonCombatTeams(this); }

        Vector3 basePosition = crystalPositionObject.transform.position;
        Quaternion baseRotation = crystalPositionObject.transform.rotation;

        // Per-hero position offsets around the crystal.
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(-.75f, -.25f,  0f),
            new Vector3( 0.50f, -.25f,  2.5f),
            new Vector3( 3.50f, -.25f,  2.5f),
            new Vector3( 4.20f, -.25f, -1.75f),
        };

        // Per-hero rotation offsets around the crystal.
        Quaternion[] rotationOffsets = new Quaternion[]
        {
            Quaternion.Euler(0f,    0f, 0f),
            Quaternion.Euler(0f,   50f, 0f),
            Quaternion.Euler(0f,  120f, 0f),
            Quaternion.Euler(0f, -115f, 0f),
        };

        List<HeroUnit> heroes = GetHeroes();
        List<Vector3> targetPositions = new List<Vector3>();

        for (int i = 0; i < heroes.Count; i++)
        {
            if (i >= offsets.Length || i >= rotationOffsets.Length)
            {
                if (debug.IsDebugging) Debug.LogWarning($"Skipping Hero {i}, as no offset is defined for this index.");
                continue;
            }

            Vector3 targetPosition = basePosition + offsets[i];
            Quaternion targetRotation = baseRotation * rotationOffsets[i];
            targetPositions.Add(targetPosition);

            if (debug.IsDebugging)
                Debug.Log($"Moving Hero {i} to position {targetPosition} with rotation {targetRotation}.");

            StartCoroutine(heroes[i].MoveHeroToEndCrystal(targetPosition, targetRotation));
        }

        yield return new WaitUntil(() =>
            heroes.Zip(targetPositions, (hero, targetPos) =>
                Vector3.Distance(hero.gameObject.transform.position, targetPos) < 0.1f
            ).All(result => result)
        );

        SetState(HeroTeamStates.AtEndPoint);
        GetHeroManager().SetHeroTeamAtCrystal(this);

        if (debug.IsDebugging) Debug.Log("All heroes have reached the endpoint.");
    }

    /// <summary>
    /// Animates each hero one by one into their assigned combat slots on the
    /// current floor. Links the <see cref="HeroSlotsAddon"/> and
    /// <see cref="CoreUnit"/>, then sets state to
    /// <see cref="HeroTeamStates.InCombat"/> on completion.
    /// </summary>
    public IEnumerator MoveHeroesIntoSlots()
    {
        TowerSectionAddon floorSection = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
        HeroSlotsAddon heroSlots = floorSection.GetHeroSlots();

        if (heroSlots == null) { Debug.LogError("Error!!: Invalid find Hero Slots"); yield break; }

        if (debug.IsDebugging) Debug.Log("HeroTeam is being assigned to slots.");

        List<GameObject> slotObjects = heroSlots.GetSlots();
        if (slotObjects == null || slotObjects.Count == 0) { Debug.LogError("Slot objects are null or empty. Cannot move heroes."); yield break; }
        if (slotObjects.Count < heroes.Count) { Debug.LogWarning($"Not enough slots ({slotObjects.Count}) for heroes ({heroes.Count}). Only some heroes will be moved."); }

        heroSlots.SetHeroTeam(this);
        SetSlotsAddon(heroSlots);

        TowerSectionAddon towerAddon = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
        if (towerAddon != null) { towerAddon.RemoveHeroTeamToNonCombatTeams(this); }

        CoreSlotAddon coreAddon = towerAddon.GetCoreSlot();
        CoreUnit core = coreAddon.GetCoreUnit();
        if (core != null)
        {
            SetFloorCore(core);
            core.GetCoreInfo().ResetLockTime();
        }

        SetState(HeroTeam.HeroTeamStates.Moving);
        if (debug.IsDebugging) Debug.Log("HeroTeam state set to Moving.");

        for (int i = 0; i < GetHeroes().Count; i++)
        {
            HeroUnit hero = GetHeroes()[i];
            GameObject slot = slotObjects[i];

            if (debug.IsDebugging) Debug.Log($"Moving Hero to Slot {i} at position {slot.transform.position}");

            hero.SetAnimationState(HeroUnit.HeroAnimations.Run);
            yield return StartCoroutine(hero.MoveToTarget(slot));
            hero.SetAnimationState(HeroUnit.HeroAnimations.Idle);
        }

        SetState(HeroTeam.HeroTeamStates.InCombat);
        if (debug.IsDebugging) Debug.Log("HeroTeam state set to InCombat.");
    }

    /// <summary>
    /// Instantly places all heroes into their assigned combat slots with no
    /// movement animation. Requires <see cref="slotsAddon"/> to already be set.
    /// Sets state to <see cref="HeroTeamStates.InCombat"/> on completion.
    /// </summary>
    public void SnapHeroesIntoSlots()
    {
        if (GetHeroes() == null || GetHeroes().Count <= 0) return;
        if (GetSlotsAddon() == null) { Debug.LogError("Error!!: Invalid find Hero Slots"); return; }

        GetSlotsAddon().SetHeroTeam(this);
        if (debug.IsDebugging) Debug.Log("HeroTeam is being assigned to slots.");

        TowerSectionAddon towerAddon = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()));
        if (towerAddon != null) { towerAddon.RemoveHeroTeamToNonCombatTeams(this); }

        List<GameObject> slotObjects = GetSlotsAddon().GetSlots();
        if (slotObjects == null || slotObjects.Count == 0) { Debug.LogError("Slot objects are null or empty. Cannot move heroes."); return; }
        if (slotObjects.Count < heroes.Count) { Debug.LogWarning($"Not enough slots ({slotObjects.Count}) for heroes ({heroes.Count}). Only some heroes will be moved."); }

        SetState(HeroTeam.HeroTeamStates.Moving);

        for (int i = 0; i < GetHeroes().Count; i++)
        {
            HeroUnit hero = GetHeroes()[i];
            GameObject slot = slotObjects[i];

            hero.SnapToTarget(slot);
            hero.SetAnimationState(HeroUnit.HeroAnimations.Idle);

            if (debug.IsDebugging) Debug.Log($"Hero {i} snapped to Slot {i} and set to Idle.");
        }

        SetState(HeroTeam.HeroTeamStates.InCombat);
        if (debug.IsDebugging) Debug.Log("HeroTeam state set to InCombat.");
    }

    /// <summary>
    /// Repositions the team UI to the average world position of all living heroes.
    /// Skips silently if the hero list or team UI is missing.
    /// </summary>
    private void UpdateTeamUiPosition()
    {
        if (GetHeroes() == null || GetHeroes().Count == 0 || GetTeamUi() == null) return;

        Vector3 averagePosition = Vector3.zero;
        foreach (HeroUnit hero in GetHeroes())
        {
            averagePosition += hero.gameObject.transform.position;
        }
        averagePosition /= GetHeroes().Count;

        GetTeamUi().transform.position = averagePosition;
    }

    // -------------------------------------------------------------------------
    // Team Actions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Increments each hero's action bar by their speed stat, with a 0.5 s delay
    /// between heroes, and returns each hero to Idle afterward.
    /// </summary>
    public IEnumerator TeamGainEnergy()
    {
        for (int i = 0; i < GetHeroes().Count; i++)
        {
            HeroUnit hero = GetHeroes()[i];
            hero.AddToActionBar(hero.GetUnitInfo().GetSpeed());

            if (debug.IsDebugging)
                Debug.Log($"{hero.GetUnitInfo().GetName()}'s Action Bar updated to {hero.GetUnitInfo().GetActionBar()}");

            yield return new WaitForSeconds(0.5f);

            hero.SetAnimationState(HeroUnit.HeroAnimations.Idle);
        }
    }

    /// <summary>
    /// Steps through each hero in order. If the hero's action bar is full, it
    /// executes its ability against the current floor's monster team (or the core
    /// if no monsters remain), or attacks the boss if on the top floor.
    /// Waits 0.5 s between each hero.
    /// </summary>
    public IEnumerator TeamExecuteAction()
    {
        foreach (HeroUnit hero in GetHeroes())
        {
            if (debug.IsDebugging) Debug.Log("Processing action for Hero.");

            MonsterTeam monsterTeam = GetMonsterTeamOnFloor();
            bool onTopFloor = (GetCurrentFloor() >= (GetTowerManager().GetFloors().Count - 1));

            if (!onTopFloor)
            {
                if (monsterTeam != null && monsterTeam.GetMonsters().Count > 0)
                {
                    if (hero.GetUnitInfo().GetActionBar() >= hero.GetUnitInfo().GetActionBarMax())
                    {
                        yield return hero.StartCoroutine(hero.ExecuteAction(hero.GetUnitInfo(), monsterTeam, this));
                        hero.ResetActionBar();
                    }
                }
                else
                {
                    CoreUnit core = GetTowerManager()
                        .GetTowerSectionAddon(GetTowerManager().GetFloor(GetCurrentFloor()))
                        .GetCoreSlot().GetCoreUnit();

                    if (core != null) { hero.ExecuteHeroHitCore(hero.GetUnitInfo(), core); }
                }
            }
            else
            {
                hero.ExecuteHeroHitBoss(hero.GetUnitInfo(), monsterTeam, this);
            }

            yield return new WaitForSeconds(0.5f);
        }

        if (debug.IsDebugging) Debug.Log("All heroes in HeroTeam have completed their actions.");
    }

    /// <summary>
    /// Plays <see cref="HeroUnit.HeroAnimations.Action1"/> simultaneously on all
    /// heroes for 2.5 s, simulating a synchronized crystal attack, then returns
    /// them all to Idle.
    /// </summary>
    public IEnumerator TeamAttackCrystal()
    {
        foreach (HeroUnit hero in GetHeroes())
        {
            hero.SetAnimationState(HeroUnit.HeroAnimations.Action1);
        }

        yield return new WaitForSeconds(2.5f);

        foreach (HeroUnit hero in GetHeroes())
        {
            hero.SetAnimationState(HeroUnit.HeroAnimations.Idle);
        }

        if (debug.IsDebugging) Debug.Log("All heroes in HeroTeam have completed their crystal attack.");
    }

    /// <summary>
    /// Moves each hero to the floor's jump point then launches them out of the
    /// tower in staggered, randomized arcs. Sets <see cref="escapedTower"/> to
    /// <c>true</c> once all heroes have landed.
    /// </summary>
    public IEnumerator TeamRunToCenterAndJumpOut()
    {
        SetEscapedTower(false);

        TowerSectionAddon floorSection = GetFloorAddon();
        GameObject jumpPoint = floorSection.GetJumpPoint();
        GameObject positionObject = GetTowerManager().GetTowerPositionObject();

        if (jumpPoint == null)
        {
            Debug.LogError("Jump point not found. Cannot proceed with jumping.");
            yield break;
        }

        float upPower = 8f;
        float forwardPower = 35f;
        float maxFallSpeed = -5f;
        float gravity = -9.8f;
        float randomAngleRange = 45f;
        float minAngleDiff = 5f;

        List<float> previousAngles = new List<float>();
        List<Coroutine> jumpCoroutines = new List<Coroutine>();

        foreach (HeroUnit hero in GetHeroes())
        {
            jumpCoroutines.Add(StartCoroutine(HeroJump(hero, jumpPoint, positionObject, upPower, forwardPower, maxFallSpeed, gravity, randomAngleRange, minAngleDiff, previousAngles)));
            yield return new WaitForSeconds(0.25f);
        }

        foreach (Coroutine jump in jumpCoroutines) { yield return jump; }

        yield return new WaitForSeconds(0.5f);
        SetEscapedTower(true);
    }

    /// <summary>
    /// Moves a single hero to the jump point, then launches it in a randomized
    /// parabolic arc out of the tower, landing at the tower position object's Y level.
    /// Ensures a minimum angular separation between heroes' jump directions.
    /// </summary>
    /// <param name="hero">The hero to launch.</param>
    /// <param name="jumpPoint">The takeoff waypoint.</param>
    /// <param name="positionObject">Object whose Y coordinate defines the landing plane.</param>
    /// <param name="upPower">Initial upward velocity.</param>
    /// <param name="forwardPower">Initial forward velocity.</param>
    /// <param name="maxFallSpeed">Minimum (most negative) vertical speed before the arc ends.</param>
    /// <param name="gravity">Downward acceleration applied each frame.</param>
    /// <param name="randomAngleRange">�range in degrees for the random jump direction.</param>
    /// <param name="minAngleDifference">Minimum degrees between this hero's angle and any previous.</param>
    /// <param name="previousAngles">Accumulating list of chosen angles used for separation checks.</param>
    private IEnumerator HeroJump(
        HeroUnit hero, GameObject jumpPoint, GameObject positionObject,
        float upPower, float forwardPower, float maxFallSpeed, float gravity,
        float randomAngleRange, float minAngleDifference, List<float> previousAngles)
    {
        yield return StartCoroutine(hero.MoveToPosition(jumpPoint.transform.position, jumpPoint.transform.rotation));

        float randomAngle;
        do
        {
            randomAngle = UnityEngine.Random.Range(-randomAngleRange, randomAngleRange);
        }
        while (previousAngles.Exists(a => Mathf.Abs(Mathf.DeltaAngle(a, randomAngle)) < minAngleDifference));

        previousAngles.Add(randomAngle);

        Vector3 jumpDirection = Quaternion.Euler(0f, randomAngle, 0f) * jumpPoint.transform.forward;
        Vector3 currentPosition = hero.transform.position;
        float verticalSpeed = upPower;
        float forwardSpeed = forwardPower;
        float targetY = positionObject.transform.position.y;

        while (verticalSpeed > maxFallSpeed || currentPosition.y > targetY)
        {
            verticalSpeed += gravity * Time.deltaTime;
            forwardSpeed = Mathf.Max(0f, forwardSpeed - Time.deltaTime * forwardPower);
            currentPosition += jumpDirection * forwardSpeed * Time.deltaTime + Vector3.up * verticalSpeed * Time.deltaTime;
            hero.transform.position = currentPosition;
            yield return null;
        }

        currentPosition.y = targetY;
        hero.transform.position = currentPosition;
        hero.SetAnimationState(HeroUnit.HeroAnimations.Idle);
    }

    // -------------------------------------------------------------------------
    // Animations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Calls <see cref="HeroUnit.UpdateHeroSpeeds"/> on every living hero to sync
    /// their move and animation speeds with the current <see cref="HeroManager"/> settings.
    /// </summary>
    public void UpdateHeroesAnimationSpeed()
    {
        List<HeroUnit> heroes = GetHeroes();
        for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i] != null) { heroes[i].UpdateHeroSpeeds(); }
        }
    }

    // -------------------------------------------------------------------------
    // Getters & Setters
    // -------------------------------------------------------------------------

    /// <returns><c>true</c> if the team has successfully escaped the tower.</returns>
    public bool GetEscapedTower() => escapedTower;
    private void SetEscapedTower(bool newEscapedTower) => escapedTower = newEscapedTower;

    private void SetFloorObject(GameObject newFloorObject) => floorObject = newFloorObject;
    private GameObject GetFloorObject() => floorObject;

    private void SetFloorAddon(TowerSectionAddon newFloorAddon) => floorAddon = newFloorAddon;
    private TowerSectionAddon GetFloorAddon() => floorAddon;

    private void SetHeroes(List<HeroUnit> newHeroes) => heroes = newHeroes;
    /// <returns>The list of living <see cref="HeroUnit"/>s on this team.</returns>
    public List<HeroUnit> GetHeroes() => heroes;

    private void SetTeamUi(GameObject newTeamUi) => teamUi = newTeamUi;
    private GameObject GetTeamUi() => teamUi;

    private void SetTeamUiAddon(HeroTeamUiAddon newAddon) => teamUiAddon = newAddon;
    private HeroTeamUiAddon GetTeamUiAddon() => teamUiAddon;

    private void SetSlotsAddon(HeroSlotsAddon newSlots) => slotsAddon = newSlots;
    private HeroSlotsAddon GetSlotsAddon() => slotsAddon;

    private void SetFloorCore(CoreUnit newFloorCore) => floorCore = newFloorCore;
    private CoreUnit GetFloorCore() => floorCore;
    private void ResetFloorCore() => floorCore = null;

    private void SetFloorPath(List<GameObject> newFloorPath) => floorPath = newFloorPath;
    /// <returns>The ordered list of floor waypoints for the current floor.</returns>
    public List<GameObject> GetFloorPath() => floorPath;

    private void SetState(HeroTeamStates newState) => heroTeamState = newState;
    /// <returns>The current <see cref="HeroTeamStates"/> of this team.</returns>
    public HeroTeamStates GetState() => heroTeamState;

    /// <summary>
    /// Sets the current floor index and immediately calls
    /// <see cref="UpdateFloorInformation"/> to refresh all floor references.
    /// </summary>
    /// <param name="newCurrentFloor">The new floor index.</param>
    public void SetCurrentFloor(int newCurrentFloor) { currentFloor = newCurrentFloor; UpdateFloorInformation(); }
    /// <returns>The index of the floor this team is currently on.</returns>
    public int GetCurrentFloor() => currentFloor;

    private void SetSpawnNumber(int newSpawnNumber) => spawnNumber = newSpawnNumber;
    /// <returns>The spawn-order index for this team on its current floor.</returns>
    public int GetSpawnNumber() => spawnNumber;

    private void SetIsInitalized(bool newInitalized) => isInitalized = newInitalized;
    /// <returns><c>true</c> if <see cref="InitializeHeroTeam"/> has completed.</returns>
    public bool GetIsInitalized() => isInitalized;

    /// <param name="newMonsterTeam">The <see cref="MonsterTeam"/> this team is now fighting.</param>
    public void SetMonsterTeamInCombatWith(MonsterTeam newMonsterTeam) => monsterTeamInCombatWith = newMonsterTeam;
    /// <returns>The <see cref="MonsterTeam"/> this team is currently fighting, or <c>null</c>.</returns>
    public MonsterTeam GetMonsterTeamInCombatWith() => monsterTeamInCombatWith;

    /// <returns><c>true</c> if the team's state is <see cref="HeroTeamStates.InCombat"/>.</returns>
    public bool GetIsInCombat() => heroTeamState == HeroTeamStates.InCombat;

    /// <returns>The unique hero team ID.</returns>
    public int GetHtID() => htID;
    /// <param name="newHtID">New hero team ID to assign.</param>
    public void SetHtID(int newHtID) => htID = newHtID;

    /// <returns><c>true</c> if the team is locked on its current floor.</returns>
    public bool GetIsLockedOnFloor() => isLocked;
    /// <param name="newIsLocked">New locked state to assign.</param>
    public void SetIsLockedOnFloor(bool newIsLocked) => isLocked = newIsLocked;

    /// <returns>The display name of this team.</returns>
    public string GetName() => teamName;
    /// <param name="newTeamName">New display name to assign.</param>
    public void SetName(string newTeamName) => teamName = newTeamName;

    /// <param name="newManager">The <see cref="TowerDefenseManager"/> to bind.</param>
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    /// <returns>The central <see cref="TowerDefenseManager"/>.</returns>
    public TowerDefenseManager GetGameManager() => GameManager;

    /// <returns>The <see cref="TowerManager"/> retrieved via the game manager.</returns>
    public TowerManager GetTowerManager() => GetGameManager().GetTowerManager();
    /// <returns>The <see cref="HeroManager"/> retrieved via the game manager.</returns>
    public HeroManager GetHeroManager() => GetGameManager().GetHeroManager();
    /// <returns>The <see cref="CoreManager"/> retrieved via the game manager.</returns>
    public CoreManager GetCoreManager() => GetGameManager().GetCoreManager();

    /// <summary>
    /// Returns the <see cref="MonsterTeam"/> currently occupying the combat slots
    /// on the same floor as this team, or <c>null</c> if none is present.
    /// </summary>
    private MonsterTeam GetMonsterTeamOnFloor()
    {
        GameObject monsterFloor = GetTowerManager().GetFloor(GetCurrentFloor());
        MonsterSlotsAddon monsterSlots = GetTowerManager().GetTowerSectionAddon(monsterFloor).GetMonsterSlots();
        return monsterSlots.GetMonsterTeam();
    }
}