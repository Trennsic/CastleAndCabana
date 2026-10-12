using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Input Actions Asset")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction action;
    private InputAction cancel;
    private InputAction secondary;
    private InputAction tertiary;

    private InputAction moveAction;
    private InputAction startAction;
    private InputAction selectAction;

    private static InputManager _instance;
    [SerializeField] private DebugInputManager _inputDebugger = new DebugInputManager();

    public enum InputState
    {
        Pressed,
        Released,
        Held
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeInputActions();
    }

    private void Update()
    {
        // Update the DebugInputManager
        _inputDebugger.UpdateInputDebugger();
    }

    private void InitializeInputActions()
    {
        // Access actions from the Input Actions Asset
        var gameplayMap = inputActions.FindActionMap("Player");
        action = gameplayMap.FindAction("Action");
        cancel = gameplayMap.FindAction("Cancel");
        secondary = gameplayMap.FindAction("Secondary");
        tertiary = gameplayMap.FindAction("Tertiary");

        moveAction = gameplayMap.FindAction("Move");
        startAction = gameplayMap.FindAction("Start");
        selectAction = gameplayMap.FindAction("Select");

        // Enable the action map
        gameplayMap.Enable();
    }

    // Face Buttons
    public bool GetAction() => (GetActionPressed() || GetActionHeld());
    public bool GetActionPressed() => action.WasPressedThisFrame();
    public bool GetActionReleased() => action.WasReleasedThisFrame();
    public bool GetActionHeld() => action.IsPressed();

    public bool GetCancel() => (GetCancelPressed() || GetCancelHeld());
    public bool GetCancelPressed() => cancel.WasPressedThisFrame();
    public bool GetCancelReleased() => cancel.WasReleasedThisFrame();
    public bool GetCancelHeld() => cancel.IsPressed();

    public bool GetSecondaryPressed() => secondary.WasPressedThisFrame();
    public bool GetSecondaryReleased() => secondary.WasReleasedThisFrame();
    public bool GetSecondaryHeld() => secondary.IsPressed();

    public bool GetTertiaryPressed() => tertiary.WasPressedThisFrame();
    public bool GetTertiaryReleased() => tertiary.WasReleasedThisFrame();
    public bool GetTertiaryHeld() => tertiary.IsPressed();

    public bool GetStartPressed() => startAction.WasPressedThisFrame();
    public bool GetSelectPressed() => selectAction.WasPressedThisFrame();

    // Add this to the InputManager
    public bool GetLeftTrigger(InputState state)
    {
        return GetActionState("LeftTrigger", state);
    }

    public bool GetRightTrigger(InputState state)
    {
        return GetActionState("RightTrigger", state);
    }

    public bool GetLeftBumper(InputState state)
    {
        return GetActionState("LeftBumper", state);
    }

    public bool GetRightBumper(InputState state)
    {
        return GetActionState("RightBumper", state);
    }

    public bool GetMoveUpPressed() => moveAction.WasPressedThisFrame() && moveAction.ReadValue<Vector2>().y > 0;
    public bool GetMoveUpHeld() => moveAction.IsPressed() && moveAction.ReadValue<Vector2>().y > 0;

    public bool GetMoveDownPressed() => moveAction.WasPressedThisFrame() && moveAction.ReadValue<Vector2>().y < 0;
    public bool GetMoveDownHeld() => moveAction.IsPressed() && moveAction.ReadValue<Vector2>().y < 0;

    public bool GetMoveLeftPressed() => moveAction.WasPressedThisFrame() && moveAction.ReadValue<Vector2>().x < 0;
    public bool GetMoveLeftHeld() => moveAction.IsPressed() && moveAction.ReadValue<Vector2>().x < 0;

    public bool GetMoveRightPressed() => moveAction.WasPressedThisFrame() && moveAction.ReadValue<Vector2>().x > 0;
    public bool GetMoveRightHeld() => moveAction.IsPressed() && moveAction.ReadValue<Vector2>().x > 0;


    // Helper method for trigger and bumper actions
    private bool GetActionState(string actionName, InputState state)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null) return false;

        switch (state)
        {
            case InputState.Pressed:
                return action.WasPressedThisFrame();
            case InputState.Released:
                return action.WasReleasedThisFrame();
            case InputState.Held:
                return action.IsPressed();
            default:
                return false;
        }
    }

    // Utility Methods
    private bool GetButtonState(KeyCode key, GamepadButton button, InputState state)
    {
        if (Gamepad.current != null) // Check for Gamepad input
        {
            var gamepad = Gamepad.current;
            switch (state)
            {
                case InputState.Pressed:
                    return gamepad[button].wasPressedThisFrame || Input.GetKeyDown(key);
                case InputState.Released:
                    return gamepad[button].wasReleasedThisFrame || Input.GetKeyUp(key);
                case InputState.Held:
                    return gamepad[button].isPressed || Input.GetKey(key);
                default:
                    return false;
            }
        }
        else // Default to keyboard input
        {
            switch (state)
            {
                case InputState.Pressed:
                    return Input.GetKeyDown(key);
                case InputState.Released:
                    return Input.GetKeyUp(key);
                case InputState.Held:
                    return Input.GetKey(key);
                default:
                    return false;
            }
        }
    }
}

[System.Serializable]
public class DebugInputManager
{
    // Face Buttons
    public bool Action;
    public bool Cancel;
    public bool ActionPressed;
    public bool CancelPressed;
    public bool SecondaryPressed;
    public bool TertiaryPressed;

    // Triggers and Bumpers
    public bool LeftTriggerPressed;
    public bool RightTriggerPressed;
    public bool LeftBumperPressed;
    public bool RightBumperPressed;

    // Movement
    public bool MoveUpPressed;
    public bool MoveDownPressed;
    public bool MoveLeftPressed;
    public bool MoveRightPressed;
    public bool MoveUpHeld;
    public bool MoveDownHeld;
    public bool MoveLeftHeld;
    public bool MoveRightHeld;

    // Start and Select
    public bool StartPressed;
    public bool SelectPressed;

    public void UpdateInputDebugger()
    {
        // Face Buttons
        Action = InputManager.Instance.GetAction();
        Cancel = InputManager.Instance.GetCancel();

        // Face Buttons
        ActionPressed = InputManager.Instance.GetActionPressed();
        CancelPressed = InputManager.Instance.GetCancelPressed();
        SecondaryPressed = InputManager.Instance.GetSecondaryPressed();
        TertiaryPressed = InputManager.Instance.GetTertiaryPressed();

        // Triggers and Bumpers
        LeftTriggerPressed = InputManager.Instance.GetLeftTrigger(InputManager.InputState.Pressed);
        RightTriggerPressed = InputManager.Instance.GetRightTrigger(InputManager.InputState.Pressed);
        LeftBumperPressed = InputManager.Instance.GetLeftBumper(InputManager.InputState.Pressed);
        RightBumperPressed = InputManager.Instance.GetRightBumper(InputManager.InputState.Pressed);

        // Movement
        MoveUpPressed = InputManager.Instance.GetMoveUpPressed();
        MoveDownPressed = InputManager.Instance.GetMoveDownPressed();
        MoveLeftPressed = InputManager.Instance.GetMoveLeftPressed();
        MoveRightPressed = InputManager.Instance.GetMoveRightPressed();
        MoveUpHeld = InputManager.Instance.GetMoveUpHeld();
        MoveDownHeld = InputManager.Instance.GetMoveDownHeld();
        MoveLeftHeld = InputManager.Instance.GetMoveLeftHeld();
        MoveRightHeld = InputManager.Instance.GetMoveRightHeld();

        // Start and Select
        StartPressed = InputManager.Instance.GetStartPressed();
        SelectPressed = InputManager.Instance.GetSelectPressed();
    }
}