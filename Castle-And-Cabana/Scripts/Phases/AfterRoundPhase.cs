using System.Collections;
using UnityEngine;

public class AfterRoundPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.AfterRound;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Executing AfterRound phase logic...");
        game.UpdateUiManager();
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Monster, game.GetMonsterManager().AfterRoundCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Hero, game.GetHeroManager().AfterRoundCoroutine()));
        yield return game.StartCoroutine(game.RunManagerBeat(TowerDefenseManager.ManagerPhases.Core, game.GetCoreManager().AfterRoundCoroutine()));
        game.SetManagerPhase(TowerDefenseManager.ManagerPhases.None);
        game.SetManagerPhase(TowerDefenseManager.ManagerPhases.Tower);
        yield return game.StartCoroutine(game.GetTowerManager().CheckFloorsForEndOfRoundDestruction());
        game.CheckIfLevelIsCompleted();
        game.SetPhaseStarted(false);
        game.SetPhaseEnded(true);
        game.AdvancePhase();
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.PlayerInput;
    }
}
