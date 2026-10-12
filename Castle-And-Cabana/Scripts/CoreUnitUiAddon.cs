using UnityEngine;
using TMPro;

public class CoreUnitUiAddon : MonoBehaviour
{
    #region // Variables
    [SerializeField] private CoreUnit coreUnit;
    [SerializeField] private bool isLocked;
    [SerializeField] private GameObject placeHolder;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private TextMeshProUGUI lockText;
    [SerializeField] private GameObject healthIcon;
    [SerializeField] private TextMeshProUGUI healthText;
    #endregion
    #region // Functions
    #region // Unity Functions

    #endregion
    #region // User Functions
    public void UpdateCoreUi()
    {
        // For Health 
        GameObject healthIcon = GetHealthIcon();
        TextMeshProUGUI healthText = GetHealthText();
        //  Update the Health text to text
        bool healthOn = true;
        GetHealthIcon().SetActive(healthOn);
        healthText.text = GetCoreUnit().GetCoreInfo().GetHealth().ToString();
        // For Lock Timer
        GameObject lockIcon = GetLockIcon();
        TextMeshProUGUI lockText = GetLockText();
        // See if the locktime is > 0
        int lockTime = GetCoreUnit().GetCoreInfo().GetLockTime();

        if (lockTime <= 0) 
            lockIcon.SetActive(false);
        else 
            lockIcon.SetActive(true);

        lockText.text = lockTime.ToString();
        // If so turn on the lock icon
        // Update the lock text
        //HeroTeamUiStates state = GetState();
        //
        //bool isBackgroundOn = (state != HeroTeamUiStates.Off);
        //bool isMoving = (state == HeroTeamUiStates.Moving);
        //bool isHealth = (state == HeroTeamUiStates.Health);
        //bool isWaiting = (state == HeroTeamUiStates.Waiting);

        //GetUiCanvas().enabled = isBackgroundOn;
        //GetMovingIcon().SetActive(isMoving);
        //GetHealthIcon().SetActive(isHealth);
        //GetWaitingIcon().SetActive(isWaiting);
    }
    #region // Getters and Setters 
    //public void SetUiState(HeroTeamUiStates newState) { SetState(newState); UpdateUiState(); }
    //public HeroTeamUiStates GetUiState() => currentState;
    //private void SetState(HeroTeamUiStates newState) => currentState = newState;
    //private HeroTeamUiStates GetState() => currentState;
    private void SetPlaceHolder(GameObject newPlaceHolder) => placeHolder = newPlaceHolder;
    private GameObject GetPlaceHolder() => placeHolder;

    private void SetUiCanvas(Canvas newUiCanvas) => uiCanvas = newUiCanvas;
    private Canvas GetUiCanvas() => uiCanvas;

    private void SetLockIcon(GameObject newLockedIcon) => lockIcon = newLockedIcon;
    private GameObject GetLockIcon() => lockIcon;

    private void SetHealthIcon(GameObject newHealthIcon) => healthIcon = newHealthIcon;
    private GameObject GetHealthIcon() => healthIcon;
    private void SetHealthText(TextMeshProUGUI newHealthText) => healthText = newHealthText;
    public TextMeshProUGUI GetHealthText() => healthText;
    private void SetLockText(TextMeshProUGUI newLockText) => lockText = newLockText;
    public TextMeshProUGUI GetLockText() => lockText;
    public void SetCoreUnit(CoreUnit newCoreUnit) => coreUnit = newCoreUnit;
    public CoreUnit GetCoreUnit() => coreUnit;
    #endregion
    #endregion
    #endregion
}
