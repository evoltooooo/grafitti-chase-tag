
using UnityEngine;

[RequireComponent(typeof(CharacterActionRuntime))]
public class CharacterActionSFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Action SFX")]
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip climbSFX;
    [SerializeField] private AudioClip poleSpinVaultSFX;
    [SerializeField] private AudioClip slideSFX;
    [SerializeField] private AudioClip ticTacSFX;
    [SerializeField] private AudioClip wallReboundSFX;
    [SerializeField] private AudioClip spraySFX;

    [Header("SFX Start Time (Seconds)")]
    [Min(0f)] [SerializeField] private float jumpStartTime;
    [Min(0f)] [SerializeField] private float climbStartTime;
    [Min(0f)] [SerializeField] private float poleSpinVaultStartTime;
    [Min(0f)] [SerializeField] private float slideStartTime;
    [Min(0f)] [SerializeField] private float ticTacStartTime;
    [Min(0f)] [SerializeField] private float wallReboundStartTime;
    [Min(0f)] [SerializeField] private float sprayStartTime;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMultiplier = 1f;

    private void Awake()
    {
        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();
    }

    private void OnEnable()
    {
        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (actionRuntime != null)
            actionRuntime.ActionStarted += HandleActionStarted;
    }

    private void OnDisable()
    {
        if (actionRuntime != null)
            actionRuntime.ActionStarted -= HandleActionStarted;
    }

    private void HandleActionStarted()
    {
        if (AudioManager.Instance == null)
            return;

        AudioClip clip = GetActionClip();

        if (clip == null)
            return;

        float startTime = GetActionStartTime(clip);

        AudioManager.Instance.PlaySFX(
            clip,
            volumeMultiplier,
            startTime
        );
    }

    private float GetActionStartTime(AudioClip clip)
    {
        if (clip == jumpSFX) return jumpStartTime;
        if (clip == climbSFX) return climbStartTime;
        if (clip == poleSpinVaultSFX) return poleSpinVaultStartTime;
        if (clip == slideSFX) return slideStartTime;
        if (clip == ticTacSFX) return ticTacStartTime;
        if (clip == wallReboundSFX) return wallReboundStartTime;
        if (clip == spraySFX) return sprayStartTime;

        return 0f;
    }

    private AudioClip GetActionClip()
    {
        switch (actionRuntime.CurrentParkourActionType)
        {
            case ParkourActionType.Vault:
            case ParkourActionType.PoleSpin:
                return poleSpinVaultSFX;

            case ParkourActionType.Climb:
                return climbSFX;

            case ParkourActionType.Slide:
                return slideSFX;

            case ParkourActionType.TicTac:
                return ticTacSFX;

            case ParkourActionType.WallRebound:
                return wallReboundSFX;
        }

        switch (actionRuntime.CurrentActionType)
        {
            case CharacterActionType.Jump:
                return jumpSFX;

            case CharacterActionType.Slide:
                return slideSFX;

            case CharacterActionType.Tag:
                return spraySFX;

            case CharacterActionType.Vault:
            case CharacterActionType.PoleSpin:
                return poleSpinVaultSFX;

            case CharacterActionType.Climb:
                return climbSFX;

            case CharacterActionType.TicTac:
                return ticTacSFX;

            case CharacterActionType.WallRebound:
                return wallReboundSFX;
        }

        return null;
    }
}
