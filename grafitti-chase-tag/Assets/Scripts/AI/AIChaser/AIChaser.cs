using UnityEngine;

public class AIChaser
{
    private AIChaserState state;

    private readonly AISteering steering;
    private readonly AINavigation navigation;
    private readonly AIChaserDestination destination;

    private readonly float destinationRepathDistance;

    private Vector3 lastPathDestination;
    private bool hasPathDestination;

    private float pathRetryTimer;

    private readonly float pathRetryInterval;

    private readonly AIChaserMovement movement;

    private bool isDescendingPartialPath;
    private bool hasLeftPartialPathSurface;
    private bool isAscendingPartialPath;

    public AIChaser(
        AISteering steering,
        AINavigation navigation,
        AIChaserDestination destination,
        AIChaserMovement movement,
        float destinationRepathDistance,
        float pathRetryInterval)
    {
        this.steering = steering;
        this.navigation = navigation;
        this.destination = destination;
        this.destinationRepathDistance =
            destinationRepathDistance;
        this.pathRetryInterval =
            pathRetryInterval;
        this.movement = movement;

        state = AIChaserState.Pursue;
    }

    public void Update(
        AIContext context,
        CharacterActionController actionController,
        bool lowObstacleAhead)
    {
        pathRetryTimer -= Time.deltaTime;

        switch (state)
        {
            case AIChaserState.Pursue:
                UpdatePursue(
                    context,
                    actionController,
                    lowObstacleAhead
                );
                break;

            case AIChaserState.Intercept:
                UpdateIntercept(
                    context,
                    actionController,
                    lowObstacleAhead
                );
                break;

            case AIChaserState.Search:
                UpdateSearch(
                    context,
                    actionController
                );
                break;
        }
    }

    public void SetState(AIChaserState newState)
    {
        if (state == newState)
            return;

        state = newState;

        // Invalidate the previous destination.
        hasPathDestination = false;
        lastPathDestination = Vector3.zero;

        // Allow the new state to build a path immediately.
        pathRetryTimer = 0f;

        // Remove the previous state's NavMesh path.
        navigation.ClearPath();
    }

    public AIChaserState CurrentState => state;

    public bool HasReachedPartialPathEnd()
    {
        return navigation.ReachedPartialPathEnd;
    }

    public bool IsAscendingPartialPath()
    {
        return isAscendingPartialPath;
    }

    
    private void UpdatePursue(
        AIContext context,
        CharacterActionController actionController,
        bool lowObstacleAhead)
    {
        Vector3 destination =
            this.destination.Calculate(
                context,
                AIChaserState.Pursue
            );
        
        bool sprint =
            movement.ShouldSprint(
                context,
                AIChaserState.Pursue,
                lowObstacleAhead
            );

        MoveTowardDestination(
            context,
            actionController,
            destination,
            sprint
        );
    }

    private void UpdateIntercept(
        AIContext context,
        CharacterActionController actionController,
        bool lowObstacleAhead)
    {
        Vector3 destination =
            this.destination.Calculate(
                context,
                AIChaserState.Intercept
            );

        bool sprint =
            movement.ShouldSprint(
                context,
                AIChaserState.Intercept,
                lowObstacleAhead
            );

        MoveTowardDestination(
            context,
            actionController,
            destination,
            sprint
        );
    }

    private void UpdateSearch(
        AIContext context,
        CharacterActionController actionController)
    {
        if (context.OpponentVisible)
        {
            Stop(actionController);
            return;
        }

        Vector3 destination =
            this.destination.Calculate(
                context,
                AIChaserState.Search
            );

        MoveTowardDestination(
            context,
            actionController,
            destination,
            false
        );
    }

    private void MoveTowardDestination(
        AIContext context,
        CharacterActionController actionController,
        Vector3 destination,
        bool sprint)
    {
        if (isDescendingPartialPath)
        {
            Vector3 dropDirection =
                destination - context.Position;

            dropDirection.y = 0f;

            if (dropDirection.sqrMagnitude > 1f)
            {
                dropDirection.Normalize();
            }

            actionController.SetMovementIntent(
                new CharacterMovementIntent(
                    dropDirection,
                    sprint,
                    Vector2.up
                )
            );

            if (!context.IsGrounded)
            {
                hasLeftPartialPathSurface = true;
            }

            if (hasLeftPartialPathSurface &&
                context.IsGrounded)
            {
                isDescendingPartialPath = false;
                hasLeftPartialPathSurface = false;
                hasPathDestination = false;
            }

            return;
        }

        bool destinationChanged =
            !hasPathDestination ||
            !navigation.HasPath ||
            Vector3.Distance(
                lastPathDestination,
                destination
            ) >= destinationRepathDistance;

        if (destinationChanged && pathRetryTimer <= 0f)
        {
            bool pathBuilt =
                navigation.TryBuildPath(
                    context.Position,
                    destination
                );

            if (pathBuilt)
            {
                lastPathDestination = destination;
                hasPathDestination = true;

                // A complete NavMesh path means the AI no longer
                // needs the upward-climb recovery for a partial path.
                if (!navigation.IsPartialPath)
                {
                    isAscendingPartialPath = false;
                }

                pathRetryTimer = 0f;
            }

            else
            {
                pathRetryTimer =
                    pathRetryInterval;
            }
        }

        bool waypointUpdated =
            navigation.UpdateWaypoint(context.Position);
        
        if (state == AIChaserState.Search)
        {
            navigation.DrawDebugPath(
                context.Position,
                new Color(0.54f, 0.17f, 0.89f, 1f)
            );
        }

        if (!waypointUpdated)
        {
            if (navigation.ReachedPartialPathEnd)
            {
                float verticalDifference =
                    destination.y - context.Position.y;

                bool targetIsBelow =
                    verticalDifference < -1f;

                bool targetIsAbove =
                    verticalDifference > 1f;

                if (targetIsAbove)
                {
                    isAscendingPartialPath = true;
                }

                Vector3 toTarget =
                    destination - context.Position;

                toTarget.y = 0f;

                if (targetIsBelow)
                {
                    Vector3 dropDirection =
                        destination - context.Position;

                    dropDirection.y = 0f;

                    if (dropDirection.sqrMagnitude > 1f)
                    {
                        dropDirection.Normalize();
                    }

                    isDescendingPartialPath = true;
                    hasLeftPartialPathSurface = false;

                    CharacterMovementIntent dropIntent =
                        new CharacterMovementIntent(
                            dropDirection,
                            sprint,
                            Vector2.up
                        );

                    actionController.SetMovementIntent(
                        dropIntent
                    );

                    return;
                }
                
                if (!targetIsAbove && !targetIsBelow)
                {
                    // Rebuild from the AI's current position.
                    // The destination may now be reachable from a different route.
                    bool recoveryPathBuilt = navigation.TryBuildPath(
                        context.Position,
                        destination
                    );

                    if (recoveryPathBuilt)
                    {
                        lastPathDestination = destination;
                        hasPathDestination = true;
                        pathRetryTimer = 0f;
                        return;
                    }

                    // No alternate path was found. Avoid repeatedly pushing
                    // directly into the same obstacle.
                    hasPathDestination = false;
                    pathRetryTimer = pathRetryInterval;

                    Stop(actionController);
                    return;
                }

            }

            Stop(actionController);
            return;
        }

        CharacterMovementIntent intent =
            steering.CreateMoveTowardIntent(
                context.Position,
                navigation.CurrentWaypoint,
                sprint
            );
        
        if (context.OpponentVisible)
        {
            Vector3 lookDirection =
                context.OpponentPosition - context.Position;

            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                intent.LookDirection = lookDirection.normalized;
                intent.HasLookDirection = true;
            }
        }

        actionController.SetMovementIntent(intent);
    }

    private void Stop(
        CharacterActionController actionController)
    {
        actionController.SetMovementIntent(
            new CharacterMovementIntent(
                Vector3.zero,
                false
            )
        );
    }

    public void ResetForNewRound()
    {
        state = AIChaserState.Pursue;

        lastPathDestination = Vector3.zero;
        hasPathDestination = false;
        pathRetryTimer = 0f;

        isDescendingPartialPath = false;
        hasLeftPartialPathSurface = false;
        isAscendingPartialPath = false;

        navigation.ClearPath();

        Debug.Log("AI CHASER RESET | Ready for new round");
    }
}