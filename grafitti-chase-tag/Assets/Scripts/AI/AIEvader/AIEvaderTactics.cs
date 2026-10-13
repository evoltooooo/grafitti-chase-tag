
using UnityEngine;

public class AIEvaderTactics
{
    private readonly CharacterMotor characterMotor;
    private readonly AISlideOpportunity slideOpportunity;

    private readonly float slideCooldown;
    private float slideCooldownTimer;

    public AIEvaderTactics(
        CharacterMotor characterMotor,
        float slideCooldown,
        float slideDetectionDistance,
        float slideLowDetectionHeight,
        float slideHighDetectionHeight,
        LayerMask slideObstacleMask)
    {
        this.characterMotor = characterMotor;
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
    }

    public void Update(
        AIContext context,
        CharacterActionController actionController,
        bool wantsToSprint)
    {
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
