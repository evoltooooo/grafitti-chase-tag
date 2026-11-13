using UnityEngine;

public class ParkourTrajectoryMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Vault Landing")]
    [Range(0.0f, 0.95f)]
    [SerializeField] private float vaultLandingApproachStartTime = 0.7f;

    private ParkourMotionProfile currentProfile;
    private Vector3 actionStartPosition;

    private Vector3 vaultLandingStartPosition;
    private bool vaultLandingApproachInitialized;

    private CharacterActionType lastAction =
        CharacterActionType.None;

    private ParkourActionType lastParkourAction =
        ParkourActionType.None;

    private bool actionInitialized;

    // CharacterAnimator uses this to prevent horizontal root motion
    // from competing with the final landing movement.
    public bool ShouldSuppressVaultHorizontalRootMotion =>
        actionRuntime != null &&
        actionRuntime.IsExecuting &&
        actionRuntime.CurrentParkourActionType ==
            ParkourActionType.Vault &&
        actionRuntime.CurrentMotionType ==
            ActionMotionType.TargetMatch &&
        actionRuntime.ActionNormalizedTime >=
            vaultLandingApproachStartTime;
    
    private float previousClimbForwardProgress;

    private void Awake()
    {
        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();
    }

    private void Update()
    {
        if (characterMotor == null || actionRuntime == null)
            return;

        if (!actionRuntime.IsExecuting)
        {
            ResetMotion();
            return;
        }

        ParkourActionType parkourType =
            actionRuntime.CurrentParkourActionType;

        if (parkourType == ParkourActionType.None ||
            parkourType == ParkourActionType.PoleSpin)
        {
            ResetMotion();
            return;
        }

        // VAULT: TARGET MATCH + VERTICAL COLLIDER CORRECTION
        if (parkourType == ParkourActionType.Vault &&
            actionRuntime.CurrentMotionType ==
                ActionMotionType.TargetMatch)
        {
            if (actionRuntime.CurrentActionType != lastAction ||
                parkourType != lastParkourAction)
            {
                BeginAction();
            }

            if (!actionInitialized || currentProfile == null)
            {
                characterMotor.ResetParkourColliderVerticalOffset();
                return;
            }

            UpdateVaultColliderTrajectory();
            UpdateVaultLandingApproach();
            return;
        }

        // CLIMB: SUPPORT ALL MOTION TYPES
        if (parkourType == ParkourActionType.Climb)
        {
            if (actionRuntime.CurrentActionType != lastAction ||
                parkourType != lastParkourAction)
            {
                BeginAction();
            }

            if (actionRuntime.CurrentMotionType ==
                    ActionMotionType.ScriptMotion ||
                actionRuntime.CurrentMotionType ==
                    ActionMotionType.Hybrid)
            {
                if (actionInitialized && currentProfile != null)
                    UpdateMotion();
            }
            else
            {
                UpdateClimbTargetMatchForwardMovement();
            }

            return;
        }

        // EXISTING SCRIPT / HYBRID TRAJECTORY MOVEMENT
        if (actionRuntime.CurrentMotionType !=
                ActionMotionType.ScriptMotion &&
            actionRuntime.CurrentMotionType !=
                ActionMotionType.Hybrid)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentActionType != lastAction ||
            parkourType != lastParkourAction)
        {
            BeginAction();
        }

        if (!actionInitialized || currentProfile == null)
            return;

        UpdateMotion();
    }

    private void BeginAction()
    {
        previousClimbForwardProgress = 0f;
        characterMotor.ResetParkourColliderVerticalOffset();

        lastAction = actionRuntime.CurrentActionType;
        lastParkourAction = actionRuntime.CurrentParkourActionType;

        currentProfile =
            actionRuntime.CurrentParkourMotionProfile;

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        vaultLandingApproachInitialized = false;
        vaultLandingStartPosition = Vector3.zero;

        if (currentProfile == null || !target.IsValid)
        {
            actionInitialized = false;
            return;
        }

        actionStartPosition = target.TakeoffPosition;
        actionInitialized = true;
    }

    private void UpdateVaultColliderTrajectory()
    {
        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        float normalizedTime =
            actionRuntime.ActionNormalizedTime;

        Vector3 desiredPosition =
            currentProfile.EvaluatePosition(
                actionStartPosition,
                target,
                normalizedTime
            );

        float verticalOffset =
            desiredPosition.y - actionStartPosition.y;

        verticalOffset = Mathf.Max(0f, verticalOffset);

        characterMotor.SetParkourColliderVerticalOffset(
            verticalOffset
        );
    }

    private void UpdateVaultLandingApproach()
    {
        float normalizedTime =
            actionRuntime.ActionNormalizedTime;

        if (normalizedTime < vaultLandingApproachStartTime)
            return;

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        if (!target.IsValid)
            return;

        // Capture the character's actual position when the
        // final landing approach begins.
        if (!vaultLandingApproachInitialized)
        {
            vaultLandingStartPosition = transform.position;
            vaultLandingApproachInitialized = true;
        }

        float progress = Mathf.InverseLerp(
            vaultLandingApproachStartTime,
            1f,
            normalizedTime
        );

        progress = Mathf.SmoothStep(0f, 1f, progress);

        // Move horizontally toward the detected landing point.
        // Preserve the current root height.
        Vector3 landingPosition = target.LandingPosition;
        landingPosition.y = vaultLandingStartPosition.y;

        Vector3 desiredPosition = Vector3.Lerp(
            vaultLandingStartPosition,
            landingPosition,
            progress
        );

        Vector3 deltaPosition =
            desiredPosition - transform.position;

        deltaPosition.y = 0f;

        characterMotor.ApplyParkourMotion(
            deltaPosition,
            transform.rotation,
            0f
        );
    }

    private void UpdateMotion()
    {
        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        float normalizedTime =
            actionRuntime.ActionNormalizedTime;

        Vector3 desiredPosition =
            currentProfile.EvaluatePosition(
                actionStartPosition,
                target,
                normalizedTime
            );

        // Add the Climb forward offset to the trajectory.
        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.Climb)
        {
            desiredPosition += GetClimbForwardOffset();
        }

        Quaternion desiredRotation =
            currentProfile.EvaluateRotation(
                transform.rotation,
                target,
                normalizedTime
            );

        Vector3 deltaPosition =
            desiredPosition - transform.position;

        characterMotor.ApplyParkourMotion(
            deltaPosition,
            desiredRotation,
            currentProfile.rotationSpeed
        );
    }

    private Vector3 GetClimbForwardOffset()
    {
        ParkourActionData actionData =
            actionRuntime.CurrentParkourActionData;

        if (actionData == null)
            return Vector3.zero;

        float normalizedTime =
            actionRuntime.ActionNormalizedTime;

        float startTime =
            actionData.climbForwardMovementStartTime;

        if (normalizedTime < startTime)
            return Vector3.zero;

        float progress = Mathf.InverseLerp(
            startTime,
            1f,
            normalizedTime
        );

        progress = Mathf.SmoothStep(0f, 1f, progress);

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        Vector3 forward =
            target.TargetRotation * Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        forward.Normalize();

        return forward *
            actionData.climbForwardMovementDistance *
            progress;
    }

    private void UpdateClimbTargetMatchForwardMovement()
    {
        Vector3 desiredOffset = GetClimbForwardOffset();

        // Calculate only the additional movement since last frame.
        Vector3 previousOffset =
            GetClimbForwardDirection() *
            previousClimbForwardProgress;

        Vector3 deltaMovement =
            desiredOffset - previousOffset;

        previousClimbForwardProgress =
            desiredOffset.magnitude;

        if (deltaMovement.sqrMagnitude < 0.000001f)
            return;

        characterMotor.ApplyParkourMotion(
            deltaMovement,
            transform.rotation,
            0f
        );
    }

    private Vector3 GetClimbForwardDirection()
    {
        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        Vector3 forward =
            target.TargetRotation * Vector3.forward;

        forward.y = 0f;

        return forward.normalized;
    }

    private void ResetMotion()
    {
        if (characterMotor != null)
            characterMotor.ResetParkourColliderVerticalOffset();
        
        previousClimbForwardProgress = 0f;

        currentProfile = null;
        actionStartPosition = Vector3.zero;

        vaultLandingStartPosition = Vector3.zero;
        vaultLandingApproachInitialized = false;

        actionInitialized = false;
        lastAction = CharacterActionType.None;
        lastParkourAction = ParkourActionType.None;
    }

    private void OnDisable()
    {
        if (characterMotor != null)
            characterMotor.ResetParkourColliderVerticalOffset();
    }
}
