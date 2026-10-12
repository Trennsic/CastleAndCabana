using System.Collections;
using UnityEngine;

public class LevelFailedPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.LevelFailed;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Level Failed: Heroes reached the top of the tower.");
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Level Failed!!");
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().LevelFailedCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Tower, game.GetTowerManager().LevelFailedCoroutine()));
            game.GetUiManager().StartLevelFailedUI();
            game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        }
        game.SetPhaseStarted(false);
        yield return new WaitForSeconds(1.0f);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());
        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.FailedOver;
    }
}
