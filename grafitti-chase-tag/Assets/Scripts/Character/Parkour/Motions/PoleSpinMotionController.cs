using UnityEngine;

public class PoleSpinMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterStamina stamina;
    [SerializeField] private Animator animator;

    private Vector3 poleSpinPivot;
    private float poleSpinRadius;
    private float poleSpinAngle;
    private float poleSpinAngularSpeed;
    private float poleSpinHeight;
    private bool poleSpinClockwise;

    private CharacterActionType lastAction =
        CharacterActionType.None;

    private bool actionInitialized;

    private void Awake()
    {
        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (stamina == null)
            stamina = GetComponent<CharacterStamina>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();    
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
            ParkourActionType.PoleSpin)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentActionType != lastAction)
        {
            BeginPoleSpin();
        }

        if (animator != null &&
            animator.isMatchingTarget)
        {
            return;
        }

        if (actionInitialized)
        {
            UpdatePoleSpin();
        }
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

    private void ResetMotion()
    {
        actionInitialized = false;

        lastAction =
            CharacterActionType.None;

        poleSpinPivot =
            Vector3.zero;

        poleSpinRadius =
            0f;

        poleSpinAngle =
            0f;

        poleSpinAngularSpeed =
            0f;

        poleSpinHeight =
            0f;

        poleSpinClockwise =
            false;
    }
}