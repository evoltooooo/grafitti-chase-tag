using UnityEngine;

public class AIController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Character")]
    [SerializeField] private CharacterRole characterRole;
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterStamina characterStamina;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterActionController actionController;

    [Header("Opponent")]
    [SerializeField] private Transform opponentTransform;
    [SerializeField] private CharacterMotor opponentMotor;

    [Header("Navigation")]
    [SerializeField] private float destinationRepathDistance = 2f;
    [SerializeField] private float pathRetryInterval = 0.25f;

    [Header("Perception")]
    [SerializeField] private AIPerception perception;

    [Header("Prediction")]
    [SerializeField] private float minPredictionTime = 0.15f;
    [SerializeField] private float maxPredictionTime = 1.0f;

    [Header("Intercept")]
    [SerializeField] private float minInterceptLeadTime = 0.25f;
    [SerializeField] private float maxInterceptLeadTime = 1.25f;

    [Header("Chaser Commitment")]
    [SerializeField] private float minimumChaserStateTime = 0.5f;


    // =========================================================
    // AI
    // =========================================================

    private AIContext context;
    private AIChaser chaser;
    private AIChaserDecision chaserDecision;
    private AIChaserDestination chaserDestination;
    private AIChaserCommitment chaserCommitment;
    private AIChaserMovement chaserMovement;


    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (characterRole == null)
            characterRole = GetComponent<CharacterRole>();

        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();

        if (characterStamina == null)
            characterStamina = GetComponent<CharacterStamina>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (actionController == null)
            actionController = GetComponent<CharacterActionController>();

        if (perception == null)
            perception = GetComponent<AIPerception>();


        context = new AIContext();

        AISteering steering = new AISteering();
        AINavigation navigation = new AINavigation();
        AIPrediction prediction = new AIPrediction();
        AIIntercept intercept = new AIIntercept();
        
        chaserMovement = new AIChaserMovement();

        chaserDestination = new AIChaserDestination(
            prediction,
            intercept,
            minPredictionTime,
            maxPredictionTime,
            minInterceptLeadTime,
            maxInterceptLeadTime
        );

        chaser = new AIChaser(
            steering,
            navigation,
            chaserDestination,
            chaserMovement,
            destinationRepathDistance,
            pathRetryInterval
        );

        chaserDecision = new AIChaserDecision();

        chaserCommitment =
            new AIChaserCommitment(
                minimumChaserStateTime
            );

        chaserCommitment.SetInitialState(
            AIChaserState.Pursue
        );
    }


    private void Update()
    {
        chaserCommitment.Update(
            Time.deltaTime
        );

        UpdateContext();
        UpdateAI();

        actionController.TickMovement();
    }


    // =========================================================
    // CONTEXT
    // =========================================================

    private void UpdateContext()
    {
        context.Update(
            characterMotor,
            characterStamina,
            actionRuntime,
            characterRole,

            opponentTransform,
            opponentMotor,

            perception.OpponentVisible,
            perception.LastKnownOpponentPosition,
            perception.LastKnownOpponentVelocity,
            perception.TimeSinceOpponentSeen
        );
    }


    // =========================================================
    // AI
    // =========================================================

    private void UpdateAI()
    {
        if (characterRole == null)
            return;

        switch (characterRole.CurrentRole)
        {
            case CharacterRoleType.Chaser:

                AIChaserState desiredState =
                    chaserDecision.Decide(context);

                AIChaserState currentState =
                    chaser.CurrentState;

                // =========================================================
                // CRITICAL TRANSITION
                // =========================================================

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
                }

                // =========================================================
                // NORMAL TRANSITION
                // =========================================================

                else if (desiredState != currentState)
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

                // =========================================================
                // EXECUTE CURRENT CHASER STATE
                // =========================================================

                chaser.Update(
                    context,
                    actionController
                );

                break;

            case CharacterRoleType.Evader:
                UpdateEvader();
                break;
        }
    }


    private void UpdateEvader()
    {
        // Decision system will be added later.
    }
}