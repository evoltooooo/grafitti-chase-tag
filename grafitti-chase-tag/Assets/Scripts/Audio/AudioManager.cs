using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("SFX Voice Pool")]
    [Min(1)]
    [SerializeField] private int pooledVoiceCount = 8;

    [Header("Settings Preview SFX")]
    [SerializeField] private AudioClip settingsSFXPreview;

    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonBackSFX;

    [Header("Role Shuffle SFX")]
    [SerializeField] private AudioClip roleShuffleTickSFX;
    [SerializeField] private AudioClip roleRevealSFX;

    [Header("Countdown SFX")]
    [SerializeField] private AudioClip countdownTickSFX;
    [SerializeField] private AudioClip countdownGoSFX;

    [Header("Tag Result SFX")]
    [SerializeField] private AudioClip playerTaggedSFX;
    [SerializeField] private AudioClip playerEvadedSFX;
    [SerializeField] private AudioClip resultDisplaySFX;

    [Header("Token SFX")]
    [SerializeField] private AudioClip tokenSpawnSFX;

    [Header("BGM Settings")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 1f;

    [SerializeField, Min(0f)] private float bgmFadeDuration = 1f;

    private Coroutine bgmFadeCoroutine;

    public float SFXVolume => sfxVolume;

    private readonly List<AudioSource> voicePool = new List<AudioSource>();
    private readonly List<float> voiceStartTimes = new List<float>();

    private void Awake()
    {
        // Singleton protection.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize the main AudioSource.
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = sfxVolume;

        if (sfxMixerGroup != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;

        InitializeVoicePool();

        InitializeBGM();
    }

    private void InitializeVoicePool()
    {
        voicePool.Clear();
        voiceStartTimes.Clear();

        int count = Mathf.Max(1, pooledVoiceCount);

        for (int i = 0; i < count; i++)
        {
            GameObject voiceObject = new GameObject(
                $"SFX_Voice_{i}"
            );

            voiceObject.transform.SetParent(transform);

            AudioSource voice = voiceObject.AddComponent<AudioSource>();

            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0f;
            voice.volume = sfxVolume;

            voice.outputAudioMixerGroup =
                sfxSource.outputAudioMixerGroup;

            voice.priority = sfxSource.priority;

            voicePool.Add(voice);
            voiceStartTimes.Add(0f);
        }
    }

    public void PlaySettingsSFXPreview()
    {
        PlayConfiguredSFX(settingsSFXPreview);
    }

    public void PlaySFX(
        AudioClip clip,
        float volumeMultiplier,
        float startTimeSeconds)
    {
        if (clip == null || sfxSource == null)
            return;

        startTimeSeconds = Mathf.Clamp(
            startTimeSeconds,
            0f,
            clip.length
        );

        if (startTimeSeconds >= clip.length)
            return;

        AudioSource voice = GetAvailableVoice();

        if (voice == null)
            return;

        int voiceIndex = voicePool.IndexOf(voice);

        voice.Stop();

        voice.clip = clip;

        voice.outputAudioMixerGroup =
            sfxSource.outputAudioMixerGroup;

        voice.volume =
            sfxSource.volume * Mathf.Clamp01(volumeMultiplier);

        voice.pitch = sfxSource.pitch;
        voice.spatialBlend = 0f;
        voice.loop = false;
        voice.playOnAwake = false;

        voice.timeSamples = Mathf.FloorToInt(
            startTimeSeconds * clip.frequency
        );

        voiceStartTimes[voiceIndex] = Time.realtimeSinceStartup;

        voice.Play();
    }

    private AudioSource GetAvailableVoice()
    {
        if (voicePool.Count == 0)
            return null;

        // Prefer an AudioSource that is currently idle.
        for (int i = 0; i < voicePool.Count; i++)
        {
            if (!voicePool[i].isPlaying)
                return voicePool[i];
        }

        // If every source is busy, reuse the oldest voice.
        int oldestVoiceIndex = 0;
        float oldestStartTime = voiceStartTimes[0];

        for (int i = 1; i < voicePool.Count; i++)
        {
            if (voiceStartTimes[i] < oldestStartTime)
            {
                oldestStartTime = voiceStartTimes[i];
                oldestVoiceIndex = i;
            }
        }

        return voicePool[oldestVoiceIndex];
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier)
    {
        if (clip == null)
        {
            Debug.LogWarning(
                "AUDIO MANAGER | Cannot play SFX. AudioClip is missing."
            );
            return;
        }

        if (sfxSource == null)
            return;

        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volumeMultiplier)
        );
    }


    // Plays a configured clip through the pooled SFX AudioSources.
    private void PlayConfiguredSFX(AudioClip clip)
    {
        if (clip == null)
            return;

        PlaySFX(clip, 1f, 0f);
    }

    // UI
    public void PlayButtonHover()
    {
        PlayConfiguredSFX(buttonHoverSFX);
    }

    public void PlayButtonClick()
    {
        PlayConfiguredSFX(buttonClickSFX);
    }

    public void PlayButtonBack()
    {
        PlayConfiguredSFX(buttonBackSFX);
    }

    // Role Shuffle
    public void PlayRoleShuffleTick()
    {
        PlayConfiguredSFX(roleShuffleTickSFX);
    }

    public void PlayRoleReveal()
    {
        PlayConfiguredSFX(roleRevealSFX);
    }

    // Countdown
    public void PlayCountdownTick()
    {
        PlayConfiguredSFX(countdownTickSFX);
    }

    public void PlayCountdownGo()
    {
        PlayConfiguredSFX(countdownGoSFX);
    }

    // Tag Result
    public void PlayPlayerTagged()
    {
        PlayConfiguredSFX(playerTaggedSFX);
    }

    public void PlayPlayerEvaded()
    {
        PlayConfiguredSFX(playerEvadedSFX);
    }

    public void PlayResultDisplaySFX()
    {
        PlayConfiguredSFX(resultDisplaySFX);
    }

    // Tokens
    public void PlayTokenSpawn()
    {
        PlayConfiguredSFX(tokenSpawnSFX);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void InitializeBGM()
    {
        if (bgmSource == null)
        {
            GameObject bgmObject = new GameObject("BGM_Source");
            bgmObject.transform.SetParent(transform);

            bgmSource = bgmObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = 0f;

        if (bgmMixerGroup != null)
            bgmSource.outputAudioMixerGroup = bgmMixerGroup;
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null || bgmSource == null)
            return;

        // Avoid restarting the same track unnecessarily.
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            return;
        }

        StopBGMFade();

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        bgmFadeCoroutine = StartCoroutine(
            FadeBGM(bgmVolume, bgmFadeDuration)
        );
    }

    public void FadeOutBGM(Action onComplete)
    {
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            onComplete?.Invoke();
            return;
        }

        StopBGMFade();

        bgmFadeCoroutine = StartCoroutine(
            FadeOutBGMCoroutine(onComplete)
        );
    }

    private IEnumerator FadeOutBGMCoroutine(Action onComplete)
    {
        yield return FadeBGM(0f, bgmFadeDuration);

        bgmSource.Stop();
        bgmSource.clip = null;

        bgmFadeCoroutine = null;

        onComplete?.Invoke();
    }

    private IEnumerator FadeBGM(float targetVolume, float duration)
    {
        float startVolume = bgmSource.volume;

        if (duration <= 0f)
        {
            bgmSource.volume = targetVolume;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            bgmSource.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                Mathf.Clamp01(elapsed / duration)
            );

            yield return null;
        }

        bgmSource.volume = targetVolume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.volume = bgmVolume;
    }

    private void StopBGMFade()
    {
        if (bgmFadeCoroutine == null)
            return;

        StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = null;
    }
}
