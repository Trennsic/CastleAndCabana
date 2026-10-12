using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    #region /// Attributes
    public enum SelectionStates
    {
        Inactive,
        Locked,
        TowerOverview,
        FloorBrowse,
        FloorSelected,
        HeroFocus,
        MonsterFocus,
        MonsterInspect
    }

    public enum FloorGroup
    {
        Monsters,
        Heroes,
        Core,
        Floor
    }

    [SerializeField] private TowerDefenseManager GameManager;
    [SerializeField] private SelectionStates currentState = SelectionStates.Inactive;
    [SerializeField] private int selectedFloor = 1;
    [SerializeField] private int selectedSlot = 0;
    [SerializeField] private FloorGroup selectedGroup = FloorGroup.Monsters;
    #endregion

    #region /// Methods
    #region // Unity Functions
    private void Update()
    {
        UpdateSelections();
    }
    #endregion

    #region // Initialization
    public void InitializeSelectionManager()
    {
        currentState = SelectionStates.Inactive;
        selectedFloor = 1;
    }
    #endregion

    #region // State Dispatch
    private void UpdateSelections()
    {
        switch (currentState)
        {
            case SelectionStates.TowerOverview:  UpdateTowerOverview();  break;
            case SelectionStates.FloorBrowse:    UpdateFloorBrowse();    break;
            case SelectionStates.FloorSelected:  UpdateFloorSelected();  break;
            case SelectionStates.HeroFocus:      UpdateHeroFocus();      break;
            case SelectionStates.MonsterFocus:   UpdateMonsterFocus();   break;
            case SelectionStates.MonsterInspect: UpdateMonsterInspect(); break;
        }
    }
    #endregion

    #region // Tower Overview
    public void StartTowerOverview()
    {
        currentState = SelectionStates.TowerOverview;
        GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.FullTower, selectedFloor);
    }

    private void UpdateTowerOverview()
    {
        if (GetInputManager().GetMoveUpPressed() || GetInputManager().GetMoveDownPressed())
        {
            StartFloorBrowse(Mathf.Max(selectedFloor, 1));
            return;
        }

        if (GetInputManager().GetCancelPressed())
            ReturnToCastleMenu();
    }
    #endregion

    #region // Floor Browse
    public void StartFloorBrowse(int floor)
    {
        currentState = SelectionStates.FloorBrowse;
        selectedFloor = Mathf.Clamp(floor, 0, GetTowerManager().GetFloorCount());
        GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, selectedFloor);
        GetTowerManager().TurnOnFloorSelectionGlows(selectedFloor);
    }

    private void UpdateFloorBrowse()
    {
        TowerManager tm = GetTowerManager();

        if (GetInputManager().GetMoveUpPressed())
        {
            selectedFloor = Mathf.Clamp(selectedFloor + 1, 0, tm.GetFloorCount());
            GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, selectedFloor);
            tm.TurnOnFloorSelectionGlows(selectedFloor);
            return;
        }

        if (GetInputManager().GetMoveDownPressed())
        {
            selectedFloor = Mathf.Clamp(selectedFloor - 1, 0, tm.GetFloorCount());
            GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedOut, selectedFloor);
            tm.TurnOnFloorSelectionGlows(selectedFloor);
            return;
        }

        if (GetInputManager().GetActionPressed())
        {
            StartFloorSelected(selectedFloor);
            return;
        }

        if (GetInputManager().GetCancelPressed())
            ReturnToCastleMenu();
    }
    #endregion

    #region // Floor Selected
    public void StartFloorSelected(int floor)
    {
        currentState = SelectionStates.FloorSelected;
        selectedFloor = Mathf.Clamp(floor, 0, GetTowerManager().GetFloorCount());
        selectedGroup = FloorGroup.Monsters;
        GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedIn, selectedFloor);
    }

    private void UpdateFloorSelected()
    {
        TowerManager tm = GetTowerManager();

        if (GetInputManager().GetMoveUpPressed())
        {
            selectedFloor = Mathf.Clamp(selectedFloor + 1, 0, tm.GetFloorCount());
            GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedIn, selectedFloor);
            return;
        }

        if (GetInputManager().GetMoveDownPressed())
        {
            selectedFloor = Mathf.Clamp(selectedFloor - 1, 0, tm.GetFloorCount());
            GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.ZoomedIn, selectedFloor);
            return;
        }

        if (GetInputManager().GetMoveRightPressed())
        {
            selectedGroup = (FloorGroup)(((int)selectedGroup + 1) % 4);
            return;
        }

        if (GetInputManager().GetMoveLeftPressed())
        {
            int prev = (int)selectedGroup - 1;
            selectedGroup = (FloorGroup)(prev < 0 ? 3 : prev);
            return;
        }

        if (GetInputManager().GetActionPressed())
        {
            switch (selectedGroup)
            {
                case FloorGroup.Monsters: StartMonsterFocus(selectedFloor); break;
                case FloorGroup.Heroes:   StartHeroFocus(selectedFloor);    break;
                case FloorGroup.Core:     break; // future
                case FloorGroup.Floor:    break; // future
            }
            return;
        }

        if (GetInputManager().GetCancelPressed())
            StartFloorBrowse(selectedFloor);
    }
    #endregion

    #region // Hero Focus
    public void StartHeroFocus(int floor)
    {
        TowerManager tm = GetTowerManager();
        TowerSectionAddon floorAddon = tm.GetTowerSectionAddon(tm.GetFloor(floor));
        HeroSlotsAddon heroSlots = floorAddon.GetHeroSlots();

        int firstSlot = FindFirstOccupiedHeroSlot(heroSlots);
        if (firstSlot == 0)
        {
            StartFloorSelected(floor);
            return;
        }

        currentState = SelectionStates.HeroFocus;
        selectedFloor = floor;
        selectedSlot = firstSlot;
        GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.HeroZoom, selectedFloor);
        floorAddon.SetHeroSelectionNumber(selectedSlot);
    }

    private void UpdateHeroFocus()
    {
        TowerManager tm = GetTowerManager();
        TowerSectionAddon floorAddon = tm.GetTowerSectionAddon(tm.GetFloor(selectedFloor));
        HeroSlotsAddon heroSlots = floorAddon.GetHeroSlots();

        if (GetInputManager().GetMoveRightPressed())
        {
            int next = FindNextOccupiedHeroSlot(heroSlots, selectedSlot, 1);
            if (next != 0)
            {
                floorAddon.SetHeroSelectionNumber(0);
                selectedSlot = next;
                floorAddon.SetHeroSelectionNumber(selectedSlot);
            }
            return;
        }

        if (GetInputManager().GetMoveLeftPressed())
        {
            int next = FindNextOccupiedHeroSlot(heroSlots, selectedSlot, -1);
            if (next != 0)
            {
                floorAddon.SetHeroSelectionNumber(0);
                selectedSlot = next;
                floorAddon.SetHeroSelectionNumber(selectedSlot);
            }
            return;
        }

        if (GetInputManager().GetCancelPressed())
        {
            floorAddon.SetHeroSelectionNumber(0);
            StartFloorSelected(selectedFloor);
        }
    }

    private int FindFirstOccupiedHeroSlot(HeroSlotsAddon heroSlots)
    {
        for (int i = 1; i <= 4; i++)
        {
            if (heroSlots.GetHeroInSlot(i) != null)
                return i;
        }
        return 0;
    }

    private int FindNextOccupiedHeroSlot(HeroSlotsAddon heroSlots, int current, int direction)
    {
        for (int count = 0; count < 4; count++)
        {
            current += direction;
            if (current < 1) current = 4;
            if (current > 4) current = 1;
            if (heroSlots.GetHeroInSlot(current) != null)
                return current;
        }
        return 0;
    }
    #endregion

    #region // Monster Focus
    public void StartMonsterFocus(int floor)
    {
        currentState = SelectionStates.MonsterFocus;
        selectedFloor = floor;
        selectedSlot = 1;
        GetCameraManager().SetCameraStateAndPosition(CameraManager.CameraStates.MonsterZoom, selectedFloor);

        TowerManager tm = GetTowerManager();
        TowerSectionAddon floorAddon = tm.GetTowerSectionAddon(tm.GetFloor(floor));
        floorAddon.SetMonsterSelectionNumber(selectedSlot);
    }

    private void UpdateMonsterFocus()
    {
        TowerManager tm = GetTowerManager();
        TowerSectionAddon floorAddon = tm.GetTowerSectionAddon(tm.GetFloor(selectedFloor));
        MonsterSlotsAddon monsterSlots = floorAddon.GetMonsterSlots();

        if (GetInputManager().GetMoveRightPressed())
        {
            floorAddon.SetMonsterSelectionNumber(0);
            selectedSlot = selectedSlot >= 4 ? 1 : selectedSlot + 1;
            floorAddon.SetMonsterSelectionNumber(selectedSlot);
            return;
        }

        if (GetInputManager().GetMoveLeftPressed())
        {
            floorAddon.SetMonsterSelectionNumber(0);
            selectedSlot = selectedSlot <= 1 ? 4 : selectedSlot - 1;
            floorAddon.SetMonsterSelectionNumber(selectedSlot);
            return;
        }

        if (GetInputManager().GetActionPressed())
        {
            MonsterUnit monster = monsterSlots.GetMonsterInSlot(selectedSlot);
            if (monster == null)
            {
                currentState = SelectionStates.Locked;
                GetCastleMenuManager().StartSummonMonsterForSlot(selectedFloor, selectedSlot);
            }
            else
            {
                StartMonsterInspect(selectedFloor, selectedSlot);
            }
            return;
        }

        if (GetInputManager().GetCancelPressed())
        {
            floorAddon.SetMonsterSelectionNumber(0);
            StartFloorSelected(selectedFloor);
        }
    }
    #endregion

    #region // Monster Inspect
    public void StartMonsterInspect(int floor, int slot)
    {
        currentState = SelectionStates.MonsterInspect;
        selectedFloor = floor;
        selectedSlot = slot;
        // Camera stays at MonsterZoom; detail UI to be added in a future task
    }

    private void UpdateMonsterInspect()
    {
        if (GetInputManager().GetCancelPressed())
            StartMonsterFocus(selectedFloor);
    }
    #endregion

    #region // Helpers
    private void ReturnToCastleMenu()
    {
        currentState = SelectionStates.Inactive;
        GetCastleMenuManager().SetCastleMenuState(CastleMenuManager.CastleMenuStates.Main);
    }

    public SelectionStates GetSelectionState() => currentState;
    public void SetSelectionState(SelectionStates state) => currentState = state;

    public int GetSelectedFloor() => selectedFloor;
    public void SetSelectedFloor(int floor) => selectedFloor = floor;

    public int GetSelectedSlot() => selectedSlot;
    public void SetSelectedSlot(int slot) => selectedSlot = slot;

    public FloorGroup GetSelectedGroup() => selectedGroup;

    public TowerDefenseManager GetGameManager() => GameManager;
    private InputManager GetInputManager() => GameManager.GetInputManager();
    private TowerManager GetTowerManager() => GameManager.GetTowerManager();
    private CameraManager GetCameraManager() => GameManager.GetCameraManager();
    private CastleMenuManager GetCastleMenuManager() => GameManager.GetCastleMenuManager();
    #endregion
    #endregion
}
