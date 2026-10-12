
using UnityEngine;
public class AIEvaderController
{
    private readonly AIEvader evader;
    private readonly AIEvaderTactics tactics;

    public AIEvaderController(
        CharacterMotor characterMotor,
        float destinationRepathDistance,
        float pathRetryInterval,
        float evaderSlideCooldown,
        float slideDetectionDistance,
        float slideLowDetectionHeight,
        float slideHighDetectionHeight,
        LayerMask slideObstacleMask)
    {
        AISteering steering = new AISteering();
        AINavigation navigation = new AINavigation();

        AIEvaderDecision decision =
            new AIEvaderDecision();

        AIEvaderDestination destination =
            new AIEvaderDestination();

        AIEvaderMovement movement =
            new AIEvaderMovement(
                steering,
                navigation,
                destinationRepathDistance,
                pathRetryInterval);

        evader = new AIEvader(
            decision,
            destination,
            movement);

        tactics = new AIEvaderTactics(
            characterMotor,
            evaderSlideCooldown,
            slideDetectionDistance,
            slideLowDetectionHeight,
            slideHighDetectionHeight,
            slideObstacleMask);
    }

    public void Update(
        AIContext context,
        CharacterActionController actionController)
    {
        tactics.UpdateTimer();

        evader.Update(context, actionController);

        tactics.Update(context, actionController);
    }
}
