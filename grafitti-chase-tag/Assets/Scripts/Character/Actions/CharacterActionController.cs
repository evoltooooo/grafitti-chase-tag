using UnityEngine;

public class CharacterActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterActionResolver actionResolver;
    [SerializeField] private ParkourActionResolver parkourActionResolver;
    [SerializeField] private CharacterActionExecutor actionExecutor;
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    private CharacterMovementIntent movementIntent;

    [Header("Interaction")]
    [SerializeField] private CharacterInteractionResolver interactionResolver;

    [Header("Round Control")]
    [SerializeField] private RoundManager roundManager;
    private bool IsRoundLocked => 
        roundManager != null && !roundManager.IsRoundActive;

    private void Awake()
    {

        if (actionResolver == null)
        {
            actionResolver =
                GetComponent<CharacterActionResolver>();
        }

        if (parkourActionResolver == null)
        {
            parkourActionResolver =
                GetComponent<ParkourActionResolver>();
        }

        if (actionExecutor == null)
        {
            actionExecutor =
                GetComponent<CharacterActionExecutor>();
        }

        if (characterMotor == null)
        {
            characterMotor =
                GetComponent<CharacterMotor>();
        }

        if (actionRuntime == null)
        {
            actionRuntime =
                GetComponent<CharacterActionRuntime>();
        }

        if (interactionResolver == null)
        {
            interactionResolver = GetComponent<CharacterInteractionResolver>();   
        }
    }

    public void SetRoundManager(RoundManager manager)
    {
        roundManager = manager;
    }

    public bool RequestAction(
        CharacterActionRequest request)
    {
        if (IsRoundLocked)
            return false;

        if (request.ActionType == CharacterActionType.None)
        {
            return false;
        }

        if (actionResolver == null ||
            actionExecutor == null)
        {
            return false;
        }

        if (!actionResolver.TryResolve(
                request,
                out CharacterActionData actionData))
        {
            return false;
        }

        return actionExecutor.Execute(
            actionData
        );
    }

    public bool RequestParkourAction(
        ParkourActionType parkourActionType)
    {
        if (IsRoundLocked)
            return false;

        if (actionResolver == null ||
            parkourActionResolver == null ||
            actionExecutor == null)
            return false;

        if (!actionResolver.TryResolveParkourAction(
                parkourActionType,
                out ParkourActionData parkourActionData))
        {
            return false;
        }

        if (!parkourActionResolver.TryResolve(
                parkourActionData,
                out ParkourTarget target))
        {
            return false;
        }

        return actionExecutor.ExecuteParkour(
            parkourActionData,
            target
        );
    }

    public void SetInteractionHeld(bool held)
    {
        if (!held &&
            actionRuntime.IsExecuting &&
            actionRuntime.IsHoldControlled)
        {
            actionRuntime.BeginRelease();
        }
    }

    public void RequestInteraction()
    {
        if (IsRoundLocked)
            return;

        if (interactionResolver == null)
        {
            return;
        }

        interactionResolver.TryResolveInteraction();
    }

    public bool RequestJump()
    {
        return RequestAction(
            new CharacterActionRequest(
                CharacterActionType.Jump
            )
        );
    }

    public bool RequestSlide()
    {
        return RequestAction(
            new CharacterActionRequest(
                CharacterActionType.Slide
            )
        );
    }

    public bool RequestTag()
    {
        return RequestAction(
            new CharacterActionRequest(
                CharacterActionType.Tag
            )
        );
    }

    private bool IsAirborne()
    {
        return characterMotor != null &&
            !characterMotor.IsGrounded;
    }

    private bool TryAirborneJumpAction()
    {
        if (RequestParkourAction(
                ParkourActionType.TicTac))
        {
            return true;
        }

        return false;
    }

    public bool RequestJumpInput()
    {
        // =====================================================
        // AIRBORNE → TRY TIC TAC
        // =====================================================

        if (IsAirborne())
        {
            return TryAirborneJumpAction();
        }

        // =====================================================
        // GROUNDED → NORMAL JUMP
        // =====================================================

        bool jumpPerformed =
            RequestJump();

        return jumpPerformed;
    }

    public bool RequestTagInput()
    {
        bool tagPerformed = RequestTag();

        return tagPerformed;
    }

    public void SetMovementIntent(
        CharacterMovementIntent intent)
    {
        movementIntent = intent;
    }

    public void TickMovement()
    {
        if (characterMotor == null)
            return;

        if (IsRoundLocked)
        {
            characterMotor.TickWorldDirection(
                Vector3.zero,
                false,
                Vector2.zero,
                false,
                Vector3.zero,
                false
            );

            return;
        }

        characterMotor.TickWorldDirection(
            movementIntent.WorldDirection,
            movementIntent.Sprint,
            movementIntent.MovementInput,
            movementIntent.FaceCameraDirection,
            movementIntent.LookDirection,
            movementIntent.HasLookDirection
        );
    }
    public bool TryFindParkourTarget(
        ParkourActionType parkourActionType,
        out ParkourTarget target)
    {
        target = default;

        if (actionResolver == null ||
            parkourActionResolver == null)
        {
            return false;
        }

        if (!actionResolver.TryResolveParkourAction(
                parkourActionType,
                out ParkourActionData parkourActionData))
        {
            return false;
        }

        if (!parkourActionResolver.TryResolve(
                parkourActionData,
                out target))
        {
            return false;
        }

        return target.IsValid;
    }

    public bool ResetForRoundStart(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return false;
        }

        // Cancel stale actions and movement before repositioning.
        StopForRoundTransition();

        if (characterMotor == null)
            return false;

        bool success = characterMotor.ResetPositionForNewRound(
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (!success)
        {
            return false;
        }

        // Clear any movement intent left by the previous round.
        movementIntent = default;

        return true;
    }

    public void StopForRoundTransition()
    {
        movementIntent = default;

        if (actionRuntime != null)
            actionRuntime.CancelAction();

        if (characterMotor != null)
            characterMotor.StopForRoundTransition();

        CharacterStateController stateController =
            GetComponent<CharacterStateController>();

        if (stateController != null)
            stateController.ResetForRoundTransition();
    }
}