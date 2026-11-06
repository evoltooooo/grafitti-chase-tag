
using UnityEngine;
using DG.Tweening;

public class GameEndTransitionController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup elapsedTitleGroup;
    [SerializeField] private CanvasGroup elapsedScoreGroup;
    [SerializeField] private CanvasGroup winnerGroup;
    [SerializeField] private CanvasGroup playAgainGroup;
    [SerializeField] private CanvasGroup mainMenuGroup;

    [Header("UI Transforms")]
    [SerializeField] private RectTransform titleTransform;
    [SerializeField] private RectTransform elapsedTitleTransform;
    [SerializeField] private RectTransform elapsedScoreTransform;
    [SerializeField] private RectTransform winnerTransform;
    [SerializeField] private RectTransform playAgainTransform;
    [SerializeField] private RectTransform mainMenuTransform;

    [Header("Entry Start Offsets")]
    [SerializeField] private Vector2 titleStartOffset = new(0f, 50f);
    [SerializeField] private Vector2 elapsedTitleStartOffset = new(0f, 30f);
    [SerializeField] private Vector2 elapsedScoreStartOffset = new(0f, 30f);
    [SerializeField] private Vector2 winnerStartOffset = new(0f, -50f);
    [SerializeField] private Vector2 playAgainStartOffset = new(-60f, 0f);
    [SerializeField] private Vector2 mainMenuStartOffset = new(60f, 0f);

    [Header("Animation Durations")]
    [SerializeField, Min(0f)] private float titleDuration = 0.5f;
    [SerializeField, Min(0f)] private float elapsedTitleDuration = 0.4f;
    [SerializeField, Min(0f)] private float elapsedScoreDuration = 0.4f;
    [SerializeField, Min(0f)] private float winnerDuration = 0.5f;
    [SerializeField, Min(0f)] private float playAgainDuration = 0.4f;
    [SerializeField, Min(0f)] private float mainMenuDuration = 0.4f;

    [Header("Animation Delays")]
    [SerializeField, Min(0f)] private float titleDelay = 0f;
    [SerializeField, Min(0f)] private float elapsedTitleDelay = 0.2f;
    [SerializeField, Min(0f)] private float elapsedScoreDelay = 0.3f;
    [SerializeField, Min(0f)] private float winnerDelay = 0.5f;
    [SerializeField, Min(0f)] private float playAgainDelay = 0.8f;
    [SerializeField, Min(0f)] private float mainMenuDelay = 0.95f;

    [Header("Scale Settings")]
    [SerializeField, Range(0.1f, 1f)]
    private float buttonStartScale = 0.85f;

    [Header("Exit Animation")]
    [SerializeField, Min(0.01f)]
    private float exitDuration = 0.3f;

    [SerializeField]
    private Ease exitEase = Ease.InCubic;

    [Header("Animation Easing")]
    [SerializeField] private Ease titleEase = Ease.OutCubic;
    [SerializeField] private Ease elapsedTitleEase = Ease.OutCubic;
    [SerializeField] private Ease elapsedScoreEase = Ease.OutCubic;
    [SerializeField] private Ease winnerEase = Ease.OutBack;
    [SerializeField] private Ease playAgainEase = Ease.OutBack;
    [SerializeField] private Ease mainMenuEase = Ease.OutBack;

    private Vector2 originalTitlePosition;
    private Vector2 originalElapsedTitlePosition;
    private Vector2 originalElapsedScorePosition;
    private Vector2 originalWinnerPosition;
    private Vector2 originalPlayAgainPosition;
    private Vector2 originalMainMenuPosition;

    private Vector3 originalPlayAgainScale;
    private Vector3 originalMainMenuScale;

    private Sequence entrySequence;
    private Sequence exitSequence;

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
        PrepareInitialState();
    }

    private void Start()
    {
        PlayEntryAnimation();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        valid &= CheckReference(titleGroup, nameof(titleGroup));
        valid &= CheckReference(elapsedTitleGroup, nameof(elapsedTitleGroup));
        valid &= CheckReference(elapsedScoreGroup, nameof(elapsedScoreGroup));
        valid &= CheckReference(winnerGroup, nameof(winnerGroup));
        valid &= CheckReference(playAgainGroup, nameof(playAgainGroup));
        valid &= CheckReference(mainMenuGroup, nameof(mainMenuGroup));

        valid &= CheckReference(titleTransform, nameof(titleTransform));
        valid &= CheckReference(elapsedTitleTransform, nameof(elapsedTitleTransform));
        valid &= CheckReference(elapsedScoreTransform, nameof(elapsedScoreTransform));
        valid &= CheckReference(winnerTransform, nameof(winnerTransform));
        valid &= CheckReference(playAgainTransform, nameof(playAgainTransform));
        valid &= CheckReference(mainMenuTransform, nameof(mainMenuTransform));

        return valid;
    }

    private bool CheckReference(Object reference, string fieldName)
    {
        if (reference != null)
            return true;

        Debug.LogError(
            $"GAME END TRANSITION | Missing reference: {fieldName}",
            this
        );

        return false;
    }

    private void CacheOriginalValues()
    {
        originalTitlePosition = titleTransform.anchoredPosition;
        originalElapsedTitlePosition = elapsedTitleTransform.anchoredPosition;
        originalElapsedScorePosition = elapsedScoreTransform.anchoredPosition;
        originalWinnerPosition = winnerTransform.anchoredPosition;
        originalPlayAgainPosition = playAgainTransform.anchoredPosition;
        originalMainMenuPosition = mainMenuTransform.anchoredPosition;

        originalPlayAgainScale = playAgainTransform.localScale;
        originalMainMenuScale = mainMenuTransform.localScale;
    }

    private void PrepareInitialState()
    {
        titleGroup.alpha = 0f;
        elapsedTitleGroup.alpha = 0f;
        elapsedScoreGroup.alpha = 0f;
        winnerGroup.alpha = 0f;
        playAgainGroup.alpha = 0f;
        mainMenuGroup.alpha = 0f;

        SetInteraction(false);

        titleTransform.anchoredPosition =
            originalTitlePosition + titleStartOffset;

        elapsedTitleTransform.anchoredPosition =
            originalElapsedTitlePosition + elapsedTitleStartOffset;

        elapsedScoreTransform.anchoredPosition =
            originalElapsedScorePosition + elapsedScoreStartOffset;

        winnerTransform.anchoredPosition =
            originalWinnerPosition + winnerStartOffset;

        playAgainTransform.anchoredPosition =
            originalPlayAgainPosition + playAgainStartOffset;

        mainMenuTransform.anchoredPosition =
            originalMainMenuPosition + mainMenuStartOffset;

        playAgainTransform.localScale =
            originalPlayAgainScale * buttonStartScale;

        mainMenuTransform.localScale =
            originalMainMenuScale * buttonStartScale;
    }

    private void PlayEntryAnimation()
    {
        entrySequence = DOTween.Sequence().SetAutoKill(false);

        // Title.
        AddFadeAndMovement(
            titleGroup, titleTransform,
            originalTitlePosition,
            titleDuration, titleDelay, titleEase
        );

        // Elapsed time label.
        AddFadeAndMovement(
            elapsedTitleGroup, elapsedTitleTransform,
            originalElapsedTitlePosition,
            elapsedTitleDuration, elapsedTitleDelay, elapsedTitleEase
        );

        // Elapsed time value.
        AddFadeAndMovement(
            elapsedScoreGroup, elapsedScoreTransform,
            originalElapsedScorePosition,
            elapsedScoreDuration, elapsedScoreDelay, elapsedScoreEase
        );

        // Winner text.
        AddFadeAndMovement(
            winnerGroup, winnerTransform,
            originalWinnerPosition,
            winnerDuration, winnerDelay, winnerEase
        );

        // Play Again button.
        AddFadeAndMovement(
            playAgainGroup, playAgainTransform,
            originalPlayAgainPosition,
            playAgainDuration, playAgainDelay, playAgainEase
        );

        entrySequence.Insert(
            playAgainDelay,
            playAgainTransform.DOScale(
                originalPlayAgainScale,
                playAgainDuration
            ).SetEase(playAgainEase)
        );

        // Main Menu button.
        AddFadeAndMovement(
            mainMenuGroup, mainMenuTransform,
            originalMainMenuPosition,
            mainMenuDuration, mainMenuDelay, mainMenuEase
        );

        entrySequence.Insert(
            mainMenuDelay,
            mainMenuTransform.DOScale(
                originalMainMenuScale,
                mainMenuDuration
            ).SetEase(mainMenuEase)
        );

        entrySequence.OnComplete(() =>
        {
            isEntryComplete = true;
            SetInteraction(true);
        });
    }

    public void PlayExitAnimation(System.Action onComplete)
    {
        if (isExiting || !isEntryComplete)
            return;

        if (entrySequence == null || !entrySequence.IsActive())
        {
            Debug.LogError(
                "GAME END TRANSITION | Entry sequence is missing or inactive.",
                this
            );

            return;
        }

        isExiting = true;

        SetInteraction(false);

        exitSequence = DOTween.Sequence();

        // All elements begin their exit animation at the same time.

        // Title.
        exitSequence.Insert(0f,
            titleGroup.DOFade(0f, exitDuration));

        exitSequence.Insert(0f,
            titleTransform.DOAnchorPos(
                originalTitlePosition + titleStartOffset,
                exitDuration
            ).SetEase(exitEase));

        // Elapsed time title.
        exitSequence.Insert(0f,
            elapsedTitleGroup.DOFade(0f, exitDuration));

        exitSequence.Insert(0f,
            elapsedTitleTransform.DOAnchorPos(
                originalElapsedTitlePosition + elapsedTitleStartOffset,
                exitDuration
            ).SetEase(exitEase));

        // Elapsed time score.
        exitSequence.Insert(0f,
            elapsedScoreGroup.DOFade(0f, exitDuration));

        exitSequence.Insert(0f,
            elapsedScoreTransform.DOAnchorPos(
                originalElapsedScorePosition + elapsedScoreStartOffset,
                exitDuration
            ).SetEase(exitEase));

        // Winner text.
        exitSequence.Insert(0f,
            winnerGroup.DOFade(0f, exitDuration));

        exitSequence.Insert(0f,
            winnerTransform.DOAnchorPos(
                originalWinnerPosition + winnerStartOffset,
                exitDuration
            ).SetEase(exitEase));

        // Play Again button.
        exitSequence.Insert(0f,
            playAgainGroup.DOFade(0f, exitDuration));

        exitSequence.Insert(0f,
            playAgainTransform.DOAnchorPos(
                originalPlayAgainPosition + playAgainStartOffset,
                exitDuration
            ).SetEase(exitEase));

        exitSequence.Insert(0f,
            playAgainTransform.DOScale(
                originalPlayAgainScale * buttonStartScale,
                exitDuration
            ).SetEase(exitEase));

        // Main Menu button.
        exitSequence.Insert(0f,
            mainMenuGroup.DOFade(0f, exitDuration));

        exitSequence.Insert(0f,
            mainMenuTransform.DOAnchorPos(
                originalMainMenuPosition + mainMenuStartOffset,
                exitDuration
            ).SetEase(exitEase));

        exitSequence.Insert(0f,
            mainMenuTransform.DOScale(
                originalMainMenuScale * buttonStartScale,
                exitDuration
            ).SetEase(exitEase));

        // Load the destination scene after all animations finish.
        exitSequence.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    private void AddFadeAndMovement(
        CanvasGroup group,
        RectTransform target,
        Vector2 originalPosition,
        float duration,
        float delay,
        Ease ease)
    {
        entrySequence.Insert(
            delay,
            group.DOFade(1f, duration)
                .SetEase(Ease.OutQuad)
        );

        entrySequence.Insert(
            delay,
            target.DOAnchorPos(originalPosition, duration)
                .SetEase(ease)
        );
    }

    private void SetInteraction(bool interactable)
    {
        playAgainGroup.interactable = interactable;
        playAgainGroup.blocksRaycasts = interactable;

        mainMenuGroup.interactable = interactable;
        mainMenuGroup.blocksRaycasts = interactable;
    }

    private void OnDisable()
    {
        if (entrySequence != null && entrySequence.IsActive())
            entrySequence.Kill();

        if (exitSequence != null && exitSequence.IsActive())
            exitSequence.Kill();
    }
}
