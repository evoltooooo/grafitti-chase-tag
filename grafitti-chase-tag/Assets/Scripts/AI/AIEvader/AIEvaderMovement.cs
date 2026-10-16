using UnityEngine;

public class AIEvaderMovement
{
    private readonly AISteering steering;
    private readonly AINavigation navigation;

    private readonly float destinationRepathDistance;
    private readonly float pathRetryInterval;

    private Vector3 lastDestination;
    private bool hasDestination;
    private float retryTimer;

    private const float FinalCornerApproachDistance = 1.5f;

    public bool NeedsDestination =>
        !hasDestination || !navigation.HasPath;

    public bool HasPartialPathEndpoint =>
        navigation.IsPartialPath &&
        navigation.PartialPathEndpoint != Vector3.zero;

    public Vector3 PartialPathEndpoint =>
        navigation.PartialPathEndpoint;

    public bool ReachedPartialPathEnd =>
        navigation.ReachedPartialPathEnd;

    public bool CanRetry =>
        retryTimer <= 0f;

    public AIEvaderMovement(
        AISteering steering,
        AINavigation navigation,
        float destinationRepathDistance,
        float pathRetryInterval)
    {
        this.steering = steering;
        this.navigation = navigation;

        this.destinationRepathDistance =
            destinationRepathDistance;

        this.pathRetryInterval =
            pathRetryInterval;
    }

    public void UpdateTimer()
    {
        retryTimer -= Time.deltaTime;
    }

    public bool TrySetDestination(
        Vector3 startPosition,
        Vector3 destination)
    {
        bool destinationChanged =
            !hasDestination ||
            Vector3.Distance(
                lastDestination,
                destination) >= destinationRepathDistance;

        if (!destinationChanged && navigation.HasPath)
        {
            return true;
        }

        if (!navigation.TryBuildPath(
            startPosition,
            destination))
        {
            RegisterFailedAttempt();
            return false;
        }

        lastDestination = destination;
        hasDestination = true;
        retryTimer = 0f;

        Debug.Log(
            $"AI EVADER PATH BUILT | " +
            $"Destination={destination}");

        return true;
    }

    public void InvalidateDestination()
    {
        navigation.ClearPath();

        hasDestination = false;
        lastDestination = Vector3.zero;
        retryTimer = 0f;

        Debug.Log("EVADER DESTINATION INVALIDATED");
    }

    public void RegisterFailedAttempt()
    {
        hasDestination = false;
        retryTimer = pathRetryInterval;
    }

    public void Update(
        AIContext context,
        CharacterActionController actionController,
        bool sprint)
    {
        Debug.Log(
            $"EVADER NAV CHECK | " +
            $"NeedsDestination={NeedsDestination} | " +
            $"CanRetry={CanRetry} | " +
            $"HasPath={navigation.HasPath} | " +
            $"Position={context.Position} | " +
            $"Destination={lastDestination}");

        bool waypointUpdated =
            navigation.UpdateWaypoint(context.Position);

        Debug.Log(
            $"EVADER WAYPOINT | " +
            $"Updated={waypointUpdated} | " +
            $"Waypoint={navigation.CurrentWaypoint}");

        if (!waypointUpdated)
        {
            Stop(actionController);
            return;
        }

        Debug.Log(
            $"EVADER APPROACH DEBUG | " +
            $"Position={context.Position} | " +
            $"Waypoint={navigation.CurrentWaypoint} | " +
            $"Direction={(navigation.CurrentWaypoint - context.Position).normalized} | " +
            $"FinalWaypoint={navigation.IsFinalWaypoint}");

        CharacterMovementIntent intent =
            steering.CreateMoveTowardIntent(
                context.Position,
                navigation.CurrentWaypoint,
                sprint);

        Debug.Log(
            $"EVADER INTENT | " +
            $"WorldDirection={intent.WorldDirection} | " +
            $"MovementInput={intent.MovementInput} | " +
            $"Sprint={intent.Sprint}");

        actionController.SetMovementIntent(intent);
    }

    public bool IsApproachingFinalCorner(Vector3 position)
    {
        if (!navigation.IsFinalWaypoint)
            return false;

        return Vector3.Distance(
            position,
            navigation.CurrentWaypoint
        ) <= FinalCornerApproachDistance;
    }

    private void Stop(
        CharacterActionController actionController)
    {
        hasDestination = false;

        actionController.SetMovementIntent(
            new CharacterMovementIntent(
                Vector3.zero,
                false,
                Vector2.zero));
    }
}
