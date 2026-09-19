using UnityEngine;

public struct ParkourTarget
{
    public ParkourActionType ActionType;

    // Character movement points
    public Vector3 TakeoffPosition;
    public Vector3 LandingPosition;

    // Environment interaction point
    public Vector3 InteractionPosition;

    // Surface information
    public Vector3 PivotPosition;
    public Vector3 SurfaceNormal;

    // Character orientation
    public Quaternion TargetRotation;

    // Obstacle information
    public float ObstacleHeight;
    public float ObstacleDistance;

    public bool IsValid;

    public ParkourTarget(
        ParkourActionType actionType,
        Vector3 takeoffPosition,
        Vector3 landingPosition,
        Vector3 interactionPosition,
        Vector3 pivotPosition,
        Vector3 surfaceNormal,
        Quaternion targetRotation,
        float obstacleHeight,
        float obstacleDistance)
    {
        ActionType = actionType;

        TakeoffPosition = takeoffPosition;
        LandingPosition = landingPosition;

        InteractionPosition =
            interactionPosition;

        SurfaceNormal =
            surfaceNormal;

        TargetRotation =
            targetRotation;

        ObstacleHeight =
            obstacleHeight;

        ObstacleDistance =
            obstacleDistance;

        PivotPosition = 
            pivotPosition;

        IsValid = true;
    }
}