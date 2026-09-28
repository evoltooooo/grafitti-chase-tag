using UnityEngine;

public class ParkourTrajectoryMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    private ParkourMotionProfile currentProfile;
    private Vector3 actionStartPosition;

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

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.None)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.PoleSpin)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentMotionType !=
                ActionMotionType.ScriptMotion &&
            actionRuntime.CurrentMotionType !=
                ActionMotionType.Hybrid)
        {
            ResetMotion();
            return;
        }

        if (actionRuntime.CurrentActionType != lastAction)
        {
            BeginAction();
        }

        if (!actionInitialized ||
            currentProfile == null)
        {
            return;
        }

        UpdateMotion();
    }

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

    private void ResetMotion()
    {
        currentProfile = null;

        actionStartPosition =
            Vector3.zero;

        actionInitialized = false;

        lastAction =
            CharacterActionType.None;
    }
}