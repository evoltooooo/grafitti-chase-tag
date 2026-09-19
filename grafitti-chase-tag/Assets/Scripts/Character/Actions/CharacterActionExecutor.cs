using UnityEngine;

public class CharacterActionExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterStateController stateController;
    [SerializeField] private CharacterStamina stamina;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterTagDetector tagDetector;

    private void Awake()
    {
        if (characterMotor == null)
        {
            characterMotor =
                GetComponent<CharacterMotor>();
        }

        if (characterMotor == null)
        {
            Debug.LogError(
                "CharacterActionExecutor: CharacterMotor is missing.",
                this
            );
        }

        if (stateController == null)
        {
            stateController =
                GetComponent<CharacterStateController>();
        }

        if (stateController == null)
        {
            Debug.LogError(
                "CharacterActionExecutor: CharacterStateController is missing.",
                this
            );
        }

        if (stamina == null)
        {
            stamina =
                GetComponent<CharacterStamina>();
        }

        if (stamina == null)
        {
            Debug.LogError(
                "CharacterActionExecutor: CharacterStamina is missing.",
                this
            );
        }

        if (actionRuntime == null)
        {
            actionRuntime =
                GetComponent<CharacterActionRuntime>();
        }

        if (actionRuntime == null)
        {
            Debug.LogError(
                "CharacterActionExecutor: CharacterActionRuntime is missing.",
                this
            );
        }

        if (tagDetector == null)
        {
            tagDetector = GetComponent<CharacterTagDetector>();
        }

        if (tagDetector == null)
        {
            Debug.LogError(
                "CharacterActionExecutor: CharacterTagDetector is missing.",
                this
            );
        }
    }

    public bool Execute(CharacterActionData actionData)
    {
        if (actionData == null)
            return false;

        if (stamina != null && stamina.IsEmpty)
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

        if (stamina != null && stamina.IsEmpty)
            return false;

        if (!target.IsValid)
            return false;

        if (actionRuntime.IsExecuting &&
            actionRuntime.CurrentActionType ==
            actionData.actionType)
        {
            Debug.Log(
                $"PARKOUR EXECUTION FAILED | " +
                $"Action {actionData.actionType} is already executing."
            );

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
                Debug.Log(
                    "PARKOUR EXECUTION FAILED | " +
                    "Character is Busy."
                );

                return false;
            }

            Debug.Log(
                "PARKOUR EXECUTION BUSY OVERRIDE | " +
                $"{actionData.parkourActionType} " +
                "allowed during airborne state."
            );
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

        Debug.Log(
            $"[{name}] Parkour execution started.\n" +
            $"Action: {actionData.actionType}\n" +
            $"Parkour: {actionData.parkourActionType}\n" +
            $"State: {actionData.actionState}\n" +
            $"Interaction Position: {target.InteractionPosition}\n" +
            $"Landing Position: {target.LandingPosition}",
            this
        );

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
            Debug.Log(
                "SLIDE EXECUTION FAILED | " +
                "ActionData has no valid action state."
            );

            return false;
        }

        if (!TryConsumeActionStamina(actionData))
            return false;

        actionRuntime.StartAction(actionData);

        SetActionState(actionData);

        return true;
    }

    private bool ExecuteTag(CharacterActionData actionData)
    {
        if (stateController.IsBusy)
            return false;

        if (tagDetector == null)
            return false;

        if (!tagDetector.TryDetectTag(out Collider target))
            return false;

        if (!TryConsumeActionStamina(actionData))
            return false;

        actionRuntime.StartAction(actionData);
        SetActionState(actionData);

        Debug.Log(
            $"TAG ACTION EXECUTED | Target: {target.name}"
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
            Debug.Log(
                "ACTION EXECUTION FAILED | " +
                "ActionData has no valid action state.",
                this
            );
            return false;
        }

        return true;
    }
}