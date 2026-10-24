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

    [Header("Parkour")]
    [SerializeField] private float parkourCheckInterval = 0.15f;

    [Header("Slide")]
    [SerializeField] private float chaserSlideCooldown = 1.5f;
    [SerializeField] private float evaderSlideCooldown = 1.5f;

    [Header("Slide Detection")]
    [SerializeField] private float slideDetectionDistance = 2f;
    [SerializeField] private float slideLowDetectionHeight = 0.55f;
    [SerializeField] private float slideHighDetectionHeight = 1.2f;
    [SerializeField] private LayerMask slideObstacleMask;

    [Header("Evader Drop")]
    [SerializeField] private LayerMask dropLandingMask;
    [SerializeField] private float maximumEvaderDropHeight = 6f;
    [SerializeField] private float dropApproachDistance = 0.6f;


    // =========================================================
    // AI
    // =========================================================

    private AIContext context;

    private AIChaserController chaserController;
    private AIEvaderController evaderController;

    // Future:
    // private AIEvaderController evaderController;


    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (characterRole == null)
            characterRole =
                GetComponent<CharacterRole>();

        if (characterMotor == null)
            characterMotor =
                GetComponent<CharacterMotor>();

        if (characterStamina == null)
            characterStamina =
                GetComponent<CharacterStamina>();

        if (actionRuntime == null)
            actionRuntime =
                GetComponent<CharacterActionRuntime>();

        if (actionController == null)
            actionController =
                GetComponent<CharacterActionController>();

        if (perception == null)
            perception =
                GetComponent<AIPerception>();


        context =
            new AIContext();


        chaserController =
            new AIChaserController(
                characterMotor,
                destinationRepathDistance,
                pathRetryInterval,
                minPredictionTime,
                maxPredictionTime,
                minInterceptLeadTime,
                maxInterceptLeadTime,
                minimumChaserStateTime,
                chaserSlideCooldown,
                parkourCheckInterval,
                slideDetectionDistance,
                slideLowDetectionHeight,
                slideHighDetectionHeight,
                slideObstacleMask
            );
        
            evaderController = new AIEvaderController(
                characterMotor,
                destinationRepathDistance,
                pathRetryInterval,
                evaderSlideCooldown,
                slideDetectionDistance,
                slideLowDetectionHeight,
                slideHighDetectionHeight,
                slideObstacleMask,
                transform,
                dropLandingMask,
                maximumEvaderDropHeight,
                dropApproachDistance
            );
    }

    private void Update()
    {
        UpdateContext();

        UpdateAI();

        actionController.TickMovement();

        Debug.Log(
            $"AI MOVEMENT DEBUG | " +
            $"Role={context.Role} | " +
            $"Visible={context.OpponentVisible} | " +
            $"AI Pos={context.Position} | " +
            $"Player Pos={context.OpponentPosition} | " +
            $"AI Speed={context.HorizontalSpeed:0.00}"
        );
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

                chaserController.Update(
                    context,
                    actionController
                );

                break;


            case CharacterRoleType.Evader:

                evaderController.Update(
                    context,
                    actionController
                );

                break;
        }
    }

    public void ResetForNewRound()
    {
        if (chaserController != null)
            chaserController.ResetForNewRound();

        if (evaderController != null)
            evaderController.ResetForNewRound();

        if (actionController != null)
            actionController.SetMovementIntent(default);

        Debug.Log("AI CONTROLLER RESET | New round");
    }
}