using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootloaderController : MonoBehaviour
{
    [Header("Startup Scene")]
    [SerializeField] private string startupSceneName = "Main Menu";

    private IEnumerator Start()
    {
        if (string.IsNullOrWhiteSpace(startupSceneName))
        {
            Debug.LogError(
                "Bootloader: Startup scene name is empty."
            );
            yield break;
        }

        Debug.Log(
            $"Bootloader: Loading scene '{startupSceneName}'..."
        );

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(startupSceneName);

        if (operation == null)
        {
            Debug.LogError(
                $"Bootloader: Failed to load scene '{startupSceneName}'. " +
                "Make sure it is included in the Build Profile."
            );
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }
    }
}
