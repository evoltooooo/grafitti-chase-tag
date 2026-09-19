using UnityEngine;

public class CharacterActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterActionResolver actionResolver;
    [SerializeField] private ParkourActionResolver parkourActionResolver;
    [SerializeField] private CharacterActionExecutor actionExecutor;
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Interaction")]
    [SerializeField] private CharacterInteractionResolver interactionResolver;

    private void Awake()
    {

        if (actionResolver == null)
        {
            actionResolver =
                GetComponent<CharacterActionResolver>();
        }

        if (parkourActionResolver == null)
        {
            parkourActionResolver =
                GetComponent<ParkourActionResolver>();
        }

        if (actionExecutor == null)
        {
            actionExecutor =
                GetComponent<CharacterActionExecutor>();
        }

        if (characterMotor == null)
        {
            characterMotor =
                GetComponent<CharacterMotor>();
        }

        if (characterMotor == null)
        {
            Debug.LogError(
                "CharacterActionController: CharacterMotor is missing.",
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
                "CharacterActionController: CharacterActionRuntime is missing.",
                this
            );
        }
        

        if (interactionResolver == null)
        {
            interactionResolver = GetComponent<CharacterInteractionResolver>();   
        }
    }

    public bool RequestAction(
        CharacterActionRequest request)
    {
        if (request.ActionType == CharacterActionType.None)
        {
            Debug.LogWarning(
                "ACTION REQUEST FAILED | ActionType is None.",
                this
            );
            return false;
        }

        if (actionResolver == null ||
            actionExecutor == null)
        {
            return false;
        }

        if (!actionResolver.TryResolve(
                request,
                out CharacterActionData actionData))
        {
            return false;
        }

        return actionExecutor.Execute(
            actionData
        );
    }

    public bool RequestParkourAction(
        ParkourActionType parkourActionType)
    {
        if (actionResolver == null ||
            parkourActionResolver == null ||
            actionExecutor == null)
            return false;

        if (!actionResolver.TryResolveParkourAction(
                parkourActionType,
                out ParkourActionData parkourActionData))
        {
            return false;
        }

        if (!parkourActionResolver.TryResolve(
                parkourActionData,
                out ParkourTarget target))
        {
            return false;
        }

        return actionExecutor.ExecuteParkour(
            parkourActionData,
            target
        );
    }

    public void SetInteractionHeld(bool held)
    {
        if (!held &&
            actionRuntime.IsExecuting &&
            actionRuntime.IsHoldControlled)
        {
            actionRuntime.CompleteAction();
        }
    }

    public void RequestInteraction()
    {
        if (interactionResolver == null)
        {
            Debug.LogError(
                "CharacterActionController: InteractionResolver is missing.",
                this
            );

            return;
        }

        interactionResolver.TryResolveInteraction();
    }

    public bool RequestJumpInput()
    {
        Debug.Log("Jump input received.");

        // =====================================================
        // AIRBORNE → TRY TIC TAC
        // =====================================================

        if (!characterMotor.IsGrounded)
        {
            Debug.Log("JUMP INPUT → AIRBORNE → TRY TIC TAC");

            if (RequestParkourAction(
                    ParkourActionType.TicTac))
            {
                Debug.Log("JUMP INPUT → TIC TAC");
                return true;
            }

            Debug.Log("JUMP INPUT → NO TIC TAC");

            return false;
        }

        // =====================================================
        // GROUNDED → NORMAL JUMP
        // =====================================================

        bool jumpPerformed =
            RequestAction(
                new CharacterActionRequest(
                    CharacterActionType.Jump
                )
            );

        if (jumpPerformed)
        {
            Debug.Log("JUMP INPUT → NORMAL JUMP");
        }

        return jumpPerformed;
    }

    public bool RequestTagInput()
    {
        Debug.Log("Tag input received.");

        bool tagPerformed =
            RequestAction(
                new CharacterActionRequest(
                    CharacterActionType.Tag
                )
            );

        if (tagPerformed)
        {
            Debug.Log("TAG INPUT → TAG");
        }

        return tagPerformed;
    }
}