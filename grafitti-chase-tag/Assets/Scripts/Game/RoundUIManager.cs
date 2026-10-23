
using TMPro;
using UnityEngine;

public class RoundUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text aiScoreText;
    [SerializeField] private TMP_Text taggedText;

    private void OnEnable()
    {
        if (roundManager == null)
        {
            Debug.LogError("ROUND UI | RoundManager is missing.", this);
            return;
        }

        roundManager.RoundStarted += OnRoundStarted;
        roundManager.TimerUpdated += OnTimerUpdated;
        roundManager.RoundEnded += OnRoundEnded;

        // Display the current values when this UI becomes enabled.
        UpdateTimer(roundManager.TimeRemaining);
        UpdateScores();

        if (taggedText != null)
            taggedText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (roundManager == null)
            return;

        roundManager.RoundStarted -= OnRoundStarted;
        roundManager.TimerUpdated -= OnTimerUpdated;
        roundManager.RoundEnded -= OnRoundEnded;
    }

    private void OnRoundStarted(int roundNumber, float duration)
    {
        UpdateTimer(duration);
        UpdateScores();

        if (taggedText != null)
            taggedText.gameObject.SetActive(false);
    }

    private void OnTimerUpdated(float timeRemaining)
    {
        UpdateTimer(timeRemaining);
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(
            Mathf.Max(0f, timeRemaining));

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateScores()
    {
        if (roundManager == null)
            return;

        if (playerScoreText != null)
            playerScoreText.text = roundManager.PlayerScore.ToString();

        if (aiScoreText != null)
            aiScoreText.text = roundManager.AIScore.ToString();
    }

    private void OnRoundEnded(
        int roundNumber,
        CharacterRoleType winningRole,
        int playerScore,
        int aiScore)
    {
        UpdateScores();

        // A Chaser winning means a successful tag ended the round.
        // An Evader winning means the timer expired.
        if (winningRole == CharacterRoleType.Chaser)
        {
            if (taggedText != null)
                taggedText.gameObject.SetActive(true);
        }
    }
}
