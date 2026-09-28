using UnityEngine;

[System.Serializable]
public class TargetMatchPhase
{
    [Header("Animation")]
    public string animationStateName;

    [Range(0f, 1f)]
    public float matchStartTime = 0.05f;

    [Range(0f, 1f)]
    public float matchEndTime = 0.5f;

    [Header("Target")]
    public AvatarTarget matchBodyPart = AvatarTarget.RightHand;

    public bool matchRotation = true;

    public Vector3 targetPositionOffset;

    [Header("Loop")]
    public bool repeatWhileActionActive;
}