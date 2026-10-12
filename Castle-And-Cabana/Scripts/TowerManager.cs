using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    [Tooltip("Container Object for Base object and all floors")]
    public GameObject Tower;
    [Tooltip("The object that is the 'Base' floor of the tower")]
    public GameObject Base;
    [Tooltip("The object that is the 'Top' floor of the tower")]
    public GameObject Top;
    [Tooltip("The object that is the reference for UI object facing")]
    public GameObject FacingReferenceObject;
    [Tooltip("The object that is the reference for Throne of the tower")]
    public GameObject Throne;

    public GameObject TowerExplosionVFX;

    public GameObject GoldPilePrefab;
    public GameObject GoldFountainPrefab;


    [Tooltip("List of floors on the tower")]
    public List<GameObject> Floors = new List<GameObject>();

    [Tooltip("Prefab of floor object")]
    public GameObject Floor;

    [Tooltip("The ")]
    public GameObject TowerPositionObject;

    [Tooltip("The ")]
    public List<GameObject> LevelFailedHeroPositions;

    [Tooltip("GameObject containing the full tower camera information")]
    [SerializeField] private GameObject FullTowerCameraHolder;


    [SerializeField] private bool towerIsDestroyed;
    [SerializeField] private bool hasMiddleFloors;

    

    




    [SerializeField] private int maxFloors = 5;

    #region // Placing Floors
    public enum PlacingFloorStates { NotPlacing, StartPlacing, PlacingFloor, EndingPlacing }
    [SerializeField] private PlacingFloorStates placingState;
    [SerializeField] private bool placingFloor;
    [SerializeField] private int floorPlacePosition;
    [SerializeField] private GameObject temporaryFloor;
    [SerializeField] private float placingFloorCdInit = .5f;
    [SerializeField] private float placingFloorCd;
    [SerializeField] private CoreUnit.CoreTypes placingFloorCoreType;
    [SerializeField] private GameObject placingFloorCoreObject;



    #endregion

    #region // Debugging
    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;
    #endregion

    /// <summary>
    /// The manager that handles the tower's logic and game progression.
    /// This should be assigned in the inspector and provides access to tower-related features such as floors and sections.
    /// </summary>
    [SerializeField] private TowerDefenseManager TowerGameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize with debugging enabled
        debug = new DebugUtility(false);
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacingFloor();
        CheckForMiddleFloors();
    }

    public void InitializeTower()
    {
        #region// Initalize state vars
        SetTowerIsDestroyed(false);
        SetPlacingState(PlacingFloorStates.NotPlacing);
        ResetTraveledThisTurn();
        #endregion
        #region // Set inital position of tower
        if (Tower != null)
        {
            Tower.transform.SetParent(gameObject.transform);
        }
        #endregion
        #region// Initalize floor list
        if (Floors != null)
        {
            foreach (GameObject floor in Floors)
            {
                if (floor != null) Destroy(floor);
            }
            Floors.Clear();
        }
        else
        {
            Floors = new List<GameObject>();
        }
        #endregion
        #region // Setup the base floor of the tower
        // Ensure Base is assigned
        if (Base == null)
        {
            Debug.LogError("Base floor is not assigned! Tower cannot be initialized.");
            return;
        }

        
        // Assign Base floor
        TowerSectionAddon baseSection = GetTowerSectionAddon(Base);
        if (baseSection != null)
        {
            baseSection.SetFloorNumber(Floors.Count);
            if (Tower != null) Base.transform.SetParent(Tower.transform);
            Floors.Add(Base);
            baseSection.InitializeSection(GetGameManager());
        }
        else
        {
            Debug.LogError("Base floor does not have a TowerSectionAddon component! Cannot set FloorNumber.");
            return;
        }
        #endregion
        #region // Setup the top floor of the tower
        // Ensure Top is assigned
        if (Top == null)
        {
            Debug.LogError("Top floor is not assigned! Tower cannot be initialized.");
            return;
        }

        // Assign Top floor
        TowerSectionAddon topSection = GetTowerSectionAddon(Top);
        if (topSection != null)
        {
            topSection.SetFloorNumber(Floors.Count);
            if (Tower != null) Top.transform.SetParent(Tower.transform);
            Floors.Add(Top);
            // Make sure to spawn boss Team up top
            //GetMonsterManager().SpawnBossMonsterTeam();
            //GetCoreManager().SpawnBossCore();
            topSection.InitializeSection(GetGameManager(), CoreUnit.CoreTypes.Boss);
        }
        else
        {
            Debug.LogError("Top floor does not have a TowerSectionAddon component! Cannot set FloorNumber.");
            return;
        }
        #endregion
        #region // Update Floor Numbers and Camera
        //
        UpdateAllFloorNumbers();

        // Set the camera to outer and base floor
        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, 0);
        #endregion
        #region// Debug initialization
        if (debug.IsDebugging)
        {
            Debug.Log($"Tower initialized successfully. Floors: {Floors.Count}");
            foreach (var floor in Floors)
            {
                Debug.Log($"Floor: {floor?.name ?? "null"}");
            }
        }
        #endregion
    }

    private void UpdateAllFloorNumbers()
    {
        for (int i = 0; i < Floors.Count; i++)
        {
            GameObject floor = Floors[i];
            TowerSectionAddon addon = GetTowerSectionAddon(floor);

            if (addon != null)
            {
                if (i == 0)
                {
                    // Base floor
                    addon.SetFloorNumber(0);
                    floor.name = "Base Floor";
                }
                else if (i == Floors.Count - 1)
                {
                    // Top floor
                    addon.SetFloorNumber(Floors.Count - 1);
                    floor.name = "Top Floor";
                }
                else
                {
                    // Middle floors
                    int floorNumber = i; // Adjusted numbering for middle floors
                    addon.SetFloorNumber(floorNumber);
                    floor.name = $"Floor {floorNumber}";
                }

                addon.UpdateCameraNumbers();
            }
            else
            {
                Debug.LogWarning($"TowerSectionAddon not found on floor {i}");
            }
        }
    }


    private void UpdateAllFloorPositions()
    {
        for (int i = 0; i < Floors.Count; i++)
        {
            GameObject floor = Floors[i];
            TowerSectionAddon addon = GetTowerSectionAddon(floor);

            if (addon != null)
            {
                if (i == 0)
                {
                    floor.transform.position = Tower.transform.position;
                }
                else
                {
                    GameObject previousFloor = Floors[i - 1];
                    TowerSectionAddon previousAddon = GetTowerSectionAddon(previousFloor);

                    if (previousAddon != null)
                    {
                        Vector3 previousTopPosition = previousAddon.GetTopConnection().transform.position;
                        floor.transform.position = previousTopPosition;
                    }
                }
                addon.UpdateUnitsPosition();
            }
        }

        // Reorder sibling indices
        for (int i = 0; i < Floors.Count; i++)
        {
            Floors[i].transform.SetSiblingIndex(i);
        }
    }


    // Get the TowerSectionAddon component from a floor GameObject
    public TowerSectionAddon GetTowerSectionAddon(GameObject floor)
    {
        if (floor == null) return null;

        TowerSectionAddon sectionAddon = floor.GetComponent<TowerSectionAddon>();
        if (sectionAddon == null)
        {
            sectionAddon = floor.GetComponentInChildren<TowerSectionAddon>(true);
        }

        return sectionAddon;
    }

    // Adds a new floor to the tower
    public void AddFloor(int whereToCreate = -1, CoreUnit.CoreTypes floorCore = CoreUnit.CoreTypes.Basic)
    {
        // Ensure base and top floors are defined
        if (Base == null || Floors.Count < 2)
        {
            Debug.LogError("Base or Top Floor is not properly set. Cannot add floors.");
            return;
        }

        if (Floors.Count >= maxFloors)
        {
            Debug.LogWarning($"Cannot add more floors. Maximum of {maxFloors} floors reached.");
            return;
        }

        // Clamp whereToCreate to valid middle indices
        if (whereToCreate <= 0 || whereToCreate >= Floors.Count - 1)
        {
            whereToCreate = Floors.Count - 1; // Default to inserting before the top floor
        }

        // Instantiate the new floor
        GameObject newFloor = Instantiate(Floor, Tower.transform.position, Tower.transform.rotation, Tower.transform);
        if (newFloor == null)
        {
            Debug.LogError("Failed to instantiate a new floor. Check the Floor prefab.");
            return;
        }

        // Insert the floor in the desired position
        Floors.Insert(whereToCreate, newFloor);

        UpdateAllFloorNumbers();

        // Initialize the new floor
        TowerSectionAddon newFloorSection = GetTowerSectionAddon(newFloor);
        if (newFloorSection != null)
        {
            // Create and Empty Monster Team
            // Spawn a specific floor core
            newFloorSection.InitializeSection(TowerGameManager, floorCore);
        }
        else
        {
            Debug.LogError($"TowerSectionAddon not found on {newFloor.name}. Ensure the Floor prefab includes this component.");
            return;
        }

        // Update floor positions and numbers
        UpdateAllFloorPositions();

        // Focus the camera on the new floor
        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, newFloorSection.GetFloorNumber());

        //newFloorSection.SpawnEmptyMonsterTeam();

        // Spawn core and monster team for the new floor
        //newFloorSection.SpawnSpecificCore(floorCore);
        
    }

    public void RemoveFloorAndReorderTower(int floorToRemove)
    {
        if (floorToRemove == 0) { Debug.LogError("Attempting to remove Base floor, something is wrong"); return; }
        if (floorToRemove == GetFloors().Count - 1) { Debug.LogError("Attempting to remove Top floor, something is wrong"); return; }
        // Remove floors cameras

        // Remove the floor in the list athe location
        Floors.RemoveAt(floorToRemove);

        #region // Update Floor Information
        Debug.Log("Updating floor numbers...");
        // Run UpdateAllFloorNumbers to correctly set floor numbers
        UpdateAllFloorNumbers();
        Debug.Log("Floor numbers updated.");

        Debug.Log("Updating floor positions...");
        // Run UpdateAllFloorPositions to correctly position the floors
        UpdateAllFloorPositions();
        Debug.Log("Floor positions updated.");
        #endregion

    }

    public void ReorderTowerAfterSectionDestruction(int destroyedFloorNumber)
    {
        // Validate destroyed floor number
        if (destroyedFloorNumber < 0 || destroyedFloorNumber >= Floors.Count)
        {
            Debug.LogError($"Invalid destroyed floor number: {destroyedFloorNumber}. Valid range: 0 to {Floors.Count - 1}");
            return;
        }

        // List to store remaining floors (excluding destroyed floor)
        List<GameObject> remainingFloors = new List<GameObject>();

        // Loop through floors and add them to remainingFloors list, excluding the destroyed one
        for (int i = 0; i < Floors.Count; i++)
        {
            if (i != destroyedFloorNumber)
            {
                remainingFloors.Add(Floors[i]);
            }
        }

        // Clear the Floors list
        Floors.Clear();

        // Re-assign remaining floors to the Floors list in the correct order based on floor numbers
        foreach (GameObject floor in remainingFloors.OrderBy(f => GetTowerSectionAddon(f).GetFloorNumber()))
        {
            Floors.Add(floor);
        }

        // Update references for Top and Base floors
        Base = Floors[0];
        Top = Floors[Floors.Count - 1];

        // Re-parent floors to the Tower object
        foreach (GameObject floor in Floors)
        {
            floor.transform.SetParent(Tower.transform);
        }

        // Re-position floors based on their order in the Floors list
        for (int i = 0; i < Floors.Count; i++)
        {
            GameObject floor = Floors[i];
            TowerSectionAddon sectionAddon = GetTowerSectionAddon(floor);

            if (sectionAddon != null)
            {
                int floorNumber = sectionAddon.GetFloorNumber();

                // Check if the floor below exists (except for the base)
                GameObject floorBelow = null;
                if (floorNumber > 0)
                {
                    floorBelow = Floors[floorNumber - 1];
                }

                // If floor below exists, position the current floor on top of its TopConnection
                if (floorBelow != null)
                {
                    TowerSectionAddon floorBelowSection = GetTowerSectionAddon(floorBelow);
                    if (floorBelowSection != null)
                    {
                        floor.transform.position = floorBelowSection.GetTopConnection().transform.position;
                        floor.transform.rotation = floorBelowSection.GetTopConnection().transform.rotation;
                    }
                }
                // Otherwise, if it's the base floor, position it at the base position
                else if (floor == Base)
                {
                    floor.transform.position = Base.transform.parent.position; // Assuming Base is a child of the Tower object
                    floor.transform.rotation = Base.transform.parent.rotation;
                }
            }
        }

        // Update monster and core information based on the new floor order
        UpdateMonsterAndCoreInfoAfterReorder();

        // Reset traveled this turn flag for all remaining floors
        ResetTraveledThisTurn();

        // Debug logging
        if (debug.IsDebugging)
        {
            Debug.Log($"Tower reordered after floor {destroyedFloorNumber} destruction. New Floor Count: {Floors.Count}");
        }
    }

    private void UpdateMonsterAndCoreInfoAfterReorder()
    {
        // Loop through floors and update monster team and core information based on their new floor number
        for (int i = 0; i < Floors.Count; i++)
        {
            GameObject floor = Floors[i];
            TowerSectionAddon sectionAddon = GetTowerSectionAddon(floor);

            if (sectionAddon != null)
            {
                int floorNumber = sectionAddon.GetFloorNumber();
                MonsterTeam monsterTeam = sectionAddon.GetMonsterSlots().GetMonsterTeam();
                CoreUnit coreUnit = sectionAddon.GetCoreSlot().GetCoreUnit();

                if (monsterTeam != null)
                {
                    monsterTeam.SetCurrentFloor(floorNumber);
                }

                if (coreUnit != null)
                {
                    coreUnit.SetCurrentFloor(floorNumber);
                }
            }
        }
    }

    public IEnumerator CheckFloorsForEndOfRoundDestruction()
    {
        if (debug.IsDebugging) Debug.Log("Starting CheckFloorsForEndOfRoundDestruction...");

        // Iterate through floors, excluding base and top
        for (int i = 1; i < Floors.Count - 1; i++)
        {
            GameObject floor = Floors[i];
            Debug.Log($"Checking floor {i} for end-of-round destruction.");

            TowerSectionAddon sectionAddon = GetTowerSectionAddon(floor);

            if (sectionAddon != null)
            {
                Debug.Log($"Floor {i} has a TowerSectionAddon. Checking for destruction...");
                StartCoroutine(sectionAddon.CheckForEndOfRoundDestruction());
            }
            else
            {
                Debug.LogError($"Floor {i} does not have a TowerSectionAddon component!");
            }

            // Yield to allow other coroutines to execute
            yield return null;
        }

        if (debug.IsDebugging) Debug.Log("Finished CheckFloorsForEndOfRoundDestruction.");
    }
    public IEnumerator LevelFailedCoroutine()
    {
        if (debug.IsDebugging) Debug.Log("Starting Tower Manager LevelFailedCoroutine...");

        // Start the isDying Process for every floor
        foreach (var floor in GetFloors())
        {
            // Get the addon of the floor
            TowerSectionAddon addon = GetTowerSectionAddon(floor);
            //Start the dying process of the addon
            addon.SetIsDying(true);
        }

        // Move the camera to the top of the tower

        // Explode the top floors crystal

        // Pull the camera back

        // Wait untill all heroes have escaped
        yield return new WaitUntil(() => GetHeroManager().GetHeroesHaveEscaped());


        #region // Destroy all floors between Base and Top
        for (int _i = Floors.Count - 2; _i > 0; _i--)
        {
            // Get the floor object
            GameObject floor = Floors[_i];
            // Get the addon of the floor
            TowerSectionAddon addon = GetTowerSectionAddon(floor);
            //Start the dying process of the addon
            addon.SetIsDestroyed(true);
            //Actually destroy the floor
            yield return StartCoroutine(addon.CheckForEndOfRoundDestruction());
            // Wait flor each floor to destroy
            yield return new WaitForSeconds(0.5f);
        }

        // Wait a bit for floors to destroy
        yield return new WaitForSeconds(2.5f);
        #endregion

        #region // Destroy the Tower and Spawn the Gold Pile/Fountain

        // Create explosion effect
        SpawnTowerExplosionVFX(Base.transform.position + new Vector3(0f, 10f, 0f));

        // Create GoldPilePrefab: start at 0.25 scale, positioned at Base's location, and link it to this game object
        float goldPileMinScale = .01f;
        GameObject goldPile = Instantiate(GoldPilePrefab, Base.transform.position, Quaternion.identity);
        goldPile.transform.localScale = Vector3.one * goldPileMinScale;
        goldPile.transform.parent = this.transform;

        // Destroy the rest of the tower
        Destroy(Top);
        Destroy(Base);

        // Make gold rain on the ruins
        // Create GoldFountainPrefab at the gold pile's location
        GameObject goldFountain = Instantiate(GoldFountainPrefab, Base.transform.position, GoldFountainPrefab.transform.rotation);
        goldFountain.transform.localScale = (new Vector3(8f, 8f, 8f));
        goldFountain.transform.SetParent(this.transform);

        // Gradually increase the gold pile's scale to 30x over 10 seconds
        float duration = 10f;
        float elapsedTime = 0f;
        Vector3 initialScale = goldPile.transform.localScale;
        float goldPileMaxScale = 2.0f;
        Vector3 targetScale = Vector3.one * goldPileMaxScale;

        while (elapsedTime < duration)
        {
            // Lerp scale from initial to target based on elapsed time
            goldPile.transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsedTime / duration);

            // Increment elapsed time
            elapsedTime += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Ensure the final scale is exactly 30x
        goldPile.transform.localScale = targetScale;
        #endregion

        #region // Tower has been destroyed fully
        SetTowerIsDestroyed(true);

        if (debug.IsDebugging) Debug.Log("Finished LevelFailedCoroutine.");
        #endregion

    }
    private void SpawnTowerExplosionVFX(Vector3 location)
    {
        // Instantiate the death VFX prefab at the death point location with the specified rotation
        if (TowerExplosionVFX == null) return;

        Quaternion rotation = Quaternion.identity; // Specify the desired rotation
        Instantiate(TowerExplosionVFX, location, rotation);
    }

    

    #region // Placing Floor
    public void StartPlacingFloor(CoreUnit.CoreTypes whichCore = CoreUnit.CoreTypes.Basic)
    {
        // Change State
        SetPlacingState(PlacingFloorStates.StartPlacing);
        // Set Initial Position
        SetPlacingPosition(1);
        // Set Core Type of placing floor
        SetPlacingFloorCoreType(whichCore);
        // Set hold phase change on game manager
        GetGameManager().SetHoldPhase(true);
        //
        ResetPlacingFloorCd();
        // Insert the Temp floor into floors section at Placing Position
        InsertTemporaryFloor(0);
        //Change Camera to full tower
        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, GetPlacingPosition());
        // Update buttons
        GetGameManager().GetCastleMenuManager().UpdatePlaceFloorMenuButtons();
    }
    public void CancelPlacingFloor()
    {
        // Set flag for no longer placing floor
        SetPlacingState(PlacingFloorStates.NotPlacing);
        // Reset Initial Position
        SetPlacingPosition(0);
        // No long hold phase on main
        GetGameManager().SetHoldPhase(false);
        // Remove the temp floor
        RemoveTemporaryFloor();
        //Reset cooldown
        ResetPlacingFloorCd();
    }
    public void ConfirmPlacingFloor()
    {
        

        // Set flag for no longer placing floor
        SetPlacingState(PlacingFloorStates.EndingPlacing);
        // Remove the temp floor
        RemoveTemporaryFloor();
        // Insert actual floor at the correct position
        int placingPosition = GetPlacingPosition();
        CoreUnit.CoreTypes placingCoreType = GetPlacingFloorCoreType();
        AddFloor(placingPosition, placingCoreType);
        // No long hold phase on Game manager
        GetGameManager().SetHoldPhase(false);
        //Reset cooldown
        ResetPlacingFloorCd();
        #region// Take out the essence cost
        // Get the cost
        int floorEssenceCost = GetCoreManager().GetCoreEssenceCost(GetPlacingFloorCoreType());
        //Get the inventory essence
        int heldEssence = GetGameManager().GetInventoryManager().GetEssence();
        // Remove the amount of essence
        GetGameManager().GetInventoryManager().SetEssence(heldEssence - floorEssenceCost);
        #endregion
        SetPlacingState(PlacingFloorStates.NotPlacing);
    }
    private void InsertTemporaryFloor(int floorPosition)
    {
        #region// Create a new instance of Floor Prefab and set it to temporary
        SetTemporaryFloor(Instantiate(Floor, Tower.transform.position, Tower.transform.rotation));
        GameObject tempFloor = GetTemporaryFloor();
        // Get the tower section addon of temporary floor
        TowerSectionAddon tempFloorSection = GetTowerSectionAddon(tempFloor);
        // Initalize the section
        tempFloorSection.InitializeTemporarySection(GetGameManager());
        // change the temp floor material to see through purple
        tempFloorSection.StartTemporarySeeThrough();
        // Get the List of floors
        List<GameObject> floors = GetFloors();
        if (floors == null)
        {
            Debug.LogError("Floors list is null. Cannot insert.");
            return;
        }

        // Add temp floor to floors at Floor position
        if (floorPosition < 0 || floorPosition > floors.Count)
        {
            Debug.LogError("Invalid floor position. Cannot insert temporary floor.");
            return;
        }
        #endregion
        #region// Update TemporaryFloor Position
        SetPlacingPosition(floorPosition);

        floors.Insert(GetPlacingPosition(), tempFloor);
        #endregion
        #region // Update Floor Information
        if (debug.IsDebugging) Debug.Log("Updating floor numbers...");
        // Run UpdateAllFloorNumbers to correctly set floor numbers
        UpdateAllFloorNumbers();
        if (debug.IsDebugging) Debug.Log("Floor numbers updated.");

        if (debug.IsDebugging) Debug.Log("Updating floor positions...");
        // Run UpdateAllFloorPositions to correctly position the floors
        UpdateAllFloorPositions();
        if (debug.IsDebugging) Debug.Log("Floor positions updated.");
        #endregion
        #region // Add the temporary core object to the floor
        CreateTemporaryFloorCore();
        #endregion
        #region // Update the camera
        //Change Camera to full tower
        GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, GetPlacingPosition());
        #endregion
    }
    public void CreateTemporaryFloorCore()
    {
        CoreUnit.CoreTypes coreType = GetPlacingFloorCoreType();
        // Create the Core Object
        GameObject corePrefab = GetGameManager().GetCoreManager().GetCoreModelPrefab(coreType);
        GameObject coreObject = Instantiate(corePrefab);
        if (coreObject != null)
        {
            // Place the core
            GameObject tempFloor = GetTemporaryFloor();
            Vector3 adjustedPosition = tempFloor.transform.position;
            adjustedPosition.y += 8.0f; // Shift 8 units down on the Y-axis
            coreObject.transform.position = adjustedPosition;

            // Triple the scale of the core object
            coreObject.transform.localScale *= 3.0f;

            // Save the object 
            SetPlacingFloorCoreObject(coreObject);
        }
    }
    public void MoveTemporaryFloor(int floorPosition)
    {
        #region // Get Variables
        // Get Temporary Floor
        GameObject tempFloor = GetTemporaryFloor();
        if (tempFloor == null) { Debug.LogError("Temporary floor is null. Cannot move."); return; }

        // Get the List of floors
        List<GameObject> floors = GetFloors();
        if (floors == null) { Debug.LogError("Floors list is null. Cannot move temporary floor."); return; }
        #endregion
        #region // Remove Temp Floor
        // Remove temp floor from the floors list
        if (!floors.Remove(tempFloor)) { Debug.LogWarning("Temporary floor not found in the floors list."); return; }

        // Remove temporary cameras from camera list
        //GetTowerSectionAddon(tempFloor).RemoveCamerasFromManager(); ;
        #endregion
        #region // Update Floor Information
        Debug.Log("Updating floor numbers...");
        // Run UpdateAllFloorNumbers to correctly set floor numbers
        UpdateAllFloorNumbers();
        Debug.Log("Floor numbers updated.");

        Debug.Log("Updating floor positions...");
        // Run UpdateAllFloorPositions to correctly position the floors
        UpdateAllFloorPositions();
        Debug.Log("Floor positions updated.");
        #endregion
        #region// Readd Temp Floor at new position
        // Add temp floor to floors at Floor position
        if (floorPosition < 0 || floorPosition > floors.Count)
        {
            Debug.LogError("Invalid floor position. Cannot insert temporary floor.");
            return;
        }
        //Update TemporaryFloor Position
        SetPlacingPosition(floorPosition);
        floors.Insert(GetPlacingPosition(), tempFloor);
        if (debug.IsDebugging) Debug.Log($"Inserted temporary floor at position {GetPlacingPosition()}.");
        #endregion
        #region // Update Floor Information
        Debug.Log("Updating floor numbers...");
        // Run UpdateAllFloorNumbers to correctly set floor numbers
        UpdateAllFloorNumbers();
        Debug.Log("Floor numbers updated.");

        Debug.Log("Updating floor positions...");
        // Run UpdateAllFloorPositions to correctly position the floors
        UpdateAllFloorPositions();
        Debug.Log("Floor positions updated.");
        #endregion

    }
    private void RemoveTemporaryFloor()
    {
        // Get Temporary Floor
        GameObject tempFloor = GetTemporaryFloor();
        if (tempFloor == null)
        {
            Debug.LogError("Temporary floor is null. Cannot remove.");
            return;
        }

        // Get the List of floors
        List<GameObject> floors = GetFloors();
        if (floors == null)
        {
            Debug.LogError("Floors list is null. Cannot remove temporary floor.");
            return;
        }
        // Get the Temporary Core Object
        GameObject coreObject = GetPlacingFloorCoreObject();

        // REmove the Temporary  core object
        if (coreObject != null) { Destroy(coreObject); }

        // Remove temp floor from the floors list
        if (!floors.Remove(tempFloor))
        {
            Debug.LogWarning("Temporary floor not found in the floors list.");
            return;
        }
        //GetTowerSectionAddon(tempFloor).RemoveCamerasFromManager();

        Debug.Log("Removed temporary floor.");

        // Remove the Cameras from the list
        GetTowerSectionAddon(tempFloor).RemoveCamerasFromManager();

        // Destroy the temporary floor GameObject
        Destroy(tempFloor);
        SetTemporaryFloor(null); // Clear the reference

        // Update all floor positions
        #region // Update Floor Information
        Debug.Log("Updating floor numbers...");
        // Run UpdateAllFloorNumbers to correctly set floor numbers
        UpdateAllFloorNumbers();
        Debug.Log("Floor numbers updated.");

        Debug.Log("Updating floor positions...");
        // Run UpdateAllFloorPositions to correctly position the floors
        UpdateAllFloorPositions();
        Debug.Log("Floor positions updated.");
        #endregion
    }
    private void UpdatePlacingFloor()
    {
        // Always countdown cooldown
        if (GetPlacingFloorCd() > 0)
        { SetPlacingFloorCd(GetPlacingFloorCd() - Time.deltaTime); }
        else { SetPlacingFloorCd(0); }

        if (GetPlacingState() == PlacingFloorStates.NotPlacing) return;

        if (GetPlacingFloorCd() <= 0)
        {
            // Make sure the cooldown doesn't go below 0


            // If Confirm key is pressed place the floor
            // If Cancel key is pressed go back 
            if (GetInputManager().GetAction())
            {
                ConfirmPlacingFloor();
            }
            // If Cancel key is pressed go back 
            if (GetInputManager().GetCancel())
            {
                CancelPlacingFloor();
            }

            // If Up Key is pressed move place floor postion up 

            // If Down Key is pressed move place floor postion down 

        }

    }
    public IEnumerable PlacingFloorCoroutine()
    {


        // Run Coroutine until no longer placing floor
        yield return new WaitUntil(() => !GetPlacingFloor());
    }
    private void UpdateTemporaryFloorPosition()
    {
        // Set flag for placing floor
        SetPlacingFloor(true);
        // Set Initial Position
        SetPlacingPosition(0);
        // Create a new instance of Floor Prefab and set it to temporary
        SetTemporaryFloor(Instantiate(Floor, Tower.transform.position, Tower.transform.rotation));
        GameObject tempFloor = GetTemporaryFloor();
        // Get the tower section addon of temporary floor
        TowerSectionAddon tempFloorSection = GetTowerSectionAddon(tempFloor);
        // Initalize the section
        tempFloorSection.InitializeSection(GetGameManager());
        // change the temp floor material to see through purple
        tempFloorSection.StartTemporarySeeThrough();
        // Insert the Temp floor into floors section at Placing Position
    }
    public void MoveTempFloorUp()
    {
        // Get Temp floor information
        int newFloorPosition = GetPlacingPosition() + 1;
        // Remove Currently placed floor
        RemoveTemporaryFloor();
        // Readd new floor in correct place
        InsertTemporaryFloor(newFloorPosition);
        //MoveTemporaryFloor(newFloorPosition);
        // Update the buttons
        GetGameManager().GetCastleMenuManager().UpdatePlaceFloorMenuButtons();
    }
    public void MoveTempFloorDown()
    {
        // Get Temp floor information
        int newFloorPosition = GetPlacingPosition() - 1;
        // Remove Currently placed floor
        RemoveTemporaryFloor();
        // Readd new floor in correct place
        InsertTemporaryFloor(newFloorPosition);
        //MoveTemporaryFloor(newFloorPosition);
        // Update the buttons
        GetGameManager().GetCastleMenuManager().UpdatePlaceFloorMenuButtons();
    }
    public void ChangeTempFloorLeft()
    {
        // Get Temp floor information
        int floorPosition = GetPlacingPosition();
        CoreUnit.CoreTypes coreType = GetPlacingFloorCoreType();
        List<CoreUnit.CoreTypes> unlockedCoreTypes = GetCoreManager().GetListOfUnlockedCores();

        // Get the index of the current coreType in the unlockedCoreTypes list
        int currentIndex = unlockedCoreTypes.IndexOf(coreType);

        // Check if there's a previous coreType in the list
        if (currentIndex > 0)
        {
            // Change coreType to the previous coreType
            CoreUnit.CoreTypes newCoreType = unlockedCoreTypes[currentIndex - 1];

            // Update the coreType
            SetPlacingFloorCoreType(newCoreType);

            // Remove the current temporary floor
            RemoveTemporaryFloor();

            // Re-add temporary floor using the new core type
            InsertTemporaryFloor(floorPosition);
        }
        else
        {
            Debug.LogWarning("Cannot change to a previous core type. Current core type is the first in the list.");
        }

        // Update the buttons
        GetGameManager().GetCastleMenuManager().UpdatePlaceFloorMenuButtons();
    }
    public void ChangeTempFloorRight()
    {
        // Get Temp floor information
        int floorPosition = GetPlacingPosition();
        Debug.Log($"[ChangeTempFloorRight] Floor Position: {floorPosition}");

        CoreUnit.CoreTypes coreType = GetPlacingFloorCoreType();
        Debug.Log($"[ChangeTempFloorRight] Current Core Type: {coreType}");

        List<CoreUnit.CoreTypes> unlockedCoreTypes = GetCoreManager().GetListOfUnlockedCores();
        Debug.Log($"[ChangeTempFloorRight] Unlocked Core Types: {string.Join(", ", unlockedCoreTypes)}");

        // Get the index of the current coreType in the unlockedCoreTypes list
        int currentIndex = unlockedCoreTypes.IndexOf(coreType);
        Debug.Log($"[ChangeTempFloorRight] Current Core Type Index: {currentIndex}");

        // Check if there's a next coreType in the list
        if (currentIndex >= 0 && currentIndex < unlockedCoreTypes.Count - 1)
        {
            // Change coreType to the next coreType
            CoreUnit.CoreTypes newCoreType = unlockedCoreTypes[currentIndex + 1];
            Debug.Log($"[ChangeTempFloorRight] Changing to Next Core Type: {newCoreType}");

            // Update the coreType
            SetPlacingFloorCoreType(newCoreType);

            // Remove the current temporary floor
            RemoveTemporaryFloor();
            Debug.Log($"[ChangeTempFloorRight] Removed Temporary Floor at Position: {floorPosition}");

            // Re-add temporary floor using the new core type
            InsertTemporaryFloor(floorPosition);
            Debug.Log($"[ChangeTempFloorRight] Inserted Temporary Floor with Core Type: {newCoreType} at Position: {floorPosition}");
        }
        else
        {
            Debug.LogWarning($"[ChangeTempFloorRight] Cannot change to the next core type. " +
                             $"Either current core type is not found or it is the last in the list. Current Index: {currentIndex}");
        }

        // Update the buttons
        Debug.Log("[ChangeTempFloorRight] Updating Place Floor Menu Buttons");
        GetGameManager().GetCastleMenuManager().UpdatePlaceFloorMenuButtons();
    }

    public bool CanTempFloorMoveUp()
    {
        #region // Get Temp Floor and Position information
        // Get Temp floor information
        int floorPosition = GetPlacingPosition();
        // Get the Tower Information
        int topPosition = GetFloors().Count;
        #endregion 
        #region // Figure out if temp floor can move
        // If Floor position is less than max amount, you can move up
        if (floorPosition < topPosition - 2)
        {
            return true;
        }
        #endregion 

        // If not trues have been found, cannot move
        return false;
    }
    public bool CanTempFloorMoveDown()
    {
        #region // Get Temp Floor and Position information
        // Get Temp floor information
        int floorPosition = GetPlacingPosition();
        #endregion 
        #region // Figure out if temp floor can move
        // If Floor position is less than max amount, you can move up
        if (floorPosition > 1)
        {
            return true;
        }
        #endregion 

        // If not trues have been found, cannot move
        return false;
    }
    public bool CanTempFloorChangeRight()
    {
        #region // Get Temp floor core, and overall core information
        // Get Temp floor core information
        CoreUnit.CoreTypes coreType = GetPlacingFloorCoreType();
        List<CoreUnit.CoreTypes> unlockedCoreTypes = GetCoreManager().GetListOfUnlockedCores();
        #endregion

        // Check if coreType is the last item in the unlockedCoreTypes list
        if (unlockedCoreTypes.Count == 0)
        {
            Debug.LogError("Unlocked core types list is empty!");
            return false; // No cores available to check
        }

        // Get the index of the current coreType in the unlockedCoreTypes list
        int currentIndex = unlockedCoreTypes.IndexOf(coreType);

        // If the coreType is not found or it's the last in the list, return false
        if (currentIndex == -1 || currentIndex == unlockedCoreTypes.Count - 1)
        {
            return false;
        }

        // Otherwise, there are cores to the right
        return true;
    }
    public bool CanTempFloorChangeLeft()
    {
        #region // Get Temp floor core, and overall core information
        // Get Temp floor core information
        CoreUnit.CoreTypes coreType = GetPlacingFloorCoreType();
        List<CoreUnit.CoreTypes> unlockedCoreTypes = GetCoreManager().GetListOfUnlockedCores();
        #endregion

        // Check if the unlockedCoreTypes list is empty
        if (unlockedCoreTypes.Count == 0)
        {
            Debug.LogError("Unlocked core types list is empty!");
            return false; // No cores available to check
        }

        // Get the index of the current coreType in the unlockedCoreTypes list
        int currentIndex = unlockedCoreTypes.IndexOf(coreType);

        // If the coreType is not found or it's the first item in the list, return false
        if (currentIndex == -1 || currentIndex == 0)
        {
            return false;
        }

        // Otherwise, there are cores to the left
        return true;
    }




    #endregion

    #region // Floor Selections
    public void TurnOffAllFloorsSelectionGlows()
    {
        List<GameObject> floors = GetFloors();
        foreach (GameObject floor in floors)
        {
            // Get the Addon
            TowerSectionAddon addon = GetTowerSectionAddon(floor);
            // Turn off the selction glow
            addon.SetIsFloorSelected(false);
        }

    }
    public void TurnOnFloorSelectionGlows(int whichFloor)
    {
        // Turn off all selection glows before starting a new one
        TurnOffAllFloorsSelectionGlows();
        if (whichFloor <= 0 || whichFloor > GetFloorCount()) return;
        // Get the floor object
        GameObject floor = GetFloor(whichFloor);
        // Get the Addon
        TowerSectionAddon addon = GetTowerSectionAddon(floor);
        // Turn on the selction glow
        addon.SetIsFloorSelected(true);

    }
    #endregion

    private void CheckForMiddleFloors()
    {
        int floorAmount = GetFloorCount();
        // Floor 0 = Base, Floor top / 1 = Base and Top
        // If more than 1 floor 
        //hasMiddleFloors
    }


    // Returns the GameObject representing the floor. 0 should be the base (Floors[0]) and the top should be Floors[Count - 1].
    public GameObject GetFloor(int index)
    {
        // Validate the index range
        if (index >= 0 && index < Floors.Count)
        {
            if (debug.IsDebugging) Debug.Log($"Retrieving floor at index {index}. Total floors: {Floors.Count}");
            return Floors[index];
        }

        // Log an error if the index is out of range
        Debug.LogError($"Floor index {index} is out of range. Valid range: 0 to {Floors.Count - 1}");
        return null;
    }
    public List<GameObject> GetFloors() => Floors;

    public void ResetTraveledThisTurn()
    {
        foreach (GameObject floor in Floors)
        {
            TowerSectionAddon section = GetTowerSectionAddon(floor);
            section.SetHaveTraveresedThisTurn(false);
        }

    }

    // Checks if the tower has reached its maximum floors
    public bool IsTowerReached()
    {
        return Floors.Count >= maxFloors;
    }

    // Resets the tower by removing all floors
    public void ResetTower()
    {
        if (debug.IsDebugging) Debug.Log("Resetting the tower...");

        foreach (GameObject floor in Floors)
        {
            if (floor != null)
            {
                Destroy(floor);
            }
        }

        Floors.Clear();
        if (debug.IsDebugging) Debug.Log("Tower reset complete.");
    }
    public GameObject GetFullTowerCameraHolder() => FullTowerCameraHolder;
    public List<GameObject> GetLevelFailedHeroPositions() => LevelFailedHeroPositions;
    public GameObject GetTowerPositionObject() => TowerPositionObject;
    public bool GetTowerIsDestroyed() => towerIsDestroyed;
    public void SetTowerIsDestroyed(bool newTowerIsDestroyed) => towerIsDestroyed = newTowerIsDestroyed;
    public void SetPlacingState(PlacingFloorStates newState) => placingState = newState;
    public PlacingFloorStates GetPlacingState() => placingState;
    private GameObject GetTowerObject() => Tower;
    public GameObject GetFacingReference() { return FacingReferenceObject; }
    public GameObject GetThrone() { return Throne; }
    public void SetPlacingFloor(bool newPlacingFloor) => placingFloor = newPlacingFloor;
    public bool GetPlacingFloor() => placingFloor;
    public void SetPlacingPosition(int newPlacePosition) => floorPlacePosition = Mathf.Clamp(newPlacePosition, 1, GetFloors().Count);
    public int GetPlacingPosition() => floorPlacePosition;
    public GameObject GetTemporaryFloor() => temporaryFloor;
    public void SetTemporaryFloor(GameObject newTemporaryFloor) => temporaryFloor = newTemporaryFloor;
    private void ResetPlacingFloorCd() => placingFloorCd = placingFloorCdInit;
    private void SetPlacingFloorCd(float newCd) => placingFloorCd = newCd;
    public float GetPlacingFloorCd() => placingFloorCd;
    public bool GetTowerAtMaxFloorCapacity() { return (GetFloors().Count >= maxFloors); }
    public int GetFloorCount() { return (GetFloors().Count); }
    public void SetPlacingFloorCoreType(CoreUnit.CoreTypes newPlacingFloorCore) => placingFloorCoreType = newPlacingFloorCore;
    public void SetPlacingFloorCoreObject(GameObject newPlacingFloorCoreObject) => placingFloorCoreObject = newPlacingFloorCoreObject;
    public GameObject GetPlacingFloorCoreObject() => placingFloorCoreObject;
    public CoreUnit.CoreTypes GetPlacingFloorCoreType() => placingFloorCoreType;
    public TowerDefenseManager GetGameManager() => TowerGameManager;
    public TowerManager GetTowerManager() { return GetGameManager().GetTowerManager(); }
    public HeroManager GetHeroManager() { return GetGameManager().GetHeroManager(); }
    public MonsterManager GetMonsterManager() { return GetGameManager().GetMonsterManager(); }
    public CoreManager GetCoreManager() { return GetGameManager().GetCoreManager(); }
    public InputManager GetInputManager() { return GetGameManager().GetInputManager(); }
    
}
