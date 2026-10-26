
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
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
        LoadScene("Game End");
    }

    public void PlayAgain()
    {
        LoadScene("Main Game");
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        Debug.Log("EXIT GAME | Exit is available in a built game.");
#else
        Application.Quit();
#endif
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

        SceneManager.LoadScene(sceneName);
    }
}
