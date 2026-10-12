using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TowerSectionAddon : MonoBehaviour
{
    #region // Variables
    public enum TowerSectionTypes
    {
        Base,
        Floor,
        Top
    }

    [Tooltip("The type of tower section (Base, Floor, or Top).")]
    public TowerSectionTypes Type;

    [Tooltip("The object that marks the location on top of the object that connects to the tower.")]
    public GameObject TopConnection;

    [Tooltip("The object that marks the location on the bottom of the object that connects to the tower.")]
    public GameObject BottomConnection;

    [Tooltip("List of GameObjects that define the path for this tower section.")]
    public List<GameObject> Path = new List<GameObject>();

    [Tooltip("List of GameObjects that define the deathpoints for this tower section.")]
    public List<GameObject> DeathPoints = new List<GameObject>();

    [Tooltip("List of GameObjects that define where the hero UI should be placed for this tower section.")]
    public List<GameObject> HeroUiPoints = new List<GameObject>();

    [Tooltip("GameObject that defines the jump point for this tower section.")]
    public GameObject JumpPoint;

    [Tooltip("GameObject containing the floor's inner camera information.")]
    [SerializeField] private GameObject InnerCameraHolder;

    [Tooltip("GameObject containing the floor's outer camera information.")]
    [SerializeField] private GameObject OuterCameraHolder;

    [Tooltip("GameObject containing the floor's monster camera information.")]
    [SerializeField] private GameObject MonsterCameraHolder;

    [Tooltip("GameObject containing the floor's monster camera information.")]
    [SerializeField] private GameObject HeroCameraHolder;
    
    [Tooltip("GameObject containing the floor's monster camera information.")]
    [SerializeField] private GameObject AllCharactersCameraHolder;

    [Tooltip("GameObject containing all the room pieces for this floor.")]
    [SerializeField] private GameObject RoomPart;
    
    [Tooltip("GameObject containing all the room pieces for when this floor dies.")]
    [SerializeField] private GameObject DyingRoomPart;

    [Tooltip(" ")]
    [SerializeField] private GameObject FloorSelectionGlow;

    [Tooltip(" ")]
    public List<GameObject> MonsterSelectionGlowList = new List<GameObject>();
    [Tooltip(" ")]
    public List<GameObject> HeroSelectionGlowList = new List<GameObject>();

    [Tooltip("Red transparent material used during the flash effect.")]
    [SerializeField] private Material RedFlashMaterial;

    [Tooltip("Transparent material used to see through the floor during placement.")]
    [SerializeField] private Material SeeThroughMaterial;

    [Tooltip("Duration of each flash effect when the floor takes damage.")]
    [SerializeField] private float flashDuration = 0.1f;

    [Tooltip("Number of times the flash effect occurs when the floor takes damage.")]
    [SerializeField] private int flashCount = 5;

    [Tooltip("Original materials for different parts of the tower, stored before applying visual effects.")]
    private Material[][] originalMaterials;

    [Tooltip("Indicates whether this tower section is currently destroyed.")]
    public bool IsDestroyed = false;

    [Tooltip("Indicates whether this tower section is in the process of being destroyed.")]
    public bool IsDying = false;

    [SerializeField] private bool isFloorSelected;
    [SerializeField] private int monsterSelectionNumber;
    [SerializeField] private int heroSelectionNumber;



    [Tooltip("Prefab for the smoke visual effect when the tower section is damaged.")]
    public GameObject smokeVFXPrefab;

    [Tooltip("Prefab for the explosion visual effect when the tower section is destroyed.")]
    public GameObject explosionVFXPrefab;

    [Tooltip("Number representing the level of the tower this floor is on. 0 represents the base, 1 is the first floor, etc.")]
    [SerializeField] private int floorNumber; // 0 Represents the base of the tower, 1 being the first floor 

    [Tooltip("Indicates whether this floor has been traversed during this turn.")]
    [SerializeField] private bool traversedThisTurn;

    [Tooltip("Reference to the TowerDefenseManager, which handles tower logic and game progression.")]
    [SerializeField] private TowerDefenseManager GameManager; // Link to Tower Manager

    [Tooltip("Reference to the TowerManager, used for handling overall tower behavior.")]
    private TowerManager towerManager;

    [Tooltip("Reference to the MonsterManager, used for managing monsters within this floor.")]
    private MonsterManager monsterManager;

    [Tooltip("Reference to the HeroSlotsAddon, which manages hero placement on this floor.")]
    [SerializeField] private HeroSlotsAddon heroSlotsAddon;

    [Tooltip("Reference to the MonsterSlotsAddon, which manages monster placement on this floor.")]
    [SerializeField] private MonsterSlotsAddon monsterSlotsAddon;

    [Tooltip("Reference to the CoreSlotAddon, which manages core-related functionality for this floor.")]
    [SerializeField] private CoreSlotAddon coreSlotAddon;

    [Tooltip("List of non-combat hero teams currently occupying this floor.")]
    [SerializeField] private List<HeroTeam> NonCombatHeroTeamsOnFloor;

    [SerializeField] bool isTempFloor;

    [SerializeField] private Coroutine flashDyingCoroutine;
    [SerializeField] private Coroutine smokeCoroutine;
    

    [Header("Smoke VFX Settings")]
    [SerializeField] public float dyingSmokeTimerMin = 0.01f;
    [SerializeField] public float dyingSmokeTimerMax = 0.05f;
    [SerializeField] private int roomWidth = 11;
    [SerializeField] private int roomHeight = 5;
    [SerializeField] private int roomDepth = 5;
    [SerializeField] public float smokeDurationMin = 2f;
    [SerializeField] public float smokeDurationMax = 5f;
    [SerializeField] private int smokeAmountMin = 1; // Minimum smoke spawned at once
    [SerializeField] private int smokeAmountMax = 5; // Maximum smoke spawned at once
    [SerializeField] private Vector3 smokeSpawnOffset = new Vector3(0,5,0); // Adjustable XYZ offset


    [Header("Dying Part Flash Settings")]
    [SerializeField] public float dyingRoomPartTimerMin = 15.5f;
    [SerializeField] public float dyingRoomPartTimerMax = 45.5f;
    [SerializeField] private bool dyingRoomPartActive = false;


    [SerializeField] private TextMeshProUGUI useDecreeNoHeroTargetText;
    [SerializeField] private TextMeshProUGUI useDecreeNoMonsterTargetText;
    [SerializeField] private TextMeshProUGUI useDecreeNoCharTargetText;




    #region // Debugging
    [Header("Debugging")]

    [Tooltip("Debugging utility for logging and testing tower behavior.")]
    [SerializeField] private DebugUtility debug;
    #endregion
    #endregion

    #region // Functions

    #region // Unity Functions
    // Start is called before the first frame update
    void Start()
    {

    }

    private void Update()
    {
        CheckForDying();
        UpdateDying();
        CheckForDestruction();
        
    }
    #endregion

    #region // User Functions
    #region // Initialize
    /// <summary>
    /// Initializes the tower section, setting up initial state, cameras, hero slots, monster slots, and core slots.
    /// </summary>
    /// <param name="gameManager">Reference to the TowerDefenseManager handling the game logic.</param>
    public void InitializeSection(TowerDefenseManager gameManager, CoreUnit.CoreTypes coreType = CoreUnit.CoreTypes.Basic)
    {
        #region // Setup Initial State of the Tower Section
        InitalizeDebug();
        SetGameManager(gameManager); // Link the game manager
        SetIsDestroyed(false);
        SetIsDying(false);
        SetOriginalMaterials();
        SetIsTempFloor(false);
        SetIsFloorSelected(false);
        SetMonsterSelectionNumber(0);
        SetHeroSelectionNumber(0);
        #endregion

        #region // Add cameras to camera managers 
        AddCamerasToManager();
        #endregion

        #region // Setup NON combat hero teams
        // This is a list of hero teams waiting to move from the start of the floor
        // Or up to the next floor
        if (Type == TowerSectionTypes.Base) 
        { 
            if (debug.IsDebugging) Debug.Log("Base sections do not have hero slots."); 
            return; 
        }
        if (Type == TowerSectionTypes.Top) 
        {
            if (debug.IsDebugging) Debug.Log("Top sections do not have hero slots."); 
            return; 
        }
        InitializeGetNonCombatHeroTeamsOnFloor();
        #endregion

        #region // Setup Hero Slots Addon
        heroSlotsAddon = GetComponent<HeroSlotsAddon>();
        // If you don't find it on this object, search the children
        if (heroSlotsAddon == null) { heroSlotsAddon = GetComponentInChildren<HeroSlotsAddon>(); }
        // If you don't find it on the children, Ourput Error
        if (heroSlotsAddon == null) { Debug.LogError("HeroSlotsAddon could not be found for this tower section."); }
        // If you do have it, Initalize the slots
        else { heroSlotsAddon.InitializeHeroSlots(); }
        #endregion

        #region // Setup Monster Slots Addon & Empty Monster Team
        monsterSlotsAddon = GetComponent<MonsterSlotsAddon>();
        // If you don't find it on this object, search the children
        if (monsterSlotsAddon == null) { monsterSlotsAddon = GetComponentInChildren<MonsterSlotsAddon>(); }
        // If you don't find it on the children, Ourput Error
        if (monsterSlotsAddon == null) { Debug.LogError("MonsterSlotsAddon could not be found for this tower section."); }
        // If you do have it, Initalize the slots
        else { monsterSlotsAddon.InitializeMonsterSlots(); }
        // Spawn an empty Monster Team
        SpawnEmptyMonsterTeam();
        #endregion

        #region // Setup Core Slot Addon & Core
        coreSlotAddon = GetComponent<CoreSlotAddon>();
        // If you don't find it on this object, search the children
        if (coreSlotAddon == null) { coreSlotAddon = GetComponentInChildren<CoreSlotAddon>(); }
        // If you don't find it on the children, Ourput Error
        if (coreSlotAddon == null) { Debug.LogError("coreSlotAddon could not be found for this tower section."); }
        // If you do have it, Initalize the slots
        else { coreSlotAddon.InitializeCoreSlot(); }
        // Spawn the passed core
        SpawnSpecificCore(coreType);
        #endregion

        #region// Update Team size based on core info
        InitializeMonsterSlotTeamSize();
        #endregion
    }
    public void InitializeTemporarySection(TowerDefenseManager gameManager)
    {
        #region // Setup Initial State of the Tower Section
        SetGameManager(gameManager); // Link the game manager
        SetIsDestroyed(false);
        SetIsDying(false);
        SetOriginalMaterials();
        SetIsTempFloor(true);
        #endregion

        #region // Add cameras to camera managers 
        AddCamerasToManager();
        #endregion

        #region // Setup NON combat hero teams
        // This is a list of hero teams waiting to move from the start of the floor
        // Or up to the next floor
        if (Type == TowerSectionTypes.Base) { Debug.LogWarning("Base sections do not have hero slots."); return; }
        if (Type == TowerSectionTypes.Top) { Debug.LogWarning("Top sections do not have hero slots."); return; }
        InitializeGetNonCombatHeroTeamsOnFloor();
        #endregion

        #region // Setup Hero Slots Addon
        heroSlotsAddon = GetComponent<HeroSlotsAddon>();
        // If you don't find it on this object, search the children
        if (heroSlotsAddon == null) { heroSlotsAddon = GetComponentInChildren<HeroSlotsAddon>(); }
        // If you don't find it on the children, Ourput Error
        if (heroSlotsAddon == null) { Debug.LogError("HeroSlotsAddon could not be found for this tower section."); }
        // If you do have it, Initalize the slots
        else { }//heroSlotsAddon.InitializeHeroSlots(); }
        #endregion

        #region // Setup Monster Slots Addon & Empty Monster Team
        monsterSlotsAddon = GetComponent<MonsterSlotsAddon>();
        // If you don't find it on this object, search the children
        if (monsterSlotsAddon == null) { monsterSlotsAddon = GetComponentInChildren<MonsterSlotsAddon>(); }
        // If you don't find it on the children, Ourput Error
        if (monsterSlotsAddon == null) { Debug.LogError("MonsterSlotsAddon could not be found for this tower section."); }
        // If you do have it, Initalize the slots
        else { monsterSlotsAddon.InitializeMonsterSlots(); }
        #endregion

        #region // Setup Core Slot Addon & Core
        coreSlotAddon = GetComponent<CoreSlotAddon>();
        // If you don't find it on this object, search the children
        if (coreSlotAddon == null) { coreSlotAddon = GetComponentInChildren<CoreSlotAddon>(); }
        // If you don't find it on the children, Ourput Error
        if (coreSlotAddon == null) { Debug.LogError("coreSlotAddon could not be found for this tower section."); }
        // If you do have it, Initalize the slots
        else { coreSlotAddon.InitializeCoreSlot(); }
        // Spawn the passed core
        #endregion
    }
    private void InitializeMonsterSlotTeamSize()
    {
        #region// Get the Core Team Size Info
        CoreSlotAddon coreSlot = GetCoreSlot();
        CoreUnit coreUnit = coreSlot.GetCoreUnit();
        CoreInfo coreUnitInfo = coreUnit.GetCoreInfo();
        int coreTeamSize = coreUnitInfo.GetTeamSize();
        #endregion
        #region // Update Monster Slot Size info
        MonsterSlotsAddon monsterSlot = GetMonsterSlots();
        monsterSlot.SetTeamSize(coreTeamSize);
        monsterSlot.InitializeSlotTeamSize();
        #endregion
    }
    private void InitalizeDebug() { debug = new DebugUtility(false); }
    #endregion

    #region // Positioning
    /// <summary>
    /// Updates the positions of units (core, monsters, heroes, and non-combat heroes) on the current floor.
    /// </summary>
    public void UpdateUnitsPosition()
    {

        if (Type != TowerSectionTypes.Base)
        {
            // Get the Core
            CoreSlotAddon coreSlotAddon = GetCoreSlot();
            CoreUnit coreUnit = coreSlotAddon.GetCoreUnit();
            if (coreUnit != null) coreUnit.SetCurrentFloor(GetFloorNumber());

            // Get the Monster Team 
            MonsterSlotsAddon monsterSlotsAddon = GetMonsterSlots();
            MonsterTeam monsterTeam = monsterSlotsAddon.GetMonsterTeam();
            if (monsterTeam != null) monsterTeam.SetCurrentFloor(GetFloorNumber());

            // Get the Hero Team
            HeroTeam heroTeam = GetHeroSlots().GetHeroTeam();
            if (heroTeam != null)
            {
                heroTeam.SetCurrentFloor(GetFloorNumber());
                heroTeam.SnapHeroesIntoSlots();
            }

            // Get the NonCombat Hero Teams 
            List<HeroTeam> nonCombatHeroTeams = GetNonCombatHeroTeamsOnFloor();
            foreach (HeroTeam nonCombatHeroTeam in nonCombatHeroTeams)
            {
                nonCombatHeroTeam.SetCurrentFloor(GetFloorNumber());
                nonCombatHeroTeam.SnapTeamToStartPoint();
            }
        }
    }
    public void MoveHeroesToNextFloor()
    {

    }
    #endregion

    #region // Spawn Monsters and Cores
    /// <summary>
    /// Spawns a random monster team on the current floor.
    /// </summary>
    public void SpawnRandomMonsterTeam()
    {
        Debug.Log("Starting SpawnRandomMonsterTeam method");

        GetGameManager().GetMonsterManager().SpawnRandomMonsterTeam(GetFloorNumber());

    }

    /// <summary>
    /// Spawns an empty monster team on the current floor.
    /// </summary>
    public void SpawnEmptyMonsterTeam()
    {
        if (Type == TowerSectionTypes.Base) { return; }
        Debug.Log("Starting SpawnEmptyMonsterTeam method");
        GetGameManager().GetMonsterManager().SpawnEmptyMonsterTeam(GetFloorNumber());
    }

    /// <summary>
    /// Spawns a random core unit on the current floor.
    /// </summary>
    public void SpawnRandomCore()
    {
        Debug.Log("Starting SpawnRandomCore method");

        CoreUnit.CoreTypes coreToSpawn = (CoreUnit.CoreTypes)Random.Range(0, (System.Enum.GetValues(typeof(CoreUnit.CoreTypes)).Length) - 1);
        GetGameManager().GetCoreManager().SpawnCore(GetFloorNumber(), coreToSpawn);

    }

    /// <summary>
    /// Spawns a specific core type on the current floor.
    /// </summary>
    /// <param name="coreTypeToSpawn">The core type to spawn.</param>
    public void SpawnSpecificCore(CoreUnit.CoreTypes coreTypeToSpawn)
    {
        if (Type == TowerSectionTypes.Base) { return; }
        if (debug.IsDebugging) { Debug.Log($"Starting Specific Core: {coreTypeToSpawn}"); }
        GetGameManager().GetCoreManager().SpawnCore(GetFloorNumber(), coreTypeToSpawn);
    }
    #endregion

    #region // Destruction
    /// <summary>
    /// Checks if the section should be destroyed at the end of the round and performs destruction if necessary.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    public IEnumerator CheckForEndOfRoundDestruction()
    {
        if (!GetIsDying())
        {
            if (debug.IsDebugging) Debug.Log("CheckForEndOfRoundDestruction: Section is not destroyed. Exiting...");
            yield break;
        }

        if (debug.IsDebugging) Debug.Log("CheckForEndOfRoundDestruction: Starting destruction process for section.");
        // If is dying start destroying
        SetIsDestroyed(true);

        // Move all heroes to the next floor
        HeroTeam combatHeroTeam = GetHeroSlots()?.GetHeroTeam();
        if (combatHeroTeam != null) combatHeroTeam.GotoNextFloor();
        List<HeroTeam> nonCombatHeroTeams =  GetNonCombatHeroTeamsOnFloor();
        for (int i = nonCombatHeroTeams.Count - 1; i >= 0; i--) { nonCombatHeroTeams[i].GotoNextFloor(); }


        // End-of-Round Destruction Effects (assuming you have explosion VFX prefab)
        if (debug.IsDebugging) Debug.Log("Spawning explosion VFX...");
        //StartCoroutine(SpawnExplosionVFX());

        // Destroy all Monsters on Floor
        if (debug.IsDebugging) Debug.Log("Destroying monsters on floor...");
        DestroyMonstersOnFloor();

        // Destroy all Devices on Floor (if applicable)
        // Debugging for this can be added if uncommented
        // if (debug.IsDebugging) Debug.Log("Destroying devices on floor...");
        // DestroyDevicesOnFloor();

        // Destroy all Cores on Floor
        if (debug.IsDebugging) Debug.Log("Destroying core on floor...");
        DestroyCoreOnFloor();

        RemoveCamerasFromManager();

        // Delay final destruction and section removal
        if (debug.IsDebugging) Debug.Log("Waiting before final destruction...");
        yield return new WaitForSeconds(0.25f);

        // Remove Object from Floor List

        if (debug.IsDebugging) Debug.Log("CheckForEndOfRoundDestruction: Destroying section and removing game object...");
        Destroy(gameObject);

        // Reorder Tower
        if (debug.IsDebugging) Debug.Log($"Reordering tower after section destruction. Floor Number: {floorNumber}");
        GetGameManager().GetTowerManager().RemoveFloorAndReorderTower(GetFloorNumber());

        if (debug.IsDebugging) Debug.Log("CheckForEndOfRoundDestruction: Section destruction process complete.");
    }
    public void CheckForDying()
    {
        // If already dying or already destroyed, don't check any longer
        if (GetIsDying() || GetIsDestroyed()) { return; }

        // Base and Top don't "Die"
        if (Type == TowerSectionTypes.Base ||
            Type == TowerSectionTypes.Top ||
            GetIsTempFloor()) { return; }

        // Pull the core
        CoreUnit core = GetCoreSlot()?.GetCoreUnit();

        // If the core is gone for whatever reason, the floor should be dying
        if (core == null) SetIsDying(true);

        // and if floor is dying, set monster team to dying as well
        if (GetIsDying())
        {
            MonsterTeam monsterTeam = GetMonsterSlots()?.GetMonsterTeam();
            if (monsterTeam != null) { monsterTeam.StartDying(); }
        }

    }
    private void UpdateDying()
    {
        if (!GetIsDying() || GetIsDestroyed()) { return; }

        if (GetSmokeCoroutine() == null)
        {
            SetSmokeCoroutine(StartCoroutine(SpawnSmokeRoutine()));
        }
        
        if (GetFlashDyingCoroutine() == null)
        {
            SetFlashDyingCoroutine(StartCoroutine(FlashDyingPart()));
        }
    }
    /// <summary>
    /// Destroys all monsters on the current floor.
    /// </summary>
    private void DestroyMonstersOnFloor()
    {
        // Get a list of all monsters on this section
        MonsterTeam monsterTeamOnFloor = GetMonsterSlots().GetMonsterTeam();
        if (monsterTeamOnFloor != null) { monsterTeamOnFloor.RemoveTeam(); }

    }
    /// <summary>
    /// Destroys the core unit on the current floor.
    /// </summary>
    private void DestroyCoreOnFloor()
    {
        // Get the core for this section
        CoreUnit coreOnFloor = GetCoreSlot().GetCoreUnit();
        if (coreOnFloor != null) { coreOnFloor.ExecuteDeath(); }

    }
    /// <summary>
    /// Checks if the section should begin destruction effects (smoke, shaking, flashing) before removal.
    /// </summary>
    public void CheckForDestruction()
    {
        if (!GetIsDying()) return;

        // Pre-Destruction Effects (assuming you have pre-fab smoke VFX)
        //StartCoroutine(SpawnSmokeVFX());

        // Shake the tower section itself
        //StartCoroutine(ShakeTowerSection());

        // Flash the RoomPart Red
        //StartCoroutine(FlashRoutine());

    }
    #endregion

    #region // Cameras
    /// <summary>
    /// Removes camera holders from the CameraManager.
    /// </summary>
    public void RemoveCamerasFromManager()
    {
        if (GetInnerCameraHolder() != null) { GetGameManager().GetCameraManager().RemoveCloseCameraHolder(GetInnerCameraHolder()); }
        if (GetOuterCameraHolder() != null) { GetGameManager().GetCameraManager().RemoveFarCameraHolder(GetOuterCameraHolder()); }
        if (GetMonsterCameraHolder() != null) { GetGameManager().GetCameraManager().RemoveMonsterCameraHolder(GetMonsterCameraHolder()); }
        if (GetHeroCameraHolder() != null) { GetGameManager().GetCameraManager().RemoveHeroCameraHolder(GetHeroCameraHolder()); }
        if (GetAllCharactersCameraHolder() != null) { GetGameManager().GetCameraManager().RemoveAllCharactersCameraHolder(GetAllCharactersCameraHolder()); }
    }
    /// <summary>
    /// Adds camera holders to the CameraManager.
    /// </summary>
    private void AddCamerasToManager()
    {
        if (GetInnerCameraHolder() != null) { GetGameManager().GetCameraManager().AddCloseCameraHolder(GetInnerCameraHolder()); }
        if (GetOuterCameraHolder() != null) { GetGameManager().GetCameraManager().AddFarCameraHolder(GetOuterCameraHolder()); }
        if (GetMonsterCameraHolder() != null) { GetGameManager().GetCameraManager().AddMonsterCameraHolder(GetMonsterCameraHolder()); }
        if (GetHeroCameraHolder() != null) { GetGameManager().GetCameraManager().AddHeroCameraHolder(GetHeroCameraHolder()); }
        if (GetAllCharactersCameraHolder() != null) { GetGameManager().GetCameraManager().AddAllCharactersCameraHolder(GetAllCharactersCameraHolder()); }
    }
    /// <summary>
    /// Updates camera numbers based on the floor number.
    /// </summary>
    public void UpdateCameraNumbers()
    {
        if (GetInnerCameraHolderAddon() != null) { GetInnerCameraHolderAddon().SetCameraFloorNumber(GetFloorNumber()); }
        if (GetOuterCameraHolderAddon() != null) { GetOuterCameraHolderAddon().SetCameraFloorNumber(GetFloorNumber()); }
        if (GetMonsterCameraHolderAddon() != null) { GetMonsterCameraHolderAddon().SetCameraFloorNumber(GetFloorNumber()); }
        if (GetHeroCameraHolderAddon() != null) { GetHeroCameraHolderAddon().SetCameraFloorNumber(GetFloorNumber()); }
        if (GetAllCharactersCameraHolderAddon() != null) { GetAllCharactersCameraHolderAddon().SetCameraFloorNumber(GetFloorNumber()); }
    }
    #endregion

    #region // Section Material
    /// <summary>
    /// Sets the material of the given renderers to the specified material.
    /// </summary>
    /// <param name="renderers">Array of renderers to update.</param>
    /// <param name="flashMaterial">Material to apply.</param>
    private void SetMaterial(Renderer[] renderers, Material flashMaterial)
    {
        foreach (Renderer renderer in renderers)
        {
            Material[] newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = flashMaterial;
            }
            renderer.materials = newMaterials;
        }
    }
    /// <summary>
    /// Resets the materials of the given renderers to their original state.
    /// </summary>
    /// <param name="renderers">Array of renderers to update.</param>
    private void ResetMaterials(Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = originalMaterials[i];
        }
    }
    /// <summary>
    /// Stores the original materials of all child renderers in RoomPart.
    /// </summary>
    private void SetOriginalMaterials()
    {
        if (RoomPart == null)
        {
            Debug.LogError("RoomPart is not assigned.");
            return;
        }

        var renderers = RoomPart.GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }

        if (debug.IsDebugging) Debug.Log("Original materials set for RoomPart.");
    }
    /// <summary>
    /// Starts a temporary see-through effect on RoomPart.
    /// </summary>
    public void StartTemporarySeeThrough()
    {
        if (RoomPart == null)
        {
            Debug.LogError("RoomPart is not assigned.");
            return;
        }

        if (originalMaterials == null || originalMaterials.Length == 0)
        {
            Debug.LogError("Original materials have not been initialized.");
            return;
        }

        var renderers = RoomPart.GetComponentsInChildren<Renderer>();

        if (renderers.Length != originalMaterials.Length)
        {
            Debug.LogError("Mismatch between saved original materials and renderers in RoomPart.");
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            var seeThroughMaterials = new Material[originalMaterials[i].Length];

            for (int j = 0; j < seeThroughMaterials.Length; j++)
            {
                // Assign the see throughmateria
                seeThroughMaterials[j] = SeeThroughMaterial;
            }

            renderers[i].materials = seeThroughMaterials;
        }

        Debug.Log("Temporary see-through effect started.");
    }
    /// <summary>
    /// Stops the temporary see-through effect and restores original materials.
    /// </summary>
    public void StopTemporarySeeThrough()
    {
        if (RoomPart == null || originalMaterials == null)
        {
            Debug.LogError("RoomPart or original materials are not properly initialized.");
            return;
        }

        var renderers = RoomPart.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (i < originalMaterials.Length)
            {
                renderers[i].materials = originalMaterials[i];
            }
        }

        originalMaterials = null; // Clear saved materials to avoid reuse
        Debug.Log("Temporary see-through effect stopped.");
    }
    /// <summary>
    /// Coroutine to flash the object by changing its material multiple times.
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        Renderer[] renderers = RoomPart.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < flashCount; i++)
        {
            SetMaterial(renderers, RedFlashMaterial);
            yield return new WaitForSeconds(flashDuration);

            ResetMaterials(renderers);
            yield return new WaitForSeconds(flashDuration);
        }
    }
    #endregion

    #region // VFXs

    
    

    private IEnumerator SpawnSmokeRoutine()
    {
        if (GetSmokeVFXPrefab() == null) yield break;

        while (GetIsDying())
        {
            float spawnRate = Random.Range(GetDyingSmokeTimerMin(), GetDyingSmokeTimerMax());

            int smokeAmount = Random.Range(GetSmokeAmountMin(), GetSmokeAmountMax() + 1); // Random smoke count

            for (int i = 0; i < smokeAmount; i++)
            {
                Vector3 randomPosition = new Vector3(
                    Random.Range(-GetRoomWidth(), GetRoomWidth()),
                    Random.Range(-GetRoomHeight(), GetRoomHeight()),
                    Random.Range(-GetRoomDepth(), GetRoomDepth())
                ) + transform.position + GetSmokeSpawnOffset(); // Apply offset

                GameObject smoke = Instantiate(GetSmokeVFXPrefab(), randomPosition, Quaternion.identity);
                float smokeDuration = Random.Range(GetSmokeDurationMin(), GetSmokeDurationMax());
                Destroy(smoke, smokeDuration);
            }

            yield return new WaitForSeconds(spawnRate);
        }

        SetFlashDyingCoroutine(null); // Reset so it can restart if needed
    }


    private IEnumerator FlashDyingPart()
    {
        if (GetDyingRoomPart() == null) yield break;

        while (GetIsDying())
        {
            //dyingRoomPartActive = !dyingRoomPartActive;
            SetDyingRoomPartActive(!GetDyingRoomPartActive());
            GetDyingRoomPart().SetActive(GetDyingRoomPartActive());

            float flashTime = Random.Range(GetDyingRoomPartTimerMin(), GetDyingRoomPartTimerMax());
            yield return new WaitForSeconds(flashTime);
        }

        GetDyingRoomPart().SetActive(false);
        SetFlashDyingCoroutine(null); // Reset so it can restart if needed
    }
    #endregion

    #region //  Non Combat Teams
    public void AddHeroTeamToNonCombatTeams(HeroTeam heroTeam)
    {
        // Check if the hero team is not already in the list
        if (!GetNonCombatHeroTeamsOnFloor().Contains(heroTeam))
        {
            // Add it only if it's not already there
            GetNonCombatHeroTeamsOnFloor().Add(heroTeam);
        }
    }

    public void RemoveHeroTeamToNonCombatTeams(HeroTeam heroTeam) { GetNonCombatHeroTeamsOnFloor().Remove(heroTeam); }

    private void InitializeGetNonCombatHeroTeamsOnFloor() { NonCombatHeroTeamsOnFloor = new List<HeroTeam>(); }
    
    public List<HeroTeam> GetNonCombatHeroTeamsOnFloor() => NonCombatHeroTeamsOnFloor;
    #endregion 

    #region // Setters & Getters

    #region// Slot Addons
    public HeroSlotsAddon GetHeroSlots()
    {
        if (Type == TowerSectionTypes.Base)
        {
            Debug.LogWarning("Base sections do not have hero slots.");
            return null;
        }

        HeroSlotsAddon heroSlots = GetComponent<HeroSlotsAddon>();
        heroSlots = GetComponentInChildren<HeroSlotsAddon>();

        if (heroSlotsAddon == null)
        {
            Debug.LogError("HeroSlotsAddon is not set or missing.");
            return null;
        }

        return heroSlotsAddon;
    }
    public MonsterSlotsAddon GetMonsterSlots()
    {
        if (Type == TowerSectionTypes.Base)
        {
            Debug.LogWarning("Base sections do not have monster slots.");
            return null;
        }

        if (monsterSlotsAddon == null)
        {
            Debug.LogError("MonsterSlotsAddon is not set or missing.");
            return null;
        }

        return monsterSlotsAddon;
    }
    public CoreSlotAddon GetCoreSlot()
    {
        if (Type == TowerSectionTypes.Base)
        {
            Debug.LogWarning("Base sections do not have monster slots.");
            return null;
        }

        if (coreSlotAddon == null)
        {
            Debug.LogError("coreSlotAddon is not set or missing.");
            return null;
        }

        return coreSlotAddon;
    }
    #endregion

    #region // Positioning Connector
    public GameObject GetTopConnection()
    {
        if (TopConnection == null)
        {
            Debug.LogError("Top Connection is not set.");
            return null;
        }
        return TopConnection;
    }

    public GameObject GetBottomConnection()
    {
        if (BottomConnection == null)
        {
            Debug.LogError("Bottom Connection is not set.");
            return null;
        }
        return BottomConnection;
    }
    #endregion

    #region // Jump Point
    public void SetJumpPoint(GameObject newJumpPoint) => JumpPoint = newJumpPoint;
    public GameObject GetJumpPoint() => JumpPoint;
    #endregion

    #region // IsDestroyed
    public void SetIsDestroyed(bool newDestroyed) => IsDestroyed = newDestroyed;
    public bool GetIsDestroyed() => IsDestroyed;
    #endregion

    #region // Is Dying
    public void SetIsDying(bool newDying) => IsDying = newDying;
    public bool GetIsDying() => IsDying;
    #endregion

    #region// Floor Number
    public void SetFloorNumber(int floorNumber) => this.floorNumber = floorNumber;
    public int GetFloorNumber() => floorNumber;
    #endregion

    #region // Camera Holders
    public GameObject GetInnerCameraHolder() => InnerCameraHolder;
    public GameObject GetOuterCameraHolder() => OuterCameraHolder;
    public GameObject GetMonsterCameraHolder() => MonsterCameraHolder;
    public GameObject GetHeroCameraHolder() => HeroCameraHolder;
    public GameObject GetAllCharactersCameraHolder() => AllCharactersCameraHolder;
    private CameraHolderAddon GetInnerCameraHolderAddon()
    {
        var holder = GetInnerCameraHolder();
        return holder != null ? holder.GetComponent<CameraHolderAddon>() : null;
    }
    private CameraHolderAddon GetOuterCameraHolderAddon()
    {
        var holder = GetOuterCameraHolder();
        return holder != null ? holder.GetComponent<CameraHolderAddon>() : null;
    }
    private CameraHolderAddon GetMonsterCameraHolderAddon()
    {
        var holder = GetMonsterCameraHolder();
        return holder != null ? holder.GetComponent<CameraHolderAddon>() : null;
    }
    private CameraHolderAddon GetHeroCameraHolderAddon()
    {
        var holder = GetHeroCameraHolder();
        return holder != null ? holder.GetComponent<CameraHolderAddon>() : null;
    }
    private CameraHolderAddon GetAllCharactersCameraHolderAddon()
    {
        var holder = GetAllCharactersCameraHolder();
        return holder != null ? holder.GetComponent<CameraHolderAddon>() : null;
    }
    #endregion

    #region // TraversedThisTurn
    public void SetHaveTraveresedThisTurn(bool hasTraveled) => traversedThisTurn = hasTraveled;
    public bool GetHaveTraveresedThisTurn() => traversedThisTurn;
    #endregion

    #region// Game Manager
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    public TowerDefenseManager GetGameManager() => GameManager;
    #endregion

    #region // Path & Deathpoints
    public List<GameObject> GetPath()
    {
        if (Path == null || Path.Count == 0) { Debug.LogError("Path is not set or is empty."); return null; }
        return Path;
    }
    public List<GameObject> GetDeathPoints()
    {
        if (DeathPoints == null || DeathPoints.Count == 0) { Debug.LogError("DeathPoints is not set or is empty."); return null; }
        return DeathPoints;
    }
    #endregion

    #region // Selection Glow
    private void UpdateFloorSelectionGlow()
    {
        if (GetFloorSelectionGlow() == null) return;

        if (GetIsFloorSelected()){
            GetFloorSelectionGlow().SetActive(true);
        } 
        else
        {
            GetFloorSelectionGlow().SetActive(false);
        }
    }
    private void UpdateMonsterSelectionGlows()
    {
        if (GetMonsterSelectionGlowList() == null) return;

        int glowNumber = GetMonsterSelectionNumber();
        //if (glowNumber < 0 || glowNumber > 3) return;

        foreach (GameObject o in GetMonsterSelectionGlowList()) { o.SetActive(false); }

        for (int i = 1; i <= 4; i++)
        {
            // If above 4 activate all glows
            if (GetMonsterSelectionNumber() == i || GetMonsterSelectionNumber() > 4)
            {
                // If the glow number == this glow, light it up
                GetMonsterSelectionGlowList()[i - 1].SetActive(true);
            }
        }
    }
    private void UpdateHeroSelectionGlows()
    {
        if (GetHeroSelectionGlowList() == null) return;

        int glowNumber = GetHeroSelectionNumber();
        //if (glowNumber < 0 || glowNumber > 3) return;

        foreach (GameObject o in GetHeroSelectionGlowList()) { o.SetActive(false); }

        for (int i = 1; i <= 4; i++)
        {
            // If the glow number == this glow, light it up
            if (GetHeroSelectionNumber() == i || GetHeroSelectionNumber() > 4)
            {
                GetHeroSelectionGlowList()[i - 1].SetActive(true);
            }
        }

    }
    #endregion

    public TextMeshProUGUI GetNoHeroText() => useDecreeNoHeroTargetText;
    public TextMeshProUGUI GetNoMonsterText() => useDecreeNoHeroTargetText;
    public TextMeshProUGUI GetNoCharacterText() => useDecreeNoHeroTargetText;
    public void SetNoHeroText(TextMeshProUGUI newText) => useDecreeNoHeroTargetText = newText;
    public void SetNoMonsterText(TextMeshProUGUI newText) => useDecreeNoHeroTargetText = newText;
    public void SetNoCharacterText(TextMeshProUGUI newText) => useDecreeNoHeroTargetText = newText;

    public GameObject GetDyingRoomPart() => DyingRoomPart;
    public void SetDyingRoomPart(GameObject newDyingRoomPart) => DyingRoomPart = newDyingRoomPart;
    public GameObject GetSmokeVFXPrefab() => smokeVFXPrefab;
    public void SetSmokeVFXPrefab(GameObject newSmokeVFXPrefab) => smokeVFXPrefab = newSmokeVFXPrefab;
    public float GetDyingSmokeTimerMin() => dyingSmokeTimerMin;
    public void SetDyingSmokeTimerMin(float newDyingSmokeTimerMin) => dyingSmokeTimerMin = newDyingSmokeTimerMin;
    public float GetDyingSmokeTimerMax() => dyingSmokeTimerMax;
    public void SetDyingSmokeTimerMax(float newDyingSmokeTimerMax) => dyingSmokeTimerMax = newDyingSmokeTimerMax;

    public bool GetIsFloorSelected() => isFloorSelected;
    public void SetIsFloorSelected(bool newIsSelected) { isFloorSelected = newIsSelected; UpdateFloorSelectionGlow(); }
    public void SetMonsterSelectionNumber(int newMonsterSelectionNumber) { monsterSelectionNumber = newMonsterSelectionNumber; UpdateMonsterSelectionGlows(); }
    public int GetMonsterSelectionNumber() => monsterSelectionNumber;
    public void SetHeroSelectionNumber(int newHeroSelectionNumber) { monsterSelectionNumber = newHeroSelectionNumber; UpdateHeroSelectionGlows(); }
    public int GetHeroSelectionNumber() => monsterSelectionNumber;
    private GameObject GetFloorSelectionGlow() => FloorSelectionGlow;
    private List<GameObject> GetMonsterSelectionGlowList() => MonsterSelectionGlowList;
    private List<GameObject> GetHeroSelectionGlowList() => HeroSelectionGlowList;
    public int GetRoomWidth() => roomWidth;
    public void SetRoomWidth(int newRoomWidth) => roomWidth = newRoomWidth;
    public int GetRoomHeight() => roomHeight;
    public void SetRoomHeight(int newRoomHeight) => roomHeight = newRoomHeight;
    public int GetRoomDepth() => roomDepth;
    public void SetRoomDepth(int newRoomDepth) => roomDepth = newRoomDepth;

    public float GetSmokeDurationMin() => smokeDurationMin;
    public void SetSmokeDurationMin(float newSmokeDurationMin) => smokeDurationMin = newSmokeDurationMin;
    public float GetSmokeDurationMax() => smokeDurationMax;
    public void SetSmokeDurationMax(float newSmokeDurationMax) => smokeDurationMax = newSmokeDurationMax;
    public int GetSmokeAmountMin() => smokeAmountMin;
    public void SetSmokeAmountMin(int newSmokeAmountMin) => smokeAmountMin = newSmokeAmountMin;
    public int GetSmokeAmountMax() => smokeAmountMax;
    public void SetSmokeAmountMax(int newSmokeAmountMax) => smokeAmountMax = newSmokeAmountMax;

    public float GetDyingRoomPartTimerMin() => dyingRoomPartTimerMin;
    public void SetDyingRoomPartTimerMin(float newDyingRoomPartTimerMin) => dyingRoomPartTimerMin = newDyingRoomPartTimerMin;
    public float GetDyingRoomPartTimerMax() => dyingRoomPartTimerMax;
    public void SetDyingRoomPartTimerMax(float newDyingRoomPartTimerMax) => dyingRoomPartTimerMax = newDyingRoomPartTimerMax;
    public bool GetDyingRoomPartActive() => dyingRoomPartActive;
    public void SetDyingRoomPartActive(bool newDyingRoomPartActive) => dyingRoomPartActive = newDyingRoomPartActive;

    public void SetIsTempFloor(bool newIsTempFloor) => isTempFloor = newIsTempFloor;
    public bool GetIsTempFloor() => isTempFloor;

    public void SetFlashDyingCoroutine(Coroutine newFlashDyingCoroutine) => flashDyingCoroutine = newFlashDyingCoroutine;
    public Coroutine GetFlashDyingCoroutine() => flashDyingCoroutine;
    
    public void SetSmokeCoroutine(Coroutine newSmokeCoroutine) => smokeCoroutine = newSmokeCoroutine;
    public Coroutine GetSmokeCoroutine() => smokeCoroutine;

    public void SetSmokeSpawnOffset(Vector3 newSmokeSpawnOffset) => smokeSpawnOffset = newSmokeSpawnOffset;
    public Vector3 GetSmokeSpawnOffset() => smokeSpawnOffset;

    #endregion

    #endregion

    #endregion
}
