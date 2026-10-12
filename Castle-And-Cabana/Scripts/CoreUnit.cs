using System.Collections;
using TMPro;
using UnityEngine;
using static CoreUnit;
using static HeroTeamUiAddon;
using static HeroUnit;
using static MonsterTeam;
using static MonsterUnit;
using static UnitInfo;

/// <summary>
/// Represents the base class for all core unit types in the game..
/// Provides foundational functionality and properties for unit initialization, state management, and interaction handling.
/// </summary>
[DisallowMultipleComponent]
public class CoreUnit : MonoBehaviour
{

    #region // Variables

    #region // Enums
    /// <summary>
    /// Types of cores available.
    /// </summary>
    public enum CoreTypes
    {
        Basic,
        Heart,
        Boss,
    }

    /// <summary>
    /// Animation states for the core.
    /// </summary>
    public enum CoreAnimations
    {
        Idle,
    }

    /// <summary>
    /// Operational states of the core.
    /// </summary>
    public enum CoreStates
    {
        Inactive,
        Idle,
        Dying,
    }
    #endregion

    #region // Unit Info
    [Header("Unit Info")]
    [Tooltip("Metadata about the core.")]
    [SerializeField] private CoreInfo coreInfo;

    [Tooltip("The type of core (e.g., Basic, Heart).")]
    [SerializeField] private CoreTypes coreType;

    [Tooltip("Unique ID assigned to the core.")]
    [SerializeField] private int cuID;

    [Tooltip("The current floor where the core is located.")]
    [SerializeField] private int currentFloor;

    [Tooltip("The operational state of the core.")]
    [SerializeField] private CoreStates coreState = CoreStates.Inactive;

    [Tooltip("Tracks whether the core has been initialized.")]
    [SerializeField] private bool isInitialized = false;

    [Tooltip("Indicates whether the core is in the dying state.")]
    [SerializeField] private bool isDying = false;

    [Tooltip("Initial duration for the dying timer.")]
    [SerializeField] private float dyingTimerInit = 10f;
    

    [Tooltip("Current remaining time on the dying timer.")]
    [SerializeField] private float dyingTimer;

    [Tooltip("Tracks whether dying coroutines have started.")]
    [SerializeField] private bool dyingCoroutinesStarted = false;

    [Tooltip("Initial duration for the death timer.")]
    [SerializeField] private float deathTimerInit = 2f;

    [Tooltip("Current remaining time on the death timer.")]
    [SerializeField] private float deathTimer;

    [Tooltip("Indicates whether the core is actively locking onto targets.")]
    [SerializeField] private bool isLocking;

    [Tooltip("Indicates whether the core has been setup or not")]
    [SerializeField] private bool isInitalized = false;
    #endregion

    #region // Model
    [Header("Model")]
    [Tooltip("Prefab for the core model.")]
    [SerializeField] private GameObject modelPrefab;

    [Tooltip("Animator component controlling the core's animations.")]
    [SerializeField] private Animator animator;

    [Tooltip("Current animation state of the core.")]
    [SerializeField] private CoreAnimations animationState;
    #endregion

    #region // References
    [Header("References")]
    [Tooltip("Reference to the CoreUnitPrefabAddon component.")]
    [SerializeField] private CoreUnitPrefabAddon coreAddon;

    [Tooltip("Reference to the PopupTextAddon component.")]
    [SerializeField] private PopupTextAddon popupTextAddon;

    [Tooltip("Reference to the game manager for managing gameplay.")]
    [SerializeField] private TowerDefenseManager GameManager;

    [Tooltip("The floor object currently associated with the core.")]
    [SerializeField] private GameObject floorObject;

    [Tooltip("Addon component for managing floor-specific behavior.")]
    [SerializeField] private TowerSectionAddon floorAddon;

    [Tooltip("Addon component for managing the core's slot behavior.")]
    [SerializeField] private CoreSlotAddon slotAddon;

    [Tooltip("Prefab for the death visual effects.")]
    [SerializeField] private GameObject DeathVfx;

    [SerializeField] private GameObject coreUi;
    [SerializeField] private CoreUnitUiAddon coreUiAddon;
    #endregion

    #region // Debugging
    [Header("Debugging")]
    [Tooltip("Utility for enabling and managing debug messages.")]
    [SerializeField] private DebugUtility debug;

    [Tooltip("GameObject used for positioning the core.")]
    [SerializeField] private GameObject positioningObject;
    #endregion

    #endregion

    
    #region // Methods ==================
    #region // Unity Functions
    private void Start()
    {
        // Initialize with debugging enabled
        debug = new DebugUtility(false);
    }
    private void Update()
    {
        CheckForUnitDeath();
        UpdateForUnitDying();
    }
    #endregion
    #region // User Functions
    #region // Initalization
    /// <summary>
    /// Initializes the core with the specified parameters.
    /// </summary>
    /// <param name="manager">The TowerDefenseManager managing this core.</param>
    /// <param name="coreType">The type of the core.</param>
    /// <param name="cuID">The unique identifier for the core unit.</param>
    /// <param name="floorToSpawnOn">The floor on which to spawn the core.</param>
    public void InitializeCore(TowerDefenseManager manager, CoreTypes coreType, int cuID, int floorToSpawnOn)
    {
        // Initalize Debug Object
        InitalizeDebug();
        // Link Tower Manager
        SetGameManager(manager);
        // Set the monster Team ID
        SetCuID(cuID);
        // Get the Core Details from Core Info Class
        SetupInfo(coreType);
        // Enable, Posistion and spawn Monster Team at Start Location
        SetupCore(floorToSpawnOn);
        //
        InitializeCoreUi();
        //
        InitializeSpecificCoreActions();
        // This has been setup (No long inactive)
        SetState(CoreStates.Idle);
        // Cores start off not locking targets
        SetisLocking(true);
        // 
        SetIsInitalized(true);
    }
    private void InitializeCoreUi()
    {
        // Get the Ui Prefab
        GameObject coreUiPrefab = GetGameManager().GetCoreManager().GetCoreUnitUiPrefab();
        GameObject coreUiObject = Instantiate(coreUiPrefab);
        // Set the Ui object
        coreUiObject.transform.SetParent(gameObject.transform);
        SetCoreUi(coreUiObject);
        // Set the Ui Addon
        SetCoreUiAddon(coreUiObject.GetComponent<CoreUnitUiAddon>());
        GetCoreUiAddon().SetCoreUnit(this);
        // 
        UpdateCoreUiAddon();

        // Move to the correct position
        UpdateCoreUiPosition();
    }
    private void InitializeSpecificCoreActions()
    {
        CoreTypes coreType = GetCoreType();
        if (coreType == CoreTypes.Basic)
        {
            #region// Get information
            // Get Monster Slots Addon & Team
            TowerSectionAddon floorAddon = GetFloorAddon();
            if (floorAddon == null) return;
            MonsterSlotsAddon monsterSlotsAddon = floorAddon.GetMonsterSlots();
            MonsterTeam monsterTeam = monsterSlotsAddon.GetMonsterTeam();
            // Create Monster
            NewMonster dummiiMonster = new NewMonster(MonsterUnit.MonsterTypes.Dummii, 1);
            // Spawn a Dummii monster in the 4th slot
            monsterTeam.AddMonsterToTeam(dummiiMonster, 4);
            #endregion

            
        }
    }
    /// <summary>
    /// Initializes the debugging utility for this core.
    /// </summary>
    private void InitalizeDebug() { debug = new DebugUtility(false); }
    /// <summary>
    /// Sets up core information based on its type.
    /// </summary>
    /// <param name="coreType">The type of the core.</param>
    private void SetupInfo(CoreTypes coreType)
    {
        CoreInfo newInfo = new CoreInfo(coreType);
        SetCoreInfo(newInfo);
    }
    /// <summary>
    /// Configures the core's position and initializes its components.
    /// </summary>
    /// <param name="floorToSpawnOn">The floor on which to position the core.</param>
    private void SetupCore(int floorToSpawnOn = 0)
    {
        #region// Setup Current Floor, Update Floor references
        SetCurrentFloor(floorToSpawnOn);
        #endregion

        #region // Create the core
        SpawnCore();
        #endregion
    }
    /// <summary>
    /// Spawns the core model and links necessary components.
    /// </summary>
    public void SpawnCore()
    {
        #region// Create from monster prefab
        // Pull the core manager
        CoreManager coreManager = GetCoreManager();
        if (debug.IsDebugging)
        {
            Debug.Log("Core manager retrieved successfully.");
        }

        // Get the Model based on your class
        GameObject corePrefab = coreManager.GetCoreModelPrefab(GetCoreType());

        // Create the prefab at any location
        GameObject coreObject = Instantiate(corePrefab);


        // Parent the transform to the unit transform
        coreObject.transform.position = this.gameObject.transform.position;
        coreObject.transform.rotation = this.gameObject.transform.rotation;
        coreObject.transform.SetParent(this.gameObject.transform);

        // Set the model to this
        SetModel(coreObject);
        #endregion

        #region// Link Addons and Animator from prefab
        SetCorePrefabAddon(coreObject.GetComponent<CoreUnitPrefabAddon>());

        if (GetCorePrefabAddon() == null)
        {
            SetCorePrefabAddon(coreObject.GetComponentInChildren<CoreUnitPrefabAddon>());
            if (debug.IsDebugging && GetCorePrefabAddon() != null)
            {
                Debug.Log($"CoreAddon component found in children and set.");
            } 
        }
        if (GetCorePrefabAddon() != null)
        {
            DeathVfx = GetCorePrefabAddon().GetDeathVfx();
        }

        // Initali the core add on
        if (GetCorePrefabAddon() != null) { GetCorePrefabAddon().InitializeSliders(this); }

        SetPopupTextAddon(coreObject.GetComponent<PopupTextAddon>());

        if (GetPopupTextAddon() == null)
        {
            SetPopupTextAddon(coreObject.GetComponentInChildren<PopupTextAddon>());
            if (debug.IsDebugging && GetPopupTextAddon() != null)
            {
                Debug.Log($"PopupTextAddon component found in children and set.");
            }
        }

        //SetAnimator(GetCorePrefabAddon().GetAnimator());
        if (debug.IsDebugging && GetAnimator() != null)
        {
            Debug.Log($"Animator set to: {GetAnimator().name}");
        }
        #endregion

        #region // Output error Checking
        if (GetCorePrefabAddon() == null)
        {
            Debug.LogError("Error! Unable to find GetCorePrefabAddon on object or children.");
        }
        if (GetPopupTextAddon() == null)
        {
            Debug.LogError("Error! Unable to find GetPopupTextAddon on object or children.");
        }
        if (GetAnimator() == null)
        {
            //Debug.LogError("Error! Unable to find animator on object or children.");
        }
        #endregion
    }
    #endregion
    #region // Move Unit

    /// <summary>
    /// Snaps the CoreUnit into its designated slot on the current floor.
    /// Ensures precise positioning and updates the CoreUnit's state to Idle.
    /// </summary>
    public void SnapCoreIntoSlot()
    {
        if (debug.IsDebugging) Debug.Log("Starting SnapCoreIntoSlot function.");

        #region // Retrieve Core Slot Addon
        // Check if the SlotsAddon component is valid
        var slotsAddon = GetSlotsAddon();
        if (slotsAddon == null)
        {
            Debug.LogError("Error!!: Invalid Core Slot. The SlotsAddon component is null.");
            return;
        }

        if (debug.IsDebugging)
            Debug.Log("Successfully retrieved SlotsAddon.");
        #endregion

        #region // Assign Core to Slot
        // Assign this CoreUnit to the slot within the addon
        slotsAddon.SetCoreSlot(this);
        if (debug.IsDebugging)
            Debug.Log($"Core '{name}' is being assigned to its slot.");
        #endregion

        #region // Retrieve Physical Slot GameObject
        // Get the physical GameObject representing the slot
        GameObject slotObject = slotsAddon.GetCoreObject();
        if (slotObject == null)
        {
            Debug.LogError($"Error!!: Slot object for core '{name}' is null. Cannot move slot.");
            return;
        }

        if (debug.IsDebugging)
            Debug.Log($"Slot object retrieved successfully for core '{name}'. Slot name: {slotObject.name}");
        #endregion

        #region // Update Core State to Idle
        // Set the core state to Idle
        SetState(CoreStates.Idle);
        if (debug.IsDebugging)
            Debug.Log($"Core '{name}' state set to Idle.");
        #endregion

        #region // Snap Core to Slot
        // Target slot GameObject and this CoreUnit's GameObject
        GameObject targetObject = slotObject;
        GameObject core = this.gameObject;

        if (targetObject == null)
        {
            Debug.LogError($"Target object is null for core '{name}'. Aborting SnapCoreIntoSlot.");
            return; // Exit early to avoid further processing
        }

        // Debug logs for initial and target positioning
        if (debug.IsDebugging)
        {
            Debug.Log($"Snapping core '{name}' to target slot '{targetObject.name}'.");
            Debug.Log($"Target position: {targetObject.transform.position}, Target rotation: {targetObject.transform.rotation.eulerAngles}");
            Debug.Log($"Core initial position: {core.transform.position}, Core initial rotation: {core.transform.rotation.eulerAngles}");
        }

        // Snap the CoreUnit to the slot's exact position and rotation
        core.transform.position = targetObject.transform.position;
        core.transform.rotation = targetObject.transform.rotation;

        // Debug logs after snapping
        if (debug.IsDebugging)
        {
            Debug.Log($"Core '{name}' successfully snapped to target slot '{targetObject.name}'.");
            Debug.Log($"New core position: {core.transform.position}, New core rotation: {core.transform.rotation.eulerAngles}");
        }

        // Update the positioning object reference
        positioningObject = targetObject;
        #endregion

        if (debug.IsDebugging)
            Debug.Log("SnapCoreIntoSlot function completed successfully.");
    }

    #endregion
    #region // Update Unit Info
    private void UpdateFloorInformation()
    {
        // Update floor information
        int currentFloor = GetCurrentFloor();
        if (debug.IsDebugging) Debug.Log($"Updating floor {currentFloor} information.");

        // Get the Tower Floor Object
        GameObject floorObject = GetTowerManager().GetFloor(currentFloor);
        if (debug.IsDebugging) Debug.Log(floorObject != null ? "Successfully retrieved floor object." : "Error: Floor object is null!");

        // Get the Tower section addon of that floor
        TowerSectionAddon floorAddon = floorObject != null ? GetTowerManager().GetTowerSectionAddon(floorObject) : null;
        if (debug.IsDebugging) Debug.Log(floorAddon != null ? "Successfully retrieved Tower Section Addon." : "Error: Tower Section Addon is null!");

        // Get the Core Slots addon of that tower Section
        CoreSlotAddon slotsAddon = floorAddon != null ? floorAddon.GetCoreSlot() : null;
        if (debug.IsDebugging) Debug.Log(slotsAddon != null ? "Successfully retrieved Core Slots Addon." : "Error: Core Slots Addon is null!");

        // Update all information
        SetFloorObject(floorObject);
        SetFloorAddon(floorAddon);
        SetSlotsAddon(slotsAddon);
        if (GetSlotsAddon() != null)
        {
            SnapCoreIntoSlot();
            if (debug.IsDebugging) Debug.Log("Successfully snapped core into slot.");
        }
        else
        {
            if (debug.IsDebugging) Debug.Log("Error: Slots Addon is null when attempting to snap core into slot!");
        }

        if (debug.IsDebugging) Debug.Log("UpdateFloorInformation completed.");
    }

    private void UpdateCoreUiPosition()
    {
        ////if (GetHeroes() == null || GetHeroes().Count == 0) return;
        if (GetCoreUi() == null)
            return;

        // Update the HeroTeam's position
        GetCoreUi().transform.position = gameObject.transform.position;

    }
    private void UpdateCoreUiAddon()
    {
        CoreUnitUiAddon uiAddon = GetCoreUiAddon();
        if (uiAddon == null) return;
        uiAddon.UpdateCoreUi();
        // Update the lock Text
        //TextMeshProUGUI lockText = uiAddon.GetLockText();
        //if (lockText != null) lockText.text = GetCoreInfo().GetLockTime().ToString();
        //// Update the Health Text
        //TextMeshProUGUI healthText = uiAddon.GetHealthText();
        //if (healthText != null) healthText.text = GetCoreInfo().GetHealth().ToString();
        //uiAddon.SetUiState(newUiState);
    }
    #endregion
    #region // Unit Actions

    /// <summary>
    /// Executes the start of a round based on the CoreUnit's type.
    /// Logs the core type for debugging and transitions to the corresponding round start coroutine.
    /// </summary>
    /// <returns>An IEnumerator for use in Unity coroutines.</returns>
    public IEnumerator ExecuteRoundStart()
    {
        if (debug.IsDebugging)
            Debug.Log($"Core Type: {GetCoreType()}");

        UpdateCoreUiAddon();

        switch (GetCoreType())
        {
            case CoreTypes.Basic:
                // Initiates the basic round start logic
                yield return BasicRoundStart();
                break;

            case CoreTypes.Heart:
                // Initiates the Heart-specific round start logic
                yield return HeartRoundStart();
                break;
            case CoreTypes.Boss:
                // Initiates the Heart-specific round start logic
                yield return BossRoundStart();
                break;

            default:
                // No action for unknown core types
                break;
        }

        // Brief pause before transitioning to the relaxing state
        yield return new WaitForSeconds(0.5f);
    }
    /// <summary>
    /// Executes the start of a round based on the CoreUnit's type.
    /// Logs the core type for debugging and transitions to the corresponding round start coroutine.
    /// </summary>
    /// <returns>An IEnumerator for use in Unity coroutines.</returns>
    public IEnumerator ExecuteRoundAction()
    {
        if (debug.IsDebugging)
            Debug.Log($"Core Type: {GetCoreType()}");

        UpdateCoreUiAddon();

        // Brief pause before transitioning to the relaxing state
        yield return new WaitForSeconds(0.1f);
    }
    public IEnumerator ExecuteRoundEnd()
    {
        if (debug.IsDebugging)
            Debug.Log($"Core Type: {GetCoreType()}");

        UpdateCoreUiAddon();

        // Brief pause before transitioning to the relaxing state
        yield return new WaitForSeconds(0.1f);
    }
    public IEnumerator ExecuteBeforeRound() 
    {
        CheckForLockedHeroTeam();
        UpdateCoreUiAddon();
        yield return new WaitForSeconds(0.1f);
    }
    public IEnumerator ExecuteStartToAction()
    {
        CheckForLockedHeroTeam();
        UpdateCoreUiAddon();
        yield return new WaitForSeconds(0.1f);
    }
    public IEnumerator ExecuteActionToEnd()
    {
        CheckForLockedHeroTeam();
        UpdateCoreUiAddon();
        yield return new WaitForSeconds(0.1f);
    }
    public IEnumerator ExecuteAfterRound()
    {
        if (debug.IsDebugging) Debug.Log("Running After Round for Core");
        
        GetCoreInfo().DecrementLockTime();
        CheckForLockedHeroTeam();
        UpdateCoreUiAddon();
        // Brief pause before transitioning to the relaxing state
        yield return new WaitForSeconds(0.1f);
    }


    




    /// <summary>
    /// Handles the basic round start logic for the CoreUnit.
    /// Currently, this method performs no specific actions.
    /// </summary>
    /// <returns>An empty IEnumerator for coroutine compatibility.</returns>
    public IEnumerator BasicRoundStart()
    {
        yield return null; // Placeholder logic
    }

    /// <summary>
    /// Handles the Heart core's round start logic.
    /// Processes monster and hero teams on the associated floor and applies any necessary modifications.
    /// </summary>
    /// <returns>An IEnumerator for use in Unity coroutines.</returns>
    public IEnumerator HeartRoundStart()
    {
        TowerSectionAddon floorAddon = GetFloorAddon();
        MonsterSlotsAddon monsterSlots = floorAddon.GetMonsterSlots();
        HeroSlotsAddon heroSlots = floorAddon.GetHeroSlots();
        // Retrieve the monster and hero teams from the floor's add-ons
        MonsterTeam monsters = monsterSlots.GetMonsterTeam();
        HeroTeam heroes = heroSlots.GetHeroTeam();

        if (monsters != null && monsters.GetMonsters().Count > 0)
        {
            if (debug.IsDebugging)
            {
                Debug.Log($"Monsters count: {monsters.GetMonsters().Count}");
                Debug.Log($"Heroes count: {heroes.GetHeroes().Count}");
            }

            // Future enhancement: Add a buff to double the health of each unit in the monster team
            // Example: ApplyBuff(monsters, BuffType.DoubleHealth);
        }

        yield return null; // Return after processing
    }
    public IEnumerator BossRoundStart()
    {

        yield return null; // Return after processing
    }

    public void StartCoreLockTimer()
    {
        if (GetCoreInfo() == null) { Debug.LogError("Some How this core unit does not have a set Core Info"); return; }
        // Reset the lock timer
        GetCoreInfo().ResetLockTime();

    }
    public void CheckForLockedHeroTeam()
    {
        // If lock timer if off, skip this function
        CoreInfo coreInfo = GetCoreInfo();
        if (coreInfo.GetLockTime() <= 0) return;
        // If lock timer is on, make sure there is a hero team on this floor
        TowerSectionAddon floorAddon = GetFloorAddon();
        if (floorAddon == null) { Debug.LogWarning($"Warning. No Floor addon connected to core {GetCuID()}"); return; }
        HeroSlotsAddon heroAddon = floorAddon.GetHeroSlots();
        if (heroAddon == null) { Debug.LogWarning($"Warning. No heroAddon connected to core {GetCuID()}"); return; }
        HeroTeam heroTeam = heroAddon.GetHeroTeam();
        // If there is no hero team, set lock timer to 0
        if (heroTeam == null) coreInfo.SetLockTime(0);
    }

    #endregion
    #region // Unit Damage
    /// <summary>
    /// Applies damage to the unit, reducing its health.
    /// Logs relevant debug information for tracking damage application.
    /// </summary>
    /// <param name="damage">The amount of damage to apply.</param>
    public void TakeDamage(int damage)
    {
        GetCoreInfo().TakeDamage(damage);
    }
    #endregion
    #region // Unit Death

    /// <summary>
    /// Checks if the CoreUnit's health is zero or below and initiates the dying process if necessary.
    /// </summary>
    private void CheckForUnitDeath()
    {
        CoreInfo info = GetCoreInfo();

        if (info.GetHealth() <= 0)
        {
            StartDying();
        }
    }

    /// <summary>
    /// Updates the CoreUnit's state when it is dying.
    /// Handles the dying timer and ensures death execution when the timer reaches zero.
    /// </summary>
    private void UpdateForUnitDying()
    {
        if (!GetIsDying())
            return;

        if (GetDyingTimer() > 0)
        {
            if (debug.IsDebugging)
                Debug.Log($"Core '{name}' dying timer: {GetDyingTimer()}");

            // Start coroutines if not already started
            if (!GetDyingCoroutinesStarted())
            {
                StartCoroutine(ShakeGameObject());
                StartCoroutine(FlashRed());
                SetDyingCoroutinesStarted(true);
            }

            SetDyingTimer(GetDyingTimer() - Time.deltaTime);
        }
        else
        {
            SetDyingTimer(0);
            ExecuteDeath();
        }
    }

    /// <summary>
    /// Coroutine to shake the GameObject to simulate a dying effect.
    /// Restores the original position after the dying state ends.
    /// </summary>
    /// <returns>Yields control for each frame during the shake effect.</returns>
    private IEnumerator ShakeGameObject()
    {
        Vector3 originalPosition = transform.position;
        float shakeMagnitude = 0.1f;

        while (GetIsDying())
        {
            transform.position = originalPosition + (Vector3)Random.insideUnitCircle * shakeMagnitude;
            yield return null; // Wait for the next frame
        }

        transform.position = originalPosition; // Restore the original position
    }

    /// <summary>
    /// Coroutine to flash the GameObject red to indicate a dying state.
    /// Ensures the original color is restored after the dying state ends.
    /// </summary>
    /// <returns>Yields control for timed color transitions.</returns>
    private IEnumerator FlashRed()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer == null)
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>();

            if (childRenderers.Length > 0)
            {
                renderer = childRenderers[0];
            }
            else
            {
                Debug.LogWarning($"No Renderer found on {name} or its children. Aborting FlashRed.");
                yield break;
            }
        }

        Color originalColor = renderer.material.color;
        Color flashColor = Color.red;
        float flashDuration = 0.1f;

        while (GetIsDying())
        {
            renderer.material.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            renderer.material.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        renderer.material.color = originalColor; // Ensure the color is reset
    }

    /// <summary>
    /// Initiates the timed! dying process for the CoreUnit.
    /// Disables health-related UI, starts animations, and sets up the death timer.
    /// </summary>
    private void StartDying()
    {
        if (GetIsDying())
        {
            if (debug.IsDebugging)
                Debug.LogWarning("Already dying, exiting StartDying.");
            return;
        }

        SetIsDying(true);
        SetDyingCoroutinesStarted(false);
        SetDyingTimer(deathTimerInit);

        if (debug.IsDebugging)
            Debug.Log("Starting the dying process.");

        GetCorePrefabAddon().GetHealthBarSlider().gameObject.SetActive(false);

        SetState(CoreStates.Dying);
        //GetFloorAddon().SetIsDying(true);
    }

    /// <summary>
    /// Executes the death logic for the CoreUnit.
    /// Handles VFX, model destruction, and clearing the unit's slot on the floor.
    /// </summary>
    public void ExecuteDeath()
    {
        // Modify the position to be 2 units down from the current position
        Vector3 deathLocation = transform.position - new Vector3(0, 2f, 0);
        SpawnDeathVFX(deathLocation);

        Destroy(GetModel());

        DestroyCoreUi();

        CoreSlotAddon coreSlotAddon = GetSlotsAddon();
        if (coreSlotAddon != null)
        {
            coreSlotAddon.ClearCoreSlot();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Spawns the death visual effects (VFX) at the specified location.
    /// </summary>
    /// <param name="location">The world position to spawn the death VFX.</param>
    private void SpawnDeathVFX(Vector3 location)
    {
        if (DeathVfx == null) return;

        Quaternion rotation = Quaternion.Euler(-90f, 0f, 180f);
        Instantiate(DeathVfx, location, rotation);
    }
    private void DestroyCoreUi()
    {
        if (GetCoreUi() == null) return;

        Destroy(GetCoreUi());
    }

    #endregion
    #region // Animations

    #endregion
    #region // Setters & Getters

    /// <summary>
    /// Gets whether the core is currently in the dying state.
    /// </summary>
    /// <returns>True if the core is dying, otherwise false.</returns>
    public bool GetIsDying() => isDying;

    /// <summary>
    /// Sets whether the core is in the dying state.
    /// </summary>
    /// <param name="newIsDying">New dying state to set.</param>
    public void SetIsDying(bool newIsDying) => isDying = newIsDying;

    /// <summary>
    /// Sets whether the core is locking onto a target.
    /// </summary>
    /// <param name="newLocking">New locking state to set.</param>
    public void SetisLocking(bool newLocking) => isLocking = newLocking;

    /// <summary>
    /// Gets whether the core is locking onto a target.
    /// </summary>
    /// <returns>True if the core is locking, otherwise false.</returns>
    public bool GetisLocking() => isLocking;

    /// <summary>
    /// Sets whether dying coroutines have started.
    /// </summary>
    /// <param name="newDyingCoroutinesStarted">New coroutine state to set.</param>
    public void SetDyingCoroutinesStarted(bool newDyingCoroutinesStarted) => dyingCoroutinesStarted = newDyingCoroutinesStarted;

    /// <summary>
    /// Gets whether dying coroutines have started.
    /// </summary>
    /// <returns>True if coroutines have started, otherwise false.</returns>
    public bool GetDyingCoroutinesStarted() => dyingCoroutinesStarted;

    /// <summary>
    /// Sets the core information object.
    /// </summary>
    /// <param name="newInfo">The new CoreInfo to set.</param>
    public void SetCoreInfo(CoreInfo newInfo) => coreInfo = newInfo;

    /// <summary>
    /// Gets the core information object.
    /// </summary>
    /// <returns>The current CoreInfo.</returns>
    public CoreInfo GetCoreInfo() => coreInfo;

    /// <summary>
    /// Sets the animator component for the core.
    /// </summary>
    /// <param name="newAnimator">The new Animator to set.</param>
    public void SetAnimator(Animator newAnimator) { animator = newAnimator; }

    /// <summary>
    /// Gets the animator component of the core.
    /// </summary>
    /// <returns>The current Animator.</returns>
    public Animator GetAnimator() { return animator; }

    /// <summary>
    /// Sets the popup text addon for the core.
    /// </summary>
    /// <param name="newPopupTextAddon">The new PopupTextAddon to set.</param>
    public void SetPopupTextAddon(PopupTextAddon newPopupTextAddon) { popupTextAddon = newPopupTextAddon; }

    /// <summary>
    /// Gets the popup text addon for the core.
    /// </summary>
    /// <returns>The current PopupTextAddon.</returns>
    public PopupTextAddon GetPopupTextAddon() { return popupTextAddon; }

    /// <summary>
    /// Sets the core prefab addon.
    /// </summary>
    /// <param name="newCoreAddon">The new CoreUnitPrefabAddon to set.</param>
    public void SetCorePrefabAddon(CoreUnitPrefabAddon newCoreAddon) { coreAddon = newCoreAddon; }

    /// <summary>
    /// Gets the core prefab addon.
    /// </summary>
    /// <returns>The current CoreUnitPrefabAddon.</returns>
    public CoreUnitPrefabAddon GetCorePrefabAddon() { return coreAddon; }

    /// <summary>
    /// Sets the model prefab for the core.
    /// </summary>
    /// <param name="newModel">The new model GameObject to set.</param>
    public void SetModel(GameObject newModel) { modelPrefab = newModel; }

    /// <summary>
    /// Gets the model prefab for the core.
    /// </summary>
    /// <returns>The current model GameObject.</returns>
    public GameObject GetModel() { return modelPrefab; }

    /// <summary>
    /// Gets the core type from the CoreInfo object.
    /// </summary>
    /// <returns>The CoreType of the core.</returns>
    public CoreTypes GetCoreType() => GetCoreInfo().GetCoreType();

    /// <summary>
    /// Sets the floor object for the core.
    /// </summary>
    /// <param name="newFloorObject">The new floor GameObject to set.</param>
    private void SetFloorObject(GameObject newFloorObject) => floorObject = newFloorObject;

    /// <summary>
    /// Gets the floor object for the core.
    /// </summary>
    /// <returns>The current floor GameObject.</returns>
    private GameObject GetFloorObject() => floorObject;

    /// <summary>
    /// Sets the floor addon for the core.
    /// </summary>
    /// <param name="newFloorAddon">The new TowerSectionAddon to set.</param>
    private void SetFloorAddon(TowerSectionAddon newFloorAddon) => floorAddon = newFloorAddon;

    /// <summary>
    /// Gets the floor addon for the core.
    /// </summary>
    /// <returns>The current TowerSectionAddon.</returns>
    private TowerSectionAddon GetFloorAddon() => floorAddon;

    /// <summary>
    /// Sets the slot addon for the core.
    /// </summary>
    /// <param name="newSlotsAddon">The new CoreSlotAddon to set.</param>
    private void SetSlotsAddon(CoreSlotAddon newSlotsAddon) => slotAddon = newSlotsAddon;

    /// <summary>
    /// Gets the slot addon for the core.
    /// </summary>
    /// <returns>The current CoreSlotAddon.</returns>
    private CoreSlotAddon GetSlotsAddon() => slotAddon;

    /// <summary>
    /// Sets whether the core is initialized.
    /// </summary>
    /// <param name="newInitialized">The new initialized state to set.</param>
    private void SetIsInitalized(bool newInitialized) { isInitalized = newInitialized; }

    /// <summary>
    /// Gets whether the core is initialized.
    /// </summary>
    /// <returns>True if initialized, otherwise false.</returns>
    public bool GetIsInitalized() { return isInitalized; }

    /// <summary>
    /// Sets the state of the core.
    /// </summary>
    /// <param name="newState">The new CoreState to set.</param>
    private void SetState(CoreStates newState) { coreState = newState; }

    /// <summary>
    /// Gets the state of the core.
    /// </summary>
    /// <returns>The current CoreState.</returns>
    public CoreStates GetState() { return coreState; }

    /// <summary>
    /// Gets the unique ID of the core.
    /// </summary>
    /// <returns>The core's unique ID.</returns>
    public int GetCuID() => cuID;

    /// <summary>
    /// Sets the unique ID of the core.
    /// </summary>
    /// <param name="newCuID">The new unique ID to set.</param>
    public void SetCuID(int newCuID) => cuID = newCuID;
    private void SetCoreUi(GameObject newCoreUi) => coreUi = newCoreUi;
    private GameObject GetCoreUi() => coreUi;
    private void SetCoreUiAddon(CoreUnitUiAddon newCoreUiAddon) => coreUiAddon = newCoreUiAddon;
    private CoreUnitUiAddon GetCoreUiAddon() => coreUiAddon;
    public float GetDyingTimerInit() => dyingTimerInit;
    public void SetDyingTimerInit(float newDyingTimerInit) => dyingTimerInit = newDyingTimerInit;

    /// <summary>
    /// Sets the current floor of the core and updates floor information.
    /// </summary>
    /// <param name="newCurrentFloor">The new floor number to set.</param>
    public void SetCurrentFloor(int newCurrentFloor) { currentFloor = newCurrentFloor; UpdateFloorInformation(); }

    /// <summary>
    /// Gets the current floor of the core.
    /// </summary>
    /// <returns>The current floor number.</returns>
    public int GetCurrentFloor() { return currentFloor; }

    /// <summary>
    /// Gets the current death timer of the core.
    /// </summary>
    /// <returns>The current death timer value.</returns>
    public float GetDyingTimer() => deathTimer;

    /// <summary>
    /// Sets the death timer for the core.
    /// </summary>
    /// <param name="newDeathTimer">The new death timer value to set.</param>
    public void SetDyingTimer(float newDeathTimer) => deathTimer = newDeathTimer;

    /// <summary>
    /// Sets the game manager reference.
    /// </summary>
    /// <param name="newManager">The new TowerDefenseManager to set.</param>
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;

    /// <summary>
    /// Gets the game manager reference.
    /// </summary>
    /// <returns>The current TowerDefenseManager.</returns>
    public TowerDefenseManager GetGameManager() => GameManager;

    /// <summary>
    /// Gets the tower manager from the game manager.
    /// </summary>
    /// <returns>The current TowerManager.</returns>
    public TowerManager GetTowerManager() { return GetGameManager().GetTowerManager(); }

    /// <summary>
    /// Gets the hero manager from the game manager.
    /// </summary>
    /// <returns>The current HeroManager.</returns>
    public HeroManager GetHeroManager() { return GetGameManager().GetHeroManager(); }

    /// <summary>
    /// Gets the monster manager from the game manager.
    /// </summary>
    /// <returns>The current MonsterManager.</returns>
    public MonsterManager GetMonsterManager() { return GetGameManager().GetMonsterManager(); }

    /// <summary>
    /// Gets the core manager from the game manager.
    /// </summary>
    /// <returns>The current CoreManager.</returns>
    public CoreManager GetCoreManager() { return GetGameManager().GetCoreManager(); }
    private void SetisInitialized(bool newIsInitialized) => isInitialized = newIsInitialized;
    public bool GetisInitialized() => isInitialized;
    #endregion

    #endregion
    #endregion =====================
}
[System.Serializable]
public class CoreInfo
{
    #region // Variables
    [SerializeField] private float health;
    [SerializeField] private int lockTime;
    [SerializeField] private int teamSize;

    [SerializeField] private float maxHealth;
    [SerializeField] private int initialLockTime;

    [SerializeField] private CoreTypes coreType;
    #endregion
    #region // Methods
    public CoreInfo(CoreTypes type)
    {
        InitializeCoreInfo(type);
    }
    private void InitializeCoreInfo(CoreTypes newType)
    {
        SetCoreType(newType);
        switch (GetCoreType())
        {
            case CoreTypes.Basic:
                SetMaxHealth(1);
                SetInitialLockTime(2);
                SetTeamSize(0);
                break;
            case CoreTypes.Heart:
                SetMaxHealth(1);
                SetInitialLockTime(3);
                SetTeamSize(3);
                break;
            case CoreTypes.Boss:
                SetMaxHealth(1);
                SetInitialLockTime(99);
                SetTeamSize(0);
                break;
            default: SetMaxHealth(1); break;
        }
        // Reset health
        SetHealth(GetMaxHealth());
        SetLockTime(0);
    }
    
    public void TakeDamage(int damage) { SetHealth(health - damage); }
    private void SetHealth(float newHealth) => health = Mathf.Clamp(newHealth, 0, GetMaxHealth());
    public float GetHealth() => health;
    private void SetTeamSize(int newTeamSize) => teamSize = Mathf.Clamp(newTeamSize, 0, 4);
    public int GetTeamSize() => teamSize;
    private void SetMaxHealth(float newMaxHealth) => maxHealth = newMaxHealth;
    public void SetLockTime(int newLockTime) => lockTime = newLockTime;
    public void DecrementLockTime(int valueToTakeAway = 1) => SetLockTime(Mathf.Max(0, lockTime - valueToTakeAway));
    public void ResetLockTime() => SetLockTime(GetInitialLockTime());
    private void SetInitialLockTime(int newInitialLockTime) => initialLockTime = newInitialLockTime;
    public float GetMaxHealth() => maxHealth;
    public int GetInitialLockTime() => initialLockTime;
    public int GetLockTime() => lockTime;
    private void SetCoreType(CoreTypes newType) => coreType = newType;
    public CoreTypes GetCoreType() => coreType;
    

    #endregion
}