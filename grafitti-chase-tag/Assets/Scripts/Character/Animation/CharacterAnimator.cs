using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterActionLibrary actionLibrary;

    [Header("Animation Settings")]
    [SerializeField] private float dampTime = 0.1f;

    private static readonly int MoveXHash =
        Animator.StringToHash("MoveX");

    private static readonly int MoveYHash =
        Animator.StringToHash("MoveY");

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int GroundedHash =
        Animator.StringToHash("Grounded");

    private static readonly int VerticalVelocityHash =
        Animator.StringToHash("VerticalVelocity");

    private static readonly int JumpingHash =
        Animator.StringToHash("Jumping");

    private static readonly int ActionTypeHash =
        Animator.StringToHash("ActionType");

    private void Awake()
    {
        if (characterMotor == null)
        {
            characterMotor =
                GetComponentInParent<CharacterMotor>();
        }

        if (actionRuntime == null)
        {
            actionRuntime =
                GetComponentInParent<CharacterActionRuntime>();
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (actionLibrary == null)
        {
            actionLibrary =
                GetComponentInParent<CharacterActionLibrary>();
        }
    }

    private void Update()
    {
        if (characterMotor == null ||
            animator == null)
        {
            return;
        }

        UpdateLocomotion();
        UpdateAirState();
        UpdateActionState();
    }

    private void UpdateLocomotion()
    {
        Vector2 input = characterMotor.MovementInput;

        float moveX = 0f;
        float moveY = 0f;

        // Sprint has priority.
        // W + Sprint, including W+A/D, always plays Sprint.
        if (characterMotor.IsSprinting)
        {
            moveX = 0f;
            moveY = 2f;
        }
        // Forward has priority over A/D.
        else if (input.y > 0.1f)
        {
            moveX = 0f;
            moveY = 1f;
        }
        // Backward has priority over A/D.
        else if (input.y < -0.1f)
        {
            moveX = 0f;
            moveY = -1f;
        }
        // Only A/D now produces strafing.
        else if (input.x > 0.1f)
        {
            moveX = 1f;
            moveY = 0f;
        }
        else if (input.x < -0.1f)
        {
            moveX = -1f;
            moveY = 0f;
        }

        animator.SetFloat(
            MoveXHash,
            moveX,
            dampTime,
            Time.deltaTime
        );

        animator.SetFloat(
            MoveYHash,
            moveY,
            dampTime,
            Time.deltaTime
        );

        animator.SetFloat(
            SpeedHash,
            characterMotor.HorizontalSpeed,
            dampTime,
            Time.deltaTime
        );
    }

    private void UpdateAirState()
    {
        animator.SetBool(
            GroundedHash,
            characterMotor.IsGrounded
        );

        animator.SetFloat(
            VerticalVelocityHash,
            characterMotor.VerticalVelocity,
            dampTime,
            Time.deltaTime
        );

        // Parkour actions such as Vault/Climb control
        // their own animation state.
        // Do not let airborne locomotion override them.
        bool parkourActionExecuting =
            actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentParkourActionType !=
                ParkourActionType.None;

        animator.SetBool(
            JumpingHash,
            !parkourActionExecuting &&
            characterMotor.IsJumping
        );
    }

    private void UpdateActionState()
    {
        if (actionRuntime == null || animator == null)
            return;

        if (actionRuntime.IsExecuting)
        {
            int actionValue =
                (int)actionRuntime.CurrentActionType;

            animator.SetInteger(
                ActionTypeHash,
                actionValue
            );
        }
        else
        {
            animator.SetInteger(
                ActionTypeHash,
                0
            );
        }
    }

    private void OnAnimatorMove()
    {
        if (characterMotor == null ||
            animator == null)
        {
            return;
        }

        if (actionRuntime == null ||
            !actionRuntime.IsExecuting)
        {
            return;
        }

        ActionMotionType motionType =
            actionRuntime.CurrentMotionType;

        if (motionType != ActionMotionType.RootMotion &&
            motionType != ActionMotionType.TargetMatch &&
            motionType != ActionMotionType.Hybrid &&
            motionType != ActionMotionType.Specialized)
        {
            return;
        }
        Vector3 deltaPosition =
            animator.deltaPosition;

        Quaternion deltaRotation =
            animator.deltaRotation;

        characterMotor.ApplyActionMotion(
            deltaPosition,
            deltaRotation
        );
    }
}