using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Scale")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;

    [Header("Hover Color")]
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("Animation")]
    [SerializeField] private float hoverDuration = 0.15f;
    [SerializeField] private float returnDuration = 0.15f;

    [Header("Easing")]
    [SerializeField] private Ease hoverEase = Ease.OutBack;
    [SerializeField] private Ease returnEase = Ease.OutCubic;

    private Button button;
    private RectTransform buttonTransform;

    private Image whiteImage;
    private Color originalColor;
    private Vector3 originalScale;

    private Tween scaleTween;
    private Tween colorTween;

    private bool hasHoverColor;
    private bool hasExternalOriginalScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonTransform = GetComponent<RectTransform>();

        if (!hasExternalOriginalScale)
            originalScale = transform.localScale;

        FindHoverImage();
    }

    private void FindHoverImage()
    {
        Image[] childImages = GetComponentsInChildren<Image>(true);

        foreach (Image image in childImages)
        {
            if (image.transform == transform)
                continue;

            if (image.name.EndsWith("ButtonWhite"))
            {
                whiteImage = image;
                hasHoverColor = true;
                break;
            }
        }

        if (whiteImage == null)
        {
            hasHoverColor = false;
            return;
        }

        originalColor = whiteImage.color;
    }

    // Allows animation controllers to provide the original design scale.
    public void SetOriginalScale(Vector3 scale)
    {
        originalScale = scale;
        hasExternalOriginalScale = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.IsInteractable())
            return;

        AnimateScale(
            originalScale * hoverScaleMultiplier,
            hoverDuration,
            hoverEase
        );

        if (hasHoverColor)
            AnimateColor(hoverColor, hoverDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReturnToOriginal();
    }

    private void ReturnToOriginal()
    {
        AnimateScale(originalScale, returnDuration, returnEase);

        if (hasHoverColor)
            AnimateColor(originalColor, returnDuration);
    }

    // Used when a popup or menu transition begins.
    public void ResetHoverImmediately()
    {
        scaleTween?.Kill();
        colorTween?.Kill();

        if (buttonTransform != null)
            buttonTransform.localScale = originalScale;

        if (hasHoverColor && whiteImage != null)
            whiteImage.color = originalColor;
    }

    private void AnimateScale(Vector3 targetScale, float duration, Ease ease)
    {
        scaleTween?.Kill();

        scaleTween = buttonTransform
            .DOScale(targetScale, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void AnimateColor(Color targetColor, float duration)
    {
        colorTween?.Kill();

        colorTween = whiteImage
            .DOColor(targetColor, duration)
            .SetEase(returnEase)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        ResetHoverImmediately();
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
    }
}
