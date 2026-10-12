using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static Decree;
using static MonsterManager;

public class DecreeManager : MonoBehaviour
{
    // Singleton instance for easy access
    public enum DecreeStates
    {
        Inactive,
        Casting,
        Used,
    }
    [SerializeField] private DecreeStates decreeState;
    [SerializeField] private Decree castingDecree;

    [SerializeField] private int decreeFloor;
    [SerializeField] private int decreeCharacter;
    [SerializeField] private float castingDecreeCd;
    
    public static DecreeManager Instance { get; private set; }

    [SerializeField] private TowerDefenseManager gameManager; // Reference to the GameManager


    public Decree currentDecree;
    [SerializeField] private List<Decree> allDecrees = new List<Decree>();
    [SerializeField] private List<Decree> unlockedDecrees = new List<Decree>();
    [SerializeField] private List<Decree> activeDecrees = new List<Decree>();
    // Read-only accessors
    public IReadOnlyList<Decree> AllDecrees => allDecrees.AsReadOnly();
    public IReadOnlyList<Decree> UnlockedDecrees => unlockedDecrees.AsReadOnly();
    public IReadOnlyList<Decree> ActiveDecrees => activeDecrees.AsReadOnly();

    private const int MAX_ACTIVE = 5;  // Maximum number of active decrees at a time

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        IncrementCastingDecreeCd();
    }
    /// <summary>
    /// Initializes both the full list of decrees and clears any unlocked/active lists.
    /// </summary>
    public void InitializeDecreeManager()
    {
        InitializeAllDecrees();
        unlockedDecrees.Clear();
        activeDecrees.Clear();

        // Testing unlock and active decrees
        TryToUnlockDecree(Decree.Decrees.Expel);
        TryToUnlockDecree(Decree.Decrees.Encourage);
        TryToActivateDecree(Decree.Decrees.Expel);
        TryToActivateDecree(Decree.Decrees.Encourage);
    }

    /// <summary>
    /// Populates 'allDecrees' based on the Decree.Decrees enum.
    /// </summary>
    private void InitializeAllDecrees()
    {
        allDecrees.Clear();
        foreach (Decree.Decrees decreeEnum in Enum.GetValues(typeof(Decree.Decrees)))
        {
            allDecrees.Add(new Decree(decreeEnum, this));
        }
    }

    /// <summary>
    /// Unlocks a decree (no limit), returns false if already unlocked or not found.
    /// </summary>
    public bool TryToUnlockDecree(Decree.Decrees decreeEnum)
    {
        Decree decree = allDecrees.Find(d => d.GetDecreeEnum() == decreeEnum);
        if (decree == null)
        {
            Debug.LogWarning($"Decree '{decreeEnum}' not found in allDecrees.");
            return false;
        }
        if (unlockedDecrees.Contains(decree))
        {
            Debug.LogWarning($"Decree '{decreeEnum}' is already unlocked.");
            return false;
        }
        unlockedDecrees.Add(decree);
        return true;
    }

    /// <summary>
    /// Activates an unlocked decree (up to MAX_ACTIVE), returns false if limit reached, not unlocked, or already active.
    /// </summary>
    public bool TryToActivateDecree(Decree.Decrees decreeEnum)
    {
        if (activeDecrees.Count >= MAX_ACTIVE)
        {
            Debug.LogWarning("Maximum active decrees reached.");
            return false;
        }

        Decree decree = unlockedDecrees.Find(d => d.GetDecreeEnum() == decreeEnum);
        if (decree == null)
        {
            Debug.LogWarning($"Cannot activate '{decreeEnum}' because it is not unlocked.");
            return false;
        }
        if (activeDecrees.Contains(decree))
        {
            Debug.LogWarning($"Decree '{decreeEnum}' is already active.");
            return false;
        }

        activeDecrees.Add(decree);
        return true;
    }

    /// <summary>
    /// Deactivates an active decree, returns false if not currently active.
    /// </summary>
    public bool TryToDeactivateDecree(Decree.Decrees decreeEnum)
    {
        Decree decree = activeDecrees.Find(d => d.GetDecreeEnum() == decreeEnum);
        if (decree == null)
        {
            Debug.LogWarning($"Decree '{decreeEnum}' is not active.");
            return false;
        }
        activeDecrees.Remove(decree);
        return true;
    }

    public Decrees ConvertStringToDecree(string decreeString)
    {
        if (System.Enum.TryParse(decreeString, true, out Decrees result))
        {
            Debug.Log($"Converted '{decreeString}' to Decree: {result}");
            return result;
        }
        else
        {
            Debug.LogWarning($"Failed to convert '{decreeString}' to a Decree. Defaulting to Expel.");
            return Decrees.Expel; // Fallback/default
        }
    }

    /// <summary>
    /// Executes an unlocked decree against targets.
    /// </summary>
    public void ExecuteDecree(Decree.Decrees decreeEnum, List<UnitInfo> hostileTargets, List< UnitInfo> friendlyTargets)
    {
        Decree decree = unlockedDecrees.Find(d => d.GetDecreeEnum() == decreeEnum);
        if (decree == null)
        {
            Debug.LogError($"Cannot execute decree '{decreeEnum}' because it is not unlocked.");
            return;
        }
        decree.ExecuteDecree(hostileTargets, friendlyTargets);
    }

    /// <summary>
    /// Retrieves the description of any decree by enum.
    /// </summary>
    public string GetDecreeDescription(Decree.Decrees decreeEnum)
    {
        Decree decree = allDecrees.Find(d => d.GetDecreeEnum() == decreeEnum);
        if (decree == null)
        {
            Debug.LogError($"Description for decree '{decreeEnum}' not found.");
            return string.Empty;
        }
        return decree.GetDecreeDescription();
    }
    #region // Using Decree Functions
    private Decree CreateDecree(Decree.Decrees newDecreeType)
    {
        Decree newDecree = new Decree(newDecreeType, this);
        SetCurrentDecree(newDecree);
        return GetCurrentDecree();
    }
    public void StartUsingDecree(Decree.Decrees newDecree, int whichFloor)
    {
        // Create the decree we'll be using
        CreateDecree(newDecree);
        //as
        Decree decree = GetCastingDecree();
        // Change State to currently
        SetDecreeState(DecreeStates.Casting);
        // Set the decree floor and target
        SetDecreeFloor(whichFloor);
        SetInitialDecreeTarget();
        // Zoom camera to target floor
        ChangeDecreeCamera();
        // Set hold phase change on game manager (So you cannot start round without finishing casting decree)
        GetGameManager().SetHoldPhase(true);
        // Reset the internal CD
        ResetCastingDecreeCd();
    }
    public void ChangeDecreeCamera()
    {
        // Next enable based on targeting type
        Decree decree = GetCurrentDecree();
        Decree.DecreeTargetType targetType = decree.GetDecreeTargetType();
        int decreeFloor = GetDecreeFloor();
        //
        switch (targetType)
        {
            // ---- Allies (monsters on this floor) ----
            case Decree.DecreeTargetType.OneAlly:
            case Decree.DecreeTargetType.AllAllies:
                GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, decreeFloor);
                break;

            // ---- Enemies (heroes on this floor) ----
            case Decree.DecreeTargetType.OneEnemy:
            case Decree.DecreeTargetType.AllEnemies:
                GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.HeroZoom, decreeFloor);
                break;

            // ---- Any single character or all characters ----
            case Decree.DecreeTargetType.OneCharacter:
            case Decree.DecreeTargetType.AllCharacters:
                GetGameManager().GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.CharactersZoom, decreeFloor);
                break;
            default: Debug.LogError($"Unknown DecreeTargetType: {targetType}"); break;
        }
    }// lEFT x = -550, Right x = 450, Up y = 250, Down y = -10
    private void UpdateCastingDecreeTargetingIndicators()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion
        #region // Handle the No valid target first
        bool validTarget = CheckIfAnyValidDecreeTargetsExist();
        // Link the Decree no target text of relavent floor addon (This should be set by decree manager)
        TextMeshProUGUI noHeroText = towerAddon.GetNoHeroText();
        TextMeshProUGUI noMonsterText = towerAddon.GetNoMonsterText();
        TextMeshProUGUI noCharText = towerAddon.GetNoCharacterText();
        // Disable the no-valid target text ui
        if (noHeroText != null) { noHeroText.gameObject.SetActive(false); }
        if (noMonsterText != null) { noMonsterText.gameObject.SetActive(false); }
        if (noCharText != null) { noCharText.gameObject.SetActive(false); }
        // If there is no valid target for current Decree activate the text
        if (!validTarget)
        {
            switch (GetCurrentDecree().GetDecreeTargetType())
            {
                // ---- Allies (monsters on this floor) ----
                case Decree.DecreeTargetType.OneAlly:
                case Decree.DecreeTargetType.AllAllies:
                    // Turn on the No hero target text
                    if (noHeroText != null) { noHeroText.gameObject.SetActive(true); }
                    break;

                // ---- Enemies (heroes on this floor) ----
                case Decree.DecreeTargetType.OneEnemy:
                case Decree.DecreeTargetType.AllEnemies:
                    // Turn on the No monster target text
                    if (noMonsterText != null) { noMonsterText.gameObject.SetActive(true); }
                    break;

                // ---- Any single character or all characters ----
                case Decree.DecreeTargetType.OneCharacter:
                case Decree.DecreeTargetType.AllCharacters:
                    // Turn on the No character target text
                    if (noCharText != null) { noCharText.gameObject.SetActive(true); }
                    break;

                default:
                    Debug.LogError($"Unknown DecreeTargetType: {GetCurrentDecree().GetDecreeTargetType()}");
                    break;
            }
        }
        #endregion
        #region // Enable and Disable slot targeting fx based on target type / character selected
        // Only enable targeti ndicators if valid targets
        if (validTarget)
        {
            // First disable current indicators
            DisableCastingDecreeTargetingIndicators();
            // Next enable based on targeting type
            Decree decree = GetCurrentDecree();
            Decree.DecreeTargetType targetType = decree.GetDecreeTargetType();
            int targetNum = GetDecreeCharacter();
            // (Heroes are Characters 0 - 3, Monsters are Character 4 - 7)
            switch (targetType)
            {
                // If single target single character
                case Decree.DecreeTargetType.OneEnemy:
                    if (targetNum < 0 || targetNum > 3)
                    {
                        Debug.LogWarning($"Decree Target Number is outside of range for enemies [0 - 3] - {targetNum}");
                        targetNum = Math.Clamp(targetNum, 0, 3);
                    }
                    heroSlots.EnableTargetingFx(targetNum + 1);
                    break;
                case Decree.DecreeTargetType.OneAlly:
                    if (targetNum < 4 || targetNum > 7)
                    {
                        Debug.LogWarning($"Decree Target Number is outside of range for allies [4 - 7] - {targetNum}");
                        targetNum = Math.Clamp(targetNum, 4, 7);
                    }
                    monsterSlots.EnableTargetingFx(targetNum - 3);
                    break;
                case Decree.DecreeTargetType.OneCharacter:
                    if (targetNum >= 4 && targetNum <= 7)
                    {
                        monsterSlots.EnableTargetingFx(targetNum - 3);
                        Debug.Log($"Targeting Monster Slot {targetNum - 3 + 1}");
                    }
                    else if (targetNum >= 0 && targetNum <= 3)
                    {
                        heroSlots.EnableTargetingFx(targetNum + 1);
                        Debug.Log($"Targeting Hero Slot {targetNum + 1}");
                    }
                    else
                    {
                        Debug.LogWarning($"Decree Target Number is outside valid character range [0 - 7] - {targetNum}");
                    }
                    break;

                case Decree.DecreeTargetType.AllEnemies:
                    heroSlots.EnableAllTargetingFx();
                    break;
                case Decree.DecreeTargetType.AllAllies:
                    monsterSlots.EnableAllTargetingFx();
                    break;
                case Decree.DecreeTargetType.AllCharacters:
                    heroSlots.EnableAllTargetingFx();
                    monsterSlots.EnableAllTargetingFx();
                    break;
                default:
                    Debug.LogError($"Unknown Decree Target Type: {decree.GetDecreeTargetType()}");
                    return;
            }
        }
        else
        {
            heroSlots.DisableAllTargetingFx();
            monsterSlots.DisableAllTargetingFx();
        }
            #endregion
        #region // Update the castle menu buttons based on the current target
        GetGameManager().GetCastleMenuManager().UpdateUseDecreeButtons();
        #endregion
        // Update the camera as well
        ChangeDecreeCamera();
    }
    private void DisableCastingDecreeTargetingIndicators()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion
        #region// Disable targeting indicators for both heroes and monsters
        monsterSlots.DisableAllTargetingFx();
        heroSlots.DisableAllTargetingFx();
        #endregion
    }
    private void SetInitialDecreeTarget()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion
        #region// Choose Character to target based on targeting type of decree
        Decree decree = GetCurrentDecree();
        switch (decree.GetDecreeTargetType())
        {
            case Decree.DecreeTargetType.OneEnemy:
                // The inital number will be 1 - 4, Changed to 0 -  3 indexing
                SetDecreeCharacter((heroSlots.GetFirstOccupiedSlot() - 1));
                break;
            case Decree.DecreeTargetType.OneAlly:
                // The inital number will be 1 - 4, Changed to 0 -  3 indexing, then + 4 for monsters
                SetDecreeCharacter((monsterSlots.GetFirstOccupiedSlot() - 1) + 4);
                break;
            case Decree.DecreeTargetType.OneCharacter:
                int monsterIndex = monsterSlots.GetFirstOccupiedSlot();
                if (monsterIndex > 0)
                {
                    // Convert to 0-based index and shift by +4 for monsters
                    SetDecreeCharacter((monsterIndex - 1) + 4);
                    Debug.Log("Decree target set to first occupied monster slot.");
                }
                else
                {
                    int heroIndex = heroSlots.GetFirstOccupiedSlot();
                    if (heroIndex > 0)
                    {
                        // Convert to 0-based index
                        SetDecreeCharacter(heroIndex - 1);
                        Debug.Log("No monsters found. Decree target set to first occupied hero slot.");
                    }
                    else
                    {
                        Debug.LogWarning("No valid target found for OneCharacter decree.");
                    }
                }
                break;
            case Decree.DecreeTargetType.AllEnemies:
            case Decree.DecreeTargetType.AllAllies:
            case Decree.DecreeTargetType.AllCharacters:
                SetDecreeCharacter(0);
                break;
            default:
                Debug.LogError($"Unknown Decree Target Type: {decree.GetDecreeTargetType()}");
                return;
        }
        #endregion
    }
    public bool CheckIfAnyValidDecreeTargetsExist()
    {
        Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Starting target validation check...");

        // Grab the floor & slot addons
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        Debug.Log($"[CheckIfAnyValidDecreeTargetsExist] Getting floor {GetDecreeFloor()}...");

        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null)
        {
            Debug.LogError("Tower Floor is null. Cannot check targets.");
            return false;
        }

        Debug.Log($"[CheckIfAnyValidDecreeTargetsExist] Getting tower section addon for floor {GetDecreeFloor()}...");
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null)
        {
            Debug.LogError("Tower Addon is null. Cannot check targets.");
            return false;
        }

        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Retrieved monster and hero slots.");

        // What decree are we casting?
        Decree decree = GetCurrentDecree();
        Decree.DecreeTargetType targetType = decree.GetDecreeTargetType();
        Debug.Log($"[CheckIfAnyValidDecreeTargetsExist] Checking for decree target type: {targetType}");

        switch (targetType)
        {
            // ---- Allies (monsters on this floor) ----
            case Decree.DecreeTargetType.OneAlly:
            case Decree.DecreeTargetType.AllAllies:
                Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Checking for ally targets (monsters)...");
                // If any monster slot has a monster, we have a valid target
                for (int s = 1; s <= 4; s++)
                {
                    var monster = monsterSlots.GetMonsterInSlot(s);
                    Debug.Log($"[CheckIfAnyValidDecreeTargetsExist] Slot {s} monster: {(monster != null ? "Exists" : "Empty")}");
                    if (monster != null)
                    {
                        Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Found valid ally target.");
                        return true;
                    }
                }
                Debug.Log("[CheckIfAnyValidDecreeTargetsExist] No ally targets found.");
                return false;

            // ---- Enemies (heroes on this floor) ----
            case Decree.DecreeTargetType.OneEnemy:
            case Decree.DecreeTargetType.AllEnemies:
                Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Checking for enemy targets (heroes)...");
                // If any hero slot has a hero, we have a valid target
                for (int s = 1; s <= 4; s++)
                {
                    var hero = heroSlots.GetHeroInSlot(s);
                    Debug.Log($"[CheckIfAnyValidDecreeTargetsExist] Slot {s} hero: {(hero != null ? "Exists" : "Empty")}");
                    if (hero != null)
                    {
                        Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Found valid enemy target.");
                        return true;
                    }
                }
                Debug.Log("[CheckIfAnyValidDecreeTargetsExist] No enemy targets found.");
                return false;

            // ---- Any single character or all characters ----
            case Decree.DecreeTargetType.OneCharacter:
            case Decree.DecreeTargetType.AllCharacters:
                Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Checking for any character targets...");
                // Check heroes first...
                for (int s = 1; s <= 4; s++)
                {
                    var hero = heroSlots.GetHeroInSlot(s);
                    Debug.Log($"[CheckIfAnyValidDecreeTargetsExist] Slot {s} hero: {(hero != null ? "Exists" : "Empty")}");
                    if (hero != null)
                    {
                        Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Found valid hero target.");
                        return true;
                    }
                }
                // ...then monsters
                for (int s = 1; s <= 4; s++)
                {
                    var monster = monsterSlots.GetMonsterInSlot(s);
                    Debug.Log($"[CheckIfAnyValidDecreeTargetsExist] Slot {s} monster: {(monster != null ? "Exists" : "Empty")}");
                    if (monster != null)
                    {
                        Debug.Log("[CheckIfAnyValidDecreeTargetsExist] Found valid monster target.");
                        return true;
                    }
                }
                Debug.Log("[CheckIfAnyValidDecreeTargetsExist] No character targets found.");
                return false;

            default:
                Debug.LogError($"Unknown DecreeTargetType: {targetType}");
                return false;
        }
    }
    public void CancelUsingDecree()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion

        // Set flag for no longer using decree
        SetDecreeState(DecreeStates.Inactive);

        // No long hold phase on main
        GetGameManager().SetHoldPhase(false);

        // Turn off all targeting indicators
        heroSlots.DisableAllTargetingFx();
        monsterSlots.DisableAllTargetingFx();

        // Turn off No target text as well
        DecreeDisableNoTargetText();

        //Reset cooldown
        ResetCastingDecreeCd();
    }
    public void DecreeDisableNoTargetText()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        #endregion
        #region // Handle the No valid target first
        // Link the Decree no target text of relavent floor addon (This should be set by decree manager)
        TextMeshProUGUI noHeroText = towerAddon.GetNoHeroText();
        TextMeshProUGUI noMonsterText = towerAddon.GetNoMonsterText();
        TextMeshProUGUI noCharText = towerAddon.GetNoCharacterText();
        // Disable the no-valid target text ui
        if (noHeroText != null) { noHeroText.gameObject.SetActive(false); }
        if (noMonsterText != null) { noMonsterText.gameObject.SetActive(false); }
        if (noCharText != null) { noCharText.gameObject.SetActive(false); }
        #endregion
    }
    public void ConfirmUsingDecree()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots is null. Cannot insert."); return; }
        #endregion

        Decree decree = GetCurrentDecree();
        Decree.DecreeTargetType decreeType = decree.GetDecreeTargetType();
        int decreeCharacter = GetDecreeCharacter();

        List<UnitInfo> hostileTargets = new List<UnitInfo>();
        List<UnitInfo> friendlyTargets = new List<UnitInfo>();

        // Hero index remapping: 0→3, 1→1, 2→2, 3→4
        int RemapHeroIndex(int dc)
        {
            return dc switch
            {
                0 => 3,
                1 => 2,
                2 => 1,
                3 => 4,
                _ => -1
            };
        }

        switch (decreeType)
        {
            case Decree.DecreeTargetType.OneEnemy:
                {
                    int heroSlot = RemapHeroIndex(decreeCharacter);
                    if (heroSlot != -1)
                    {
                        HeroUnit hero = heroSlots.GetHeroInSlot(heroSlot);
                        if (hero != null)
                            hostileTargets.Add(hero.GetUnitInfo());
                    }
                    break;
                }

            case Decree.DecreeTargetType.OneAlly:
                {
                    int monsterSlot = decreeCharacter - 3;
                    if (monsterSlot >= 1 && monsterSlot <= 4)
                    {
                        MonsterUnit monster = monsterSlots.GetSlot(monsterSlot).GetMonster();
                        if (monster != null)
                            friendlyTargets.Add(monster.GetUnitInfo());
                    }
                    break;
                }

            case Decree.DecreeTargetType.OneCharacter:
                {
                    if (decreeCharacter >= 0 && decreeCharacter <= 3)
                    {
                        int heroSlot = RemapHeroIndex(decreeCharacter);
                        if (heroSlot != -1)
                        {
                            HeroUnit hero = heroSlots.GetHeroInSlot(heroSlot);
                            if (hero != null)
                                hostileTargets.Add(hero.GetUnitInfo());
                        }
                    }
                    else if (decreeCharacter >= 4 && decreeCharacter <= 7)
                    {
                        int monsterSlot = decreeCharacter - 3;
                        if (monsterSlot >= 1 && monsterSlot <= 4)
                        {
                            MonsterUnit monster = monsterSlots.GetSlot(monsterSlot).GetMonster();
                            if (monster != null)
                                friendlyTargets.Add(monster.GetUnitInfo());
                        }
                    }
                    break;
                }

            case Decree.DecreeTargetType.AllEnemies:
                {
                    for (int i = 0; i <= 3; i++)
                    {
                        int heroSlot = RemapHeroIndex(i);
                        if (heroSlot != -1)
                        {
                            HeroUnit hero = heroSlots.GetHeroInSlot(heroSlot);
                            if (hero != null)
                                hostileTargets.Add(hero.GetUnitInfo());
                        }
                    }
                    break;
                }

            case Decree.DecreeTargetType.AllAllies:
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        MonsterUnit monster = monsterSlots.GetSlot(i).GetMonster();
                        if (monster != null)
                            friendlyTargets.Add(monster.GetUnitInfo());
                    }
                    break;
                }

            case Decree.DecreeTargetType.AllCharacters:
                {
                    for (int i = 0; i <= 3; i++)
                    {
                        int heroSlot = RemapHeroIndex(i);
                        if (heroSlot != -1)
                        {
                            HeroUnit hero = heroSlots.GetHeroInSlot(heroSlot);
                            if (hero != null)
                                hostileTargets.Add(hero.GetUnitInfo());
                        }
                    }
                    for (int i = 1; i <= 4; i++)
                    {
                        MonsterUnit monster = monsterSlots.GetSlot(i).GetMonster();
                        if (monster != null)
                            friendlyTargets.Add(monster.GetUnitInfo());
                    }
                    break;
                }

            default:
                Debug.LogError($"Unknown Decree Target Type: {decreeType}");
                return;
        }

        // Actually cast the decree
        decree.ExecuteDecree(hostileTargets, friendlyTargets);

        // Set flag for no longer using decree
        SetDecreeState(DecreeStates.Inactive);

        // No longer hold phase on main
        GetGameManager().SetHoldPhase(false);

        // Turn off all targeting indicators
        heroSlots.DisableAllTargetingFx();
        monsterSlots.DisableAllTargetingFx();

        // Turn off No target text
        DecreeDisableNoTargetText();

        // Reset cooldown
        ResetCastingDecreeCd();
    }

    private void IncrementCastingDecreeCd()
    {
        if (GetCastingDecreeCd() > 0)
        {
            float castingCd = GetCastingDecreeCd();
            castingCd = castingCd - Time.deltaTime;
            SetCastingDecreeCd(castingCd);
        }
        else if (GetCastingDecreeCd() < 0) { SetCastingDecreeCd(0); }
    }
    private void ResetCastingDecreeCd()
    {
        SetCastingDecreeCd(.5f);
    }
    public bool CanDecreeTargetMoveRight()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return false; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return false; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return false; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return false; }
        #endregion
        
        #region // Get the current decree target and target type
        var decree = GetCurrentDecree();
        var type = decree.GetDecreeTargetType();
        int currentChar = GetDecreeCharacter();
        #endregion

        #region //  Find if there is a valid character in any slots to the right
        switch (type)
        {
            case DecreeTargetType.OneAlly:
                // monster slots → chars 4–7
                int curSlotM = currentChar - 4 + 1;
                for (int s = curSlotM + 1; s <= 4; s++)
                    if (monsterSlots.GetMonsterInSlot(s) != null)
                        return true;
                break;

            case DecreeTargetType.OneEnemy:
                // hero slots → chars 0–3
                int curSlotH = currentChar + 1;
                for (int s = curSlotH + 1; s <= 4; s++)
                    if (heroSlots.GetHeroInSlot(s) != null)
                        return true;
                break;

            case DecreeTargetType.OneCharacter:
                // both heroes and monsters → chars 0–7
                for (int c = currentChar + 1; c <= 7; c++)
                {
                    if (c < 4)
                    {
                        int slotH = c + 1;
                        if (heroSlots.GetHeroInSlot(slotH) != null)
                            return true;
                    }
                    else
                    {
                        int slotM = c - 4 + 1;
                        if (monsterSlots.GetMonsterInSlot(slotM) != null)
                            return true;
                    }
                }
                break;
        }
        #endregion

        #region// If no valid chararacters, or targeting types
        return false;
        #endregion
    }
    public void MoveDecreeTargetRight()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion

        #region // Ensure target can move right still
        if (!CanDecreeTargetMoveRight()) { return; }
        #endregion

        #region // Get the current decree target and target type
        var decree = GetCurrentDecree();
        var type = decree.GetDecreeTargetType();
        int currentChar = GetDecreeCharacter();
        #endregion

        #region // Move the targeting indicator
        switch (type)
        {
            case DecreeTargetType.OneAlly:
                {
                    int curSlotM = currentChar - 4 + 1;
                    for (int s = curSlotM + 1; s <= 4; s++)
                    {
                        if (monsterSlots.GetMonsterInSlot(s) != null)
                        {
                            SetDecreeCharacter((s - 1) + 4);
                            break;
                        }
                    }
                    break;
                }

            case DecreeTargetType.OneEnemy:
                {
                    int curSlotH = currentChar + 1;
                    for (int s = curSlotH + 1; s <= 4; s++)
                    {
                        if (heroSlots.GetHeroInSlot(s) != null)
                        {
                            SetDecreeCharacter(s - 1);
                            break;
                        }
                    }
                    break;
                }

            case DecreeTargetType.OneCharacter:
                {
                    for (int c = currentChar + 1; c <= 7; c++)
                    {
                        if (c < 4)
                        {
                            int slotH = c + 1;
                            if (heroSlots.GetHeroInSlot(slotH) != null)
                            {
                                SetDecreeCharacter(c);
                                break;
                            }
                        }
                        else
                        {
                            int slotM = c - 4 + 1;
                            if (monsterSlots.GetMonsterInSlot(slotM) != null)
                            {
                                SetDecreeCharacter(c);
                                break;
                            }
                        }
                    }
                    break;
                }
        }

        UpdateCastingDecreeTargetingIndicators();
        #endregion
    }
    public bool CanDecreeTargetMoveLeft()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return false; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return false; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return false; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return false; }
        #endregion

        #region // Get the current decree target and target type
        var decree = GetCurrentDecree();
        var type = decree.GetDecreeTargetType();
        int currentChar = GetDecreeCharacter();
        #endregion

        #region // Find if there is a valid character in any slots to the left
        switch (type)
        {
            case DecreeTargetType.OneAlly:
                // monster slots → chars 4–7
                int curSlotM = currentChar - 4 + 1;
                for (int s = curSlotM - 1; s >= 1; s--)
                    if (monsterSlots.GetMonsterInSlot(s) != null)
                        return true;
                break;

            case DecreeTargetType.OneEnemy:
                // hero slots → chars 0–3
                int curSlotH = currentChar + 1;
                for (int s = curSlotH - 1; s >= 1; s--)
                    if (heroSlots.GetHeroInSlot(s) != null)
                        return true;
                break;

            case DecreeTargetType.OneCharacter:
                // both heroes and monsters → chars 0–7
                for (int c = currentChar - 1; c >= 0; c--)
                {
                    if (c < 4)
                    {
                        int slotH = c + 1;
                        if (heroSlots.GetHeroInSlot(slotH) != null)
                            return true;
                    }
                    else
                    {
                        int slotM = c - 4 + 1;
                        if (monsterSlots.GetMonsterInSlot(slotM) != null)
                            return true;
                    }
                }
                break;
        }
        #endregion

        #region// If no valid characters or not a single‐target decree
        return false;
        #endregion
    }
    public void MoveDecreeTargetLeft()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion

        #region // Ensure target can move left
        if (!CanDecreeTargetMoveLeft()) return;
        #endregion

        #region // Get the current decree target and target type
        var decree = GetCurrentDecree();
        var type = decree.GetDecreeTargetType();
        int currentChar = GetDecreeCharacter();
        #endregion

        #region // Move the targeting indicator
        switch (type)
        {
            case DecreeTargetType.OneAlly:
                {
                    int curSlotM = currentChar - 4 + 1;
                    for (int s = curSlotM - 1; s >= 1; s--)
                    {
                        if (monsterSlots.GetMonsterInSlot(s) != null)
                        {
                            SetDecreeCharacter((s - 1) + 4);
                            break;
                        }
                    }
                    break;
                }

            case DecreeTargetType.OneEnemy:
                {
                    int curSlotH = currentChar + 1;
                    for (int s = curSlotH - 1; s >= 1; s--)
                    {
                        if (heroSlots.GetHeroInSlot(s) != null)
                        {
                            SetDecreeCharacter(s - 1);
                            break;
                        }
                    }
                    break;
                }

            case DecreeTargetType.OneCharacter:
                {
                    for (int c = currentChar - 1; c >= 0; c--)
                    {
                        if (c < 4)
                        {
                            int slotH = c + 1;
                            if (heroSlots.GetHeroInSlot(slotH) != null)
                            {
                                SetDecreeCharacter(c);
                                break;
                            }
                        }
                        else
                        {
                            int slotM = c - 4 + 1;
                            if (monsterSlots.GetMonsterInSlot(slotM) != null)
                            {
                                SetDecreeCharacter(c);
                                break;
                            }
                        }
                    }
                    break;
                }
        }

        UpdateCastingDecreeTargetingIndicators();
        #endregion
    }
    public bool CanDecreeTargetMoveUp()
    {
        // Grab your managers
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        int floorCount = tm.GetFloors().Count;

        // Current decree floor
        int currentFloor = GetDecreeFloor();

        // Can move up only if we're below the top “real” floor (exclude boss/top anchor)
        // Floors: [0 = bottom anchor] ... [Count-1 = top anchor]
        return (currentFloor < floorCount - 2);
    }
    public bool CanDecreeTargetMoveDown()
    {
        // Grab your managers
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();

        // Current decree floor
        int currentFloor = GetDecreeFloor();

        // Can move down only if we're above the bottom “real” floor (exclude bottom anchor at 0)
        return (currentFloor > 1);
    }
    public void MoveDecreeTargetUp()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion

        // Early-out if we’re already at or above the highest “real” floor
        if (!CanDecreeTargetMoveUp()) return;

        // 1) Deactivate Current Fx
        heroSlots.DisableAllTargetingFx();
        monsterSlots.DisableAllTargetingFx();

        // 1) Change floor
        SetDecreeFloor(GetDecreeFloor() + 1);

        // 2) Re-select an initial character on that floor
        SetInitialDecreeTarget();

        // 3) Refresh the FX on that new target
        UpdateCastingDecreeTargetingIndicators();

        // 4) Recenter camera on the new floor
        GetGameManager()
          .GetCameraManager()
          .SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, GetDecreeFloor());
    }
    public void MoveDecreeTargetDown()
    {
        #region // Get the floor & slot information
        TowerDefenseManager gm = GetGameManager();
        TowerManager tm = gm.GetTowerManager();
        GameObject towerFloor = tm.GetFloor(GetDecreeFloor());
        if (towerFloor == null) { Debug.LogError("Tower Floor is null. Cannot insert."); return; }
        TowerSectionAddon towerAddon = tm.GetTowerSectionAddon(towerFloor);
        if (towerAddon == null) { Debug.LogError("Tower Addon not found is null. Cannot insert."); return; }
        MonsterSlotsAddon monsterSlots = towerAddon.GetMonsterSlots();
        if (monsterSlots == null) { Debug.LogError("Monster Slots is null. Cannot insert."); return; }
        HeroSlotsAddon heroSlots = towerAddon.GetHeroSlots();
        if (heroSlots == null) { Debug.LogError("heroSlots  is null. Cannot insert."); return; }
        #endregion

        // Early-out if we’re already at or below the bottom “real” floor
        if (!CanDecreeTargetMoveDown()) return;

        // 1) Deactivate Current Fx
        heroSlots.DisableAllTargetingFx();
        monsterSlots.DisableAllTargetingFx();

        // 1) Change floor
        SetDecreeFloor(GetDecreeFloor() - 1);

        // 2) Re-select an initial character on that floor
        SetInitialDecreeTarget();

        // 3) Refresh the FX on that new target
        UpdateCastingDecreeTargetingIndicators();

        // 4) Recenter camera on the new floor
        GetGameManager()
          .GetCameraManager()
          .SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, GetDecreeFloor());
    }
    public bool GetCheckIfValidDecreeTarget()
    {
        return true;
    }
    #endregion
    public void SetDecreeState(DecreeStates newState) => decreeState = newState;
    public DecreeStates GetDecreeState() => decreeState;
    public void SetDecreeFloor(int newFloor) => decreeFloor = newFloor;
    public int GetDecreeFloor() => decreeFloor;
    public void SetDecreeCharacter(int newCharacter){ decreeCharacter = newCharacter; UpdateCastingDecreeTargetingIndicators(); }
    public int GetDecreeCharacter() => decreeCharacter;
    public void SetCastingDecree(Decree newDecree) => castingDecree = newDecree;
    public Decree GetCastingDecree() => castingDecree;
    public void SetCastingDecreeCd(float newCd) => castingDecreeCd = newCd;
    public float GetCastingDecreeCd() => castingDecreeCd;
    public void SetCurrentDecree(Decree newDecree) => currentDecree = newDecree;
    public Decree GetCurrentDecree() => currentDecree;
    public TowerDefenseManager GetGameManager() => gameManager;
}
