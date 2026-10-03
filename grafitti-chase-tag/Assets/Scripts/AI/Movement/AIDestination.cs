using UnityEngine;

public struct AIDestination
{
    public Vector3 Position;
    public bool HasDestination;

    public AIDestination(Vector3 position)
    {
        Position = position;
        HasDestination = true;
    }

    public static AIDestination None =>
        new AIDestination
        {
            Position = Vector3.zero,
            HasDestination = false
        };
}