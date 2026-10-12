using Unity.Cinemachine;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] private RoundTimerAddon roundTimerAddon;
    [SerializeField] private EssenceUiAddon essenceUiAddon;
    [SerializeField] private CastleMenuUiAddon castleMenuUiAddon;
    [SerializeField] private LevelCompleteUiAddon levelCompleteUiAddon;
    [SerializeField] private LevelFailedUiAddon levelFailedUiAddon;
    [SerializeField] private TowerDefenseManager gameManager; // New reference to the GameManager
    [SerializeField] private float uiSpeed = 1f;
    [SerializeField] private float baseUiSpeed = 1f;

    

    private void Start()
    {
        
    }

    public void InitializeUiManager()
    {
        InitalizeRoundTimer();
        PositionRoundTimer();
        InitalizeEssenceUi();
        PositionEssenceUi();
        InitalizeCastleMenuUiAddon();
        PositionCastleMenuUiAddon();
        InitalizeLevelCompleteUiAddon();
        InitalizeLevelFailedUiAddon();
        UpdateUiSpeed();
    }

    /// <summary>
    /// Enables the Round Timer UI element.
    /// </summary>
    public void ShowRoundTimer()
    {
        if (roundTimerAddon != null)
        {
            roundTimerAddon.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Disables the Round Timer UI element.
    /// </summary>
    public void HideRoundTimer()
    {
        if (roundTimerAddon != null)
        {
            roundTimerAddon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Instantly updates the round timer's arrow position.
    /// </summary>
    /// <param name="progress">A value between 0 and 1 representing the round progress.</param>
    public void SnapRoundTimer(float progress)
    {
        if (roundTimerAddon != null)
        {
            roundTimerAddon.SnapArrowToPosition(progress);
        }
    }

    /// <summary>
    /// Smoothly moves the round timer's arrow to a new position over a duration.
    /// </summary>
    /// <param name="progress">A value between 0 and 1 representing the round progress.</param>
    /// <param name="duration">The time in seconds over which to move the arrow.</param>
    public void MoveRoundTimer(float progress, float duration = 1f)
    {
        if (roundTimerAddon != null)
        {
            roundTimerAddon.MoveArrowToPosition(progress, duration);
        }
    }

    /// <summary>
    /// Updates the displayed round number.
    /// </summary>
    /// <param name="roundNumber">The current round number.</param>
    public void UpdateRoundNumber(int roundNumber)
    {
        if (roundTimerAddon != null)
        {
            roundTimerAddon.SetRound(roundNumber);
        }
    }
    private void InitalizeRoundTimer()
    {
        if (roundTimerAddon != null)
        {
            roundTimerAddon.SetUiManager(this);
            roundTimerAddon.InitalizeRoundTimerAddon();
        }
    }
    /// <summary>
    /// Positions the round timer in the top-left corner of the screen.
    /// </summary>
    private void PositionRoundTimer()
    {
        if (roundTimerAddon != null)
        {
            RectTransform rt = roundTimerAddon.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(10, -10); // Offset from the top-left corner
            }
        }
    }
    private void InitalizeEssenceUi()
    {
        if (essenceUiAddon != null)
        {
            essenceUiAddon.SetUiManager(this);
            essenceUiAddon.InitalizeEssenceUiAddon();
            UpdateEssenceUi();
        }
    }
    /// <summary>
    /// Positions the round timer in the top-left corner of the screen.
    /// </summary>
    private void PositionEssenceUi()
    {
        if (essenceUiAddon != null)
        {
            RectTransform eu = essenceUiAddon.GetComponent<RectTransform>();
            if (eu != null)
            {
                // Set anchors to the top-right corner
                eu.anchorMin = new Vector2(1, 1);
                eu.anchorMax = new Vector2(1, 1);
                eu.pivot = new Vector2(1, 1);

                // Set position relative to the top-right corner
                eu.anchoredPosition = new Vector2(-60, -10); // Slight offset from the top-right
            }
        }
    }
    public void UpdateEssenceUi()
    {
        if (essenceUiAddon != null)
        {
            int essence = GetGameManager().GetInventoryManager().GetEssence();
            essenceUiAddon.MoveAmountToPosition(essence);
        }
    }
    private void InitalizeLevelCompleteUiAddon()
    {
        if (levelCompleteUiAddon != null)
        {
            levelCompleteUiAddon.SetUiManager(this);
            levelCompleteUiAddon.InitializeLevelCompleteUiAddon();
        }
    }
    public void StartLevelCompleteUI(int stars = 1, int rewards = 350, int bonus = 125, int total = 1300)
    {
        if (levelCompleteUiAddon != null)
        {
            levelCompleteUiAddon.StartLevelCompleteUI(stars,  rewards, bonus, total);
        }
    }
    private void InitalizeLevelFailedUiAddon()
    {
        if (levelFailedUiAddon != null)
        {
            levelFailedUiAddon.SetUiManager(this);
            levelFailedUiAddon.InitializeLevelFailedUiAddon();
        }
    }
    public void StartLevelFailedUI(int remaining = 1)
    {
        if (levelFailedUiAddon != null)
        {
            levelFailedUiAddon.StartLevelFailedUI(remaining);
        }
    }
    private void InitalizeCastleMenuUiAddon()
    {
        if (castleMenuUiAddon != null)
        {
            castleMenuUiAddon.SetUiManager(this);
            castleMenuUiAddon.InitalizeEssenceUiAddon();
        }
    }
    private void PositionCastleMenuUiAddon()
    {
        if (castleMenuUiAddon != null)
        {
            RectTransform cm = castleMenuUiAddon.GetComponent<RectTransform>();
            if (cm != null)
            {
                // Set anchors to the middle-right of the screen
                cm.anchorMin = new Vector2(1, 0.5f);
                cm.anchorMax = new Vector2(1, 0.5f);
                cm.pivot = new Vector2(1, 0.5f); // Set pivot to right-center

                // Set position relative to the middle-right
                cm.anchoredPosition = new Vector2(-60, 0); // Slight offset to the left from the right edge
            }
        }
    }
    public TowerDefenseManager GetGameManager()
    {
        return gameManager;
    }
    public void UpdateUiSpeed()
    {
        // Get game Speed from game manager
        float gameSpeed = (int)GetGameManager().GetGameSpeed();
        // Agument movespeed based on game speed
        float uiSpeed = GetBaseUiSpeed() * ((float)gameSpeed / 2f);
        // Sset final Speed
        SetUiSpeed(uiSpeed);
    }
    public void SetUiSpeed(float newSpeed) => uiSpeed = newSpeed;
    public float GetUiSpeed() => uiSpeed;
    public void SetBaseUiSpeed(float newBaseUiSpeed) => baseUiSpeed = newBaseUiSpeed;
    public float GetBaseUiSpeed() => baseUiSpeed;
}
 