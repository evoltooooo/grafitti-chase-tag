using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public class MainMenuButtonAnimator : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private RectTransform[] buttonTransforms = new RectTransform[3];
    [SerializeField] private Image[] buttonWhiteImages = new Image[3];

    [Header("Hover Settings")]
    [SerializeField] private float hoverScaleMultiplier = 1.12f;
    [SerializeField] private float pushDistance = 18f;
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("Animation Settings")]
    [SerializeField] private float hoverDuration = 0.25f;
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private Ease hoverEase = Ease.OutBack;
    [SerializeField] private Ease movementEase = Ease.OutQuad;

    private Vector2[] originalPositions;
    private Vector3[] originalScales;
    private Color[] originalColors;

    private Canvas rootCanvas;
    private Camera uiCamera;

    private int currentHoveredIndex = -1;

    private void Awake()
    {
        if (buttonTransforms.Length != 3 ||
            buttonWhiteImages.Length != 3)
        {
            Debug.LogError(
                "MAIN MENU BUTTON ANIMATOR | Exactly 3 buttons are required.",
                this
            );

            enabled = false;
            return;
        }

        originalPositions = new Vector2[3];
        originalScales = new Vector3[3];
        originalColors = new Color[3];

        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera;

        for (int i = 0; i < 3; i++)
        {
            if (buttonTransforms[i] == null ||
                buttonWhiteImages[i] == null)
            {
                Debug.LogError(
                    $"MAIN MENU BUTTON ANIMATOR | Missing reference at index {i}.",
                    this
                );

                enabled = false;
                return;
            }

            originalPositions[i] =
                buttonTransforms[i].anchoredPosition;

            originalScales[i] =
                buttonTransforms[i].localScale;

            originalColors[i] =
                buttonWhiteImages[i].color;
        }
    }

    private void Update()
    {
        int hoveredIndex = GetHoveredButtonIndex();

        if (hoveredIndex != currentHoveredIndex)
        {
            SetHoveredButton(hoveredIndex);
        }
    }

    private int GetHoveredButtonIndex()
    {
        if (Mouse.current == null)
            return -1;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();

        // Check the visible white button surfaces.
        for (int i = 0; i < 3; i++)
        {
            RectTransform hitArea =
                buttonWhiteImages[i].rectTransform;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                hitArea,
                pointerPosition,
                uiCamera))
            {
                return i;
            }
        }

        return -1;
    }

    private void SetHoveredButton(int hoveredIndex)
    {
        currentHoveredIndex = hoveredIndex;

        float duration = hoveredIndex >= 0
            ? hoverDuration
            : returnDuration;

        for (int i = 0; i < 3; i++)
        {
            bool isHovered = i == hoveredIndex;

            Vector2 targetPosition = originalPositions[i];

            if (hoveredIndex >= 0 && !isHovered)
            {
                targetPosition.y +=
                    i < hoveredIndex
                        ? pushDistance
                        : -pushDistance;
            }

            Vector3 targetScale = originalScales[i];

            if (isHovered)
                targetScale *= hoverScaleMultiplier;

            buttonTransforms[i].DOKill();
            buttonWhiteImages[i].DOKill();

            buttonTransforms[i]
                .DOAnchorPos(targetPosition, duration)
                .SetEase(movementEase);

            buttonTransforms[i]
                .DOScale(targetScale, duration)
                .SetEase(isHovered ? hoverEase : movementEase);

            buttonWhiteImages[i]
                .DOColor(
                    isHovered ? hoverColor : originalColors[i],
                    duration
                )
                .SetEase(movementEase);
        }
    }

    private void OnDisable()
    {
        if (buttonTransforms != null)
        {
            foreach (RectTransform button in buttonTransforms)
            {
                if (button != null)
                    button.DOKill();
            }
        }

        if (buttonWhiteImages != null)
        {
            foreach (Image image in buttonWhiteImages)
            {
                if (image != null)
                    image.DOKill();
            }
        }
    }
}
