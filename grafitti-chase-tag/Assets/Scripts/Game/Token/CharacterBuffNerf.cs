
using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBuffNerf : MonoBehaviour
{
    [Serializable]
    private class ActiveEffect
    {
        public BuffNerfTokenData data;
        public float remainingDuration;
    }

    [Header("References")]
    [SerializeField] private CharacterRole characterRole;

    private readonly List<ActiveEffect> activeEffects =
        new List<ActiveEffect>();

    public event Action EffectsChanged;

    public int ActiveEffectCount => activeEffects.Count;

    private void Awake()
    {
        if (characterRole == null)
        {
            characterRole = GetComponent<CharacterRole>();
        }

        if (characterRole == null)
        {
            Debug.LogWarning(
                $"{name}: CharacterBuffNerf could not find CharacterRole.",
                this
            );
        }
    }

    private void Update()
    {
        UpdateEffectDurations();
    }

    // Called when this character collects a token.
    public bool TryApplyToken(BuffNerfTokenData tokenData)
    {
        if (tokenData == null)
            return false;

        // Chaser-only effects cannot be applied to an Evader.
        if (!CanReceiveEffect(tokenData))
        {
            Debug.Log(
                $"BUFF NERF IGNORED | Character={name} | " +
                $"Token={tokenData.DisplayName} | " +
                "Role restriction not satisfied.",
                this
            );

            return false;
        }

        // Find an existing effect with the same token ID.
        ActiveEffect existingEffect =
            activeEffects.Find(effect =>
                effect.data != null &&
                effect.data.TokenId == tokenData.TokenId
            );

        if (existingEffect != null)
        {
            // Refresh duration without stacking its multiplier.
            existingEffect.data = tokenData;
            existingEffect.remainingDuration =
                tokenData.Duration;

            Debug.Log(
                $"BUFF NERF REFRESHED | Character={name} | " +
                $"Token={tokenData.DisplayName} | " +
                $"Duration={tokenData.Duration:F1}s",
                this
            );
        }
        else
        {
            // A different token type gets its own duration.
            activeEffects.Add(new ActiveEffect
            {
                data = tokenData,
                remainingDuration = tokenData.Duration
            });

            Debug.Log(
                $"BUFF NERF APPLIED | Character={name} | " +
                $"Token={tokenData.DisplayName} | " +
                $"Multiplier={tokenData.EffectMultiplier:F2} | " +
                $"Duration={tokenData.Duration:F1}s",
                this
            );
        }

        EffectsChanged?.Invoke();
        return true;
    }

    private bool CanReceiveEffect(BuffNerfTokenData tokenData)
    {
        if (tokenData.RoleRestriction ==
            BuffNerfRoleRestriction.Any)
        {
            return true;
        }

        if (tokenData.RoleRestriction ==
            BuffNerfRoleRestriction.ChaserOnly)
        {
            return characterRole != null &&
                   characterRole.IsChaser;
        }

        return false;
    }

    private void UpdateEffectDurations()
    {
        bool effectsRemoved = false;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];

            if (effect.data == null)
            {
                activeEffects.RemoveAt(i);
                effectsRemoved = true;
                continue;
            }

            effect.remainingDuration -= Time.deltaTime;

            if (effect.remainingDuration <= 0f)
            {
                Debug.Log(
                    $"BUFF NERF EXPIRED | Character={name} | " +
                    $"Token={effect.data.DisplayName}",
                    this
                );

                activeEffects.RemoveAt(i);
                effectsRemoved = true;
            }
        }

        if (effectsRemoved)
        {
            EffectsChanged?.Invoke();
        }
    }

    // Returns the combined multiplier for a particular effect category.
    public float GetMultiplier(BuffNerfEffectType effectType)
    {
        float combinedMultiplier = 1f;

        foreach (ActiveEffect effect in activeEffects)
        {
            if (effect.data == null)
                continue;

            if (effect.data.EffectType != effectType)
                continue;

            combinedMultiplier *= effect.data.EffectMultiplier;
        }

        return combinedMultiplier;
    }

    // Returns the remaining duration of a particular token.
    public float GetRemainingDuration(BuffNerfTokenId tokenId)
    {
        ActiveEffect effect = activeEffects.Find(item =>
            item.data != null &&
            item.data.TokenId == tokenId
        );

        return effect != null
            ? Mathf.Max(0f, effect.remainingDuration)
            : 0f;
    }

    public bool HasActiveEffect(BuffNerfTokenId tokenId)
    {
        return activeEffects.Exists(effect =>
            effect.data != null &&
            effect.data.TokenId == tokenId
        );
    }
}
