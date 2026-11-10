using System;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundManager roundManager;

    private InputManager inputManager;

    public bool IsPaused { get; private set; }

    private bool pauseInputLocked;

    public event Action GamePaused;
    public event Action GameResumed;

    public void SetInputManager(InputManager manager)
    {
        if (inputManager != null)
            inputManager.PausePressed -= TogglePause;

        inputManager = manager;

        if (inputManager != null)
            inputManager.PausePressed += TogglePause;
    }

    private void TogglePause()
    {
        if (pauseInputLocked)
            return;

        // Pause is only available during an active round.
        if (roundManager == null || !roundManager.IsRoundActive)
            return;

        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        pauseInputLocked = true;

        GameplayInputLock.SetLocked(true);
        Time.timeScale = 0f;

        GamePaused?.Invoke();

        Debug.Log("GAME PAUSED");
    }

    public void ResumeGame()
    {
        if (!IsPaused)
            return;

        pauseInputLocked = true;

        Time.timeScale = 1f;
        GameplayInputLock.SetLocked(false);

        IsPaused = false;

        GameResumed?.Invoke();

        Debug.Log("GAME RESUMED");
    }

    public void SetPauseInputLocked(bool locked)
    {
        pauseInputLocked = locked;
    }

    private void OnDisable()
    {
        if (inputManager != null)
            inputManager.PausePressed -= TogglePause;

        if (IsPaused)
        {
            Time.timeScale = 1f;
            GameplayInputLock.SetLocked(false);
            IsPaused = false;
        }

        pauseInputLocked = false;
    }
}
