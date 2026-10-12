using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonsterSlotsAddon : MonoBehaviour
{

    
    [SerializeField] private List<MonsterSlotAddon> Slots = new List<MonsterSlotAddon>(); // 4 MonsterSlotAddon objects representing monster slot positions
    [SerializeField] private MonsterSlotAddon placingSlot; // Unit occupying Slot 1
    [SerializeField] private GameObject CoreSlot; // Core slot position
    [SerializeField] private int teamSize;

    #region // Targeting Fx
    [SerializeField] private Image monsterTargetFx1; // Targeting fx for Slot 1
    [SerializeField] private Image monsterTargetFx2; // Targeting fx for Slot 2
    [SerializeField] private Image monsterTargetFx3; // Targeting fx for Slot 3
    [SerializeField] private Image monsterTargetFx4; // Targeting fx for Slot 4
    private float fxFloatSpeed = 2f; // speed of rise/fall
    private float fxFloatAmount = 1.5f; // how much to move up/down
    private float fxFloatOffsetY = 5f; // how much to move up/down
    private bool targetingFxActive = false;
    private List<Image> activeTargetingFxs = new List<Image>();
    #endregion


    private CoreUnit slotCore; // Unit occupying the core slot
    [SerializeField] private MonsterTeam monsterTeam;

    private bool slotsTaken; // Is there a MonsterTeam in the slots

    // Debugging
    [SerializeField]
    private bool isDebugging = false;

    private void Update()
    {
        UpdateTargetFxPosition();
    }

    // Ensure there are 4 slots in the Slots list
    public void InitializeMonsterSlots()
    {
        // Make sure slots are connected properly
        if (Slots.Count != 4)
        {
            Debug.LogError("Slots list must contain exactly 4 MonsterSlotAddon elements.");
            return;
        }

        // Check that all monster target FX are assigned
        if (monsterTargetFx1 == null || monsterTargetFx2 == null || monsterTargetFx3 == null || monsterTargetFx4 == null)
        {
            Debug.LogError("One or more monsterTargetFx references are not assigned in the Inspector!");
            return;
        }

        // Turn off all targeting FX
        DisableAllTargetingFx();

        // Clear existing monsters from slots
        ClearMonsterSlots();

        if (isDebugging) Debug.Log("Monster slots initialized.");
    }


    public void InitializeSlotTeamSize()
    {
        // Disable all monster slot(s)
        for (int i = 1; i <= 4; i++)
        {
            MonsterSlotAddon slot = GetSlot(i);
            slot.SetIsDisabled(true);
        }
        // Reenable based on how many are allowed in team size
        if (GetTeamSize() >= 1) { GetSlot(1).SetIsDisabled(false); }
        if (GetTeamSize() >= 2) { GetSlot(2).SetIsDisabled(false); }
        if (GetTeamSize() >= 3) { GetSlot(3).SetIsDisabled(false); }
        if (GetTeamSize() >= 4) { GetSlot(4).SetIsDisabled(false); }
    }
    public void EnableSpecificSlot(int whichSlot) { GetSlot(whichSlot).SetIsDisabled(false); }
    public void DisableSpecificSlot(int whichSlot) { GetSlot(whichSlot).SetIsDisabled(true); }

    // Clears the monster slots
    public void ClearMonsterSlots()
    {
        foreach (MonsterSlotAddon slotAddon in GetSlots())
        {
            slotAddon.ClearMonster();
        }
        monsterTeam = null;
        if (isDebugging) Debug.Log("Monster slots cleared.");
    }

    // Set a MonsterTeam in the slots
    public void SetMonsterTeam(MonsterTeam monsterTeam)
    {
        if (monsterTeam == null)
        {
            Debug.LogError("MonsterTeam is null or empty.");
            return;
        }

        ClearMonsterSlots(); // Clear existing slots before assigning new monsters
        this.monsterTeam = monsterTeam;
    }
    #region // Targeting Fx
    private void UpdateTargetFxPosition()
    {
        if (!targetingFxActive) return;

        float offset = Mathf.Sin(Time.time * fxFloatSpeed) * fxFloatAmount;

        Camera mainCam = Camera.main;
        if (mainCam == null) return; // No camera found

        foreach (Image fx in activeTargetingFxs)
        {
            if (fx == null) continue;

            RectTransform rect = fx.rectTransform;

            // Animate up and down
            Vector3 basePosition = rect.anchoredPosition;
            rect.anchoredPosition = new Vector3(basePosition.x, (offset + fxFloatOffsetY), basePosition.z);

            // Make the FX face the camera
            rect.rotation = Quaternion.LookRotation(rect.position - mainCam.transform.position);
        }
    }
    public void EnableAllTargetingFx()
    {
        DisableAllTargetingFx(); // Reset first

        for (int slot = 1; slot <= 4; slot++)
        {
            MonsterUnit monster = GetMonsterInSlot(slot);
            if (monster != null)
            {
                switch (slot)
                {
                    case 1:
                        monsterTargetFx1.gameObject.SetActive(true);
                        activeTargetingFxs.Add(monsterTargetFx1);
                        break;
                    case 2:
                        monsterTargetFx2.gameObject.SetActive(true);
                        activeTargetingFxs.Add(monsterTargetFx2);
                        break;
                    case 3:
                        monsterTargetFx3.gameObject.SetActive(true);
                        activeTargetingFxs.Add(monsterTargetFx3);
                        break;
                    case 4:
                        monsterTargetFx4.gameObject.SetActive(true);
                        activeTargetingFxs.Add(monsterTargetFx4);
                        break;
                }
            }
        }

        targetingFxActive = activeTargetingFxs.Count > 0;
    }


    public void EnableTargetingFx(int whichFx)
    {
        DisableAllTargetingFx(); // Reset first

        switch (whichFx)
        {
            case 1:
                monsterTargetFx1.gameObject.SetActive(true);
                activeTargetingFxs.Add(monsterTargetFx1);
                break;
            case 2:
                monsterTargetFx2.gameObject.SetActive(true);
                activeTargetingFxs.Add(monsterTargetFx2);
                break;
            case 3:
                monsterTargetFx3.gameObject.SetActive(true);
                activeTargetingFxs.Add(monsterTargetFx3);
                break;
            case 4:
                monsterTargetFx4.gameObject.SetActive(true);
                activeTargetingFxs.Add(monsterTargetFx4);
                break;
            default:
                Debug.LogWarning("Invalid FX slot: " + whichFx);
                break;
        }

        targetingFxActive = true;
    }

    public void DisableAllTargetingFx()
    {
        monsterTargetFx1.gameObject.SetActive(false);
        monsterTargetFx2.gameObject.SetActive(false);
        monsterTargetFx3.gameObject.SetActive(false);
        monsterTargetFx4.gameObject.SetActive(false);

        activeTargetingFxs.Clear();
        targetingFxActive = false;
    }
    #endregion

    // Get the monster in a specific slot
    public MonsterUnit GetMonsterInSlot(int slot) => GetSlot(slot).GetMonster();

    // Get all monsters in slots
    public List<MonsterUnit> GetAllMonsters()
    {
        List<MonsterUnit> monsters = new List<MonsterUnit>();

        for (int i = 1; i <= 4; i++)
        {
            // Set the corresponding monster into the monster slot
            MonsterUnit monster = GetSlot(i).GetMonster();
            if (monster != null)
            {
                monsters.Add(GetSlot(i).GetMonster());
            }
        }

        return monsters;
    }

    public int GetFirstOpenSlot()
    {
        for (int i = 1; i <= 4; i++)
        {
            // Set the corresponding monster into the monster slot
            MonsterUnit monster = GetSlot(i).GetMonster();
            if (monster == null)
            {
                if (GetSlot(i).GetMonster() == null) return (i);
            }
        }

        return -1; // Indicates no open slots
    }
    public int GetFirstOccupiedSlot()
    {
        for (int i = 1; i <= 4; i++)
        {
            MonsterUnit monster = GetSlot(i).GetMonster();
            if (monster != null)
            {
                return i; // Return the first slot number that is occupied
            }
        }

        return -1; // Indicates no occupied slots
    }

    // Retrieves a specific slot object based on the provided slot number (1-based indexing)
    public MonsterSlotAddon GetSlot(int slotNumber)
    {
        // Validate the slotNumber to ensure it's within the valid range
        if (slotNumber < 1 || slotNumber > Slots.Count)
        {
            Debug.LogError($"Invalid slot number: {slotNumber}. Valid range is 1 to {Slots.Count}.");
            return null;
        }

        // Adjust to 0-based indexing for accessing the list
        return Slots[slotNumber - 1];
    }

    public bool AreSlotsFull()
    {
        bool slotsFull = true;
        for (int i = 1; i <= 4; i++)
        {
            // Make sure the slot being checked isn't disabled 
            bool isDisabled = GetSlot(i).GetIsDisabled();
            if (isDisabled) continue; // if disabled continue to the next slot
            // Set the corresponding monster into the monster slot
            MonsterUnit monster = GetSlot(i).GetMonster();
            if (monster == null) { slotsFull = false; }
        }
        return slotsFull;
    }

    public MonsterTeam GetMonsterTeam()
    {
        return monsterTeam;
    }
    public bool GetIsTaken()
    {
        return slotsTaken;
    }
    public List<MonsterSlotAddon> GetSlots()
    {
        return Slots;
    }
    public MonsterSlotAddon GetPlacingSlot()
    {
        return placingSlot;
    }
    public void SetAllSlotAddonsToInactive()
    {
        foreach (MonsterSlotAddon slotAddon in GetSlots() )
        {
            slotAddon.SetMonsterSlotState(MonsterSlotAddon.MonsterSlotStates.Inactive);
        }
    }
    public void SetSlotAddonState(int whichSlot, MonsterSlotAddon.MonsterSlotStates whichState)
    {
        GetSlots()[whichSlot].SetMonsterSlotState(whichState);
    }
    public void SetPlacingSlotAddonState(MonsterSlotAddon.MonsterSlotStates whichState)
    {
        GetPlacingSlot().SetMonsterSlotState(whichState);
    }
    public int GetTeamSize() => teamSize;
    public void SetTeamSize(int newTeamSize) => teamSize = newTeamSize;
}
