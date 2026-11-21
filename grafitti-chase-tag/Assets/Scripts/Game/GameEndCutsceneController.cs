using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameEndCutsceneController : MonoBehaviour
{
    [Header("Video References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip playerWinClip;
    [SerializeField] private VideoClip aiWinClip;

    [Header("Winner Detection")]
    [SerializeField] private string playerWinnerIdentifier = "PLAYER";
    [SerializeField] private string aiWinnerIdentifier = "AI";

    [Header("UI References")]
    [SerializeField] private CanvasGroup winGraphicGroup;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private GameEndTransitionController resultTransition;
    [SerializeField] private RectTransform winGraphicTransform;

    [Header("WIN Graphic Animation")]
    [SerializeField, Min(0f)]
    private float winStartScale = 0.65f;

    [SerializeField, Min(0f)]
    private float winPrePopupScale = 0.85f;

    [SerializeField, Min(0f)]
    private float winPopupScale = 1.2f;

    [SerializeField, Min(0f)]
    private float winSlowDuration = 0.65f;

    [SerializeField, Min(0f)]
    private float winPopupDuration = 0.18f;

    [SerializeField, Min(0f)]
    private float winSettleDuration = 0.12f;

    [SerializeField]
    private Ease winSlowEase = Ease.InSine;

    [SerializeField]
    private Ease winPopupEase = Ease.OutBack;

    [SerializeField]
    private Ease winSettleEase = Ease.OutSine;
    private Vector3 originalWinScale;

    [Header("Cutscene Timing")]
    [SerializeField, Min(0f)]
    private float delayBeforeWinGraphic = 1f;

    [SerializeField, Min(0f)]
    private float winGraphicFadeInDuration = 0.3f;

    [SerializeField, Min(0f)]
    private float winGraphicDisplayDuration = 1.5f;

    [SerializeField, Min(0f)]
    private float backgroundFadeDuration = 0.8f;

    [SerializeField, Min(0f)]
    private float resultRevealDelay = 0f;

    [Header("Result Button References")]
    [SerializeField] private RectTransform playAgainButtonTransform;
    [SerializeField] private RectTransform mainMenuButtonTransform;

    [SerializeField] private RectTransform playAgainButtonWhiteTransform;
    [SerializeField] private RectTransform mainMenuButtonWhiteTransform;

    [Header("Button Hover Settings")]
    [SerializeField, Min(1f)]
    private float buttonHoverScaleMultiplier = 1.12f;

    [SerializeField, Min(0f)]
    private float buttonHoverDuration = 0.12f;

    [SerializeField]
    private Ease buttonHoverEase = Ease.OutQuad;

    [SerializeField]
    private Color buttonHoverColor = Color.yellow;

    private Image playAgainButtonWhiteImage;
    private Image mainMenuButtonWhiteImage;

    private Vector3 playAgainOriginalScale;
    private Vector3 mainMenuOriginalScale;

    private Color playAgainOriginalColor;
    private Color mainMenuOriginalColor;

    private Tween playAgainScaleTween;
    private Tween playAgainColorTween;

    private Tween mainMenuScaleTween;
    private Tween mainMenuColorTween;

    private Canvas uiCameraCanvas;

    private int currentHoveredButton = -1;
    private bool resultButtonsReady;

    [Header("Debug")]
    [SerializeField] private bool useDebugWinner = false;
    [SerializeField] private string debugWinner = "PLAYER WINS!";

    private void Awake()
    {
        if (videoPlayer == null ||
            winGraphicGroup == null ||
            winGraphicTransform == null ||
            backgroundGroup == null ||
            resultTransition == null ||
            playAgainButtonTransform == null ||
            mainMenuButtonTransform == null ||
            playAgainButtonWhiteTransform == null ||
            mainMenuButtonWhiteTransform == null)
        {
            Debug.LogError(
                "GAME END CUTSCENE | Missing references.",
                this
            );

            enabled = false;
            return;
        }

        winGraphicGroup.alpha = 0f;
        winGraphicGroup.interactable = false;
        winGraphicGroup.blocksRaycasts = false;

        backgroundGroup.alpha = 0f;
        backgroundGroup.interactable = false;
        backgroundGroup.blocksRaycasts = false;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        originalWinScale = winGraphicTransform.localScale;

        winGraphicTransform.localScale =
            originalWinScale * winStartScale;
        
        playAgainButtonWhiteImage =
            playAgainButtonWhiteTransform.GetComponent<Image>();

        mainMenuButtonWhiteImage =
            mainMenuButtonWhiteTransform.GetComponent<Image>();

        if (playAgainButtonWhiteImage == null ||
            mainMenuButtonWhiteImage == null)
        {
            Debug.LogError(
                "GAME END CUTSCENE | Button White Image missing.",
                this
            );

            enabled = false;
            return;
        }

        playAgainOriginalScale =
            playAgainButtonTransform.localScale;

        mainMenuOriginalScale =
            mainMenuButtonTransform.localScale;

        playAgainOriginalColor =
            playAgainButtonWhiteImage.color;

        mainMenuOriginalColor =
            mainMenuButtonWhiteImage.color;

        Canvas rootCanvas =
            playAgainButtonWhiteTransform.GetComponentInParent<Canvas>();

        if (rootCanvas != null)
        {
            rootCanvas = rootCanvas.rootCanvas;

            if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCameraCanvas = rootCanvas;
        }

        videoPlayer.waitForFirstFrame = true;
    }

    private void Start()
    {
        StartCoroutine(PlayCutscene());
    }

    private void Update()
    {
        if (!resultButtonsReady)
            return;

        if (Mouse.current == null)
            return;

        int hoveredButton = GetHoveredButtonIndex();

        if (hoveredButton != currentHoveredButton)
        {
            SetHoveredButton(hoveredButton);
        }
    }

    private int GetHoveredButtonIndex()
    {
        Vector2 pointerPosition =
            Mouse.current.position.ReadValue();

        Camera eventCamera = uiCameraCanvas != null
            ? uiCameraCanvas.worldCamera
            : null;

        if (RectTransformUtility.RectangleContainsScreenPoint(
            playAgainButtonWhiteTransform,
            pointerPosition,
            eventCamera))
        {
            return 0;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(
            mainMenuButtonWhiteTransform,
            pointerPosition,
            eventCamera))
        {
            return 1;
        }

        return -1;
    }

    private void SetHoveredButton(int hoveredButton)
    {
        currentHoveredButton = hoveredButton;

        AnimatePlayAgainButton(false);
        AnimateMainMenuButton(false);

        if (hoveredButton == 0)
            AnimatePlayAgainButton(true);

        else if (hoveredButton == 1)
            AnimateMainMenuButton(true);
    }

    private void AnimatePlayAgainButton(bool hovered)
    {
        playAgainScaleTween?.Kill();
        playAgainColorTween?.Kill();

        Vector3 targetScale = hovered
            ? playAgainOriginalScale * buttonHoverScaleMultiplier
            : playAgainOriginalScale;

        Color targetColor = hovered
            ? buttonHoverColor
            : playAgainOriginalColor;

        playAgainScaleTween = playAgainButtonTransform
            .DOScale(targetScale, buttonHoverDuration)
            .SetEase(buttonHoverEase)
            .SetUpdate(true);

        playAgainColorTween = playAgainButtonWhiteImage
            .DOColor(targetColor, buttonHoverDuration)
            .SetEase(buttonHoverEase)
            .SetUpdate(true);
    }

    private void AnimateMainMenuButton(bool hovered)
    {
        mainMenuScaleTween?.Kill();
        mainMenuColorTween?.Kill();

        Vector3 targetScale = hovered
            ? mainMenuOriginalScale * buttonHoverScaleMultiplier
            : mainMenuOriginalScale;

        Color targetColor = hovered
            ? buttonHoverColor
            : mainMenuOriginalColor;

        mainMenuScaleTween = mainMenuButtonTransform
            .DOScale(targetScale, buttonHoverDuration)
            .SetEase(buttonHoverEase)
            .SetUpdate(true);

        mainMenuColorTween = mainMenuButtonWhiteImage
            .DOColor(targetColor, buttonHoverDuration)
            .SetEase(buttonHoverEase)
            .SetUpdate(true);
    }

    private IEnumerator PlayCutscene()
    {
        string winner = useDebugWinner
            ? debugWinner
            : SessionResults.Winner;

        VideoClip selectedClip = GetWinnerClip(winner);

        if (selectedClip == null)
        {
            Debug.LogError(
                $"GAME END CUTSCENE | No video assigned for winner: {winner}",
                this
            );

            yield break;
        }

        videoPlayer.clip = selectedClip;

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.Play();
    
        // Wait until the first video frame is available.
        while (videoPlayer.frame < 0)
            yield return null;

        // Reveal the Game End scene only after video playback is ready.
        SceneTransitionController transition =
            SceneTransitionController.Instance;

        if (transition != null)
        {
            transition.SetAlphaInstant(1f);
            transition.FadeIn();
        }
        else
        {
            Debug.LogWarning(
                "GAME END CUTSCENE | SceneTransitionController not found.",
                this
            );
        }

        yield return new WaitForSecondsRealtime(
            delayBeforeWinGraphic
        );

        // Show WIN graphic.
        yield return PlayWinGraphicAnimation();

        yield return new WaitForSecondsRealtime(
            winGraphicDisplayDuration
        );

        // Dim the video and hide the WIN graphic.
        Sequence dimSequence = DOTween.Sequence()
            .SetUpdate(true);

        dimSequence.Join(
            backgroundGroup.DOFade(
                1f,
                backgroundFadeDuration
            )
        );

        dimSequence.Join(
            winGraphicGroup.DOFade(
                0f,
                backgroundFadeDuration
            )
        );

        yield return dimSequence.WaitForCompletion();

        if (resultRevealDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                resultRevealDelay
            );
        }

        // Reveal the existing result UI.
        resultTransition.ShowResults();

        resultButtonsReady = true;

        Debug.Log(
            $"GAME END CUTSCENE | Finished. Winner: {winner}",
            this
        );
    }

    private IEnumerator PlayWinGraphicAnimation()
    {
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true);

        // Stage 1: Slow anticipation.
        sequence.Append(
            winGraphicTransform.DOScale(
                originalWinScale * winPrePopupScale,
                winSlowDuration
            ).SetEase(winSlowEase)
        );

        // Stage 2: Fast popup — shrink from large to small.
        sequence.Append(
            winGraphicTransform.DOScale(
                originalWinScale * winStartScale,
                winPopupDuration
            ).SetEase(Ease.InCubic)
        );

        // Stage 3: Settle to normal size.
        sequence.Append(
            winGraphicTransform.DOScale(
                originalWinScale,
                winSettleDuration
            ).SetEase(winSettleEase)
        );

        // Fade in while the scale animation plays.
        winGraphicGroup
            .DOFade(1f, winGraphicFadeInDuration)
            .SetUpdate(true);

        yield return sequence.WaitForCompletion();
    }

    private VideoClip GetWinnerClip(string winner)
    {
        if (string.IsNullOrWhiteSpace(winner))
            return null;

        if (winner.IndexOf(
            playerWinnerIdentifier,
            System.StringComparison.OrdinalIgnoreCase
        ) >= 0)
        {
            return playerWinClip;
        }

        if (winner.IndexOf(
            aiWinnerIdentifier,
            System.StringComparison.OrdinalIgnoreCase
        ) >= 0)
        {
            return aiWinClip;
        }

        Debug.LogError(
            $"GAME END CUTSCENE | Cannot identify winner: {winner}",
            this
        );

        return null;
    }

    private void OnDisable()
    {
        DOTween.Kill(winGraphicGroup);
        DOTween.Kill(backgroundGroup);

        playAgainScaleTween?.Kill();
        playAgainColorTween?.Kill();

        mainMenuScaleTween?.Kill();
        mainMenuColorTween?.Kill();
    }
}
