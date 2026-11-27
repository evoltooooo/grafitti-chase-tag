using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootloaderController : MonoBehaviour
{
    [Header("Startup Scene")]
    [SerializeField] private string startupSceneName = "Main Menu";

    [Header("Initialization")]
    [SerializeField] private bool requireAudioManager = true;
    [SerializeField] private bool requireSettingsManager = true;
    [SerializeField] private bool requireInputManager = true;
    [SerializeField] private bool requireTransitionController = true;

    private IEnumerator Start()
    {
        // Allow all scene objects to finish Awake and Start initialization.
        yield return null;

        if (!ValidateStartupScene())
            yield break;

        if (!ValidateManagers())
            yield break;

        SceneTransitionController transition =
            InitializeTransitionController();

        if (requireTransitionController && transition == null)
            yield break;

        Debug.Log("BOOTLOADER | Initialization completed.");

        yield return LoadStartupScene(transition);
    }

    private bool ValidateStartupScene()
    {
        if (string.IsNullOrWhiteSpace(startupSceneName))
        {
            Debug.LogError(
                "BOOTLOADER | Startup scene name is empty.",
                this
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(startupSceneName))
        {
            Debug.LogError(
                $"BOOTLOADER | Cannot load '{startupSceneName}'. " +
                "Check the scene name and Build Profiles.",
                this
            );

            return false;
        }

        return true;
    }

    private bool ValidateManagers()
    {
        bool valid = true;

        if (requireAudioManager && AudioManager.Instance == null)
        {
            Debug.LogError(
                "BOOTLOADER | AudioManager was not initialized.",
                this
            );

            valid = false;
        }

        if (requireSettingsManager &&
            GameSettingsManager.Instance == null)
        {
            Debug.LogError(
                "BOOTLOADER | GameSettingsManager was not initialized.",
                this
            );

            valid = false;
        }

        if (requireInputManager &&
            (InputManager.Instance == null ||
             !InputManager.Instance.IsInitialized))
        {
            Debug.LogError(
                "BOOTLOADER | InputManager was not initialized.",
                this
            );

            valid = false;
        }

        return valid;
    }

    private SceneTransitionController InitializeTransitionController()
    {
        if (SceneTransitionController.Instance != null)
            return SceneTransitionController.Instance;

        SceneTransitionController prefab =
            Resources.Load<SceneTransitionController>(
                "SceneTransitionController"
            );

        if (prefab == null)
        {
            Debug.LogError(
                "BOOTLOADER | SceneTransitionController prefab " +
                "was not found in a Resources folder.",
                this
            );

            return null;
        }

        SceneTransitionController transition =
            Instantiate(prefab);

        transition.SetAlphaInstant(1f);

        Debug.Log(
            "BOOTLOADER | SceneTransitionController initialized."
        );

        return transition;
    }

    private IEnumerator LoadStartupScene(
        SceneTransitionController transition)
    {
        Debug.Log(
            $"BOOTLOADER | Loading '{startupSceneName}'..."
        );

        if (transition != null)
        {
            transition.SetAlphaInstant(1f);
            transition.FadeInAfterNextSceneLoad();
        }

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(startupSceneName);

        if (operation == null)
        {
            Debug.LogError(
                $"BOOTLOADER | Failed to load '{startupSceneName}'.",
                this
            );

            yield break;
        }

        while (!operation.isDone)
            yield return null;

        Debug.Log(
            $"BOOTLOADER | '{startupSceneName}' loaded successfully."
        );
    }
}
