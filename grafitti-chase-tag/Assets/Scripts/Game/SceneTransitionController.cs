using UnityEngine;
using DG.Tweening;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class SceneTransitionController : MonoBehaviour
{
    public static SceneTransitionController Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.6f;

    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;

    [Header("Overlay Settings")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private bool blockRaycastsDuringFade = true;

    private Tween fadeTween;
    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (overlayGroup == null)
            overlayGroup = GetComponent<CanvasGroup>();

        overlayGroup.alpha = 1f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = true;
    }

    public void FadeOut(Action onComplete = null)
    {
        StartFade(
            1f,
            fadeOutDuration,
            fadeOutEase,
            onComplete
        );
    }

    public void FadeIn(Action onComplete = null)
    {
        StartFade(
            0f,
            fadeInDuration,
            fadeInEase,
            onComplete
        );
    }

    private void StartFade(
        float targetAlpha,
        float duration,
        Ease ease,
        Action onComplete)
    {
        fadeTween?.Kill();

        isTransitioning = true;

        overlayGroup.blocksRaycasts = blockRaycastsDuringFade;
        overlayGroup.interactable = false;

        fadeTween = overlayGroup
            .DOFade(targetAlpha, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                isTransitioning = false;

                overlayGroup.blocksRaycasts = targetAlpha >= 1f;

                onComplete?.Invoke();
            });
    }

    public void SetAlphaInstant(float alpha)
    {
        fadeTween?.Kill();

        isTransitioning = false;

        overlayGroup.alpha = Mathf.Clamp01(alpha);
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = alpha >= 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            fadeTween?.Kill();
            Instance = null;
        }
    }
}
