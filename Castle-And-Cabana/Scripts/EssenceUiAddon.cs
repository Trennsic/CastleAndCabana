using UnityEngine;
using TMPro;
using System.Collections;

public class EssenceUiAddon : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI essenceText;
    [SerializeField] private UiManager uiManager;
    private int essenceAmount = 1;
    private Coroutine essenceUpdateCoroutine;

    void Start()
    {
        //InitalizeEssenceUiAddon();
    }

    public void InitalizeEssenceUiAddon()
    {
        if (essenceText == null)
        {
            Debug.LogError("Essence Text is not assigned in EssenceUiAddon.");
            return;
        }
        gameObject.SetActive(true);
    }

    public void SnapAmountToPosition(int target)
    {
        essenceAmount = target;
        UpdateEssenceText();
    }

    public void MoveAmountToPosition(int target, float duration = 1f)
    {
        if (essenceUpdateCoroutine != null)
        {
            StopCoroutine(essenceUpdateCoroutine);
        }
        if (gameObject.activeInHierarchy)
        {
            essenceUpdateCoroutine = StartCoroutine(UpdateEssenceAmount(target, duration));
        }
        else
        {
            Debug.LogWarning("Cannot start coroutine because EssenceUi is inactive.");
        }

    }

    private IEnumerator UpdateEssenceAmount(int target, float duration)
    {
        float elapsedTime = 0f;
        int startAmount = essenceAmount;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            essenceAmount = Mathf.RoundToInt(Mathf.Lerp(startAmount, target, t));
            UpdateEssenceText();
            yield return null;
        }

        essenceAmount = target;
        UpdateEssenceText();
    }

    private void UpdateEssenceText()
    {
        if (essenceText != null)
        {
            essenceText.text = essenceAmount.ToString();
        }
    }

    public void SetUiManager(UiManager newUiManager) { uiManager = newUiManager; }
    private UiManager GetUiManager() { return uiManager; }
}
