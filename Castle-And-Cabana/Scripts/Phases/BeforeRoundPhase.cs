using System.Collections;
using UnityEngine;

public class BeforeRoundPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.BeforeRound;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Executing BeforeRound phase logic...");
        game.UpdateUiManager();
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Monster, game.GetMonsterManager().BeforeRoundCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().BeforeRoundCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Core, game.GetCoreManager().BeforeRoundCoroutine()));
        game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        game.SetPhaseStarted(false);
        if (!game.GetPhaseEnded())
        {
            game.CheckIfLevelIsCompleted();
        }
        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.RoundStart;
    }
}
