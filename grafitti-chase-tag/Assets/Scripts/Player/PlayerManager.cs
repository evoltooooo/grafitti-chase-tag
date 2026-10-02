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
        actionRuntime.SetSteeringInput(inputManager.MovementInput);

        Vector3 worldDirection =
            characterMotor.GetWorldDirection(inputManager.MovementInput);

        CharacterMovementIntent intent =
            new CharacterMovementIntent(
                worldDirection,
                inputManager.SprintHeld
            );

        actionController.SetMovementIntent(intent);
        actionController.TickMovement();
    }

    private void LateUpdate()
    {
        if (cameraManager != null)
        {
            cameraManager.HandleAllCameraMovement();
        }
    }
}