using UnityEngine;

[CreateAssetMenu(
    fileName = "StaminaSettings",
    menuName = "Chase Tag/Stats/Stamina Settings"
)]
public class StaminaSettings : ScriptableObject
{
    [Header("Stamina")]
    [Min(1f)]
    public float maxStamina = 100f;

    [Header("Regeneration")]
    [Min(0f)]
    public float regenerationRate = 20f;

    [Min(0f)]
    public float regenerationDelay = 0.5f;
}