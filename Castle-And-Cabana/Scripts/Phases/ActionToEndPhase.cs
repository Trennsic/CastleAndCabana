using System.Collections;
using UnityEngine;

public class ActionToEndPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.ActionToEnd;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Executing ActionToEnd phase logic...");
        game.UpdateUiManager();
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Monster, game.GetMonsterManager().ActionToEndCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().ActionToEndCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Core, game.GetCoreManager().ActionToEndCoroutine()));
        game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        game.CheckIfLevelIsCompleted();
        game.SetPhaseStarted(false);
        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.RoundEnd;
    }
}
