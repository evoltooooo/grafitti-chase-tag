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
        lastAction =
            actionRuntime.CurrentActionType;

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        if (!target.IsValid)
        {
            actionInitialized = false;
            return;
        }

        Vector3 incomingVelocity =
            characterMotor.HorizontalVelocity;

        Vector3 incomingDirection =
            incomingVelocity;

        incomingDirection.y = 0f;

        if (incomingDirection.sqrMagnitude < 0.001f)
        {
            incomingDirection =
                transform.forward;

            incomingDirection.y = 0f;
        }

        if (incomingDirection.sqrMagnitude < 0.001f)
        {
            actionInitialized = false;
            return;
        }

        incomingDirection.Normalize();

        Vector3 wallNormal =
            target.SurfaceNormal;

        wallNormal.y = 0f;

        if (wallNormal.sqrMagnitude < 0.001f)
        {
            actionInitialized = false;
            return;
        }

        wallNormal.Normalize();

        Vector3 reflectedDirection =
            Vector3.Reflect(
                incomingDirection,
                wallNormal
            );

        reflectedDirection.y = 0f;

        if (reflectedDirection.sqrMagnitude < 0.001f)
        {
            actionInitialized = false;
            return;
        }

        reflectedDirection.Normalize();

        ticTacDirection =
            reflectedDirection;

        float incomingSpeed =
            incomingVelocity.magnitude;

        ticTacHorizontalSpeed =
            Mathf.Max(
                incomingSpeed,
                actionRuntime
                    .CurrentParkourActionData
                    .TicTacSettings
                    .minBounceSpeed
            );

        ticTacVerticalVelocity =
            actionRuntime
                .CurrentParkourActionData
                .TicTacSettings
                .verticalLaunchSpeed;

        actionInitialized = true;

        Debug.Log(
            $"TIC TAC MOTION STARTED | " +
            $"Incoming={incomingDirection} | " +
            $"WallNormal={wallNormal} | " +
            $"Bounce={ticTacDirection} | " +
            $"Speed={ticTacHorizontalSpeed:F2} | " +
            $"Vertical={ticTacVerticalVelocity:F2}",
            this
        );
    }

    private void UpdateTicTac()
    {
        float gravity =
            characterMotor.Gravity;

        ticTacVerticalVelocity +=
            gravity *
            Time.deltaTime;

        Vector3 horizontalMovement =
            ticTacDirection *
            ticTacHorizontalSpeed *
            Time.deltaTime;

        Vector3 verticalMovement =
            Vector3.up *
            ticTacVerticalVelocity *
            Time.deltaTime;

        Vector3 deltaPosition =
            horizontalMovement +
            verticalMovement;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                ticTacDirection,
                Vector3.up
            );

        characterMotor.ApplyParkourMotion(
            deltaPosition,
            targetRotation,
            10f
        );
    }

    private void ResetMotion()
    {
        actionInitialized = false;

        lastAction =
            CharacterActionType.None;

        ticTacDirection =
            Vector3.zero;

        ticTacHorizontalSpeed =
            0f;

        ticTacVerticalVelocity =
            0f;
    }
}