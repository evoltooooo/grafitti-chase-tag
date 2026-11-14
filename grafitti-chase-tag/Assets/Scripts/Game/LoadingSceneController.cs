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
        {
            // Reveal the Loading Scene and its loading text.
            transition.SetAlphaInstant(0f);
        }

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
        {
            transition.SetAlphaInstant(1f);
        }

        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        Debug.Log(
            $"LOADING SCENE | Destination activated: {destinationScene}"
        );

        // The coroutine survives scene activation because this
        // LoadingSceneController is persistent.
        if (transition != null)
        {
            transition.FadeIn(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
