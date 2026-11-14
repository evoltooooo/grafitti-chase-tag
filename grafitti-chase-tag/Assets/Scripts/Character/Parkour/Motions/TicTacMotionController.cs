using UnityEngine;

public class TicTacMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    private Vector3 ticTacDirection;
    private float ticTacHorizontalSpeed;
    private float ticTacVerticalVelocity;

    private CharacterActionType lastAction =
        CharacterActionType.None;

    private bool actionInitialized;

    private void Awake()
    {
        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();
    }

    private void Update()
    {
        if (characterMotor == null ||
            actionRuntime == null)
            return;

        if (!actionRuntime.IsExecuting)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentParkourActionType !=
            ParkourActionType.TicTac)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentActionType != lastAction)
        {
            BeginTicTac();
        }

        if (actionInitialized)
        {
            UpdateTicTac();
        }
    }

    private void BeginTicTac()
    {
        lastAction = actionRuntime.CurrentActionType;

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        if (!target.IsValid)
        {
            actionInitialized = false;
            return;
        }

        // The wall normal points away from the wall.
        // Use it directly because the Tic Tac animation
        // already handles the character's turning.
        Vector3 launchDirection =
            target.SurfaceNormal;

        launchDirection.y = 0f;

        if (launchDirection.sqrMagnitude < 0.001f)
        {
            actionInitialized = false;
            return;
        }

        ticTacDirection = launchDirection.normalized;

        Vector3 incomingVelocity =
            characterMotor.HorizontalVelocity;

        float incomingSpeed =
            incomingVelocity.magnitude;

        TicTacActionSettings settings =
            actionRuntime.CurrentParkourActionData.TicTacSettings;

        ticTacHorizontalSpeed =
            Mathf.Max(
                incomingSpeed,
                settings.minBounceSpeed
            );

        ticTacVerticalVelocity =
            settings.verticalLaunchSpeed;

        actionInitialized = true;
    }

    private void UpdateTicTac()
    {
        float gravity = characterMotor.Gravity;

        ticTacVerticalVelocity +=
            gravity * Time.deltaTime;

        Vector3 horizontalMovement =
            ticTacDirection *
            ticTacHorizontalSpeed *
            Time.deltaTime;

        Vector3 verticalMovement =
            Vector3.up *
            ticTacVerticalVelocity *
            Time.deltaTime;

        Vector3 deltaPosition =
            horizontalMovement + verticalMovement;

        // Do not override rotation here.
        // The Tic Tac animation controls the character's turn.
        characterMotor.ApplyParkourMotion(
            deltaPosition,
            Quaternion.identity,
            0f
        );
    }

    private void ResetMotion()
    {
        actionInitialized = false;

        lastAction = CharacterActionType.None;

        ticTacDirection = Vector3.zero;

        ticTacHorizontalSpeed = 0f;

        ticTacVerticalVelocity = 0f;
    }
}
