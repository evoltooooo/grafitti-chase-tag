using UnityEngine;

public class AIChaserActionDecision
{
    private readonly float slideDistance;
    private readonly float slideCooldown;

    private float slideCooldownTimer;

    public AIChaserActionDecision(
        float slideDistance,
        float slideCooldown)
    {
        this.slideDistance = slideDistance;
        this.slideCooldown = slideCooldown;
    }

    public void UpdateTimer()
    {
        slideCooldownTimer -= Time.deltaTime;
    }

    public bool ShouldSlide(
        AIContext context,
        AIChaserState state)
    {
        if (state == AIChaserState.Search)
            return false;

        if (context.IsExecutingAction)
            return false;

        if (!context.IsGrounded)
            return false;

        if (context.IsStaminaEmpty)
            return false;

        if (slideCooldownTimer > 0f)
            return false;

        float distance =
            Vector3.Distance(
                context.Position,
                context.OpponentPosition
            );

        if (distance > slideDistance)
            return false;

        return true;
    }

    public void StartSlideCooldown()
    {
        slideCooldownTimer = slideCooldown;
    }
}