
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
        CharacterActionController actionController)
    {
        if (context.IsExecutingAction)
            return;

        if (!context.IsGrounded)
            return;

        if (context.IsStaminaEmpty)
            return;

        if (slideCooldownTimer > 0f)
            return;

        if (!slideOpportunity.HasLowObstacleAhead(context))
            return;

        // Only slide after the motor confirms actual sprinting.
        if (!characterMotor.IsSprinting)
            return;

        if (!actionController.RequestSlide())
        {
            Debug.Log("AI EVADER SLIDE FAILED | Request rejected");
            return;
        }

        slideCooldownTimer = slideCooldown;

        Debug.Log("AI EVADER SLIDE | Executed");
    }
}
