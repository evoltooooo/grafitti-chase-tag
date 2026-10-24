using UnityEngine;

public class AIChaserController
{
    private readonly CharacterMotor characterMotor;
    private readonly AIChaser chaser;
    private readonly AIChaserDecision chaserDecision;
    private readonly AIChaserCommitment chaserCommitment;
    private readonly AIChaserTactics tactics;

    private const float TagAttemptInterval = 0.2f;
    private float tagAttemptTimer;

    public AIChaserController(
        CharacterMotor characterMotor,
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
        this.characterMotor = characterMotor;

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

        tactics = new AIChaserTactics(
            characterMotor,
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

        // Detect before deciding whether to sprint.
        bool lowObstacleAhead =
            tactics.UpdateSlideDetection(context);

        // Feed the same detection result into movement.
        chaser.Update(
            context,
            actionController,
            lowObstacleAhead
        );

        // Keep the existing tactics update and other parkour checks.
        UpdateTactics(
            context,
            actionController
        );

        TryTagOpponent(context, actionController);
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

    public void ResetForNewRound()
    {
        chaser.ResetForNewRound();
        chaserCommitment.ResetForNewRound();
        tactics.ResetForNewRound();

        tagAttemptTimer = 0f;

        Debug.Log("AI CHASER CONTROLLER RESET | New round");
    }

    private void TryTagOpponent(
        AIContext context,
        CharacterActionController actionController)
    {
        tagAttemptTimer -= Time.deltaTime;

        if (context.Role != CharacterRoleType.Chaser)
            return;

        if (!context.OpponentVisible)
            return;

        if (context.IsExecutingAction)
            return;

        if (!context.IsGrounded)
            return;

        if (tagAttemptTimer > 0f)
            return;

        // Throttle attempts so the AI does not request Tag every frame.
        tagAttemptTimer = TagAttemptInterval;

        Vector3 toOpponent =
            context.OpponentPosition - context.Position;

        toOpponent.y = 0f;

        Debug.Log(
            $"AI TAG CHECK | " +
            $"Distance={toOpponent.magnitude:F2} | " +
            $"Visible={context.OpponentVisible} | " +
            $"Grounded={context.IsGrounded} | " +
            $"Forward={context.Forward}"
        );

        bool tagPerformed = actionController.RequestTag();

        Debug.Log(
            $"AI TAG ATTEMPT | Performed={tagPerformed} | " +
            $"Distance={toOpponent.magnitude:F2}"
        );
    }
}