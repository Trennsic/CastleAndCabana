using System.Collections.Generic;
using UnityEngine;

public class CoreSlotAddon : MonoBehaviour
{

    private CoreUnit slottedCore; // Unit occupying Slot 1
    [SerializeField] private bool slotsTaken; // Is there a CoreUnit in the slots

    // Debugging
    [SerializeField]
    private bool isDebugging = false;

    // Ensure there are 4 slots in the Slots list
    public void InitializeCoreSlot()
    {
        if (this.gameObject == null)
        {
            Debug.LogError("Core Slot gameobject must not be null.");
            return;
        }

        ClearCoreSlot();
        if (isDebugging) Debug.Log("Core slot initialized.");
    }

    // Clears the monster slots
    public void ClearCoreSlot()
    {
        slottedCore = null;
        slotsTaken = false;

        if (isDebugging) Debug.Log("Core slot cleared.");
    }

    // Set a MonsterTeam in the slots
    public void SetCoreSlot(CoreUnit core)
    {
        if (core == null)
        {
            Debug.LogError("Core unit is null");
            return;
        }

        ClearCoreSlot(); // Clear existing slot before assigning new core
        slotsTaken = true;
        // Set this core to passed core
        slottedCore = core;
    }

    // Get the monster in a specific slot
    public CoreUnit GetCoreUnit()
    {
        return slottedCore;
    }
    public GameObject GetCoreObject()
    {
        return this.gameObject;
    }
    public bool GetIsTaken()
    {
        return slotsTaken;
    }
}
