using UnityEngine;
using DG.Tweening;
using System;

public class MainMenuTransitionController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup titleGroup;

    [SerializeField] private CanvasGroup playGroup;
    [SerializeField] private CanvasGroup creditsGroup;
    [SerializeField] private CanvasGroup exitGroup;

    [SerializeField] private CanvasGroup highScoreGroup;

    [Header("Transition Overlay")]
    [SerializeField] private CanvasGroup transitionOverlay;
    [SerializeField] private float overlayFadeDuration = 0.6f;

    [Header("UI Transforms")]
    [SerializeField] private RectTransform titleTransform;
    [SerializeField] private RectTransform playTransform;
    [SerializeField] private RectTransform creditsTransform;
    [SerializeField] private RectTransform exitTransform;
    [SerializeField] private RectTransform highScoreTransform;

    [Header("Entry Start Offsets")]
    [SerializeField] private Vector2 titleStartOffset = new Vector2(0f, -40f);
    [SerializeField] private Vector2 playStartOffset = new Vector2(-50f, 0f);
    [SerializeField] private Vector2 creditsStartOffset = new Vector2(-50f, 0f);
    [SerializeField] private Vector2 exitStartOffset = new Vector2(-50f, 0f);
    [SerializeField] private Vector2 highScoreStartOffset = new Vector2(60f, 0f);

    [Header("Animation Timing")]
    [SerializeField] private float titleDuration = 0.5f;
    [SerializeField] private float playDuration = 0.4f;
    [SerializeField] private float creditsDuration = 0.4f;
    [SerializeField] private float exitButtonDuration = 0.4f;
    [SerializeField] private float highScoreDuration = 0.5f;

    [Header("Animation Delays")]
    [SerializeField] private float titleDelay = 0f;
    [SerializeField] private float playDelay = 0.15f;
    [SerializeField] private float creditsDelay = 0.3f;
    [SerializeField] private float exitButtonDelay = 0.45f;
    [SerializeField] private float highScoreDelay = 0.2f;

    [Header("Scale Settings")]
    [SerializeField] private float buttonStartScale = 0.85f;
    
    [Header("Animation Easing")]
    [SerializeField] private Ease titleEase = Ease.OutCubic;

    [SerializeField] private Ease playEase = Ease.OutBack;
    [SerializeField] private Ease creditsEase = Ease.OutBack;
    [SerializeField] private Ease exitButtonEase = Ease.OutBack;

    [SerializeField] private Ease highScoreEase = Ease.OutCubic;

    [Header("Controller References")]
    [SerializeField] private MainMenuButtonAnimator buttonAnimator;

    private Vector2 originalTitlePosition;
    private Vector2 originalPlayPosition;
    private Vector2 originalCreditsPosition;
    private Vector2 originalExitPosition;
    private Vector2 originalHighScorePosition;

    private Vector3 originalPlayScale;
    private Vector3 originalCreditsScale;
    private Vector3 originalExitScale;

    private Sequence entrySequence;

    private bool isEntryComplete;
    private bool isExiting;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        CacheOriginalValues();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        valid &= CheckReference(titleGroup, nameof(titleGroup));
        valid &= CheckReference(playGroup, nameof(playGroup));
        valid &= CheckReference(creditsGroup, nameof(creditsGroup));
        valid &= CheckReference(exitGroup, nameof(exitGroup));
        valid &= CheckReference(highScoreGroup, nameof(highScoreGroup));

        valid &= CheckReference(transitionOverlay, nameof(transitionOverlay));

        valid &= CheckReference(titleTransform, nameof(titleTransform));
        valid &= CheckReference(playTransform, nameof(playTransform));
        valid &= CheckReference(creditsTransform, nameof(creditsTransform));
        valid &= CheckReference(exitTransform, nameof(exitTransform));
        valid &= CheckReference(highScoreTransform, nameof(highScoreTransform));

        return valid;
    }

    private bool CheckReference(UnityEngine.Object reference, string fieldName)
    {
        if (reference != null)
            return true;

        Debug.LogError(
            $"MAIN MENU TRANSITION | Missing Inspector reference: {fieldName}",
            this
        );

        return false;
    }

    private void Start()
    {
        PrepareInitialState();
        PlayEntryAnimation();
    }

    private void CacheOriginalValues()
    {
        originalTitlePosition = titleTransform.anchoredPosition;

        originalPlayPosition = playTransform.anchoredPosition;
        originalCreditsPosition = creditsTransform.anchoredPosition;
        originalExitPosition = exitTransform.anchoredPosition;

        originalHighScorePosition = highScoreTransform.anchoredPosition;

        originalPlayScale = playTransform.localScale;
        originalCreditsScale = creditsTransform.localScale;
        originalExitScale = exitTransform.localScale;
    }

    private void PrepareInitialState()
    {
        // Prevent hovering during the entrance animation.
        if (buttonAnimator != null)
            buttonAnimator.enabled = false;

        // Start with a black screen.
        transitionOverlay.alpha = 1f;
        transitionOverlay.interactable = false;
        transitionOverlay.blocksRaycasts = true;

        // Hide the remaining UI.
        titleGroup.alpha = 0f;

        playGroup.alpha = 0f;
        creditsGroup.alpha = 0f;
        exitGroup.alpha = 0f;

        highScoreGroup.alpha = 0f;

        // Disable interaction during the entrance.
        SetInteraction(false);

        // Move the title down from its original position.
        titleTransform.anchoredPosition =
            originalTitlePosition + titleStartOffset;

        // Move buttons in from the left.
        playTransform.anchoredPosition =
            originalPlayPosition + playStartOffset;

        creditsTransform.anchoredPosition =
            originalCreditsPosition + creditsStartOffset;

        exitTransform.anchoredPosition =
            originalExitPosition + exitStartOffset;

        // Start buttons slightly smaller.
        playTransform.localScale =
            originalPlayScale * buttonStartScale;

        creditsTransform.localScale =
            originalCreditsScale * buttonStartScale;

        exitTransform.localScale =
            originalExitScale * buttonStartScale;

        // Move high score panel in from the right.
        highScoreTransform.anchoredPosition =
            originalHighScorePosition + highScoreStartOffset;
    }

    private void PlayEntryAnimation()
    {
        entrySequence = DOTween.Sequence().SetAutoKill(false);

        // Black overlay fades away first.
        entrySequence.Append(
            transitionOverlay.DOFade(0f, overlayFadeDuration)
                .SetEase(Ease.OutQuad)
        );

        // All other delays are measured from the end of the overlay fade.
        float animationStartTime = overlayFadeDuration;

        // Title.
        entrySequence.Insert(
            animationStartTime + titleDelay,
            titleGroup.DOFade(1f, titleDuration)
                .SetEase(Ease.OutQuad)
        );

        entrySequence.Insert(
            animationStartTime + titleDelay,
            titleTransform.DOAnchorPos(
                originalTitlePosition,
                titleDuration
            ).SetEase(titleEase)
        );

        // Play button.
        entrySequence.Insert(
            animationStartTime + playDelay,
            playGroup.DOFade(1f, playDuration)
                .SetEase(Ease.OutQuad)
        );

        entrySequence.Insert(
            animationStartTime + playDelay,
            playTransform.DOAnchorPos(
                originalPlayPosition,
                playDuration
            ).SetEase(playEase)
        );

        entrySequence.Insert(
            animationStartTime + playDelay,
            playTransform.DOScale(
                originalPlayScale,
                playDuration
            ).SetEase(playEase)
        );

        // Credits button.
        entrySequence.Insert(
            animationStartTime + creditsDelay,
            creditsGroup.DOFade(1f, creditsDuration)
                .SetEase(Ease.OutQuad)
        );

        entrySequence.Insert(
            animationStartTime + creditsDelay,
            creditsTransform.DOAnchorPos(
                originalCreditsPosition,
                creditsDuration
            ).SetEase(creditsEase)
        );

        entrySequence.Insert(
            animationStartTime + creditsDelay,
            creditsTransform.DOScale(
                originalCreditsScale,
                creditsDuration
            ).SetEase(creditsEase)
        );

        // Exit button.
        entrySequence.Insert(
            animationStartTime + exitButtonDelay,
            exitGroup.DOFade(1f, exitButtonDuration)
                .SetEase(Ease.OutQuad)
        );

        entrySequence.Insert(
            animationStartTime + exitButtonDelay,
            exitTransform.DOAnchorPos(
                originalExitPosition,
                exitButtonDuration
            ).SetEase(exitButtonEase)
        );

        entrySequence.Insert(
            animationStartTime + exitButtonDelay,
            exitTransform.DOScale(
                originalExitScale,
                exitButtonDuration
            ).SetEase(exitButtonEase)
        );

        // High score.
        entrySequence.Insert(
            animationStartTime + highScoreDelay,
            highScoreGroup.DOFade(1f, highScoreDuration)
                .SetEase(Ease.OutQuad)
        );

        entrySequence.Insert(
            animationStartTime + highScoreDelay,
            highScoreTransform.DOAnchorPos(
                originalHighScorePosition,
                highScoreDuration
            ).SetEase(highScoreEase)
        );

        // Re-enable interaction after the entire sequence finishes.
        entrySequence.OnComplete(() =>
        {
            isEntryComplete = true;

            transitionOverlay.blocksRaycasts = false;

            SetInteraction(true);

            if (buttonAnimator != null)
                buttonAnimator.enabled = true;
        });
    }

    private void SetInteraction(bool interactable)
    {
        playGroup.interactable = interactable;
        playGroup.blocksRaycasts = interactable;

        creditsGroup.interactable = interactable;
        creditsGroup.blocksRaycasts = interactable;

        exitGroup.interactable = interactable;
        exitGroup.blocksRaycasts = interactable;
    }

public void PlayExitAnimation(Action onComplete)
{
    if (isExiting)
    return;

    if (!isEntryComplete)
    {
        Debug.LogWarning(
            "MAIN MENU TRANSITION | Cannot start exit before entry animation finishes.",
            this
        );

        return;
    }

    if (entrySequence == null || !entrySequence.IsActive())
    {
        Debug.LogError(
            "MAIN MENU TRANSITION | Entry sequence is missing or inactive.",
            this
        );

        return;
    }

    isExiting = true;

    // Prevent further interaction.
    SetInteraction(false);

    if (buttonAnimator != null)
        buttonAnimator.enabled = false;

    // Block clicks while the exit animation plays.
    transitionOverlay.blocksRaycasts = true;

    // Reverse the original entry animation.
    entrySequence.OnRewind(() =>
    {
        onComplete?.Invoke();
    });

    entrySequence.PlayBackwards();
}

    private void OnDisable()
    {
        if (entrySequence != null && entrySequence.IsActive())
            entrySequence.Kill();
    }
}
