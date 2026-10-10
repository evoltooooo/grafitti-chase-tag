using UnityEngine;

public class AIChaserMovement
{
    public bool ShouldSprint(
        AIContext context,
        AIChaserState state,
        bool lowObstacleAhead = false)
    {
        if (context.IsStaminaEmpty)
        return false;

        // Prepare for a Slide when a low obstacle is detected.
        // This must come before the Search-state restriction.
        if (lowObstacleAhead &&
            context.IsGrounded &&
            context.NormalizedStamina > 0.4f)
        {
            return true;
        }

        // Normally, do not sprint while searching.
        if (state == AIChaserState.Search)
            return false;

        float distance =
            Vector3.Distance(
                context.Position,
                context.OpponentPosition
            );

        // Sprint when close enough to catch the target.
        if (distance < 8f &&
            context.NormalizedStamina > 0.4f)
        {
            return true;
        }

        // Sprint when the target is moving away.
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