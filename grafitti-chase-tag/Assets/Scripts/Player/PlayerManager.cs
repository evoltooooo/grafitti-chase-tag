using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterActionController actionController;

    private void Awake()
    {
        if (inputManager == null)
            inputManager = InputManager.Instance;

        if (inputManager == null)
        {
            Debug.LogError(
                "PLAYER MANAGER | InputManager instance not found.",
                this
            );

            enabled = false;
            return;
        }

        if (characterMotor == null)
            characterMotor =
                GetComponent<CharacterMotor>();
        
        if (actionRuntime == null)
            actionRuntime =
                GetComponent<CharacterActionRuntime>();

        if (actionController == null)
            actionController = 
                GetComponent<CharacterActionController>();
    }

    private void Update()
    {
        if (GameplayInputLock.IsLocked)
        {
            characterMotor.SetMovementInput(Vector2.zero);
            actionController.SetMovementIntent(default);
            return;
        }

        characterMotor.SetMovementInput(
            inputManager.MovementInput
        );

        Vector3 worldDirection =
            characterMotor.GetWorldDirection(
                inputManager.MovementInput
            );

        CharacterMovementIntent intent =
            new CharacterMovementIntent(
                worldDirection,
                inputManager.SprintHeld,
                inputManager.MovementInput,
                true
            );

        actionController.SetMovementIntent(intent);
        actionController.TickMovement();
    }
}