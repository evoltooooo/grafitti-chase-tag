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
}
