using UnityEngine;

public struct CharacterMovementIntent
{
    public Vector3 WorldDirection;
    public bool Sprint;
    public Vector2 MovementInput;
    public bool FaceCameraDirection;

    // Optional direction the character should face.
    public Vector3 LookDirection;
    public bool HasLookDirection;

    public CharacterMovementIntent(
        Vector3 worldDirection,
        bool sprint,
        Vector2 movementInput = default,
        bool faceCameraDirection = false,
        Vector3 lookDirection = default,
        bool hasLookDirection = false)
    {
        WorldDirection = worldDirection;
        Sprint = sprint;
        MovementInput = movementInput;
        FaceCameraDirection = faceCameraDirection;

        LookDirection = lookDirection;
        HasLookDirection = hasLookDirection;
    }
}