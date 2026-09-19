using UnityEngine;

public abstract class ParkourMotionProfile : ScriptableObject
{
    [Header("General")]
    [Min(0f)]
    public float rotationSpeed = 10f;

    /// <summary>
    /// Calculates the desired character position
    /// for the current action progress.
    /// </summary>
    public abstract Vector3 EvaluatePosition(
        Vector3 startPosition,
        ParkourTarget target,
        float normalizedTime
    );

    /// <summary>
    /// Calculates the desired character rotation.
    /// Profiles may override this when they need
    /// custom rotation behavior.
    /// </summary>
    public virtual Quaternion EvaluateRotation(
        Quaternion currentRotation,
        ParkourTarget target,
        float normalizedTime
    )
    {
        return target.TargetRotation;
    }
}