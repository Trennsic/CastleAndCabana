using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private int essence;

    [SerializeField] private TowerDefenseManager gameManager; // New reference to the GameManager

    /// <summary>
    /// Returns the current amount of essence.
    /// </summary>
    /// <returns>The current essence value.</returns>
    public int GetEssence()
    {
        return essence;
    }

    /// <summary>
    /// Sets the essence value to a new value.
    /// </summary>
    /// <param name="newEssence">The new essence value.</param>
    public void SetEssence(int newEssence)
    {
        essence = newEssence;
        // Update the ui
        GetGameManager().GetUiManager().UpdateEssenceUi();
    }
    public TowerDefenseManager GetGameManager() { return gameManager; }
}
