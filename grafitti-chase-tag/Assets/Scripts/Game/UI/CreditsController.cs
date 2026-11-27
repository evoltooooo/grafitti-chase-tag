using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CreditsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform creditsContent;
    [SerializeField] private CanvasGroup returnButtonGroup;

    [Header("Credits Scrolling")]
    [SerializeField] private bool autoScroll = true;

    [Min(0f)]
    [SerializeField] private float scrollSpeed = 35f;

    [Min(0f)]
    [SerializeField] private float scrollDistance = 900f;

    [SerializeField] private bool useUnscaledTime = true;

    [Header("Return Button")]
    [SerializeField] private bool hideButtonOnStart = true;

    [Min(0f)]
    [SerializeField] private float buttonFadeDuration = 0.25f;

    [Min(0f)]
    [SerializeField] private float buttonVisibleDuration = 4f;

    [SerializeField] private Ease buttonFadeEase = Ease.OutSine;

    private InputManager inputManager;

    private Vector2 creditsStartPosition;
    private float scrollProgress;

    private Tween buttonTween;
    private Tween hideButtonTween;

    private void Awake()
    {
        if (creditsContent == null || returnButtonGroup == null)
        {
            Debug.LogError(
                "CREDITS | Credits Content or Return Button Group is missing.",
                this
            );

            enabled = false;
            return;
        }

        creditsStartPosition = creditsContent.anchoredPosition;

        SetReturnButtonVisible(!hideButtonOnStart);
    }

    private void OnEnable()
    {
        inputManager = InputManager.Instance;

        if (inputManager == null)
        {
            Debug.LogError(
                "CREDITS | Persistent InputManager instance not found. " +
                "Make sure the scene is launched through Bootloader.",
                this
            );

            return;
        }

        inputManager.SkipIntroPressed += OnSkipPressed;

        ResetCredits();
    }

    private void OnDisable()
    {
        if (inputManager != null)
            inputManager.SkipIntroPressed -= OnSkipPressed;

        inputManager = null;

        KillButtonTweens();
    }

    private void Update()
    {
        UpdateCreditsScroll();
    }

    private void ResetCredits()
    {
        scrollProgress = 0f;
        creditsContent.anchoredPosition = creditsStartPosition;

        KillButtonTweens();
        SetReturnButtonVisible(!hideButtonOnStart);
    }

    private void UpdateCreditsScroll()
    {
        if (!autoScroll || scrollProgress >= scrollDistance)
            return;

        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        scrollProgress = Mathf.Min(
            scrollProgress + scrollSpeed * deltaTime,
            scrollDistance
        );

        creditsContent.anchoredPosition =
            creditsStartPosition +
            Vector2.up * scrollProgress;
    }

    private void OnSkipPressed()
    {
        ShowReturnButton();
    }

    private void ShowReturnButton()
    {
        KillButtonTweens();

        returnButtonGroup.interactable = true;
        returnButtonGroup.blocksRaycasts = true;

        buttonTween = returnButtonGroup
            .DOFade(1f, buttonFadeDuration)
            .SetEase(buttonFadeEase)
            .SetUpdate(true);

        hideButtonTween = DOVirtual.DelayedCall(
            buttonVisibleDuration,
            HideReturnButton
        ).SetUpdate(true);
    }

    private void HideReturnButton()
    {
        buttonTween?.Kill();

        buttonTween = returnButtonGroup
            .DOFade(0f, buttonFadeDuration)
            .SetEase(buttonFadeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                returnButtonGroup.interactable = false;
                returnButtonGroup.blocksRaycasts = false;
            });
    }

    private void SetReturnButtonVisible(bool visible)
    {
        returnButtonGroup.alpha = visible ? 1f : 0f;
        returnButtonGroup.interactable = visible;
        returnButtonGroup.blocksRaycasts = visible;
    }

    private void KillButtonTweens()
    {
        buttonTween?.Kill();
        hideButtonTween?.Kill();

        buttonTween = null;
        hideButtonTween = null;
    }

    private void OnDestroy()
    {
        KillButtonTweens();
    }
}
