using UnityEngine;

public class CharacterTaggedSmokeVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem smokeParticles;

    [Header("Burst Settings")]
    [SerializeField, Min(1)] private int particleCount = 50;

    [SerializeField]
    private Vector2 particleSpeedRange = new Vector2(2f, 4f);

    [Header("Color Variation")]
    [SerializeField, Range(0f, 90f)]
    private float hueVariation = 20f;

    [SerializeField, Range(0f, 0.5f)]
    private float saturationVariation = 0.15f;

    [SerializeField, Range(0f, 0.5f)]
    private float brightnessVariation = 0.2f;

    private void Awake()
    {
        if (smokeParticles == null)
            smokeParticles = GetComponent<ParticleSystem>();

        if (smokeParticles == null)
        {
            Debug.LogError(
                "TAGGED SMOKE VFX | Particle System reference is missing.",
                this
            );

            enabled = false;
        }
    }

    public void PlayBurst(Color inheritedColor)
    {
        if (smokeParticles == null)
            return;

        // Clear any previous particles.
        smokeParticles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        smokeParticles.Play();

        // Generate the burst from the character's center.
        Vector3 burstPosition = transform.position;

        for (int i = 0; i < particleCount; i++)
        {
            ParticleSystem.EmitParams emitParams =
                new ParticleSystem.EmitParams();

            // Random direction across the entire sphere.
            Vector3 direction = Random.onUnitSphere;

            float speed = Random.Range(
                particleSpeedRange.x,
                particleSpeedRange.y
            );

            emitParams.position = burstPosition;
            emitParams.velocity = direction * speed;
            emitParams.startColor = GenerateSmokeColor(inheritedColor);

            smokeParticles.Emit(emitParams, 1);
        }
    }

    private Color GenerateSmokeColor(Color baseColor)
    {
        Color.RGBToHSV(
            baseColor,
            out float hue,
            out float saturation,
            out float brightness
        );

        // Apply randomized HSV variations.
        hue += Random.Range(
            -hueVariation,
            hueVariation
        ) / 360f;

        saturation += Random.Range(
            -saturationVariation,
            saturationVariation
        );

        brightness += Random.Range(
            -brightnessVariation,
            brightnessVariation
        );

        hue = Mathf.Repeat(hue, 1f);
        saturation = Mathf.Clamp01(saturation);
        brightness = Mathf.Clamp01(brightness);

        Color result = Color.HSVToRGB(
            hue,
            saturation,
            brightness
        );

        result.a = baseColor.a;

        return result;
    }
}
