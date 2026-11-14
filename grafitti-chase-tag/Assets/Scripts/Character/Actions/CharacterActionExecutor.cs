using System;
using UnityEngine;

public class CharacterActionExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterStateController stateController;
    [SerializeField] private CharacterStamina stamina;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterTagDetector tagDetector;
    [SerializeField] private CharacterRole characterRole;

    [Header("Debug Settings")]
    [SerializeField]
    private bool allowTagForNonChasersForDebug = false;
    
    public event Action<Collider> TagSucceeded;

    private void Awake()
    {
        if (characterMotor == null)
        {
            characterMotor =
                GetComponent<CharacterMotor>();
        }

        if (stateController == null)
        {
            stateController =
                GetComponent<CharacterStateController>();
        }

        if (stamina == null)
        {
            stamina =
                GetComponent<CharacterStamina>();
        }

        if (actionRuntime == null)
        {
            actionRuntime =
                GetComponent<CharacterActionRuntime>();
        }

        if (tagDetector == null)
        {
            tagDetector = GetComponent<CharacterTagDetector>();
        }

        if (characterRole == null)
        {
            characterRole = GetComponent<CharacterRole>();
        }
    }

    public bool Execute(CharacterActionData actionData)
    {
        if (actionData == null)
            return false;

        if (!HasValidActionState(actionData))
            return false; 

        switch (actionData.actionType)
        {
            case CharacterActionType.Jump:
                return ExecuteJump(actionData);

            case CharacterActionType.Slide:
                return ExecuteSlide(actionData);
            
            case CharacterActionType.Tag:
                return ExecuteTag(actionData);
            
            case CharacterActionType.WallRebound:
                return ExecuteWallRebound(actionData);

            default:
                return false;
        }
    }

    public bool ExecuteParkour(
        ParkourActionData actionData,
        ParkourTarget target)
    {
        if (actionData == null)
            return false;

        if (!target.IsValid)
            return false;

        if (actionRuntime.IsExecuting &&
            actionRuntime.CurrentActionType ==
            actionData.actionType)
        {
            return false;
        }

        if (stateController.IsBusy)
        {
            bool airborneAction =
                actionData.parkourActionType ==
                    ParkourActionType.Climb ||
                actionData.parkourActionType ==
                    ParkourActionType.TicTac;

            bool airborneState =
                stateController.CurrentState ==
                    CharacterState.Jumping ||
                stateController.CurrentState ==
                    CharacterState.Falling;

            if (!airborneAction || !airborneState)
            {
                return false;
            }
        }

        if (!HasValidActionState(actionData))
            return false;

        if (!TryConsumeActionStamina(actionData))
            return false;

        actionRuntime.StartParkourAction(
            actionData,
            target
        );

        SetActionState(actionData);

        return true;
    }

    private bool ExecuteJump(
        CharacterActionData actionData)
    {
        if (characterMotor == null)
            return false;

        if (!characterMotor.IsGrounded)
            return false;

        if (stateController.IsBusy)
        {
            return false;
        }

        if (!TryConsumeActionStamina(actionData))
            return false;

        bool jumped =
            characterMotor.PerformJump();

        if (!jumped)
            return false;

        actionRuntime.StartAction(
            actionData
        );

        SetActionState(actionData);

        return true;
    }

    private bool ExecuteSlide(CharacterActionData actionData)
    {
        if (characterMotor == null)
            return false;

        if (!characterMotor.IsGrounded)
            return false;

        if (!characterMotor.IsSprinting)
            return false;

        if (stateController.IsBusy)
        {
            return false;
        }

        if (actionData.actionState == CharacterState.Locomotion)
        {
            return false;
        }

        if (!TryConsumeActionStamina(actionData))
            return false;

        actionRuntime.StartAction(actionData);

        if (!characterMotor.BeginSlide())
        {
            actionRuntime.CancelAction();
            return false;
        }

        SetActionState(actionData);

        return true;
    }

    private bool CanPerformTag()
    {
        if (allowTagForNonChasersForDebug)
            return true;

        return characterRole != null &&
            characterRole.IsChaser;
    }

    private bool ExecuteTag(CharacterActionData actionData)
    {
        if (!CanPerformTag())
            return false;

        if (stateController.IsBusy)
            return false;

        if (tagDetector == null)
            return false;

        // Detection determines whether the Tag succeeds,
        // not whether the Tag action can be performed.
        bool hasTarget =
            tagDetector.TryDetectTag(out Collider target);

        if (!TryConsumeActionStamina(actionData))
            return false;

        // Start the Tag action regardless of detection result.
        actionRuntime.StartAction(actionData);

        SetActionState(actionData);

        // Only successful detection triggers gameplay tagging.
        if (hasTarget)
        {
            TagSucceeded?.Invoke(target);
        }

        return true;
    }
    
    private bool ExecuteWallRebound(
        CharacterActionData actionData)
    {
        if (characterMotor == null ||
            actionRuntime == null ||
            stateController == null)
        {
            return false;
        }

        // Do not interrupt another action.
        if (actionRuntime.IsExecuting)
            return false;

        // Wall Rebound is allowed while airborne.
        CharacterState currentState =
            stateController.CurrentState;

        bool validState =
            currentState == CharacterState.Locomotion ||
            currentState == CharacterState.Jumping ||
            currentState == CharacterState.Falling;

        if (!validState)
            return false;

        if (actionData.actionState !=
            CharacterState.WallRebound)
        {
            return false;
        }

        if (!TryConsumeActionStamina(actionData))
            return false;

        actionRuntime.StartAction(actionData);

        SetActionState(actionData);

        Debug.Log(
            $"WALL REBOUND EXECUTED | " +
            $"PreviousState={currentState} | " +
            $"NewState={actionData.actionState}"
        );

        return true;
    }

    private bool TryConsumeActionStamina(
        CharacterActionData actionData)
    {
        if (actionData == null)
            return false;

        if (stamina == null)
            return false;

        return stamina.TryConsume(
            actionData.staminaCost
        );
    }

    private void SetActionState(CharacterActionData actionData)
    {
        stateController.SetState(actionData.actionState);
    }

    private bool HasValidActionState(
        CharacterActionData actionData)
    {
        if (actionData == null)
            return false;

        if (actionData.actionState == CharacterState.Locomotion)
        {
            return false;
        }

        return true;
    }
}