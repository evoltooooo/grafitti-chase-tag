using System;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public enum RoundState
    {
        WaitingToStart,
        RoleShuffle,
        Countdown,
        Active,
        Intermission
    }

    public enum StartingRole
    {
        PlayerChaser,
        AIChaser,
        Random
    }

    [Header("Character Executors")]
    [SerializeField] private CharacterActionExecutor playerActionExecutor;
    [SerializeField] private CharacterActionExecutor aiActionExecutor;

    [Header("Character Controllers")]
    [SerializeField] private CharacterActionController playerActionController;
    [SerializeField] private CharacterActionController aiActionController;

    [Header("Characters")]
    [SerializeField] private CharacterRole playerRole;
    [SerializeField] private CharacterRole aiRole;

    [Header("AI")]
    [SerializeField] private AIController aiController;

    [Header("Arena Spawn Points")]
    [SerializeField] private Transform spawnWest;
    [SerializeField] private Transform spawnEast;
    [SerializeField] private Transform spawnNorth;
    [SerializeField] private Transform spawnSouth;

    [Header("Round Settings")]
    [SerializeField, Min(1f)] private float roundDuration = 90f;
    [SerializeField, Min(0f)] private float intermissionDuration = 3f;
    [SerializeField, Min(1)] private int pointsPerRound = 1;

    [Header("Session Settings")]
    [SerializeField, Min(1)] private int winsRequired = 3;
    [SerializeField, Min(1)] private int maximumRounds = 5;
    [SerializeField, Min(0f)] private float roundResultDuration = 5f;

    private bool sessionTimerStarted;
    public float SessionElapsedTime { get; private set; }
    public bool IsSessionComplete { get; private set; }

    private float roundElapsedTime;
    private float roundResultTimer;
    public float RoundResultDuration => roundResultDuration;

    [Header("Role Shuffle")]
    [SerializeField] private RoleShuffleController roleShuffleController;

    private float countdownDuration = 3f;
    public float CountdownRemaining => countdownTimer;

    public float LastRoundElapsedTime { get; private set; }

    private float countdownTimer;
    [SerializeField] private StartingRole startingRole =
        StartingRole.PlayerChaser;

    public RoundState CurrentState { get; private set; }
        = RoundState.WaitingToStart;

    public int CurrentRound { get; private set; }
    public float TimeRemaining { get; private set; }

    public int PlayerScore { get; private set; }
    public int AIScore { get; private set; }

    public event Action<int, float> RoundStarted;
    public event Action<float> TimerUpdated;
    public event Action<int, CharacterRoleType, int, int> RoundEnded;
    public event Action<float> CountdownUpdated;

    public bool IsRoundActive => CurrentState == RoundState.Active;
    public float IntermissionDuration => intermissionDuration;

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        playerActionExecutor.TagSucceeded += OnPlayerTagSucceeded;
        aiActionExecutor.TagSucceeded += OnAITagSucceeded;

        AssignInitialRoles();
        BeginRound();
    }

    private void Update()
    {
        if (sessionTimerStarted && !IsSessionComplete)
            SessionElapsedTime += Time.deltaTime;

        switch (CurrentState)
        {
            case RoundState.WaitingToStart:
                break;
            
            case RoundState.RoleShuffle:
                break;

            case RoundState.Countdown:
                UpdateCountdown();
                break;

            case RoundState.Active:
                UpdateRoundTimer();
                break;

            case RoundState.Intermission:
                UpdateIntermission();
                break;
        }
    }

    private bool ValidateReferences()
    {
        if (playerRole == null || aiRole == null)
        {
            Debug.LogError(
                "ROUND MANAGER | PlayerRole or AIRole is missing.",
                this);

            return false;
        }

        if (playerRole == aiRole)
        {
            Debug.LogError(
                "ROUND MANAGER | Player and AI must have separate CharacterRole components.",
                this);

            return false;
        }

        if (playerActionExecutor == null || aiActionExecutor == null)
        {
            Debug.LogError(
                "ROUND MANAGER | PlayerActionExecutor or AIActionExecutor is missing.",
                this);

            return false;
        }

        if (spawnWest == null ||
            spawnEast == null ||
            spawnNorth == null ||
            spawnSouth == null)
        {
            Debug.LogError(
                "ROUND MANAGER | One or more spawn points are missing.",
                this
            );

            return false;
        }

        return true;
    }

    private bool PositionCharactersForNewRound()
    {
        Transform firstSpawn;
        Transform secondSpawn;

        // Choose an opposing pair.
        if (UnityEngine.Random.value < 0.5f)
        {
            firstSpawn = spawnWest;
            secondSpawn = spawnEast;
        }
        else
        {
            firstSpawn = spawnNorth;
            secondSpawn = spawnSouth;
        }

        // Randomize which character occupies each end.
        Transform playerSpawn = firstSpawn;
        Transform aiSpawn = secondSpawn;

        if (UnityEngine.Random.value < 0.5f)
        {
            playerSpawn = secondSpawn;
            aiSpawn = firstSpawn;
        }

        bool playerPlaced =
            playerActionController != null &&
            playerActionController.ResetForRoundStart(playerSpawn);

        bool aiPlaced =
            aiActionController != null &&
            aiActionController.ResetForRoundStart(aiSpawn);

        if (!playerPlaced || !aiPlaced)
        {
            Debug.LogError(
                "ROUND MANAGER | Failed to position both characters. " +
                "The round will not start."
            );

            return false;
        }

        Debug.Log(
            $"ROUND SPAWNS | Player={playerSpawn.name} | " +
            $"AI={aiSpawn.name}"
        );

        return true;
    }

    private void AssignInitialRoles()
    {
        CharacterRoleType playerAssignedRole;

        switch (startingRole)
        {
            case StartingRole.PlayerChaser:
                playerAssignedRole = CharacterRoleType.Chaser;
                break;

            case StartingRole.AIChaser:
                playerAssignedRole = CharacterRoleType.Evader;
                break;

            default:
                playerAssignedRole =
                    UnityEngine.Random.value < 0.5f
                    ? CharacterRoleType.Chaser
                    : CharacterRoleType.Evader;
                break;
        }

        CharacterRoleType aiAssignedRole =
            playerAssignedRole == CharacterRoleType.Chaser
            ? CharacterRoleType.Evader
            : CharacterRoleType.Chaser;

        playerRole.SetRole(playerAssignedRole);
        aiRole.SetRole(aiAssignedRole);
    }

    private void UpdateCountdown()
    {
        countdownTimer = Mathf.Max(
            0f,
            countdownTimer - Time.deltaTime
        );

        CountdownUpdated?.Invoke(countdownTimer);

        if (countdownTimer <= 0f)
        {
            ActivateRound();
        }
    }

    private void ActivateRound()
    {

        if (CurrentState != RoundState.Countdown)
            return;

        // Clear any decisions or movement intent accumulated
        // by the AI while movement was locked.
        if (aiController != null)
            aiController.ResetForNewRound();

        roundElapsedTime = 0f;

        if (!sessionTimerStarted)
            sessionTimerStarted = true;

        CurrentState = RoundState.Active;

        Debug.Log(
            $"ROUND STARTED | Round={CurrentRound} | " +
            $"PlayerRole={playerRole.CurrentRole} | " +
            $"AIRole={aiRole.CurrentRole} | " +
            $"Time={TimeRemaining:F1}"
        );

        CountdownUpdated?.Invoke(0f);
        RoundStarted?.Invoke(CurrentRound, TimeRemaining);
        TimerUpdated?.Invoke(TimeRemaining);
    }
    
    private void BeginRound()
    {
        // Keep gameplay locked while preparing the round.
        GameplayInputLock.SetLocked(true);
        CurrentState = RoundState.WaitingToStart;

        if (!PositionCharactersForNewRound())
        {
            GameplayInputLock.SetLocked(false);
            return;
        }

        if (aiController != null)
            aiController.ResetForNewRound();

        CurrentRound++;

        // Prepare the timer, but do not count it down yet.
        TimeRemaining = roundDuration;
        TimerUpdated?.Invoke(TimeRemaining);

        // Show the role shuffle before starting the countdown.
        if (roleShuffleController != null)
        {
            CurrentState = RoundState.RoleShuffle;

            roleShuffleController.Play(
                playerRole.CurrentRole,
                BeginCountdown);
        }
        else
        {
            Debug.LogWarning(
                "ROUND MANAGER | RoleShuffleController is missing. " +
                "Starting countdown without the role shuffle.",
                this);

            BeginCountdown();
        }
    }

    private void BeginCountdown()
    {
        // Prevent duplicate or late callbacks from starting a round
        // after the session has moved into another state.
        if (CurrentState != RoundState.RoleShuffle &&
            CurrentState != RoundState.WaitingToStart)
        {
            return;
        }

        if (aiController != null)
            aiController.ResetForNewRound();

        countdownTimer = countdownDuration;
        CurrentState = RoundState.Countdown;

        Debug.Log(
            $"ROUND COUNTDOWN | Round={CurrentRound} | " +
            $"Duration={countdownTimer:F1} | " +
            $"RoundTime={TimeRemaining:F1}");

        CountdownUpdated?.Invoke(countdownTimer);
    }

    private void UpdateRoundTimer()
    {
        roundElapsedTime += Time.deltaTime;

        TimeRemaining = Mathf.Max(
            0f,
            TimeRemaining - Time.deltaTime);

        TimerUpdated?.Invoke(TimeRemaining);

        if (TimeRemaining <= 0f)
        {
            // If time expires, the Evader wins the round.
            EndRound(CharacterRoleType.Evader);
        }
    }

    private void UpdateIntermission()
    {
        roundResultTimer -= Time.deltaTime;

        if (roundResultTimer > 0f)
            return;

        // Stop the session if either side has enough wins,
        // or the maximum number of rounds has been completed.
        if (PlayerScore >= winsRequired ||
            AIScore >= winsRequired ||
            CurrentRound >= maximumRounds)
        {
            EndSession();
            return;
        }

        SwapRoles();
        BeginRound();
    }
    
    private void EndSession()
    {
        if (IsSessionComplete)
            return;

        IsSessionComplete = true;
        CurrentState = RoundState.WaitingToStart;

        GameplayInputLock.SetLocked(true);

        // Save the final session data before changing scenes.
        SessionResults.SetResults(
            PlayerScore,
            AIScore,
            SessionElapsedTime
        );

        Debug.Log(
            $"SESSION ENDED | PlayerScore={PlayerScore} | " +
            $"AIScore={AIScore} | Rounds={CurrentRound} | " +
            $"Elapsed={SessionElapsedTime:F2}s"
        );

        UnityEngine.SceneManagement.SceneManager.LoadScene("Game End");
    }

    /// <summary>
    /// Call this only after the tag system confirms a successful tag.
    /// The Chaser must tag the opposing Evader.
    /// </summary>
    public bool RegisterSuccessfulTag(
        CharacterRole tagger,
        CharacterRole target)
    {
        if (CurrentState != RoundState.Active)
            return false;

        if (tagger == null || target == null)
            return false;

        if (tagger == target)
            return false;

        if (tagger.CurrentRole != CharacterRoleType.Chaser ||
            target.CurrentRole != CharacterRoleType.Evader)
        {
            Debug.LogWarning(
                "ROUND TAG REJECTED | Only a Chaser tagging an Evader can end the round.");

            return false;
        }

        EndRound(CharacterRoleType.Chaser);
        return true;
    }

    private void EndRound(CharacterRoleType winningRole)
    {
        // Prevent multiple scoring events for the same round.
        if (CurrentState != RoundState.Active)
            return;

        CurrentState = RoundState.Intermission;

        if (playerActionController != null)
            playerActionController.StopForRoundTransition();

        if (aiActionController != null)
            aiActionController.StopForRoundTransition();
        
        if (aiController != null)
            aiController.ResetForNewRound();

        LastRoundElapsedTime = roundElapsedTime;

        AwardPoints(winningRole);

        Debug.Log(
            $"ROUND ENDED | Round={CurrentRound} | " +
            $"Winner={winningRole} | " +
            $"PlayerScore={PlayerScore} | AIScore={AIScore}");

        RoundEnded?.Invoke(
            CurrentRound,
            winningRole,
            PlayerScore,
            AIScore);

        roundResultTimer = roundResultDuration;
    }

    private void AwardPoints(CharacterRoleType winningRole)
    {
        if (playerRole.CurrentRole == winningRole)
        {
            PlayerScore += pointsPerRound;
        }
        else if (aiRole.CurrentRole == winningRole)
        {
            AIScore += pointsPerRound;
        }
    }

    private void SwapRoles()
    {
        CharacterRoleType previousPlayerRole =
            playerRole.CurrentRole;

        playerRole.SetRole(aiRole.CurrentRole);
        aiRole.SetRole(previousPlayerRole);

        Debug.Log(
            $"ROLES SWAPPED | Player={playerRole.CurrentRole} | " +
            $"AI={aiRole.CurrentRole}");
    }

    private void OnPlayerTagSucceeded(Collider target)
    {
        HandleSuccessfulTag(playerRole, target);
    }

    private void OnAITagSucceeded(Collider target)
    {
        HandleSuccessfulTag(aiRole, target);
    }

    private void HandleSuccessfulTag(
        CharacterRole tagger,
        Collider targetCollider)
    {
        if (targetCollider == null)
            return;

        CharacterRole targetRole =
            targetCollider.GetComponentInParent<CharacterRole>();

        if (targetRole == null)
        {
            Debug.LogWarning(
                $"ROUND TAG IGNORED | No CharacterRole found on target {targetCollider.name}");
            return;
        }

        bool roundResolved =
            RegisterSuccessfulTag(tagger, targetRole);

        Debug.Log(
            $"ROUND TAG RESULT | " +
            $"Tagger={tagger.CurrentRole} | " +
            $"Target={targetRole.CurrentRole} | " +
            $"Resolved={roundResolved}");
    }

    private void OnDestroy()
    {
        if (playerActionExecutor != null)
            playerActionExecutor.TagSucceeded -= OnPlayerTagSucceeded;

        if (aiActionExecutor != null)
            aiActionExecutor.TagSucceeded -= OnAITagSucceeded;
    }
}
