
using System.Collections;
using TMPro;
using UnityEngine;

public class RoundEndUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private GameObject roundEndRoot;
    [SerializeField] private CanvasGroup roundEndCanvasGroup;
    [SerializeField] private GameObject gameplayUIRoot;

    [Header("Result UI")]
    [SerializeField] private TMP_Text roundEndTitle;
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text aiScoreText;

    [Header("Immediate Round Feedback")]
    [SerializeField] private GameObject taggedDisplay;

    [Header("Timing")]
    [SerializeField, Min(0f)]
    private float immediateMessageDuration = 1.5f;

    [SerializeField, Min(0f)]
    private float roleShuffleLeadTime = 1.5f;

    [SerializeField, Min(0f)]
    private float fadeInDuration = 0.3f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.3f;

    private Coroutine displayRoutine;

    private void Awake()
    {
        if (roundEndRoot != null)
            roundEndRoot.SetActive(false);
    }

    private void SetGameplayUIVisible(bool visible)
    {
        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(visible);
    }

    private void OnEnable()
    {
        if (roundManager == null)
        {
            Debug.LogError(
                "ROUND END UI | RoundManager is missing.", this);
            return;
        }

        roundManager.RoundEnded += OnRoundEnded;
    }

    private void OnDisable()
    {
        if (roundManager != null)
            roundManager.RoundEnded -= OnRoundEnded;

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }
    }

    private void OnRoundEnded(
        int roundNumber,
        CharacterRoleType winningRole,
        int playerScore,
        int aiScore)
    {
        if (roundManager == null || roundEndRoot == null)
            return;

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        if (roundEndTitle != null)
            roundEndTitle.text = $"ROUND {roundNumber} ENDED";

        if (elapsedTimeText != null)
        {
            int seconds = Mathf.FloorToInt(
                roundManager.LastRoundElapsedTime);

            elapsedTimeText.text =
                $"{seconds / 60:00}:{seconds % 60:00}";
        }

        if (playerScoreText != null)
            playerScoreText.text = playerScore.ToString();

        if (aiScoreText != null)
            aiScoreText.text = aiScore.ToString();

        roundEndRoot.SetActive(false);

        displayRoutine = StartCoroutine(DisplayRoundEnd());
    }

    private IEnumerator DisplayRoundEnd()
    {
        float intermission = roundManager.RoundResultDuration;

        // Keep TAGGED!/ESCAPED! visible first.
        float messageTime = Mathf.Min(
            immediateMessageDuration, intermission);

        yield return new WaitForSeconds(messageTime);

        if (taggedDisplay != null)
            taggedDisplay.SetActive(false);

        // Reserve time for the next role shuffle.
        float availableTime =
            intermission - messageTime - roleShuffleLeadTime;

        float fadeIn = Mathf.Min(fadeInDuration, availableTime);
        float fadeOut = Mathf.Min(
            fadeOutDuration,
            Mathf.Max(0f, availableTime - fadeIn));

        float holdTime = Mathf.Max(
            0f, availableTime - fadeIn - fadeOut);

        if (availableTime > 0f)
        {
            // First hide the immediate message's parent HUD.
            SetGameplayUIVisible(false);

            // Then reveal the round-end screen.
            roundEndRoot.SetActive(true);

            if (roundEndCanvasGroup != null)
                roundEndCanvasGroup.alpha = 0f;

            // Fade in.
            yield return FadeCanvasGroup(0f, 1f, fadeIn);

            // Hold the result screen.
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            // Fade out.
            yield return FadeCanvasGroup(1f, 0f, fadeOut);
        }

        roundEndRoot.SetActive(false);
        displayRoutine = null;
    }

    private IEnumerator FadeCanvasGroup(
        float from, float to, float duration)
    {
        if (roundEndCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            roundEndCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            roundEndCanvasGroup.alpha = Mathf.Lerp(
                from, to, Mathf.Clamp01(elapsed / duration));

            yield return null;
        }

        roundEndCanvasGroup.alpha = to;
    }
}
