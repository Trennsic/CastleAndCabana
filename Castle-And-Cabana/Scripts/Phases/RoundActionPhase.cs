using System.Collections;
using UnityEngine;

public class RoundActionPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.RoundAction;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Round Action: Heroes take actions.");
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Round Action!!");
            game.UpdateUiManager();
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Monster, game.GetMonsterManager().RoundActionCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().RoundActionCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Boss, game.GetMonsterManager().BossRoundActionCoroutine()));
            game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        }
        game.SetPhaseStarted(false);
        yield return new WaitForSeconds(1.0f);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());
        game.CheckIfLevelIsCompleted();
        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.ActionToEnd;
    }
}
