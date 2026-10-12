using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.TextCore.Text;

/// <summary>
/// Represents a group of up to four <see cref="MonsterUnit"/>s occupying the
/// combat slots of a single tower floor. Manages initialization, slot assignment,
/// combat actions (including boss behaviour), death handling, and animator speed
/// propagation. Boss teams follow a distinct state machine with relaxing/combat/throne
/// transitions driven by <see cref="BossTeamCheckToEnterCombat"/> and
/// <see cref="BossTeamCheckToLeaveCombat"/>.
/// </summary>
public class MonsterTeam : MonoBehaviour
{
    #region // Variables

    #region // Enums

    /// <summary>
    /// High-level states describing the team's current combat status.
    /// </summary>
    public enum MonsterTeamStates
    {
        /// <summary>Team exists but has not yet been placed in the tower.</summary>
        Inactive,
        /// <summary>Team is at the start waypoint of its floor (unused for monsters currently).</summary>
        AtStartPoint,
        /// <summary>Team has reached the end waypoint of its floor (unused for monsters currently).</summary>
        AtEndPoint,
        /// <summary>Regular team is occupying combat slots and fighting a <see cref="HeroTeam"/>.</summary>
        InCombat,
        /// <summary>Boss is seated on the throne, waiting for a hero team to arrive.</summary>
        BossRelaxing,
        /// <summary>Boss is in the process of standing up and moving to its combat position.</summary>
        BossEnteringCombat,
        /// <summary>Boss is in its combat slots and actively fighting.</summary>
        BossInCombat,
        /// <summary>Boss has defeated all heroes and is returning to the throne.</summary>
        BossLeavingCombat,
        /// <summary>Team or individual monsters are currently moving.</summary>
        Moving,
        /// <summary>All monsters are dead; team is being cleaned up.</summary>
        Destroyed,
    }

    #endregion

    #region // References

    [Header("References")]
    [Tooltip("All living monster units that belong to this team.")]
    [SerializeField] private List<MonsterUnit> monsters;

    [Tooltip("Trap units associated with this team (reserved for future use).")]
    [SerializeField] private List<TrapUnit> traps;

    [Tooltip("The CoreUnit on the current floor, linked during initialization.")]
    [SerializeField] private CoreUnit core;

    [Tooltip("The HeroTeam currently fighting this monster team, or null if none.")]
    [SerializeField] private HeroTeam heroTeamInCombatWith;

    [Tooltip("Reference to the central TowerDefenseManager.")]
    [SerializeField] private TowerDefenseManager GameManager;

    [Tooltip("The floor GameObject this team is currently on.")]
    [SerializeField] private GameObject floorObject;

    [Tooltip("The TowerSectionAddon of the floor this team is on.")]
    [SerializeField] private TowerSectionAddon floorAddon;

    [Tooltip("The MonsterSlotsAddon for the current floor.")]
    [SerializeField] private MonsterSlotsAddon slotsAddon;

    #endregion

    #region // Team Information

    [Header("Team Information")]
    [Tooltip("Current high-level state of this team.")]
    [SerializeField] private MonsterTeamStates monsterTeamState = MonsterTeamStates.Inactive;

    [Tooltip("Index of the tower floor this team is currently on.")]
    [SerializeField] private int currentFloor;

    [Tooltip("Spawn-order index used to differentiate multiple teams on the same floor.")]
    [SerializeField] private int spawnNumber;

    [Tooltip("Unique string ID for this monster team (e.g. 'MT1').")]
    [SerializeField] private string mtID;

    [Tooltip("Whether this team has been fully initialized.")]
    [SerializeField] private bool isInitalized = false;

    [Tooltip("True when this is the boss team.")]
    [SerializeField] private bool isBossTeam = false;

    [Tooltip("Movement speed for monsters in this team.")]
    [SerializeField] private float moveSpeed = 1f;

    [Tooltip("Angular speed used when rotating toward a target.")]
    [SerializeField] private float rotationSpeed = 3;

    [Tooltip("Maximum number of monsters allowed in this team (derived from the floor's CoreUnit).")]
    [SerializeField] private int maxTeamSize = 4;

    [Tooltip("True once the floor this team is on has been destroyed; no new monsters can be added.")]
    [SerializeField] private bool isDying;

    #endregion

    #region // Debugging

    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;

    #endregion

    #region // Public Getters

    [Header("Public Getters")]
    public int CurrentFloor => currentFloor;
    public int SpawnNumber => spawnNumber;
    public MonsterTeamStates MonsterTeamState => monsterTeamState;

    #endregion

    #endregion

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake() { }
    private void Start() { }

    /// <summary>
    /// Each frame refreshes debug watches. Active death checking is currently
    /// commented out (see <c>CheckIfTeamIsDead</c>).
    /// </summary>
    private void Update()
    {
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
        debug.SetWatch("State", monsterTeamState.ToString());
        debug.SetWatch("Floor", currentFloor.ToString());
        debug.SetWatch("Monsters", monsters != null ? monsters.Count.ToString() : "null");
        debug.SetWatch("Is Boss", isBossTeam.ToString());
        debug.SetWatch("Is Dying", isDying.ToString());
        debug.SetWatch("Team ID", mtID ?? "null");
        debug.SetWatch("Hero Opp", heroTeamInCombatWith != null ? heroTeamInCombatWith.GetName() : "none");
        debug.SetWatch("Has Slots", slotsAddon != null ? "yes" : "no");

        if (monsters != null && monsters.Count > 0)
        {
            int totalHp = monsters.Sum(m => m.GetUnitInfo() != null ? m.GetUnitInfo().GetHealth() : 0);
            debug.SetWatch("Total HP", totalHp.ToString());
        }
    }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fully initializes the monster team: links the manager, sets floor and ID,
    /// spawns monsters into their slots, and sets the initial combat state.
    /// Boss teams enter <see cref="MonsterTeamStates.BossInCombat"/> instead of
    /// <see cref="MonsterTeamStates.InCombat"/>.
    /// </summary>
    /// <param name="manager">The central <see cref="TowerDefenseManager"/>.</param>
    /// <param name="newMonsters">Descriptors for each monster to spawn.</param>
    /// <param name="mtID">Unique team ID string.</param>
    /// <param name="floorToSpawnOn">The floor index to spawn on.</param>
    /// <param name="isBoss">Whether this is the boss team.</param>
    public void InitializeMonsterTeam(TowerDefenseManager manager, List<NewMonster> newMonsters, string mtID, int floorToSpawnOn, bool isBoss = false)
    {
        InitalizeDebug();
        InitalizeList();
        SetGameManager(manager);
        SetCurrentFloor(floorToSpawnOn);
        InitializeTeamSize();
        SetMtID(mtID);
        SetIsBossTeam(isBoss);
        SetupMonsterTeam(newMonsters, floorToSpawnOn);
        SetState(MonsterTeamStates.InCombat);
        if (GetIsBossTeam()) { SetState(MonsterTeamStates.BossInCombat); }
        SetIsInitalized(true);
    }

    /// <summary>
    /// Initializes an empty monster team with no monsters. Useful for pre-populating
    /// a floor slot before monsters are added via <see cref="AddMonsterToTeam"/>.
    /// </summary>
    /// <param name="manager">The central <see cref="TowerDefenseManager"/>.</param>
    /// <param name="mtID">Unique team ID string.</param>
    /// <param name="floorToSpawnOn">The floor index to spawn on.</param>
    public void InitializeEmptyMonsterTeam(TowerDefenseManager manager, string mtID, int floorToSpawnOn)
    {
        InitalizeDebug();
        InitalizeList();
        SetGameManager(manager);
        SetCurrentFloor(floorToSpawnOn);
        InitializeTeamSize();
        SetMtID(mtID);
        SetIsBossTeam(false);
        SetState(MonsterTeamStates.InCombat);
        SetIsInitalized(true);
    }

    /// <summary>
    /// Reads the team size from the floor's <see cref="CoreUnit"/> and caches it.
    /// Falls back silently if the floor addon is not yet set.
    /// </summary>
    private void InitializeTeamSize()
    {
        TowerSectionAddon floorAddon = GetFloorAddon();
        if (floorAddon != null) return;
        CoreUnit core = floorAddon.GetCoreSlot().GetCoreUnit();
        int teamSize = core.GetCoreInfo().GetTeamSize();
        SetMaxTeamSize(teamSize);
    }

    /// <summary>Resets the monsters list to a new empty instance.</summary>
    private void InitalizeList() { monsters = new List<MonsterUnit>(); }

    /// <summary>Creates a fresh <see cref="DebugUtility"/> for this team.</summary>
    private void InitalizeDebug() { debug = new DebugUtility(false); }

    /// <summary>
    /// Spawns each monster described in <paramref name="newMonsters"/>, parents
    /// them to this transform, and snaps them into their combat slots.
    /// </summary>
    /// <param name="newMonsters">Descriptors for the monsters to create.</param>
    /// <param name="floorToSpawnOn">Floor index (used to set current floor).</param>
    private void SetupMonsterTeam(List<NewMonster> newMonsters, int floorToSpawnOn = 0)
    {
        if (debug.IsDebugging)
            Debug.Log($"Setting up monster team for floor {floorToSpawnOn} with {newMonsters.Count} monsters.");

        SetCurrentFloor(floorToSpawnOn);

        for (int i = 0; i < newMonsters.Count; i++)
        {
            if (debug.IsDebugging)
                Debug.Log($"Processing monster at index {i}: Class = {newMonsters[i].monsterClass}, Level = {newMonsters[i].level}");

            string muID = $"{GetMtID()}U1";

            GameObject newMonsterObject = new GameObject("Monster " + muID);
            newMonsterObject.transform.SetParent(this.transform);

            MonsterUnit newMonsterUnit = newMonsterObject.AddComponent<MonsterUnit>();
            newMonsterUnit.InitializeMonster(GetGameManager(), this, muID, newMonsters[i].monsterClass, newMonsters[i].level);

            GetMonsters().Add(newMonsterUnit);

            if (debug.IsDebugging)
                Debug.Log($"Monster {i} initialized: {monsters[i].name} (Class = {newMonsters[i].monsterClass}, Level = {newMonsters[i].level})");
        }

        SnapMonstersIntoSlots();

        if (debug.IsDebugging) Debug.Log("All monsters snapped into slots. Monster team setup complete.");
    }

    // -------------------------------------------------------------------------
    // Monster Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new <see cref="MonsterUnit"/> and adds it to the team at the
    /// given slot position, re-sorting the team and snapping all monsters into
    /// their slots afterward.
    /// </summary>
    /// <param name="newMonster">Descriptor for the monster to add.</param>
    /// <param name="slotPosition">The 1-based slot index to place the monster in.</param>
    public void AddMonsterToTeam(NewMonster newMonster, int slotPosition)
    {
        if (debug.IsDebugging)
            Debug.Log($"Processing monster: Class = {newMonster.monsterClass}, Level = {newMonster.level}, Slot Position = {slotPosition}");

        GameObject newMonsterObject = new GameObject("Monster " + slotPosition);
        newMonsterObject.transform.SetParent(this.transform);

        MonsterUnit newMonsterUnit = newMonsterObject.AddComponent<MonsterUnit>();
        string muID = $"{GetMtID()}U{slotPosition}";

        newMonsterUnit.InitializeMonster(GetGameManager(), this, muID, newMonster.monsterClass, newMonster.level);
        GetMonsters().Add(newMonsterUnit);

        if (debug.IsDebugging)
            Debug.Log($"Monster added to team. Total monsters: {GetMonsters().Count}");

        try
        {
            SortMonsterTeam();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while sorting monsters: {e.Message}\n{e.StackTrace}");
            return;
        }

        SnapMonstersIntoSlots();
    }

    /// <summary>
    /// Sorts the monster list by slot position using a bubble sort, ensuring
    /// monsters appear in ascending slot order for sequential slot assignment.
    /// </summary>
    public void SortMonsterTeam()
    {
        if (debug.IsDebugging) Debug.Log("Manually sorting the monster team.");
        if (GetMonsters().Count <= 1) return;

        for (int i = 0; i < GetMonsters().Count - 1; i++)
        {
            for (int j = i + 1; j < GetMonsters().Count; j++)
            {
                MonsterUnit a = GetMonsters()[i];
                MonsterUnit b = GetMonsters()[j];

                if (a.GetSlotPosition() > b.GetSlotPosition())
                {
                    if (debug.IsDebugging)
                        Debug.Log($"Swapping {a.name} (Slot {a.GetSlotPosition()}) with {b.name} (Slot {b.GetSlotPosition()})");

                    GetMonsters()[i] = b;
                    GetMonsters()[j] = a;
                }
            }
        }

        if (debug.IsDebugging) Debug.Log("Manual sorting complete.");
    }

    // -------------------------------------------------------------------------
    // Team Removal / Death
    // -------------------------------------------------------------------------

    /// <summary>
    /// Marks the team as dying, zeros out all monster health, triggers their
    /// death sequences, and removes the team.
    /// </summary>
    public void StartDying()
    {
        if (GetIsDying()) return;
        SetIsDying(true);

        if (GetMonsters() == null || GetMonsters().Count <= 0) return;

        foreach (MonsterUnit unit in GetMonsters())
        {
            unit.GetUnitInfo().SetHealth(0);
            unit.CheckForDeath();
        }

        RemoveTeam();
    }

    /// <summary>
    /// Severs this team's links to its combat slot addon and any
    /// <see cref="HeroTeam"/> it was fighting, without destroying anything.
    /// </summary>
    public void UnlinkTeam()
    {
        if (GetSlotsAddon() != null) { GetSlotsAddon().ClearMonsterSlots(); }
        if (GetHeroTeamInCombatWith() != null) { GetHeroTeamInCombatWith().SetMonsterTeamInCombatWith(null); }
    }

    /// <summary>
    /// Full team teardown: destroys all monster GameObjects, unlinks combat
    /// references, deregisters from <see cref="MonsterManager"/>, and destroys
    /// this GameObject.
    /// </summary>
    public void RemoveTeam()
    {
        if (GetMonsters() != null && GetMonsters().Count > 0)
        {
            foreach (MonsterUnit unit in GetMonsters()) { Destroy(unit); }
        }

        UnlinkTeam();
        GetGameManager().GetMonsterManager().RemoveMonsterTeam(GetMtID());
        Destroy(gameObject);
    }

    /// <summary>
    /// Coroutine that waits for a delay before destroying the team's GameObject
    /// and component. Currently unused in active code paths.
    /// </summary>
    /// <param name="delay">Seconds to wait before destruction.</param>
    private IEnumerator DestroyTeamAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
        Destroy(this);
    }

    // -------------------------------------------------------------------------
    // Floor Information
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes all floor-specific references (floor GameObject, section addon,
    /// slots addon, and hero team) based on <see cref="currentFloor"/>. Also
    /// re-snaps monsters into slots if the slots addon is present.
    /// Called automatically whenever <see cref="SetCurrentFloor"/> is invoked.
    /// </summary>
    private void UpdateFloorInformation()
    {
        int floor = GetCurrentFloor();
        if (debug.IsDebugging) Debug.Log($"Updating floor {floor} information.");

        GameObject floorObj = GetTowerManager().GetFloor(floor);
        TowerSectionAddon addon = floorObj != null ? GetTowerManager().GetTowerSectionAddon(floorObj) : null;
        MonsterSlotsAddon slots = addon != null ? addon.GetMonsterSlots() : null;
        HeroTeam heroTeam = addon != null ? addon.GetHeroSlots()?.GetHeroTeam() : null;

        SetFloorObject(floorObj);
        SetFloorAddon(addon);
        SetSlotsAddon(slots);
        SetHeroTeamInCombatWith(heroTeam);

        if (GetSlotsAddon() != null)
        {
            GetSlotsAddon().SetMonsterTeam(this);
            SnapMonstersIntoSlots();
        }

        if (debug.IsDebugging) Debug.Log("UpdateFloorInformation completed.");
    }

    /// <summary>
    /// Queries the current floor's <see cref="HeroSlotsAddon"/> for the
    /// occupying <see cref="HeroTeam"/> and updates cross-references in both
    /// directions. Safe to call every round.
    /// </summary>
    public void UpdateHeroTeamOnFloor()
    {
        HeroSlotsAddon heroSlots = GetTowerManager().GetTowerSectionAddon(GetFloorObject()).GetHeroSlots();
        HeroTeam heroTeam = heroSlots.GetHeroTeam();
        SetHeroTeamInCombatWith(heroTeam);
        if (heroTeam != null) { heroTeam.SetMonsterTeamInCombatWith(this); }
    }

    // -------------------------------------------------------------------------
    // Movement / Slot Assignment
    // -------------------------------------------------------------------------

    /// <summary>
    /// Instantly places all monsters into their assigned combat slots with no
    /// movement animation. Links each monster to its <see cref="MonsterSlotAddon"/>
    /// and vice versa. Sets state to <see cref="MonsterTeamStates.InCombat"/>
    /// (or <see cref="MonsterTeamStates.BossInCombat"/> for boss teams) on completion.
    /// </summary>
    public void SnapMonstersIntoSlots()
    {
        List<MonsterUnit> monsters = GetMonsters();
        MonsterSlotsAddon slotsAddon = GetSlotsAddon();
        List<MonsterSlotAddon> slotAddons = slotsAddon.GetSlots();

        if (debug.IsDebugging)
        {
            Debug.Log($"Total slots found: {slotAddons.Count}");
            for (int j = 0; j < slotAddons.Count; j++)
                Debug.Log($"Slot {j + 1}: {slotAddons[j].gameObject.name}, Position: {slotAddons[j].gameObject.transform.position}");
        }

        if (slotsAddon == null) { Debug.LogError("Error!!: Invalid find Monster Slots"); return; }
        if (monsters == null || monsters.Count <= 0) { Debug.LogWarning("Warning!!: No monsters to snap into slots"); return; }
        if (slotAddons == null || slotAddons.Count == 0) { Debug.LogError("Slot objects are null or empty. Cannot move monsters."); return; }

        SetState(MonsterTeam.MonsterTeamStates.Moving);

        foreach (var monster in monsters)
        {
            int slotPosition = monster.GetSlotPosition();
            if (slotPosition < 1 || slotPosition > slotAddons.Count)
            {
                Debug.LogError($"Monster '{monster.name}' has an invalid slot position: {slotPosition}. Skipping.");
                continue;
            }

            MonsterSlotAddon targetSlot = slotAddons[slotPosition - 1];
            monster.SnapToTarget(targetSlot.gameObject);
            monster.SetSlotAddon(targetSlot);
            targetSlot.SetMonster(monster);

            if (debug.IsDebugging)
                Debug.Log($"Monster '{monster.name}' snapped to slot {slotPosition}.");
        }

        SetState(MonsterTeam.MonsterTeamStates.InCombat);
        if (GetIsBossTeam()) { SetState(MonsterTeamStates.BossInCombat); }

        if (debug.IsDebugging) Debug.Log("MonsterTeam state set to InCombat.");
    }

    // -------------------------------------------------------------------------
    // Team Actions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Increments each monster's action bar by their speed stat, with a 0.5 s
    /// delay between monsters, and returns each to Idle afterward.
    /// </summary>
    public IEnumerator TeamGainEnergy()
    {
        if (debug.IsDebugging) Debug.Log($"Executing Team Gain Energy Action for: {GetMtID()}");
        OutputMonsterTeamInfo();

        List<MonsterUnit> monsters = GetMonsters();
        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster == null) continue;

            monster.AddToActionBar(monster.GetUnitInfo().GetSpeed());

            if (debug.IsDebugging)
                Debug.Log($"{monster.GetUnitInfo().GetName()}'s Action Bar updated to {monster.GetUnitInfo().GetActionBar()}");

            yield return new WaitForSeconds(0.5f);
            monster.SetAnimationState(MonsterUnit.MonsterAnimations.Idle);
        }
    }

    /// <summary>
    /// Steps through each living monster in order. Non-boss monsters attack when
    /// their action bar is full. Boss monsters execute a full-team swipe each turn
    /// regardless of action bar state. Waits 0.5 s between each monster.
    /// </summary>
    public IEnumerator TeamExecuteAction()
    {
        HeroTeam heroTeam = GetHeroTeamInCombatWith();

        foreach (MonsterUnit monster in monsters)
        {
            if (debug.IsDebugging) Debug.Log("Processing action for Monster.");

            if (!monster.GetIsDying())
            {
                if (!GetIsBossTeam())
                {
                    if (monster.GetUnitInfo().GetActionBar() >= monster.GetUnitInfo().GetActionBarMax())
                    {
                        if (heroTeam == null || heroTeam.GetHeroes().Count <= 0) continue;
                        monster.ExecuteAction(monster.GetUnitInfo(), heroTeam, this);
                        monster.ResetActionBar();
                    }
                }
                else
                {
                    if (heroTeam == null || heroTeam.GetHeroes().Count <= 0) continue;
                    monster.ExecuteBossSwipeTeam(monster.GetUnitInfo(), heroTeam, this);
                }

                yield return new WaitForSeconds(0.5f);
                monster.SetAnimationState(MonsterUnit.MonsterAnimations.Idle);
            }
        }

        if (debug.IsDebugging) Debug.Log("All monsters in MonsterTeam have completed their actions.");
    }

    /// <summary>
    /// Boss combat entry sequence: if a hero team is present, transitions the boss
    /// from <see cref="MonsterTeamStates.BossRelaxing"/> through
    /// <see cref="MonsterTeamStates.BossEnteringCombat"/> to
    /// <see cref="MonsterTeamStates.BossInCombat"/>.
    /// </summary>
    public IEnumerator BossTeamCheckToEnterCombat()
    {
        HeroTeam heroTeam = GetHeroTeamInCombatWith();

        if (heroTeam != null && heroTeam.GetHeroes().Count > 0)
        {
            SetState(MonsterTeamStates.BossEnteringCombat);

            MonsterUnit bossUnit = monsters[0];
            yield return bossUnit.BossUnitStandUpFromThrone();
            yield return bossUnit.BossUnitMoveToCombatPosition();

            SetState(MonsterTeamStates.BossInCombat);
        }

        if (debug.IsDebugging) Debug.Log("Boss Team has entered combat and completed their actions.");
    }

    /// <summary>
    /// Boss combat exit sequence: if no hero team remains, transitions the boss
    /// from <see cref="MonsterTeamStates.BossInCombat"/> through
    /// <see cref="MonsterTeamStates.BossLeavingCombat"/> back to
    /// <see cref="MonsterTeamStates.BossRelaxing"/>.
    /// </summary>
    public IEnumerator BossTeamCheckToLeaveCombat()
    {
        HeroTeam heroTeam = GetHeroTeamInCombatWith();

        if (heroTeam == null || heroTeam.GetHeroes().Count == 0)
        {
            SetState(MonsterTeamStates.BossLeavingCombat);

            MonsterUnit bossUnit = monsters[0];
            yield return bossUnit.BossUnitMoveToThronePosition();
            yield return bossUnit.BossUnitSitOnThrone();

            SetState(MonsterTeamStates.BossRelaxing);
        }
    }

    /// <summary>Plays the <see cref="MonsterUnit.MonsterAnimations.Victory"/> animation on all monsters.</summary>
    public void TeamVictoryPose()
    {
        foreach (MonsterUnit monster in GetMonsters())
        {
            monster.SetAnimationState(MonsterUnit.MonsterAnimations.Victory);
        }
    }

    // -------------------------------------------------------------------------
    // Animations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Calls <see cref="MonsterUnit.UpdateMonsterSpeeds"/> on every living monster
    /// to sync their move and animation speeds with the current
    /// <see cref="MonsterManager"/> settings.
    /// </summary>
    public void UpdateMonstersAnimationSpeed()
    {
        List<MonsterUnit> monsters = GetMonsters();
        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] != null) monsters[i].UpdateMonsterSpeeds();
        }
    }

    // -------------------------------------------------------------------------
    // Debug Output
    // -------------------------------------------------------------------------

    /// <summary>
    /// Logs the team ID, monster count, and individual unit info for every
    /// monster in the team. Used for detailed runtime debugging.
    /// </summary>
    public void OutputMonsterTeamInfo()
    {
        Debug.Log($"=== Monster Team Info ===\nTeam ID: {GetMtID()}\nMonster Count: {GetMonsters().Count}");
        foreach (var monster in GetMonsters()) { monster.OutputMonsterUnitInfo(); }
        Debug.Log("=== End of Team Info ===");
    }

    // -------------------------------------------------------------------------
    // Getters & Setters
    // -------------------------------------------------------------------------

    private void SetMonsters(List<MonsterUnit> newMonsters) => monsters = newMonsters;
    /// <returns>The list of living <see cref="MonsterUnit"/>s on this team.</returns>
    public List<MonsterUnit> GetMonsters() => monsters;

    /// <returns>The maximum number of monsters this team can hold.</returns>
    public int GetMaxTeamSize() => maxTeamSize;
    /// <param name="newMaxTeamSize">New max team size, clamped 0–4.</param>
    public void SetMaxTeamSize(int newMaxTeamSize) => maxTeamSize = Mathf.Clamp(newMaxTeamSize, 0, 4);

    private void SetTraps(List<TrapUnit> newTraps) => traps = newTraps;
    /// <returns>The list of trap units associated with this team.</returns>
    public List<TrapUnit> GetTraps() => traps;

    private void SetCore(CoreUnit newCore) => core = newCore;
    /// <returns>The floor's <see cref="CoreUnit"/>, if linked.</returns>
    public CoreUnit GetCore() => core;

    private void SetState(MonsterTeamStates newState) => monsterTeamState = newState;
    /// <returns>The current <see cref="MonsterTeamStates"/> of this team.</returns>
    public MonsterTeamStates GetState() => monsterTeamState;

    /// <summary>
    /// Sets the current floor index and immediately calls
    /// <see cref="UpdateFloorInformation"/> to refresh all floor references.
    /// </summary>
    public void SetCurrentFloor(int newCurrentFloor) { currentFloor = newCurrentFloor; UpdateFloorInformation(); }
    /// <returns>The index of the floor this team is currently on.</returns>
    public int GetCurrentFloor() => currentFloor;

    private void SetSpawnNumber(int newSpawnNumber) => spawnNumber = newSpawnNumber;
    /// <returns>The spawn-order index for this team on its current floor.</returns>
    public int GetSpawnNumber() => spawnNumber;

    private void SetIsInitalized(bool newInitalized) => isInitalized = newInitalized;
    /// <returns><c>true</c> if this team has been fully initialized.</returns>
    public bool GetIsInitalized() => isInitalized;

    /// <param name="isBoss">Whether to designate this team as the boss team.</param>
    public void SetIsBossTeam(bool isBoss) => isBossTeam = isBoss;
    /// <returns><c>true</c> if this is the boss team.</returns>
    public bool GetIsBossTeam() => isBossTeam;

    /// <returns>The unique string ID for this monster team.</returns>
    public string GetMtID() => mtID;
    /// <param name="newMtID">New team ID to assign.</param>
    public void SetMtID(string newMtID) => mtID = newMtID;

    /// <summary>
    /// Returns the monster occupying the given 1-based slot position,
    /// or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="slotPosition">Slot index to search (1–4).</param>
    public MonsterUnit GetMonsterAtSlot(int slotPosition)
    {
        if (slotPosition < 1 || slotPosition > 4)
        {
            Debug.LogWarning($"Invalid slot position: {slotPosition}. Must be between 1 and 4.");
            return null;
        }

        foreach (MonsterUnit monster in GetMonsters())
        {
            if (monster == null) continue;
            if (monster.GetSlotPosition() == slotPosition) return monster;
        }

        if (debug.IsDebugging) Debug.Log($"No monster found at slot position {slotPosition}.");
        return null;
    }

    /// <param name="newIsDying">New dying state to assign.</param>
    public void SetIsDying(bool newIsDying) => isDying = newIsDying;
    /// <returns><c>true</c> if the team's floor has been destroyed.</returns>
    public bool GetIsDying() => isDying;

    private void SetFloorObject(GameObject newFloorObject) => floorObject = newFloorObject;
    private void SetFloorAddon(TowerSectionAddon newFloorAddon) => floorAddon = newFloorAddon;
    private void SetSlotsAddon(MonsterSlotsAddon newSlotsAddon) => slotsAddon = newSlotsAddon;
    private GameObject GetFloorObject() => floorObject;
    private TowerSectionAddon GetFloorAddon() => floorAddon;
    /// <returns>The <see cref="MonsterSlotsAddon"/> for the current floor.</returns>
    public MonsterSlotsAddon GetSlotsAddon() => slotsAddon;

    /// <param name="newHeroTeam">The <see cref="HeroTeam"/> now fighting this monster team.</param>
    public void SetHeroTeamInCombatWith(HeroTeam newHeroTeam) => heroTeamInCombatWith = newHeroTeam;
    /// <returns>The <see cref="HeroTeam"/> currently fighting this team, or <c>null</c>.</returns>
    public HeroTeam GetHeroTeamInCombatWith() => heroTeamInCombatWith;

    /// <param name="newManager">The <see cref="TowerDefenseManager"/> to bind.</param>
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    /// <returns>The central <see cref="TowerDefenseManager"/>.</returns>
    public TowerDefenseManager GetGameManager() => GameManager;

    /// <returns>The <see cref="TowerManager"/> retrieved via the game manager.</returns>
    public TowerManager GetTowerManager() => GetGameManager().GetTowerManager();
    /// <returns>The <see cref="HeroManager"/> retrieved via the game manager.</returns>
    public HeroManager GetHeroManager() => GetGameManager().GetHeroManager();
    /// <returns>The <see cref="MonsterManager"/> retrieved via the game manager.</returns>
    public MonsterManager GetMonsterManager() => GetGameManager().GetMonsterManager();
}