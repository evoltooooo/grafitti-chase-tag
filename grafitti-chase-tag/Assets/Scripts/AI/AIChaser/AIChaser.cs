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

        Debug.Log(
            $"AI STATE TRANSITION PATH RESET | " +
            $"PreviousState={state} | " +
            $"NewState={newState} | " +
            $"HasPath={navigation.HasPath}"
        );

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
        
        Debug.Log(
            $"AI SPRINT INTENT DEBUG | " +
            $"State=Pursue | " +
            $"LowObstacleAhead={lowObstacleAhead} | " +
            $"ShouldSprint={sprint} | " +
            $"Grounded={context.IsGrounded} | " +
            $"CurrentSprinting={context.IsSprinting}"
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
                    sprint
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
            
            Debug.Log(
                $"AI PATH DEBUG | " +
                $"State={state} | " +
                $"Start={context.Position} | " +
                $"Destination={destination} | " +
                $"PathBuilt={pathBuilt}"
            );

            if (pathBuilt)
            {
                lastPathDestination = destination;
                hasPathDestination = true;

                pathRetryTimer = 0f;
            }
            else
            {
                pathRetryTimer =
                    pathRetryInterval;
            }
        }

        Debug.Log(
            $"AI NAV AFTER VAULT CHECK | " +
            $"HasPath={navigation.HasPath} | " +
            $"Position={context.Position} | " +
            $"PartialEnd={navigation.ReachedPartialPathEnd}"
        );

        bool waypointUpdated =
            navigation.UpdateWaypoint(context.Position);
        
        if (state == AIChaserState.Search)
        {
            Debug.Log(
                $"AI SEARCH ROUTE DEBUG | " +
                $"Position={context.Position} | " +
                $"Waypoint={navigation.CurrentWaypoint} | " +
                $"Destination={destination} | " +
                $"HasPath={navigation.HasPath}"
            );
        }
        
        if (state == AIChaserState.Search)
        {
            navigation.DrawDebugPath(
                context.Position,
                new Color(0.54f, 0.17f, 0.89f, 1f)
            );
        }

        Debug.Log(
            $"AI NAV WAYPOINT RESULT | " +
            $"Updated={waypointUpdated} | " +
            $"HasPath={navigation.HasPath} | " +
            $"Waypoint={navigation.CurrentWaypoint} | " +
            $"Position={context.Position}"
        );


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

                Debug.Log(
                    $"AI NAV PARTIAL END | " +
                    $"TargetBelow={targetIsBelow} | " +
                    $"TargetAbove={targetIsAbove} | " +
                    $"VerticalDifference={verticalDifference:F2}"
                );

                Debug.Log(
                    $"AI NAV PARTIAL DIRECTION | " +
                    $"Direction={toTarget.normalized}"
                );

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
                            sprint
                        );

                    actionController.SetMovementIntent(
                        dropIntent
                    );

                    return;
                }
                
                if (!targetIsAbove && !targetIsBelow)
                {
                    Debug.Log(
                        $"AI NAV PARTIAL END | " +
                        $"Target at similar height. " +
                        $"VerticalDifference={verticalDifference:F2} | " +
                        $"Attempting path rebuild."
                    );

                    // Rebuild from the AI's current position.
                    // The destination may now be reachable from a different route.
                    bool recoveryPathBuilt = navigation.TryBuildPath(
                        context.Position,
                        destination
                    );

                    Debug.Log(
                        $"AI PARTIAL PATH RECOVERY | " +
                        $"PathBuilt={recoveryPathBuilt} | " +
                        $"HasPath={navigation.HasPath} | " +
                        $"Partial={navigation.IsPartialPath}"
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

        Debug.Log(
            $"AI PURSUIT INTENT | " +
            $"Sprint={intent.Sprint} | " +
            $"Waypoint={navigation.CurrentWaypoint} | " +
            $"Direction={intent.WorldDirection} | " +
            $"Input={intent.MovementInput} | " +
            $"Executing={context.IsExecutingAction}"
        );

        actionController.SetMovementIntent(intent);

        if (state == AIChaserState.Search)
        {
            Debug.Log(
                $"AI SEARCH MOVEMENT CHECK | " +
                $"Position={context.Position} | " +
                $"Waypoint={navigation.CurrentWaypoint} | " +
                $"IntentDirection={intent.WorldDirection} | " +
                $"Velocity={context.HorizontalVelocity} | " +
                $"Speed={context.HorizontalSpeed:F2}"
            );
        }
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
}