using UnityEngine;

public class AIChaserActions
{
    private readonly AIChaserActionDecision decision;

    public AIChaserActions(
        float slideDistance,
        float slideCooldown)
    {
        decision =
            new AIChaserActionDecision(
                slideDistance,
                slideCooldown
            );
    }

    public void UpdateTimer()
    {
        decision.UpdateTimer();
    }

    public void Update(
        AIContext context,
        AIChaserState state,
        CharacterActionController actionController)
    {
        if (decision.ShouldSlide(
                context,
                state))
        {
            bool slidePerformed =
                actionController.RequestSlide();

            if (slidePerformed)
            {
                decision.StartSlideCooldown();

                Debug.Log(
                    "AI CHASER ACTION | Slide"
                );
            }
        }
    }
}