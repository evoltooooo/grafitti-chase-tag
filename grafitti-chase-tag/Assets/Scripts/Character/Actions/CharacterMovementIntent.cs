using UnityEngine;

public struct CharacterMovementIntent
{
    public Vector3 WorldDirection;
    public bool Sprint;
    public Vector2 MovementInput;
    public bool FaceCameraDirection;

    public CharacterMovementIntent(
        Vector3 worldDirection,
        bool sprint,
        Vector2 movementInput = default,
        bool faceCameraDirection = false)
    {
        WorldDirection = worldDirection;
        Sprint = sprint;
        MovementInput = movementInput;
        FaceCameraDirection = faceCameraDirection;
    }
}