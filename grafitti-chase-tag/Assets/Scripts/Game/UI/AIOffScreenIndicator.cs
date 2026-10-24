
using UnityEngine;

public class AIOffscreenIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform aiTarget;

    [Header("Arrow Images")]
    [SerializeField] private RectTransform upArrow;
    [SerializeField] private RectTransform downArrow;
    [SerializeField] private RectTransform leftArrow;
    [SerializeField] private RectTransform rightArrow;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float screenEdgePadding = 70f;
    [SerializeField, Min(0f)] private float targetHeightOffset = 1f;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        HideAllArrows();
    }

    private void LateUpdate()
    {
        if (targetCamera == null || aiTarget == null)
        {
            HideAllArrows();
            return;
        }

        Vector3 targetPosition =
            aiTarget.position + Vector3.up * targetHeightOffset;

        Vector3 screenPosition =
            targetCamera.WorldToScreenPoint(targetPosition);

        bool behindCamera = screenPosition.z <= 0f;

        if (behindCamera)
        {
            screenPosition.x = Screen.width - screenPosition.x;
            screenPosition.y = Screen.height - screenPosition.y;
        }

        bool insideScreen =
            !behindCamera &&
            screenPosition.x >= 0f &&
            screenPosition.x <= Screen.width &&
            screenPosition.y >= 0f &&
            screenPosition.y <= Screen.height;

        if (insideScreen)
        {
            HideAllArrows();
            return;
        }

        Vector2 screenCenter =
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector2 direction =
            new Vector2(screenPosition.x, screenPosition.y) -
            screenCenter;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.up;

        direction.Normalize();

        float halfWidth =
            Mathf.Max(1f, Screen.width * 0.5f - screenEdgePadding);

        float halfHeight =
            Mathf.Max(1f, Screen.height * 0.5f - screenEdgePadding);

        // Determine which edge the indicator should use.
        float horizontalRatio =
            Mathf.Abs(direction.x) / halfWidth;

        float verticalRatio =
            Mathf.Abs(direction.y) / halfHeight;

        RectTransform selectedArrow;

        if (horizontalRatio > verticalRatio)
        {
            // Left or right edge.
            selectedArrow = direction.x < 0f
                ? leftArrow
                : rightArrow;
        }
        else
        {
            // Top or bottom edge.
            selectedArrow = direction.y < 0f
                ? downArrow
                : upArrow;
        }

        float scale = Mathf.Min(
            halfWidth / Mathf.Max(Mathf.Abs(direction.x), 0.001f),
            halfHeight / Mathf.Max(Mathf.Abs(direction.y), 0.001f)
        );

        Vector2 indicatorPosition =
            screenCenter + direction * scale;

        HideAllArrows();

        if (selectedArrow == null)
            return;

        selectedArrow.position = indicatorPosition;

        // No rotation: each arrow image keeps its original orientation.
        selectedArrow.rotation = Quaternion.identity;

        selectedArrow.gameObject.SetActive(true);
    }

    private void HideAllArrows()
    {
        SetArrowActive(upArrow, false);
        SetArrowActive(downArrow, false);
        SetArrowActive(leftArrow, false);
        SetArrowActive(rightArrow, false);
    }

    private void SetArrowActive(
        RectTransform arrow,
        bool active)
    {
        if (arrow != null && arrow.gameObject.activeSelf != active)
            arrow.gameObject.SetActive(active);
    }
}
