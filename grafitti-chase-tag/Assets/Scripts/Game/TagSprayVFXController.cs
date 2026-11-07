using UnityEngine;

public class TagSprayVFXController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterTagDetector tagDetector;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private Transform visualRoot;

    [Header("Visual Settings")]
    [SerializeField] private float verticalOffset = 1f;
    [SerializeField] private float forwardOffset = 0f;

    [Header("Particle References")]
    [SerializeField] private ParticleSystem mainSprayParticles;
    [SerializeField] private ParticleSystem sprayDroplets;
    [SerializeField] private ParticleSystem sprayFlecks;

    [Header("Particle Burst Counts")]
    [SerializeField, Min(0)] private int mainCloudCount = 8;
    [SerializeField, Min(0)] private int dropletCount = 12;
    [SerializeField, Min(0)] private int fleckCount = 8;

    [Header("Continuous Spray Settings")]
    [SerializeField, Min(0f)]
    private float mainCloudEmissionRate = 18f;

    [SerializeField, Min(0f)]
    private float dropletEmissionRate = 28f;

    [SerializeField, Min(0f)]
    private float fleckEmissionRate = 16f;

    [Header("Spray Start")]
    [SerializeField, Min(0)]
    private int initialMainCloudCount = 2;

    [SerializeField, Min(0)]
    private int initialDropletCount = 2;

    [SerializeField, Min(0)]
    private int initialFleckCount = 1;

    [Header("Spray Color")]
    [SerializeField] private Color baseColor = new Color(1f, 0.2f, 0.4f);

    [SerializeField, Range(0f, 90f)]
    private float hueVariation = 30f;

    [SerializeField, Range(0f, 0.5f)]
    private float saturationVariation = 0.15f;

    [SerializeField, Range(0f, 0.5f)]
    private float brightnessVariation = 0.2f;

    [Header("Main Cloud Color Variation")]
    [SerializeField, Range(0f, 90f)]
    private float mainHueVariation = 20f;

    [SerializeField, Range(0f, 0.5f)]
    private float mainBrightnessVariation = 0.15f;

    [Header("Droplet Color Variation")]
    [SerializeField, Range(0f, 90f)]
    private float dropletHueVariation = 35f;

    [SerializeField, Range(0f, 0.5f)]
    private float dropletBrightnessVariation = 0.25f;

    [Header("Fleck Color Variation")]
    [SerializeField, Range(0f, 90f)]
    private float fleckHueVariation = 45f;

    [SerializeField, Range(0f, 0.5f)]
    private float fleckBrightnessVariation = 0.3f;

    private bool wasSpraying;

    private float mainEmissionAccumulator;
    private float dropletEmissionAccumulator;
    private float fleckEmissionAccumulator;

    private Vector3 baseVisualScale;

    private bool hasLoggedAIDiagnostic;

    private void Awake()
    {
        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (tagDetector == null)
            tagDetector = GetComponent<CharacterTagDetector>();

        if (mainSprayParticles == null ||
            sprayDroplets == null ||
            sprayFlecks == null)
        {
            enabled = false;
            return;
        }

        baseVisualScale = visualRoot.localScale;

        visualRoot.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (name == "AI" && !hasLoggedAIDiagnostic)
        {
            hasLoggedAIDiagnostic = true;
        }

        bool isSpraying =
            actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentActionType == CharacterActionType.Tag;

        // Continuously emit while Tag is executing.
        if (isSpraying)
        {
            UpdateAllParticleGeometry();
            EmitContinuousSpray(Time.deltaTime);
        }

        // Tag action has just ended.
        if (!isSpraying && wasSpraying)
        {
            StopSprayEmission();
        }

        wasSpraying = isSpraying;

        // Keep the visual root alive until every particle disappears.
        if (!isSpraying &&
            visualRoot.gameObject.activeSelf &&
            !AreParticlesAlive())
        {
            visualRoot.gameObject.SetActive(false);
        }

        UpdateVisualPosition();
    }

    private void UpdateParticleGeometry(ParticleSystem particleSystem)
    {
        if (particleSystem == null || tagDetector == null)
            return;

        // Match the detector's cone angle.
        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.angle = tagDetector.EffectiveTagHalfAngle;

        // Calculate the speed required to cover the effective range.
        float lifetime = particleSystem.main.startLifetime.constant;

        if (lifetime <= 0f)
            return;

        ParticleSystem.MainModule main = particleSystem.main;

        main.startSpeed =
            tagDetector.EffectiveTagDistance / lifetime;
    }

    private void UpdateAllParticleGeometry()
    {
        UpdateParticleGeometry(mainSprayParticles);
        UpdateParticleGeometry(sprayDroplets);
        UpdateParticleGeometry(sprayFlecks);
    }

    private void StartSpray()
    {
        visualRoot.gameObject.SetActive(true);

        UpdateVisualPosition();

        mainEmissionAccumulator = 0f;
        dropletEmissionAccumulator = 0f;
        fleckEmissionAccumulator = 0f;

        PrepareParticleSystem(mainSprayParticles);
        PrepareParticleSystem(sprayDroplets);
        PrepareParticleSystem(sprayFlecks);

        float densityMultiplier = GetSprayDensityMultiplier();

        // Immediate initial spray.
        EmitColoredParticles(
            mainSprayParticles,
            Mathf.RoundToInt(initialMainCloudCount * densityMultiplier),
            mainHueVariation,
            mainBrightnessVariation
        );

        EmitColoredParticles(
            sprayDroplets,
            Mathf.RoundToInt(initialDropletCount * densityMultiplier),
            dropletHueVariation,
            dropletBrightnessVariation
        );

        EmitColoredParticles(
            sprayFlecks,
            Mathf.RoundToInt(initialFleckCount * densityMultiplier),
            fleckHueVariation,
            fleckBrightnessVariation
        );
    }

    private void UpdateVisualPosition()
    {
        visualRoot.localPosition = new Vector3(
            0f,
            verticalOffset,
            forwardOffset
        );
    }

    private void PrepareParticleSystem(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
            return;

        particleSystem.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        particleSystem.Play(true);
    }

    private float GetSprayDensityMultiplier()
    {
        if (tagDetector == null)
            return 1f;

        float rangeMultiplier =
            tagDetector.EffectiveTagDistance /
            tagDetector.BaseTagDistance;

        float radiusMultiplier =
            tagDetector.EffectiveTagRadius /
            tagDetector.BaseTagRadius;

        // Cone volume is proportional to radius² × length.
        return rangeMultiplier *
            radiusMultiplier *
            radiusMultiplier;
    }

    private void EmitContinuousSpray(float deltaTime)
    {
        float densityMultiplier = GetSprayDensityMultiplier();

        EmitContinuousLayer(
            mainSprayParticles,
            mainCloudEmissionRate * densityMultiplier,
            ref mainEmissionAccumulator,
            mainHueVariation,
            mainBrightnessVariation,
            deltaTime
        );

        EmitContinuousLayer(
            sprayDroplets,
            dropletEmissionRate * densityMultiplier,
            ref dropletEmissionAccumulator,
            dropletHueVariation,
            dropletBrightnessVariation,
            deltaTime
        );

        EmitContinuousLayer(
            sprayFlecks,
            fleckEmissionRate * densityMultiplier,
            ref fleckEmissionAccumulator,
            fleckHueVariation,
            fleckBrightnessVariation,
            deltaTime
        );
    }

    private void EmitContinuousLayer(
        ParticleSystem particleSystem,
        float emissionRate,
        ref float accumulator,
        float hueVariation,
        float brightnessVariation,
        float deltaTime)
    {
        if (particleSystem == null || emissionRate <= 0f)
            return;

        accumulator += emissionRate * deltaTime;

        int particlesToEmit = Mathf.FloorToInt(accumulator);

        if (particlesToEmit <= 0)
            return;

        accumulator -= particlesToEmit;

        EmitColoredParticles(
            particleSystem,
            particlesToEmit,
            hueVariation,
            brightnessVariation
        );
    }

    private void StopSprayEmission()
    {
        StopParticleEmission(mainSprayParticles);
        StopParticleEmission(sprayDroplets);
        StopParticleEmission(sprayFlecks);
    }

    private void StopParticleEmission(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
            return;

        particleSystem.Stop(
            false,
            ParticleSystemStopBehavior.StopEmitting
        );
    }

    private Color GenerateSprayColor(
        float additionalHueVariation,
        float additionalBrightnessVariation)
    {
        Color.RGBToHSV(
            baseColor,
            out float hue,
            out float saturation,
            out float brightness
        );

        float totalHueVariation =
            (hueVariation + additionalHueVariation) / 360f;

        hue += Random.Range(
            -totalHueVariation,
            totalHueVariation
        );

        saturation += Random.Range(
            -saturationVariation,
            saturationVariation
        );

        brightness += Random.Range(
            -(brightnessVariation + additionalBrightnessVariation),
            brightnessVariation + additionalBrightnessVariation
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

    private void EmitColoredParticles(
        ParticleSystem particleSystem,
        int count,
        float additionalHueVariation,
        float additionalBrightnessVariation)
    {
        if (particleSystem == null || count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.EmitParams emitParams =
                new ParticleSystem.EmitParams();

            emitParams.startColor = GenerateSprayColor(
                additionalHueVariation,
                additionalBrightnessVariation
            );

            particleSystem.Emit(emitParams, 1);
        }
    }

    private void EmitSprayColorPalette()
    {
        EmitColoredParticles(
            mainSprayParticles,
            mainCloudCount,
            mainHueVariation,
            mainBrightnessVariation
        );

        EmitColoredParticles(
            sprayDroplets,
            dropletCount,
            dropletHueVariation,
            dropletBrightnessVariation
        );

        EmitColoredParticles(
            sprayFlecks,
            fleckCount,
            fleckHueVariation,
            fleckBrightnessVariation
        );
    }

    private bool AreParticlesAlive()
    {
        return
            (mainSprayParticles != null &&
            mainSprayParticles.IsAlive(true)) ||

            (sprayDroplets != null &&
            sprayDroplets.IsAlive(true)) ||

            (sprayFlecks != null &&
            sprayFlecks.IsAlive(true));
    }

    private void HandleActionStarted()
    {
        if (actionRuntime == null)
            return;

        if (actionRuntime.CurrentActionType != CharacterActionType.Tag)
            return;

        wasSpraying = true;

        StartSpray();
    }

    private void OnEnable()
    {
        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (actionRuntime != null)
        {
            actionRuntime.ActionStarted += HandleActionStarted;
        }
    }

    private void OnDisable()
    {
        if (actionRuntime != null)
        {
            actionRuntime.ActionStarted -= HandleActionStarted;
        }

        wasSpraying = false;

        if (visualRoot != null)
            visualRoot.gameObject.SetActive(false);
    }
}
