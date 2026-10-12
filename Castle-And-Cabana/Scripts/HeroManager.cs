using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.InputSystem;

// =============================================================================
// Supporting types
// =============================================================================

/// <summary>
/// Lightweight descriptor used to define a hero when spawning a new <see cref="HeroTeam"/>.
/// Serializable so it can appear inline in <see cref="WaveDefinition"/> Inspector lists.
/// </summary>
[System.Serializable]
public struct NewHero
{
    /// <summary>The archetype class to assign to the new hero.</summary>
    public HeroUnit.HeroClass heroClass;

    /// <summary>The starting level to pass to <see cref="UnitInfo"/>.</summary>
    public int level;

    /// <param name="setClass">The hero class to assign.</param>
    /// <param name="setLevel">The starting level to assign.</param>
    public NewHero(HeroUnit.HeroClass setClass, int setLevel)
    {
        this.heroClass = setClass;
        this.level = setLevel;
    }
}

/// <summary>
/// Inspector-friendly representation of a single level's phase schedule.
/// Kept for compatibility with <see cref="CoreManager"/> and <see cref="MonsterManager"/>.
/// </summary>
[System.Serializable]
public class LevelPhase
{
    /// <summary>The level index this phase schedule belongs to.</summary>
    public int level;

    /// <summary>
    /// Human-readable phase strings.
    /// Each index corresponds to a phase, e.g. "Trainee, Trainee" or "null".
    /// </summary>
    [SerializeField]
    public List<string> phases = new List<string>();
}

/// <summary>
/// Maps a <see cref="HeroUnit.HeroClass"/> to the <see cref="HeroUnit"/> prefab
/// that should be instantiated for that class. Populate every entry in
/// <see cref="HeroManager.heroPrefabList"/> so the runtime dictionary is complete.
/// </summary>
[System.Serializable]
public class HeroClassPrefabEntry
{
    /// <summary>The hero class this entry covers.</summary>
    public HeroUnit.HeroClass heroClass;

    /// <summary>The prefab to instantiate when spawning a hero of this class.</summary>
    public HeroUnit prefab;
}

/// <summary>
/// A single wave in the spawn schedule: an ordered list of heroes that spawn
/// together. Leave <see cref="heroes"/> empty to produce a skip/empty wave.
/// </summary>
[System.Serializable]
public class WaveDefinition
{
    [Tooltip("Heroes to spawn in this wave. Leave empty for a skip wave.")]
    public List<NewHero> heroes = new List<NewHero>();
}

// =============================================================================
// HeroManager
// =============================================================================

/// <summary>
/// Manages the full lifecycle of hero teams throughout a level: building the hero
/// prefab dictionary, reading the Inspector-authored wave schedule, spawning teams
/// wave by wave, routing each team through the round-phase coroutines
/// (RoundStart → Action → RoundEnd → BeforeRound), and handling the level-failed
/// escape sequence.
/// </summary>
public class HeroManager : MonoBehaviour
{
    #region // Variables

    // -------------------------------------------------------------------------
    // Hero Prefabs
    // -------------------------------------------------------------------------

    [Header("Hero Prefabs")]
    [Tooltip("Maps each hero class to its spawnable HeroUnit prefab. " +
             "Add one entry per HeroClass; the runtime dictionary is built from this list.")]
    [SerializeField] private List<HeroClassPrefabEntry> heroPrefabList = new List<HeroClassPrefabEntry>();

    /// <summary>Runtime lookup built from <see cref="heroPrefabList"/> during initialization.</summary>
    private Dictionary<HeroUnit.HeroClass, HeroUnit> heroList = new Dictionary<HeroUnit.HeroClass, HeroUnit>();

    // -------------------------------------------------------------------------
    // Team UI
    // -------------------------------------------------------------------------

    [Header("Team UI")]
    [Tooltip("Prefab for the floating team UI overlay instantiated per hero team.")]
    public GameObject HeroTeamUiPrefab;

    // -------------------------------------------------------------------------
    // Wave Schedule
    // -------------------------------------------------------------------------

    [Header("Wave Schedule")]
    [Tooltip("Ordered list of waves for the current level. " +
             "Each wave defines which heroes spawn together. " +
             "Empty waves act as skip/breather rounds.")]
    [SerializeField] private List<WaveDefinition> waveSchedule = new List<WaveDefinition>();

    // -------------------------------------------------------------------------
    // Runtime — Teams
    // -------------------------------------------------------------------------

    [Header("Runtime — Teams")]
    [Tooltip("All hero teams currently active in the scene.")]
    [SerializeField][MMReadOnly] private List<HeroTeam> heroTeams = new List<HeroTeam>();

    [Tooltip("The hero team currently positioned at the end crystal.")]
    [SerializeField][MMReadOnly] private HeroTeam heroTeamAtCrystal = null;

    // -------------------------------------------------------------------------
    // Runtime — State
    // -------------------------------------------------------------------------

    [Header("Runtime — State")]
    [Tooltip("True once all hero teams have completed the escape sequence.")]
    [SerializeField][MMReadOnly] private bool heroesHaveEscaped;

    [Tooltip("True once all phases have been exhausted and no hero teams remain.")]
    [SerializeField][MMReadOnly] private bool allHeroTeamsDefeated;

    // -------------------------------------------------------------------------
    // Speed Settings
    // -------------------------------------------------------------------------

    [Header("Speed Settings")]
    [Tooltip("Movement speed for all heroes at 1x game speed.")]
    [SerializeField] private float baseMoveSpeed = 5f;

    [Tooltip("Scaled movement speed after applying the current game speed multiplier.")]
    [SerializeField][MMReadOnly] private float moveSpeed = 0f;

    [Tooltip("Animator speed multiplier for all heroes at 1x game speed.")]
    [SerializeField] private float baseAnimationSpeed = 1f;

    [Tooltip("Scaled animation speed after applying the current game speed multiplier.")]
    [SerializeField][MMReadOnly] private float animationSpeed = 0f;

    // -------------------------------------------------------------------------
    // References
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("Reference to the TowerManager, used to query floor data and section addons.")]
    [SerializeField] private TowerManager TowerManager;

    [Tooltip("Reference to the central TowerDefenseManager.")]
    [SerializeField] private TowerDefenseManager GameManager;

    // -------------------------------------------------------------------------
    // Debugging
    // -------------------------------------------------------------------------

    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;

    #endregion

    // -------------------------------------------------------------------------
    // Debug Watches
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes all Inspector watch entries each frame with the manager's current
    /// runtime state.
    /// </summary>
    private void UpdateDebugWatches()
    {
        debug.SetWatch("Active Teams", heroTeams != null ? heroTeams.Count.ToString() : "null");
        debug.SetWatch("Heroes Escaped", heroesHaveEscaped.ToString());
        debug.SetWatch("All Defeated", allHeroTeamsDefeated.ToString());
        debug.SetWatch("Move Speed", moveSpeed.ToString("F2"));
        debug.SetWatch("Anim Speed", animationSpeed.ToString("F2"));
        debug.SetWatch("Crystal Team", heroTeamAtCrystal != null ? heroTeamAtCrystal.GetName() : "none");

        if (heroTeams != null)
        {
            int inCombat = heroTeams.Count(t => t.GetIsInCombat());
            int atStart = heroTeams.Count(t => t.GetState() == HeroTeam.HeroTeamStates.AtStartPoint);
            int atEnd = heroTeams.Count(t => t.GetState() == HeroTeam.HeroTeamStates.AtEndPoint);
            int moving = heroTeams.Count(t => t.GetState() == HeroTeam.HeroTeamStates.Moving);
            debug.SetWatch("  In Combat", inCombat.ToString());
            debug.SetWatch("  At Start", atStart.ToString());
            debug.SetWatch("  At End", atEnd.ToString());
            debug.SetWatch("  Moving", moving.ToString());
        }
    }

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Update()
    {
        UpdateDebugWatches();
    }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets up the debug utility and syncs hero speeds to the current game speed.
    /// Called by <see cref="TowerDefenseManager"/> during game setup.
    /// </summary>
    public void InitializeHeroManager()
    {
        InitalizeDebug();
        BuildHeroList();
        UpdateHeroesSpeeds();
    }

    /// <summary>
    /// Builds the runtime <see cref="heroList"/> dictionary from the Inspector-authored
    /// <see cref="heroPrefabList"/>. Logs an error for any class with a null prefab.
    /// </summary>
    private void BuildHeroList()
    {
        heroList.Clear();
        foreach (HeroClassPrefabEntry entry in heroPrefabList)
        {
            if (entry.prefab == null)
            {
                Debug.LogError($"[HeroManager] heroPrefabList entry for {entry.heroClass} has no prefab assigned.");
                continue;
            }
            if (heroList.ContainsKey(entry.heroClass))
            {
                Debug.LogWarning($"[HeroManager] Duplicate entry for {entry.heroClass} in heroPrefabList; keeping first.");
                continue;
            }
            heroList[entry.heroClass] = entry.prefab;
        }
    }

    /// <summary>
    /// Resets per-level state flags and rebuilds the runtime <see cref="levelPhases"/>
    /// dictionary from <see cref="waveSchedule"/> so phase-count queries remain valid.
    /// </summary>
    /// <param name="level">The level index to initialize phases for.</param>
    public void InitializeLevel(int level)
    {
        SetHeroesHaveEscaped(false);
        SetAllHeroTeamsDefeated(false);

        // Build runtime phases dict from the Inspector-authored wave schedule.
        List<List<NewHero>> phases = new List<List<NewHero>>();
        foreach (WaveDefinition wave in waveSchedule)
        {
            phases.Add(wave.heroes.Count > 0 ? new List<NewHero>(wave.heroes) : null);
        }
        levelPhases[level] = phases;
    }

    /// <summary>Creates a fresh <see cref="DebugUtility"/> for this manager.</summary>
    private void InitalizeDebug() { debug = new DebugUtility(false); }

    /// <summary>
    /// Initializes the level schedule and immediately spawns the first wave's
    /// hero team if one is defined.
    /// </summary>
    /// <param name="currentLevel">The level index to start.</param>
    public void LevelStart(int currentLevel)
    {
        InitializeLevel(currentLevel);

        List<NewHero> phaseHeroes = GetPhaseHeroes(currentLevel, 1);
        if (phaseHeroes != null && phaseHeroes.Count > 0)
        {
            SpawnHeroTeam(phaseHeroes);
        }
    }

    /// <summary>
    /// Creates a new <see cref="HeroTeam"/> GameObject, initializes it with the
    /// provided hero descriptors, and registers it with this manager.
    /// </summary>
    /// <param name="newHeroes">The heroes to include in the new team.</param>
    /// <param name="floorToSpawnOn">The floor index the team should start on (default 0).</param>
    public void SpawnHeroTeam(List<NewHero> newHeroes, int floorToSpawnOn = 0)
    {
        if (debug.IsDebugging)
            Debug.Log($"Spawning hero team on floor {floorToSpawnOn} with {newHeroes.Count} heroes.");

        int numOfTeams = GetHeroTeams().Count;

        GameObject newTeamObject = new GameObject("Hero Team " + numOfTeams.ToString());
        newTeamObject.transform.SetParent(this.transform);

        HeroTeam newHeroTeam = newTeamObject.AddComponent<HeroTeam>();
        newHeroTeam.InitializeHeroTeam(GetGameManager(), newHeroes, numOfTeams, floorToSpawnOn);

        GetHeroTeams().Add(newHeroTeam);

        if (debug.IsDebugging)
            Debug.Log("Hero team successfully spawned and linked to HeroManager.");
    }

    /// <summary>
    /// Returns the list of <see cref="NewHero"/> descriptors for the given level and
    /// 1-based phase index, or <c>null</c> if the phase is empty or out of range.
    /// </summary>
    public List<NewHero> GetPhaseHeroes(int level, int phase)
    {
        if (levelPhases.ContainsKey(level) && levelPhases[level].Count > phase - 1)
            return levelPhases[level][phase - 1];
        return null;
    }

    // -------------------------------------------------------------------------
    // Prefab Lookups
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the <see cref="HeroUnit"/> prefab registered for the given
    /// <paramref name="heroClass"/>, or <c>null</c> if no entry was found.
    /// </summary>
    public HeroUnit GetHeroPrefab(HeroUnit.HeroClass heroClass)
    {
        if (debug.IsDebugging)
        {
            Debug.Log("Printing hero Classes");
            // Print all registered HeroClass keys
            foreach (HeroUnit.HeroClass key in heroList.Keys)
            {
                Debug.Log("Registered HeroClass: " + key);
            }
        }

        // Return the matching prefab, or log an error if not found
        if (heroList.TryGetValue(heroClass, out HeroUnit prefab)) return prefab;
        Debug.LogError($"[HeroManager] No prefab registered for HeroClass.{heroClass}.");
        return null;
    }

    /// <summary>
    /// Returns the prefab <see cref="GameObject"/> for the given
    /// <paramref name="heroClass"/>. Convenience wrapper over <see cref="GetHeroPrefab"/>
    /// for callers that need a raw <see cref="GameObject"/> reference.
    /// </summary>
    public GameObject GetHeroModelPrefab(HeroUnit.HeroClass heroClass)
    {
        HeroUnit prefab = GetHeroPrefab(heroClass);
        return prefab != null ? prefab.gameObject : null;
    }

    // -------------------------------------------------------------------------
    // Round Phase Coroutines
    // -------------------------------------------------------------------------

    /// <summary>
    /// Round-start phase: checks for total defeat, sorts teams, then processes
    /// each team in order. In-combat teams either gain energy (if locked) or
    /// advance along the path. Teams at a floor's start point move into slots or
    /// along the path depending on floor type and slot availability.
    /// </summary>
    public IEnumerator RoundStartCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("RoundStart initiated. Hero Manager Processing...");

        CheckIfAllHeroTeamsAreDefeated();
        SortHeroTeams();

        foreach (HeroTeam heroTeam in GetHeroTeams())
        {
            if (debug.IsDebugging)
                Debug.Log($"Processing {heroTeam.GetName()}, Currently: {heroTeam.GetState()}");

            // --- In Combat ---
            if (heroTeam.HeroTeamState == HeroTeam.HeroTeamStates.InCombat)
            {
                if (debug.IsDebugging) Debug.Log("Team is in Combat");

                heroTeam.UpdateIsLocked();

                if (heroTeam.GetIsLockedOnFloor())
                    yield return StartCoroutine(heroTeam.TeamGainEnergy());
                else
                    yield return StartCoroutine(heroTeam.MoveHeroesAlongPath());
            }

            // --- At Start Point ---
            else if (heroTeam.GetState() == HeroTeam.HeroTeamStates.AtStartPoint)
            {
                int currentFloor = heroTeam.GetCurrentFloor();
                int numberOfFloors = GetTowerManager().GetFloors().Count - 1;

                if (debug.IsDebugging)
                    Debug.Log($"{heroTeam.GetName()} is on floor {currentFloor}.");

                // Base floor: always traverse.
                if (currentFloor == 0)
                {
                    if (debug.IsDebugging) Debug.Log($"{heroTeam.GetName()} is on the base floor.");
                    yield return StartCoroutine(heroTeam.MoveHeroesAlongPath());
                }

                // Top floor (boss): enter slots if boss is alive, else traverse.
                else if (currentFloor >= numberOfFloors)
                {
                    TowerSectionAddon floorSection = TowerManager.GetTowerSectionAddon(TowerManager.GetFloor(currentFloor));
                    if (floorSection == null)
                    {
                        Debug.LogError($"Floor section for floor {currentFloor} is not set.");
                        continue;
                    }

                    bool taken = floorSection.GetHeroSlots().GetIsTaken();
                    bool traveledThisTurn = floorSection.GetHaveTraveresedThisTurn();
                    bool bossAlive = floorSection.GetMonsterSlots().GetMonsterTeam() != null
                                           && floorSection.GetMonsterSlots().GetMonsterTeam().GetMonsters().Count > 0;

                    if (debug.IsDebugging)
                    {
                        Debug.Log($"Hero slots on floor {currentFloor} are {(taken ? "taken" : "available")}.");
                        Debug.Log($"Boss is {(bossAlive ? "alive" : "defeated")}.");
                    }

                    if (!bossAlive && !traveledThisTurn)
                        yield return StartCoroutine(heroTeam.MoveHeroesAlongPath());
                    else if (bossAlive && !taken)
                    {
                        if (debug.IsDebugging) Debug.Log("Hero slots available. Moving HeroTeam into slots.");
                        yield return StartCoroutine(heroTeam.MoveHeroesIntoSlots());
                    }
                }

                // Middle floors: enter slots if available, otherwise traverse.
                else
                {
                    TowerSectionAddon floorSection = TowerManager.GetTowerSectionAddon(TowerManager.GetFloor(currentFloor));
                    if (floorSection == null)
                    {
                        Debug.LogError($"Floor section for floor {currentFloor} is not set.");
                        continue;
                    }

                    bool taken = floorSection.GetHeroSlots().GetIsTaken();
                    bool traveledThisTurn = floorSection.GetHaveTraveresedThisTurn();

                    if (debug.IsDebugging)
                        Debug.Log($"Hero slots on floor {currentFloor} are {(taken ? "taken" : "available")}.");

                    if (taken && !traveledThisTurn)
                        yield return StartCoroutine(heroTeam.MoveHeroesAlongPath());
                    else if (!taken && !traveledThisTurn)
                    {
                        yield return StartCoroutine(heroTeam.MoveHeroesIntoSlots());
                        yield return StartCoroutine(heroTeam.TeamGainEnergy());
                    }
                }
            }

            else
            {
                if (debug.IsDebugging)
                    Debug.Log($"Unknown State type. Current state: {heroTeam.GetState()}");
            }
        }

        if (debug.IsDebugging) Debug.Log("RoundStart processing complete.");
    }

    /// <summary>
    /// Round-action phase: every in-combat team executes its heroes' actions
    /// sequentially via <see cref="HeroTeam.TeamExecuteAction"/>.
    /// </summary>
    public IEnumerator RoundActionCoroutine()
    {
        CheckIfAllHeroTeamsAreDefeated();
        if (debug.IsDebugging) Debug.Log("Hero Manager RoundAction initiated...");

        foreach (var heroTeam in GetHeroTeams())
        {
            if (debug.IsDebugging)
                Debug.Log($"Processing HeroTeam Action: Floor {heroTeam.GetCurrentFloor()}, State: {heroTeam.GetState()}");

            if (heroTeam.GetState() == HeroTeam.HeroTeamStates.InCombat)
            {
                if (debug.IsDebugging) Debug.Log("Team is in Combat");
                yield return StartCoroutine(heroTeam.TeamExecuteAction());
            }
        }

        if (debug.IsDebugging) Debug.Log("RoundAction processing complete.");
        if (debug.IsDebugging) Debug.Log("________________________________________________________");
    }

    /// <summary>
    /// Round-end phase: checks defeat state, spawns the next wave via
    /// <see cref="SpawnNextWave"/>, and re-sorts the team list.
    /// </summary>
    public IEnumerator RoundEndCoroutine()
    {
        CheckIfAllHeroTeamsAreDefeated();
        if (debug.IsDebugging) Debug.Log("Hero Manager RoundEnd initiated...");

        foreach (HeroTeam heroTeam in GetHeroTeams())
        {
            // Reserved for future per-team round-end logic.
        }

        yield return 0;

        if (debug.IsDebugging) Debug.Log("RoundEnd processing complete.");

        SpawnNextWave();
        SortHeroTeams();
    }

    /// <summary>
    /// Before-round phase: any team that reached its floor's end point during the
    /// previous round is advanced to the next floor.
    /// </summary>
    public IEnumerator BeforeRoundCoroutine()
    {
        CheckIfAllHeroTeamsAreDefeated();
        if (debug.IsDebugging) Debug.Log("Hero Manager BeforeRound initiated...");

        foreach (HeroTeam heroTeam in GetHeroTeams())
        {
            if (debug.IsDebugging)
                Debug.Log($"Processing HeroTeam: Floor {heroTeam.GetCurrentFloor()}, State: {heroTeam.GetState()}");

            if (heroTeam.GetState() == HeroTeam.HeroTeamStates.AtEndPoint)
            {
                if (debug.IsDebugging) Debug.Log("Team is at Endpoint of the floor.");
                yield return new WaitForSeconds(0.05f);
                heroTeam.GotoNextFloor();
            }
        }

        if (debug.IsDebugging) Debug.Log("BeforeRound processing complete.");
        SortHeroTeams();
    }

    /// <summary>Start-to-action transition phase. Reserved for future use.</summary>
    public IEnumerator StartToActionCoroutine() { yield return null; }

    /// <summary>
    /// Action-to-end transition phase: for in-combat teams, checks whether the
    /// floor is dying (moves team to end point) or the boss is defeated (triggers
    /// the level-failed sequence by moving the team to the end crystal).
    /// </summary>
    public IEnumerator ActionToEndCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Hero Manager ActionToEnd initiated...");
        CheckIfAllHeroTeamsAreDefeated();

        foreach (HeroTeam heroTeam in GetHeroTeams())
        {
            if (debug.IsDebugging)
                Debug.Log($"Processing HeroTeam ActionToEnd: Floor {heroTeam.GetCurrentFloor()}, State: {heroTeam.GetState()}");

            if (heroTeam.GetState() != HeroTeam.HeroTeamStates.InCombat) continue;

            int teamFloor = heroTeam.GetCurrentFloor();
            int topFloor = GetTowerManager().GetFloors().Count - 1;
            bool onTopFloor = teamFloor >= topFloor;

            if (!onTopFloor)
            {
                bool floorDying = GetTowerManager()
                    .GetTowerSectionAddon(GetTowerManager().GetFloor(heroTeam.GetCurrentFloor()))
                    .GetIsDying();

                if (floorDying)
                    yield return StartCoroutine(heroTeam.MoveHeroesAlongPath());
            }
            else
            {
                MonsterTeam bossTeam = heroTeam.GetMonsterTeamInCombatWith();
                bool allMonstersDead = true;

                if (bossTeam != null)
                {
                    foreach (MonsterUnit monster in bossTeam.GetMonsters())
                    {
                        if (!monster.GetIsDying()) { allMonstersDead = false; break; }
                    }
                }

                if (bossTeam == null || allMonstersDead)
                {
                    yield return StartCoroutine(heroTeam.MoveHeroesToEndCrystal());
                    SetHeroTeamAtCrystal(heroTeam);
                    GetGameManager().SetHasFailedLevel(true);
                }
            }
        }

        if (debug.IsDebugging) Debug.Log("ActionToEnd processing complete.");
    }

    /// <summary>After-round transition phase. Reserved for future use.</summary>
    public IEnumerator AfterRoundCoroutine() { yield return null; }

    /// <summary>
    /// Level-failed sequence: the crystal team performs an attack animation, all
    /// remaining teams jump out of the tower, heroes regroup at the failed-level
    /// positions, play their victory animations, and <see cref="heroesHaveEscaped"/>
    /// is set to <c>true</c> once everything completes.
    /// </summary>
    public IEnumerator LevelFailedCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Hero Manager Level Failed initiated...");
        Debug.Log("Hero Manager Level Failed initiated...");

        // Crystal team attacks the crystal.
        HeroTeam teamAtCrystal = GetHeroTeamAtCrystal();
        if (teamAtCrystal != null)
        {
            yield return StartCoroutine(teamAtCrystal.TeamAttackCrystal());
        }

        // All teams jump out of the tower simultaneously.
        foreach (HeroTeam team in GetHeroTeams())
        {
            StartCoroutine(team.TeamRunToCenterAndJumpOut());
        }

        yield return new WaitUntil(() => GetHeroTeams().All(team => team.GetEscapedTower()));

        // Collect all escaped heroes into a flat list.
        List<HeroUnit> escapedHeroes = new List<HeroUnit>();
        foreach (HeroTeam team in GetHeroTeams())
        {
            foreach (HeroUnit hero in team.GetHeroes())
            {
                escapedHeroes.Add(hero);
            }
        }

        List<GameObject> failedLevelPositions = GetTowerManager().GetLevelFailedHeroPositions();
        if (failedLevelPositions == null || failedLevelPositions.Count == 0)
        {
            Debug.LogError("Failed Level positions not found. Cannot proceed.");
            yield break;
        }

        // Distribute heroes evenly across the available positions with per-slot offsets.
        int numPositions = failedLevelPositions.Count;
        int heroesPerPosition = Mathf.CeilToInt((float)escapedHeroes.Count / numPositions);
        int heroIndex = 0;
        float offsetDistance = 3f;

        for (int i = 0; i < failedLevelPositions.Count; i++)
        {
            GameObject position = failedLevelPositions[i];
            if (heroIndex >= escapedHeroes.Count) break;

            for (int j = 0; j < heroesPerPosition && heroIndex < escapedHeroes.Count; j++)
            {
                HeroUnit hero = escapedHeroes[heroIndex];
                int offsetIndex = j % 6;

                Vector3 offset = offsetIndex switch
                {
                    1 => new Vector3(-offsetDistance, 0f, 0f),
                    2 => new Vector3(offsetDistance, 0f, 0f),
                    3 => new Vector3(-offsetDistance, 0f, -offsetDistance),
                    4 => new Vector3(0f, 0f, -offsetDistance),
                    5 => new Vector3(offsetDistance, 0f, -offsetDistance),
                    _ => Vector3.zero,
                };

                hero.SetMovingToVictoryPosition(true);
                hero.SetAtVictoryPosition(false);
                StartCoroutine(hero.MoveToPosition(position.transform.position + offset, position.transform.rotation));

                heroIndex++;
            }
        }

        yield return new WaitUntil(() => escapedHeroes.All(h => h.GetAtVictoryPosition()));

        foreach (HeroUnit hero in escapedHeroes)
        {
            hero.SetAnimationState(HeroUnit.HeroAnimations.Victory);
        }

        foreach (HeroTeam team in GetHeroTeams()) { team.UnlinkTeam(); }

        yield return new WaitForSeconds(0.5f);

        SetHeroesHaveEscaped(true);
    }

    // -------------------------------------------------------------------------
    // Internal Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets <see cref="allHeroTeamsDefeated"/> to <c>true</c> only when all
    /// phases have been exhausted and no hero teams remain in the scene.
    /// </summary>
    private void CheckIfAllHeroTeamsAreDefeated()
    {
        if (!GetGameManager().GetReachedEndOfPhases())
        {
            SetAllHeroTeamsDefeated(false);
            return;
        }

        if (GetHeroTeams().Count <= 0)
        {
            SetAllHeroTeamsDefeated(true);
        }
    }

    /// <summary>
    /// Looks up the hero descriptors for the next phase and spawns a team if
    /// one is defined. Called at the end of each round.
    /// </summary>
    private void SpawnNextWave()
    {
        int currentLevel = GetGameManager().GetLevel();
        int currentPhase = GetGameManager().GetRound();

        List<NewHero> phaseHeroes = GetPhaseHeroes(currentLevel, currentPhase + 1);
        if (phaseHeroes != null && phaseHeroes.Count > 0)
        {
            SpawnHeroTeam(phaseHeroes);
        }
    }

    /// <summary>
    /// Re-orders <see cref="heroTeams"/> so that in-combat teams act first,
    /// with ties broken by floor number (higher floors first).
    /// </summary>
    private void SortHeroTeams()
    {
        SetHeroTeams(GetHeroTeams()
            .OrderByDescending(t => t.GetIsInCombat())
            .ThenByDescending(t => t.GetCurrentFloor())
            .ToList());
    }

    /// <summary>
    /// Removes the hero team with the given ID from the active team list.
    /// </summary>
    /// <param name="whichHtID">The <see cref="HeroTeam"/> ID to remove.</param>
    public void RemoveHeroTeam(int whichHtID)
    {
        List<HeroTeam> heroTeams = GetHeroTeams();

        for (int i = heroTeams.Count - 1; i >= 0; i--)
        {
            if (heroTeams[i].GetHtID() == whichHtID)
            {
                heroTeams.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>Resets the hero team list to a new empty instance.</summary>
    private void InitalizeList() { heroTeams = new List<HeroTeam>(); }

    // -------------------------------------------------------------------------
    // Speed Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Recalculates and applies both move and animation speed multipliers based
    /// on the current game speed setting.
    /// </summary>
    public void UpdateHeroesSpeeds()
    {
        UpdateHeroesMoveSpeed();
        UpdateHeroesAnimationSpeed();
    }

    private void UpdateHeroesMoveSpeed()
    {
        float gameSpeed = (int)GetGameManager().GetGameSpeed();
        SetMoveSpeed(GetBaseMoveSpeed() * (gameSpeed / 2f));
    }

    private void UpdateHeroesAnimationSpeed()
    {
        float gameSpeed = (int)GetGameManager().GetGameSpeed();
        SetAnimationSpeed(GetBaseAnimationSpeed() * (gameSpeed / 2f));
        UpdateHeroTeamsAnimationSpeeds();
    }

    private void UpdateHeroTeamsAnimationSpeeds()
    {
        List<HeroTeam> heroTeams = GetHeroTeams();
        for (int i = 0; i < heroTeams.Count; i++)
        {
            if (heroTeams[i] != null) { heroTeams[i].UpdateHeroesAnimationSpeed(); }
        }
    }

    // -------------------------------------------------------------------------
    // Getters & Setters
    // -------------------------------------------------------------------------

    /// <param name="newHeroTeams">New list of active hero teams to assign.</param>
    public void SetHeroTeams(List<HeroTeam> newHeroTeams) => heroTeams = newHeroTeams;
    /// <returns>The list of all currently active <see cref="HeroTeam"/>s.</returns>
    public List<HeroTeam> GetHeroTeams() => heroTeams;

    /// <param name="newHeroTeamAtCrystal">The team to designate as the crystal team.</param>
    public void SetHeroTeamAtCrystal(HeroTeam newHeroTeamAtCrystal) => heroTeamAtCrystal = newHeroTeamAtCrystal;
    /// <returns>The <see cref="HeroTeam"/> currently at the end crystal, or <c>null</c>.</returns>
    public HeroTeam GetHeroTeamAtCrystal() => heroTeamAtCrystal;

    /// <returns>The team UI prefab instantiated for each hero team.</returns>
    public GameObject GetHeroTeamUiPrefabl() => HeroTeamUiPrefab;

    /// <returns><c>true</c> once all hero teams have completed the escape sequence.</returns>
    public bool GetHeroesHaveEscaped() => heroesHaveEscaped;
    /// <param name="newHeroesHaveEscaped">New escaped state to assign.</param>
    public void SetHeroesHaveEscaped(bool newHeroesHaveEscaped) => heroesHaveEscaped = newHeroesHaveEscaped;

    /// <returns><c>true</c> when all phases are exhausted and no teams remain.</returns>
    public bool GetAllHeroTeamsDefeated() => allHeroTeamsDefeated;
    /// <param name="newAllHeroTeamsDefeated">New all-defeated state to assign.</param>
    public void SetAllHeroTeamsDefeated(bool newAllHeroTeamsDefeated) => allHeroTeamsDefeated = newAllHeroTeamsDefeated;

    /// <returns>The runtime phase schedule dictionary, keyed by level index.</returns>
    public Dictionary<int, List<List<NewHero>>> GetLevelPhases() => levelPhases;

    /// <returns>The scaled hero movement speed for the current game speed.</returns>
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

    /// <param name="newManager">The <see cref="TowerDefenseManager"/> to bind.</param>
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    /// <returns>The central <see cref="TowerDefenseManager"/>.</returns>
    public TowerDefenseManager GetGameManager() => GameManager;

    /// <returns>The <see cref="TowerManager"/> retrieved via the game manager.</returns>
    public TowerManager GetTowerManager() => GetGameManager().GetTowerManager();
    /// <returns>This manager, via the game manager (self-reference convenience).</returns>
    public HeroManager GetHeroManager() => GetGameManager().GetHeroManager();
    /// <returns>The <see cref="MonsterManager"/> retrieved via the game manager.</returns>
    public MonsterManager GetMonsterManager() => GetGameManager().GetMonsterManager();

    // -------------------------------------------------------------------------
    // Private runtime state (backing field for GetLevelPhases)
    // -------------------------------------------------------------------------

    private Dictionary<int, List<List<NewHero>>> levelPhases = new Dictionary<int, List<List<NewHero>>>();
}
