
using UnityEngine;

public class AIEvaderDecision
{
    private const float RecoverStaminaThreshold = 0.4f;

    public AIEvaderState Decide(AIContext context)
    {
        if (context.OpponentVisible)
            return AIEvaderState.Evade;

        if (context.NormalizedStamina <= RecoverStaminaThreshold)
            return AIEvaderState.Recover;

        return AIEvaderState.Reposition;
    }

    public Vector3 GetThreatReference(AIContext context)
    {
        if (context.OpponentVisible)
            return context.OpponentPosition;

        if (!float.IsInfinity(context.TimeSinceOpponentSeen))
            return context.LastKnownOpponentPosition;

        // No opponent information yet: initially move forward.
        return context.Position - context.Forward * 10f;
    }

    public bool ShouldSprint(
        AIContext context,
        AIEvaderState state,
        Vector3 threatPosition)
    {
        if (state != AIEvaderState.Evade ||
            context.IsStaminaEmpty ||
            context.NormalizedStamina <= RecoverStaminaThreshold)
        {
            return false;
        }

        float distance = Vector3.Distance(
            context.Position,
            threatPosition);

        return distance < 8f;
    }
}
