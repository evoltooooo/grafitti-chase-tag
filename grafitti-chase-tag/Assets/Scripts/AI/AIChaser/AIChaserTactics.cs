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
    private bool upwardClimbJumpRequested;

    // TICTAC
    private float ticTacCooldownTimer;
    private const float TicTacCooldown = 1f;
    private bool ticTacApproachActive;
    private float ticTacJumpCooldownTimer;
    private const float TicTacJumpCooldown = 0.75f;


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
        ticTacCooldownTimer -= Time.deltaTime;
        ticTacJumpCooldownTimer -= Time.deltaTime;
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

            upwardClimbJumpRequested = false;

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

        CheckTicTacOpportunity(
            context,
            state,
            actionController
        );
    }

    private void CheckVaultOpportunity(
        AIContext context,
        CharacterActionController actionController)
    {
        if (context.IsExecutingAction)
            return;

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
        if (context.IsExecutingAction)
            return;

        if (slideCooldownTimer > 0f)
            return;

        if (!slideOpportunity.HasLowObstacleAhead(context))
            return;

        Debug.Log(
            "AI SLIDE OPPORTUNITY | Low obstacle ahead"
        );

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

        // Do not request another jump while waiting
        // for the previous jump request to take effect.
        if (upwardClimbJumpRequested)
            return;

        bool jumpPerformed =
            actionController.RequestJump();

        if (!jumpPerformed)
            return;

        upwardClimbJumpRequested = true;

        Debug.Log("AI UPWARD CLIMB | Jump requested");
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
    
    private void CheckTicTacOpportunity(
        AIContext context,
        AIChaserState state,
        CharacterActionController actionController)
    {
        // Only attempt this traversal while actively chasing.
        if (state != AIChaserState.Pursue &&
            state != AIChaserState.Intercept)
            return;

        if (!context.OpponentVisible)
            return;

        if (ticTacCooldownTimer > 0f)
            return;

        if (context.IsExecutingAction)
            return;

        if (context.IsStaminaEmpty)
            return;

        if (context.HorizontalSpeed < 0.5f)
            return;

        // Phase 1: find a wall and deliberately jump toward it.
        if (context.IsGrounded)
        {
            if (ticTacApproachActive)
                return;

            // Measure the player's movement on the ground plane.
            Vector3 opponentHorizontalVelocity =
                new Vector3(
                    context.OpponentVelocity.x,
                    0f,
                    context.OpponentVelocity.z
                );

            float opponentHorizontalSpeed =
                opponentHorizontalVelocity.magnitude;

            // TicTac may be useful if the player is actively
            // moving, or is significantly above the AI.
            bool opponentIsMoving =
                opponentHorizontalSpeed >= 1f;

            bool opponentIsHigher =
                context.OpponentPosition.y >
                context.Position.y + 1f;

            if (!opponentIsMoving && !opponentIsHigher)
                return;

            if (!parkourOpportunity.TryFindTicTac(
                    actionController,
                    out ParkourTarget groundTarget))
                return;

            Debug.Log(
                $"AI TICTAC APPROACH | " +
                $"PlayerSpeed={opponentHorizontalSpeed:F2} | " +
                $"PlayerHigher={opponentIsHigher} | " +
                $"Wall={groundTarget.InteractionPosition}"
            );

            bool jumpPerformed =
                actionController.RequestJump();

            if (!jumpPerformed)
            {
                Debug.Log("AI TICTAC APPROACH FAILED | Jump");
                ticTacJumpCooldownTimer = TicTacJumpCooldown;
                return;
            }

            ticTacApproachActive = true;

            Debug.Log("AI TICTAC APPROACH | Jump requested");
            return;
        }

        // Phase 2: while airborne, find the wall again
        // and request the actual TicTac action.
        if (!ticTacApproachActive)
            return;

        if (!parkourOpportunity.TryFindTicTac(
                actionController,
                out ParkourTarget airTarget))
        {
            Debug.Log("AI TICTAC CHECK | No valid wall while airborne");
            return;
        }

        Debug.Log(
            $"AI TICTAC TARGET FOUND | " +
            $"Target={airTarget.InteractionPosition}"
        );

        bool performed =
            actionController.RequestParkourAction(
                ParkourActionType.TicTac
            );

        if (!performed)
        {
            Debug.Log("AI TICTAC EXECUTION FAILED");
            return;
        }

        ticTacApproachActive = false;
        ticTacCooldownTimer = TicTacCooldown;

        Debug.Log("AI TICTAC EXECUTION | TicTac");
    }

}