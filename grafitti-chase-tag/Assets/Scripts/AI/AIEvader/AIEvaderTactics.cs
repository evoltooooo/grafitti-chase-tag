using UnityEngine;

public class AIEvaderTactics
{
    private readonly CharacterMotor characterMotor;
    private readonly AISlideOpportunity slideOpportunity;
    private readonly AIEvaderMovement movement;

    private readonly float slideCooldown;
    private float slideCooldownTimer;

    private readonly AIParkourOpportunity parkourOpportunity = new AIParkourOpportunity();
    private bool partialPathJumpRequested;

    private float climbCheckTimer;
    private float climbDebugTimer;
    private float climbRetryLockoutTimer;

    private const float ClimbRetryLockoutDuration = 1.0f;
    private const float ClimbCheckInterval = 0.15f;

    private const float JumpAscentThreshold = 0.1f;
    private const float JumpStartTimeout = 1.5f;

    private bool climbTraversalArmed;
    private float climbTraversalStartTime;

    private bool climbRequestPending;
    private bool climbActionObservedExecuting;

    public AIEvaderTactics(
        CharacterMotor characterMotor,
        AIEvaderMovement movement,
        float slideCooldown,
        float slideDetectionDistance,
        float slideLowDetectionHeight,
        float slideHighDetectionHeight,
        LayerMask slideObstacleMask)
    {
        this.characterMotor = characterMotor;
        this.movement = movement;
        this.slideCooldown = slideCooldown;

        slideOpportunity = new AISlideOpportunity(
            slideDetectionDistance,
            slideLowDetectionHeight,
            slideHighDetectionHeight,
            slideObstacleMask);
    }

    public void UpdateTimer()
    {
        slideCooldownTimer -= Time.deltaTime;
        climbCheckTimer -= Time.deltaTime;
        climbRetryLockoutTimer -= Time.deltaTime;
        climbDebugTimer -= Time.deltaTime;
    }

    private bool CheckPartialPathJump(
        AIContext context,
        CharacterActionController actionController)
    {
        if (climbTraversalArmed || climbRequestPending)
            return false;

        if (!context.IsGrounded ||
            context.IsExecutingAction ||
            context.IsStaminaEmpty)
        {
            return false;
        }

        if (climbRetryLockoutTimer > 0f)
            return false;

        if (!parkourOpportunity.TryFindClimb(
                actionController,
                out ParkourTarget target))
        {
            Debug.Log(
                $"AI EVADER CLIMB TARGET LOST OR INVALID | " +
                $"Position={context.Position} | " +
                $"VelocityY={context.Velocity.y:F2} | " +
                $"Grounded={context.IsGrounded} | " +
                $"Action={context.CurrentActionType}");

            return false;
        }

        Debug.Log(
            $"AI EVADER CLIMB TARGET CONFIRMED | " +
            $"Interaction={target.InteractionPosition} | " +
            $"Landing={target.LandingPosition} | " +
            $"VelocityY={context.Velocity.y:F2}");

        Debug.Log(
            $"AI EVADER CLIMB JUMP REQUEST | " +
            $"Position={context.Position} | " +
            $"HasPartialEndpoint={movement.HasPartialPathEndpoint} | " +
            $"ReachedPartialEnd={movement.ReachedPartialPathEnd} | " +
            $"Target={target.InteractionPosition}");

        bool jumpPerformed = actionController.RequestJump();

        Debug.Log(
            $"AI EVADER CLIMB JUMP RESULT | Accepted={jumpPerformed}");

        if (!jumpPerformed)
            return false;

        climbTraversalArmed = true;
        climbTraversalStartTime = Time.time;

        // Prevent immediate repeated Jump requests if the attempt times out.
        climbRetryLockoutTimer = ClimbRetryLockoutDuration;

        Debug.Log(
            "AI EVADER CLIMB JUMP ACCEPTED | " +
            "Waiting for upward movement");

        return true;
    }

    private bool CheckClimbOpportunity(
        AIContext context,
        CharacterActionController actionController)
    {
        Debug.Log(
            $"EVADER CLIMB CHECK ENTER | " +
            $"Armed={climbTraversalArmed} | " +
            $"Pending={climbRequestPending} | " +
            $"Grounded={context.IsGrounded} | " +
            $"VelocityY={context.Velocity.y:F2} | " +
            $"Executing={context.IsExecutingAction} | " +
            $"Action={context.CurrentActionType}");

        if (!climbTraversalArmed)
        {
            Debug.Log("EVADER CLIMB BLOCKED | Traversal not armed");
            return false;
        }

        if (climbRequestPending)
        {
            Debug.Log("EVADER CLIMB BLOCKED | Request pending");
            return false;
        }

        if (context.IsGrounded ||
            context.Velocity.y <= JumpAscentThreshold)
        {
            Debug.Log("EVADER CLIMB BLOCKED | Not ascending");
            return false;
        }

        if (climbCheckTimer > 0f)
        {
            Debug.Log(
                $"EVADER CLIMB BLOCKED | Timer={climbCheckTimer:F3}");
            return false;
        }

        climbCheckTimer = ClimbCheckInterval;

        if (context.IsExecutingAction &&
            context.CurrentActionType != CharacterActionType.Jump)
        {
            Debug.Log(
                $"EVADER CLIMB BLOCKED | Other action: " +
                $"{context.CurrentActionType}");
            return false;
        }

        if (context.IsStaminaEmpty)
        {
            Debug.Log("EVADER CLIMB BLOCKED | Stamina empty");
            return false;
        }

        Debug.Log("EVADER CLIMB CHECK | Trying to resolve target");

        if (!parkourOpportunity.TryFindClimb(
                actionController,
                out ParkourTarget target))
        {
            Debug.Log(
                "EVADER CLIMB BLOCKED | Target resolution failed");
            return false;
        }

        Debug.Log(
            $"EVADER CLIMB TARGET FOUND | " +
            $"Interaction={target.InteractionPosition} | " +
            $"Landing={target.LandingPosition} | " +
            $"VelocityY={context.Velocity.y:F2}");

        bool performed = actionController.RequestParkourAction(
            ParkourActionType.Climb);

        Debug.Log(
            $"EVADER CLIMB EXECUTION RESULT | Accepted={performed}");

        if (!performed)
            return false;

        climbRetryLockoutTimer = ClimbRetryLockoutDuration;
        climbRequestPending = true;
        climbActionObservedExecuting = false;

        Debug.Log("EVADER CLIMB REQUEST ACCEPTED");

        return true;
    }

    public void Update(
        AIContext context,
        CharacterActionController actionController,
        bool wantsToSprint,
        bool isApproachingFinalCorner)
    {
        // Cancel a traversal attempt that never produces a climb.
        if (climbTraversalArmed &&
            !climbRequestPending &&
            Time.time - climbTraversalStartTime > JumpStartTimeout)
        {
            Debug.Log(
                $"AI EVADER CLIMB TRAVERSAL TIMEOUT | " +
                $"Position={context.Position} | " +
                $"Grounded={context.IsGrounded} | " +
                $"VerticalVelocity={context.Velocity.y:F2}");

            climbTraversalArmed = false;
        }

        if (climbRequestPending)
        {
            if (context.IsExecutingAction)
            {
                climbActionObservedExecuting = true;
            }
            else if (climbActionObservedExecuting)
            {
                // The requested action started and has now ended.
                climbRequestPending = false;
                climbActionObservedExecuting = false;
                climbTraversalArmed = false;

                Debug.Log(
                    $"AI EVADER CLIMB REQUEST CYCLE ENDED | " +
                    $"Position={context.Position} | " +
                    $"Grounded={context.IsGrounded} | " +
                    $"Executing={context.IsExecutingAction} | " +
                    $"Stamina={context.NormalizedStamina:F2}"
                );
            }
        }

        if (CheckPartialPathJump(context, actionController))
            return;

        if (CheckClimbOpportunity(context, actionController))
            return;

        // Slide is an Evade behavior, not a Reposition behavior.
        if (!context.OpponentVisible)
            return;

        if (context.IsExecutingAction)
            return;

        if (!context.IsGrounded || context.IsStaminaEmpty)
            return;

        if (!wantsToSprint || slideCooldownTimer > 0f)
            return;

        if (!slideOpportunity.HasLowObstacleAhead(context))
            return;

        // Keep the same actual-sprint validation used by the Chaser.
        if (!characterMotor.IsSprinting)
            return;

        bool slidePerformed = actionController.RequestSlide();

        if (!slidePerformed)
        {
            Debug.Log("AI EVADER SLIDE FAILED | Low obstacle");
            return;
        }

        slideCooldownTimer = slideCooldown;

        Debug.Log("AI EVADER SLIDE | Executed");
    }
}
