using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class RoundUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text aiScoreText;
    [SerializeField] private GameObject taggedDisplayRoot;
    [SerializeField] private TMP_Text taggedText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text roundResultText;

    [Header("Character Role Images")]
    [SerializeField] private CharacterRole playerRole;
    [SerializeField] private CharacterRole aiRole;

    [SerializeField] private Image playerRoleImage;
    [SerializeField] private Image aiRoleImage;

    [Header("Role Sprites")]
    [SerializeField] private Sprite chaserSprite;
    [SerializeField] private Sprite evaderSprite;
    
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

        if (taggedDisplayRoot != null)
            taggedDisplayRoot.SetActive(false);

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

        // Make sure the parent and message are both enabled.
        if (taggedDisplayRoot != null)
            taggedDisplayRoot.SetActive(true);

        if (taggedText != null)
        {
            taggedText.text =
                winningRole == CharacterRoleType.Chaser
                    ? "TAGGED!"
                    : "ESCAPED!";

            taggedText.gameObject.SetActive(true);
        }

        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);
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

    private Sprite GetRoleSprite(CharacterRoleType role)
    {
        return role == CharacterRoleType.Chaser
            ? chaserSprite
            : evaderSprite;
    }

    private void OnPlayerRoleChanged(CharacterRoleType role)
    {
        if (playerRoleImage != null)
            playerRoleImage.sprite = GetRoleSprite(role);
    }

    private void OnAIRoleChanged(CharacterRoleType role)
    {
        if (aiRoleImage != null)
            aiRoleImage.sprite = GetRoleSprite(role);
    }
}
