
using UnityEngine;

public enum BuffNerfTokenId
{
    SpeedUp,
    SpeedDown,
    StaminaRegenUp,
    StaminaRegenDown,
    CheaperStaminaCost,
    ExpensiveStaminaCost,
    TagSprayRangeUp,
    TagSprayRangeDown,
    TagSprayRadiusUp,
    TagSprayRadiusDown
}

public enum BuffNerfTokenCategory
{
    Buff,
    Nerf
}

public enum BuffNerfEffectType
{
    SpeedMultiplier,
    StaminaRegenMultiplier,
    StaminaCostMultiplier,
    TagSprayRangeMultiplier,
    TagSprayRadiusMultiplier
}

public enum BuffNerfRoleRestriction
{
    Any,
    ChaserOnly
}

[CreateAssetMenu(
    fileName = "BuffNerfToken",
    menuName = "Chase Tag/Buff Nerf/Token Data"
)]
public class BuffNerfTokenData : ScriptableObject
{
    [Header("Token Identity")]
    [SerializeField] private BuffNerfTokenId tokenId;
    [SerializeField] private string displayName;

    [Header("Token Classification")]
    [SerializeField] private BuffNerfTokenCategory category;
    [SerializeField] private BuffNerfEffectType effectType;

    [Header("Effect Settings")]
    [SerializeField, Min(0.01f)]
    private float effectMultiplier = 1f;

    [SerializeField, Min(0.1f)]
    private float duration = 10f;

    [Header("Role Restriction")]
    [SerializeField]
    private BuffNerfRoleRestriction roleRestriction =
        BuffNerfRoleRestriction.Any;

    [Header("Visual References")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject worldPrefab;
    [SerializeField] private GameObject pickupVfxPrefab;

    public BuffNerfTokenId TokenId => tokenId;
    public string DisplayName => displayName;
    public BuffNerfTokenCategory Category => category;
    public BuffNerfEffectType EffectType => effectType;
    public float EffectMultiplier => effectMultiplier;
    public float Duration => duration;
    public BuffNerfRoleRestriction RoleRestriction => roleRestriction;
    public Sprite Icon => icon;
    public GameObject WorldPrefab => worldPrefab;
    public GameObject PickupVfxPrefab => pickupVfxPrefab;

    private void OnValidate()
    {
        effectMultiplier = Mathf.Max(0.01f, effectMultiplier);
        duration = Mathf.Max(0.1f, duration);
    }
}
