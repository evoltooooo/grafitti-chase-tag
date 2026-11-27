using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerControls playerControls;

    public bool IsInitialized => playerControls != null;

    public Vector2 MovementInput { get; private set; }
    public Vector2 CameraInput { get; private set; }

    public bool SprintHeld { get; private set; }

    public event Action PausePressed;
    public event Action SkipIntroPressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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

        playerControls.Action.SkipIntro.performed +=
            _ => SkipIntroPressed?.Invoke();
    }

    private void OnEnable()
    {
        if (Instance != this || playerControls == null)
            return;

        playerControls.Enable();
    }

    private void OnDisable()
    {
        if (Instance != this || playerControls == null)
            return;

        playerControls.Disable();

        ResetInputValues();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        playerControls?.Dispose();
        playerControls = null;

        Instance = null;
    }

    private void ResetInputValues()
    {
        MovementInput = Vector2.zero;
        CameraInput = Vector2.zero;
        SprintHeld = false;
    }
}
