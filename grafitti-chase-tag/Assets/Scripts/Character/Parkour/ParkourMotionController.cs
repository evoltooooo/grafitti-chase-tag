using UnityEngine;

public class ParkourMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterStamina stamina;

    private ParkourMotionProfile currentProfile;

    private Vector3 actionStartPosition;

    private CharacterActionType lastAction =
        CharacterActionType.None;

    private bool actionInitialized;

    // =========================================================
    // TIC TAC RUNTIME VALUES
    // =========================================================

    private Vector3 ticTacDirection;
    private float ticTacHorizontalSpeed;
    private float ticTacVerticalVelocity;

    // =========================================================
    // POLE SPIN RUNTIME VALUES
    // =========================================================

    private Vector3 poleSpinPivot;
    private float poleSpinRadius;
    private float poleSpinAngle;
    private float poleSpinAngularSpeed;
    private float poleSpinHeight;
    private bool poleSpinClockwise;

    private void Awake()
    {
        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (stamina == null)
            stamina = GetComponent<CharacterStamina>(); 
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

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.None)
            return;

        // =====================================================
        // TIC TAC
        // =====================================================

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.TicTac)
        {
            if (actionRuntime.CurrentActionType != lastAction)
            {
                BeginTicTac();
            }

            if (actionInitialized)
            {
                UpdateTicTac();
            }

            return;
        }

        // =====================================================
        // POLE SPIN
        // =====================================================

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.PoleSpin)
        {
            if (actionRuntime.CurrentActionType != lastAction)
            {
                BeginPoleSpin();
            }

            if (actionInitialized)
            {
                UpdatePoleSpin();
            }

            return;
        }
        

        // =====================================================
        // NORMAL PARKOUR MOTION
        // =====================================================

        if (actionRuntime.CurrentMotionType !=
                ActionMotionType.ScriptMotion &&
            actionRuntime.CurrentMotionType !=
                ActionMotionType.Hybrid)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentActionType != lastAction)
            BeginAction();

        if (!actionInitialized ||
            currentProfile == null)
            return;

        UpdateMotion();
    }

    // =========================================================
    // NORMAL PARKOUR ACTION
    // =========================================================

    private void BeginAction()
    {
        lastAction =
            actionRuntime.CurrentActionType;

        currentProfile =
            actionRuntime.CurrentParkourMotionProfile;

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        if (currentProfile == null ||
            !target.IsValid)
        {
            actionInitialized = false;
            return;
        }

        actionStartPosition =
            target.TakeoffPosition;

        actionInitialized = true;
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

        Debug.Log(
            $"PARKOUR MOTION DEBUG | " +
            $"Time={normalizedTime:F2} | " +
            $"Start={actionStartPosition} | " +
            $"Target={target.LandingPosition} | " +
            $"Desired={desiredPosition} | " +
            $"Current={transform.position}"
        );

        Quaternion desiredRotation =
            currentProfile.EvaluateRotation(
                transform.rotation,
                target,
                normalizedTime
            );

        Vector3 deltaPosition =
            desiredPosition -
            transform.position;

        characterMotor.ApplyParkourMotion(
            deltaPosition,
            desiredRotation,
            currentProfile.rotationSpeed
        );
    }

    // =========================================================
    // TIC TAC
    // =========================================================

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

        // -----------------------------------------------------
        // CAPTURE INCOMING MOVEMENT
        // -----------------------------------------------------

        Vector3 incomingVelocity =
            characterMotor.HorizontalVelocity;

        Vector3 incomingDirection =
            incomingVelocity;

        incomingDirection.y = 0f;

        // If there is almost no horizontal velocity,
        // fall back to the character's forward direction.
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

        // -----------------------------------------------------
        // WALL NORMAL
        // -----------------------------------------------------

        Vector3 wallNormal =
            target.SurfaceNormal;

        wallNormal.y = 0f;

        if (wallNormal.sqrMagnitude < 0.001f)
        {
            actionInitialized = false;
            return;
        }

        wallNormal.Normalize();

        // -----------------------------------------------------
        // CALCULATE BOUNCE
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // BOUNCE SPEED
        // -----------------------------------------------------

        float incomingSpeed =
            incomingVelocity.magnitude;

        ticTacHorizontalSpeed =
            Mathf.Max(
                incomingSpeed,
                actionRuntime.CurrentParkourActionData.TicTacSettings.minBounceSpeed
            );

        // -----------------------------------------------------
        // VERTICAL LAUNCH
        // -----------------------------------------------------

        ticTacVerticalVelocity =
            actionRuntime.CurrentParkourActionData.TicTacSettings.verticalLaunchSpeed;
        
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
        // -----------------------------------------------------
        // GRAVITY
        // -----------------------------------------------------

        float gravity =
            GetGravity();

        ticTacVerticalVelocity +=
            gravity *
            Time.deltaTime;

        // -----------------------------------------------------
        // HORIZONTAL
        // -----------------------------------------------------

        Vector3 horizontalMovement =
            ticTacDirection *
            ticTacHorizontalSpeed *
            Time.deltaTime;

        // -----------------------------------------------------
        // VERTICAL
        // -----------------------------------------------------

        Vector3 verticalMovement =
            Vector3.up *
            ticTacVerticalVelocity *
            Time.deltaTime;

        Vector3 deltaPosition =
            horizontalMovement +
            verticalMovement;

        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

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

    private void BeginPoleSpin()
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

        poleSpinPivot =
            target.PivotPosition;

        Vector3 fromPivot =
            transform.position -
            poleSpinPivot;

        fromPivot.y = 0f;

        if (fromPivot.sqrMagnitude < 0.001f)
        {
            actionInitialized = false;
            return;
        }

        poleSpinRadius =
            fromPivot.magnitude;

        poleSpinAngle =
            Mathf.Atan2(
                fromPivot.z,
                fromPivot.x
            );

        poleSpinHeight =
            transform.position.y;

        // Determine spin direction from the character's
        // current movement/orientation around the pole.
        Vector3 radialDirection =
            fromPivot.normalized;

        Vector3 tangent =
            Vector3.Cross(
                Vector3.up,
                radialDirection
            );

        float directionDot =
            Vector3.Dot(
                transform.forward,
                tangent
            );

        poleSpinClockwise =
            directionDot < 0f;

        poleSpinAngularSpeed =
            actionRuntime.CurrentParkourActionData.PoleSpinSettings.angularSpeed;

        actionInitialized = true;

        Debug.Log(
            $"POLE SPIN MOTION STARTED | " +
            $"Pivot={poleSpinPivot} | " +
            $"Radius={poleSpinRadius:F2} | " +
            $"Angle={poleSpinAngle:F2} | " +
            $"Clockwise={poleSpinClockwise} | " +
            $"AngularSpeed={poleSpinAngularSpeed:F2}",
            this
        );
    }

    private void UpdatePoleSpin()
    {
        // -----------------------------------------------------
        // 1. UPDATE ORBIT ANGLE
        // -----------------------------------------------------

        float direction =
            poleSpinClockwise ? -1f : 1f;

        float previousAngle =
            poleSpinAngle;

        poleSpinAngle +=
            direction *
            poleSpinAngularSpeed *
            Time.deltaTime;

        float angleDelta =
            Mathf.Abs(
                poleSpinAngle - previousAngle
            );

        float revolutions =
            angleDelta / (Mathf.PI * 2f);

        float staminaCost =
            revolutions *
            actionRuntime.CurrentParkourActionData.PoleSpinSettings.staminaPerRevolution;

        if (stamina != null &&
            staminaCost > 0f)
        {
            if (!stamina.TryConsume(staminaCost))
            {
                actionRuntime.CompleteAction();
                return;
            }
        }

        // -----------------------------------------------------
        // 2. CALCULATE NEW ORBIT POSITION
        // -----------------------------------------------------

        Vector3 orbitOffset =
            new Vector3(
                Mathf.Cos(poleSpinAngle),
                0f,
                Mathf.Sin(poleSpinAngle)
            ) *
            poleSpinRadius;

        Vector3 desiredPosition =
            poleSpinPivot +
            orbitOffset;

        desiredPosition.y =
            poleSpinHeight;

        // -----------------------------------------------------
        // 3. CALCULATE MOVEMENT DELTA
        // -----------------------------------------------------

        Vector3 deltaPosition =
            desiredPosition -
            transform.position;

        // -----------------------------------------------------
        // 4. FACE TANGENT TO THE ORBIT
        // -----------------------------------------------------

        Vector3 radialDirection =
            desiredPosition -
            poleSpinPivot;

        radialDirection.y = 0f;

        if (radialDirection.sqrMagnitude < 0.001f)
            return;

        radialDirection.Normalize();

        Vector3 tangentDirection =
            poleSpinClockwise
                ? Vector3.Cross(
                    radialDirection,
                    Vector3.up
                )
                : Vector3.Cross(
                    Vector3.up,
                    radialDirection
                );

        tangentDirection.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                tangentDirection,
                Vector3.up
            );

        // -----------------------------------------------------
        // 5. MOVE THROUGH CHARACTER MOTOR
        // -----------------------------------------------------

        characterMotor.ApplyParkourMotion(
            deltaPosition,
            targetRotation,
            actionRuntime.CurrentParkourActionData.PoleSpinSettings.rotationSpeed
        );
    }

    private float GetGravity()
    {
        return characterMotor.Gravity;
    }
    

    // =========================================================
    // RESET
    // =========================================================

    private void ResetMotion()
    {
        currentProfile = null;

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