using TMPro;
using UnityEngine;
using DG.Tweening;

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

    [Header("Animation Easing")]
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;

    private Sequence displaySequence;

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
                "ROUND END UI | RoundManager is missing.",
                this
            );

            return;
        }

        roundManager.RoundEnded += OnRoundEnded;
    }

    private void OnDisable()
    {
        if (roundManager != null)
            roundManager.RoundEnded -= OnRoundEnded;

        KillDisplaySequence();
    }

    private void OnRoundEnded(
        int roundNumber,
        CharacterRoleType winningRole,
        int playerScore,
        int aiScore)
    {
        if (roundManager == null || roundEndRoot == null)
            return;

        KillDisplaySequence();

        if (roundEndTitle != null)
            roundEndTitle.text = $"ROUND {roundNumber} ENDED";

        if (elapsedTimeText != null)
        {
            int seconds = Mathf.FloorToInt(
                roundManager.LastRoundElapsedTime
            );

            elapsedTimeText.text =
                $"{seconds / 60:00}:{seconds % 60:00}";
        }

        if (playerScoreText != null)
            playerScoreText.text = playerScore.ToString();

        if (aiScoreText != null)
            aiScoreText.text = aiScore.ToString();

        roundEndRoot.SetActive(false);

        PlayRoundEndSequence();
    }

    private void PlayRoundEndSequence()
    {
        float intermission = roundManager.RoundResultDuration;

        // Keep TAGGED!/ESCAPED! visible first.
        float messageTime = Mathf.Min(
            immediateMessageDuration,
            intermission
        );

        // Reserve time for the next role shuffle.
        float availableTime = Mathf.Max(
            0f,
            intermission - messageTime - roleShuffleLeadTime
        );

        float fadeIn = Mathf.Min(
            fadeInDuration,
            availableTime
        );

        float fadeOut = Mathf.Min(
            fadeOutDuration,
            Mathf.Max(0f, availableTime - fadeIn)
        );

        float holdTime = Mathf.Max(
            0f,
            availableTime - fadeIn - fadeOut
        );

        displaySequence = DOTween.Sequence();

        // Keep immediate round feedback visible.
        displaySequence.AppendInterval(messageTime);

        displaySequence.AppendCallback(() =>
        {
            if (taggedDisplay != null)
                taggedDisplay.SetActive(false);
        });

        if (availableTime > 0f)
        {
            displaySequence.AppendCallback(() =>
            {
                // Hide gameplay HUD first.
                SetGameplayUIVisible(false);

                // Reveal the round-end panel.
                roundEndRoot.SetActive(true);

                if (roundEndCanvasGroup != null)
                    roundEndCanvasGroup.alpha = 0f;
            });

            // Fade in.
            if (roundEndCanvasGroup != null)
            {
                if (fadeIn > 0f)
                {
                    displaySequence.Append(
                        roundEndCanvasGroup.DOFade(1f, fadeIn)
                            .SetEase(fadeInEase)
                    );
                }
                else
                {
                    displaySequence.AppendCallback(() =>
                    {
                        roundEndCanvasGroup.alpha = 1f;
                    });
                }
            }

            // Hold the result screen.
            if (holdTime > 0f)
                displaySequence.AppendInterval(holdTime);

            // Fade out.
            if (roundEndCanvasGroup != null)
            {
                if (fadeOut > 0f)
                {
                    displaySequence.Append(
                        roundEndCanvasGroup.DOFade(0f, fadeOut)
                            .SetEase(fadeOutEase)
                    );
                }
                else
                {
                    displaySequence.AppendCallback(() =>
                    {
                        roundEndCanvasGroup.alpha = 0f;
                    });
                }
            }
        }

        // Hide the result panel after the sequence.
        displaySequence.OnComplete(() =>
        {
            roundEndRoot.SetActive(false);
            displaySequence = null;
        });
    }

    private void KillDisplaySequence()
    {
        if (displaySequence != null && displaySequence.IsActive())
            displaySequence.Kill();

        displaySequence = null;
    }
}
