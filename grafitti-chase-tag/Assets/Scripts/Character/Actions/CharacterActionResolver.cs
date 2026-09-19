using UnityEngine;

public class CharacterActionResolver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterActionLibrary actionLibrary;
    [SerializeField] private CharacterStateController stateController;
    [SerializeField] private CharacterStamina stamina;

    private void Awake()
    {
        if (actionLibrary == null)
        {
            actionLibrary =
                GetComponent<CharacterActionLibrary>();
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
    }

    public bool TryResolve(
        CharacterActionRequest request,
        out CharacterActionData actionData)
    {
        actionData = null;

        if (actionLibrary == null)
            return false;

        if (!actionLibrary.TryGetActionData(
                request.ActionType,
                out actionData))
        {
            return false;
        }

        if (!CanUseAction(actionData))
        {
            actionData = null;
            return false;
        }

        return true;
    }

    public bool TryResolveParkourAction(
        ParkourActionType parkourActionType,
        out ParkourActionData actionData)
    {
        actionData = null;

        if (actionLibrary == null)
            return false;

        CharacterActionType actionType =
            parkourActionType.ToActionType();

        if (actionType == CharacterActionType.None)
            return false;

        if (!actionLibrary.TryGetActionData(
                actionType,
                out CharacterActionData baseActionData))
        {
            return false;
        }

        actionData =
            baseActionData as ParkourActionData;

        if (actionData == null)
            return false;

        return true;
    }

    private bool CanUseAction(
        CharacterActionData actionData)
    {
        if (actionData == null)
            return false;

        if (stateController != null &&
            stateController.IsBusy)
        {
            return false;
        }

        if (stamina != null &&
            !stamina.CanConsume(
                actionData.staminaCost))
        {
            return false;
        }

        return true;
    }
}