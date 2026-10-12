using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;
using static TowerDefenseManager;
using Unity.VisualScripting;
using UnityEngine.Rendering;

public class CastleMenuManager : MonoBehaviour
{
    #region // Variables
    public enum CastleMenuStates
    {
        Closed,
        Main,

        AddFloor,
        SummonMonster,
        Spells,
        Info,

        PlaceFloor,
        PlaceMonster,

        StartDecree,
        UseDecree,


    }
    public CastleMenuStates castleMenuState;
    #region // Main Menu
    [Header("Castle Main Menu")]
    public GameObject mainMenuPanel;
    // Link specific buttons in the Inspector
    public Button StartRoundButton;
    public Button AddFloorButton;
    public Button SummonMonstersButton;
    public Button StartDecreeButton;
    public Button SpellsButton;
    public Button mainMenuBackButton;
    #endregion
    #region // Add Floor Menu
    [Header("Castle Add Floor Menu")]
    public GameObject addFloorMenuPanel;
    public Transform addFloorButtonContainer;
    public Button addFloorButtonsPrefab;
    public Button addFloorBackButton;
    #endregion
    #region // Place Floor Menu
    [Header("Castle Place Floor Menu")]
    public GameObject placeFloorMenuPanel;
    public Button upArrowPlaceFloorButton;
    public Button downArrowPlaceFloorButton;
    public Button leftArrowPlaceFloorButton;
    public Button rightArrowPlaceFloorButton;
    public Button placePlaceFloorButton;
    public Button placeFloorBackButton;
    #endregion
    #region // Summon Monster Menu
    [Header("Castle Summon Monster Menu")]
    public GameObject summonMenuPanel;
    public Transform summonButtonContainer;
    public Button summonMonstersButtonsPrefab;
    public Button summonMonsterBackButton;
    #endregion
    #region // Place Summon Menu
    [Header("Castle Place Monster Menu")]
    public GameObject placeMonsterMenuPanel;
    public Button upArrowPlaceMonsterButton;
    public Button downArrowPlaceMonsterButton;
    public Button leftArrowPlaceMonsterButton;
    public Button rightArrowPlaceMonsterButton;
    public Button placePlaceMonsterButton;
    public Button placeMonsterBackButton;
    #endregion
    #region // Start Decree Menu
    [Header("Castle Start Decree Menu")]
    public GameObject startDecreeMenuPanel;
    public Transform startDecreeButtonContainer;
    public Button startDecreeButtonsPrefab;
    public Button startDecreeBackButton;
    #endregion
    #region // Use Decree Menu
    [Header("Castle Use Decree Menu")]
    public GameObject useDecreeMenuPanel;
    public Button commandUseDecreeButton;
    public Button useDecreeBackButton;

    public Button upArrowUseDecreeHeroButton;
    public Button downArrowUseDecreeHeroButton;
    public Button leftArrowUseDecreeHeroButton;
    public Button rightArrowUseDecreeHeroButton;

    public Button upArrowUseDecreeMonstersButton;
    public Button downArrowUseDecreeMonstersButton;
    public Button leftArrowUseDecreeMonstersButton;
    public Button rightArrowUseDecreeMonstersButton;

    public Button upArrowUseDecreeCharButton;
    public Button downArrowUseDecreeCharButton;
    public Button leftArrowUseDecreeCharButton;
    public Button rightArrowUseDecreeCharButton;
    #endregion
    #region // Generic Extras
    [Header("Generic Prefabs")]
    public Button floorButtonPrefab;
    #endregion
    private List<string> unlockedFloors = new List<string>(); // List of unlocked floor names.
    /// <summary>
    /// The manager that handles the tower's logic and game progression.
    /// This should be assigned in the inspector and provides access to tower-related features such as floors and sections.
    /// </summary>
    [SerializeField] private TowerDefenseManager GameManager;
    [SerializeField] private int menuIndex;
    [SerializeField] private int menuIndexMax;
    [SerializeField] private bool spaceForMonster;
    [SerializeField] private bool hasMiddleFloors;

    // Set when opening the summon menu directly from a MonsterFocus slot
    private int pendingSummonFloor = 0;
    private int pendingSummonSlot = 0;
    #region // Debugging
    [Header("Debugging")]

    [Tooltip("Debugging utility for logging and testing tower behavior.")]
    [SerializeField] private DebugUtility debug;
    #endregion
    #endregion
    #region // Functions
    #region // Unity Functions
    private void Update()
    {
        if (castleMenuState == CastleMenuStates.Main && GetInputManager().GetMoveLeftPressed())
        {
            SetCastleMenuState(CastleMenuStates.Closed);
            GetGameManager().GetSelectionManager().StartTowerOverview();
        }
    }
    #endregion
    #region // Initialization
    public void InitializeCastleMenu()
    {
        InitalizeDebug();
        // Connect Buttons to Functions
        #region // Start Menu
        if (StartRoundButton != null) StartRoundButton.onClick.AddListener(OnStartRoundButtonClicked);
        if (AddFloorButton != null) AddFloorButton.onClick.AddListener(OnAddFloorButtonClicked);
        if (SummonMonstersButton != null) SummonMonstersButton.onClick.AddListener(OnSummonButtonClicked);
        if (StartDecreeButton != null) StartDecreeButton.onClick.AddListener(OnStartDecreeButtonClicked);
        #endregion
        #region // Add Floor Menu
        if (addFloorBackButton != null) addFloorBackButton.onClick.AddListener(OnAddFloorBackButtonClicked);
        #endregion
        #region // Place Floor Menu
        if (placeFloorBackButton != null) placeFloorBackButton.onClick.AddListener(OnPlaceFloorBackButtonClicked);
        if (placePlaceFloorButton != null) placePlaceFloorButton.onClick.AddListener(() => OnPlaceFloorButtonClicked("SomeFloorName"));
        if (upArrowPlaceFloorButton != null) upArrowPlaceFloorButton.onClick.AddListener(OnPlaceFloorUpArrowButtonClicked);
        if (downArrowPlaceFloorButton != null) downArrowPlaceFloorButton.onClick.AddListener(OnPlaceFloorDownArrowButtonClicked);
        if (leftArrowPlaceFloorButton != null) leftArrowPlaceFloorButton.onClick.AddListener(OnPlaceFloorLeftArrowButtonClicked);
        if (rightArrowPlaceFloorButton != null) rightArrowPlaceFloorButton.onClick.AddListener(OnPlaceFloorRightArrowButtonClicked);
        #endregion
        #region // Summon Monster Menu
        if (summonMonsterBackButton != null) summonMonsterBackButton.onClick.AddListener(OnSummonBackButtonClicked);
        #endregion
        #region // Place Monster menu
        if (placeMonsterBackButton != null) placeMonsterBackButton.onClick.AddListener(OnPlaceMonsterBackButtonClicked);
        if (upArrowPlaceMonsterButton != null) upArrowPlaceMonsterButton.onClick.AddListener(OnPlaceMonsterUpArrowButtonClicked);
        if (downArrowPlaceMonsterButton != null) downArrowPlaceMonsterButton.onClick.AddListener(OnPlaceMonsterDownArrowButtonClicked);
        if (leftArrowPlaceMonsterButton != null) leftArrowPlaceMonsterButton.onClick.AddListener(OnPlaceMonsterLeftArrowButtonClicked);
        if (rightArrowPlaceMonsterButton != null) rightArrowPlaceMonsterButton.onClick.AddListener(OnPlaceMonsterRightArrowButtonClicked);
        if (placePlaceMonsterButton != null) placePlaceMonsterButton.onClick.AddListener(() => OnPlaceMonsterButtonClicked("SomeFloorName")); // Example
        #endregion
        #region // Start Decree Menu
        if (startDecreeBackButton != null) startDecreeBackButton.onClick.AddListener(OnStartDecreeBackButtonClicked);
        #endregion
        #region // Use Decree Menu
        if (useDecreeBackButton != null) useDecreeBackButton.onClick.AddListener(OnUseDecreeBackButtonClicked);
        if (commandUseDecreeButton != null) commandUseDecreeButton.onClick.AddListener(() => OnUseDecreeCommandButtonClicked("SomeFloorName"));
        
        if (upArrowUseDecreeHeroButton != null) upArrowUseDecreeHeroButton.onClick.AddListener(OnUseDecreeUpArrowButtonClicked);
        if (downArrowUseDecreeHeroButton != null) downArrowUseDecreeHeroButton.onClick.AddListener(OnUseDecreeDownArrowButtonClicked);
        if (leftArrowUseDecreeHeroButton != null) leftArrowUseDecreeHeroButton.onClick.AddListener(OnUseDecreeLeftArrowButtonClicked);
        if (rightArrowUseDecreeHeroButton != null) rightArrowUseDecreeHeroButton.onClick.AddListener(OnUseDecreeRightArrowButtonClicked);
        
        if (upArrowUseDecreeMonstersButton != null) upArrowUseDecreeMonstersButton.onClick.AddListener(OnUseDecreeUpArrowButtonClicked);
        if (downArrowUseDecreeMonstersButton != null) downArrowUseDecreeMonstersButton.onClick.AddListener(OnUseDecreeDownArrowButtonClicked);
        if (leftArrowUseDecreeMonstersButton != null) leftArrowUseDecreeMonstersButton.onClick.AddListener(OnUseDecreeLeftArrowButtonClicked);
        if (rightArrowUseDecreeMonstersButton != null) rightArrowUseDecreeMonstersButton.onClick.AddListener(OnUseDecreeRightArrowButtonClicked);
        
        if (upArrowUseDecreeCharButton != null) upArrowUseDecreeCharButton.onClick.AddListener(OnUseDecreeUpArrowButtonClicked);
        if (downArrowUseDecreeCharButton != null) downArrowUseDecreeCharButton.onClick.AddListener(OnUseDecreeDownArrowButtonClicked);
        if (leftArrowUseDecreeCharButton != null) leftArrowUseDecreeCharButton.onClick.AddListener(OnUseDecreeLeftArrowButtonClicked);
        if (rightArrowUseDecreeCharButton != null) rightArrowUseDecreeCharButton.onClick.AddListener(OnUseDecreeRightArrowButtonClicked);

        #endregion


        SetCastleMenuState(CastleMenuStates.Main);
    }
    private void InitalizeDebug() { debug = new DebugUtility(false); }
    #endregion
    #region // Main Menu
    public void OnStartRoundButtonClicked()
    {
        // Make sure you can start Round
        TowerDefenseManager gm = GetGameManager();
        if (gm.GetGamePhase() == GamePhases.PlayerInput && gm.GetReadyToStartRound())
        {
            // Start the phase
            // Disable main menu and enable add floor menu
            SetCastleMenuState(CastleMenuStates.Closed);
            gm.PlayerInputStartingRound();
        } 
    }
    public void OnSummonButtonClicked()
    {
        // Disable main menu and enable add floor menu
        SetCastleMenuState(CastleMenuStates.SummonMonster);

        // Populate the summon menu
        PopulateSummonMenu();
    }
    public void OnAddFloorButtonClicked()
    {
        // Disable main menu and enable add floor menu
        SetCastleMenuState(CastleMenuStates.AddFloor);

        // Populate the add floor menu
        PopulateAddFloorMenu();
    }
    public void OnStartDecreeButtonClicked()
    {
        // Disable main menu and enable add floor menu
        SetCastleMenuState(CastleMenuStates.StartDecree);

        // Populate the add floor menu
        PopulateStartDecreeMenu();
    }
    #endregion
    #region // Add Floor Menu
    public void OnFloorButtonClicked(string coreName)
    {
        if (debug.IsDebugging) Debug.Log($"Selected: {coreName}");
        // Add functionality for what happens when a floor is selected.
        // Make sure It's during Player input phase
        TowerDefenseManager gameManager = GetGameManager();
        if (gameManager.GetGamePhase() != TowerDefenseManager.GamePhases.PlayerInput)
        { Debug.LogWarning("Cannot Add Floors outside of player input phase. Aborting menu AddFloor"); return; }
        // Make Sure Tower Manager Placing floor is Idle
        if (GetTowerManager().GetPlacingState() != TowerManager.PlacingFloorStates.NotPlacing)
        { Debug.LogWarning("Tower Manager Already Placing Floor. Aborting menu AddFloor"); return; }
        // Make sure Tower Manager Placing Floor CD is not ticking
        if (GetTowerManager().GetPlacingFloorCd() > 0)
        { Debug.LogWarning("Tower Manager Placing CD is up. Aborting menu AddFloor"); return; }

        CoreUnit.CoreTypes coreType = GetCoreManager().ConvertStringToCoreTypes(coreName);
        // Start Adding floor
        GetTowerManager().StartPlacingFloor(coreType);

        // Change menu
        SetCastleMenuState(CastleMenuStates.PlaceFloor);
    }
    public void OnAddFloorBackButtonClicked()
    {
        // Disable main menu and enable add floor menu
        SetCastleMenuState(CastleMenuStates.Main);
    }
    private void PopulateAddFloorMenu()
    {
        foreach (Transform child in addFloorButtonContainer)
        {
            Destroy(child.gameObject);
        }

        List<CoreUnit.CoreTypes> unlockedCoreTypes = GetCoreManager().GetListOfUnlockedCores();
        int numberOfOptions = 0;

        foreach (var coreType in unlockedCoreTypes)
        {
            Button newButton = Instantiate(addFloorButtonsPrefab, addFloorButtonContainer);
            TextAddon textAddon = newButton.GetComponent<TextAddon>();

            if (textAddon != null)
            {
                textAddon.GetText1().text = coreType.ToString();
                textAddon.GetText2().text = GetCoreManager().GetCoreEssenceCost(coreType).ToString();
            }
            else
            {
                Debug.LogError("TextAddon component not found in button prefab.");
            }

            newButton.onClick.AddListener(() => OnFloorButtonClicked(coreType.ToString()));
            numberOfOptions++;
        }
        SetIndexMax(numberOfOptions);
    }
    #endregion
    #region // Place Floor menu
    public void OnPlaceFloorUpArrowButtonClicked()
    {
        // Tell Floor Manager to Move Temp Floor Left
        GetTowerManager().MoveTempFloorUp();
    }
    public void OnPlaceFloorDownArrowButtonClicked()
    {
        // Tell Floor Manager to Move Temp Floor Left
        GetTowerManager().MoveTempFloorDown();
    }
    public void OnPlaceFloorLeftArrowButtonClicked()
    {
        // Tell Floor Manager to Move Temp Floor Left
        GetTowerManager().ChangeTempFloorLeft();
    }
    public void OnPlaceFloorRightArrowButtonClicked()
    {
        // Tell Floor Manager to Move Temp Floor Left   
        GetTowerManager().ChangeTempFloorRight();
    }
    public void OnPlaceFloorButtonClicked(string floorName)
    {
        #region// Check that you have enough to place floor
        // Get the cost
        int floorEssenceCost = GetGameManager().GetCoreManager().GetCoreEssenceCost(GetGameManager().GetTowerManager().GetPlacingFloorCoreType());
        //Get the inventory essence
        int heldEssence = GetGameManager().GetInventoryManager().GetEssence();
        if (floorEssenceCost > heldEssence) { return; }
        #endregion
        // Tell TowerManager to place floor
        GetTowerManager().ConfirmPlacingFloor();
        // 
        SetCastleMenuState(CastleMenuStates.Main);
    }
    public void UpdatePlaceFloorMenuButtons()
    {
        #region // Update Summon Menu Buttons
        bool upValid = GetTowerManager().CanTempFloorMoveUp();
        bool downValid = GetTowerManager().CanTempFloorMoveDown();
        bool leftValid = GetTowerManager().CanTempFloorChangeLeft();
        bool rightValid = GetTowerManager().CanTempFloorChangeRight();

        // Enable or disable the Up arrow GameObject based on if the temp monster can move
        if (upArrowPlaceFloorButton != null) { upArrowPlaceFloorButton.gameObject.SetActive(upValid); }

        if (downArrowPlaceFloorButton != null) { downArrowPlaceFloorButton.gameObject.SetActive(downValid); }

        if (leftArrowPlaceFloorButton != null) { leftArrowPlaceFloorButton.gameObject.SetActive(leftValid); }

        if (rightArrowPlaceFloorButton != null) { rightArrowPlaceFloorButton.gameObject.SetActive(rightValid); }
        #endregion
    }
    public void OnPlaceFloorBackButtonClicked()
    {
        // Make sure It's during Player input phase
        TowerDefenseManager gameManager = GetGameManager();
        // Start Adding floor
        GetTowerManager().CancelPlacingFloor();
        // Disable main menu and enable add floor menu
        SetCastleMenuState(CastleMenuStates.AddFloor);
    }
    #endregion
    #region // Summon Monster Menu

    public void OnSummonButtonClicked(string monsterName)
    {
        Debug.Log($"Selected: {monsterName}");

        TowerDefenseManager gameManager = GetGameManager();
        if (gameManager.GetGamePhase() != TowerDefenseManager.GamePhases.PlayerInput)
        { Debug.LogWarning("Cannot Add Floors outside of player input phase. Aborting menu AddFloor"); return; }
        if (GetMonsterManager().GetPlacingState() != MonsterManager.PlacingMonsterStates.NotPlacing)
        { Debug.LogWarning("Tower Manager Already Placing Floor. Aborting menu AddFloor"); return; }
        if (GetTowerManager().GetPlacingFloorCd() > 0)
        { Debug.LogWarning("Tower Manager Placing CD is up. Aborting menu AddFloor"); return; }

        MonsterUnit.MonsterTypes monsterType = GetMonsterManager().ConvertStringToMonsterType(monsterName);

        if (pendingSummonSlot > 0)
        {
            // Came from MonsterFocus on an empty slot — place directly, skip arrow navigation
            int floor = pendingSummonFloor;
            int slot = pendingSummonSlot;
            pendingSummonFloor = 0;
            pendingSummonSlot = 0;

            GetMonsterManager().PlaceMonsterDirectlyInSlot(floor, slot, monsterType);
            SetCastleMenuState(CastleMenuStates.Closed);
            GetGameManager().GetSelectionManager().StartMonsterFocus(floor);
        }
        else
        {
            // Normal flow: go to PlaceMonster with arrow navigation
            GetMonsterManager().StartPlacingMonster(GetCameraManager().GetFloorPosition(), monsterType);
            SetCastleMenuState(CastleMenuStates.PlaceMonster);
        }
    }
    public void OnSummonBackButtonClicked()
    {
        if (pendingSummonSlot > 0)
        {
            int floor = pendingSummonFloor;
            pendingSummonFloor = 0;
            pendingSummonSlot = 0;
            SetCastleMenuState(CastleMenuStates.Closed);
            GetGameManager().GetSelectionManager().StartMonsterFocus(floor);
        }
        else
        {
            SetCastleMenuState(CastleMenuStates.Main);
        }
    }

    public void StartSummonMonsterForSlot(int floor, int slot)
    {
        pendingSummonFloor = floor;
        pendingSummonSlot = slot;
        SetCastleMenuState(CastleMenuStates.SummonMonster);
        PopulateSummonMenu();
    }
    private void PopulateSummonMenu()
    {
        // Clear existing buttons to prevent duplicates
        foreach (Transform child in summonButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Get the list of unlocked CoreTypes from CoreManager
        List<MonsterUnit.MonsterTypes> unlockedMonsterTypes = GetMonsterManager().GetListOfUnlockedMonsters();

        int numberOfOptions = 0;

        // Create a button for each unlocked CoreType
        foreach (var monsterType in unlockedMonsterTypes)
        {
            Button newButton = Instantiate(summonMonstersButtonsPrefab, summonButtonContainer);
            TextAddon textAddon = newButton.GetComponent<TextAddon>();

            if (textAddon != null)
            {
                textAddon.GetText1().text = monsterType.ToString(); // Set monster name
                textAddon.GetText2().text = GetMonsterManager().GetMonsterEssenceCost(monsterType).ToString(); // Set monster cost
            }
            else
            {
                Debug.LogError("TextAddon component not found in button prefab.");
            }

            newButton.onClick.AddListener(() => OnSummonButtonClicked(monsterType.ToString())); // Add listener
            numberOfOptions++;
        }

        // Update the max options
        SetIndexMax(numberOfOptions);
    }
    #endregion
    #region // Place Monster menu
    public void OnPlaceMonsterUpArrowButtonClicked()
    {
        // Tell Monster Manager to Move Temp Monster Left
        GetMonsterManager().MoveTempMonsterUp();
    }
    public void OnPlaceMonsterDownArrowButtonClicked()
    {
        // Tell Monster Manager to Move Temp Monster Left
        GetMonsterManager().MoveTempMonsterDown();
    }
    public void OnPlaceMonsterLeftArrowButtonClicked()
    {
        // Tell Monster Manager to Move Temp Monster Left
        GetMonsterManager().MoveTempMonsterLeft();
    }
    public void OnPlaceMonsterRightArrowButtonClicked()
    {
        // Tell Monster Manager to Move Temp Monster Left
        GetMonsterManager().MoveTempMonsterRight();
    }
    public void OnPlaceMonsterButtonClicked(string floorName)
    {
        #region// Check that you have enough to place floor
        // Get the cost
        int monsterEssenceCost = GetGameManager().GetMonsterManager().GetMonsterEssenceCost(GetGameManager().GetMonsterManager().GetPlacingMonsterType());
        //Get the inventory essence
        int heldEssence = GetGameManager().GetInventoryManager().GetEssence();
        if (monsterEssenceCost > heldEssence) { return; }
        #endregion
        // Tell TowerManager to place floor
        GetMonsterManager().ConfirmPlacingMonster           ();
        // 
        SetCastleMenuState(CastleMenuStates.Main);
    }
    public void UpdateSummonMenuButtons()
    {
        #region // Update Summon Menu Buttons
        bool upValid = GetMonsterManager().CanTempMonsterMoveUp();
        bool downValid = GetMonsterManager().CanTempMonsterMoveDown();
        bool leftValid = GetMonsterManager().CanTempMonsterMoveLeft();
        bool rightValid = GetMonsterManager().CanTempMonsterMoveRight();

        // Enable or disable the Up arrow GameObject based on if the temp monster can move
        if (upArrowPlaceMonsterButton != null) { upArrowPlaceMonsterButton.gameObject.SetActive(upValid); }

        if (downArrowPlaceMonsterButton != null) { downArrowPlaceMonsterButton.gameObject.SetActive(downValid); }

        if (leftArrowPlaceMonsterButton != null) { leftArrowPlaceMonsterButton.gameObject.SetActive(leftValid); }

        if (rightArrowPlaceMonsterButton != null) { rightArrowPlaceMonsterButton.gameObject.SetActive(rightValid); }
        #endregion
        #region // Check if the valid placement for monster, for Place Monster button
        MonsterManager mm = GetMonsterManager();
        bool validPlacement = mm.GetCheckIfValidPlacingSlot();

        // Enable or disable the AddFloorButton based on the tower's capacity
        if (placePlaceMonsterButton != null) { placePlaceMonsterButton.interactable = validPlacement; }

        // Optionally, update button visuals to provide feedback
        if (validPlacement) { Debug.Log("Not a valid monster Placement. Place Place Monster is disabled."); }
        #endregion
    }
    public void OnPlaceMonsterBackButtonClicked()
    {
        // Make sure It's during Player input phase
        TowerDefenseManager gameManager = GetGameManager();
        // Start Adding floor
        GetMonsterManager().CancelPlacingMonster();
        // Disable main menu and enable add floor menu
        SetCastleMenuState(CastleMenuStates.SummonMonster);
    }
    #endregion
    #region // Start Decree menu
    public void OnStartDecreeListButtonClicked(string decreeName)
    {
        Debug.LogWarning($"Selected: {decreeName}");
        // Add functionality for what happens when a floor is selected.
        // Make sure It's during Player input phase
        TowerDefenseManager gameManager = GetGameManager();
        if (gameManager.GetGamePhase() != TowerDefenseManager.GamePhases.PlayerInput)
        { Debug.LogWarning("Cannot use Decrees outside of player input phase. Aborting menu Use Decree"); return; }
        // Make Sure Tower Manager Placing floor is Idle
        if (gameManager.GetDecreeManager().GetDecreeState() != DecreeManager.DecreeStates.Inactive)
        { Debug.LogWarning("Decree Manager Already Using Decree. Aborting menu Use Decree"); return; }
        // Make sure Tower Manager Placing Floor CD is not ticking
        if (gameManager.GetDecreeManager().GetCastingDecreeCd() > 0)
        { Debug.LogWarning("Decreemanager casting CD is up. Aborting menu Use Decree"); return; }
        
        Decree.Decrees decreeType = gameManager.GetDecreeManager().ConvertStringToDecree(decreeName);
        // (There should already be a precheck that there are middle floors and the boss is sitting in throne)
        int whichFloor = gameManager.GetCameraManager().GetFloorPosition();
        // Start Casting Use Decree
        gameManager.GetDecreeManager().StartUsingDecree(decreeType, whichFloor);

        // Change menu
        SetCastleMenuState(CastleMenuStates.UseDecree);
    }
    public void OnStartDecreeBackButtonClicked()
    {
        // Disable main menu and enable add floor menu
        SetCastleMenuState(CastleMenuStates.Main);
    }
    private void PopulateStartDecreeMenu()
    {
        // Clear out any existing buttons
        foreach (Transform child in startDecreeButtonContainer)
            Destroy(child.gameObject);

        var activeDecrees = DecreeManager.Instance.ActiveDecrees;
        int numberOfOptions = 0;

        foreach (var decree in activeDecrees)
        {
            // Instantiate a new button
            Button newButton = Instantiate(startDecreeButtonsPrefab, startDecreeButtonContainer);
            TextAddon textAddon = newButton.GetComponent<TextAddon>();

            if (textAddon != null)
            {
                // Use the enum name as �Text1�
                string decreeName = decree.GetDecreeEnum().ToString();
                textAddon.GetText1().text = decreeName;

                // Optional: put the description into Text2
                textAddon.GetText2().text = decree.GetDecreeCost().ToString();
            }
            else
            {
                Debug.LogError("TextAddon component not found on button prefab.");
            }

            // Capture the decreeEnum safely for the button click
            var capturedEnum = decree.GetDecreeTitle();
            newButton.onClick.AddListener(() => OnStartDecreeListButtonClicked(capturedEnum));

            numberOfOptions++;
        }

        SetIndexMax(numberOfOptions);
    }


    #endregion
    #region // Use Decree menu
    public void OnUseDecreeBackButtonClicked()
    {
        Debug.Log("Use Decree Back Button used");
        // Change camera back to Zoomed out
        GetGameManager()
          .GetCameraManager()
          .SetCameraStateAndPosition
          (
            CameraManager.CameraStates.ZoomedOut, 
            GetGameManager().GetDecreeManager().GetDecreeFloor()
          );
        // Stop the decree casting
        GetGameManager().GetDecreeManager().CancelUsingDecree();
        // Disable no Target Text
        GetGameManager().GetDecreeManager().DecreeDisableNoTargetText();
        // Go back to start decree menu
        SetCastleMenuState(CastleMenuStates.StartDecree);
    }
    public void OnUseDecreeCommandButtonClicked(string floorName)
    {
        Debug.LogWarning($"Selected: {floorName}");
        //#region// Check that you have enough to place floor
        //// Get the cost
        //int floorEssenceCost = GetGameManager().GetCoreManager().GetCoreEssenceCost(GetGameManager().GetTowerManager().GetPlacingFloorCoreType());
        ////Get the inventory essence
        //int heldEssence = GetGameManager().GetInventoryManager().GetEssence();
        //if (floorEssenceCost > heldEssence) { return; }
        //#endregion
        // Disable no Target Text
        GetGameManager().GetDecreeManager().DecreeDisableNoTargetText();
        // Tell TowerManager to place floor
        GetGameManager().GetDecreeManager().ConfirmUsingDecree();
        // 
        SetCastleMenuState(CastleMenuStates.Main);
    }
    public void OnUseDecreeUpArrowButtonClicked()
    {
        // Tell Decree Manager to move targeting indicator up
        GetGameManager().GetDecreeManager().MoveDecreeTargetUp();
    }
    public void OnUseDecreeDownArrowButtonClicked()
    {
        // Tell Decree Manager to move targeting indicator down
        GetGameManager().GetDecreeManager().MoveDecreeTargetDown();
    }
    public void OnUseDecreeLeftArrowButtonClicked()
    {
        // Tell Decree Manager to move targeting indicator left
        GetGameManager().GetDecreeManager().MoveDecreeTargetLeft();
    }
    public void OnUseDecreeRightArrowButtonClicked()
    {
        // Tell Decree Manager to move targeting indicator right
        GetGameManager().GetDecreeManager().MoveDecreeTargetRight();
    }
    private void DisableNoTargetText()
    {

    }
    public void UpdateUseDecreeButtons()
    {
        #region // First Disable all Arrows
        #region // Hero Buttons
        if (upArrowUseDecreeHeroButton != null) { upArrowUseDecreeHeroButton.gameObject.SetActive(false); }
        if (downArrowUseDecreeHeroButton != null) { downArrowUseDecreeHeroButton.gameObject.SetActive(false); }
        if (leftArrowUseDecreeHeroButton != null) { leftArrowUseDecreeHeroButton.gameObject.SetActive(false); }
        if (rightArrowUseDecreeHeroButton != null) { rightArrowUseDecreeHeroButton.gameObject.SetActive(false); }
        #endregion
        #region // Monsters Buttons
        if (upArrowUseDecreeMonstersButton != null) { upArrowUseDecreeMonstersButton.gameObject.SetActive(false); }
        if (downArrowUseDecreeMonstersButton != null) { downArrowUseDecreeMonstersButton.gameObject.SetActive(false); }
        if (leftArrowUseDecreeMonstersButton != null) { leftArrowUseDecreeMonstersButton.gameObject.SetActive(false); }
        if (rightArrowUseDecreeMonstersButton != null) { rightArrowUseDecreeMonstersButton.gameObject.SetActive(false); }
        #endregion
        #region // Char Buttons
        if (upArrowUseDecreeCharButton != null) { upArrowUseDecreeCharButton.gameObject.SetActive(false); }
        if (downArrowUseDecreeCharButton != null) { downArrowUseDecreeCharButton.gameObject.SetActive(false); }
        if (leftArrowUseDecreeCharButton != null) { leftArrowUseDecreeCharButton.gameObject.SetActive(false); }
        if (rightArrowUseDecreeCharButton != null) { rightArrowUseDecreeCharButton.gameObject.SetActive(false); }
        #endregion
        #endregion
        #region // Update Use Decree Buttons
        bool upValid = GetGameManager().GetDecreeManager().CanDecreeTargetMoveUp();
        bool downValid = GetGameManager().GetDecreeManager().CanDecreeTargetMoveDown();
        bool leftValid = GetGameManager().GetDecreeManager().CanDecreeTargetMoveLeft();
        bool rightValid = GetGameManager().GetDecreeManager().CanDecreeTargetMoveRight();
        // Enable to disable button sets based on which decree
        Decree decree = GetGameManager().GetDecreeManager().GetCurrentDecree();
        switch (decree.GetDecreeTargetType())
        {
            // If single target single character
            case Decree.DecreeTargetType.OneEnemy:
            case Decree.DecreeTargetType.AllEnemies:
                #region // Hero Buttons
                if (upArrowUseDecreeHeroButton != null) { upArrowUseDecreeHeroButton.gameObject.SetActive(upValid); }
                if (downArrowUseDecreeHeroButton != null) { downArrowUseDecreeHeroButton.gameObject.SetActive(downValid); }
                if (leftArrowUseDecreeHeroButton != null) { leftArrowUseDecreeHeroButton.gameObject.SetActive(leftValid); }
                if (rightArrowUseDecreeHeroButton != null) { rightArrowUseDecreeHeroButton.gameObject.SetActive(rightValid); }
                #endregion
                break;
            case Decree.DecreeTargetType.OneAlly:
            case Decree.DecreeTargetType.AllAllies:
                #region // Monsters Buttons
                if (upArrowUseDecreeMonstersButton != null) { upArrowUseDecreeMonstersButton.gameObject.SetActive(upValid); }
                if (downArrowUseDecreeMonstersButton != null) { downArrowUseDecreeMonstersButton.gameObject.SetActive(downValid); }
                if (leftArrowUseDecreeMonstersButton != null) { leftArrowUseDecreeMonstersButton.gameObject.SetActive(leftValid); }
                if (rightArrowUseDecreeMonstersButton != null) { rightArrowUseDecreeMonstersButton.gameObject.SetActive(rightValid); }
                #endregion
                break;
            case Decree.DecreeTargetType.AllCharacters:
            case Decree.DecreeTargetType.OneCharacter:
                #region // Char Buttons
                if (upArrowUseDecreeCharButton != null) { upArrowUseDecreeCharButton.gameObject.SetActive(upValid); }
                if (downArrowUseDecreeCharButton != null) { downArrowUseDecreeCharButton.gameObject.SetActive(downValid); }
                if (leftArrowUseDecreeCharButton != null) { leftArrowUseDecreeCharButton.gameObject.SetActive(leftValid); }
                if (rightArrowUseDecreeCharButton != null) { rightArrowUseDecreeCharButton.gameObject.SetActive(rightValid); }
                #endregion
                break;
            default:
                Debug.LogError($"Unknown Decree Target Type: {decree.GetDecreeTargetType()}");
                return;
        }
        #endregion
        #region // Check if the valid placement for decree, for Command Use Decree button
        DecreeManager dm = GetGameManager().GetDecreeManager();
        bool validPlacement = dm.GetCheckIfValidDecreeTarget();

        // Enable or disable the AddFloorButton based on the tower's capacity
        if (commandUseDecreeButton != null) { commandUseDecreeButton.interactable = validPlacement; }

        // Optionally, update button visuals to provide feedback
        if (validPlacement) { Debug.Log("Not a valid decree target. Command Use Decree is disabled."); }
        #endregion
    }
    #endregion
    #region // Generice Menu
    private void UpdateCastleMenus()
    {
        // Reset the index to 0 at the start of each update
        SetIndex(0);

        // Disable all menus
        DisableAllMenus();

        switch (GetCastleMenuState())
        {
            #region // Main Menu
            case CastleMenuStates.Main:
                // Activate the correct menu
                mainMenuPanel.SetActive(true);
                #region // Check if the tower is at maximum floor capacity, for AddFloorButton
                bool atCapacity = GetTowerManager().GetTowerAtMaxFloorCapacity();

                // Enable or disable the AddFloorButton based on the tower's capacity
                if (AddFloorButton != null) { AddFloorButton.interactable = !atCapacity; }

                // Optionally, update button visuals to provide feedback
                if (atCapacity) { Debug.Log("Tower is at maximum floor capacity. AddFloorButton is disabled."); }
                #endregion
                #region // Check if the tower has space to summon monsters, for SummonMonsters
                bool SpawnMonsterValid = true;

                SetSpaceForMonsters(GetMonsterManager().IsThereSpaceToSummonMonster());
                if (!GetSpaceForMonsters()) { SpawnMonsterValid = false; }
                if (GetGameManager().GetLevel() <= 1) { SpawnMonsterValid = false; }

                // Enable or disable the AddFloorButton based on the tower's capacity
                if (SummonMonstersButton != null) { SummonMonstersButton.interactable = SpawnMonsterValid; }

                // Optionally, update button visuals to provide feedback
                if (!GetSpaceForMonsters())
                {
                    if (debug.IsDebugging) Debug.Log("No floors with space to summon monsters. SummonMonstersButton is disabled.");
                }
                #endregion
                #region // Check if the tower has middle floors (later if boss is on throne), for Use Decree
                bool MakeDecreeValid = true;

                SetHasMiddleFloors(GetTowerManager().GetFloorCount() > 2);
                if (!GetHasMiddleFloors()) { MakeDecreeValid = false; }
                if (GetGameManager().GetLevel() <= 1) { MakeDecreeValid = false; }

                // Enable or disable the AddFloorButton based on the tower's capacity
                if (StartDecreeButton != null) { StartDecreeButton.interactable = MakeDecreeValid; }

                // Optionally, update button visuals to provide feedback
                if (!GetHasMiddleFloors())
                {
                    if (debug.IsDebugging) Debug.Log("No Middle Floors/ Floors to use Decrees on . Use Decree Button is disabled.");
                }
                #endregion
                break;
            #endregion

            #region // Add Floor Menu
            case CastleMenuStates.AddFloor:
                // Activate the correct menu
                addFloorMenuPanel.SetActive(true);
                break;
            #endregion

            #region // Summon Monster Menu
            case CastleMenuStates.SummonMonster:
                // Activate the correct menu
                summonMenuPanel.SetActive(true);

                break;
            #endregion

            #region // Place Floor Menu
            case CastleMenuStates.PlaceFloor:
                // Activate the correct menu
                placeFloorMenuPanel.SetActive(true);
                break;
            #endregion

            #region // Place Monster Menu
            case CastleMenuStates.PlaceMonster:
                // Activate the correct menu
                placeMonsterMenuPanel.SetActive(true);

                break;
            #endregion

            #region // Start Decree Menu
            case CastleMenuStates.StartDecree:
                // Activate the correct menu
                startDecreeMenuPanel.SetActive(true);
                break;
            #endregion

            #region // Use Decree Menu
            case CastleMenuStates.UseDecree:
                // Activate the correct menu
                useDecreeMenuPanel.SetActive(true);
                break;
                #endregion

        }
    }
    private void DisableAllMenus()
    {
        mainMenuPanel.SetActive(false);
        addFloorMenuPanel.SetActive(false);
        summonMenuPanel.SetActive(false);
        placeFloorMenuPanel.SetActive(false);
        placeMonsterMenuPanel.SetActive(false);
        startDecreeMenuPanel.SetActive(false);
        useDecreeMenuPanel.SetActive(false);
    }
    #endregion
    private void SetSpaceForMonsters(bool newSpaceForMonsters) => spaceForMonster = newSpaceForMonsters;
    private void SetHasMiddleFloors(bool newHasMiddleFloors) => hasMiddleFloors = newHasMiddleFloors;
    private bool GetHasMiddleFloors() => hasMiddleFloors;
    private bool GetSpaceForMonsters() => spaceForMonster;

    #region // Manager References
    public void SetCastleMenuState(CastleMenuStates newCastleMenuState) { castleMenuState = newCastleMenuState; UpdateCastleMenus(); }
    public CastleMenuStates GetCastleMenuState() => castleMenuState;
    public void SetIndex(int index) => menuIndex = Mathf.Clamp(index, -1, menuIndexMax);
    public void SetIndexMax(int indexMax) => menuIndex = indexMax;
    public int GetIndex() => menuIndex;
    public int GetIndexMax() => menuIndexMax;
    public TowerDefenseManager GetGameManager() => GameManager;
    public InputManager GetInputManager() => GetGameManager().GetInputManager();
    public TowerManager GetTowerManager() => GetGameManager().GetTowerManager();
    public HeroManager GetHeroManager() => GetGameManager().GetHeroManager();
    public MonsterManager GetMonsterManager() => GetGameManager().GetMonsterManager();
    public CoreManager GetCoreManager() => GetGameManager().GetCoreManager();
    public CameraManager GetCameraManager() => GetGameManager().GetCameraManager();
    public CastleMenuManager GetCastleMenuManager() => GetGameManager().GetCastleMenuManager();
    #endregion
    #endregion
}
