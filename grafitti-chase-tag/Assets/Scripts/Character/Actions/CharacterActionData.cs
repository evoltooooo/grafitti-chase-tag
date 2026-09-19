using UnityEngine;

public enum ActionMotionType
{
    ScriptMotion,
    RootMotion,
    TargetMatch,
    Hybrid
}

[CreateAssetMenu(
    fileName = "CharacterActionData",
    menuName = "Chase Tag/Actions/Character Action Data"
)]
public class CharacterActionData : ScriptableObject
{
    [Header("Identity")]
    public CharacterActionType actionType;

    [Header("Character State")]
    public CharacterState actionState =
        CharacterState.Locomotion;

    [Header("Animation")]
    public string animationStateName;

    [Header("Motion")]
    public ActionMotionType motionType =
        ActionMotionType.ScriptMotion;

    [Header("Timing")]
    public bool holdToExecute = false;
    
    [Min(0f)]
    public float duration = 1f;

    [Min(0f)]
    public float cooldown = 0f;

    [Header("Stamina")]
    [Min(0f)]
    public float staminaCost = 0f;

    [Header("Action Movement")]
    [Min(0f)]
    public float movementSpeed = 0f;

    public AnimationCurve movementSpeedCurve =
        AnimationCurve.Linear(0f, 1f, 1f, 0f);
    
    [Header("Action Steering")]
    [Range(0f, 1f)]
    public float steeringAmount = 0.25f;

    [Min(0f)]
    public float steeringRotationSpeed = 5f;
}