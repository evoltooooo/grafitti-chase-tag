
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

    [Header("Timing")]
    [SerializeField, Min(0f)] private float totalDuration = 3f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float revealHoldDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float shuffleInterval = 0.12f;

    [Header("Title Pop Animation")]
    [SerializeField, Min(0f)] private float titlePopDuration = 0.1f;
    [SerializeField, Range(0.1f, 1f)] private float titlePopStartScale = 0.75f;
    [SerializeField] private Ease titlePopEase = Ease.OutCubic;

    [Header("Overlay Easing")]
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;

    private Sequence shuffleSequence;

    private bool isPlaying;

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

        SetVisible(false);
    }

    public void Play(
        CharacterRoleType playerRole,
        Action onComplete)
    {
        // Stop an existing shuffle if Play is called again.
        KillShuffleSequence();

        isPlaying = true;

        // Lock gameplay input.
        GameplayInputLock.SetLocked(true);

        if (cameraInput != null)
            cameraInput.enabled = false;

        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(false);

        SetVisible(true);

        // Start with a transparent overlay.
        overlayGroup.alpha = 0f;

        if (infoText != null)
            infoText.text = "This round you are...";

        SetTitle(null);

        // Calculate the shuffle duration.
        float shuffleDuration = Mathf.Max(
            0f,
            totalDuration -
            fadeInDuration -
            revealHoldDuration -
            fadeOutDuration
        );

        // Prepare the selected role title.
        Image selectedTitleImage =
            playerRole == CharacterRoleType.Chaser
                ? chaserTitle
                : evaderTitle;

        Transform selectedTitleTransform =
            selectedTitleImage != null
                ? selectedTitleImage.transform
                : null;

        Vector3 originalTitleScale =
            selectedTitleTransform != null
                ? selectedTitleTransform.localScale
                : Vector3.one;

        bool showChaser = true;

        shuffleSequence = DOTween.Sequence();

        // Fade overlay in.
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

        // Alternate between Chaser and Evader titles.
        int switchCount = Mathf.FloorToInt(
            shuffleDuration / shuffleInterval
        );

        for (int i = 0; i < switchCount; i++)
        {
            shuffleSequence.AppendInterval(shuffleInterval);

            shuffleSequence.AppendCallback(() =>
            {
                showChaser = !showChaser;

                SetTitle(
                    showChaser
                        ? CharacterRoleType.Chaser
                        : CharacterRoleType.Evader
                );
            });
        }

        // Preserve the remaining shuffle time.
        float remainingShuffleTime =
            shuffleDuration - (switchCount * shuffleInterval);

        if (remainingShuffleTime > 0f)
            shuffleSequence.AppendInterval(remainingShuffleTime);

        // Reveal the actual player role.
        shuffleSequence.AppendCallback(() =>
        {
            SetTitle(playerRole);

            if (selectedTitleTransform != null)
            {
                selectedTitleTransform.localScale =
                    originalTitleScale * titlePopStartScale;
            }
        });

        // Animate the selected title popping into place.
        if (selectedTitleTransform != null && titlePopDuration > 0f)
        {
            shuffleSequence.Append(
                selectedTitleTransform.DOScale(
                    originalTitleScale,
                    titlePopDuration
                ).SetEase(titlePopEase)
            );
        }
        else if (selectedTitleTransform != null)
        {
            shuffleSequence.AppendCallback(() =>
            {
                selectedTitleTransform.localScale = originalTitleScale;
            });
        }

        // Hold the revealed role.
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
