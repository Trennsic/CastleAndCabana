using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using static MonsterUnit;

public class CoreManager : MonoBehaviour
{
    #region /// Attributes
    /// <summary>
    /// A dictionary that stores level waves by their phase number.
    /// Each key represents a wave number (int), and each value is a list of monster teams in that wave.
    /// </summary>
    [SerializeField] private List<LevelPhase> levelPhaseInspectorView = new List<LevelPhase>();

    private Dictionary<int, List<List<NewMonster>>> levelPhases = new Dictionary<int, List<List<NewMonster>>>();

    /// <summary>
    /// Dictionary to track which CoreTypes are unlocked.
    /// Each key represents a CoreType, and the value is a boolean indicating whether it is unlocked.
    /// </summary>
    private Dictionary<CoreUnit.CoreTypes, bool> coreUnlockStatus;
    /// <summary>
    /// A list of monster teams that have been spawned in the game.
    /// This is populated at runtime and used to track all active monster teams in the game.
    /// </summary>
    [SerializeField] private List<CoreUnit> cores = new List<CoreUnit>();

    private Dictionary<CoreUnit.CoreTypes, int> coreEssenceCosts = new Dictionary<CoreUnit.CoreTypes, int>();


    public GameObject CoreUnitUiPrefab;

    /// <summary>
    /// The manager that handles the tower's logic and game progression.
    /// This should be assigned in the inspector and provides access to tower-related features such as floors and sections.
    /// </summary>
    [SerializeField] private TowerDefenseManager GameManager; // Link to Tower Manager

    /// <summary>
    /// The GameObject prefab for the Guard model.
    /// This should be assigned in the inspector with a 3D model representing the Guard monster.
    /// </summary>
    public GameObject BasicModel;
    public GameObject HeartModel;
    public GameObject BossModel;



    /// <summary>
    /// The speed at which monsters move through the tower. This value affects the movement speed for all monsters.
    /// The higher the value, the faster the monsters will move.
    /// </summary>
    public float moveSpeed = .05f;

    

    /// <summary>
    /// Flag to enable or disable debugging output in the console.
    /// Set to true to print additional debug logs for game operations like monster movement, state transitions, etc.
    /// </summary>
    #region // Debugging
    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;
    #endregion
    #endregion
    #region /// Methods
    public void InitializeCoreManager()
    {
        InitalizeDebug();
        InitializeCoreUnlockStatus();
        // Unlock correct cores
        UnlockCoreType(CoreUnit.CoreTypes.Basic);
        LockCoreType(CoreUnit.CoreTypes.Heart);
        InitializeCoreEssenceCosts();
        // Spawn Boss Core
        //SpawnBossCore();
    }
    public void SpawnCore(int floorToSpawnOn, CoreUnit.CoreTypes coreType)
    {

        // Find out how many cores have been spawned
        int numOfCores = GetCores().Count;

        // Create an empty core object
        GameObject newCoreObject = new GameObject("Core: " + numOfCores.ToString());

        // Parent the Team to this object
        newCoreObject.transform.SetParent(this.transform);

        // Add a new CoreUnit Component to the new core object
        CoreUnit newCoreUnit = newCoreObject.AddComponent<CoreUnit>();

        // Configure the CoreUnit
        newCoreUnit.InitializeCore(GetGameManager(), coreType, numOfCores, floorToSpawnOn);

        // Add the new monster team to your monster team list
        GetCores().Add(newCoreUnit);
    }
    public void SpawnBossCore()
    {

        //
        CoreUnit.CoreTypes coreType = CoreUnit.CoreTypes.Boss;
        int topFloor = GetTowerManager().GetFloors().Count - 1;
        // Spawn the boss team
        SpawnCore( topFloor, coreType);
    }

    /// <summary>
    /// Initializes the core unlock status dictionary.
    /// All CoreTypes are set to locked (false) by default.
    /// </summary>
    private void InitializeCoreUnlockStatus()
    {
        coreUnlockStatus = new Dictionary<CoreUnit.CoreTypes, bool>();

        foreach (CoreUnit.CoreTypes coreType in System.Enum.GetValues(typeof(CoreUnit.CoreTypes)))
        {
            coreUnlockStatus[coreType] = false;
        }
    }
    private void InitalizeDebug() { debug = new DebugUtility(false); }
    /// <summary>
    /// Checks if a CoreType is unlocked.
    /// </summary>
    /// <param name="coreType">The CoreType to check.</param>
    /// <returns>True if unlocked; otherwise, false.</returns>
    public bool IsCoreTypeUnlocked(CoreUnit.CoreTypes coreType)
    {
        if (coreUnlockStatus.TryGetValue(coreType, out bool isUnlocked))
        {
            return isUnlocked;
        }

        Debug.LogWarning($"CoreType {coreType} not found in coreUnlockStatus dictionary.");
        return false;
    }

    /// <summary>
    /// Retrieves a list of CoreTypes that are currently unlocked.
    /// </summary>
    /// <returns>A list of CoreUnit.CoreTypes representing the unlocked core types.</returns>
    public List<CoreUnit.CoreTypes> GetListOfUnlockedCores()
    {
        if (coreUnlockStatus == null)
        {
            Debug.LogError("coreUnlockStatus is null!");
            return new List<CoreUnit.CoreTypes>(); // Return an empty list as fallback
        }

        return coreUnlockStatus
            .Where(kvp => kvp.Value) // Select only those with a value of 'true'
            .Select(kvp => kvp.Key) // Extract the CoreType (key)
            .ToList();              // Convert the result to a List
    }
    public CoreUnit.CoreTypes ConvertStringToCoreTypes(string coreTypeString)
    {
        CoreUnit.CoreTypes coreType;

        // Try to parse the string into an enum value
        if (Enum.TryParse(coreTypeString, true, out coreType))
        {
            // Successfully parsed the string to coreType enum
            return coreType;
        }
        else
        {
            // Handle the case where parsing fails (e.g., return a default value or log an error)
            Debug.LogError($"Invalid core type string: {coreTypeString}. Returning default CoreType.");
            return CoreUnit.CoreTypes.Basic; // Replace with a default value of your choice
        }
    }

    /// <summary>
    /// Unlocks a specific CoreType.
    /// </summary>
    /// <param name="coreType">The CoreType to unlock.</param>
    public void UnlockCoreType(CoreUnit.CoreTypes coreType)
    {
        if (coreUnlockStatus.ContainsKey(coreType))
        {
            coreUnlockStatus[coreType] = true;
            if (debug.IsDebugging) Debug.Log($"CoreType {coreType} has been unlocked.");
        }
        else
        {
            Debug.LogWarning($"CoreType {coreType} not found in coreUnlockStatus dictionary.");
        }
    }

    /// <summary>
    /// Locks a specific CoreType.
    /// </summary>
    /// <param name="coreType">The CoreType to lock.</param>
    public void LockCoreType(CoreUnit.CoreTypes coreType)
    {
        if (coreUnlockStatus.ContainsKey(coreType))
        {
            coreUnlockStatus[coreType] = false;
            Debug.Log($"CoreType {coreType} has been locked.");
        }
        else
        {
            Debug.LogWarning($"CoreType {coreType} not found in coreUnlockStatus dictionary.");
        }
    }

    #region // Core Essence Cost
    // Initializes the essence cost for each core type
    private void InitializeCoreEssenceCosts()
    {
        coreEssenceCosts[CoreUnit.CoreTypes.Basic] = 100;
        coreEssenceCosts[CoreUnit.CoreTypes.Heart] = 150;
        coreEssenceCosts[CoreUnit.CoreTypes.Boss] = 999;
    }

    // Sets the essence cost for a specific core type
    public void SetCoreEssenceCost(CoreUnit.CoreTypes coreType, int cost)
    {
        if (coreEssenceCosts.ContainsKey(coreType))
        {
            coreEssenceCosts[coreType] = cost;
        }
        else
        {
            Debug.LogWarning($"Core type {coreType} not found in dictionary.");
        }
    }

    // Gets the essence cost for a specific core type
    public int GetCoreEssenceCost(CoreUnit.CoreTypes coreType)
    {
        if (coreEssenceCosts.TryGetValue(coreType, out int cost))
        {
            return cost;
        }
        else
        {
            Debug.LogWarning($"Core type {coreType} not found in dictionary.");
            return -1; // Return a default value indicating an error
        }
    }
    #endregion


    public void LevelStart(int currentLevel)
    {
        
    }
    public IEnumerator RoundStartCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("RoundStart initiated. Core Manager Processing...");

        // At the start of each round, cycle through each monster team that has been spawned
        foreach (CoreUnit core in GetCores())
        {
            #region // Update heroes on the same floor with the monster team
            // Process monster actions with a delay
            yield return StartCoroutine(core.ExecuteRoundStart());
            #endregion
        }

        if (debug.IsDebugging) Debug.Log("RoundStart processing complete.");
    }

    public IEnumerator RoundActionCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("RoundStart initiated. Core Manager Processing...");

        // At the start of each round, cycle through each monster team that has been spawned
        foreach (CoreUnit core in GetCores())
        {
            #region // Update heroes on the same floor with the monster team
            // Process monster actions with a delay
            yield return StartCoroutine(core.ExecuteRoundAction());
            #endregion
        }

        if (debug.IsDebugging) Debug.Log("RoundStart processing complete.");
    }

    public IEnumerator RoundEndCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("RoundStart initiated. Core Manager Processing...");

        // At the start of each round, cycle through each monster team that has been spawned
        foreach (CoreUnit core in GetCores())
        {
            #region // Update heroes on the same floor with the monster team
            // Process monster actions with a delay
            yield return StartCoroutine(core.ExecuteRoundEnd());
            #endregion
        }

        if (debug.IsDebugging) Debug.Log("RoundStart processing complete.");
    }
    public IEnumerator BeforeRoundCoroutine()
    {
        // At the start of each round, cycle through each monster team that has been spawned
        foreach (CoreUnit core in GetCores())
        {
            #region // Update heroes on the same floor with the monster team
            // Process monster actions with a delay
            yield return StartCoroutine(core.ExecuteBeforeRound());
            #endregion
        }
    }
    public IEnumerator StartToActionCoroutine()
    {
        // At the start of each round, cycle through each monster team that has been spawned
        foreach (CoreUnit core in GetCores())
        {
            #region // Update heroes on the same floor with the monster team
            // Process monster actions with a delay
            yield return StartCoroutine(core.ExecuteStartToAction());
            #endregion
        }
    }
    public IEnumerator ActionToEndCoroutine()
    {
        // At the start of each round, cycle through each monster team that has been spawned
        foreach (CoreUnit core in GetCores())
        {
            #region // Update heroes on the same floor with the monster team
            // Process monster actions with a delay
            yield return StartCoroutine(core.ExecuteActionToEnd());
            #endregion
        }
    }
    public IEnumerator AfterRoundCoroutine() 
    {
        // At the start of each round, cycle through each monster team that has been spawned
        foreach (CoreUnit core in GetCores())
        {
            #region // Update heroes on the same floor with the monster team
            // Process monster actions with a delay
            yield return StartCoroutine(core.ExecuteAfterRound());
            #endregion
        }
    }

    

    public GameObject GetCoreModelPrefab(CoreUnit.CoreTypes coreType)
    {
        return coreType switch
        {
            CoreUnit.CoreTypes.Basic => BasicModel,
            CoreUnit.CoreTypes.Heart => HeartModel,
            CoreUnit.CoreTypes.Boss => BossModel,
            _ => null,
        };
    }

    private void InitalizeList() { SetCores(new List<CoreUnit>());}
    public void  SetCores(List<CoreUnit> newCores) => cores = newCores;
    public GameObject GetCoreUnitUiPrefab() => CoreUnitUiPrefab;
    public List<CoreUnit> GetCores() => cores;
    public TowerDefenseManager GetGameManager() => GameManager;
    public TowerManager GetTowerManager() { return GetGameManager().GetTowerManager(); }
    public HeroManager GetHeroManager() { return GetGameManager().GetHeroManager(); }
    public MonsterManager GetMonsterManager() { return GetGameManager().GetMonsterManager(); }

    #endregion
}
