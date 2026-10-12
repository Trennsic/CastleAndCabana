using System.Collections;
using UnityEngine;

public class CompleteOverPhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.CompleteOver;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Complete Over: All heroes defeated. Congratulations!");
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Complete Over!");
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
