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

    public float SFXVolume => sfxVolume;

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

        // Initialize AudioSource.
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = sfxVolume;

        if (sfxMixerGroup != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    public void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, 1f);
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
