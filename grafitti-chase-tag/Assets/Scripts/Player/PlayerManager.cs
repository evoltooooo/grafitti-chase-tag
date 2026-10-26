using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterActionController actionController;

    private void Awake()
    {
        if (inputManager == null)
            inputManager =
                GetComponent<InputManager>();

        if (characterMotor == null)
            characterMotor =
                GetComponent<CharacterMotor>();

        if (cameraManager == null)
            cameraManager =
                FindAnyObjectByType<CameraManager>();
        
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
            actionRuntime.SetSteeringInput(Vector2.zero);
            characterMotor.SetMovementInput(Vector2.zero);
            actionController.SetMovementIntent(default);
            return;
        }
        
        actionRuntime.SetSteeringInput(
            inputManager.MovementInput
        );

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