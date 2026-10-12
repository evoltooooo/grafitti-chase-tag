
using UnityEngine;

public class AIEvader
{
    private readonly AIEvaderDecision decision;
    private readonly AIEvaderDestination destination;
    private readonly AIEvaderMovement movement;
    public AIEvaderState CurrentState { get; private set; } = AIEvaderState.Reposition;

    private const float ThreatRecheckInterval = 0.2f;
    private float threatRecheckTimer;

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
        // Do not overwrite movement intent while a shared action runs.
        if (context.IsExecutingAction)
            return;

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

        bool shouldRecheckThreat =
            context.OpponentVisible &&
            threatRecheckTimer <= 0f;

        bool shouldFindDestination =
            movement.NeedsDestination &&
            movement.CanRetry;

        if (shouldRecheckThreat || shouldFindDestination)
        {
            if (destination.TryCalculate(
                context,
                threatPosition,
                out Vector3 escapeDestination))
            {
                if (movement.TrySetDestination(
                    context.Position,
                    escapeDestination))
                {
                    threatRecheckTimer = ThreatRecheckInterval;
                }
            }
            else
            {
                // Keep the existing path if one is still valid.
                // Retry sooner than the next threat recheck.
                if (movement.NeedsDestination)
                    movement.RegisterFailedAttempt();

                threatRecheckTimer = 0.1f;
            }
        }

        bool sprint = decision.ShouldSprint(
            context,
            CurrentState,
            threatPosition);

        movement.Update(
            context,
            actionController,
            sprint);
    }
}
