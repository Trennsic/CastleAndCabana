using System.Collections;
using UnityEngine;

public class LevelStartPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.LevelStart;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Level Start: Initializing Level.");
            game.UpdateUiManager();
            game.SetManagerPhase(TowerDefenseManager.ManagerPhases.Hero);
            game.GetCameraManager().InitializeCameraManager();
            game.GetUiManager().InitializeUiManager();
            game.GetTowerManager().InitializeTower();
            game.GetCastleMenuManager().InitializeCastleMenu();
            game.GetHeroManager().InitializeHeroManager();
            game.GetHeroManager().LevelStart(game.GetLevel());
            game.GetMonsterManager().InitializeMonsterManager();
            game.GetCoreManager().InitializeCoreManager();
            game.GetSelectionManager().InitializeSelectionManager();
            game.GetDecreeManager().InitializeDecreeManager();
            game.UpdateLengthOfLevel();
            game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
            game.SetRound(1);
        }
        game.SetPhaseStarted(false);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());
        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.PlayerInput;
    }
}
