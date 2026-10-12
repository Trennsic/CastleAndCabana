using System.Collections;

public interface IGamePhase
{
    TowerDefenseManager.GamePhases PhaseId { get; }
    IEnumerator Run(TowerDefenseManager game);
    TowerDefenseManager.GamePhases GetNextPhase(TowerDefenseManager game);
}
