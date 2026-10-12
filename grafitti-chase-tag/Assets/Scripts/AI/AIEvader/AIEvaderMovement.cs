
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

    public bool NeedsDestination =>
        !hasDestination || !navigation.HasPath;

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

        if (navigation.IsPartialPath)
        {
            navigation.ClearPath();
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
        if (!navigation.UpdateWaypoint(context.Position))
        {
            Stop(actionController);
            return;
        }

        CharacterMovementIntent intent =
            steering.CreateMoveTowardIntent(
                context.Position,
                navigation.CurrentWaypoint,
                sprint);

        actionController.SetMovementIntent(intent);
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
