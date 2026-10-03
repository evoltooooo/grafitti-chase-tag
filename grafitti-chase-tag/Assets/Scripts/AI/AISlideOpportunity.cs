using UnityEngine;

public class AISlideOpportunity
{
    private readonly float detectionDistance;
    private readonly float lowDetectionHeight;
    private readonly float highDetectionHeight;
    private readonly LayerMask obstacleMask;

    public AISlideOpportunity(
        float detectionDistance,
        float lowDetectionHeight,
        float highDetectionHeight,
        LayerMask obstacleMask)
    {
        this.detectionDistance = detectionDistance;
        this.lowDetectionHeight = lowDetectionHeight;
        this.highDetectionHeight = highDetectionHeight;
        this.obstacleMask = obstacleMask;
    }

    public bool HasLowObstacleAhead(
        AIContext context)
    {
        Vector3 direction =
            context.Forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return false;

        direction.Normalize();

        Vector3 lowOrigin =
            context.Position +
            Vector3.up * lowDetectionHeight;

        Vector3 highOrigin =
            context.Position +
            Vector3.up * highDetectionHeight;

        Debug.DrawRay(
            lowOrigin,
            direction * detectionDistance,
            Color.green,
            0f,
            false
        );

        Debug.DrawRay(
            highOrigin,
            direction * detectionDistance,
            Color.red,
            0f,
            false
        );

        Debug.DrawLine(
            lowOrigin,
            lowOrigin + Vector3.up * 0.1f,
            Color.green
        );

        Debug.DrawLine(
            highOrigin,
            highOrigin + Vector3.up * 0.1f,
            Color.red
        );

        bool lowBlocked =
            Physics.Raycast(
                lowOrigin,
                context.Forward,
               detectionDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );

        bool highBlocked =
            Physics.Raycast(
                highOrigin,
                context.Forward,
               detectionDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );

        Debug.Log(
            $"SLIDE RAYS | " +
            $"LowBlocked={lowBlocked} | " +
            $"HighBlocked={highBlocked}"
        );

        // Slide opportunity:
        //
        // Low space is clear
        // High space is blocked
        //
        // Meaning:
        // "Something is blocking the upper body,
        // but there is space underneath it."

        return !lowBlocked && highBlocked;
    }
}