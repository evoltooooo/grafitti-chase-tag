using UnityEngine;

public class AIIntercept
{
    public Vector3 CalculateInterceptPosition(
        Vector3 selfPosition,
        float selfSpeed,
        Vector3 targetPosition,
        Vector3 targetVelocity,
        float minLeadTime,
        float maxLeadTime)
    {
        if (selfSpeed <= 0.01f)
            selfSpeed = 1f;

        Vector3 horizontalTargetVelocity =
            new Vector3(
                targetVelocity.x,
                0f,
                targetVelocity.z
            );

        Vector3 toTarget =
            targetPosition - selfPosition;

        toTarget.y = 0f;

        float distance =
            toTarget.magnitude;

        float leadTime =
            distance / selfSpeed;

        leadTime = Mathf.Clamp(
            leadTime,
            minLeadTime,
            maxLeadTime
        );

        Vector3 result =
            targetPosition +
            horizontalTargetVelocity * leadTime;

        result.y = selfPosition.y;

        return result;
    }
}