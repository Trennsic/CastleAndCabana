using System.Collections;
using UnityEngine;

public class RoundStartPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.RoundStart;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Round Start!!");
            game.UpdateUiManager();
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Monster, game.GetMonsterManager().RoundStartCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().RoundStartCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Boss, game.GetMonsterManager().BossRoundStartCoroutine()));
            yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Core, game.GetCoreManager().RoundStartCoroutine()));
            game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        }
        game.SetPhaseStarted(false);
        if (game.IsDebugging) Debug.Log("RoundStart phase actions completed. Preparing transition...");
        yield return new WaitForSeconds(1.0f);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());
        game.CheckIfLevelIsCompleted();
        game.SetPhaseEnded(true);
        if (game.IsDebugging) Debug.Log("RoundStart phase ended. Transitioning to next phase...");
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.StartToAction;
    }
}
