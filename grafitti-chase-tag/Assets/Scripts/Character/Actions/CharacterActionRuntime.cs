using UnityEngine;

public class CharacterActionRuntime : MonoBehaviour
{
    public bool IsExecuting { get; private set; }
    public Vector2 SteeringInput { get; private set; }
    public bool IsHoldControlled { get; private set; }
    public ActionMotionType CurrentMotionType { get; private set; }

    public event System.Action ActionCompleted;

    public CharacterActionType CurrentActionType
    {
        get;
        private set;
    }

    public CharacterActionData CurrentActionData
    {
        get;
        private set;
    }

    public ParkourActionType CurrentParkourActionType
    {
        get;
        private set;
    }

    public ParkourTarget CurrentParkourTarget
    {
        get;
        private set;
    }

    public float ActionTime
    {
        get;
        private set;
    }

    public float ActionDuration
    {
        get;
        private set;
    }

    public float ActionNormalizedTime
    {
        get
        {
            if (ActionDuration <= 0f)
                return 1f;

            return Mathf.Clamp01(
                ActionTime / ActionDuration
            );
        }
    }

    public ParkourMotionProfile CurrentParkourMotionProfile
    {
        get;
        private set;
    }

    public ParkourActionData CurrentParkourActionData
    {
        get;
        private set;
    }

    private void Update()
    {
        if (!IsExecuting)
            return;

        ActionTime += Time.deltaTime;

        // Hold-controlled actions do NOT end from ActionData duration.
        // They end when the hold is released.
        if (IsHoldControlled)
            return;

        // Normal actions remain duration-controlled.
        if (ActionDuration > 0f &&
            ActionTime >= ActionDuration)
        {
            CompleteAction();
        }
}

    public void StartAction(
        CharacterActionData actionData)
    {
        if (actionData == null)
            return;

        IsExecuting = true;

        CurrentActionData = actionData;

        IsHoldControlled = actionData.holdToExecute;

        CurrentActionType =
            actionData.actionType;

        CurrentParkourActionType =
            ParkourActionType.None;

        CurrentParkourTarget =
            default;
        
        CurrentParkourActionData = null;

        CurrentParkourMotionProfile = null;

        CurrentMotionType =
            actionData.motionType;

        ActionTime = 0f;

        ActionDuration =
            actionData.duration;

        Debug.Log(
            $"[{name}] Action Started: " +
            $"{CurrentActionType} | " +
            $"Motion: {CurrentMotionType} | " +
            $"Duration: {ActionDuration:0.00} | " +
            $"State: {actionData.actionState}",
            this
        );
    }

    public void StartParkourAction(
        ParkourActionData actionData,
        ParkourTarget target)
    {
        IsExecuting = true;

        CurrentActionData = actionData;

        IsHoldControlled = actionData.holdToExecute;

        CurrentParkourActionData = actionData;

        CurrentActionType = actionData.actionType;

        CurrentParkourActionType =
            actionData.parkourActionType;

        CurrentParkourTarget =
            target;

        CurrentMotionType =
            actionData.motionType;

        CurrentParkourMotionProfile =
            actionData.MotionProfile;

        ActionTime = 0f;

        ActionDuration =
            actionData.duration;
    }

    public void CompleteAction()
    {
        if (!IsExecuting)
            return;

        Debug.Log(
            $"[{name}] Action Completed: " +
            $"{CurrentActionType} | " +
            $"Time: {ActionTime:0.00} / " +
            $"{ActionDuration:0.00}",
            this
        );

        ResetRuntime();

        ActionCompleted?.Invoke();
    }

    public void CancelAction()
    {
        if (!IsExecuting)
            return;

        Debug.Log(
            $"[{name}] Action Cancelled: " +
            $"{CurrentActionType}",
            this
        );

        ResetRuntime();
    }

    public void SetSteeringInput(Vector2 input)
    {
        SteeringInput = input;
    }

    private void ResetRuntime()
    {
        IsHoldControlled = false;

        CurrentActionData = null;

        SteeringInput = Vector2.zero;

        IsExecuting = false;

        CurrentParkourActionData = null;

        CurrentParkourMotionProfile = null;

        CurrentActionType =
            CharacterActionType.None;

        CurrentParkourActionType =
            ParkourActionType.None;

        CurrentParkourTarget =
            default;

        CurrentMotionType =
            ActionMotionType.ScriptMotion;

        ActionTime = 0f;

        ActionDuration = 0f;
    }
}