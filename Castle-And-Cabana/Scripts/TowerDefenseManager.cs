using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TowerDefenseManager : MonoBehaviour
{
    public InputManager inputManager;
    public HeroManager heroManager;
    public MonsterManager monsterManager;
    public CoreManager coreManager;
    public TowerManager towerManager;
    public CameraManager cameraManager;
    public CastleMenuManager castleMenuManager;
    public InventoryManager inventoryManager;
    public SelectionManager selectionManager;
    public DecreeManager decreeManager;
    public UiManager uiManager;
    public GameObject Tower;

    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentRound = 1;
    private int amtOfPhases;
    [SerializeField] private float minimumPhaseTimerInit = 1f;
    private float minimumPhaseTimer = 1f;
    [SerializeField] private bool phaseSnapToEnd;

    [SerializeField] private bool hasFailedLevel;
    [SerializeField] private bool hasCompletedLevel;

    [SerializeField] private bool reachedEndOfPhases;
    [SerializeField] private bool playerProceedingToNextPhase;
    [SerializeField] private bool holdPhase;

    public enum GameSpeed { Stopped = 0, Slow = 1, Normal = 2, Fast = 3, VeryFast = 4, MaxSpeed = 8 }

    [SerializeField] private GameSpeed currentGameSpeed = GameSpeed.Normal;

    private bool phaseStarted;
    private bool phaseEnded;

    private Dictionary<GamePhases, IGamePhase> _phases;

    #region // Debugging
    [Header("Debugging")]
    [SerializeField] private DebugUtility debug = new DebugUtility(false);
    #endregion

    public bool IsDebugging => debug.IsDebugging;

    public enum GamePhases
    {
        LevelStart      ,
        PlayerInput     ,
        BeforeRound     ,
        RoundStart      ,
        StartToAction   ,
        RoundAction     ,
        ActionToEnd     ,
        RoundEnd        ,
        AfterRound      ,
        LevelComplete   ,
        CompleteOver    ,
        LevelFailed     ,
        FailedOver
    }
    public enum ManagerPhases
    {
        None    ,
        Hero    ,
        Monster ,
        Core    ,
        Boss    ,
        Tower   ,
        Player
    }

    private float rKeyHoldTime = 0f;
    private const float restartTimeThreshold = 1f;

    private GamePhases gamePhase;
    private ManagerPhases managerPhase;

    private float escapeKeyHoldTime = 0f;
    private const float closeGameThreshold = 3f;

    private void Awake()
    {
        _phases = new Dictionary<GamePhases, IGamePhase>();
        RegisterPhase(new LevelStartPhase());
        RegisterPhase(new PlayerInputPhase());
        RegisterPhase(new BeforeRoundPhase());
        RegisterPhase(new RoundStartPhase());
        RegisterPhase(new StartToActionPhase());
        RegisterPhase(new RoundActionPhase());
        RegisterPhase(new ActionToEndPhase());
        RegisterPhase(new RoundEndPhase());
        RegisterPhase(new AfterRoundPhase());
        RegisterPhase(new LevelCompletePhase());
        RegisterPhase(new CompleteOverPhase());
        RegisterPhase(new LevelFailedPhase());
        RegisterPhase(new FailedOverPhase());
    }

    private void RegisterPhase(IGamePhase phase)
    {
        _phases[phase.PhaseId] = phase;
    }

    private void Start()
    {
        GameStart();
    }

    private void Update()
    {
        UpdateGameSpeed();
        CheckForPlayerInput();
        CheckForNextPhase();
        UpdatePhaseTimer();
        CheckForCloseGame();

        debug.SetWatch("Game Phase", gamePhase);
        debug.SetWatch("Manager Phase", managerPhase);

        if (phaseText != null) phaseText.text = $"Current Phase: {GetGamePhase()}";
        if (timerText != null) timerText.text = $"Time until next phase: {Mathf.CeilToInt(minimumPhaseTimer)}s";
    }

    private void CheckForPlayerInput()
    {
        #region // Check if the Z key is pressed
        if (GetInputManager().GetAction())
        {
            if (GetTowerManager().GetPlacingState() == TowerManager.PlacingFloorStates.NotPlacing && GetTowerManager().GetPlacingFloorCd() <= 0)
            {
                GetTowerManager().StartPlacingFloor();
            }
        }
        #endregion

        #region // Testing
        if (GetInputManager().GetSecondaryPressed())
        {
            GetSelectionManager().StartTowerOverview();
        }
        #endregion

        #region // Check if the R key is held down
        if (UnityEngine.InputSystem.Keyboard.current.rKey.isPressed)
        {
            rKeyHoldTime += Time.deltaTime;
            if (rKeyHoldTime >= restartTimeThreshold)
            {
                RestartGame();
            }
        }
        else
        {
            rKeyHoldTime = 0f;
        }
        #endregion
    }

    private void CheckForCloseGame()
    {
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.isPressed)
        {
            escapeKeyHoldTime += Time.deltaTime;
            if (escapeKeyHoldTime >= closeGameThreshold)
            {
                Debug.Log("Closing the game...");
                Application.Quit();
            }
        }
        else
        {
            escapeKeyHoldTime = 0f;
        }
    }

    public bool GetReadyToStartRound()
    {
        return (minimumPhaseTimer <= 0 && !GetHoldPhase());
    }

    public void PlayerInputStartingRound()
    {
        TransitionToPhase(ResolveNextPhase());
    }

    private void CheckForNextPhase()
    {
        if (GetGamePhase() == GamePhases.PlayerInput) return;
        if (minimumPhaseTimer > 0 || !phaseEnded) return;
        TransitionToPhase(ResolveNextPhase());
    }

    private void RestartGame()
    {
        if (debug.IsDebugging) Debug.Log("Restarting the game...");
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public GamePhases ResolveNextPhase()
    {
        if (GetHasFailedLevel() && gamePhase != GamePhases.LevelFailed) return GamePhases.LevelFailed;
        if (GetHasCompletedLevel() && gamePhase != GamePhases.LevelComplete) return GamePhases.LevelComplete;

        if (_phases.TryGetValue(gamePhase, out IGamePhase current))
            return current.GetNextPhase(this);

        return GamePhases.LevelStart;
    }

    protected void GameStart()
    {
        if (towerManager == null) { Debug.LogError("Tower manager Component Not setup. Cannot Continue"); return; }
        if (heroManager == null) { Debug.LogError("Hero manager Component Not setup. Cannot Continue"); return; }

        currentLevel = 1;
        GetInventoryManager().SetEssence(500);
        SetManagerPhase(ManagerPhases.None);
        TransitionToPhase(GamePhases.LevelStart);
    }

    public void TransitionToPhase(GamePhases phase)
    {
        SetGamePhase(phase);
        phaseStarted = true;
        phaseEnded = false;
        SetPhaseTimer(phase);

        if (_phases.TryGetValue(phase, out IGamePhase phaseInstance))
        {
            StartCoroutine(phaseInstance.Run(this));
        }
    }

    private void SetPhaseTimer(GamePhases phase)
    {
        minimumPhaseTimer = minimumPhaseTimerInit;
    }

    private void UpdatePhaseTimer()
    {
        if (minimumPhaseTimer > 0) { minimumPhaseTimer -= Time.deltaTime; }
        else { minimumPhaseTimer = 0; }
    }

    public IEnumerator WaitForPhaseTimer()
    {
        while (minimumPhaseTimer > 0)
            yield return null;
    }

    public IEnumerator RunManagerBeat(ManagerPhases phase, IEnumerator routine)
    {
        SetManagerPhase(phase);
        yield return StartCoroutine(routine);
    }

    public void UpdateLengthOfLevel()
    {
        Dictionary<int, List<List<NewHero>>> levelPhases = heroManager.GetLevelPhases();
        amtOfPhases = levelPhases[currentLevel].Count;
        if (debug.IsDebugging) Debug.Log($"Amount of Phases = {amtOfPhases} ");
    }

    public void CheckIfLevelIsCompleted()
    {
        if (GetReachedEndOfPhases() && GetHeroManager().GetAllHeroTeamsDefeated())
        {
            SetHasCompletedLevel(true);
        }
    }

    public void UpdateUiManager()
    {
        if (GetUiManager() == null) return;
        UiManager uiManager = GetUiManager();

        GamePhases gamePhase = GetGamePhase();
        float phasePosition;

        switch (gamePhase)
        {
            case GamePhases.PlayerInput:    phasePosition = 0f;     break;
            case GamePhases.RoundStart:     phasePosition = .25f;   break;
            case GamePhases.RoundAction:    phasePosition = .5f;    break;
            case GamePhases.RoundEnd:       phasePosition = .75f;   break;
            case GamePhases.BeforeRound:    phasePosition = .125f;  break;
            case GamePhases.StartToAction:  phasePosition = .375f;  break;
            case GamePhases.ActionToEnd:    phasePosition = .625f;  break;
            case GamePhases.AfterRound:     phasePosition = .875f;  break;
            default: phasePosition = 0f; break;
        }

        uiManager.MoveRoundTimer(phasePosition);
        uiManager.UpdateRoundNumber(GetRound());
    }

    public void AdvancePhase()
    {
        if (GetRound() < amtOfPhases)
        {
            SetRound(GetRound() + 1);
        }
        if (GetRound() >= amtOfPhases) { SetReachedEndOfPhases(true); }
    }

    private void UpdateGameSpeed()
    {
        GetHeroManager().UpdateHeroesSpeeds();
        GetMonsterManager().UpdateMonstersSpeeds();
        GetCameraManager().UpdateCameraSpeed();
        GetUiManager().UpdateUiSpeed();
    }

    public bool GetPhaseStarted() => phaseStarted;
    public void SetPhaseStarted(bool v) => phaseStarted = v;
    public bool GetPhaseEnded() => phaseEnded;
    public void SetPhaseEnded(bool v) => phaseEnded = v;

    public InputManager GetInputManager() => inputManager;
    public TowerManager GetTowerManager() => towerManager;
    public HeroManager GetHeroManager() => heroManager;
    public MonsterManager GetMonsterManager() => monsterManager;
    public CoreManager GetCoreManager() => coreManager;
    public CameraManager GetCameraManager() => cameraManager;
    public CastleMenuManager GetCastleMenuManager() => castleMenuManager;
    public InventoryManager GetInventoryManager() => inventoryManager;
    public UiManager GetUiManager() => uiManager;
    public SelectionManager GetSelectionManager() => selectionManager;
    public DecreeManager GetDecreeManager() => decreeManager;
    public int GetLevel() => currentLevel;
    public int GetRound() => currentRound;
    public void SetHoldPhase(bool newHoldPhase) => holdPhase = newHoldPhase;
    public bool GetHoldPhase() => holdPhase;
    public void SetGamePhase(GamePhases newGamePhase) => gamePhase = newGamePhase;
    public GamePhases GetGamePhase() => gamePhase;
    public void SetPlayerProceedingToNextPhase(bool newPlayerProceedingToNextPhase) => playerProceedingToNextPhase = newPlayerProceedingToNextPhase;
    public bool GetPlayerProceedingToNextPhase() => playerProceedingToNextPhase;
    public void SetRound(int newRound) => currentRound = (Mathf.Clamp(newRound, 1, amtOfPhases));
    public void SetHasFailedLevel(bool newHasFailedLevel) => hasFailedLevel = newHasFailedLevel;
    public bool GetHasFailedLevel() => hasFailedLevel;
    public void SetReachedEndOfPhases(bool newReachedEndOfPhases) => reachedEndOfPhases = newReachedEndOfPhases;
    public bool GetReachedEndOfPhases() => reachedEndOfPhases;
    public void SetHasCompletedLevel(bool newHasCompletedLevel) => hasCompletedLevel = newHasCompletedLevel;
    public bool GetHasCompletedLevel() => hasCompletedLevel;
    public void SetManagerPhase(ManagerPhases newManagerPhase) => managerPhase = newManagerPhase;
    public ManagerPhases GetManagerPhase() => managerPhase;
    public void SetGameSpeed(GameSpeed speed) { currentGameSpeed = speed; UpdateGameSpeed(); }
    public GameSpeed GetGameSpeed() { return currentGameSpeed; }
}
