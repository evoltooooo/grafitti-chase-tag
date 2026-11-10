using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private PlayerControls playerControls;

    public Vector2 MovementInput { get; private set; }
    public Vector2 CameraInput { get; private set; }

    public bool SprintHeld { get; private set; }

    public event Action PausePressed;

    private void Awake()
    {
        playerControls = new PlayerControls();

        playerControls.Movement.Movement.performed +=
            context => MovementInput = context.ReadValue<Vector2>();

        playerControls.Movement.Movement.canceled +=
            context => MovementInput = Vector2.zero;

        playerControls.Movement.Camera.performed +=
            context => CameraInput = context.ReadValue<Vector2>();

        playerControls.Movement.Camera.canceled +=
            context => CameraInput = Vector2.zero;

        playerControls.Action.Sprint.performed +=
            _ => SprintHeld = true;

        playerControls.Action.Sprint.canceled +=
            _ => SprintHeld = false;

        playerControls.Action.Pause.performed +=
            _ => PausePressed?.Invoke();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();

        MovementInput = Vector2.zero;
        CameraInput = Vector2.zero;
        SprintHeld = false;
    }
}
