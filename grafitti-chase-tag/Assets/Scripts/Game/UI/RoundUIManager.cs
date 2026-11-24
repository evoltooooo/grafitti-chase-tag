using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class RoundUIManager : MonoBehaviour
{
    [Serializable]
    private class ImageAnimationSettings
    {
        [Header("Popup")]
        public bool enablePopup = true;

        [Min(0f)]
        public float popupDuration = 0.35f;

        [Range(0.1f, 2f)]
        public float startScale = 0.5f;

        [Min(1f)]
        public float overshootScale = 1.15f;

        public Ease popupEase = Ease.OutBack;

        [Header("Fade In")]
        public bool enableFadeIn = true;

        [Min(0f)]
        public float fadeInDuration = 0.2f;

        public Ease fadeInEase = Ease.OutQuad;

        [Header("Fade Out")]
        public bool enableFadeOut = true;

        [Min(0f)]
        public float fadeOutDuration = 0.2f;

        public Ease fadeOutEase = Ease.InQuad;
    }

    [Header("References")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text aiScoreText;
    [SerializeField] private GameObject taggedDisplayRoot;
    [SerializeField] private TMP_Text roundResultText;

    [Header("Tagged / Evaded Image")]
    [SerializeField] private Image taggedImage;
    [SerializeField] private Sprite taggedSprite;
    [SerializeField] private Sprite evadedSprite;

    [SerializeField] private ImageAnimationSettings taggedAnimation =
        new ImageAnimationSettings();

    [Header("Countdown Image")]
    [SerializeField] private Image countdownImage;
    [SerializeField] private Sprite countdown3Sprite;
    [SerializeField] private Sprite countdown2Sprite;
    [SerializeField] private Sprite countdown1Sprite;

    [SerializeField] private ImageAnimationSettings countdownAnimation =
        new ImageAnimationSettings();

    [Header("Character Role Images")]
    [SerializeField] private CharacterRole playerRole;
    [SerializeField] private CharacterRole aiRole;

    [SerializeField] private Image playerRoleImage;
    [SerializeField] private Image aiRoleImage;

    [Header("Role Sprites")]
    [SerializeField] private Sprite chaserSprite;
    [SerializeField] private Sprite evaderSprite;

    private Sequence taggedSequence;
    private Sequence countdownSequence;
    private Sprite currentCountdownSprite;

    private void OnEnable()
    {
        if (roundManager == null)
        {
            Debug.LogError(
                "ROUND UI | RoundManager is missing.",
                this
            );
            return;
        }

        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);

        roundManager.RoundStarted += OnRoundStarted;
        roundManager.TimerUpdated += OnTimerUpdated;
        roundManager.CountdownUpdated += OnCountdownUpdated;
        roundManager.RoundEnded += OnRoundEnded;

        UpdateTimer(roundManager.TimeRemaining);
        UpdateScores();

        HideTaggedDisplay(false);
        HideCountdown(false);

        if (roundManager.CurrentState ==
            RoundManager.RoundState.Countdown)
        {
            OnCountdownUpdated(roundManager.CountdownRemaining);
        }

        if (playerRole != null)
            playerRole.RoleChanged += OnPlayerRoleChanged;

        if (aiRole != null)
            aiRole.RoleChanged += OnAIRoleChanged;

        UpdateRoleDisplays();
    }

    private void OnDisable()
    {
        if (roundManager != null)
        {
            roundManager.RoundStarted -= OnRoundStarted;
            roundManager.TimerUpdated -= OnTimerUpdated;
            roundManager.CountdownUpdated -= OnCountdownUpdated;
            roundManager.RoundEnded -= OnRoundEnded;
        }

        if (playerRole != null)
            playerRole.RoleChanged -= OnPlayerRoleChanged;

        if (aiRole != null)
            aiRole.RoleChanged -= OnAIRoleChanged;

        KillSequences();
    }

    public void SetCharacterRoles(
        CharacterRole newPlayerRole,
        CharacterRole newAIRole)
    {
        if (playerRole != null)
            playerRole.RoleChanged -= OnPlayerRoleChanged;

        if (aiRole != null)
            aiRole.RoleChanged -= OnAIRoleChanged;

        playerRole = newPlayerRole;
        aiRole = newAIRole;

        if (isActiveAndEnabled)
        {
            if (playerRole != null)
                playerRole.RoleChanged += OnPlayerRoleChanged;

            if (aiRole != null)
                aiRole.RoleChanged += OnAIRoleChanged;
        }

        UpdateRoleDisplays();
    }

    private void OnRoundStarted(int roundNumber, float duration)
    {
        UpdateTimer(duration);
        UpdateScores();

        HideTaggedDisplay(true);
        HideCountdown(true);

        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);
    }

    private void OnTimerUpdated(float timeRemaining)
    {
        UpdateTimer(timeRemaining);
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(
            Mathf.Max(0f, timeRemaining)
        );

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateScores()
    {
        if (roundManager == null)
            return;

        if (playerScoreText != null)
            playerScoreText.text = roundManager.PlayerScore.ToString();

        if (aiScoreText != null)
            aiScoreText.text = roundManager.AIScore.ToString();
    }

    private void OnRoundEnded(
        int roundNumber,
        CharacterRoleType winningRole,
        int playerScore,
        int aiScore)
    {
        UpdateScores();

        HideCountdown(true);

        Sprite resultSprite =
            winningRole == CharacterRoleType.Chaser
                ? taggedSprite
                : evadedSprite;

        // Play SFX according to the result image being displayed.
        if (AudioManager.Instance != null)
        {
            if (resultSprite == taggedSprite)
                AudioManager.Instance.PlayPlayerTagged();
            else if (resultSprite == evadedSprite)
                AudioManager.Instance.PlayPlayerEvaded();
        }

        ShowTaggedImage(resultSprite);

        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);
    }

    private void OnCountdownUpdated(float timeRemaining)
    {
        HideTaggedDisplay(true);

        if (roundResultText != null)
            roundResultText.gameObject.SetActive(false);

        if (countdownImage == null || roundManager == null)
            return;

        bool isCountingDown =
            roundManager.CurrentState ==
                RoundManager.RoundState.Countdown &&
            timeRemaining > 0f;

        if (!isCountingDown)
        {
            HideCountdown(true);
            return;
        }

        int countdownNumber = Mathf.CeilToInt(timeRemaining);

        Sprite nextSprite = countdownNumber switch
        {
            3 => countdown3Sprite,
            2 => countdown2Sprite,
            1 => countdown1Sprite,
            _ => null
        };

        if (nextSprite == null)
            return;

        if (nextSprite == currentCountdownSprite &&
            countdownImage.gameObject.activeSelf)
            return;

        currentCountdownSprite = nextSprite;

        ShowImage(
            countdownImage,
            nextSprite,
            countdownAnimation,
            ref countdownSequence
        );
    }

    private void ShowTaggedImage(Sprite sprite)
    {
        if (taggedImage == null || sprite == null)
            return;

        if (taggedDisplayRoot != null)
            taggedDisplayRoot.SetActive(true);

        ShowImage(
            taggedImage,
            sprite,
            taggedAnimation,
            ref taggedSequence
        );
    }

    private void ShowImage(
        Image image,
        Sprite sprite,
        ImageAnimationSettings settings,
        ref Sequence sequence)
    {
        if (image == null)
            return;

        if (sequence != null && sequence.IsActive())
            sequence.Kill();

        Transform imageTransform = image.transform;
        Vector3 originalScale = imageTransform.localScale;

        image.sprite = sprite;
        image.gameObject.SetActive(true);

        imageTransform.DOKill();
        image.DOKill();

        imageTransform.localScale =
            settings.enablePopup
                ? originalScale * settings.startScale
                : originalScale;

        Color color = image.color;
        color.a = settings.enableFadeIn ? 0f : 1f;
        image.color = color;

        sequence = DOTween.Sequence();

        if (settings.enablePopup &&
            settings.popupDuration > 0f)
        {
            sequence.Append(
                imageTransform.DOScale(
                    originalScale * settings.overshootScale,
                    settings.popupDuration * 0.65f
                ).SetEase(settings.popupEase)
            );

            sequence.Append(
                imageTransform.DOScale(
                    originalScale,
                    settings.popupDuration * 0.35f
                ).SetEase(Ease.OutQuad)
            );
        }

        if (settings.enableFadeIn &&
            settings.fadeInDuration > 0f)
        {
            image.DOFade(
                1f,
                settings.fadeInDuration
            ).SetEase(settings.fadeInEase);
        }

        sequence.SetUpdate(false);
    }

    private void HideTaggedDisplay(bool animate)
    {
        if (taggedImage != null)
            HideImage(
                taggedImage,
                taggedAnimation,
                ref taggedSequence,
                animate
            );

        if (taggedDisplayRoot != null)
            taggedDisplayRoot.SetActive(false);
    }

    private void HideCountdown(bool animate)
    {
        currentCountdownSprite = null;

        if (countdownImage != null)
            HideImage(
                countdownImage,
                countdownAnimation,
                ref countdownSequence,
                animate
            );
    }

    private void HideImage(
        Image image,
        ImageAnimationSettings settings,
        ref Sequence sequence,
        bool animate)
    {
        if (image == null)
            return;

        if (sequence != null && sequence.IsActive())
            sequence.Kill();

        image.transform.DOKill();
        image.DOKill();

        if (!animate || !settings.enableFadeOut ||
            settings.fadeOutDuration <= 0f)
        {
            image.gameObject.SetActive(false);
            return;
        }

        sequence = DOTween.Sequence();

        sequence.Append(
            image.DOFade(
                0f,
                settings.fadeOutDuration
            ).SetEase(settings.fadeOutEase)
        );

        sequence.OnComplete(() =>
        {
            image.gameObject.SetActive(false);
            image.color = new Color(
                image.color.r,
                image.color.g,
                image.color.b,
                1f
            );
        });
    }

    private void UpdateRoleDisplays()
    {
        if (playerRole != null)
            OnPlayerRoleChanged(playerRole.CurrentRole);

        if (aiRole != null)
            OnAIRoleChanged(aiRole.CurrentRole);
    }

    private Sprite GetRoleSprite(CharacterRoleType role)
    {
        return role == CharacterRoleType.Chaser
            ? chaserSprite
            : evaderSprite;
    }

    private void OnPlayerRoleChanged(CharacterRoleType role)
    {
        if (playerRoleImage != null)
            playerRoleImage.sprite = GetRoleSprite(role);
    }

    private void OnAIRoleChanged(CharacterRoleType role)
    {
        if (aiRoleImage != null)
            aiRoleImage.sprite = GetRoleSprite(role);
    }

    private void KillSequences()
    {
        if (taggedSequence != null && taggedSequence.IsActive())
            taggedSequence.Kill();

        if (countdownSequence != null && countdownSequence.IsActive())
            countdownSequence.Kill();

        taggedSequence = null;
        countdownSequence = null;
    }
}
