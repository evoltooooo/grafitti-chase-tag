using System;
using UnityEngine;
using DG.Tweening;

public class MainMenuButtonAnimator : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private RectTransform[] buttonTransforms = new RectTransform[3];
    [SerializeField] private UIButtonHoverScale[] buttonHoverComponents = new UIButtonHoverScale[3];

    [Header("Group Movement")]
    [SerializeField] private float pushDistance = 18f;

    [Header("Animation Settings")]
    [SerializeField] private float hoverDuration = 0.25f;
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private Ease movementEase = Ease.OutQuad;

    [Header("Modal UI")]
    [SerializeField] private GameObject guideDisplay;

    private Vector2[] originalPositions;

    private Action<bool>[] hoverCallbacks;

    private int currentHoveredIndex = -1;

    private void Awake()
    {
        if (buttonTransforms == null ||
            buttonHoverComponents == null ||
            buttonTransforms.Length != 3 ||
            buttonHoverComponents.Length != 3)
        {
            Debug.LogError(
                "MAIN MENU BUTTON ANIMATOR | Exactly 3 button transforms and hover components are required.",
                this
            );

            enabled = false;
            return;
        }

        originalPositions = new Vector2[3];
        hoverCallbacks = new Action<bool>[3];

        for (int i = 0; i < 3; i++)
        {
            if (buttonTransforms[i] == null ||
                buttonHoverComponents[i] == null)
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

            int index = i;

            hoverCallbacks[i] = isHovered =>
                HandleButtonHover(index, isHovered);

            buttonHoverComponents[i].HoverStateChanged +=
                hoverCallbacks[i];
        }
    }

    private void HandleButtonHover(int index, bool isHovered)
    {
        if (!isActiveAndEnabled)
            return;

        // Ignore hover movement while the Guide Display is open.
        if (guideDisplay != null && guideDisplay.activeInHierarchy)
        {
            if (currentHoveredIndex != -1)
                SetHoveredButton(-1);

            return;
        }

        if (isHovered)
        {
            SetHoveredButton(index);
        }
        else if (currentHoveredIndex == index)
        {
            SetHoveredButton(-1);
        }
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

    private void OnDestroy()
    {
        if (buttonHoverComponents == null || hoverCallbacks == null)
            return;

        for (int i = 0; i < buttonHoverComponents.Length; i++)
        {
            if (buttonHoverComponents[i] == null ||
                hoverCallbacks[i] == null)
                continue;

            buttonHoverComponents[i].HoverStateChanged -=
                hoverCallbacks[i];
        }
    }
}
