using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using static MonsterUnit;
using static TowerManager;

/// <summary>
/// Lightweight descriptor used to define a monster when spawning a new <see cref="MonsterTeam"/>.
/// Carries the monster's type and starting level; the actual <see cref="MonsterUnit"/> is
/// created at runtime by <see cref="MonsterManager.SpawnMonsterTeam"/>.
/// </summary>
public struct NewMonster
{
    /// <summary>The archetype type to assign to the new monster.</summary>
    public MonsterUnit.MonsterTypes monsterClass;

    /// <summary>The starting level to pass to <see cref="UnitInfo"/>.</summary>
    public int level;

    /// <param name="setClass">The monster type to assign.</param>
    /// <param name="setLevel">The starting level to assign.</param>
    public NewMonster(MonsterUnit.MonsterTypes setClass, int setLevel)
    {
        this.monsterClass = setClass;
        this.level = setLevel;
    }
}

/// <summary>
/// Manages the full lifecycle of monster teams throughout a level: spawning teams
/// (including the boss), routing each team through round-phase coroutines, tracking
/// unlock/lock status for each monster type, maintaining essence costs, and handling
/// the interactive monster-placement workflow.
/// </summary>
public class MonsterManager : MonoBehaviour
{
    #region // Variables

    /// <summary>
    /// Inspector-visible mirror of <see cref="levelPhases"/>, using
    /// <see cref="LevelPhase"/> strings for easier level design review.
    /// </summary>
    [SerializeField] private List<LevelPhase> levelPhaseInspectorView = new List<LevelPhase>();

    /// <summary>
    /// Runtime phase schedule: keyed by level index, each value is an ordered list
    /// of phases where each phase is a list of <see cref="NewMonster"/> descriptors.
    /// </summary>
    private Dictionary<int, List<List<NewMonster>>> levelPhases = new Dictionary<int, List<List<NewMonster>>>();

    /// <summary>
    /// Tracks whether each <see cref="MonsterUnit.MonsterTypes"/> is available
    /// for the player to summon. All types default to locked.
    /// </summary>
    private Dictionary<MonsterUnit.MonsterTypes, bool> monsterUnlockStatus;

    /// <summary>
    /// Maps each <see cref="MonsterUnit.MonsterTypes"/> to its essence cost.
    /// Initialized by <see cref="InitializeMonsterEssenceCosts"/>.
    /// </summary>
    private Dictionary<MonsterUnit.MonsterTypes, int> monsterEssenceCosts = new Dictionary<MonsterUnit.MonsterTypes, int>();

    [Tooltip("All monster teams currently active in the scene.")]
    [SerializeField] private List<MonsterTeam> monsterTeams = new List<MonsterTeam>();

    [Tooltip("String prefix used when generating unique monster team IDs (e.g. 'M').")]
    [SerializeField] public string ManagerID;

    [Tooltip("Running count of monster teams spawned this session, used to build unique IDs.")]
    [SerializeField] private int monsterTeamsSpawned;

    [Tooltip("Reference to the central TowerDefenseManager.")]
    [SerializeField] private TowerDefenseManager GameManager;

    [Header("Monster Model Prefabs")]
    [Tooltip("Prefab for the Slime monster model.")]
    public GameObject SlimeModel;

    [Tooltip("Prefab for the Bat monster model.")]
    public GameObject BatModel;

    [Tooltip("Prefab for the Spider monster model.")]
    public GameObject SpiderModel;

    [Tooltip("Prefab for the BossKnight monster model.")]
    public GameObject BossKnightModel;

    [Tooltip("Prefab for the Dummii monster model.")]
    public GameObject DummiiModel;

    [Tooltip("Prefab used as the root GameObject for new MonsterTeam instances.")]
    public GameObject MonsterTeamPrefab;

    [Header("Speed Settings")]
    [Tooltip("Movement speed for all monsters at 1x game speed.")]
    [SerializeField] private float baseMoveSpeed = 5f;

    [Tooltip("Scaled movement speed after applying the current game speed multiplier.")]
    [SerializeField] private float moveSpeed = 0f;

    [Tooltip("Animator speed multiplier for all monsters at 1x game speed.")]
    [SerializeField] private float baseAnimationSpeed = 1f;

    [Tooltip("Scaled animation speed after applying the current game speed multiplier.")]
    [SerializeField] private float animationSpeed = 0f;

    #region // Placing Monster Variables

    /// <summary>
    /// States for the interactive monster-placement workflow.
    /// </summary>
    public enum PlacingMonsterStates
    {
        /// <summary>No placement is in progress.</summary>
        NotPlacing,
        /// <summary>Placement has been initiated; a temporary monster exists.</summary>
        StartPlacing,
        /// <summary>The temporary monster is being repositioned.</summary>
        PlacingMonster,
        /// <summary>Placement has been confirmed and is being finalized.</summary>
        EndingPlacing,
    }

    [Tooltip("Current state of the monster placement workflow.")]
    [SerializeField] private PlacingMonsterStates placingState;

    [Tooltip("True while a monster is actively being placed.")]
    [SerializeField] private bool placingMonster;

    [Tooltip("Floor index the temporary monster is currently on.")]
    [SerializeField] private int floorPlacePosition;

    [Tooltip("Slot index (1�4) the temporary monster is currently in. 0 means the placing slot.")]
    [SerializeField] private int slotPlacePosition;

    [Tooltip("The temporary preview monster GameObject shown during placement.")]
    [SerializeField] private GameObject temporaryMonster;

    [Tooltip("Initial value of the placement cooldown timer.")]
    [SerializeField] private float placingMonsterCdInit = .5f;

    [Tooltip("Remaining time on the placement cooldown timer.")]
    [SerializeField] private float placingMonsterCd;

    [Tooltip("The monster type currently being placed.")]
    [SerializeField] private MonsterUnit.MonsterTypes placingMonsterType;

    #endregion

    #region // Debugging

    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;

    #endregion

    #endregion

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake() { }

    /// <summary>
    /// Each frame: ticks the placement cooldown and polls input during placement,
    /// then refreshes debug watches.
    /// </summary>
    private void Update()
    {
        UpdatePlacingMonster();
        UpdateDebugWatches();
    }

    // -------------------------------------------------------------------------
    // Debug Watches
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes all Inspector watch entries each frame with the manager's current
    /// runtime state. Called from <see cref="Update"/>.
    /// </summary>
    private void UpdateDebugWatches()
    {
        debug.SetWatch("Active Teams", monsterTeams != null ? monsterTeams.Count.ToString() : "null");
        debug.SetWatch("Teams Spawned", monsterTeamsSpawned.ToString());
        debug.SetWatch("Move Speed", moveSpeed.ToString("F2"));
        debug.SetWatch("Anim Speed", animationSpeed.ToString("F2"));
        debug.SetWatch("Placing State", placingState.ToString());
        debug.SetWatch("Placing Floor", floorPlacePosition.ToString());
        debug.SetWatch("Placing Slot", slotPlacePosition.ToString());
        debug.SetWatch("Placing Type", placingMonsterType.ToString());
        debug.SetWatch("Placing CD", placingMonsterCd.ToString("F2"));

        if (monsterTeams != null)
        {
            int inCombat = monsterTeams.Count(t => t.GetState() == MonsterTeam.MonsterTeamStates.InCombat);
            int bossActive = monsterTeams.Count(t => t.GetIsBossTeam());
            debug.SetWatch("  In Combat", inCombat.ToString());
            debug.SetWatch("  Boss Teams", bossActive.ToString());
        }
    }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets up all manager state: debug utility, speeds, ID prefix, spawn counter,
    /// unlock/cost tables, starting unlocks, and the boss team.
    /// Called by <see cref="TowerDefenseManager"/> during game setup.
    /// </summary>
    public void InitializeMonsterManager()
    {
        InitalizeDebug();
        UpdateMonstersSpeeds();
        SetManagerID("M");
        ResetMonsterTeamsSpawned();
        InitializeMonsterUnlockStatus();
        InitializeMonsterEssenceCosts();
        LockMonsterType(MonsterUnit.MonsterTypes.Slime);
        LockMonsterType(MonsterUnit.MonsterTypes.Bat);
        LockMonsterType(MonsterUnit.MonsterTypes.Spider);
        SpawnBossMonsterTeam();
    }

    /// <summary>Creates a fresh <see cref="DebugUtility"/> for this manager.</summary>
    private void InitalizeDebug() { debug = new DebugUtility(false); }

    // -------------------------------------------------------------------------
    // Lock / Unlock Status
    // -------------------------------------------------------------------------

    /// <summary>
    /// Populates <see cref="monsterUnlockStatus"/> with every
    /// <see cref="MonsterUnit.MonsterTypes"/> value, defaulting all to locked.
    /// </summary>
    private void InitializeMonsterUnlockStatus()
    {
        monsterUnlockStatus = new Dictionary<MonsterUnit.MonsterTypes, bool>();
        foreach (MonsterUnit.MonsterTypes t in Enum.GetValues(typeof(MonsterUnit.MonsterTypes)))
        {
            monsterUnlockStatus[t] = false;
        }
    }

    /// <summary>
    /// Marks the given monster type as available for the player to summon.
    /// </summary>
    /// <param name="monsterType">The type to unlock.</param>
    public void UnlockMonsterType(MonsterUnit.MonsterTypes monsterType)
    {
        if (monsterUnlockStatus.ContainsKey(monsterType))
        {
            monsterUnlockStatus[monsterType] = true;
            if (debug.IsDebugging) Debug.Log($"Monster {monsterType} has been unlocked.");
        }
        else
        {
            Debug.LogWarning($"Monster {monsterType} not found in monsterUnlockStatus dictionary.");
        }
    }

    /// <summary>
    /// Marks the given monster type as unavailable for the player to summon.
    /// </summary>
    /// <param name="monsterType">The type to lock.</param>
    public void LockMonsterType(MonsterUnit.MonsterTypes monsterType)
    {
        if (monsterUnlockStatus.ContainsKey(monsterType))
        {
            monsterUnlockStatus[monsterType] = false;
            if (debug.IsDebugging) Debug.Log($"Monster {monsterType} has been locked.");
        }
        else
        {
            Debug.LogWarning($"Monster {monsterType} not found in monsterUnlockStatus dictionary.");
        }
    }

    /// <summary>
    /// Returns whether a given monster type is currently unlocked for summoning.
    /// </summary>
    /// <param name="monsterType">The type to query.</param>
    /// <returns><c>true</c> if the type is unlocked.</returns>
    public bool IsMonsterTypeUnlocked(MonsterUnit.MonsterTypes monsterType)
    {
        if (monsterUnlockStatus.TryGetValue(monsterType, out bool isUnlocked))
            return isUnlocked;

        Debug.LogWarning($"Monster {monsterType} not found in monsterUnlockStatus dictionary.");
        return false;
    }

    /// <summary>
    /// Returns a list of all currently unlocked <see cref="MonsterUnit.MonsterTypes"/> values.
    /// </summary>
    public List<MonsterUnit.MonsterTypes> GetListOfUnlockedMonsters()
    {
        if (monsterUnlockStatus == null)
        {
            Debug.LogError("monsterUnlockStatus is null!");
            return new List<MonsterUnit.MonsterTypes>();
        }

        return monsterUnlockStatus
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    // -------------------------------------------------------------------------
    // Monster Essence Costs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets the base essence cost for each monster type.
    /// </summary>
    private void InitializeMonsterEssenceCosts()
    {
        monsterEssenceCosts[MonsterUnit.MonsterTypes.Slime] = 10;
        monsterEssenceCosts[MonsterUnit.MonsterTypes.Bat] = 25;
        monsterEssenceCosts[MonsterUnit.MonsterTypes.Spider] = 50;
        monsterEssenceCosts[MonsterUnit.MonsterTypes.Dummii] = 5;
        monsterEssenceCosts[MonsterUnit.MonsterTypes.BossKnight] = 999;
    }

    /// <summary>
    /// Overrides the essence cost for a specific monster type at runtime.
    /// </summary>
    /// <param name="monsterType">The type whose cost to update.</param>
    /// <param name="cost">The new essence cost.</param>
    public void SetMonsterEssenceCost(MonsterUnit.MonsterTypes monsterType, int cost)
    {
        if (monsterEssenceCosts.ContainsKey(monsterType))
            monsterEssenceCosts[monsterType] = cost;
        else
            Debug.LogWarning($"Monster type {monsterType} not found in dictionary.");
    }

    /// <summary>
    /// Returns the essence cost for a given monster type, or <c>-1</c> if not found.
    /// </summary>
    /// <param name="monsterType">The type to query.</param>
    public int GetMonsterEssenceCost(MonsterUnit.MonsterTypes monsterType)
    {
        if (monsterEssenceCosts.TryGetValue(monsterType, out int cost))
            return cost;

        Debug.LogWarning($"Monster type {monsterType} not found in dictionary.");
        return -1;
    }

    // -------------------------------------------------------------------------
    // Phase / Spawn
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the list of <see cref="NewMonster"/> descriptors for a specific
    /// level and phase, or <c>null</c> if the phase is empty or out of range.
    /// </summary>
    /// <param name="level">The level index to query.</param>
    /// <param name="phase">The 1-based phase index to query.</param>
    public List<NewMonster> GetPhaseMonsters(int level, int phase)
    {
        if (levelPhases.ContainsKey(level) && levelPhases[level].Count > phase - 1)
            return levelPhases[level][phase - 1];
        return null;
    }

    /// <summary>
    /// Creates a single-monster boss team containing a <see cref="MonsterUnit.MonsterTypes.BossKnight"/>
    /// at level 1, spawned on the top floor of the tower.
    /// </summary>
    public void SpawnBossMonsterTeam()
    {
        List<NewMonster> bossTeam = new List<NewMonster>
        {
            new NewMonster { level = 1, monsterClass = MonsterTypes.BossKnight }
        };
        int topFloor = GetTowerManager().GetFloors().Count - 1;
        SpawnMonsterTeam(bossTeam, topFloor, true);
    }

    /// <summary>
    /// Randomly populates up to four slots on the given floor with a 60% chance
    /// per slot. Guarantees at least one monster spawns via a fallback.
    /// </summary>
    /// <param name="floorToSpawnOn">The floor index to populate.</param>
    public void SpawnRandomMonsterTeam(int floorToSpawnOn)
    {
        int spawnChancePerSlot = 60;
        List<NewMonster> monstersToSpawn = new List<NewMonster>();
        bool hasSpawnedMonster = false;

        List<MonsterTypes> pool = new List<MonsterTypes>
        {
            MonsterTypes.Slime, MonsterTypes.Bat, MonsterTypes.Spider
        };

        for (int i = 0; i < 4; i++)
        {
            int chance = UnityEngine.Random.Range(1, 101);
            if (chance <= spawnChancePerSlot)
            {
                MonsterTypes randomClass = pool[UnityEngine.Random.Range(0, pool.Count)];
                NewMonster newMonster = new NewMonster { level = 1, monsterClass = randomClass };
                monstersToSpawn.Add(newMonster);
                hasSpawnedMonster = true;

                if (debug.IsDebugging)
                    Debug.Log($"Spawned monster in slot {i + 1}: {newMonster.monsterClass} (Level: {newMonster.level})");
            }
            else if (debug.IsDebugging)
            {
                Debug.Log($"No monster spawned in slot {i + 1} (Chance: {chance} > {spawnChancePerSlot})");
            }
        }

        if (!hasSpawnedMonster)
        {
            MonsterTypes randomClass = pool[UnityEngine.Random.Range(0, pool.Count)];
            NewMonster fallbackMonster = new NewMonster { level = 1, monsterClass = randomClass };
            monstersToSpawn.Add(fallbackMonster);

            if (debug.IsDebugging)
                Debug.Log($"No monsters were spawned initially. Added fallback monster: {fallbackMonster.monsterClass} (Level: {fallbackMonster.level})");
        }

        if (debug.IsDebugging)
        {
            Debug.Log($"Final monster team for floor {floorToSpawnOn}:");
            foreach (var monster in monstersToSpawn)
                Debug.Log($"- {monster.monsterClass} (Level: {monster.level})");
        }

        SpawnMonsterTeam(monstersToSpawn, floorToSpawnOn);
    }

    /// <summary>
    /// Creates a new <see cref="MonsterTeam"/> GameObject, initializes it with
    /// the provided monster descriptors, and registers it with this manager.
    /// </summary>
    /// <param name="newMonsters">The monsters to include in the new team.</param>
    /// <param name="floorToSpawnOn">The floor index the team should occupy.</param>
    /// <param name="isBoss">Whether this team is the boss team.</param>
    public void SpawnMonsterTeam(List<NewMonster> newMonsters, int floorToSpawnOn, bool isBoss = false)
    {
        if (newMonsters == null || newMonsters.Count == 0)
        {
            Debug.LogError("Cannot spawn a monster team without monsters.");
            return;
        }

        string monsterTeamID = GetNextMonsterTeamID();

        if (debug.IsDebugging)
            Debug.Log($"Spawning monster team {monsterTeamID} on floor {floorToSpawnOn} with {newMonsters.Count} monsters.");

        GameObject newTeamObject = new GameObject("Monster Team " + monsterTeamID);
        newTeamObject.transform.SetParent(this.transform);

        MonsterTeam newMonsterTeam = newTeamObject.AddComponent<MonsterTeam>();
        newMonsterTeam.InitializeMonsterTeam(GetGameManager(), newMonsters, monsterTeamID, floorToSpawnOn, isBoss);

        GetMonsterTeams().Add(newMonsterTeam);

        if (debug.IsDebugging)
            Debug.Log($"Monster team {monsterTeamID} successfully spawned and linked to MonsterManager.");
    }

    /// <summary>
    /// Creates a <see cref="MonsterTeam"/> with no monsters. Useful for
    /// pre-populating a floor slot before monsters are added manually.
    /// </summary>
    /// <param name="floorToSpawnOn">The floor index the empty team should occupy.</param>
    public void SpawnEmptyMonsterTeam(int floorToSpawnOn)
    {
        if (debug.IsDebugging)
            Debug.Log($"Spawning empty monster team on floor {floorToSpawnOn}.");

        string monsterTeamID = GetNextMonsterTeamID();

        GameObject newTeamObject = new GameObject($"Monster Team {monsterTeamID}");
        newTeamObject.transform.SetParent(this.transform);

        MonsterTeam newMonsterTeam = newTeamObject.AddComponent<MonsterTeam>();
        newMonsterTeam.InitializeEmptyMonsterTeam(GetGameManager(), monsterTeamID, floorToSpawnOn);

        GetMonsterTeams()?.Add(newMonsterTeam);

        if (debug.IsDebugging)
            Debug.Log($"Monster team {monsterTeamID} successfully spawned and linked to MonsterManager.");
    }

    /// <summary>
    /// Returns the model prefab associated with a given <see cref="MonsterUnit.MonsterTypes"/>.
    /// </summary>
    /// <param name="monsterClass">The type whose model prefab is needed.</param>
    /// <returns>The model prefab, or <c>null</c> if the type is unrecognized.</returns>
    public GameObject GetMonsterModelPrefab(MonsterUnit.MonsterTypes monsterClass)
    {
        return monsterClass switch
        {
            MonsterUnit.MonsterTypes.Slime => SlimeModel,
            MonsterUnit.MonsterTypes.Bat => BatModel,
            MonsterUnit.MonsterTypes.Spider => SpiderModel,
            MonsterUnit.MonsterTypes.Dummii => DummiiModel,
            MonsterUnit.MonsterTypes.BossKnight => BossKnightModel,
            _ => null,
        };
    }

    /// <summary>
    /// Resets the active monster team list to a new empty instance.
    /// Called at the start of each level.
    /// </summary>
    public void LevelStart(int currentLevel)
    {
        InitalizeList();
    }

    // -------------------------------------------------------------------------
    // Round Phase Coroutines
    // -------------------------------------------------------------------------

    /// <summary>
    /// Round-start phase: updates hero team links for all teams, then makes every
    /// in-combat non-boss team gain energy.
    /// </summary>
    public IEnumerator RoundStartCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("RoundStart initiated. Monster Manager Processing...");

        foreach (MonsterTeam monsterTeam in GetMonsterTeams())
        {
            if (debug.IsDebugging)
                Debug.Log($"Processing MonsterTeam: {monsterTeam.GetMtID()}, Floor: {monsterTeam.GetCurrentFloor()}, State: {monsterTeam.GetState()}");

            monsterTeam.UpdateHeroTeamOnFloor();

            if (!monsterTeam.GetIsBossTeam())
            {
                if (monsterTeam.MonsterTeamState == MonsterTeam.MonsterTeamStates.InCombat)
                {
                    if (debug.IsDebugging) Debug.Log("Team is in Combat");
                    yield return StartCoroutine(monsterTeam.TeamGainEnergy());
                }
                else if (debug.IsDebugging)
                {
                    Debug.Log($"Unknown State type. Current state: {monsterTeam.GetState()}");
                }
            }
        }

        if (debug.IsDebugging) Debug.Log("RoundStart processing complete.");
    }

    /// <summary>
    /// Round-action phase: every in-combat non-boss team executes its monsters'
    /// actions sequentially.
    /// </summary>
    public IEnumerator RoundActionCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Monster Manager RoundAction initiated...");

        foreach (var monsterTeam in GetMonsterTeams())
        {
            monsterTeam.UpdateHeroTeamOnFloor();

            if (!monsterTeam.GetIsBossTeam())
            {
                if (debug.IsDebugging)
                    Debug.Log($"Processing MonsterTeam: Floor {monsterTeam.GetCurrentFloor()}, State: {monsterTeam.GetState()}");

                if (monsterTeam.GetState() == MonsterTeam.MonsterTeamStates.InCombat)
                    yield return StartCoroutine(monsterTeam.TeamExecuteAction());
                else if (debug.IsDebugging)
                    Debug.Log($"Unknown State type. Current state: {monsterTeam.GetState()}");
            }
        }

        if (debug.IsDebugging) Debug.Log("RoundAction processing complete.");
        if (debug.IsDebugging) Debug.Log("________________________________________________________");
    }

    /// <summary>
    /// Round-end phase: updates hero team links for all non-boss teams.
    /// Individual team end-of-round processing is currently a no-op.
    /// </summary>
    public IEnumerator RoundEndCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Monster Manager RoundEnd initiated...");

        foreach (MonsterTeam monsterTeam in GetMonsterTeams())
        {
            monsterTeam.UpdateHeroTeamOnFloor();
            // Reserved for future per-team round-end logic.
        }

        yield return 0;

        if (debug.IsDebugging) Debug.Log("RoundEnd processing complete.");
    }

    /// <summary>Before-round transition phase. Currently a no-op; reserved for future use.</summary>
    public IEnumerator BeforeRoundCoroutine() { yield return null; }
    /// <summary>Start-to-action transition phase. Currently a no-op; reserved for future use.</summary>
    public IEnumerator StartToActionCoroutine() { yield return null; }
    /// <summary>Action-to-end transition phase. Currently a no-op; reserved for future use.</summary>
    public IEnumerator ActionToEndCoroutine() { yield return null; }
    /// <summary>After-round transition phase. Currently a no-op; reserved for future use.</summary>
    public IEnumerator AfterRoundCoroutine() { yield return null; }

    /// <summary>
    /// Boss round-start phase: for each boss team in the
    /// <see cref="MonsterTeam.MonsterTeamStates.BossRelaxing"/> state, checks
    /// whether a hero team has arrived and triggers the combat entry sequence.
    /// Runs after the standard monster and hero round-start coroutines.
    /// </summary>
    public IEnumerator BossRoundStartCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Boss RoundStart initiated. Monster Manager Processing...");

        foreach (MonsterTeam monsterTeam in GetMonsterTeams())
        {
            monsterTeam.UpdateHeroTeamOnFloor();

            if (monsterTeam.GetIsBossTeam())
            {
                if (debug.IsDebugging)
                    Debug.Log($"Processing Boss Team on floor: {monsterTeam.GetCurrentFloor()}.");

                if (monsterTeam.MonsterTeamState == MonsterTeam.MonsterTeamStates.BossRelaxing)
                {
                    if (debug.IsDebugging) Debug.Log("Boss is relaxing. Checking to enter combat.");
                    yield return StartCoroutine(monsterTeam.BossTeamCheckToEnterCombat());
                }
            }
        }

        if (debug.IsDebugging) Debug.Log("Boss RoundStart processing complete.");
    }

    /// <summary>
    /// Boss round-action phase: for each boss team in
    /// <see cref="MonsterTeam.MonsterTeamStates.BossInCombat"/>, executes the boss's
    /// combat actions. Runs after the standard monster and hero round-action coroutines.
    /// </summary>
    public IEnumerator BossRoundActionCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Boss RoundAction initiated. Monster Manager Processing...");

        foreach (MonsterTeam monsterTeam in GetMonsterTeams())
        {
            monsterTeam.UpdateHeroTeamOnFloor();

            if (monsterTeam.GetIsBossTeam() && monsterTeam.MonsterTeamState == MonsterTeam.MonsterTeamStates.BossInCombat)
                yield return StartCoroutine(monsterTeam.TeamExecuteAction());
        }

        if (debug.IsDebugging) Debug.Log("Boss RoundAction processing complete.");
    }

    /// <summary>
    /// Boss round-end phase: for each boss team in
    /// <see cref="MonsterTeam.MonsterTeamStates.BossInCombat"/>, checks whether
    /// the hero team has been defeated and triggers the throne-return sequence if so.
    /// Runs after the standard monster and hero round-end coroutines.
    /// </summary>
    public IEnumerator BossRoundEndCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Boss RoundEnd initiated. Monster Manager Processing...");

        foreach (MonsterTeam monsterTeam in GetMonsterTeams())
        {
            monsterTeam.UpdateHeroTeamOnFloor();

            if (monsterTeam.GetIsBossTeam() && monsterTeam.MonsterTeamState == MonsterTeam.MonsterTeamStates.BossInCombat)
                yield return StartCoroutine(monsterTeam.BossTeamCheckToLeaveCombat());
        }

        if (debug.IsDebugging) Debug.Log("Boss RoundEnd processing complete.");
    }

    // -------------------------------------------------------------------------
    // Monster Placement Workflow
    // -------------------------------------------------------------------------

    /// <summary>
    /// Begins the interactive monster-placement workflow on the given floor:
    /// zooms the camera, holds phase progression, inserts a temporary preview
    /// monster, and resets the placement cooldown.
    /// </summary>
    /// <param name="whichFloor">The floor index to place on.</param>
    /// <param name="whichType">The monster type to preview.</param>
    public void StartPlacingMonster(int whichFloor, MonsterUnit.MonsterTypes whichType = MonsterUnit.MonsterTypes.Bat)
    {
        SetPlacingState(PlacingMonsterStates.StartPlacing);
        SetPlacingFloor(whichFloor);

        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, whichFloor);
        GetGameManager().SetHoldPhase(true);

        ResetPlacingMonsterCd();
        SetPlacingMonsterType(whichType);
        InsertTemporaryMonster();
    }

    /// <summary>
    /// Places a monster directly into a specific floor slot, bypassing the
    /// arrow-navigation placement UI. Used when confirming an empty slot in MonsterFocus.
    /// </summary>
    public void PlaceMonsterDirectlyInSlot(int floor, int slot, MonsterUnit.MonsterTypes type)
    {
        int cost = GetMonsterEssenceCost(type);
        int heldEssence = GetGameManager().GetInventoryManager().GetEssence();
        if (cost > heldEssence)
        {
            Debug.LogWarning($"Cannot place monster: insufficient essence ({heldEssence} < {cost}).");
            return;
        }

        StartPlacingMonster(floor, type);
        MoveTemporaryMonster(floor, slot);
        ConfirmPlacingMonster();
    }

    /// <summary>
    /// Cancels an in-progress placement: removes the temporary monster, resets
    /// all placement state, and releases the phase hold.
    /// </summary>
    public void CancelPlacingMonster()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return;

        SetPlacingState(PlacingMonsterStates.NotPlacing);
        SetPlacingFloor(0);
        SetPlacingSlot(0);
        GetGameManager().SetHoldPhase(false);
        monsterSlots.SetAllSlotAddonsToInactive();
        RemoveTemporaryMonster();
        ResetPlacingMonsterCd();
    }

    /// <summary>
    /// Confirms the current placement: removes the preview monster, inserts the
    /// real monster into the chosen slot, deducts the essence cost, and releases
    /// the phase hold.
    /// </summary>
    public void ConfirmPlacingMonster()
    {
        if (GetPlacingState() != PlacingMonsterStates.StartPlacing) return;

        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return;

        SetPlacingState(PlacingMonsterStates.EndingPlacing);
        RemoveTemporaryMonster();
        monsterSlots.SetAllSlotAddonsToInactive();

        MonsterTeam mTeam = monsterSlots.GetMonsterTeam();
        NewMonster newMonster = new NewMonster { level = 1, monsterClass = GetPlacingMonsterType() };
        mTeam.AddMonsterToTeam(newMonster, GetPlacingSlot());

        GetGameManager().SetHoldPhase(false);
        ResetPlacingMonsterCd();

        int cost = GetMonsterEssenceCost(GetPlacingMonsterType());
        int heldEssence = GetGameManager().GetInventoryManager().GetEssence();
        GetGameManager().GetInventoryManager().SetEssence(heldEssence - cost);

        SetPlacingState(PlacingMonsterStates.NotPlacing);
    }

    /// <summary>
    /// Instantiates the temporary preview monster on the placing floor and positions
    /// it in the first available slot, applying a see-through material.
    /// </summary>
    private void InsertTemporaryMonster()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return;

        GameObject towerFloor = GetTowerManager().GetFloor(GetPlacingFloor());

        SetTemporaryMonster(Instantiate(GetMonsterModelPrefab(GetPlacingMonsterType()), towerFloor.transform.position, towerFloor.transform.rotation));
        MonsterUnitPrefabAddon tempAddon = GetTemporaryMonster().GetComponent<MonsterUnitPrefabAddon>();
        tempAddon.InitializePrefabAddon();
        tempAddon.StartTemporarySeeThrough();

        SetPlacingSlot(monsterSlots.AreSlotsFull() ? monsterSlots.GetFirstOpenSlot() : 0);
        SetPlacingSlot(0);
        MoveTemporaryMonster(GetPlacingFloor(), GetPlacingSlot());

        if (debug.IsDebugging)
            Debug.Log($"Inserted temporary monster on floor {GetPlacingFloor()} at position {GetPlacingSlot()}.");
    }

    /// <summary>
    /// Moves the temporary preview monster to the specified floor and slot,
    /// updates slot visual states, and refreshes the camera and menu buttons.
    /// </summary>
    /// <param name="whichFloor">Target floor index.</param>
    /// <param name="whichSlot">Target slot index (0 = placing slot, 1�4 = combat slots).</param>
    public void MoveTemporaryMonster(int whichFloor, int whichSlot)
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return;

        SetPlacingFloor(whichFloor);
        SetPlacingSlot(whichSlot);
        UpdateTemporarySlotAddonsState();

        GameObject tempMonster = GetTemporaryMonster();
        GameObject slotObject = GetPlacingSlot() <= 0
            ? monsterSlots.GetPlacingSlot().gameObject
            : monsterSlots.GetSlots()[GetPlacingSlot() - 1].gameObject;

        tempMonster.transform.position = slotObject.transform.position;
        tempMonster.transform.rotation = slotObject.transform.rotation;

        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, GetPlacingFloor());
        GetGameManager().GetCastleMenuManager().UpdateSummonMenuButtons();

        if (debug.IsDebugging)
            Debug.Log($"Moved temporary monster on floor {GetPlacingFloor()} at position {GetPlacingSlot()}.");
    }

    /// <summary>
    /// Refreshes the visual state (Valid / Free / Inactive) of every slot on the
    /// placing floor based on the current temporary monster position and occupancy.
    /// </summary>
    private void UpdateTemporarySlotAddonsState()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return;

        monsterSlots.SetPlacingSlotAddonState(GetPlacingSlot() <= 0
            ? MonsterSlotAddon.MonsterSlotStates.Invalid
            : MonsterSlotAddon.MonsterSlotStates.Inactive);

        for (int i = 0; i < monsterSlots.GetSlots().Count; i++)
        {
            int placingIndex = GetPlacingSlot() - 1;

            if (placingIndex == i)
            {
                monsterSlots.SetSlotAddonState(GetPlacingSlot() - 1, MonsterSlotAddon.MonsterSlotStates.Valid);
            }
            else
            {
                var state = monsterSlots.GetMonsterInSlot(i + 1) != null
                    ? MonsterSlotAddon.MonsterSlotStates.Inactive
                    : MonsterSlotAddon.MonsterSlotStates.Free;
                monsterSlots.SetSlotAddonState(i, state);
            }
        }

        if (debug.IsDebugging)
            Debug.Log($"Updated slot addon states on floor {GetPlacingFloor()} at position {GetPlacingSlot()}.");
    }

    /// <summary>
    /// Sets all slot visual states on the placing floor to Inactive,
    /// used when clearing the preview before a floor change.
    /// </summary>
    private void DeactivateTemporarySlotAddonsFxState()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return;

        monsterSlots.SetPlacingSlotAddonState(MonsterSlotAddon.MonsterSlotStates.Inactive);
        for (int i = 0; i < monsterSlots.GetSlots().Count; i++)
            monsterSlots.SetSlotAddonState(i, MonsterSlotAddon.MonsterSlotStates.Inactive);
    }

    /// <summary>Destroys the temporary preview monster GameObject.</summary>
    private void RemoveTemporaryMonster()
    {
        Destroy(GetTemporaryMonster());
    }

    /// <summary>
    /// Called every frame to tick the placement cooldown and poll action/cancel
    /// input while a monster is being placed.
    /// </summary>
    private void UpdatePlacingMonster()
    {
        float cd = GetPlacingMonsterCd();
        SetPlacingMonsterCd(cd > 0 ? cd - Time.deltaTime : 0f);

        if (GetPlacingState() == PlacingMonsterStates.NotPlacing) return;

        if (GetPlacingMonsterCd() <= 0)
        {
            if (GetGameManager().GetInputManager().GetAction()) { /* ConfirmPlacingMonster(); */ }
            if (GetGameManager().GetInputManager().GetCancel()) { /* CancelPlacingMonster();  */ }
        }
    }

    /// <summary>
    /// Returns <c>true</c> if at least one middle floor has an open monster slot
    /// and at least one monster type is unlocked for summoning.
    /// </summary>
    public bool IsThereSpaceToSummonMonster()
    {
        TowerManager tm = GetGameManager().GetTowerManager();

        int howManyFloors = tm.GetFloors().Count;
        if (howManyFloors <= 2) return false;

        bool hasSummonableMonster = monsterUnlockStatus.Any(kvp => kvp.Value);
        if (!hasSummonableMonster) return false;

        for (int i = 1; i < howManyFloors - 1; i++)
        {
            MonsterSlotsAddon monsterSlots = tm.GetTowerSectionAddon(tm.GetFloor(i)).GetMonsterSlots();
            if (!monsterSlots.AreSlotsFull()) return true;
        }

        return false;
    }

    /// <summary>
    /// Moves the temporary monster up one floor and reinserts the preview.
    /// </summary>
    public void MoveTempMonsterUp()
    {
        DeactivateTemporarySlotAddonsFxState();
        SetPlacingFloor(GetPlacingFloor() + 1);
        RemoveTemporaryMonster();
        InsertTemporaryMonster();
        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, GetPlacingFloor());
    }

    /// <summary>
    /// Moves the temporary monster down one floor and reinserts the preview.
    /// </summary>
    public void MoveTempMonsterDown()
    {
        DeactivateTemporarySlotAddonsFxState();
        SetPlacingFloor(GetPlacingFloor() - 1);
        RemoveTemporaryMonster();
        InsertTemporaryMonster();
        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, GetPlacingFloor());
    }

    /// <summary>
    /// Moves the temporary monster to the nearest available slot to the left.
    /// From slot 0, wraps to the rightmost open slot.
    /// </summary>
    public void MoveTempMonsterLeft()
    {
        int floorPosition = GetMonsterManager().GetPlacingFloor();
        int currentSlot = GetMonsterManager().GetPlacingSlot();

        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetSlotsForFloor(floorPosition, out towerAddon, out monsterSlots)) return;

        if (currentSlot == 0)
        {
            for (int i = 4; i >= 1; i--)
            {
                if (!monsterSlots.GetSlot(i).GetIsDisabled() && monsterSlots.GetMonsterInSlot(i) == null)
                {
                    GetMonsterManager().MoveTemporaryMonster(floorPosition, i);
                    return;
                }
            }
            Debug.LogWarning("No free slots to move to from position 0.");
            return;
        }

        for (int i = currentSlot - 1; i >= 1; i--)
        {
            if (!monsterSlots.GetSlot(i).GetIsDisabled() && monsterSlots.GetMonsterInSlot(i) == null)
            {
                GetMonsterManager().MoveTemporaryMonster(floorPosition, i);
                return;
            }
        }

        Debug.LogWarning($"No free slots to the left of slot {currentSlot} on floor {floorPosition}.");
    }

    /// <summary>
    /// Moves the temporary monster to the nearest available slot to the right.
    /// From slot 0, wraps to the leftmost open slot.
    /// </summary>
    public void MoveTempMonsterRight()
    {
        int floorPosition = GetMonsterManager().GetPlacingFloor();
        int currentSlot = GetMonsterManager().GetPlacingSlot();

        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetSlotsForFloor(floorPosition, out towerAddon, out monsterSlots)) return;

        if (currentSlot == 0)
        {
            for (int i = 1; i <= 4; i++)
            {
                if (!monsterSlots.GetSlot(i).GetIsDisabled() && monsterSlots.GetMonsterInSlot(i) == null)
                {
                    GetMonsterManager().MoveTemporaryMonster(floorPosition, i);
                    return;
                }
            }
            Debug.LogWarning("No free slots to move to from position 0.");
            return;
        }

        for (int i = currentSlot + 1; i <= 4; i++)
        {
            if (!monsterSlots.GetSlot(i).GetIsDisabled() && monsterSlots.GetMonsterInSlot(i) == null)
            {
                GetMonsterManager().MoveTemporaryMonster(floorPosition, i);
                return;
            }
        }

        Debug.LogWarning($"No free slots to the right of slot {currentSlot} on floor {floorPosition}.");
    }

    /// <returns><c>true</c> if the temporary monster can move left to a free slot.</returns>
    public bool CanTempMonsterMoveLeft()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return false;

        int currentSlot = GetPlacingSlot();
        int start = currentSlot == 0 ? 4 : currentSlot - 1;
        int end = 1;
        int step = -1;

        for (int i = start; currentSlot == 0 ? i >= end : i >= end; i += step)
        {
            if (!monsterSlots.GetSlot(i).GetIsDisabled() && monsterSlots.GetMonsterInSlot(i) == null)
                return true;
        }
        return false;
    }

    /// <returns><c>true</c> if the temporary monster can move right to a free slot.</returns>
    public bool CanTempMonsterMoveRight()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return false;

        int currentSlot = GetPlacingSlot();
        int start = currentSlot == 0 ? 1 : currentSlot + 1;

        for (int i = start; i <= 4; i++)
        {
            if (!monsterSlots.GetSlot(i).GetIsDisabled() && monsterSlots.GetMonsterInSlot(i) == null)
                return true;
        }
        return false;
    }

    /// <returns><c>true</c> if the temporary monster can move up to a non-top floor.</returns>
    public bool CanTempMonsterMoveUp()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return false;
        return GetPlacingFloor() < GetTowerManager().GetFloors().Count - 2;
    }

    /// <returns><c>true</c> if the temporary monster can move down to a floor above the base.</returns>
    public bool CanTempMonsterMoveDown()
    {
        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return false;
        return GetPlacingFloor() > 1;
    }

    /// <returns>
    /// <c>true</c> if the current placing slot index is valid (1�4) and the slot
    /// has no monster already occupying it.
    /// </returns>
    public bool GetCheckIfValidPlacingSlot()
    {
        if (GetPlacingSlot() <= 0) return false;

        TowerSectionAddon towerAddon;
        MonsterSlotsAddon monsterSlots;
        if (!TryGetMonsterSlotsForPlacingFloor(out towerAddon, out monsterSlots)) return false;

        MonsterSlotAddon slotToCheck = monsterSlots.GetSlot(GetPlacingSlot());
        if (slotToCheck == null) return false;
        return slotToCheck.GetMonster() == null;
    }

    // -------------------------------------------------------------------------
    // Internal Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Shared boilerplate: resolves the tower section and monster slots for the
    /// current placing floor, logging errors and returning <c>false</c> on any
    /// null reference in the chain.
    /// </summary>
    private bool TryGetMonsterSlotsForPlacingFloor(out TowerSectionAddon towerAddon, out MonsterSlotsAddon monsterSlots)
        => TryGetSlotsForFloor(GetPlacingFloor(), out towerAddon, out monsterSlots);

    /// <summary>
    /// Shared boilerplate: resolves the tower section and monster slots for the
    /// given floor index, logging errors and returning <c>false</c> on any null.
    /// </summary>
    private bool TryGetSlotsForFloor(int floor, out TowerSectionAddon towerAddon, out MonsterSlotsAddon monsterSlots)
    {
        towerAddon = null;
        monsterSlots = null;

        GameObject towerFloor = GetTowerManager().GetFloor(floor);
        if (towerFloor == null) { Debug.LogError("Tower Floor is null."); return false; }
        towerAddon = GetTowerManager().GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon is null."); return false; }
        monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null."); return false; }
        return true;
    }

    // -------------------------------------------------------------------------
    // Speed Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Recalculates and applies both move and animation speed multipliers based
    /// on the current game speed. Should be called whenever game speed changes.
    /// </summary>
    public void UpdateMonstersSpeeds()
    {
        UpdateMonstersMoveSpeed();
        UpdateMonstersAnimationSpeed();
    }

    /// <summary>
    /// Scales <see cref="baseMoveSpeed"/> by the current game speed and stores
    /// the result in <see cref="moveSpeed"/>.
    /// </summary>
    private void UpdateMonstersMoveSpeed()
    {
        float gameSpeed = (int)GetGameManager().GetGameSpeed();
        SetMoveSpeed(GetBaseMoveSpeed() * (gameSpeed / 2f));
    }

    /// <summary>
    /// Scales <see cref="baseAnimationSpeed"/> by the current game speed, stores
    /// the result in <see cref="animationSpeed"/>, and propagates the new value
    /// to all active monster teams.
    /// </summary>
    private void UpdateMonstersAnimationSpeed()
    {
        float gameSpeed = (int)GetGameManager().GetGameSpeed();
        SetAnimationSpeed(GetBaseAnimationSpeed() * (gameSpeed / 2f));
        UpdateMonsterTeamsAnimationSpeeds();
    }

    /// <summary>
    /// Calls <see cref="MonsterTeam.UpdateMonstersAnimationSpeed"/> on every
    /// active monster team so individual monster Animators reflect the updated speed.
    /// </summary>
    private void UpdateMonsterTeamsAnimationSpeeds()
    {
        List<MonsterTeam> teams = GetMonsterTeams();
        for (int i = 0; i < teams.Count; i++)
        {
            if (teams[i] != null) teams[i].UpdateMonstersAnimationSpeed();
        }
    }

    // -------------------------------------------------------------------------
    // Victory
    // -------------------------------------------------------------------------

    /// <summary>
    /// Triggers the victory pose animation on all non-boss monster teams.
    /// </summary>
    public void AllMonstersVictoryPose()
    {
        if (debug.IsDebugging) Debug.Log("AllMonstersVictoryPose. Monster Manager Processing...");

        foreach (MonsterTeam monsterTeam in GetMonsterTeams())
        {
            if (!monsterTeam.GetIsBossTeam())
                monsterTeam.TeamVictoryPose();
        }
    }

    /// <summary>
    /// Triggers the victory pose animation on all boss monster teams.
    /// </summary>
    public void BossMonsterVictoryPose()
    {
        if (debug.IsDebugging) Debug.Log("BossMonsterVictoryPose. Monster Manager Processing...");

        foreach (MonsterTeam monsterTeam in GetMonsterTeams())
        {
            if (monsterTeam.GetIsBossTeam())
                monsterTeam.TeamVictoryPose();
        }
    }

    // -------------------------------------------------------------------------
    // Team Management
    // -------------------------------------------------------------------------

    /// <returns>The runtime phase schedule dictionary, keyed by level index.</returns>
    public Dictionary<int, List<List<NewMonster>>> GetLevelPhases() => levelPhases;

    /// <summary>
    /// Removes all monster teams whose ID matches <paramref name="whichMtID"/>
    /// from the active list.
    /// </summary>
    /// <param name="whichMtID">The team ID string to remove.</param>
    public void RemoveMonsterTeam(string whichMtID)
    {
        var toRemove = GetMonsterTeams().Where(mt => mt.GetMtID() == whichMtID).ToList();
        foreach (var monster in toRemove) { GetMonsterTeams().Remove(monster); }
    }

    /// <summary>Resets the monster team list to a new empty instance.</summary>
    private void InitalizeList() { monsterTeams = new List<MonsterTeam>(); }

    /// <summary>
    /// Converts a string to a <see cref="MonsterUnit.MonsterTypes"/> enum value.
    /// Returns <see cref="MonsterUnit.MonsterTypes.Slime"/> as a fallback if parsing fails.
    /// </summary>
    /// <param name="monsterTypeString">The string to parse (case-insensitive).</param>
    public MonsterUnit.MonsterTypes ConvertStringToMonsterType(string monsterTypeString)
    {
        if (Enum.TryParse(monsterTypeString, true, out MonsterUnit.MonsterTypes result))
            return result;

        Debug.LogError($"Invalid monster type string: {monsterTypeString}. Returning default MonsterType.");
        return MonsterUnit.MonsterTypes.Slime;
    }

    // -------------------------------------------------------------------------
    // Placing Monster Getters & Setters
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns whether all monster slots on the given floor are occupied.
    /// </summary>
    /// <param name="whichFloor">The floor index to check.</param>
    public bool GetSpecificFloorAtMaxMonsterCapacity(int whichFloor)
    {
        MonsterSlotsAddon monsterSlots = GetTowerManager()
            .GetTowerSectionAddon(GetTowerManager().GetFloor(whichFloor))
            .GetMonsterSlots();
        Debug.Log($"Are the monster slots of this floor full: {monsterSlots.AreSlotsFull()}");
        return monsterSlots.AreSlotsFull();
    }

    /// <param name="newState">New placement state to assign.</param>
    public void SetPlacingState(PlacingMonsterStates newState) => placingState = newState;
    /// <returns>The current monster placement state.</returns>
    public PlacingMonsterStates GetPlacingState() => placingState;

    /// <param name="newPlacingMonster">New placing-monster flag to assign.</param>
    public void SetPlacingMonster(bool newPlacingMonster) => placingMonster = newPlacingMonster;
    /// <returns><c>true</c> if a monster is actively being placed.</returns>
    public bool GetPlacingMonster() => placingMonster;

    /// <param name="newPlacePosition">New floor index, clamped to valid tower range.</param>
    public void SetPlacingFloor(int newPlacePosition) => floorPlacePosition = Mathf.Clamp(newPlacePosition, 1, GetTowerManager().GetFloors().Count);
    /// <returns>The floor index the temporary monster is currently on.</returns>
    public int GetPlacingFloor() => floorPlacePosition;

    /// <param name="newPlacePosition">New slot index, clamped 0�4.</param>
    public void SetPlacingSlot(int newPlacePosition) => slotPlacePosition = Mathf.Clamp(newPlacePosition, 0, 4);
    /// <returns>The slot index the temporary monster is currently in (0 = placing slot).</returns>
    public int GetPlacingSlot() => slotPlacePosition;

    /// <returns>The temporary preview monster GameObject.</returns>
    public GameObject GetTemporaryMonster() => temporaryMonster;
    /// <param name="newTemporaryMonster">New temporary monster to assign.</param>
    public void SetTemporaryMonster(GameObject newTemporaryMonster) => temporaryMonster = newTemporaryMonster;

    private void ResetPlacingMonsterCd() => placingMonsterCd = placingMonsterCdInit;
    private void SetPlacingMonsterCd(float newCd) => placingMonsterCd = newCd;
    /// <returns>Remaining time on the placement input cooldown.</returns>
    public float GetPlacingMonsterCd() => placingMonsterCd;

    /// <param name="newPlacingMonsterType">New monster type to preview during placement.</param>
    public void SetPlacingMonsterType(MonsterUnit.MonsterTypes newPlacingMonsterType) => placingMonsterType = newPlacingMonsterType;
    /// <returns>The monster type currently being placed.</returns>
    public MonsterUnit.MonsterTypes GetPlacingMonsterType() => placingMonsterType;

    // -------------------------------------------------------------------------
    // Speed Getters & Setters
    // -------------------------------------------------------------------------

    /// <returns>The scaled monster movement speed for the current game speed.</returns>
    public float GetMoveSpeed() => moveSpeed;
    /// <param name="newMoveSpeed">New scaled move speed to assign.</param>
    public void SetMoveSpeed(float newMoveSpeed) => moveSpeed = newMoveSpeed;

    /// <returns>The base movement speed at 1x game speed.</returns>
    public float GetBaseMoveSpeed() => baseMoveSpeed;
    /// <param name="newBaseMoveSpeed">New base move speed to assign.</param>
    public void SetBaseMoveSpeed(float newBaseMoveSpeed) => baseMoveSpeed = newBaseMoveSpeed;

    /// <returns>The base animation speed multiplier at 1x game speed.</returns>
    public float GetBaseAnimationSpeed() => baseAnimationSpeed;
    /// <param name="newBaseAnimationSpeed">New base animation speed to assign.</param>
    public void SetBaseAnimationSpeed(float newBaseAnimationSpeed) => baseAnimationSpeed = newBaseAnimationSpeed;

    /// <returns>The scaled animation speed for the current game speed.</returns>
    public float GetAnimationSpeed() => animationSpeed;
    /// <param name="newAnimationSpeed">New scaled animation speed to assign.</param>
    public void SetAnimationSpeed(float newAnimationSpeed) => animationSpeed = newAnimationSpeed;

    // -------------------------------------------------------------------------
    // ID Management
    // -------------------------------------------------------------------------

    private int GetMonsterTeamsSpawned() => monsterTeamsSpawned;
    private void ResetMonsterTeamsSpawned() => monsterTeamsSpawned = 0;
    private void IncrementMonsterTeamsSpawned() => monsterTeamsSpawned++;

    /// <summary>
    /// Increments the spawn counter and returns a new unique team ID string
    /// in the format "{ManagerID}T{count}" (e.g. "MT1").
    /// </summary>
    private string GetNextMonsterTeamID()
    {
        IncrementMonsterTeamsSpawned();
        string newID = $"{GetManagerID()}T{GetMonsterTeamsSpawned()}";
        if (debug.IsDebugging) Debug.Log($"Monster Id Created: {newID}");
        return newID;
    }

    private void SetManagerID(string newManagerID) => ManagerID = newManagerID;
    private string GetManagerID() => ManagerID;

    // -------------------------------------------------------------------------
    // Manager References
    // -------------------------------------------------------------------------

    /// <returns>The list of all currently active <see cref="MonsterTeam"/>s.</returns>
    public List<MonsterTeam> GetMonsterTeams() => monsterTeams;

    /// <returns>The central <see cref="TowerDefenseManager"/>.</returns>
    public TowerDefenseManager GetGameManager() => GameManager;
    /// <returns>The <see cref="TowerManager"/> retrieved via the game manager.</returns>
    public TowerManager GetTowerManager() => GetGameManager().GetTowerManager();
    /// <returns>The <see cref="HeroManager"/> retrieved via the game manager.</returns>
    public HeroManager GetHeroManager() => GetGameManager().GetHeroManager();
    /// <returns>This manager, via the game manager (self-reference convenience).</returns>
    public MonsterManager GetMonsterManager() => GetGameManager().GetMonsterManager();
}