using UnityEngine;

public class AIChaserActions
{
    private readonly AIChaserActionDecision decision;
    private readonly AIParkourOpportunity parkourOpportunity;
    private readonly AIChaserParkourDecision parkourDecision;
    private readonly AISlideOpportunity slideOpportunity;

    private float parkourCheckTimer;
    private readonly float parkourCheckInterval;

    private float vaultCooldownTimer;
    private const float VaultCooldown = 0.75f;

    public AIChaserActions(
        float slideDistance,
        float slideCooldown,
        float parkourCheckInterval,
        float slideDetectionDistance,
        float slideLowDetectionHeight,
        float slideHighDetectionHeight,
        LayerMask slideObstacleMask)
    {
        decision =
            new AIChaserActionDecision(
                slideDistance,
                slideCooldown);

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
        decision.UpdateTimer();

        parkourCheckTimer -= Time.deltaTime;
        vaultCooldownTimer -= Time.deltaTime;
    }

    public void Update(
        AIContext context,
        AIChaserState state,
        CharacterActionController actionController)
    {
        CheckSlideOpportunity(
            context,
            actionController
        );

        CheckVault(
            context,
            actionController
        );
    }

    private void CheckVault(
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

        decision.StartSlideCooldown();

        Debug.Log(
            "AI SLIDE EXECUTION | Low obstacle"
        );
    }
}