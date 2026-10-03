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
        CharacterActionController actionController)
    {
        pathRetryTimer -= Time.deltaTime;

        switch (state)
        {
            case AIChaserState.Pursue:
                UpdatePursue(
                    context,
                    actionController
                );
                break;

            case AIChaserState.Intercept:
                UpdateIntercept(
                    context,
                    actionController
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

        hasPathDestination = false;
        lastPathDestination = Vector3.zero;
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
        CharacterActionController actionController)
    {
        Vector3 destination =
            this.destination.Calculate(
                context,
                AIChaserState.Pursue
            );

        bool sprint =
            movement.ShouldSprint(
                context,
                AIChaserState.Pursue
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
        CharacterActionController actionController)
    {
        Vector3 destination =
            this.destination.Calculate(
                context,
                AIChaserState.Intercept
            );

        bool sprint =
            movement.ShouldSprint(
                context,
                AIChaserState.Intercept
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

        if (!navigation.UpdateWaypoint(context.Position))
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
}