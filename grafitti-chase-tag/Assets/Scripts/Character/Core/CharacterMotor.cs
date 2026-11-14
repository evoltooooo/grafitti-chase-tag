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
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterBuffNerf buffNerf;
    [SerializeField] private CharacterActionController actionController;

    public float ColliderRadius =>
    characterController != null
        ? characterController.radius
        : 0f;

    public float ColliderHeight =>
        characterController != null
            ? characterController.height
            : 0f;

    public Vector3 ColliderCenter =>
        characterController != null
            ? characterController.center
            : Vector3.zero;

    [Header("Slide Collider")]
    [SerializeField] private float slideColliderHeight = 1.0f;

    [Header("Slide Movement Runtime")]
    private Vector3 slideForwardDirection;

    private float activeSlideInitialSpeed;
    private float activeSlidePeakSpeed;
    private float activeSlideExitSpeed;
    private float activeSlideStartEndTime;

    private float activeSlideAcceleration;
    private float activeSlideDeceleration;
    private float activeSlideRecoveryRate;

    private float activeSlideCurrentSpeed;

    private float slideLoopStartActionTime;
    private bool slideExitAnimationCompleted;

    private bool slideLoopStarted;
    private bool slideExitStarted;

    private float standingColliderHeight;
    private Vector3 standingColliderCenter;
    private Collider lastSlideRestoreBlocker;
    private bool wasSlideRestoreBlocked;

    public bool IsSliding { get; private set; }

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
    private float nextMotorDebugTime;

    private Vector3 lastWallNormal;
    private bool hasWallContact;
    private float lastWallReboundTime = -Mathf.Infinity;
    private Vector3 lastPhysicalHorizontalVelocity;

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
    public float MaxMovementSpeed =>
        settings != null
            ? settings.sprintSpeed *
            (buffNerf != null
                ? buffNerf.GetMultiplier(
                        BuffNerfEffectType.SpeedMultiplier)
                : 1f)
            : 0f;
    public bool IsGrounded => isGrounded;
    public bool IsJumping => isJumping;
    public float JumpStaminaCost => settings != null ? settings.jumpStaminaCost : 0f;


    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
        
        standingColliderHeight =
            characterController.height;

        standingColliderCenter =
            characterController.center;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
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
        
        if (buffNerf == null)
        {
            buffNerf = GetComponent<CharacterBuffNerf>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (actionController == null)
        {
            actionController =
                GetComponent<CharacterActionController>();
        }
    }

    public void Tick(Vector2 movementInput, bool sprintHeld)
    {
        MovementInput = movementInput;

        UpdateSlideCollider();

        if (settings == null)
            return;

        UpdateGroundedState();

        UpdateSlideMovement();

        bool actionExecuting =
            actionRuntime != null &&
            actionRuntime.IsExecuting;

        bool actionControlsNormalMovement =
            actionExecuting &&
            (actionRuntime.CurrentMotionType == ActionMotionType.RootMotion ||
            actionRuntime.CurrentMotionType == ActionMotionType.TargetMatch ||
            actionRuntime.CurrentMotionType == ActionMotionType.Hybrid ||
            actionRuntime.CurrentMotionType == ActionMotionType.Specialized);

        if (!actionControlsNormalMovement)
        {
            if (!IsSlideMovementActive() &&
                !IsWallReboundActionActive())
            {
                Vector3 worldDirection =
                    GetCameraRelativeDirection(movementInput);

                HandleHorizontalMovement(
                    worldDirection,
                    sprintHeld
                );
            }

            HandleVerticalMovement();
            ApplyMovement();
        }
    }

    public void TickWorldDirection(
        Vector3 worldDirection,
        bool sprintHeld,
        Vector2 movementInput,
        bool faceCameraDirection,
        Vector3 lookDirection,
        bool hasLookDirection)
    {
        if (settings == null)
            return;
        
        MovementInput = movementInput;
        
        UpdateSlideCollider();

        UpdateGroundedState();

        UpdateSlideMovement();

        bool actionExecuting =
            actionRuntime != null &&
            actionRuntime.IsExecuting;

        bool actionControlsNormalMovement =
            actionExecuting &&
            (actionRuntime.CurrentMotionType == ActionMotionType.RootMotion ||
            actionRuntime.CurrentMotionType == ActionMotionType.TargetMatch ||
            actionRuntime.CurrentMotionType == ActionMotionType.Hybrid ||
            actionRuntime.CurrentMotionType == ActionMotionType.Specialized);

        if (actionControlsNormalMovement)
        {
            return;
        }

        if (!IsSlideMovementActive() &&
            !IsWallReboundActionActive())
        {
            HandleHorizontalMovement(
                hasLookDirection &&
                lookDirection.sqrMagnitude > 0.001f
                    ? lookDirection
                    : worldDirection,
                sprintHeld,
                hasLookDirection
                    ? false
                    : faceCameraDirection
            );
        }

        HandleVerticalMovement();
        ApplyMovement();
    }

    private bool IsSlideMovementActive()
    {
        if (!IsSliding)
            return false;

        // Keep normal movement disabled while the Slide action runs.
        if (IsSlideActionActive())
            return true;

        // Wait until the Exit animation has started.
        if (!slideExitStarted || animator == null)
            return true;

        // Do not resume movement during the Exit transition.
        if (animator.IsInTransition(0))
            return true;

        AnimatorStateInfo currentState =
            animator.GetCurrentAnimatorStateInfo(0);

        // Slide movement is finished when Exit has completed.
        return currentState.IsName(SlideExitState) &&
            currentState.normalizedTime < 1f;
    }

    public void SetMovementInput(Vector2 movementInput)
    {
        MovementInput = movementInput;
    }

    public Vector3 GetWorldDirection(Vector2 movementInput)
    {
        return GetCameraRelativeDirection(movementInput);
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

        return true;
    }

    private const string SlideStartState =
    "Armature|Slide_Start";

    private const string SlideLoopState =
        "Armature|Slide_Loop";

    private const string SlideExitState =
        "Armature|Slide_Exit";

    private float nextSlideDebugTime;

    private void UpdateSlideCollider()
    {
        if (!IsSliding)
            return;

        bool slideActionActive =
            actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentActionType ==
                CharacterActionType.Slide;

        if (slideActionActive)
            return;

        if (animator == null)
        {
            TryEndSlide();
            return;
        }

        bool isTransitioning = animator.IsInTransition(0);

        AnimatorStateInfo currentState =
            animator.GetCurrentAnimatorStateInfo(0);

        // Check whether Slide Exit has completed.
        if (currentState.IsName(SlideExitState) &&
            currentState.normalizedTime >= 1f)
        {
            slideExitAnimationCompleted = true;
        }

        // Also inspect the previous state during the transition
        // away from Slide Exit.
        if (isTransitioning)
        {
            AnimatorStateInfo nextState =
                animator.GetNextAnimatorStateInfo(0);

            if (currentState.IsName(SlideExitState) &&
                currentState.normalizedTime >= 1f &&
                !nextState.IsName(SlideExitState))
            {
                slideExitAnimationCompleted = true;
            }

            return;
        }

        if (slideExitAnimationCompleted)
        {
            TryEndSlide();
        }
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

    private void HandleHorizontalMovement(
        Vector3 desiredDirection,
        bool sprintHeld,
        bool faceCameraDirection = true)
    {
        desiredDirection.y = 0f;

        float inputAmount =
            Mathf.Clamp01(MovementInput.magnitude);

        bool movingForward =
            MovementInput.y > 0.1f;

        bool canSprint =
            sprintHeld &&
            movingForward &&
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

        float speedMultiplier = buffNerf != null
            ? buffNerf.GetMultiplier(
                BuffNerfEffectType.SpeedMultiplier)
            : 1f;

        targetSpeed *= speedMultiplier;

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

        if (desiredDirection.sqrMagnitude > 0.001f)
        {
            if (faceCameraDirection)
            {
                RotateTowardsCamera();
            }
            else
            {
                RotateTowards(desiredDirection);
            }
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (cameraTransform == null)
        {
            if (Mathf.Abs(input.y) > 0.1f)
            {
                return new Vector3(
                    0f,
                    0f,
                    Mathf.Sign(input.y)
                );
            }

            return new Vector3(
                Mathf.Sign(input.x),
                0f,
                0f
            );
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        // Forward/backward has priority over strafing.
        if (input.y > 0.1f)
        {
            return cameraForward;
        }

        if (input.y < -0.1f)
        {
            return -cameraForward;
        }

        if (input.x > 0.1f)
        {
            return cameraRight;
        }

        if (input.x < -0.1f)
        {
            return -cameraRight;
        }

        return Vector3.zero;
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

    private void RotateTowardsCamera()
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraForward =
            cameraTransform.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude <
            0.001f)
        {
            return;
        }

        cameraForward.Normalize();

        RotateTowards(
            cameraForward
        );
    }

    public void SetParkourColliderVerticalOffset(float offset)
    {
        if (characterController == null)
            return;

        Vector3 targetCenter = standingColliderCenter;
        targetCenter.y += offset;

        characterController.center = targetCenter;
    }

    public void ResetParkourColliderVerticalOffset()
    {
        if (characterController == null)
            return;

        // Do not interfere with the crouched collider during Slide.
        if (IsSliding)
            return;

        characterController.center = standingColliderCenter;
    }

    private bool IsSlideActionActive()
    {
        return IsSliding &&
            actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentActionType ==
                CharacterActionType.Slide;
    }

    private void UpdateSlideMovement()
    {
        if (!IsSliding || animator == null)
            return;

        AnimatorStateInfo currentState =
            animator.GetCurrentAnimatorStateInfo(0);

        bool isTransitioning = animator.IsInTransition(0);

        AnimatorStateInfo nextState =
            isTransitioning
                ? animator.GetNextAnimatorStateInfo(0)
                : currentState;

        bool inSlideStart =
            currentState.IsName(SlideStartState);

        bool transitioningToStart =
            isTransitioning &&
            nextState.IsName(SlideStartState);

        bool transitioningToLoop =
            isTransitioning &&
            nextState.IsName(SlideLoopState);

        bool inSlideLoop =
            currentState.IsName(SlideLoopState);

        bool transitioningToExit =
            isTransitioning &&
            nextState.IsName(SlideExitState);

        bool inSlideExit =
            currentState.IsName(SlideExitState);

        // -----------------------------------------
        // SLIDE START: ACCELERATION
        // -----------------------------------------

        if (inSlideStart || transitioningToStart)
        {
            activeSlideCurrentSpeed =
                Mathf.MoveTowards(
                    activeSlideCurrentSpeed,
                    activeSlidePeakSpeed,
                    activeSlideAcceleration * Time.deltaTime
                );
        }

        // -----------------------------------------
        // START -> LOOP
        // -----------------------------------------

        if (transitioningToLoop)
        {
            slideLoopStarted = true;

            activeSlideCurrentSpeed =
                Mathf.MoveTowards(
                    activeSlideCurrentSpeed,
                    activeSlidePeakSpeed,
                    activeSlideAcceleration * Time.deltaTime
                );
        }

        // -----------------------------------------
        // SLIDE LOOP: DECELERATION
        // -----------------------------------------

        if (inSlideLoop)
        {
            if (!slideLoopStarted)
            {
                slideLoopStarted = true;
            }

            activeSlideCurrentSpeed =
                Mathf.MoveTowards(
                    activeSlideCurrentSpeed,
                    activeSlideExitSpeed,
                    activeSlideDeceleration * Time.deltaTime
                );
        }

        // -----------------------------------------
        // LOOP -> EXIT: RECOVER ORIGINAL SPEED
        // -----------------------------------------

        if (transitioningToExit || inSlideExit)
        {
            slideExitStarted = true;

            activeSlideCurrentSpeed =
                Mathf.MoveTowards(
                    activeSlideCurrentSpeed,
                    activeSlideInitialSpeed,
                    activeSlideRecoveryRate * Time.deltaTime
                );
        }

        // -----------------------------------------
        // UPDATE SHARED HORIZONTAL VELOCITY
        // -----------------------------------------

        if (inSlideStart ||
            transitioningToStart ||
            transitioningToLoop ||
            inSlideLoop ||
            transitioningToExit ||
            inSlideExit)
        {
            horizontalVelocity =
                slideForwardDirection * activeSlideCurrentSpeed;
        }
    }

    public bool BeginSlide()
    {
        if (characterController == null)
            return false;

        if (IsSliding)
        {
            Debug.LogWarning(
                "SLIDE START REJECTED | Previous Slide has not finished restoring its collider."
            );

            return false;
        }

        float minimumHeight =
            characterController.radius * 2f;

        float targetHeight =
            Mathf.Max(
                slideColliderHeight,
                minimumHeight
            );

        float standingBottom =
            standingColliderCenter.y -
            standingColliderHeight * 0.5f;

        float slideCenterY =
            standingBottom +
            targetHeight * 0.5f;

        characterController.height =
            targetHeight;

        characterController.center =
            new Vector3(
                standingColliderCenter.x,
                slideCenterY,
                standingColliderCenter.z
            );

        IsSliding = true;

        // Preserve the character's actual incoming movement direction.
        Vector3 incomingVelocity = horizontalVelocity;
        incomingVelocity.y = 0f;

        activeSlideInitialSpeed = incomingVelocity.magnitude;

        if (incomingVelocity.sqrMagnitude > 0.001f)
        {
            slideForwardDirection = incomingVelocity.normalized;
        }
        else
        {
            slideForwardDirection = transform.forward;
            slideForwardDirection.y = 0f;
            slideForwardDirection.Normalize();
        }

        // Slide now controls horizontal movement independently.
        IsSprinting = false;

        // Read the active Slide action settings.
        CharacterActionData slideData =
            actionRuntime != null
                ? actionRuntime.CurrentActionData
                : null;

        activeSlidePeakSpeed =
            slideData != null
                ? slideData.slidePeakSpeed
                : 8f;

        activeSlideExitSpeed =
            slideData != null
                ? slideData.slideExitSpeed
                : 3f;

        activeSlideAcceleration =
            slideData != null
                ? slideData.slideAcceleration
                : 20f;

        activeSlideDeceleration =
            slideData != null
                ? slideData.slideDeceleration
                : 8f;

        activeSlideRecoveryRate =
            slideData != null
                ? slideData.slideRecoveryRate
                : 12f;

        // Start with the speed captured before Slide.
        activeSlideCurrentSpeed = activeSlideInitialSpeed;

        slideLoopStartActionTime = 0f;

        slideLoopStarted = false;
        slideExitStarted = false;
        slideExitAnimationCompleted = false;

        return true;
    }
    
    private void TryEndSlide()
    {
        if (!IsSliding)
            return;

        bool canRestore = CanRestoreStandingCollider();

        if (!canRestore)
        {
            Debug.LogWarning(
                $"SLIDE RESTORE BLOCKED | " +
                $"Blocker={(lastSlideRestoreBlocker != null ? lastSlideRestoreBlocker.name : "Unknown")} | " +
                $"Position={transform.position} | " +
                $"Height={characterController.height}"
            );

            return;
        }

        characterController.height =
            standingColliderHeight;

        characterController.center =
            standingColliderCenter;

        // Guarantee that the original speed is restored
        // before normal movement takes over.
        horizontalVelocity =
            slideForwardDirection * activeSlideInitialSpeed;

        IsSliding = false;

        slideForwardDirection = Vector3.zero;

        activeSlideInitialSpeed = 0f;
        activeSlideCurrentSpeed = 0f;

        activeSlidePeakSpeed = 0f;
        activeSlideExitSpeed = 0f;

        activeSlideAcceleration = 0f;
        activeSlideDeceleration = 0f;
        activeSlideRecoveryRate = 0f;

        slideLoopStartActionTime = 0f;

        slideLoopStarted = false;
        slideExitStarted = false;

        slideExitAnimationCompleted = false;
    }
    
    private bool CanRestoreStandingCollider()
    {
        Vector3 worldCenter =
            transform.TransformPoint(standingColliderCenter);

        float radius = characterController.radius;

        float halfHeight = Mathf.Max(
            standingColliderHeight * 0.5f,
            radius
        );

        Vector3 bottom = worldCenter +
            Vector3.down * (halfHeight - radius);

        Vector3 top = worldCenter +
            Vector3.up * (halfHeight - radius);

        Collider[] overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        bool blocked = false;

        foreach (Collider overlap in overlaps)
        {
            // Ignore this character's own colliders.
            if (overlap.transform == transform ||
                overlap.transform.IsChildOf(transform))
            {
                continue;
            }

            blocked = true;

            lastSlideRestoreBlocker = overlap;
            wasSlideRestoreBlocked = true;
        }

        if (!blocked)
        {
            lastSlideRestoreBlocker = null;
            wasSlideRestoreBlocked = false;
        }

        return !blocked;
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
        if (actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentParkourActionType != ParkourActionType.None)
        {
            Debug.Log(
                $"PARKOUR COLLISION | " +
                $"Action={actionRuntime.CurrentParkourActionType} | " +
                $"Collider={hit.collider.name} | " +
                $"Tag={hit.collider.tag} | " +
                $"Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} | " +
                $"Normal={hit.normal} | " +
                $"MoveDirection={hit.moveDirection}"
            );

            // TEMPORARY VAULT HEIGHT DIAGNOSTIC
            Debug.Log(
                $"VAULT COLLISION HEIGHT | " +
                $"Action={actionRuntime.CurrentParkourActionType} | " +
                $"PlayerY={transform.position.y:F3} | " +
                $"ControllerBottom={characterController.bounds.min.y:F3} | " +
                $"ControllerTop={characterController.bounds.max.y:F3} | " +
                $"ObstacleTop={hit.collider.bounds.max.y:F3}"
            );
        }

        if (hit.moveDirection.y < -0.3f)
            return;

        if (Vector3.Dot(hit.normal, Vector3.up) > 0.7f)
            return;

        lastWallNormal = hit.normal;
        hasWallContact = true;
    }

    // =========================================================
    // FINAL MOVEMENT
    // =========================================================
    
    private void ApplyMovement()
    {
        hasWallContact = false;

        Vector3 movement =
            horizontalVelocity +
            Vector3.up * verticalVelocity;

        Vector3 positionBefore = transform.position;

        Vector3 velocityBeforeMove = horizontalVelocity;

        CollisionFlags collisionFlags =
            characterController.Move(
                movement * Time.deltaTime
            );

        Vector3 actualDisplacement =
            transform.position - positionBefore;

        Vector3 actualHorizontalDisplacement =
            actualDisplacement;

        actualHorizontalDisplacement.y = 0f;

        // Use the physical velocity measured BEFORE this collision.
        Vector3 incomingPhysicalVelocity =
            lastPhysicalHorizontalVelocity;

        if ((collisionFlags & CollisionFlags.Sides) != 0 &&
            hasWallContact)
        {
            Vector3 wallNormal = lastWallNormal;

            float physicalApproachSpeed =
                -Vector3.Dot(
                    incomingPhysicalVelocity,
                    wallNormal
                );

            float physicalApproachAngle =
                Vector3.Angle(
                    incomingPhysicalVelocity,
                    -wallNormal
                );

            bool movingIntoWall =
                physicalApproachSpeed > 0f;

            bool validApproachSpeed =
                physicalApproachSpeed >=
                settings.wallReboundMinSpeed;

            bool validApproachAngle =
                physicalApproachAngle <=
                settings.wallReboundMaxApproachAngle;

            bool cooldownComplete =
                Time.time - lastWallReboundTime >=
                settings.wallReboundCooldown;

            bool canWallRebound =
                validApproachSpeed &&
                movingIntoWall &&
                validApproachAngle &&
                cooldownComplete;
            
            Debug.Log(
                $"WALL REBOUND CHECK | " +
                $"Contact={hasWallContact} | " +
                $"Flags={collisionFlags} | " +
                $"IncomingVelocity={incomingPhysicalVelocity:F2} | " +
                $"ApproachSpeed={physicalApproachSpeed:F2} | " +
                $"RequiredSpeed={settings.wallReboundMinSpeed:F2} | " +
                $"ApproachAngle={physicalApproachAngle:F2} | " +
                $"MaxAngle={settings.wallReboundMaxApproachAngle:F2} | " +
                $"CooldownComplete={cooldownComplete} | " +
                $"CanRebound={canWallRebound}"
            );

            if (canWallRebound)
            {
                Vector3 reboundDirection =
                    Vector3.Reflect(
                        incomingPhysicalVelocity,
                        wallNormal
                    );

                reboundDirection.y = 0f;

                if (reboundDirection.sqrMagnitude < 0.001f)
                {
                    reboundDirection = wallNormal;
                }

                reboundDirection.Normalize();

                Debug.Log(
                    $"WALL REBOUND REQUESTING | " +
                    $"ControllerExists={actionController != null} | " +
                    $"CurrentState={(stateController != null ? stateController.CurrentState.ToString() : "Missing")} | " +
                    $"ActionExecuting={(actionRuntime != null && actionRuntime.IsExecuting)}"
                );

                bool actionStarted =
                    actionController != null &&
                    actionController.RequestAction(
                        new CharacterActionRequest(
                            CharacterActionType.WallRebound
                        )
                    );

                Debug.Log(
                    $"WALL REBOUND REQUEST RESULT | Started={actionStarted}"
                );

                if (actionStarted)
                {
                    horizontalVelocity =
                        reboundDirection *
                        settings.wallReboundSpeed;

                    lastWallReboundTime = Time.time;

                    Debug.Log(
                        $"WALL REBOUND ACTION STARTED | " +
                        $"Incoming={incomingPhysicalVelocity:F2} | " +
                        $"Outgoing={reboundDirection:F2}"
                    );
                }
            }
            else
            {
                // Remove only the velocity pushing into the wall.
                // Preserve tangential movement.
                float velocityIntoWall =
                    Vector3.Dot(
                        horizontalVelocity,
                        wallNormal
                    );

                if (velocityIntoWall < 0f)
                {
                    horizontalVelocity -=
                        wallNormal * velocityIntoWall;
                }
            }
        }

        // Always measure actual displacement, including while
        // touching a wall. This allows the cache to recover after
        // a rebound without counting blocked movement as speed.
        lastPhysicalHorizontalVelocity =
            actualHorizontalDisplacement /
            Mathf.Max(Time.deltaTime, 0.0001f);

        lastPhysicalHorizontalVelocity.y = 0f;

        isGrounded =
            (collisionFlags & CollisionFlags.Below) != 0;

        if (Time.time >= nextMotorDebugTime)
        {
            nextMotorDebugTime =
                Time.time + 0.25f;
        }

        if (isGrounded && jumpDebugActive)
        {
            Vector3 jumpDisplacement =
                transform.position - jumpStartPosition;

            jumpDisplacement.y = 0f;

            jumpDebugActive = false;
        }

        if (isGrounded && verticalVelocity <= 0f)
        {
            isJumping = false;
        }
    }

    private bool IsWallReboundActionActive()
    {
        return actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentActionType ==
                CharacterActionType.WallRebound;
    }

    public void StopHorizontalMovement()
    {
        horizontalVelocity = Vector3.zero;
    }

    public void StopForRoundTransition()
    {
        // Cancel any remaining horizontal and vertical movement.
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        MovementInput = Vector2.zero;
        IsSprinting = false;
        LocomotionAnimationValue = 0f;

        isJumping = false;
        jumpDebugActive = false;

        // Restore the standing collider if Slide was interrupted.
        if (IsSliding)
        {
            TryEndSlide();
        }

        StopHorizontalMovement();
    }
    public void CompleteClimbMotion()
    {
        // Discard leftover upward velocity from before Climb.
        // Preserve downward velocity so gravity can continue.
        if (verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }

    public void ApplyActionMotion(
        Vector3 deltaPosition,
        Quaternion deltaRotation)
    {
        if (characterController == null)
            return;

        Vector3 positionBefore = transform.position;

        characterController.Move(deltaPosition);

        Vector3 positionAfter = transform.position;

        Debug.Log(
            $"ACTION MOTION APPLIED | Frame={Time.frameCount} | " +
            $"RequestedDelta={deltaPosition:F4} | " +
            $"ActualDelta={(positionAfter - positionBefore):F4} | " +
            $"Grounded={characterController.isGrounded} | " +
            $"CollisionFlags={characterController.collisionFlags}"
        );

        if (deltaRotation != Quaternion.identity)
        {
            transform.rotation =
                transform.rotation * deltaRotation;
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

    public bool ResetPositionForNewRound(
        Vector3 position,
        Quaternion rotation)
    {
        if (characterController == null)
            return false;

        // Remember the controller's previous enabled state.
        bool wasEnabled = characterController.enabled;

        characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        characterController.enabled = wasEnabled;

        // Clear movement carried over from the previous round.
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        MovementInput = Vector2.zero;
        IsSprinting = false;
        LocomotionAnimationValue = 0f;

        isJumping = false;
        jumpDebugActive = false;
        jumpStartHorizontalSpeed = 0f;

        hasWallContact = false;
        lastWallNormal = Vector3.zero;
        lastWallReboundTime = -Mathf.Infinity;
        lastPhysicalHorizontalVelocity = Vector3.zero;

        // Restore the standing collider at the new spawn position.
        if (IsSliding)
            TryEndSlide();

        isGrounded = characterController.enabled &&
                    characterController.isGrounded;

        return true;
    }
}