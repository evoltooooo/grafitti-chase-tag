using UnityEngine;

[DisallowMultipleComponent]
public class CharacterVFXController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Dust Prefabs")]
    [SerializeField] private ParticleSystem normalDustPrefab;
    [SerializeField] private ParticleSystem sprintDustPrefab;
    [SerializeField] private ParticleSystem slideDustPrefab;

    [Header("Dust Spawn Points")]
    [SerializeField] private Transform groundPoint;
    [SerializeField] private Transform slidePoint;

    [Header("Locomotion Detection")]
    [SerializeField, Min(0f)]
    private float movementStartThreshold = 0.25f;

    [Header("Shockwave")]
    [SerializeField] private ParticleSystem shockwavePrefab;
    [SerializeField, Min(0f)] private float wallShockwaveOffset = 0.15f;

    [Header("Landing Shockwave Requirements")]
    [SerializeField, Min(0f)]
    private float minimumFallHeight = 2f;

    [SerializeField, Min(0f)]
    private float minimumFallingDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float fallingVelocityThreshold = 0.1f;

    [Header("Character Trail")]
    [SerializeField] private TrailRenderer[] characterTrails;

    private bool wasMoving;
    private bool wasSprinting;
    private bool wasSliding;

    private ParticleSystem slideDustInstance;
    private bool wasGrounded;
    private bool isTrackingFall;
    private float fallStartHeight;
    private float fallStartTime;
    private bool hasInitializedGroundState;
    private bool wasTrailActive;
    private int previousMovementMode;

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<CharacterMotor>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        PrepareSlideDust();

        if (characterTrails != null)
        {
            foreach (TrailRenderer trail in characterTrails)
            {
                if (trail == null)
                    continue;

                trail.emitting = false;
                trail.Clear();
            }
        }
    }

    private void OnEnable()
    {
        if (actionRuntime != null)
            actionRuntime.ActionStarted += HandleActionStarted;
    }

    private void OnDisable()
    {
        if (actionRuntime != null)
            actionRuntime.ActionStarted -= HandleActionStarted;

        if (slideDustInstance != null)
        {
            slideDustInstance.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );
        }
    }

    private void Update()
    {
        if (motor == null)
            return;

        UpdateLandingShockwave();
        UpdateSprintDust();
        UpdateLocomotionDust();
        UpdateSlideDust();
        UpdateCharacterTrail();
    }

    private void UpdateLandingShockwave()
    {
        bool isGrounded = motor.IsGrounded;

        // Initialize without triggering an effect at game start.
        if (!hasInitializedGroundState)
        {
            wasGrounded = isGrounded;
            hasInitializedGroundState = true;
            return;
        }

        // Start tracking only when the character is actually descending.
        if (!isGrounded &&
            !isTrackingFall &&
            motor.VerticalVelocity < -fallingVelocityThreshold)
        {
            isTrackingFall = true;

            fallStartHeight = transform.position.y;
            fallStartTime = Time.time;
        }

        // Detect landing after a tracked fall.
        if (!wasGrounded && isGrounded)
        {
            if (isTrackingFall)
            {
                float fallHeight =
                    fallStartHeight - transform.position.y;

                float fallingDuration =
                    Time.time - fallStartTime;

                bool reachedHeightRequirement =
                    fallHeight >= minimumFallHeight;

                bool reachedDurationRequirement =
                    fallingDuration >= minimumFallingDuration;

                if (reachedHeightRequirement ||
                    reachedDurationRequirement)
                {
                    Vector3 landingPosition = groundPoint != null
                        ? groundPoint.position
                        : transform.position;

                    SpawnShockwaveAt(
                        landingPosition,
                        Vector3.up
                    );

                    Debug.Log(
                        $"LANDING SHOCKWAVE | " +
                        $"Fall Height={fallHeight:F2} | " +
                        $"Falling Duration={fallingDuration:F2}"
                    );
                }
            }

            // Reset after landing.
            isTrackingFall = false;
        }

        wasGrounded = isGrounded;
    }

    // =========================================================
    // ACTION DUST
    // =========================================================

    private void HandleActionStarted()
    {
        if (actionRuntime == null)
            return;

        switch (actionRuntime.CurrentActionType)
        {
            case CharacterActionType.Jump:
                PlayJumpDust();
                break;

            case CharacterActionType.Vault:
                PlayParkourDust();
                break;

            case CharacterActionType.Climb:
                // No takeoff dust for Climb.
                break;

            case CharacterActionType.PoleSpin:
            case CharacterActionType.WallRebound:
                PlayNormalDust();
                break;

            case CharacterActionType.TicTac:
                PlayNormalDust();
                PlayTicTacShockwave();
                break;

            case CharacterActionType.Slide:
                // Continuous emission is handled by IsSliding.
                break;
        }
    }

    private void PlayTicTacShockwave()
    {
        ParkourTarget target = actionRuntime.CurrentParkourTarget;

        if (!target.IsValid)
        {
            Debug.LogWarning(
                "TIC TAC SHOCKWAVE | Invalid ParkourTarget."
            );
            return;
        }

        PlayTargetContactShockwave(
            target.InteractionPosition,
            target.SurfaceNormal
        );
    }


    // =========================================================
    // LOCOMOTION DUST
    // =========================================================
    
    private void UpdateLocomotionDust()
    {
        bool isMoving =
            motor.IsGrounded &&
            motor.HorizontalSpeed > movementStartThreshold &&
            !motor.IsSliding;

        Vector2 input = motor.MovementInput;

        int currentMovementMode = 0;

        if (input.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                currentMovementMode = 2; // Strafe
            else if (input.y < 0f)
                currentMovementMode = 3; // Backward
            else
                currentMovementMode = 1; // Forward
        }

        bool startedMoving = isMoving && !wasMoving;

        bool changedMovementMode =
            isMoving &&
            currentMovementMode != 0 &&
            previousMovementMode != 0 &&
            currentMovementMode != previousMovementMode;

        if ((startedMoving || changedMovementMode) &&
            !motor.IsSprinting &&
            (actionRuntime == null || !actionRuntime.IsExecuting))
        {
            PlayNormalDust();
        }

        wasMoving = isMoving;

        if (isMoving && currentMovementMode != 0)
            previousMovementMode = currentMovementMode;
        else if (!isMoving)
            previousMovementMode = 0;
    }

    // =========================================================
    // SPRINT DUST
    // =========================================================

    private void UpdateSprintDust()
    {
        bool startedSprinting =
            motor.IsSprinting && !wasSprinting;

        if (startedSprinting)
            PlaySprintDust();

        wasSprinting = motor.IsSprinting;
    }

    // =========================================================
    // SLIDE DUST
    // =========================================================

    private void PrepareSlideDust()
    {
        if (slideDustPrefab == null || slidePoint == null)
            return;

        slideDustInstance = Instantiate(
            slideDustPrefab,
            slidePoint.position,
            slidePoint.rotation,
            slidePoint
        );

        slideDustInstance.transform.localPosition = Vector3.zero;
        slideDustInstance.transform.localRotation = Quaternion.identity;

        slideDustInstance.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    private void UpdateSlideDust()
    {
        bool isSliding = motor.IsSliding;

        if (isSliding)
        {
            if (slideDustInstance != null)
            {
                Vector3 direction = GetSlideDustDirection();

                slideDustInstance.transform.rotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        direction
                    );

                if (!wasSliding)
                    slideDustInstance.Play();
            }
        }
        else if (wasSliding)
        {
            if (slideDustInstance != null)
            {
                slideDustInstance.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting
                );
            }
        }

        wasSliding = isSliding;
    }

    // =========================================================
    // DUST SPAWNING
    // =========================================================

    private Vector3 GetDustDirection()
    {
        Vector3 movementDirection = motor != null
            ? motor.HorizontalVelocity
            : Vector3.zero;

        if (movementDirection.sqrMagnitude > 0.01f)
            return -movementDirection.normalized;

        // Fallback when there is no horizontal movement.
        return -transform.forward;
    }

    private void PlayNormalDust()
    {
        SpawnBurst(normalDustPrefab, groundPoint, GetDustDirection());
    }

    private void PlaySprintDust()
    {
        SpawnBurst(sprintDustPrefab, groundPoint, GetDustDirection());
    }

    private void SpawnBurst(
        ParticleSystem prefab,
        Transform spawnPoint,
        Vector3 direction)
    {
        if (prefab == null)
            return;

        Vector3 position = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        Quaternion rotation = spawnPoint != null
            ? spawnPoint.rotation
            : transform.rotation;

        // Align the particle emitter's local Y axis with the desired direction.
        Quaternion directionRotation =
            Quaternion.FromToRotation(rotation * Vector3.up, direction.normalized);

        ParticleSystem instance = Instantiate(
            prefab,
            position,
            directionRotation * rotation
        );

        ParticleSystem.MainModule main = instance.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        instance.Play();
    }

    private Vector3 GetJumpDustDirection()
    {
        Vector3 horizontalDirection = GetDustDirection();

        return (
            horizontalDirection * 0.6f +
            Vector3.up * 0.8f
        ).normalized;
    }

    private void PlayJumpDust()
    {
        SpawnBurst(
            normalDustPrefab,
            groundPoint,
            GetJumpDustDirection()
        );
    }
    
    private void PlayParkourDust()
    {
        Vector3 spawnPosition =
            groundPoint != null
                ? groundPoint.position
                : transform.position;

        if (actionRuntime != null)
        {
            ParkourTarget target = actionRuntime.CurrentParkourTarget;

            if (target.IsValid &&
                target.TakeoffPosition != Vector3.zero)
            {
                spawnPosition = target.TakeoffPosition;
            }
        }

        Vector3 dustDirection = (
            GetDustDirection() * 0.6f +
            Vector3.up * 0.4f
        ).normalized;

        SpawnBurstAt(
            normalDustPrefab,
            spawnPosition,
            dustDirection
        );
    }

    private void SpawnBurstAt(
        ParticleSystem prefab,
        Vector3 position,
        Vector3 direction)
    {
        if (prefab == null)
            return;

        Quaternion rotation = Quaternion.FromToRotation(
            Vector3.up,
            direction.normalized
        );

        ParticleSystem instance = Instantiate(
            prefab,
            position,
            rotation
        );

        ParticleSystem.MainModule main = instance.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        instance.Play();
    }

    private Vector3 GetSlideDustDirection()
    {
        Vector3 movementDirection = motor.HorizontalVelocity;

        if (movementDirection.sqrMagnitude < 0.01f)
            movementDirection = transform.forward;

        return (
            -movementDirection.normalized * 0.85f +
            Vector3.up * 0.35f
        ).normalized;
    }

    public void PlayTargetContactDust(
        Vector3 contactPosition,
        Vector3 surfaceNormal)
    {
        Vector3 direction = surfaceNormal;

        if (direction.sqrMagnitude < 0.001f)
            direction = -GetDustDirection();

        SpawnBurstAt(
            normalDustPrefab,
            contactPosition,
            direction.normalized
        );

        Debug.Log(
            $"TARGET CONTACT DUST | " +
            $"Position={contactPosition:F3} | " +
            $"Direction={direction.normalized:F3}"
        );
    }

    public void PlayTargetContactShockwave(
        Vector3 contactPosition,
        Vector3 surfaceNormal)
    {
        SpawnShockwaveAt(
            contactPosition,
            surfaceNormal
        );

        Debug.Log(
            $"TARGET CONTACT SHOCKWAVE | " +
            $"Position={contactPosition:F3} | " +
            $"Normal={surfaceNormal.normalized:F3}"
        );
    }

    private void SpawnShockwaveAt(
        Vector3 position,
        Vector3 surfaceNormal)
    {
        if (shockwavePrefab == null)
            return;

        Quaternion rotation = Quaternion.FromToRotation(
            Vector3.forward,
            surfaceNormal.normalized
        );

        ParticleSystem instance = Instantiate(
            shockwavePrefab,
            position,
            rotation
        );

        ParticleSystem.MainModule main = instance.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        instance.Play();
    }

    private void UpdateCharacterTrail()
    {
        if (characterTrails == null ||
            characterTrails.Length == 0)
            return;

        bool isSprintTrailActive =
            motor != null && motor.IsSprinting;

        bool isPoleSpinTrailActive =
            actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentParkourActionType ==
                ParkourActionType.PoleSpin;

        bool shouldEmit =
            isSprintTrailActive || isPoleSpinTrailActive;

        foreach (TrailRenderer trail in characterTrails)
        {
            if (trail == null)
                continue;

            if (shouldEmit && !wasTrailActive)
            {
                trail.Clear();
            }

            trail.emitting = shouldEmit;
        }

        wasTrailActive = shouldEmit;
    }
}
