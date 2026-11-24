using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class MainMenuButtonAnimator : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private RectTransform[] buttonTransforms = new RectTransform[3];
    [SerializeField] private UnityEngine.UI.Image[] buttonWhiteImages = new UnityEngine.UI.Image[3];

    [Header("Group Movement")]
    [SerializeField] private float pushDistance = 18f;

    [Header("Animation Settings")]
    [SerializeField] private float hoverDuration = 0.25f;
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private Ease movementEase = Ease.OutQuad;

    [Header("Modal UI")]
    [SerializeField] private GameObject guideDisplay;

    private Vector2[] originalPositions;

    private Canvas rootCanvas;
    private Camera uiCamera;

    private int currentHoveredIndex = -1;

    private void Awake()
    {
        if (buttonTransforms == null ||
            buttonWhiteImages == null ||
            buttonTransforms.Length != 3 ||
            buttonWhiteImages.Length != 3)
        {
            Debug.LogError(
                "MAIN MENU BUTTON ANIMATOR | Exactly 3 button references are required.",
                this
            );

            enabled = false;
            return;
        }

        originalPositions = new Vector2[3];

        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogError(
                "MAIN MENU BUTTON ANIMATOR | No parent Canvas found.",
                this
            );

            enabled = false;
            return;
        }

        rootCanvas = parentCanvas.rootCanvas;

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
        // Ignore Main Menu hover detection while the guide is open.
        if (guideDisplay != null && guideDisplay.activeInHierarchy)
            return -1;

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
            Vector2 targetPosition = originalPositions[i];

            // Push the other buttons away from the hovered button.
            if (hoveredIndex >= 0 && i != hoveredIndex)
            {
                targetPosition.y +=
                    i < hoveredIndex
                        ? pushDistance
                        : -pushDistance;
            }

            buttonTransforms[i].DOKill();

            buttonTransforms[i]
                .DOAnchorPos(targetPosition, duration)
                .SetEase(movementEase)
                .SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        currentHoveredIndex = -1;

        if (buttonTransforms == null)
            return;

        for (int i = 0; i < buttonTransforms.Length; i++)
        {
            if (buttonTransforms[i] == null)
                continue;

            buttonTransforms[i].DOKill();

            if (originalPositions != null &&
                i < originalPositions.Length)
            {
                buttonTransforms[i].anchoredPosition =
                    originalPositions[i];
            }
        }
    }
}
