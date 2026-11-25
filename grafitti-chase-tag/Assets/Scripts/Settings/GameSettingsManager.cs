using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    private const string BGMVolumeKey = "Settings_BGMVolume";
    private const string SFXVolumeKey = "Settings_SFXVolume";
    private const string CameraSensitivityKey = "Settings_CameraSensitivity";

    private const float DefaultBGMVolume = 1f;
    private const float DefaultSFXVolume = 1f;
    private const float DefaultCameraSensitivity = 0.7f;

    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float CameraSensitivity { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplySettings();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LoadSettings()
    {
        BGMVolume = PlayerPrefs.GetFloat(
            BGMVolumeKey,
            DefaultBGMVolume
        );

        SFXVolume = PlayerPrefs.GetFloat(
            SFXVolumeKey,
            DefaultSFXVolume
        );

        CameraSensitivity = PlayerPrefs.GetFloat(
            CameraSensitivityKey,
            DefaultCameraSensitivity
        );

        BGMVolume = Mathf.Clamp01(BGMVolume);
        SFXVolume = Mathf.Clamp01(SFXVolume);

        CameraSensitivity = Mathf.Clamp01(CameraSensitivity);
    }

    public void SetBGMVolume(float value)
    {
        BGMVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(BGMVolumeKey, BGMVolume);
        PlayerPrefs.Save();

        ApplyAudioSettings();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(SFXVolumeKey, SFXVolume);
        PlayerPrefs.Save();

        ApplyAudioSettings();
    }

    public void SetCameraSensitivity(float value)
    {
        CameraSensitivity = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            CameraSensitivityKey,
            CameraSensitivity
        );

        PlayerPrefs.Save();

        ApplyCameraSettings();
    }

    private void ApplySettings()
    {
        ApplyAudioSettings();
        ApplyCameraSettings();
    }

    private void ApplyAudioSettings()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetBGMVolume(BGMVolume);
        AudioManager.Instance.SetSFXVolume(SFXVolume);
    }

    private void ApplyCameraSettings()
    {
        ParkourCameraController cameraController =
            FindFirstObjectByType<ParkourCameraController>();

        if (cameraController != null)
        {
            cameraController.SetCameraSensitivity(
                CameraSensitivity
            );
        }
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        ApplySettings();
    }
}
