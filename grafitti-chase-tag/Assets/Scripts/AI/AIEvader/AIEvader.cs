using UnityEngine;

public class AIEvader
{
    private readonly AIEvaderDecision decision;
    private readonly AIEvaderDestination destination;
    private readonly AIEvaderMovement movement;

    public AIEvaderState CurrentState { get; private set; } =
        AIEvaderState.Reposition;

    public bool WantsToSprint { get; private set; }

    public bool IsApproachingFinalCorner { get; private set; }

    private const float ThreatRecheckInterval = 0.2f;
    private const float EscapeReevaluationInterval = 0.75f;
    private const float PostClimbRecoveryTimeout = 2.5f;

    private float threatRecheckTimer;
    private float escapeReevaluationTimer;

    private float postClimbRecoveryTimer;

    private bool wasExecutingAction;
    private bool wasExecutingClimb;
    private bool postClimbRecoveryPending;

    public AIEvader(
        AIEvaderDecision decision,
        AIEvaderDestination destination,
        AIEvaderMovement movement)
    {
        this.decision = decision;
        this.destination = destination;
        this.movement = movement;
    }

    public void Update(
        AIContext context,
        CharacterActionController actionController)
    {
        // Actions retain control of movement while executing.
        if (context.IsExecutingAction)
        {
            wasExecutingAction = true;

            if (context.CurrentActionType == CharacterActionType.Climb)
                wasExecutingClimb = true;

            return;
        }

        // If Climb ended in midair, postpone navigation recovery.
        if (wasExecutingAction)
        {
            wasExecutingAction = false;

            bool climbEnded = wasExecutingClimb;
            wasExecutingClimb = false;

            if (climbEnded && !context.IsGrounded)
            {
                postClimbRecoveryPending = true;
                postClimbRecoveryTimer = 0f;

                actionController.SetMovementIntent(
                    new CharacterMovementIntent(
                        Vector3.zero,
                        false,
                        Vector2.zero));

                Debug.Log(
                    $"EVADER CLIMB RECOVERY DEFERRED | " +
                    $"Position={context.Position} | " +
                    $"VelocityY={context.Velocity.y:F2} | " +
                    $"Grounded={context.IsGrounded}");

                return;
            }

            // The action ended while grounded, or it was not Climb.
            movement.InvalidateDestination();
            threatRecheckTimer = 0f;

            Debug.Log(
                $"EVADER ACTION ENDED | " +
                $"Position={context.Position} | " +
                $"Grounded={context.IsGrounded} | " +
                $"Recalculating escape destination");
        }

        // Do not let normal navigation compete with recovery.
        if (postClimbRecoveryPending)
        {
            postClimbRecoveryTimer += Time.deltaTime;

            bool recoveryGrounded = context.IsGrounded;
            bool recoveryTimedOut =
                postClimbRecoveryTimer >= PostClimbRecoveryTimeout;

            if (!recoveryGrounded && !recoveryTimedOut)
            {
                actionController.SetMovementIntent(
                    new CharacterMovementIntent(
                        Vector3.zero,
                        false,
                        Vector2.zero));

                return;
            }

            postClimbRecoveryPending = false;
            postClimbRecoveryTimer = 0f;

            movement.InvalidateDestination();
            threatRecheckTimer = 0f;

            Debug.Log(
                $"EVADER CLIMB RECOVERY RESUMED | " +
                $"Position={context.Position} | " +
                $"Grounded={context.IsGrounded} | " +
                $"TimedOut={recoveryTimedOut}");
        }

        AIEvaderState desiredState = decision.Decide(context);

        if (desiredState != CurrentState)
        {
            Debug.Log(
                $"AI EVADER STATE CHANGE | " +
                $"{CurrentState} -> {desiredState}");

            CurrentState = desiredState;
        }

        Vector3 threatPosition =
            decision.GetThreatReference(context);

        movement.UpdateTimer();

        threatRecheckTimer -= Time.deltaTime;
        escapeReevaluationTimer -= Time.deltaTime;

        bool shouldRecheckThreat =
            context.OpponentVisible &&
            threatRecheckTimer <= 0f &&
            movement.NeedsDestination;

        bool shouldFindDestination =
            movement.NeedsDestination &&
            movement.CanRetry;

        bool shouldReevaluateEscape =
            context.OpponentVisible &&
            movement.HasNavigationPath &&
            escapeReevaluationTimer <= 0f;

        if (shouldRecheckThreat ||
            shouldFindDestination ||
            shouldReevaluateEscape)
        {
            bool foundDestination = destination.TryCalculate(
                context,
                threatPosition,
                out Vector3 escapeDestination);

            if (foundDestination)
            {
                bool destinationAccepted = movement.TrySetDestination(
                    context.Position,
                    escapeDestination);

                if (!destinationAccepted)
                {
                    Debug.Log("EVADER DESTINATION REJECTED");
                }

                threatRecheckTimer = ThreatRecheckInterval;

                if (context.OpponentVisible)
                {
                    escapeReevaluationTimer =
                        EscapeReevaluationInterval;
                }
            }
            else
            {
                Debug.Log(
                    $"EVADER DESTINATION FAILED | " +
                    $"Position={context.Position} | " +
                    $"Threat={threatPosition}");

                if (movement.NeedsDestination)
                {
                    movement.RegisterFailedAttempt();
                }

                threatRecheckTimer = 0.1f;

                if (context.OpponentVisible &&
                    movement.HasNavigationPath)
                {
                    escapeReevaluationTimer = 0.2f;
                }
            }
        }

        WantsToSprint = decision.ShouldSprint(
            context,
            CurrentState,
            threatPosition);

        IsApproachingFinalCorner =
            movement.IsApproachingFinalCorner(context.Position);

        movement.Update(
            context,
            actionController,
            WantsToSprint);
    }
}
