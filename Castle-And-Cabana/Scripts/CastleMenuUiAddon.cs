using UnityEngine;
using TMPro;
using System.Collections;

public class CastleMenuUiAddon : MonoBehaviour
{
    [SerializeField] private UiManager uiManager;

    void Start()
    {
        //InitalizeEssenceUiAddon();
    }

    public void InitalizeEssenceUiAddon()
    {
        gameObject.SetActive(true);
    }

    public void SetUiManager(UiManager newUiManager) { uiManager = newUiManager; }
    private UiManager GetUiManager() { return uiManager; }
}
