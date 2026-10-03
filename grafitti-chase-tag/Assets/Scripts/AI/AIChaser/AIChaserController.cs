using UnityEngine;

public class AIChaserController
{
    private readonly AIChaser chaser;
    private readonly AIChaserDecision chaserDecision;
    private readonly AIChaserCommitment chaserCommitment;
    private readonly AIChaserTactics tactics;

    public AIChaserController(
        float destinationRepathDistance,
        float pathRetryInterval,
        float minPredictionTime,
        float maxPredictionTime,
        float minInterceptLeadTime,
        float maxInterceptLeadTime,
        float minimumChaserStateTime,
        float chaserSlideCooldown,
        float parkourCheckInterval,
        float slideDetectionDistance,
        float slideLowDetectionHeight,
        float slideHighDetectionHeight,
        LayerMask slideObstacleMask)
    {
        AISteering steering =
            new AISteering();

        AINavigation navigation =
            new AINavigation();

        AIPrediction prediction =
            new AIPrediction();

        AIIntercept intercept =
            new AIIntercept();

        AIChaserMovement movement =
            new AIChaserMovement();

        AIChaserDestination destination =
            new AIChaserDestination(
                prediction,
                intercept,
                minPredictionTime,
                maxPredictionTime,
                minInterceptLeadTime,
                maxInterceptLeadTime
            );

        chaser =
            new AIChaser(
                steering,
                navigation,
                destination,
                movement,
                destinationRepathDistance,
                pathRetryInterval
            );

        chaserDecision =
            new AIChaserDecision();

        chaserCommitment =
            new AIChaserCommitment(
                minimumChaserStateTime
            );

        chaserCommitment.SetInitialState(
            AIChaserState.Pursue
        );

        tactics =
        new AIChaserTactics(
            chaserSlideCooldown,
            parkourCheckInterval,
            slideDetectionDistance,
            slideLowDetectionHeight,
            slideHighDetectionHeight,
            slideObstacleMask
        );
    }

    public void Update(
        AIContext context,
        CharacterActionController actionController)
    {
        chaserCommitment.Update(
            Time.deltaTime
        );

        tactics.UpdateTimer();

        UpdateState(context);

        chaser.Update(
            context,
            actionController
        );

        UpdateTactics(
            context,
            actionController
        );
    }

    private void UpdateState(
        AIContext context)
    {
        AIChaserState desiredState =
            chaserDecision.Decide(context);
        
        Debug.Log(
            $"AI CHASER DEBUG | " +
            $"Current={chaser.CurrentState} | " +
            $"Desired={desiredState} | " +
            $"Visible={context.OpponentVisible}"
        );

        AIChaserState currentState =
            chaser.CurrentState;

        // =====================================================
        // CRITICAL TRANSITION
        // =====================================================

        bool forceSearch =
            !context.OpponentVisible &&
            currentState != AIChaserState.Search;

        if (forceSearch)
        {
            Debug.Log(
                $"AI CHASER STATE CHANGE | " +
                $"{currentState} -> Search"
            );

            chaserCommitment.ForceState(
                AIChaserState.Search
            );

            chaser.SetState(
                AIChaserState.Search
            );

            return;
        }

        // =====================================================
        // NORMAL TRANSITION
        // =====================================================

        if (desiredState != currentState)
        {
            bool changed =
                chaserCommitment.TryChangeState(
                    desiredState
                );

            if (changed)
            {
                Debug.Log(
                    $"AI CHASER STATE CHANGE | " +
                    $"{currentState} -> {desiredState}"
                );

                chaser.SetState(
                    desiredState
                );
            }
        }
    }

    public bool HasReachedPartialPathEnd()
    {
        return chaser.HasReachedPartialPathEnd();
    }

    private void UpdateTactics(
        AIContext context,
        CharacterActionController actionController)
    {
        tactics.Update(
            context,
            chaser.CurrentState,
            actionController,
            chaser.HasReachedPartialPathEnd(),
            chaser.IsAscendingPartialPath()
        );
    }
    
}