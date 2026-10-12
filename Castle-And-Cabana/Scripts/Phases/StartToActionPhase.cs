using System.Collections;
using UnityEngine;

public class StartToActionPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.StartToAction;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Executing StartToAction phase logic...");
        game.UpdateUiManager();
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Monster, game.GetMonsterManager().StartToActionCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().StartToActionCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Core, game.GetCoreManager().StartToActionCoroutine()));
        game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        game.CheckIfLevelIsCompleted();
        game.SetPhaseStarted(false);
        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.RoundAction;
    }
}
