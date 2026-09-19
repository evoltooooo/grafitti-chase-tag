using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

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
    }

    private void Update()
    {
        actionRuntime.SetSteeringInput(
            inputManager.MovementInput
        );

        characterMotor.Tick(
            inputManager.MovementInput,
            inputManager.SprintHeld
        );
    }

    private void LateUpdate()
    {
        if (cameraManager != null)
        {
            cameraManager.HandleAllCameraMovement();
        }
    }
}