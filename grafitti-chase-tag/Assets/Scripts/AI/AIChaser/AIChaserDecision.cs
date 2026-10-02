using UnityEngine;

public class AIChaserDecision
{
    public AIChaserState Decide(AIContext context)
    {
        if (!context.OpponentVisible)
            return AIChaserState.Search;

        Vector3 toOpponent =
            context.OpponentPosition -
            context.Position;

        toOpponent.y = 0f;

        float distance = toOpponent.magnitude;

        if (distance < 3f)
            return AIChaserState.Pursue;

        Vector3 targetVelocity =
            context.OpponentVelocity;

        targetVelocity.y = 0f;

        float targetSpeed =
            targetVelocity.magnitude;

        if (targetSpeed < 1f)
            return AIChaserState.Pursue;

        Vector3 directionToOpponent =
            toOpponent.normalized;

        float targetMovementAlignment =
            Vector3.Dot(
                targetVelocity.normalized,
                directionToOpponent
            );

        if (targetMovementAlignment > 0.5f)
            return AIChaserState.Intercept;

        return AIChaserState.Pursue;
    }
}