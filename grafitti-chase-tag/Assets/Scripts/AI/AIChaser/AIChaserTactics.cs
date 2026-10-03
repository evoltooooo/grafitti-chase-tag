using UnityEngine;

public class AIChaserTactics
{
    private readonly AIParkourOpportunity parkourOpportunity;
    private readonly AIChaserParkourDecision parkourDecision;
    private readonly AISlideOpportunity slideOpportunity;

    private float parkourCheckTimer;
    private readonly float parkourCheckInterval;

    // VAULT
    private float vaultCooldownTimer;
    private const float VaultCooldown = 0.75f;

    // SLIDE
    private float slideCooldownTimer;
    private readonly float slideCooldown;

    // CLIMB
    private float climbCheckTimer;
    private const float ClimbCheckInterval = 0.15f;
    private bool upwardClimbAttempt;
    private bool upwardClimbHandled;

    public AIChaserTactics(
        float slideCooldown,
        float parkourCheckInterval,
        float slideDetectionDistance,
        float slideLowDetectionHeight,
        float slideHighDetectionHeight,
        LayerMask slideObstacleMask)
    {
        this.slideCooldown =
            slideCooldown;

        parkourOpportunity =
            new AIParkourOpportunity();

        parkourDecision =
            new AIChaserParkourDecision();

        slideOpportunity =
            new AISlideOpportunity(
                slideDetectionDistance,
                slideLowDetectionHeight,
                slideHighDetectionHeight,
                slideObstacleMask
            );

        this.parkourCheckInterval =
            parkourCheckInterval;
    }

    public void UpdateTimer()
    {
        parkourCheckTimer -= Time.deltaTime;
        vaultCooldownTimer -= Time.deltaTime;
        slideCooldownTimer -= Time.deltaTime;
        climbCheckTimer -= Time.deltaTime;
    }

    public void Update(
        AIContext context,
        AIChaserState state,
        CharacterActionController actionController,
        bool reachedPartialPathEnd,
        bool isAscendingPartialPath)
    {
        CheckSlideOpportunity(
            context,
            actionController
        );

        CheckVaultOpportunity(
            context,
            actionController
        );

        if (!isAscendingPartialPath)
        {
            upwardClimbHandled = false;
        }

        if (isAscendingPartialPath &&
            !upwardClimbHandled)
        {
            upwardClimbAttempt = true;
            upwardClimbHandled = true;
        }

        if (upwardClimbAttempt)
        {
            if (context.IsGrounded)
            {
                CheckUpwardClimbApproach(
                    context,
                    actionController
                );

                return;
            }

            bool climbPerformed =
                CheckClimbOpportunity(
                    context,
                    actionController
                );

            if (climbPerformed)
            {
                upwardClimbAttempt = false;
            }

            return;
        }

        CheckClimbOpportunity(
            context,
            actionController
        );
    }

    private void CheckVaultOpportunity(
        AIContext context,
        CharacterActionController actionController)
    {
        if (parkourCheckTimer > 0f)
            return;

        parkourCheckTimer =
            parkourCheckInterval;

        if (vaultCooldownTimer > 0f)
            return;

        if (!parkourOpportunity.TryFindVault(
                actionController,
                out ParkourTarget target))
        {
            return;
        }

        Debug.Log(
            $"AI PARKOUR OPPORTUNITY | " +
            $"Vault | " +
            $"Target={target.InteractionPosition}"
        );

        bool shouldVault =
            parkourDecision.ShouldVault(
                context,
                target
            );

        if (!shouldVault)
            return;

        Debug.Log(
            "AI PARKOUR DECISION | Vault"
        );

        bool vaultPerformed =
            actionController.RequestParkourAction(
                ParkourActionType.Vault
            );

        if (!vaultPerformed)
        {
            Debug.Log(
                "AI PARKOUR EXECUTION FAILED | Vault"
            );

            return;
        }

        vaultCooldownTimer =
            VaultCooldown;

        Debug.Log(
            "AI PARKOUR EXECUTION | Vault"
        );
    }

    private void CheckSlideOpportunity(
        AIContext context,
        CharacterActionController actionController)
    {
        if (slideCooldownTimer > 0f)
            return;

        if (!slideOpportunity.HasLowObstacleAhead(context))
            return;

        Debug.Log(
            "AI SLIDE OPPORTUNITY | Low obstacle ahead"
        );

        if (context.IsExecutingAction)
            return;

        if (!context.IsGrounded)
            return;

        if (context.IsStaminaEmpty)
            return;

        bool slidePerformed =
            actionController.RequestSlide();

        if (!slidePerformed)
        {
            Debug.Log(
                "AI SLIDE EXECUTION FAILED | Low obstacle"
            );

            return;
        }

        slideCooldownTimer =
            slideCooldown;

        Debug.Log(
            "AI SLIDE EXECUTION | Low obstacle"
        );
    }

    private bool CheckClimbOpportunity(
        AIContext context,
        CharacterActionController actionController)
    {
        if (climbCheckTimer > 0f)
            return false;

        climbCheckTimer =
            ClimbCheckInterval;

        if (context.IsExecutingAction)
            return false;

        if (context.IsStaminaEmpty)
            return false;

        if (!parkourOpportunity.TryFindClimb(
                actionController,
                out ParkourTarget target))
        {
            return false;
        }

        Debug.Log(
            $"AI CLIMB SUCCESS WINDOW | " +
            $"Grounded={context.IsGrounded} | " +
            $"Position={context.Position} | " +
            $"Target={target.InteractionPosition}"
        );

        bool climbPerformed =
            actionController.RequestParkourAction(
                ParkourActionType.Climb
            );

        if (!climbPerformed)
        {
            Debug.Log(
                "AI CLIMB EXECUTION FAILED"
            );

            return false;
        }

        Debug.Log(
            "AI CLIMB EXECUTION | Climb"
        );

        return true;
    }

    private void CheckUpwardClimbApproach(
        AIContext context,
        CharacterActionController actionController)
    {
        if (context.IsExecutingAction)
            return;

        if (!context.IsGrounded)
            return;

        bool jumpPerformed =
            actionController.RequestJump();

        if (!jumpPerformed)
            return;

        Debug.Log(
            "AI UPWARD CLIMB | Jump"
        );
    }

    private void CheckClimbAtPartialPath(
        AIContext context,
        CharacterActionController actionController)
    {
        Debug.Log(
            $"AI CLIMB APPROACH | " +
            $"Grounded={context.IsGrounded} | " +
            $"Stamina={context.CurrentStamina:F1}"
        );

        // ---------------------------------------------
        // START JUMP
        // ---------------------------------------------

        if (context.IsGrounded)
        {
            Debug.Log(
                $"AI CLIMB APPROACH | " +
                $"Grounded={context.IsGrounded} | " +
                $"Stamina={context.CurrentStamina:F1}"
            );

            return;
        }

        // ---------------------------------------------
        // WHILE AIRBORNE
        // ---------------------------------------------

        Debug.Log(
            "AI CLIMB APPROACH | Airborne | Checking Climb"
        );

        if (!parkourOpportunity.TryFindClimb(
                actionController,
                out ParkourTarget target))
        {
            Debug.Log(
                "AI CLIMB APPROACH | " +
                "Airborne Climb check failed"
            );

            return;
        }

        Debug.Log(
            $"AI CLIMB APPROACH | " +
            $"CLIMB TARGET FOUND | " +
            $"Interaction={target.InteractionPosition} | " +
            $"Landing={target.LandingPosition}"
        );
    }
}