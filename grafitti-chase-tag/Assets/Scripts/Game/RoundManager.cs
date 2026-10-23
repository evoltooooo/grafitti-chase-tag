
using System;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public enum RoundState
    {
        WaitingToStart,
        Active,
        Intermission
    }

    public enum StartingRole
    {
        PlayerChaser,
        AIChaser,
        Random
    }

    [Header("References")]
    [SerializeField] private CharacterActionExecutor playerActionExecutor;
    [SerializeField] private CharacterActionExecutor aiActionExecutor;

    [Header("Characters")]
    [SerializeField] private CharacterRole playerRole;
    [SerializeField] private CharacterRole aiRole;

    [Header("Round Settings")]
    [SerializeField, Min(1f)] private float roundDuration = 90f;
    [SerializeField, Min(0f)] private float intermissionDuration = 3f;
    [SerializeField, Min(1)] private int pointsPerRound = 1;
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

    private float intermissionTimer;

    public bool IsRoundActive => CurrentState == RoundState.Active;

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
        switch (CurrentState)
        {
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

    private void BeginRound()
    {
        CurrentRound++;
        TimeRemaining = roundDuration;
        CurrentState = RoundState.Active;

        Debug.Log(
            $"ROUND STARTED | Round={CurrentRound} | " +
            $"PlayerRole={playerRole.CurrentRole} | " +
            $"AIRole={aiRole.CurrentRole} | " +
            $"Time={TimeRemaining:F1}");

        RoundStarted?.Invoke(CurrentRound, TimeRemaining);
        TimerUpdated?.Invoke(TimeRemaining);
    }

    private void UpdateRoundTimer()
    {
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
        intermissionTimer -= Time.deltaTime;

        if (intermissionTimer > 0f)
            return;

        SwapRoles();
        BeginRound();
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

        intermissionTimer = intermissionDuration;
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
