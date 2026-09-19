using UnityEngine;

[CreateAssetMenu(
    fileName = "TrajectoryMotionProfile",
    menuName = "Chase Tag/Parkour/Motion/Trajectory Motion Profile"
)]
public class TrajectoryMotionProfile : ParkourMotionProfile
{
    [Header("Trajectory")]
    [SerializeField]
    private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private float arcHeight = 0.75f;

    public override Vector3 EvaluatePosition(
        Vector3 startPosition,
        ParkourTarget target,
        float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);

        float movementT =
            movementCurve.Evaluate(t);

        Vector3 basePosition =
            Vector3.Lerp(
                startPosition,
                target.LandingPosition,
                movementT
            );

        float arc =
            Mathf.Sin(t * Mathf.PI) *
            arcHeight;

        return basePosition +
               Vector3.up * arc;
    }
} 