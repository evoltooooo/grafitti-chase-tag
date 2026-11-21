using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField, Min(0f)] private float minimumLoadingDuration = 0.5f;

    private void Awake()
    {
        // The loading coordinator must survive when the Loading Scene
        // is replaced by the destination scene.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(LoadDestinationScene());
    }

    private IEnumerator LoadDestinationScene()
    {
        string destinationScene = SceneNavigation.PendingSceneName;

        SceneNavigation.ClearPendingScene();

        if (string.IsNullOrEmpty(destinationScene))
        {
            Debug.LogError(
                "LOADING SCENE | No destination scene was specified."
            );

            Destroy(gameObject);
            yield break;
        }

        SceneTransitionController transition =
            SceneTransitionController.Instance;

        if (transition != null)
            transition.SetAlphaInstant(0f);

        float loadingStartTime = Time.realtimeSinceStartup;

        Debug.Log(
            $"LOADING SCENE | Loading destination: {destinationScene}"
        );

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(destinationScene);

        if (operation == null)
        {
            Debug.LogError(
                $"LOADING SCENE | Failed to start loading '{destinationScene}'."
            );

            Destroy(gameObject);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
            yield return null;

        while (Time.realtimeSinceStartup - loadingStartTime <
            minimumLoadingDuration)
        {
            yield return null;
        }

        // Cover the Loading Scene before activating the destination.
        if (transition != null)
            transition.SetAlphaInstant(1f);

        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        Debug.Log(
            $"LOADING SCENE | Destination activated: {destinationScene}"
        );

        // Game End handles its own fade-in after its video is ready.
        if (destinationScene == "Game End")
        {
            Destroy(gameObject);
            yield break;
        }

        // Allow the destination scene to initialize behind the overlay.
        yield return null;

        transition = SceneTransitionController.Instance;

        if (transition != null)
        {
            // Explicitly guarantee that the fade starts from black.
            transition.SetAlphaInstant(1f);

            transition.FadeIn(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            Debug.LogWarning(
                "LOADING SCENE | SceneTransitionController not found. " +
                "Cannot play destination fade-in."
            );

            Destroy(gameObject);
        }
    }
}
