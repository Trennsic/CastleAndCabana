using System.Collections;
using UnityEngine;

public class LevelCompletePhase : IGamePhase
{
    public TowerDefenseManager.GamePhases PhaseId => TowerDefenseManager.GamePhases.LevelComplete;

    public IEnumerator Run(TowerDefenseManager game)
    {
        if (game.IsDebugging) Debug.Log("Level Complete: All heroes defeated. Congratulations!");
        if (game.GetPhaseStarted())
        {
            if (game.IsDebugging) Debug.Log("Round Action!!");
        }
        game.SetPhaseStarted(false);

        game.GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, 0);
        yield return new WaitForSeconds(2.5f);

        game.GetMonsterManager().AllMonstersVictoryPose();

        float travelTimePerFloor = 2f;
        int totalFloors = game.GetTowerManager().GetFloors().Count;
        for (int i = 0; i < totalFloors; i++)
        {
            game.GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, i);
            yield return new WaitForSeconds(travelTimePerFloor);
        }

        Debug.LogWarning("TIME TRAVELED");

        game.GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedIn, totalFloors - 1);
        game.GetMonsterManager().BossMonsterVictoryPose();
        game.GetUiManager().StartLevelCompleteUI();

        yield return new WaitForSeconds(1.0f);
        yield return game.StartCoroutine(game.WaitForPhaseTimer());

        game.SetPhaseEnded(true);
    }

    public TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game)
    {
        return TowerDefenseManager.GamePhases.CompleteOver;
    }
}
