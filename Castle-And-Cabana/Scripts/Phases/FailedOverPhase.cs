using System.Collections;
using UnityEngine;

public class FailedOverPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.FailedOver;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Failed Over: All heroes defeated. Congratulations!");
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Failed Over!");
        }
        game.SetPhaseStarted(false);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());
        // phaseEnded intentionally not set — terminal state
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.LevelStart;
    }
}
