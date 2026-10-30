using UnityEngine;

public class CharacterStamina : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private StaminaSettings settings;
    [SerializeField] private CharacterBuffNerf buffNerf;

    private float currentStamina;
    private float regenerationTimer;

    public float CurrentStamina =>
        currentStamina;

    public float MaxStamina =>
        settings != null
            ? settings.maxStamina
            : 0f;

    public float NormalizedStamina =>
        MaxStamina > 0f
            ? currentStamina / MaxStamina
            : 0f;

    public bool IsEmpty =>
        currentStamina <= 0f;

    public bool IsFull =>
        currentStamina >= MaxStamina;

    private void Awake()
    {
        if (settings == null)
        {
            Debug.LogError(
                $"{name}: CharacterStamina is missing StaminaSettings.",
                this
            );

            return;
        }

        currentStamina =
            settings.maxStamina;
        
        if (buffNerf == null)
        {
            buffNerf = GetComponent<CharacterBuffNerf>();
        }
    }

    private void Update()
    {
        Regenerate();
    }

    public bool CanConsume(float amount)
    {
        if (amount <= 0f)
            return true;

        float costMultiplier = buffNerf != null
            ? buffNerf.GetMultiplier(
                BuffNerfEffectType.StaminaCostMultiplier)
            : 1f;

        float modifiedCost = amount * costMultiplier;

        return currentStamina >= modifiedCost;
    }

    public bool TryConsume(float amount)
    {
        if (!CanConsume(amount))
            return false;

        float costMultiplier = buffNerf != null
            ? buffNerf.GetMultiplier(
                BuffNerfEffectType.StaminaCostMultiplier)
            : 1f;

        float modifiedCost = amount * costMultiplier;

        currentStamina -= modifiedCost;

        currentStamina =
            Mathf.Max(0f, currentStamina);

        regenerationTimer =
            settings.regenerationDelay;

        return true;
    }

    public void ConsumeOverTime(float amountPerSecond)
    {
        if (amountPerSecond <= 0f)
            return;

        currentStamina -=
            amountPerSecond * Time.deltaTime;

        currentStamina =
            Mathf.Max(0f, currentStamina);

        regenerationTimer =
            settings.regenerationDelay;
    }

    private void Regenerate()
    {
        if (settings == null)
            return;

        if (IsFull)
            return;

        if (regenerationTimer > 0f)
        {
            regenerationTimer -=
                Time.deltaTime;

            return;
        }

        float regenerationMultiplier = buffNerf != null
            ? buffNerf.GetMultiplier(
                BuffNerfEffectType.StaminaRegenMultiplier)
            : 1f;

        currentStamina +=
            settings.regenerationRate *
            regenerationMultiplier *
            Time.deltaTime;

        currentStamina =
            Mathf.Min(
                currentStamina,
                settings.maxStamina
            );
    }
}