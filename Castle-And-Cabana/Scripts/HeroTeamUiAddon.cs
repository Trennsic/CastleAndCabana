using UnityEngine;
using TMPro;

public class HeroTeamUiAddon : MonoBehaviour
{
    #region // Variables
    public enum HeroTeamUiStates { Off, Black, Moving, Combat, Waiting }

    [SerializeField] private GameObject placeHolder;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject waitingIcon;
    [SerializeField] private GameObject combatIcon;
    [SerializeField] private GameObject movingIcon;
    [SerializeField] private TextMeshProUGUI uiText;

    private HeroTeamUiStates currentState;
    #endregion
    #region // Functions
    #region // Unity Functions

    #endregion
    #region // User Functions
    private void UpdateUiState()
    {
        HeroTeamUiStates state = GetState();

        bool isBackgroundOn = (state != HeroTeamUiStates.Off);
        bool isMoving = (state == HeroTeamUiStates.Moving);
        bool isCombat = (state == HeroTeamUiStates.Combat);
        bool isWaiting = (state == HeroTeamUiStates.Waiting);

        GetUiCanvas().enabled = isBackgroundOn;
        GetMovingIcon().SetActive(isMoving);
        GetCombatIcon().SetActive(isCombat);
        GetWaitingIcon().SetActive(isWaiting);
    }
    #region // Getters and Setters 
    public void SetUiState(HeroTeamUiStates newState) { SetState(newState); UpdateUiState(); }
    public HeroTeamUiStates GetUiState() => currentState;
    private void SetState(HeroTeamUiStates newState) => currentState = newState;
    private HeroTeamUiStates GetState() => currentState;
    private void SetPlaceHolder(GameObject newPlaceHolder) => placeHolder = newPlaceHolder;
    private GameObject GetPlaceHolder() => placeHolder;

    private void SetUiCanvas(Canvas newUiCanvas) => uiCanvas = newUiCanvas;
    private Canvas GetUiCanvas() => uiCanvas;

    private void SetWaitingIcon(GameObject newWaitingIcon) => waitingIcon = newWaitingIcon;
    private GameObject GetWaitingIcon() => waitingIcon;

    private void SetCombatIcon(GameObject newCombatIcon) => combatIcon = newCombatIcon;
    private GameObject GetCombatIcon() => combatIcon;

    private void SetMovingIcon(GameObject newMovingIcon) => movingIcon = newMovingIcon;
    private GameObject GetMovingIcon() => movingIcon;

    private void SetUiText(TextMeshProUGUI newUiText) => uiText = newUiText;
    private TextMeshProUGUI GetUiText() => uiText;
    #endregion
    #endregion
    #endregion
}
