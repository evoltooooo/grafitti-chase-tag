
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
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text roundResultText;

    [Header("Character Roles")]
    [SerializeField] private CharacterRole playerRole;
    [SerializeField] private CharacterRole aiRole;
    [SerializeField] private TMP_Text playerRoleText;
    [SerializeField] private TMP_Text aiRoleText;
    
    private void OnEnable()
    {
        if (roundManager == null)
        {
            Debug.LogError(
                "ROUND UI | RoundManager is missing.",
                this
            );
            return;
        }

        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);

        roundManager.RoundStarted += OnRoundStarted;
        roundManager.TimerUpdated += OnTimerUpdated;
        roundManager.CountdownUpdated += OnCountdownUpdated;
        roundManager.RoundEnded += OnRoundEnded;

        UpdateTimer(roundManager.TimeRemaining);
        UpdateScores();

        if (taggedText != null)
            taggedText.gameObject.SetActive(false);

        // Restore the correct countdown display if this UI is enabled
        // while a countdown is already running.
        if (countdownText != null)
        {
            bool isCountingDown =
                roundManager.CurrentState ==
                RoundManager.RoundState.Countdown;

            countdownText.gameObject.SetActive(isCountingDown);

            if (isCountingDown)
            {
                OnCountdownUpdated(
                    roundManager.CountdownRemaining
                );
            }
        }

        if (playerRole != null)
            playerRole.RoleChanged += OnPlayerRoleChanged;

        if (aiRole != null)
            aiRole.RoleChanged += OnAIRoleChanged;

        UpdateRoleDisplays();
    }

    private void OnDisable()
    {
        if (roundManager == null)
            return;

        roundManager.RoundStarted -= OnRoundStarted;
        roundManager.TimerUpdated -= OnTimerUpdated;
        roundManager.CountdownUpdated -= OnCountdownUpdated;
        roundManager.RoundEnded -= OnRoundEnded;

        if (playerRole != null)
            playerRole.RoleChanged -= OnPlayerRoleChanged;

        if (aiRole != null)
            aiRole.RoleChanged -= OnAIRoleChanged;
    }

    private void OnRoundStarted(int roundNumber, float duration)
    {
        UpdateTimer(duration);
        UpdateScores();

        if (taggedText != null)
            taggedText.gameObject.SetActive(false);
        
        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);
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

        bool playerWon =
            playerRole != null &&
            playerRole.CurrentRole == winningRole;

        bool aiWon =
            aiRole != null &&
            aiRole.CurrentRole == winningRole;

        if (roundResultText != null)
        {
            if (playerWon)
                roundResultText.text = "PLAYER WINS!";
            else if (aiWon)
                roundResultText.text = "AI WINS!";
            else
                roundResultText.text = $"{winningRole} WINS!";

            roundResultText.gameObject.SetActive(true);
        }

        // Preserve the existing successful-tag popup.
        if (winningRole == CharacterRoleType.Chaser)
        {
            if (taggedText != null)
                taggedText.gameObject.SetActive(true);
        }
    }

    private void OnCountdownUpdated(float timeRemaining)
    {
        // Hide previous round feedback as soon as countdown begins.
        if (taggedText != null)
            taggedText.gameObject.SetActive(false);

        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);

        if (countdownText == null)
            return;

        bool isCountingDown =
            roundManager != null &&
            roundManager.CurrentState ==
                RoundManager.RoundState.Countdown &&
            timeRemaining > 0f;

        countdownText.gameObject.SetActive(isCountingDown);

        if (isCountingDown)
        {
            countdownText.text =
                Mathf.CeilToInt(timeRemaining).ToString();
        }
    }

    private void UpdateRoleDisplays()
    {
        if (playerRole != null)
            OnPlayerRoleChanged(playerRole.CurrentRole);

        if (aiRole != null)
            OnAIRoleChanged(aiRole.CurrentRole);
    }

    private void OnPlayerRoleChanged(CharacterRoleType role)
    {
        if (playerRoleText != null)
            playerRoleText.text = $"{role}";
    }

    private void OnAIRoleChanged(CharacterRoleType role)
    {
        if (aiRoleText != null)
            aiRoleText.text = $"{role}";
    }
}
