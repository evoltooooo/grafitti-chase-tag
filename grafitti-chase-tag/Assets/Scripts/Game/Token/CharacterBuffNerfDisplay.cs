using System.Collections.Generic;
using UnityEngine;

public class CharacterBuffNerfDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterBuffNerf characterEffects;
    [SerializeField] private Transform indicatorContainer;
    [SerializeField] private BuffNerfEffectIndicatorUI indicatorPrefab;

    [Header("Display Settings")]
    [SerializeField] private bool newestFirst;

    private readonly List<BuffNerfEffectIndicatorUI> indicators =
        new List<BuffNerfEffectIndicatorUI>();

    private void Awake()
    {
        if (indicatorContainer == null)
            Debug.LogError(
                "CharacterBuffNerfDisplay: Indicator Container is missing.",
                this
            );

        if (indicatorPrefab == null)
            Debug.LogError(
                "CharacterBuffNerfDisplay: Indicator Prefab is missing.",
                this
            );
    }

    private void OnEnable()
    {
        if (characterEffects == null)
            return;

        characterEffects.EffectsChanged += RefreshDisplay;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (characterEffects != null)
            characterEffects.EffectsChanged -= RefreshDisplay;
    }

    private void Update()
    {
        UpdateIndicatorDurations();
    }

    public void SetCharacterBuffNerf(
    CharacterBuffNerf character)
    {
        if (characterEffects == character)
            return;

        if (characterEffects != null)
            characterEffects.EffectsChanged -= RefreshDisplay;

        characterEffects = character;

        if (isActiveAndEnabled && characterEffects != null)
        {
            characterEffects.EffectsChanged += RefreshDisplay;
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        if (characterEffects == null ||
            indicatorContainer == null ||
            indicatorPrefab == null)
            return;

        // Remove existing indicators.
        foreach (BuffNerfEffectIndicatorUI indicator in indicators)
        {
            if (indicator != null)
                Destroy(indicator.gameObject);
        }

        indicators.Clear();

        // Create one indicator for each active effect.
        for (int i = 0; i < characterEffects.ActiveEffectCount; i++)
        {
            BuffNerfTokenData tokenData =
                characterEffects.GetActiveTokenDataAt(i);

            if (tokenData == null)
                continue;

            BuffNerfEffectIndicatorUI indicator =
                Instantiate(indicatorPrefab, indicatorContainer);

            indicator.Initialize(tokenData);

            // Move the newest effect to the first position if enabled.
            if (newestFirst)
            {
                indicator.transform.SetAsFirstSibling();
            }

            indicators.Add(indicator);
        }

        UpdateIndicatorDurations();
    }

    private void UpdateIndicatorDurations()
    {
        if (characterEffects == null)
            return;

        int effectCount = Mathf.Min(
            indicators.Count,
            characterEffects.ActiveEffectCount
        );

        for (int i = 0; i < effectCount; i++)
        {
            if (indicators[i] == null)
                continue;

            float remainingDuration =
                characterEffects.GetNormalizedRemainingDurationAt(i);

            indicators[i].SetRemainingDuration(remainingDuration);
        }
    }
}
