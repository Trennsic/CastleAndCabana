using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;


public class LevelCompleteUiAddon : MonoBehaviour
{
    public enum LevelCompleteUiStates
    {
        Off,
        Start,
        Victory,
        Recap,
        Finished,
        End
    }

    #region // Victory Screen
    [Header("Victory Screen")]
    [SerializeField] private GameObject VictoryBanner;
    #endregion

    #region // Recap Banner
    [Header("Recap Banner")]
    [SerializeField] private GameObject RecapBanner; // All recap banner objects are under this game object
    [SerializeField] private GameObject Star1Empty;
    [SerializeField] private GameObject Star1Full;
    [SerializeField] private GameObject Star2Empty;
    [SerializeField] private GameObject Star2Full;
    [SerializeField] private GameObject Star3Empty;
    [SerializeField] private GameObject Star3Full;
    [SerializeField] private TextMeshProUGUI rewardsAmountText;
    [SerializeField] private TextMeshProUGUI bonusAmountText;
    [SerializeField] private TextMeshProUGUI totalAmountText;
    [SerializeField] private Button DoneButton;
    #endregion

    #region // Complete Information
    [SerializeField] private int starAmount     ; // Should be clamped between 1 and 3
    [SerializeField] private int rewardAmount   ;
    [SerializeField] private int bonusAmount    ;
    [SerializeField] private int totalAmount    ;
    #endregion

    // Private state variable
    private LevelCompleteUiStates currentState = LevelCompleteUiStates.Off;

    [SerializeField] private UiManager uiManager;

    private void Awake()
    {
        

        
        
    }

    public void InitializeLevelCompleteUiAddon()
    {
        ValidateVariables();
        SetupStars();
        SetupAmountsText();
        // During initialization, turn off the VictoryBanner and RecapBanner.
        if (GetVictoryBanner() != null)
            GetVictoryBanner().SetActive(false);
        if (GetRecapBanner() != null)
            GetRecapBanner().SetActive(false);
    }

    /// <summary>
    /// Checks that all required variables are assigned.
    /// </summary>
    private void ValidateVariables()
    {
        if (GetVictoryBanner() == null)
            Debug.LogError("VictoryBanner is not assigned!");
        if (GetRecapBanner() == null)
            Debug.LogError("RecapBanner is not assigned!");
        if (GetStar1Empty() == null)
            Debug.LogError("Star1Empty is not assigned!");
        if (GetStar1Full() == null)
            Debug.LogError("Star1Full is not assigned!");
        if (GetStar2Empty() == null)
            Debug.LogError("Star2Empty is not assigned!");
        if (GetStar2Full() == null)
            Debug.LogError("Star2Full is not assigned!");
        if (GetStar3Empty() == null)
            Debug.LogError("Star3Empty is not assigned!");
        if (GetStar3Full() == null)
            Debug.LogError("Star3Full is not assigned!");
        if (GetRewardsAmountText() == null)
            Debug.LogError("rewardsAmountText is not assigned!");
        if (GetBonusAmountText() == null)
            Debug.LogError("bonusAmountText is not assigned!");
        if (GetTotalAmountText() == null)
            Debug.LogError("totalAmountText is not assigned!");
        if (GetDoneButton() == null)
            Debug.LogError("DoneButton is not assigned!");
    }

    /// <summary>
    /// Starts the Level Complete UI sequence.
    /// </summary>
    public void StartLevelCompleteUI(int stars = 1, int rewards = 350, int bonus = 125, int total = 1300)
    {
        currentState = LevelCompleteUiStates.Start;
        SetStarAmount(stars);
        SetRewardAmount(rewards);
        SetBonusAmount(bonus);
        SetTotalAmount(total);
        StartCoroutine(ProcessLevelCompleteUI());
    }
    

    /// <summary>
    /// Manages the state transitions of the UI sequence.
    /// </summary>
    private IEnumerator ProcessLevelCompleteUI()
    {
        // Start with Victory sequence.
        yield return StartCoroutine(VictorySequence());

        // Then proceed to the Recap sequence.
        yield return StartCoroutine(RecapSequence());

        // Once finished, wait for the DoneButton click.
        currentState = LevelCompleteUiStates.Finished;
        GetDoneButton().onClick.AddListener(OnDoneButtonClicked);

        // Wait until DoneButton has been clicked.
        while (currentState != LevelCompleteUiStates.End)
        {
            yield return null;
        }

        // Optionally, perform any cleanup or final actions here.
        Debug.Log("Level complete sequence ended.");
    }

    /// <summary>
    /// Animates the VictoryBanner: scales up, lingers, then scales down.
    /// </summary>
    private IEnumerator VictorySequence()
    {
        currentState = LevelCompleteUiStates.Victory;

        GameObject vb = GetVictoryBanner();
        vb.transform.localScale = Vector3.zero;
        vb.SetActive(true);

        // Grow from 0 to 1 over 2 seconds.
        yield return StartCoroutine(ScaleOverTime(vb.transform, Vector3.zero, Vector3.one, 2f));

        // Linger for 2 seconds.
        yield return new WaitForSeconds(2f);

        // Shrink from 1 to 0 over 2 seconds.
        yield return StartCoroutine(ScaleOverTime(vb.transform, Vector3.one, Vector3.zero, 2f));

        vb.SetActive(false);
    }

    /// <summary>
    /// Animates the RecapBanner, stars, and reward texts.
    /// </summary>
    private IEnumerator RecapSequence()
    {
        currentState = LevelCompleteUiStates.Recap;

        GameObject rb = GetRecapBanner();
        rb.transform.localScale = Vector3.zero;
        rb.SetActive(true);

        // Scale the RecapBanner from 0 to 1 (assuming 2 seconds for this animation).
        yield return StartCoroutine(ScaleOverTime(rb.transform, Vector3.zero, Vector3.one, 2f));

        // Setup stars: enable all empty stars and disable full stars.
        SetupStars();

        // Clamp starAmount between 1 and 3.
        int stars = Mathf.Clamp(GetStarAmount(), 1, 3);

        // Animate stars sequentially.
        if (stars >= 1)
            yield return StartCoroutine(AnimateStar(GetStar1Full(), GetStar1Empty()));
        if (stars >= 2)
            yield return StartCoroutine(AnimateStar(GetStar2Full(), GetStar2Empty()));
        if (stars >= 3)
            yield return StartCoroutine(AnimateStar(GetStar3Full(), GetStar3Empty()));

        // Animate reward number texts.
        if (GetRewardsAmountText() != null) { GetRewardsAmountText().text = "0"; }
        if (GetBonusAmountText() != null) GetBonusAmountText().text = "0";
        if (GetTotalAmountText() != null) GetTotalAmountText().text = "0";
        yield return StartCoroutine(AnimateNumberText(GetRewardsAmountText(), 0, GetRewardAmount(), 1f));
        yield return StartCoroutine(AnimateNumberText(GetBonusAmountText(), 0, GetBonusAmount(), 1f));
        yield return StartCoroutine(AnimateNumberText(GetTotalAmountText(), 0, GetTotalAmount(), 1f));
    }

    /// <summary>
    /// Sets up the stars by enabling the empty stars and disabling the full ones.
    /// </summary>
    private void SetupStars()
    {
        if (GetStar1Empty() != null) GetStar1Empty().SetActive(true);
        if (GetStar2Empty() != null) GetStar2Empty().SetActive(true);
        if (GetStar3Empty() != null) GetStar3Empty().SetActive(true);

        if (GetStar1Full() != null) GetStar1Full().SetActive(false);
        if (GetStar2Full() != null) GetStar2Full().SetActive(false);
        if (GetStar3Full() != null) GetStar3Full().SetActive(false);
    }

    private void SetupAmountsText()
    {
        if (GetRewardsAmountText() != null) { GetRewardsAmountText().text = "";}
        if (GetBonusAmountText() != null) GetBonusAmountText().text = "";
        if (GetTotalAmountText() != null) GetTotalAmountText().text = "";
    }

    /// <summary>
    /// Animates a star full image: scales up then disables the corresponding empty star.
    /// </summary>
    private IEnumerator AnimateStar(GameObject starFull, GameObject starEmpty)
    {
        // Initialize the full star.
        starFull.transform.localScale = Vector3.zero;
        starFull.SetActive(true);

        // Animate scaling from 0 to 1 over 1 second.
        yield return StartCoroutine(ScaleOverTime(starFull.transform, Vector3.zero, Vector3.one, 1f));

        // Disable the empty star.
        if (starEmpty != null)
            starEmpty.SetActive(false);
    }

    /// <summary>
    /// Smoothly scales a transform from an initial scale to a final scale over the given duration.
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
    /// Animates a TextMeshProUGUI number from a start value to an end value over the given duration.
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
    /// Called when the DoneButton is clicked.
    /// </summary>
    private void OnDoneButtonClicked()
    {
        // Remove the listener to avoid multiple triggers.
        GetDoneButton().onClick.RemoveListener(OnDoneButtonClicked);
        currentState = LevelCompleteUiStates.End;
    }

    private void SetStarAmount(int newStarAmount) => starAmount = Mathf.Clamp(newStarAmount, 1, 3);
    private void SetRewardAmount(int newRewardAmount) => rewardAmount = newRewardAmount;
    private void SetBonusAmount(int newBonusAmount) => bonusAmount = newBonusAmount;
    private void SetTotalAmount(int newTotalAmount) => totalAmount = newTotalAmount;
    public void SetUiManager(UiManager newUiManager) { uiManager = newUiManager; }

    #region // Getter Functions
    private GameObject GetVictoryBanner() => VictoryBanner;
    private GameObject GetRecapBanner() => RecapBanner;
    private GameObject GetStar1Empty() => Star1Empty;
    private GameObject GetStar1Full() => Star1Full;
    private GameObject GetStar2Empty() => Star2Empty;
    private GameObject GetStar2Full() => Star2Full;
    private GameObject GetStar3Empty() => Star3Empty;
    private GameObject GetStar3Full() => Star3Full;
    private TextMeshProUGUI GetRewardsAmountText() => rewardsAmountText;
    private TextMeshProUGUI GetBonusAmountText() => bonusAmountText;
    private TextMeshProUGUI GetTotalAmountText() => totalAmountText;
    private Button GetDoneButton() => DoneButton;
    private int GetStarAmount() => starAmount;
    private int GetRewardAmount() => rewardAmount;
    private int GetBonusAmount() => bonusAmount;
    private int GetTotalAmount() => totalAmount;
    #endregion
}
