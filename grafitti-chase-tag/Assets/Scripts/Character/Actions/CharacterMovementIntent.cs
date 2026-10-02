using UnityEngine;

public struct CharacterMovementIntent
{
    public Vector3 WorldDirection;
    public bool Sprint;

    public CharacterMovementIntent(
        Vector3 worldDirection,
        bool sprint)
    {
        WorldDirection = worldDirection;
        Sprint = sprint;
    }
}