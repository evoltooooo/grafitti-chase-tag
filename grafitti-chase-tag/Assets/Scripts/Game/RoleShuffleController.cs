using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using DG.Tweening;

public class RoleShuffleController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private Image chaserTitle;
    [SerializeField] private Image evaderTitle;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject gameplayUIRoot;

    [Header("Camera Lock")]
    [SerializeField] private CinemachineInputAxisController cameraInput;

    [Header("Overall Timing")]
    [SerializeField, Min(0f)] private float totalDuration = 3f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float revealHoldDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

    [Header("Shuffle Acceleration")]
    [SerializeField, Min(0.01f)]
    private float initialShuffleInterval = 0.28f;

    [SerializeField, Min(0.01f)]
    private float finalShuffleInterval = 0.055f;

    [SerializeField, Min(0.1f)]
    private float shuffleAccelerationPower = 1.5f;

    [Header("Final Role Reveal")]
    [SerializeField, Min(0f)]
    private float revealShrinkDuration = 0.18f;

    [SerializeField, Min(0f)]
    private float revealPopupDuration = 0.22f;

    [SerializeField, Min(0f)]
    private float revealSettleDuration = 0.10f;

    [SerializeField, Min(1f)]
    private float revealInitialScale = 1.35f;

    [SerializeField, Range(0.1f, 1f)]
    private float revealSmallScale = 0.65f;

    [SerializeField]
    private Ease revealShrinkEase = Ease.InCubic;

    [SerializeField]
    private Ease revealPopupEase = Ease.OutBack;

    [SerializeField]
    private Ease revealSettleEase = Ease.OutCubic;

    [Header("Overlay Easing")]
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;

    private Sequence shuffleSequence;
    private bool isPlaying;

    private Vector3 chaserOriginalScale;
    private Vector3 evaderOriginalScale;

    private void Awake()
    {
        if (overlayGroup == null)
            overlayGroup = GetComponent<CanvasGroup>();

        if (overlayGroup == null)
        {
            Debug.LogError(
                "ROLE SHUFFLE | CanvasGroup reference is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (chaserTitle != null)
            chaserOriginalScale = chaserTitle.transform.localScale;

        if (evaderTitle != null)
            evaderOriginalScale = evaderTitle.transform.localScale;

        SetVisible(false);
    }

    public void Play(
        CharacterRoleType playerRole,
        Action onComplete)
    {
        KillShuffleSequence();

        isPlaying = true;

        GameplayInputLock.SetLocked(true);

        if (cameraInput != null)
            cameraInput.enabled = false;

        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(false);

        SetVisible(true);

        overlayGroup.alpha = 0f;

        if (infoText != null)
            infoText.text = "This round you are...";

        ResetTitleScales();
        SetTitle(null);

        // Calculate the available shuffle time.
        float shuffleDuration = Mathf.Max(
            0f,
            totalDuration -
            fadeInDuration -
            revealShrinkDuration -
            revealPopupDuration -
            revealSettleDuration -
            revealHoldDuration -
            fadeOutDuration
        );

        Image selectedTitleImage =
            playerRole == CharacterRoleType.Chaser
                ? chaserTitle
                : evaderTitle;

        Vector3 originalTitleScale =
            selectedTitleImage != null
                ? selectedTitleImage.transform.localScale
                : Vector3.one;

        // Initialize the animation sequence.
        shuffleSequence = DOTween.Sequence();

        // Fade the role shuffle overlay in.
        if (fadeInDuration > 0f)
        {
            shuffleSequence.Append(
                overlayGroup.DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase)
            );
        }
        else
        {
            shuffleSequence.AppendCallback(() =>
            {
                overlayGroup.alpha = 1f;
            });
        }

        // Progressively accelerate role switching.
        BuildAcceleratingShuffle(shuffleDuration);

        // Reveal the actual player role.
        shuffleSequence.AppendCallback(() =>
        {
            SetTitle(playerRole);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayRoleReveal();

            if (selectedTitleImage != null)
            {
                selectedTitleImage.transform.localScale =
                    originalTitleScale * revealInitialScale;
            }
        });

        // Shrink from oversized to a smaller scale.
        if (selectedTitleImage != null && revealShrinkDuration > 0f)
        {
            shuffleSequence.Append(
                selectedTitleImage.transform.DOScale(
                    originalTitleScale * revealSmallScale,
                    revealShrinkDuration
                ).SetEase(revealShrinkEase)
            );
        }

        // Pop back toward the original scale.
        if (selectedTitleImage != null && revealPopupDuration > 0f)
        {
            shuffleSequence.Append(
                selectedTitleImage.transform.DOScale(
                    originalTitleScale,
                    revealPopupDuration
                ).SetEase(revealPopupEase)
            );
        }

        // Hold the final role.
        if (revealHoldDuration > 0f)
            shuffleSequence.AppendInterval(revealHoldDuration);

        // Fade overlay out.
        if (fadeOutDuration > 0f)
        {
            shuffleSequence.Append(
                overlayGroup.DOFade(0f, fadeOutDuration)
                    .SetEase(fadeOutEase)
            );
        }
        else
        {
            shuffleSequence.AppendCallback(() =>
            {
                overlayGroup.alpha = 0f;
            });
        }

        // Restore gameplay after the complete sequence.
        shuffleSequence.OnComplete(() =>
        {
            isPlaying = false;

            RestoreGameplayState();

            shuffleSequence = null;

            onComplete?.Invoke();
        });
    }

    private void BuildAcceleratingShuffle(float duration)
    {
        if (duration <= 0f)
            return;

        float elapsed = 0f;

        // Local variable can safely be captured by DOTween callbacks.
        bool showChaser = true;

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);

            float acceleratedProgress = Mathf.Pow(
                progress,
                shuffleAccelerationPower
            );

            float currentInterval = Mathf.Lerp(
                initialShuffleInterval,
                finalShuffleInterval,
                acceleratedProgress
            );

            float stepDuration = Mathf.Min(
                currentInterval,
                duration - elapsed
            );

            shuffleSequence.AppendInterval(stepDuration);

            elapsed += stepDuration;

            if (elapsed < duration - 0.001f)
            {
                shuffleSequence.AppendCallback(() =>
                {
                    showChaser = !showChaser;

                    SetTitle(
                        showChaser
                            ? CharacterRoleType.Chaser
                            : CharacterRoleType.Evader
                    );

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayRoleShuffleTick();
                });
            }
        }
    }

    private void SetTitle(CharacterRoleType? role)
    {
        if (chaserTitle != null)
        {
            chaserTitle.gameObject.SetActive(
                role == CharacterRoleType.Chaser
            );
        }

        if (evaderTitle != null)
        {
            evaderTitle.gameObject.SetActive(
                role == CharacterRoleType.Evader
            );
        }
    }

    private void ResetTitleScales()
    {
        if (chaserTitle != null)
            chaserTitle.transform.localScale = chaserOriginalScale;

        if (evaderTitle != null)
            evaderTitle.transform.localScale = evaderOriginalScale;
    }

    private void SetVisible(bool visible)
    {
        if (overlayGroup == null)
            return;

        overlayGroup.alpha = visible ? 1f : 0f;
        overlayGroup.interactable = visible;
        overlayGroup.blocksRaycasts = visible;
    }

    private void RestoreGameplayState()
    {
        SetVisible(false);

        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(true);

        if (cameraInput != null)
            cameraInput.enabled = true;

        GameplayInputLock.SetLocked(false);
    }

    private void KillShuffleSequence()
    {
        if (shuffleSequence != null && shuffleSequence.IsActive())
            shuffleSequence.Kill();

        shuffleSequence = null;
    }

    private void OnDisable()
    {
        KillShuffleSequence();

        if (isPlaying)
        {
            isPlaying = false;
            RestoreGameplayState();
        }
    }
}
