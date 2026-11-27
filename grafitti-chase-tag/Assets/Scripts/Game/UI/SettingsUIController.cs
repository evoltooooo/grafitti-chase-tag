using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsUIController : MonoBehaviour
{
    [Header("Settings Display")]
    [SerializeField] private GameObject settingsDisplay;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup windowGroup;
    [SerializeField] private RectTransform windowTransform;

    [Header("Volume Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Camera Slider")]
    [SerializeField] private Slider cameraSensitivitySlider;

    [Header("Value Labels")]
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;
    [SerializeField] private TMP_Text cameraSensitivityValueText;

    [Header("Background Animation")]
    [SerializeField, Range(0f, 1f)]
    private float backgroundTargetAlpha = 0.75f;

    [SerializeField, Min(0f)]
    private float backgroundFadeInDuration = 0.3f;

    [SerializeField, Min(0f)]
    private float backgroundFadeOutDuration = 0.25f;

    [SerializeField]
    private Ease backgroundEase = Ease.OutQuad;

    [Header("Window Animation")]
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

    private Vector3 windowOriginalScale;

    private Sequence displaySequence;

    private bool isAnimating;

    public bool IsOpen =>
        settingsDisplay != null && settingsDisplay.activeSelf;

    private void Awake()
    {
        if (windowTransform != null)
            windowOriginalScale = windowTransform.localScale;

        if (settingsDisplay != null)
            settingsDisplay.SetActive(false);

        ConfigureSliders();
    }

    private void Start()
    {
        RefreshSliderValues();
    }

    private void ConfigureSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.minValue = 0f;
            cameraSensitivitySlider.maxValue = 1f;

            cameraSensitivitySlider.onValueChanged.AddListener(
                OnCameraSensitivityChanged
            );
        }
    }

    public void RefreshSliderValues()
    {
        if (GameSettingsManager.Instance == null)
        {
            Debug.LogWarning(
                "SETTINGS UI | GameSettingsManager not found.",
                this
            );

            return;
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(
                GameSettingsManager.Instance.BGMVolume
            );

            UpdateBGMLabel(bgmSlider.value);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(
                GameSettingsManager.Instance.SFXVolume
            );

            UpdateSFXLabel(sfxSlider.value);
        }

        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.SetValueWithoutNotify(
                GameSettingsManager.Instance.CameraSensitivity
            );

            UpdateCameraLabel(cameraSensitivitySlider.value);
        }
    }

    private void OnBGMChanged(float value)
    {
        GameSettingsManager.Instance?.SetBGMVolume(value);
        UpdateBGMLabel(value);
    }

    private void OnSFXChanged(float value)
    {
        GameSettingsManager.Instance?.SetSFXVolume(value);
        UpdateSFXLabel(value);
    }

    private void OnCameraSensitivityChanged(float value)
    {
        GameSettingsManager.Instance?.SetCameraSensitivity(value);
        UpdateCameraLabel(value);
    }

    private void UpdateBGMLabel(float value)
    {
        if (bgmValueText != null)
            bgmValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void UpdateSFXLabel(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void UpdateCameraLabel(float value)
    {
        if (cameraSensitivityValueText != null)
            cameraSensitivityValueText.text =
                $"{Mathf.RoundToInt(value * 100f)}%";
    }

    public void OpenSettings()
    {
        if (isAnimating || settingsDisplay == null)
            return;

        RefreshSliderValues();

        isAnimating = true;

        settingsDisplay.SetActive(true);

        backgroundGroup.alpha = 0f;
        backgroundGroup.interactable = true;
        backgroundGroup.blocksRaycasts = true;

        windowGroup.alpha = 0f;
        windowGroup.interactable = false;
        windowGroup.blocksRaycasts = true;

        windowTransform.localScale =
            windowOriginalScale * windowStartScale;

        displaySequence?.Kill();

        displaySequence = DOTween.Sequence()
            .SetUpdate(true);

        displaySequence.Join(
            backgroundGroup.DOFade(
                backgroundTargetAlpha,
                backgroundFadeInDuration
            ).SetEase(backgroundEase)
        );

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
        });
    }

    public void CloseSettings()
    {
        if (isAnimating || settingsDisplay == null ||
            !settingsDisplay.activeSelf)
            return;

        isAnimating = true;

        windowGroup.interactable = false;
        backgroundGroup.interactable = false;

        displaySequence?.Kill();

        displaySequence = DOTween.Sequence()
            .SetUpdate(true);

        displaySequence.Join(
            backgroundGroup.DOFade(
                0f,
                backgroundFadeOutDuration
            ).SetEase(backgroundEase)
        );

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
            settingsDisplay.SetActive(false);

            windowTransform.localScale = windowOriginalScale;

            isAnimating = false;
        });
    }

    private void OnDestroy()
    {
        displaySequence?.Kill();

        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);

        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.onValueChanged.RemoveListener(
                OnCameraSensitivityChanged
            );
        }
    }
}
