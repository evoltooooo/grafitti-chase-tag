using UnityEngine;

public class CharacterStamina : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private StaminaSettings settings;

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
    }

    private void Update()
    {
        Regenerate();
    }

    public bool CanConsume(float amount)
    {
        if (amount <= 0f)
            return true;

        return currentStamina >= amount;
    }

    public bool TryConsume(float amount)
    {
        if (!CanConsume(amount))
            return false;

        currentStamina -= amount;
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

        currentStamina +=
            settings.regenerationRate *
            Time.deltaTime;

        currentStamina =
            Mathf.Min(
                currentStamina,
                settings.maxStamina
            );
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);

        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;

        GUI.Label(
            new Rect(20f, 20f, 400f, 50f),
            $"Stamina: {CurrentStamina:0.0} / {MaxStamina:0.0}",
            style
        );
    }
}