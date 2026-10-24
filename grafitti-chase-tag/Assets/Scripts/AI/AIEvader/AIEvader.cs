using UnityEngine;
using UnityEngine.AI;


public class AIEvader
{
    private enum DropPhase
    {
        None,
        Approach,
        StepOff,
        Falling
    }

    private DropPhase dropPhase = DropPhase.None;

    private Vector3 dropApproachPoint;
    private Vector3 dropLandingPoint;
    private Vector3 dropDirection;

    private float dropStartHeight;
    private float dropPhaseTimer;
    private float dropCooldownTimer;

    private const float DropCooldown = 2f;
    private const float MinimumUsefulEscapeGain = 1f;
    private const float StepOffDistance = 1.5f;
    private const float StepOffTimeout = 1.5f;
    private const float DropRecoveryTimeout = 4f;
    private const float MinimumConfirmedDropHeight = 0.75f;
    private const float DropApproachTimeout = 3f;

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

    private readonly AIEvaderDropPlanner dropPlanner;
    private readonly Transform selfTransform;
    private readonly LayerMask dropLandingMask;
    private readonly float maximumDropHeight;
    private readonly float dropApproachDistance;

    public AIEvader(
        AIEvaderDecision decision,
        AIEvaderDestination destination,
        AIEvaderMovement movement,
        AIEvaderDropPlanner dropPlanner,
        Transform selfTransform,
        LayerMask dropLandingMask,
        float maximumDropHeight,
        float dropApproachDistance)
    {
        this.decision = decision;
        this.destination = destination;
        this.movement = movement;

        this.dropPlanner = dropPlanner;
        this.selfTransform = selfTransform;
        this.dropLandingMask = dropLandingMask;
        this.maximumDropHeight = maximumDropHeight;
        this.dropApproachDistance = dropApproachDistance;
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
    
            bool landingOnNavMesh = NavMesh.SamplePosition(
                context.Position,
                out NavMeshHit landingHit,
                0.1f,
                NavMesh.AllAreas);

            float landingSampleDistance = landingOnNavMesh
                ? Vector3.Distance(context.Position, landingHit.position)
                : -1f;

            Debug.Log(
                $"EVADER LANDING NAVMESH CHECK | " +
                $"Actual={context.Position} | " +
                $"Grounded={context.IsGrounded} | " +
                $"Found={landingOnNavMesh} | " +
                $"Sampled={(landingOnNavMesh ? landingHit.position.ToString() : "NONE")} | " +
                $"Distance={landingSampleDistance:F2}");

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

        dropCooldownTimer = Mathf.Max(
            0f,
            dropCooldownTimer - Time.deltaTime);

        if (UpdateDropPhase(context, actionController))
            return;

        Vector3 dropThreatPosition =
            decision.GetThreatReference(context);

        if (TryStartDrop(
            context,
            actionController,
            dropThreatPosition))
        {
            return;
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
        
        Debug.Log(
            $"EVADER REPLAN CHECK | " +
            $"NeedsDestination={movement.NeedsDestination} | " +
            $"CanRetry={movement.CanRetry} | " +
            $"HasPath={movement.HasNavigationPath} | " +
            $"ShouldFind={shouldFindDestination} | " +
            $"ThreatVisible={context.OpponentVisible}");

        bool shouldReevaluateEscape =
            context.OpponentVisible &&
            movement.HasNavigationPath &&
            escapeReevaluationTimer <= 0f;

        if (shouldRecheckThreat ||
            shouldFindDestination ||
            shouldReevaluateEscape)
        {
            Debug.Log(
                $"EVADER REPLAN START | " +
                $"ShouldRecheckThreat={shouldRecheckThreat} | " +
                $"ShouldFindDestination={shouldFindDestination} | " +
                $"ShouldReevaluateEscape={shouldReevaluateEscape} | " +
                $"Position={context.Position} | " +
                $"ThreatPosition={threatPosition}");
    
            bool foundDestination = destination.TryCalculate(
                context,
                threatPosition,
                out Vector3 escapeDestination);
            
            Debug.Log(
                $"EVADER REPLAN RESULT | " +
                $"FoundDestination={foundDestination} | " +
                $"Candidate={escapeDestination} | " +
                $"Position={context.Position}");

            if (foundDestination)
            {
                bool destinationAccepted = movement.TrySetDestination(
                    context.Position,
                    escapeDestination);
                
                Debug.Log(
                    $"EVADER DESTINATION ACCEPTANCE | " +
                    $"Accepted={destinationAccepted} | " +
                    $"Position={context.Position} | " +
                    $"Destination={escapeDestination} | " +
                    $"HasPath={movement.HasNavigationPath} | " +
                    $"NeedsDestination={movement.NeedsDestination}");

                Debug.Log(
                    $"EVADER DESTINATION RESULT | " +
                    $"Accepted={destinationAccepted} | " +
                    $"Start={context.Position} | " +
                    $"Target={escapeDestination} | " +
                    $"HasPath={movement.HasNavigationPath} | " +
                    $"NeedsDestination={movement.NeedsDestination}");

                if (!destinationAccepted)
                {
                    Debug.Log("EVADER DESTINATION REJECTED");
                    movement.RegisterFailedAttempt();
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

    private bool TryStartDrop(
        AIContext context,
        CharacterActionController actionController,
        Vector3 threatPosition)
    {
        if (dropPhase != DropPhase.None ||
            !context.IsGrounded ||
            !context.OpponentVisible ||
            dropCooldownTimer > 0f)
        {
            return false;
        }

        Vector3 escapeDirection = context.Position - threatPosition;
        escapeDirection.y = 0f;

        if (escapeDirection.sqrMagnitude < 0.001f)
            return false;

        dropCooldownTimer = DropCooldown;

        if (!dropPlanner.TryFindDropCandidate(
            context.Position,
            escapeDirection,
            selfTransform,
            dropLandingMask,
            maximumDropHeight,
            out Vector3 approachPoint,
            out Vector3 landingPoint))
        {
            return false;
        }

        float currentThreatDistance = Vector3.Distance(
            context.Position,
            threatPosition);

        float landingThreatDistance = Vector3.Distance(
            landingPoint,
            threatPosition);

        if (landingThreatDistance <
            currentThreatDistance + MinimumUsefulEscapeGain)
        {
            Debug.Log(
                $"EVADER DROP REJECTED | " +
                $"CurrentThreatDistance={currentThreatDistance:F2} | " +
                $"LandingThreatDistance={landingThreatDistance:F2}");

            return false;
        }

        Vector3 outwardDirection = landingPoint - approachPoint;
        outwardDirection.y = 0f;

        if (outwardDirection.sqrMagnitude < 0.001f)
            return false;

        outwardDirection.Normalize();

        if (!movement.TrySetDestination(
            context.Position,
            approachPoint))
        {
            Debug.Log("EVADER DROP REJECTED | Approach path failed");
            return false;
        }

        dropApproachPoint = approachPoint;
        dropLandingPoint = landingPoint;
        dropDirection = outwardDirection;
        dropPhase = DropPhase.Approach;
        dropPhaseTimer = 0f;

        Debug.Log(
            $"EVADER DROP STARTED | " +
            $"Approach={dropApproachPoint} | " +
            $"Landing={dropLandingPoint} | " +
            $"CurrentThreatDistance={currentThreatDistance:F2} | " +
            $"LandingThreatDistance={landingThreatDistance:F2}");

        return true;
    }

    private bool UpdateDropPhase(
        AIContext context,
        CharacterActionController actionController)
    {
        if (dropPhase == DropPhase.None)
            return false;

        dropPhaseTimer += Time.deltaTime;

        switch (dropPhase)
        {
            case DropPhase.Approach:
            {
                if (!context.IsGrounded)
                {
                    AbortDrop("Lost grounding during approach");
                    return false;
                }

                Vector3 currentXZ = context.Position;
                currentXZ.y = 0f;

                Vector3 approachXZ = dropApproachPoint;
                approachXZ.y = 0f;

                float distanceToApproach =
                    Vector3.Distance(currentXZ, approachXZ);

                // Reached the edge approach point.
                if (distanceToApproach <= dropApproachDistance)
                {
                    movement.InvalidateDestination();

                    dropStartHeight = context.Position.y;
                    dropPhaseTimer = 0f;
                    dropPhase = DropPhase.StepOff;

                    Debug.Log(
                        $"EVADER DROP STEP-OFF | " +
                        $"Position={context.Position} | " +
                        $"Approach={dropApproachPoint} | " +
                        $"Distance={distanceToApproach:F2} | " +
                        $"Direction={dropDirection}");

                    return true;
                }

                // Prevent a failed or stalled approach from locking
                // the Evader out of its normal escape behavior.
                if (dropPhaseTimer >= DropApproachTimeout)
                {
                    Debug.Log(
                        $"EVADER DROP APPROACH TIMEOUT | " +
                        $"Position={context.Position} | " +
                        $"Approach={dropApproachPoint} | " +
                        $"Distance={distanceToApproach:F2} | " +
                        $"HasPath={movement.HasNavigationPath} | " +
                        $"Waypoint={movement.CurrentWaypoint}");

                    AbortDrop("Approach timed out");
                    return false;
                }

                // Recover the edge route if its waypoint/path was lost.
                if (!movement.HasNavigationPath)
                {
                    bool rebuilt = movement.TrySetDestination(
                        context.Position,
                        dropApproachPoint);

                    if (!rebuilt)
                    {
                        Debug.Log(
                            $"EVADER DROP APPROACH PATH REBUILD FAILED | " +
                            $"Position={context.Position} | " +
                            $"Approach={dropApproachPoint}");

                        AbortDrop("Approach path lost");
                        return false;
                    }
                }

                // Continue following the existing NavMesh path.
                movement.Update(
                    context,
                    actionController,
                    false);

                return true;
            }

            case DropPhase.StepOff:
            {
                if (!context.IsGrounded)
                {
                    dropPhase = DropPhase.Falling;
                    dropPhaseTimer = 0f;

                    actionController.SetMovementIntent(
                        new CharacterMovementIntent(
                            Vector3.zero,
                            false,
                            Vector2.zero));

                    Debug.Log(
                        $"EVADER DROP FALLING | " +
                        $"Position={context.Position} | " +
                        $"VelocityY={context.Velocity.y:F2}");

                    return true;
                }

                if (dropPhaseTimer >= StepOffTimeout)
                {
                    AbortDrop("Failed to step off edge");
                    return false;
                }

                Vector3 stepOffTarget =
                    context.Position + dropDirection * StepOffDistance;

                movement.SetMovementTowardPosition(
                    context.Position,
                    stepOffTarget,
                    actionController);

                return true;
            }

            case DropPhase.Falling:
            {
                actionController.SetMovementIntent(
                    new CharacterMovementIntent(
                        Vector3.zero,
                        false,
                        Vector2.zero));

                bool droppedEnough =
                    context.Position.y <=
                    dropStartHeight - MinimumConfirmedDropHeight;

                if (context.IsGrounded && droppedEnough)
                {
                    Debug.Log(
                        $"EVADER DROP LANDED | " +
                        $"Position={context.Position} | " +
                        $"ExpectedLanding={dropLandingPoint}");

                    FinishDrop();
                    return true;
                }

                if (dropPhaseTimer >= DropRecoveryTimeout)
                {
                    if (context.IsGrounded)
                    {
                        // The AI has landed, but not at the expected
                        // vertical drop height. Resume safely.
                        Debug.Log(
                            "EVADER DROP RECOVERY | " +
                            "Grounded without confirmed drop height");

                        FinishDrop();
                        return true;
                    }

                    AbortDrop("Fall recovery timed out while airborne");
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    private void FinishDrop()
    {
        dropPhase = DropPhase.None;
        dropPhaseTimer = 0f;
        dropCooldownTimer = DropCooldown;

        movement.InvalidateDestination();

        threatRecheckTimer = 0f;
        escapeReevaluationTimer = 0f;
    }

    private void AbortDrop(string reason)
    {
        Debug.Log($"EVADER DROP ABORTED | Reason={reason}");

        dropPhase = DropPhase.None;
        dropPhaseTimer = 0f;
        dropCooldownTimer = DropCooldown;

        movement.InvalidateDestination();

        threatRecheckTimer = 0f;
        escapeReevaluationTimer = 0f;
    }

    public void ResetForNewRound()
    {
        CurrentState = AIEvaderState.Reposition;

        WantsToSprint = false;
        IsApproachingFinalCorner = false;

        dropPhase = DropPhase.None;
        dropApproachPoint = Vector3.zero;
        dropLandingPoint = Vector3.zero;
        dropDirection = Vector3.zero;

        dropStartHeight = 0f;
        dropPhaseTimer = 0f;
        dropCooldownTimer = 0f;

        threatRecheckTimer = 0f;
        escapeReevaluationTimer = 0f;
        postClimbRecoveryTimer = 0f;

        wasExecutingAction = false;
        wasExecutingClimb = false;
        postClimbRecoveryPending = false;

        movement.InvalidateDestination();

        Debug.Log("AI EVADER RESET | New round");
    }
}
