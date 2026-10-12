using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using static TowerDefenseManager;

public class RoundTimerAddon : MonoBehaviour
{
    [SerializeField] private GameObject roundArrow;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI gameSpeedText; // New reference for displaying game speed
    [SerializeField] private Button leftGameSpeedArrow; // New button reference for left arrow
    [SerializeField] private Button rightGameSpeedArrow; // New button reference for right arrow
    [SerializeField] private UiManager uiManager;
    

    [SerializeField][Range(0f, 1f)] private float arrowPosition = 0f;

    private int currentRound = 1;
    private Coroutine moveArrowCoroutine;

    #region // Debugging
    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;
    #endregion

    void Start()
    {
        InitalizeRoundTimerAddon();
    }

    void Update()
    {
        UpdateArrowRotation();
    }
    public void InitalizeRoundTimerAddon()
    {
        // Initialize button listeners for adjusting game speed
        if (leftGameSpeedArrow != null)
        {
            if (debug.IsDebugging) Debug.Log("Assigning listener to Left Game Speed Arrow.");
            leftGameSpeedArrow.onClick.AddListener(DecreaseGameSpeed);
        }
        else
        {
            if (debug.IsDebugging) Debug.LogWarning("Left Game Speed Arrow is not assigned.");
        }

        if (rightGameSpeedArrow != null)
        {
            if (debug.IsDebugging) Debug.Log("Assigning listener to Right Game Speed Arrow.");
            rightGameSpeedArrow.onClick.AddListener(IncreaseGameSpeed);
        }
        else
        {
            if (debug.IsDebugging) Debug.LogWarning("Right Game Speed Arrow is not assigned.");
        }
        gameObject.SetActive(true);
        // Update the game speed text initially
        if (debug.IsDebugging) Debug.Log("Updating Game Speed Text.");
        UpdateGameSpeedText();
    }
    private void InitalizeDebug() { debug = new DebugUtility(false); }


    public void SnapArrowToPosition(float target)
    {
        target = Mathf.Clamp01(target);

        // Stop any ongoing movement before snapping
        if (moveArrowCoroutine != null)
        {
            StopCoroutine(moveArrowCoroutine);
            moveArrowCoroutine = null;
        }

        arrowPosition = target;
        UpdateArrowRotation();
    }

    public void MoveArrowToPosition(float target, float duration)
    {
        target = Mathf.Clamp01(target);

        // Stop any currently running coroutine before starting a new one
        if (moveArrowCoroutine != null)
        {
            StopCoroutine(moveArrowCoroutine);
            moveArrowCoroutine = null;
        }

        if (gameObject.activeInHierarchy)
        {
            moveArrowCoroutine = StartCoroutine(SmoothMoveArrow(target, duration));
        }
        else
        {
            Debug.LogWarning("Cannot start coroutine because the GameObject is inactive.");
        }
    }


    public void SetRound(int roundNumber)
    {
        currentRound = Mathf.Max(1, roundNumber); // Ensures round is at least 1
        UpdateRoundText();
    }

    private IEnumerator SmoothMoveArrow(float target, float duration)
    {
        float start = arrowPosition;
        float timeElapsed = 0f;

        // Find shortest path for rotation (clockwise or counter-clockwise)
        float delta = target - start;

        // Normalize delta to the range of [-1, 1], where -1 is counterclockwise and 1 is clockwise
        if (delta > 0.5f)
        {
            delta -= 1f; // Move counter-clockwise
        }
        else if (delta < -0.5f)
        {
            delta += 1f; // Move clockwise
        }

        // Smoothly move towards the target position
        while (timeElapsed < duration)
        {
            arrowPosition = Mathf.Lerp(start, start + delta, timeElapsed / duration);

            // Ensure the value is clamped
            arrowPosition = Mathf.Clamp01(arrowPosition);

            UpdateArrowRotation();
            timeElapsed += (Time.deltaTime * GetUiManager().GetUiSpeed());
            yield return null; 
        }

        arrowPosition = target;
        UpdateArrowRotation();

        // Reset coroutine reference after completion
        moveArrowCoroutine = null;
    }

    private void UpdateArrowRotation()
    {
        if (roundArrow != null)
        {
            float rotationZ = -arrowPosition * 360f;
            roundArrow.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }
    }

    private void UpdateRoundText()
    {
        if (roundText != null)
        {
            roundText.text = "Round: " + currentRound;
        }
    }

    public void UpdateGameSpeedText()
    {
        if (GetUiManager() == null) return;
        if (GetUiManager().GetGameManager() == null) return;
        if (gameSpeedText != null)
        {
            // Get the current game speed from the GameManager and display it
            GameSpeed currentSpeed = GetUiManager().GetGameManager().GetGameSpeed();
            gameSpeedText.text = "x " + ((float)currentSpeed / 2f).ToString();
        }
    }

    public void DecreaseGameSpeed()
    {
        if (debug.IsDebugging) Debug.Log("Decrease game speed clicked"); // Debugging line
        if (GetUiManager() == null) return;
        if (GetUiManager().GetGameManager() == null) return;

        GameSpeed currentSpeed = GetUiManager().GetGameManager().GetGameSpeed();
        GameSpeed newSpeed = (GameSpeed)Mathf.Max((int)currentSpeed - 1, 0);
        GetUiManager().GetGameManager().SetGameSpeed(newSpeed);
        UpdateGameSpeedText();
    }

    public void IncreaseGameSpeed()
    {
        if (debug.IsDebugging) Debug.Log("Increase game speed clicked"); // Debugging line
        if (GetUiManager() == null) return;
        if (GetUiManager().GetGameManager() == null) return;

        GameSpeed currentSpeed = GetUiManager().GetGameManager().GetGameSpeed();
        GameSpeed newSpeed = (GameSpeed)Mathf.Min((int)currentSpeed + 1, (int)GameSpeed.MaxSpeed);
        GetUiManager().GetGameManager().SetGameSpeed(newSpeed);
        UpdateGameSpeedText();
    }


    // A getter function for the GameManager
    public void SetUiManager(UiManager newUiManager) { uiManager = newUiManager; }  
    private UiManager GetUiManager() { return uiManager; }  
}
