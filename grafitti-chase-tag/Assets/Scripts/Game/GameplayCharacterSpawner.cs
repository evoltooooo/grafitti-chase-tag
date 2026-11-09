using Unity.Cinemachine;
using UnityEngine;

public class GameplayCharacterSpawner : MonoBehaviour
{
    [Header("Gameplay Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject aiPrefab;

    [Header("Spawn Parent")]
    [SerializeField] private Transform characterParent;

    [Header("Manager References")]
    [SerializeField] private RoundManager roundManager;

    [Header("Camera References")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private CameraOcclusionFade cameraOcclusionFade;

    [Header("UI References")]
    [SerializeField] private StaminaBarUI playerStaminaDisplay;
    [SerializeField] private CharacterBuffNerfDisplay playerBuffNerfDisplay;
    [SerializeField] private RoundUIManager roundUIManager;

    private GameObject spawnedPlayer;
    private GameObject spawnedAI;

    private void Start()
    {
        SpawnCharacters();
    }

    private void SpawnCharacters()
    {
        if (playerPrefab == null ||
            aiPrefab == null ||
            characterParent == null ||
            roundManager == null)
        {
            Debug.LogError(
                "GAMEPLAY SPAWNER | Missing required references.",
                this
            );

            enabled = false;
            return;
        }

        spawnedPlayer = Instantiate(
            playerPrefab,
            characterParent
        );

        spawnedAI = Instantiate(
            aiPrefab,
            characterParent
        );

        if (cameraOcclusionFade != null)
        {
            Renderer[] playerRenderers =
                spawnedPlayer.GetComponentsInChildren<Renderer>(true);

            cameraOcclusionFade.SetPlayerReferences(
                spawnedPlayer.transform,
                spawnedPlayer.transform,
                playerRenderers
            );
        }

        CharacterRole playerRole =
            spawnedPlayer.GetComponent<CharacterRole>();

        CharacterRole aiRole =
            spawnedAI.GetComponent<CharacterRole>();

        CharacterActionExecutor playerExecutor =
            spawnedPlayer.GetComponent<CharacterActionExecutor>();

        CharacterActionExecutor aiExecutor =
            spawnedAI.GetComponent<CharacterActionExecutor>();

        CharacterActionController playerController =
            spawnedPlayer.GetComponent<CharacterActionController>();

        CharacterActionController aiActionController =
            spawnedAI.GetComponent<CharacterActionController>();

        AIController aiController =
            spawnedAI.GetComponent<AIController>();

        CharacterMotor playerMotor =
            spawnedPlayer.GetComponent<CharacterMotor>();
        
        CharacterStamina playerStamina =
            spawnedPlayer.GetComponent<CharacterStamina>();

        CharacterBuffNerf playerBuffNerf =
            spawnedPlayer.GetComponent<CharacterBuffNerf>();

        if (playerRole == null ||
            aiRole == null ||
            playerExecutor == null ||
            aiExecutor == null ||
            playerController == null ||
            aiActionController == null ||
            aiController == null ||
            playerMotor == null ||
            playerStamina == null ||
            playerBuffNerf == null ||
            cameraOcclusionFade == null ||
            gameplayCamera == null)
        {
            Debug.LogError(
                "GAMEPLAY SPAWNER | Required character components are missing.",
                this
            );

            Destroy(spawnedPlayer);
            Destroy(spawnedAI);

            enabled = false;
            return;
        }

        playerController.SetRoundManager(roundManager);
        aiActionController.SetRoundManager(roundManager);

        aiController.SetOpponent(
            spawnedPlayer.transform,
            playerMotor
        );

        // Assign the spawned Player to the gameplay camera.
        if (gameplayCamera != null)
        {
            CameraTarget cameraTarget = gameplayCamera.Target;

            cameraTarget.TrackingTarget = spawnedPlayer.transform;
            cameraTarget.CustomLookAtTarget = false;

            gameplayCamera.Target = cameraTarget;
        }
        else
        {
            Debug.LogError(
                "GAMEPLAY SPAWNER | Gameplay Cinemachine Camera is missing.",
                this
            );
        }

        // Assign Player stamina UI.
        if (playerStaminaDisplay != null)
        {
            playerStaminaDisplay.SetCharacterStamina(
                playerStamina
            );
        }

        // Assign Player buff/nerf UI.
        if (playerBuffNerfDisplay != null)
        {
            playerBuffNerfDisplay.SetCharacterBuffNerf(
                playerBuffNerf
            );
        }

        bool initialized =
            roundManager.InitializeCharacters(
                playerRole,
                aiRole,
                playerExecutor,
                aiExecutor,
                playerController,
                aiActionController,
                aiController
            );

        if (!initialized)
        {
            Debug.LogError(
                "GAMEPLAY SPAWNER | RoundManager initialization failed.",
                this
            );

            Destroy(spawnedPlayer);
            Destroy(spawnedAI);

            enabled = false;
            return;
        }

        // Assign dynamically spawned character roles to Round UI.
        if (roundUIManager != null)
        {
            roundUIManager.SetCharacterRoles(
                playerRole,
                aiRole
            );
        }

        Debug.Log(
            "GAMEPLAY SPAWNER | Player and AI spawned successfully.",
            this
        );
    }
}
