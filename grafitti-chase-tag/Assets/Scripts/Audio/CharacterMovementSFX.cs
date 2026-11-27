using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CharacterMovementSFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private AudioSource movementAudioSource;

    [Header("Locomotion SFX")]
    [SerializeField] private AudioClip normalMovementSFX;
    [SerializeField] private AudioClip sprintSFX;

    [Range(0f, 1f)]
    [SerializeField] private float normalMovementVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float sprintVolume = 0.7f;

    [Header("Playback Settings")]
    [Min(0f)]
    [SerializeField] private float minimumMovementSpeed = 0.2f;

    [Min(0.01f)]
    [SerializeField] private float volumeFadeSpeed = 4f;

    private float targetVolume;

    private void Awake()
    {
        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (movementAudioSource == null)
            movementAudioSource = GetComponent<AudioSource>();

        movementAudioSource.playOnAwake = false;
        movementAudioSource.loop = true;
        movementAudioSource.spatialBlend = 0f;
        movementAudioSource.volume = 0f;
    }

    private void Update()
    {
        UpdateMovementSFX();
    }

    private void UpdateMovementSFX()
    {
        if (characterMotor == null ||
            movementAudioSource == null)
        {
            targetVolume = 0f;
            UpdateVolume();
            return;
        }

        bool actionExecuting =
            actionRuntime != null &&
            actionRuntime.IsExecuting;

        bool isMoving =
            characterMotor.HorizontalSpeed >
            minimumMovementSpeed;

        bool shouldPlay =
            characterMotor.IsGrounded &&
            isMoving &&
            !characterMotor.IsSliding &&
            !actionExecuting;

        if (!shouldPlay)
        {
            targetVolume = 0f;
            UpdateVolume();
            return;
        }

        AudioClip targetClip;

        if (characterMotor.IsSprinting)
        {
            targetClip = sprintSFX;
            targetVolume = sprintVolume;
        }
        else
        {
            targetClip = normalMovementSFX;
            targetVolume = normalMovementVolume;
        }

        if (targetClip == null)
        {
            targetVolume = 0f;
            UpdateVolume();
            return;
        }

        // Switch audio only when the movement type changes.
        if (movementAudioSource.clip != targetClip)
        {
            movementAudioSource.Stop();
            movementAudioSource.clip = targetClip;
            movementAudioSource.loop = true;
            movementAudioSource.Play();
        }
        else if (!movementAudioSource.isPlaying)
        {
            movementAudioSource.Play();
        }

        UpdateVolume();
    }

    private void UpdateVolume()
    {
        movementAudioSource.volume =
            Mathf.MoveTowards(
                movementAudioSource.volume,
                targetVolume,
                volumeFadeSpeed * Time.deltaTime
            );

        if (targetVolume <= 0f &&
            movementAudioSource.volume <= 0.001f &&
            movementAudioSource.isPlaying)
        {
            movementAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        if (movementAudioSource != null)
        {
            movementAudioSource.Stop();
            movementAudioSource.volume = 0f;
        }
    }
}
