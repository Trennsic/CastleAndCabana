using System.Collections.Generic;
using UnityEngine;

public class MonsterSlotAddon : MonoBehaviour
{
    public enum MonsterSlotStates
    {
        Inactive,
        Free,
        Valid,
        Invalid
    }

    [SerializeField] private MonsterSlotStates slotState;
    [SerializeField] private GameObject SlotFreeFX;
    [SerializeField] private GameObject SlotValidFX;
    [SerializeField] private GameObject SlotInvalidFX;
    [SerializeField] private MonsterUnit slotMonster;
    [SerializeField] private bool isDisabled;
    

    private void Awake()
    {
        // Ensure the FX GameObjects are not null
        if (SlotFreeFX == null || SlotValidFX == null || SlotInvalidFX == null)
        {
            Debug.LogError("One or more FX GameObjects are not assigned in MonsterSlotAddon!", this);
            DisableAllFX();
        }
    }

    public MonsterSlotStates GetMonsterSlotState() => slotState;

    public void SetMonsterSlotState(MonsterSlotStates state)
    {
        slotState = state;

        // Update the FX GameObjects based on the current state
        switch (slotState)
        {
            case MonsterSlotStates.Inactive:
                SetFXActive(false, false, false);
                break;

            case MonsterSlotStates.Free:
                SetFXActive(true, false, false);
                break;

            case MonsterSlotStates.Valid:
                SetFXActive(false, true, false);
                break;

            case MonsterSlotStates.Invalid:
                SetFXActive(false, false, true);
                break;
        }
    }

    public void SetSlotMonster(MonsterUnit monster)
    {
        SetMonster(monster);
    }
    public void ClearMonster() => SetMonster(null);
    public void SetMonster(MonsterUnit newMonster) => slotMonster = newMonster;
    public MonsterUnit GetMonster() => slotMonster;

    private void SetFXActive(bool freeActive, bool validActive, bool invalidActive)
    {
        if (GetIsDisabled()) { DisableAllFX(); return; }
        if (SlotFreeFX != null) SlotFreeFX.SetActive(freeActive);
        if (SlotValidFX != null) SlotValidFX.SetActive(validActive);
        if (SlotInvalidFX != null) SlotInvalidFX.SetActive(invalidActive);
    }
    private void DisableAllFX()
    {
        if (SlotFreeFX != null) SlotFreeFX.SetActive(false);
        if (SlotValidFX != null) SlotValidFX.SetActive(false);
        if (SlotInvalidFX != null) SlotInvalidFX.SetActive(false);
    }
    public bool GetIsDisabled() => isDisabled;
    public void SetIsDisabled(bool newIsDisabled) => isDisabled = newIsDisabled;
}
