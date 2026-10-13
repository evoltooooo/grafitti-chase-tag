
using UnityEngine;

public class AIEvader
{
    private readonly AIEvaderDecision decision;
    private readonly AIEvaderDestination destination;
    private readonly AIEvaderMovement movement;

    public AIEvaderState CurrentState { get; private set; } = AIEvaderState.Reposition;

    public bool WantsToSprint { get; private set; }

    private const float ThreatRecheckInterval = 0.2f;
    private float threatRecheckTimer;
    private bool wasExecutingAction;

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
        // Preserve shared action movement, but remember that an action ran.
        if (context.IsExecutingAction)
        {
            wasExecutingAction = true;
            return;
        }

        // Once an action finishes, discard the old escape route.
        if (wasExecutingAction)
        {
            movement.InvalidateDestination();
            threatRecheckTimer = 0f;
            wasExecutingAction = false;

            Debug.Log(
                "EVADER ACTION ENDED | Recalculating escape destination");
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
                Debug.Log(
                    $"EVADER DESTINATION FOUND | " +
                    $"Threat={threatPosition} | " +
                    $"EscapeDestination={escapeDestination}");

                if (movement.TrySetDestination(
                    context.Position,
                    escapeDestination))
                {
                    Debug.Log("EVADER DESTINATION ACCEPTED");
                    threatRecheckTimer = ThreatRecheckInterval;
                }
                else
                {
                    Debug.Log("EVADER DESTINATION REJECTED");
                }
            }
            else
            {
                Debug.Log(
                    $"EVADER DESTINATION FAILED | " +
                    $"Position={context.Position} | " +
                    $"Threat={threatPosition}");

                if (movement.NeedsDestination)
                    movement.RegisterFailedAttempt();

                threatRecheckTimer = 0.1f;
            }
        }

        WantsToSprint = decision.ShouldSprint(
            context,
            CurrentState,
            threatPosition);

        movement.Update(
            context,
            actionController,
            WantsToSprint);
    }
}
