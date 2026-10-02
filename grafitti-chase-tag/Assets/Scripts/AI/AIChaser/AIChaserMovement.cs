using UnityEngine;

public class AIChaserMovement
{
    public bool ShouldSprint(
        AIContext context,
        AIChaserState state)
    {
        if (state == AIChaserState.Search)
            return false;

        if (context.IsStaminaEmpty)
            return false;

        float distance =
            Vector3.Distance(
                context.Position,
                context.OpponentPosition
            );

        // Close enough that catching the target
        // is immediately important.
        if (distance < 8f &&
            context.NormalizedStamina > 0.4f)
        {
            return true;
        }

        // If the target is moving away,
        // increase pursuit intensity.
        Vector3 toOpponent =
            context.OpponentPosition -
            context.Position;

        toOpponent.y = 0f;

        if (toOpponent.sqrMagnitude > 0.01f)
        {
            Vector3 targetVelocity =
                context.OpponentVelocity;

            targetVelocity.y = 0f;

            if (targetVelocity.sqrMagnitude > 0.01f)
            {
                float movementAlignment =
                    Vector3.Dot(
                        targetVelocity.normalized,
                        toOpponent.normalized
                    );

                if (movementAlignment > 0.5f &&
                    context.NormalizedStamina > 0.6f)
                {
                    return true;
                }
            }
        }

        return false;
    }
}