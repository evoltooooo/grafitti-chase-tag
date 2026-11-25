using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuAnimationController : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private SceneNavigation sceneNavigation;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup pauseScreenGroup;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup bgEffect1Group;
    [SerializeField] private CanvasGroup bgEffect2Group;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private CanvasGroup confirmationGroup;
    [SerializeField] private CanvasGroup transitionOverlayGroup;

    [Header("Individual Button References")]
    [SerializeField] private CanvasGroup resumeButtonGroup;
    [SerializeField] private CanvasGroup settingsButtonGroup;
    [SerializeField] private CanvasGroup exitButtonGroup;

    [SerializeField] private RectTransform resumeButtonTransform;
    [SerializeField] private RectTransform settingsButtonTransform;
    [SerializeField] private RectTransform exitButtonTransform;

    [Header("Button Hover Components")]
    [SerializeField] private UIButtonHoverScale resumeButtonHover;
    [SerializeField] private UIButtonHoverScale settingsButtonHover;
    [SerializeField] private UIButtonHoverScale exitButtonHover;
    [SerializeField] private UIButtonHoverScale yesButtonHover;
    [SerializeField] private UIButtonHoverScale noButtonHover;

    [Header("Button Hover Settings")]
    [SerializeField, Min(1f)] private float buttonHoverScale = 1.1f;
    [SerializeField, Min(0f)] private float buttonHoverDuration = 0.2f;
    [SerializeField] private Ease buttonHoverEase = Ease.OutQuad;
    [SerializeField] private Color buttonHoverColor = Color.yellow;

    [Header("Individual Button Popup Settings")]
    [SerializeField, Range(0.1f, 1f)]
    private float buttonStartScale = 0.75f;

    [SerializeField, Min(0f)]
    private float buttonPopupDuration = 0.4f;

    [SerializeField, Min(0f)]
    private float buttonPopupDelay = 0.12f;

    [SerializeField]
    private Ease buttonPopupEase = Ease.OutBack;

    [Header("UI Transforms")]
    [SerializeField] private RectTransform bgEffect1Transform;
    [SerializeField] private RectTransform bgEffect2Transform;
    [SerializeField] private RectTransform buttonsTransform;
    [SerializeField] private RectTransform confirmationTransform;

    [Header("BG Effect Slide Settings")]
    [SerializeField] private Vector2 bgEffect1StartOffset = new(-1920f, 0f);
    [SerializeField] private Vector2 bgEffect2StartOffset = new(1920f, 0f);
    [SerializeField, Min(0f)] private float bgEffectDuration = 0.5f;
    [SerializeField] private Ease bgEffectEase = Ease.OutCubic;

    [Header("Background Settings")]
    [SerializeField, Min(0f)] private float backgroundFadeDuration = 0.25f;
    [SerializeField] private float backgroundTargetAlpha = 0.7f;

    [Header("Buttons Popup Settings")]
    [SerializeField, Range(0.1f, 1f)] private float buttonsStartScale = 0.75f;
    [SerializeField, Min(0f)] private float buttonsDuration = 0.4f;
    [SerializeField] private Ease buttonsEase = Ease.OutBack;

    [Header("Confirmation Popup Settings")]
    [SerializeField, Range(0.1f, 1f)] private float confirmationStartScale = 0.75f;
    [SerializeField, Min(0f)] private float confirmationDuration = 0.3f;
    [SerializeField] private Ease confirmationEase = Ease.OutBack;

    [Header("Close Animation")]
    [SerializeField, Min(0f)] private float closeDuration = 0.25f;
    [SerializeField] private Ease closeEase = Ease.InCubic;

    [Header("Exit To Main Menu")]
    [SerializeField, Min(0f)] private float exitFadeDuration = 0.5f;

    private Vector2 bgEffect1OriginalPosition;
    private Vector2 bgEffect2OriginalPosition;

    private Vector3 buttonsOriginalScale;
    private Vector3 confirmationOriginalScale;

    private Vector3 resumeButtonOriginalScale;
    private Vector3 settingsButtonOriginalScale;
    private Vector3 exitButtonOriginalScale;

    private Sequence pauseSequence;
    private Sequence closeSequence;
    private Sequence confirmationSequence;
    private Sequence exitSequence;

    private bool isAnimating;
    private bool isConfirmationOpen;
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
    
    private void ResetButtonHoverImmediately()
    {
        resumeButtonHover.ResetHoverImmediately();
        settingsButtonHover.ResetHoverImmediately();
        exitButtonHover.ResetHoverImmediately();
        yesButtonHover.ResetHoverImmediately();
        noButtonHover.ResetHoverImmediately();
    }

    private void OnEnable()
    {
        if (pauseManager == null)
            return;

        pauseManager.GamePaused += OpenPauseMenu;
        pauseManager.GameResumed += ClosePauseMenu;
    }

    private void OnDisable()
    {
        if (pauseManager != null)
        {
            pauseManager.GamePaused -= OpenPauseMenu;
            pauseManager.GameResumed -= ClosePauseMenu;
        }

        KillSequences();

        ResetButtonHoverImmediately();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        valid &= Check(pauseManager, nameof(pauseManager));
        valid &= Check(sceneNavigation, nameof(sceneNavigation));

        valid &= Check(pauseScreenGroup, nameof(pauseScreenGroup));
        valid &= Check(backgroundGroup, nameof(backgroundGroup));
        valid &= Check(bgEffect1Group, nameof(bgEffect1Group));
        valid &= Check(bgEffect2Group, nameof(bgEffect2Group));
        valid &= Check(buttonsGroup, nameof(buttonsGroup));
        valid &= Check(confirmationGroup, nameof(confirmationGroup));
        valid &= Check(transitionOverlayGroup, nameof(transitionOverlayGroup));

        valid &= Check(bgEffect1Transform, nameof(bgEffect1Transform));
        valid &= Check(bgEffect2Transform, nameof(bgEffect2Transform));
        valid &= Check(buttonsTransform, nameof(buttonsTransform));
        valid &= Check(confirmationTransform, nameof(confirmationTransform));

        // Individual button references.
        valid &= Check(resumeButtonGroup, nameof(resumeButtonGroup));
        valid &= Check(exitButtonGroup, nameof(exitButtonGroup));

        valid &= Check(resumeButtonTransform, nameof(resumeButtonTransform));
        valid &= Check(exitButtonTransform, nameof(exitButtonTransform));

        valid &= Check(settingsButtonGroup, nameof(settingsButtonGroup));
        valid &= Check(settingsButtonTransform, nameof(settingsButtonTransform));
        valid &= Check(settingsButtonHover, nameof(settingsButtonHover));

        valid &= Check(resumeButtonHover, nameof(resumeButtonHover));
        valid &= Check(exitButtonHover, nameof(exitButtonHover));
        valid &= Check(yesButtonHover, nameof(yesButtonHover));
        valid &= Check(noButtonHover, nameof(noButtonHover));

        return valid;
    }

    private bool Check(Object reference, string fieldName)
    {
        if (reference != null)
            return true;

        Debug.LogError(
            $"PAUSE MENU | Missing reference: {fieldName}",
            this
        );

        return false;
    }

    private void CacheOriginalValues()
    {
        bgEffect1OriginalPosition = bgEffect1Transform.anchoredPosition;
        bgEffect2OriginalPosition = bgEffect2Transform.anchoredPosition;

        buttonsOriginalScale = buttonsTransform.localScale;
        confirmationOriginalScale = confirmationTransform.localScale;

        resumeButtonOriginalScale = resumeButtonTransform.localScale;
        settingsButtonOriginalScale = settingsButtonTransform.localScale;
        exitButtonOriginalScale = exitButtonTransform.localScale;

        resumeButtonHover.SetOriginalScale(resumeButtonOriginalScale);
        settingsButtonHover.SetOriginalScale(settingsButtonOriginalScale);
        exitButtonHover.SetOriginalScale(exitButtonOriginalScale);

        yesButtonHover.SetOriginalScale(
            yesButtonHover.transform.localScale
        );

        noButtonHover.SetOriginalScale(
            noButtonHover.transform.localScale
        );
    }

    private void PrepareInitialState()
    {
        pauseScreenGroup.alpha = 1f;
        pauseScreenGroup.interactable = false;
        pauseScreenGroup.blocksRaycasts = false;

        backgroundGroup.alpha = 0f;

        bgEffect1Group.alpha = 0f;
        bgEffect2Group.alpha = 0f;

        bgEffect1Transform.anchoredPosition =
            bgEffect1OriginalPosition + bgEffect1StartOffset;

        bgEffect2Transform.anchoredPosition =
            bgEffect2OriginalPosition + bgEffect2StartOffset;

        buttonsGroup.alpha = 1f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;

        resumeButtonGroup.alpha = 0f;
        exitButtonGroup.alpha = 0f;

        resumeButtonTransform.localScale =
            resumeButtonOriginalScale * buttonStartScale;

        settingsButtonGroup.alpha = 0f;
        settingsButtonGroup.interactable = false;
        settingsButtonGroup.blocksRaycasts = false;

        settingsButtonTransform.localScale =
            settingsButtonOriginalScale * buttonStartScale;

        exitButtonTransform.localScale =
            exitButtonOriginalScale * buttonStartScale;

        confirmationGroup.alpha = 0f;
        confirmationGroup.interactable = false;
        confirmationGroup.blocksRaycasts = false;

        confirmationTransform.localScale =
            confirmationOriginalScale * confirmationStartScale;

        transitionOverlayGroup.alpha = 0f;
        transitionOverlayGroup.interactable = false;
        transitionOverlayGroup.blocksRaycasts = false;
    }

    private void OpenPauseMenu()
    {
        if (isAnimating || isExiting)
            return;
        
        pauseManager.SetPauseInputLocked(true);

        KillSequences();

        isAnimating = true;

        ResetButtonHoverImmediately();
        
        isConfirmationOpen = false;

        pauseScreenGroup.alpha = 1f;
        pauseScreenGroup.blocksRaycasts = true;
        pauseScreenGroup.interactable = false;

        // Reset individual buttons before every pause opening.
        buttonsGroup.alpha = 1f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;

        resumeButtonGroup.alpha = 0f;
        resumeButtonGroup.interactable = false;
        resumeButtonGroup.blocksRaycasts = false;

        settingsButtonGroup.alpha = 0f;
        settingsButtonGroup.interactable = false;
        settingsButtonGroup.blocksRaycasts = false;

        exitButtonGroup.alpha = 0f;
        exitButtonGroup.interactable = false;
        exitButtonGroup.blocksRaycasts = false;

        resumeButtonTransform.localScale =
            resumeButtonOriginalScale * buttonStartScale;
        
        settingsButtonTransform.localScale =
            settingsButtonOriginalScale * buttonStartScale;

        exitButtonTransform.localScale =
            exitButtonOriginalScale * buttonStartScale;

        pauseSequence = DOTween.Sequence()
            .SetUpdate(true);

        pauseSequence.Append(
            backgroundGroup.DOFade(
                backgroundTargetAlpha,
                backgroundFadeDuration
            )
        );

        pauseSequence.Insert(
            0f,
            bgEffect1Group.DOFade(1f, bgEffectDuration)
        );

        pauseSequence.Insert(
            0f,
            bgEffect1Transform.DOAnchorPos(
                bgEffect1OriginalPosition,
                bgEffectDuration
            ).SetEase(bgEffectEase)
        );

        pauseSequence.Insert(
            0f,
            bgEffect2Group.DOFade(1f, bgEffectDuration)
        );

        pauseSequence.Insert(
            0f,
            bgEffect2Transform.DOAnchorPos(
                bgEffect2OriginalPosition,
                bgEffectDuration
            ).SetEase(bgEffectEase)
        );

        // Resume Button Popup
        pauseSequence.Insert(
            0.1f,
            resumeButtonGroup.DOFade(1f, buttonPopupDuration)
        );

        pauseSequence.Insert(
            0.1f,
            resumeButtonTransform.DOScale(
                resumeButtonOriginalScale,
                buttonPopupDuration
            ).SetEase(buttonPopupEase)
        );

        // Settings Button Popup
        pauseSequence.Insert(
            0.1f + buttonPopupDelay,
            settingsButtonGroup.DOFade(1f, buttonPopupDuration)
        );

        pauseSequence.Insert(
            0.1f + buttonPopupDelay,
            settingsButtonTransform.DOScale(
                settingsButtonOriginalScale,
                buttonPopupDuration
            ).SetEase(buttonPopupEase)
        );

        // Exit Button Popup
        pauseSequence.Insert(
            0.1f + buttonPopupDelay * 2,
            exitButtonGroup.DOFade(1f, buttonPopupDuration)
        );

        pauseSequence.Insert(
            0.1f + buttonPopupDelay * 2,
            exitButtonTransform.DOScale(
                exitButtonOriginalScale,
                buttonPopupDuration
            ).SetEase(buttonPopupEase)
        );

        pauseSequence.OnComplete(() =>
        {
            isAnimating = false;

            pauseScreenGroup.interactable = true;

            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;

            resumeButtonGroup.interactable = true;
            resumeButtonGroup.blocksRaycasts = true;

            settingsButtonGroup.interactable = true;
            settingsButtonGroup.blocksRaycasts = true;

            exitButtonGroup.interactable = true;
            exitButtonGroup.blocksRaycasts = true;

            pauseManager.SetPauseInputLocked(false);
        });
    }

    public void ResumeGame()
    {
        if (pauseManager == null || isAnimating || isExiting)
            return;

        if (isConfirmationOpen)
            return;

        pauseManager.ResumeGame();
    }

    private void ClosePauseMenu()
    {
        if (isAnimating || isExiting)
            return;
        
        pauseManager.SetPauseInputLocked(true);

        KillSequences();

        isAnimating = true;

        ResetButtonHoverImmediately();

        pauseScreenGroup.interactable = false;
        pauseScreenGroup.blocksRaycasts = false;

        settingsButtonGroup.interactable = false;
        settingsButtonGroup.blocksRaycasts = false;

        closeSequence = DOTween.Sequence()
            .SetUpdate(true);

        closeSequence.Append(
            pauseScreenGroup.DOFade(0f, closeDuration)
                .SetEase(closeEase)
        );

        closeSequence.Insert(
            0f,
            backgroundGroup.DOFade(0f, closeDuration)
        );

        closeSequence.Insert(
            0f,
            bgEffect1Group.DOFade(0f, closeDuration)
        );

        closeSequence.Insert(
            0f,
            bgEffect1Transform.DOAnchorPos(
                bgEffect1OriginalPosition + bgEffect1StartOffset,
                closeDuration
            ).SetEase(closeEase)
        );

        closeSequence.Insert(
            0f,
            bgEffect2Group.DOFade(0f, closeDuration)
        );

        closeSequence.Insert(
            0f,
            bgEffect2Transform.DOAnchorPos(
                bgEffect2OriginalPosition + bgEffect2StartOffset,
                closeDuration
            ).SetEase(closeEase)
        );

        // Close Resume Button individually.
        closeSequence.Insert(
            0f,
            resumeButtonGroup.DOFade(0f, closeDuration)
        );

        closeSequence.Insert(
            0f,
            resumeButtonTransform.DOScale(
                resumeButtonOriginalScale * buttonStartScale,
                closeDuration
            ).SetEase(closeEase)
        );

        // Close Settings Button individually.
        closeSequence.Insert(
            0f,
            settingsButtonGroup.DOFade(0f, closeDuration)
        );

        closeSequence.Insert(
            0f,
            settingsButtonTransform.DOScale(
                settingsButtonOriginalScale * buttonStartScale,
                closeDuration
            ).SetEase(closeEase)
        );

        // Close Exit Button individually.
        closeSequence.Insert(
            0f,
            exitButtonGroup.DOFade(0f, closeDuration)
        );

        closeSequence.Insert(
            0f,
            exitButtonTransform.DOScale(
                exitButtonOriginalScale * buttonStartScale,
                closeDuration
            ).SetEase(closeEase)
        );
        closeSequence.OnComplete(() =>
        {
            pauseManager.SetPauseInputLocked(false);
            isAnimating = false;
            isConfirmationOpen = false;
        });
    }

    public void OpenConfirmation()
    {
        if (isAnimating || isConfirmationOpen || isExiting)
            return;
        
        pauseManager.SetPauseInputLocked(true);

        isAnimating = true;

        ResetButtonHoverImmediately();

        isConfirmationOpen = true;

        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;

        confirmationGroup.blocksRaycasts = true;

        confirmationSequence = DOTween.Sequence()
            .SetUpdate(true);

        confirmationSequence.Append(
            buttonsGroup.DOFade(0f, 0.15f)
        );

        confirmationSequence.Append(
            confirmationGroup.DOFade(1f, confirmationDuration)
        );

        confirmationSequence.Join(
            confirmationTransform.DOScale(
                confirmationOriginalScale,
                confirmationDuration
            ).SetEase(confirmationEase)
        );

        confirmationSequence.OnComplete(() =>
        {
            isAnimating = false;
            confirmationGroup.interactable = true;
        });
    }

    public void CancelConfirmation()
    {
        if (isAnimating || !isConfirmationOpen || isExiting)
            return;

        isAnimating = true;

        ResetButtonHoverImmediately();

        confirmationGroup.interactable = false;
        confirmationGroup.blocksRaycasts = false;

        confirmationSequence = DOTween.Sequence()
            .SetUpdate(true);

        confirmationSequence.Append(
            confirmationGroup.DOFade(0f, 0.15f)
        );

        confirmationSequence.Join(
            confirmationTransform.DOScale(
                confirmationOriginalScale * confirmationStartScale,
                0.15f
            ).SetEase(closeEase)
        );

        confirmationSequence.Append(
            buttonsGroup.DOFade(1f, 0.2f)
        );

        confirmationSequence.OnComplete(() =>
        {
            isConfirmationOpen = false;
            isAnimating = false;

            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;

            pauseManager.SetPauseInputLocked(false);
        });
    }

    public void ConfirmExitToMainMenu()
    {
        if (isAnimating || isExiting)
            return;
        
        pauseManager.SetPauseInputLocked(true);

        isExiting = true;
        isAnimating = true;

        ResetButtonHoverImmediately();

        pauseScreenGroup.interactable = false;
        pauseScreenGroup.blocksRaycasts = true;

        transitionOverlayGroup.alpha = 0f;
        transitionOverlayGroup.blocksRaycasts = true;

        exitSequence = DOTween.Sequence()
            .SetUpdate(true);

        exitSequence.Append(
            transitionOverlayGroup.DOFade(
                1f,
                exitFadeDuration
            ).SetEase(Ease.OutQuad)
        );

        exitSequence.OnComplete(() =>
        {
            Time.timeScale = 1f;
            GameplayInputLock.SetLocked(false);

            sceneNavigation.OpenMainMenu();
        });
    }

    private void KillSequences()
    {
        pauseSequence?.Kill();
        closeSequence?.Kill();
        confirmationSequence?.Kill();
        exitSequence?.Kill();
    }
}
