using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterParkourInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourInputSettings inputSettings;
    [SerializeField] private CharacterActionController actionController;


    private void Awake()
    {
        Debug.Log("CharacterParkourInput AWAKE", this);

        if (actionController == null)
            actionController = GetComponent<CharacterActionController>();
    }

    private void OnEnable()
    {
        Debug.Log("CharacterParkourInput ENABLED", this);

        if (inputSettings == null)
        {
            Debug.LogError(
                "CharacterParkourInput: ParkourInputSettings is NULL!",
                this
            );
            return;
        }

        // Enable inputs
        if (inputSettings.interactionInput != null)
        {
            inputSettings.interactionInput.action.Enable();
            inputSettings.interactionInput.action.started += OnInteraction;
            inputSettings.interactionInput.action.canceled += OnInteraction;
        }

        if (inputSettings.jumpInput != null)
        {
            inputSettings.jumpInput.action.Enable();
            inputSettings.jumpInput.action.performed += OnJump;
        }

        if (inputSettings.tagInput != null)
        {
            inputSettings.tagInput.action.Enable();
            inputSettings.tagInput.action.performed += OnTag;
        }
    }

    private void OnDisable()
    {
        Debug.Log("CharacterParkourInput DISABLED", this);

        if (inputSettings == null)
            return;

        if (inputSettings.interactionInput != null)
        {
            inputSettings.interactionInput.action.started -= OnInteraction;
            inputSettings.interactionInput.action.canceled -= OnInteraction;
            inputSettings.interactionInput.action.Disable();
        }

        if (inputSettings.jumpInput != null)
        {
            inputSettings.jumpInput.action.performed -= OnJump;
            inputSettings.jumpInput.action.Disable();
        }

        if (inputSettings.tagInput != null)
        {
            inputSettings.tagInput.action.performed -= OnTag;
            inputSettings.tagInput.action.Disable();
        }
    }

    private void OnInteraction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (actionController != null)
            {
                actionController.SetInteractionHeld(true);
                actionController.RequestInteraction();
            }
        }
        else if (context.canceled)
        {
            if (actionController != null)
            {
                actionController.SetInteractionHeld(false);
            }
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (actionController != null)
        {
            actionController.RequestJumpInput();
        }
    }

    private void OnTag(InputAction.CallbackContext context)
    {
        if (actionController != null)
        {
            actionController.RequestTagInput();
        }
    }
}