using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
    [Header("Optional Scene Transition")]
    [SerializeField] private MainMenuTransitionController mainMenuTransition;
    [SerializeField] private GameEndTransitionController gameEndTransition;

    public static string PendingSceneName { get; private set; }
    private bool isSceneLoadRequested;
    private const string LoadingSceneName = "Loading";

    public void OpenMainMenu()
    {
        LoadScene("Main Menu");
    }

    public void StartGame()
    {
        LoadScene("Main Game");
    }

    public void OpenCredits()
    {
        LoadScene("Credits");
    }

    public void OpenGameEnd()
    {
        LoadScene("Game EndC");
    }

    public void PlayAgain()
    {
        LoadScene("Main Game");
    }

    public void ExitGame()
    {
        SceneTransitionController transition =
            SceneTransitionController.Instance;

        if (transition == null)
        {
            SceneTransitionController prefab =
                Resources.Load<SceneTransitionController>(
                    "SceneTransitionController"
                );

            if (prefab == null)
            {
                Debug.LogError(
                    "SCENE NAVIGATION | SceneTransitionController prefab missing.",
                    this
                );

                return;
            }

            transition = Instantiate(prefab);
            transition.SetAlphaInstant(0f);
        }

        if (transition.IsTransitioning)
            return;

        transition.FadeOut(QuitApplication);
    }

    public static void ClearPendingScene()
    {
        PendingSceneName = null;
    }
    
    private void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"SCENE NAVIGATION | Cannot load scene '{sceneName}'. " +
                "Check the scene name and Build Profiles scene list.",
                this
            );

            return;
        }

        if (sceneName == LoadingSceneName)
        {
            Debug.LogError(
                "SCENE NAVIGATION | Loading cannot be used as a destination.",
                this
            );

            return;
        }

        if (isSceneLoadRequested)
            return;

        SceneTransitionController transition =
            SceneTransitionController.Instance;

        if (transition != null && transition.IsTransitioning)
            return;

        isSceneLoadRequested = true;
        PendingSceneName = sceneName;

        // Play the existing scene-specific exit animation first.
        if (mainMenuTransition != null)
        {
            mainMenuTransition.PlayExitAnimation(BeginFadeOut);
        }
        else if (gameEndTransition != null)
        {
            gameEndTransition.PlayExitAnimation(BeginFadeOut);
        }
        else
        {
            BeginFadeOut();
        }
    }
    private void BeginFadeOut()
    {
        SceneTransitionController transition =
            SceneTransitionController.Instance;

        if (transition == null)
        {
            SceneTransitionController prefab =
                Resources.Load<SceneTransitionController>(
                    "SceneTransitionController"
                );

            if (prefab == null)
            {
                Debug.LogError(
                    "SCENE NAVIGATION | SceneTransitionController prefab missing.",
                    this
                );

                PendingSceneName = null;
                isSceneLoadRequested = false;
                return;
            }

            transition = Instantiate(prefab);
            transition.SetAlphaInstant(0f);
        }

        transition.FadeOut(() =>
        {
            SceneManager.LoadScene(LoadingSceneName);
        });
    }


    private void QuitApplication()
    {
#if UNITY_EDITOR
        Debug.Log("EXIT GAME | Exit is available in a built game.");
#else
        Application.Quit();
#endif
    }
}
