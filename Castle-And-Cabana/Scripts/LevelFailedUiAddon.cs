using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement; // For resetting the game by reloading the scene

public class LevelFailedUiAddon : MonoBehaviour
{
    public enum LevelFailedUiStates
    {
        Off,
        Start,
        Recap,
        Finished,
        End
    }

    #region // Recap Banner
    [Header("Recap Banner")]
    [SerializeField] private GameObject RecapBanner; // The recap banner container
    [SerializeField] private TextMeshProUGUI remainingAmountText;
    [SerializeField] private Button RetryButton;
    [SerializeField] private Button MapButton;
    #endregion

    #region // Values
    [Header("Values")]
    [SerializeField] private LevelFailedUiStates currentState = LevelFailedUiStates.Off;
    [SerializeField] private int remainingAmount;
    #endregion

    #region // References
    [Header("References")]
    [SerializeField] private UiManager uiManager; // Optional reference if needed for additional logic
    #endregion

    private void Awake()
    {
        
    }

    public void InitializeLevelFailedUiAddon()
    {
        ValidateVariables();
        // Ensure the RecapBanner is off at start.
        if (GetRecapBanner() != null)
            GetRecapBanner().SetActive(false);
    }

    /// <summary>
    /// Checks that all required variables are assigned.
    /// </summary>
    private void ValidateVariables()
    {
        if (GetRecapBanner() == null)
            Debug.LogError("RecapBanner is not assigned!");
        if (GetRemainingAmountText() == null)
            Debug.LogError("RemainingAmountText is not assigned!");
        if (GetRetryButton() == null)
            Debug.LogError("RetryButton is not assigned!");
        if (GetMapButton() == null)
            Debug.LogError("MapButton is not assigned!");
    }

    /// <summary>
    /// Starts the Level Failed UI sequence.
    /// </summary>
    public void StartLevelFailedUI(int remaining = 1)
    {
        currentState = LevelFailedUiStates.Start;
        SetRemainingAmount(remaining);
        StartCoroutine(ProcessLevelFailedUI());
    }

    /// <summary>
    /// Manages the state transitions of the Level Failed UI sequence.
    /// </summary>
    private IEnumerator ProcessLevelFailedUI()
    {
        // Run the Recap sequence to animate the banner and the remaining heroes count.
        yield return StartCoroutine(RecapSequence());

        // Once the recap is complete, enter the Finished state and wait for button input.
        currentState = LevelFailedUiStates.Finished;
        GetMapButton().onClick.AddListener(OnMapButtonClicked);
        GetRetryButton().onClick.AddListener(OnRetryButtonClicked);

        // Wait until the Map button sets the state to End.
        while (currentState != LevelFailedUiStates.End)
        {
            yield return null;
        }

        Debug.Log("Level Failed UI sequence ended.");
    }

    /// <summary>
    /// Animates the RecapBanner scaling from 0 to 1 and then animates the remaining heroes count.
    /// </summary>
    private IEnumerator RecapSequence()
    {
        currentState = LevelFailedUiStates.Recap;

        GameObject rb = GetRecapBanner();
        rb.transform.localScale = Vector3.zero;
        rb.SetActive(true);

        // Animate the banner scaling from 0 to 1 over 2 seconds.
        yield return StartCoroutine(ScaleOverTime(rb.transform, Vector3.zero, Vector3.one, 2f));

        // Animate the remaining heroes count text from 0 to remainingAmount over 1 second.
        yield return StartCoroutine(AnimateNumberText(GetRemainingAmountText(), 0, GetRemainingAmount(), 1f));
    }

    /// <summary>
    /// Smoothly scales a transform from an initial scale to a final scale over a given duration.
    /// </summary>
    private IEnumerator ScaleOverTime(Transform target, Vector3 initialScale, Vector3 finalScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(initialScale, finalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = finalScale;
    }

    /// <summary>
    /// Animates a TextMeshProUGUI number from a start value to an end value over a given duration.
    /// </summary>
    private IEnumerator AnimateNumberText(TextMeshProUGUI textComponent, int startValue, int endValue, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            int currentValue = (int)Mathf.Lerp(startValue, endValue, elapsed / duration);
            textComponent.text = currentValue.ToString();
            elapsed += Time.deltaTime;
            yield return null;
        }
        textComponent.text = endValue.ToString();
    }

    /// <summary>
    /// Called when the MapButton is clicked.
    /// Advances the UI state to End.
    /// </summary>
    private void OnMapButtonClicked()
    {
        // Remove listeners to prevent multiple triggers.
        GetMapButton().onClick.RemoveListener(OnMapButtonClicked);
        GetRetryButton().onClick.RemoveListener(OnRetryButtonClicked);
        currentState = LevelFailedUiStates.End;
    }

    /// <summary>
    /// Called when the RetryButton is clicked.
    /// Resets the game by reloading the current scene.
    /// </summary>
    private void OnRetryButtonClicked()
    {
        // Remove listeners to prevent multiple triggers.
        GetMapButton().onClick.RemoveListener(OnMapButtonClicked);
        GetRetryButton().onClick.RemoveListener(OnRetryButtonClicked);

        // Reset the game by reloading the current scene.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #region // Getter Functions
    private GameObject GetRecapBanner() => RecapBanner;
    private TextMeshProUGUI GetRemainingAmountText() => remainingAmountText;
    private Button GetRetryButton() => RetryButton;
    private Button GetMapButton() => MapButton;
    private int GetRemainingAmount() => remainingAmount;
    #endregion

    #region // Setter Functions
    private void SetRemainingAmount(int newAmount) => remainingAmount = newAmount;
    public void SetUiManager(UiManager newUiManager) => uiManager = newUiManager;
    #endregion
}
