using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MovementSettings settings;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterStamina stamina;
    [SerializeField] private CharacterStateController stateController;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    private CharacterController characterController;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private bool isGrounded;
    private bool isJumping;

    private Vector3 jumpStartPosition;
    private float jumpStartHorizontalSpeed;
    private bool jumpDebugActive;

    private float currentSprintSpeed;
    private float nextSprintDebugTime;

    private Vector3 lastWallNormal;
    private bool hasWallContact;
    private float lastWallReboundTime = -Mathf.Infinity;

    public bool IsSprinting { get; private set; }
    
    public Vector2 MovementInput { get; private set; }

    public float LocomotionAnimationValue { get; private set; }

    public bool CanAttemptClimb
    {
        get
        {
            return !IsGrounded;
        }
    }

    public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
    public Vector3 HorizontalVelocity => horizontalVelocity;
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public float VerticalVelocity => verticalVelocity;
    public float Gravity => settings != null ? settings.gravity : 0f;
    public float MaxMovementSpeed => settings != null ? settings.sprintSpeed : 0f;
    public bool IsGrounded => isGrounded;
    public bool IsJumping => isJumping;
    public float JumpStaminaCost => settings != null ? settings.jumpStaminaCost : 0f;


    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (settings == null)
        {
            Debug.LogError(
                $"{name}: CharacterMotor is missing MovementSettings.",
                this
            );
        }

        if (stamina == null)
        {
            stamina = GetComponent<CharacterStamina>();
        }

        if (actionRuntime == null)
        {
            actionRuntime = GetComponent<CharacterActionRuntime>();
        }

        currentSprintSpeed = settings != null
            ? settings.runSpeed
            : 0f;
    }

    public void Tick(Vector2 movementInput, bool sprintHeld)
    {
        MovementInput = movementInput;

        if (settings == null)
            return;

        UpdateGroundedState();

        bool actionExecuting =
            actionRuntime != null &&
            actionRuntime.IsExecuting;

        bool actionControlsNormalMovement =
            actionExecuting &&
            (actionRuntime.CurrentMotionType ==
                ActionMotionType.RootMotion ||
            actionRuntime.CurrentMotionType ==
                ActionMotionType.TargetMatch ||
            actionRuntime.CurrentMotionType ==
                ActionMotionType.Hybrid);

        if (!actionControlsNormalMovement)
        {
            HandleHorizontalMovement(
                movementInput,
                sprintHeld
            );

            HandleVerticalMovement();

            ApplyMovement();
        }
    }

    // =========================================================
    // JUMP
    // =========================================================

    public bool PerformJump()
    {
        if (!isGrounded)
            return false;

        verticalVelocity =
            Mathf.Sqrt(
                settings.jumpHeight *
                -2f *
                settings.gravity
            );

        isJumping = true;

        jumpStartPosition = transform.position;
        jumpStartHorizontalSpeed = HorizontalSpeed;
        jumpDebugActive = true;

        Debug.Log(
            $"JUMP | HorizontalSpeed={HorizontalSpeed:F2} | " +
            $"HorizontalVelocity={HorizontalVelocity} | " +
            $"VerticalVelocity={verticalVelocity:F2}",
            this
        );

        return true;
    }

    // =========================================================
    // GROUND
    // =========================================================

    private void UpdateGroundedState()
    {
        isGrounded =
            characterController.isGrounded;

        if (isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity =
                settings.groundedVerticalVelocity;
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void HandleHorizontalMovement(Vector2 input, bool sprintHeld)
    {
        Vector3 desiredDirection =
            GetCameraRelativeDirection(input);

        float inputAmount =
            Mathf.Clamp01(input.magnitude);

        bool canSprint =
            sprintHeld &&
            input.y > 0.1f &&
            inputAmount > 0.1f &&
            stamina != null &&
            !stamina.IsEmpty;

        IsSprinting = canSprint;

        float targetSpeed;

        if (canSprint)
        {
            currentSprintSpeed =
                Mathf.MoveTowards(
                    currentSprintSpeed,
                    settings.sprintSpeed,
                    settings.sprintSpeedIncrease * Time.deltaTime
                );

            targetSpeed = currentSprintSpeed;

            if (Time.time >= nextSprintDebugTime)
            {
                Debug.Log(
                    $"SPRINT | Target: {currentSprintSpeed:F2} | " +
                    $"Actual: {HorizontalSpeed:F2}"
                );

                nextSprintDebugTime = Time.time + 0.25f;
            }

            LocomotionAnimationValue = 2f;

            stamina.ConsumeOverTime(settings.sprintStaminaPerSecond);
        }
        else if (inputAmount > 0.01f)
        {
            currentSprintSpeed = settings.runSpeed;

            targetSpeed = stamina != null && stamina.IsEmpty
                ? settings.runSpeed * 0.6f
                : settings.runSpeed;

            LocomotionAnimationValue = 1f;
        }
        else
        {
            currentSprintSpeed = settings.runSpeed;

            targetSpeed = 0f;
            LocomotionAnimationValue = 0f;
        }

        float controlMultiplier =
            isGrounded
                ? 1f
                : settings.airControl;

        Vector3 targetVelocity =
            desiredDirection *
            targetSpeed *
            inputAmount *
            controlMultiplier;

        float responseRate;

        if (targetVelocity.sqrMagnitude >
            horizontalVelocity.sqrMagnitude)
        {
            responseRate =
                settings.acceleration;
        }
        else
        {
            responseRate =
                settings.deceleration;
        }

        horizontalVelocity =
            Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                responseRate *
                Time.deltaTime
            );

        if (desiredDirection.sqrMagnitude >
            0.001f)
        {
            RotateTowards(
                desiredDirection
            );
        }
    }

    private Vector3 GetCameraRelativeDirection(
        Vector2 input)
    {
        if (cameraTransform == null)
        {
            return new Vector3(
                input.x,
                0f,
                input.y
            ).normalized;
        }

        Vector3 cameraForward =
            cameraTransform.forward;

        Vector3 cameraRight =
            cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction =
            cameraForward * input.y +
            cameraRight * input.x;

        return Vector3.ClampMagnitude(
            direction,
            1f
        );
    }

    private void RotateTowards(
        Vector3 direction)
    {
        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                settings.rotationSpeed *
                Time.deltaTime
            );
    }
    

    // =========================================================
    // GRAVITY
    // =========================================================

    private void HandleVerticalMovement()
    {
        verticalVelocity +=
            settings.gravity *
            Time.deltaTime;
    }

    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.moveDirection.y < -0.3f)
            return;

        if (Vector3.Dot(hit.normal, Vector3.up) > 0.7f)
            return;

        lastWallNormal = hit.normal;
        hasWallContact = true;

        Debug.Log(
            $"WALL CONTACT | Normal: {lastWallNormal}"
        );
    }

    // =========================================================
    // FINAL MOVEMENT
    // =========================================================

    private void ApplyMovement()
    {
        hasWallContact = false;

        Vector3 movement =
            horizontalVelocity +
            Vector3.up *
            verticalVelocity;

        CollisionFlags collisionFlags =
            characterController.Move(
                movement *
                Time.deltaTime
            );

        if ((collisionFlags & CollisionFlags.Sides) != 0)
        {
            float wallApproachSpeed =
                -Vector3.Dot(horizontalVelocity, lastWallNormal);

            float approachAngle =
                Vector3.Angle(
                    horizontalVelocity,
                    -lastWallNormal
                );

            bool movingIntoWall =
                wallApproachSpeed > 0f;

            bool validApproachAngle =
                approachAngle <= settings.wallReboundMaxApproachAngle;

            bool canWallRebound =
                HorizontalSpeed >= settings.wallReboundMinSpeed &&
                movingIntoWall &&
                validApproachAngle &&
                Time.time - lastWallReboundTime >= settings.wallReboundCooldown;

            if (canWallRebound && hasWallContact)
            {
                Vector3 reboundDirection = lastWallNormal;

                horizontalVelocity =
                    reboundDirection *
                    settings.wallReboundSpeed;
                
                lastWallReboundTime = Time.time;    

                Debug.Log(
                    $"WALL REBOUND | Direction: {reboundDirection} | " +
                    $"Speed: {settings.wallReboundSpeed:F2}"
                );
            }

            Debug.Log(
                $"WALL REBOUND CHECK | " +
                $"Speed: {HorizontalSpeed:F2} | " +
                $"Approach Speed: {wallApproachSpeed:F2} | " +
                $"Angle: {approachAngle:F1}° | " +
                $"Valid Angle: {validApproachAngle}"
            );
        }

        isGrounded =
            (collisionFlags &
             CollisionFlags.Below) != 0;

        if (isGrounded && jumpDebugActive)
        {
            Vector3 jumpDisplacement =
                transform.position - jumpStartPosition;

            jumpDisplacement.y = 0f;

            Debug.Log(
                $"JUMP LAND | " +
                $"StartSpeed={jumpStartHorizontalSpeed:F2} | " +
                $"Distance={jumpDisplacement.magnitude:F2}",
                this
            );

            jumpDebugActive = false;
        }

        if (isGrounded &&
            verticalVelocity <= 0f)
        {
            isJumping = false;
        }
    }

    public void ApplyActionMotion(
        Vector3 deltaPosition,
        Quaternion deltaRotation)
    {
        if (characterController == null)
            return;

        characterController.Move(
            deltaPosition
        );

        if (deltaRotation != Quaternion.identity)
        {
            transform.rotation =
                transform.rotation *
                deltaRotation;
        }
    }

    public void ApplyParkourMotion(
    Vector3 deltaPosition,
    Quaternion targetRotation,
    float rotationSpeed)
    {
        if (characterController == null)
            return;

        characterController.Move(deltaPosition);

        if (rotationSpeed > 0f)
        {
            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }
    }
}