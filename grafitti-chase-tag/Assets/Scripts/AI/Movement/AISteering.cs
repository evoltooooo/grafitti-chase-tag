using UnityEngine;

public class AISteering
{
    public CharacterMovementIntent CreateMoveTowardIntent(
        Vector3 currentPosition,
        Vector3 targetPosition,
        bool sprint)
    {
        Vector3 direction =
            targetPosition - currentPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        return new CharacterMovementIntent(
            direction,
            sprint
        );
    }
}