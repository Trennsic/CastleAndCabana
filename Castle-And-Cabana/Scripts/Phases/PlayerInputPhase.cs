using System.Collections;
using UnityEngine;

public class PlayerInputPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.PlayerInput;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("PlayerInput!!");
            game.UpdateUiManager();
            game.SetPlayerProceedingToNextPhase(false);
            game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
            game.GetCastleMenuManager().SetCastleMenuState(CastleMenuManager.CastleMenuStates.Main);
        }
        game.SetPhaseStarted(false);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());
        game.CheckIfLevelIsCompleted();
        game.SetPhaseEnded(true);
        if (game.IsDebugging) Debug.Log("PlayerInput phase ended. Transitioning to next phase...");
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.BeforeRound;
    }
}
