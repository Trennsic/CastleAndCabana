using System.Collections;
using UnityEngine;

public class RoundEndPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.RoundEnd;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Round End!!");
            game.UpdateUiManager();
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Monster, game.GetMonsterManager().RoundEndCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().RoundEndCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Boss, game.GetMonsterManager().BossRoundEndCoroutine()));
            game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        }
        game.SetPhaseStarted(false);
        yield return new WaitForSeconds(1.0f);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());
        if (!game.GetPhaseEnded())
        {
            game.CheckIfLevelIsCompleted();
            game.GetTowerManager().ResetTraveledThisTurn();
        }
        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.AfterRound;
    }
}
