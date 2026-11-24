using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GuideDisplayController : MonoBehaviour
{
    [Header("Guide Display")]
    [SerializeField] private GameObject guideDisplay;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup windowGroup;
    [SerializeField] private RectTransform windowTransform;
    [SerializeField] private RectTransform pageContent;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private TMP_Text pageIndicator;

    [Header("Guide Pages")]
    [SerializeField] private List<GameObject> pagePrefabs = new();

    [Header("Background Fade")]
    [SerializeField, Range(0f, 1f)]
    private float backgroundTargetAlpha = 0.75f;

    [SerializeField, Min(0f)]
    private float backgroundFadeInDuration = 0.3f;

    [SerializeField, Min(0f)]
    private float backgroundFadeOutDuration = 0.25f;

    [SerializeField]
    private Ease backgroundFadeEase = Ease.OutQuad;

    [Header("Window Popup")]
    [SerializeField, Range(0.1f, 1f)]
    private float windowStartScale = 0.85f;

    [SerializeField, Min(0f)]
    private float windowOpenDuration = 0.4f;

    [SerializeField, Min(0f)]
    private float windowCloseDuration = 0.25f;

    [SerializeField]
    private Ease windowOpenEase = Ease.OutBack;

    [SerializeField]
    private Ease windowCloseEase = Ease.InBack;

    [Header("Page Transition")]
    [SerializeField, Min(0f)]
    private float pageFadeInDuration = 0.2f;

    [SerializeField, Min(0f)]
    private float pageFadeOutDuration = 0.15f;

    [SerializeField]
    private Ease pageFadeEase = Ease.OutQuad;

    private GameObject currentPageInstance;
    private CanvasGroup currentPageGroup;

    private Vector3 windowOriginalScale;
    private int currentPageIndex;

    private Sequence displaySequence;
    private Sequence pageSequence;

    private bool isAnimating;
    private bool isPageTransitioning;

    private void Awake()
    {
        if (windowTransform != null)
            windowOriginalScale = windowTransform.localScale;

        if (guideDisplay != null)
            guideDisplay.SetActive(false);
    }

    public void OpenGuide()
    {
        if (isAnimating || guideDisplay == null)
            return;

        if (pagePrefabs.Count == 0)
        {
            Debug.LogWarning("GUIDE | No page prefabs assigned.", this);
            return;
        }

        isAnimating = true;
        currentPageIndex = 0;

        guideDisplay.SetActive(true);

        backgroundGroup.alpha = 0f;
        backgroundGroup.interactable = true;
        backgroundGroup.blocksRaycasts = true;

        windowGroup.alpha = 0f;
        windowGroup.interactable = false;
        windowGroup.blocksRaycasts = true;

        windowTransform.localScale =
            windowOriginalScale * windowStartScale;

        CreatePageInstance(currentPageIndex, false);

        displaySequence?.Kill();

        displaySequence = DOTween.Sequence();

        // Fade in the dimmed background.
        displaySequence.Join(
            backgroundGroup.DOFade(
                backgroundTargetAlpha,
                backgroundFadeInDuration
            ).SetEase(backgroundFadeEase)
        );

        // Fade and pop the guide window.
        displaySequence.Join(
            windowGroup.DOFade(1f, windowOpenDuration)
        );

        displaySequence.Join(
            windowTransform.DOScale(
                windowOriginalScale,
                windowOpenDuration
            ).SetEase(windowOpenEase)
        );

        displaySequence.OnComplete(() =>
        {
            isAnimating = false;
            windowGroup.interactable = true;
            UpdateNavigation();
        });
    }

    public void CloseGuide()
    {
        if (isAnimating || guideDisplay == null ||
            !guideDisplay.activeSelf)
            return;

        isAnimating = true;

        windowGroup.interactable = false;
        backgroundGroup.interactable = false;

        displaySequence?.Kill();

        displaySequence = DOTween.Sequence();

        // Fade out the dimming background.
        displaySequence.Join(
            backgroundGroup.DOFade(
                0f,
                backgroundFadeOutDuration
            ).SetEase(backgroundFadeEase)
        );

        // Fade and shrink the guide window.
        displaySequence.Join(
            windowGroup.DOFade(
                0f,
                windowCloseDuration
            )
        );

        displaySequence.Join(
            windowTransform.DOScale(
                windowOriginalScale * windowStartScale,
                windowCloseDuration
            ).SetEase(windowCloseEase)
        );

        displaySequence.OnComplete(() =>
        {
            // Remove the current page when closing the guide.
            if (currentPageInstance != null)
            {
                Destroy(currentPageInstance);
                currentPageInstance = null;
                currentPageGroup = null;
            }

            // Reset page navigation for the next opening.
            currentPageIndex = 0;

            guideDisplay.SetActive(false);

            windowTransform.localScale = windowOriginalScale;

            isAnimating = false;
        });
    }

    public void NextPage()
    {
        if (isAnimating || isPageTransitioning ||
            currentPageIndex >= pagePrefabs.Count - 1)
            return;

        currentPageIndex++;
        ShowCurrentPage();
    }

    public void PreviousPage()
    {
        if (isAnimating || isPageTransitioning ||
            currentPageIndex <= 0)
            return;

        currentPageIndex--;
        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        isPageTransitioning = true;
        UpdateNavigation();

        pageSequence?.Kill();

        pageSequence = DOTween.Sequence();

        if (currentPageGroup != null)
        {
            pageSequence.Append(
                currentPageGroup.DOFade(
                    0f,
                    pageFadeOutDuration
                ).SetEase(pageFadeEase)
            );
        }

        pageSequence.OnComplete(() =>
        {
            if (currentPageInstance != null)
                Destroy(currentPageInstance);

            CreatePageInstance(currentPageIndex, true);
        });
    }

    private void CreatePageInstance(int index, bool animate)
    {
        if (pageContent == null ||
            index < 0 || index >= pagePrefabs.Count ||
            pagePrefabs[index] == null)
        {
            Debug.LogError(
                $"GUIDE | Invalid page prefab at index {index}.",
                this
            );

            isPageTransitioning = false;
            return;
        }

        currentPageInstance = Instantiate(
            pagePrefabs[index],
            pageContent,
            false
        );

        currentPageGroup =
            currentPageInstance.GetComponent<CanvasGroup>();

        if (currentPageGroup == null)
            currentPageGroup =
                currentPageInstance.AddComponent<CanvasGroup>();

        currentPageGroup.alpha = animate ? 0f : 1f;
        currentPageGroup.interactable = true;
        currentPageGroup.blocksRaycasts = true;

        UpdateNavigation();

        if (!animate)
        {
            isPageTransitioning = false;
            return;
        }

        currentPageGroup.DOFade(1f, pageFadeInDuration)
            .SetEase(pageFadeEase)
            .OnComplete(() =>
            {
                isPageTransitioning = false;
                UpdateNavigation();
            });
    }

    private void UpdateNavigation()
    {
        bool canNavigate = !isAnimating && !isPageTransitioning;

        if (prevButton != null)
            prevButton.interactable =
                canNavigate && currentPageIndex > 0;

        if (nextButton != null)
            nextButton.interactable =
                canNavigate &&
                currentPageIndex < pagePrefabs.Count - 1;

        if (pageIndicator != null && pagePrefabs.Count > 0)
        {
            pageIndicator.text =
                $"{currentPageIndex + 1}/{pagePrefabs.Count}";
        }
    }

    private void OnDestroy()
    {
        displaySequence?.Kill();
        pageSequence?.Kill();
    }
}
