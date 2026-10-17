
using UnityEngine;

[DisallowMultipleComponent]
public class ClimbAnimationEntry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Animation Entry")]
    [SerializeField, Min(0f)]
    private float transitionDuration = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool logTransitions = true;

    private bool climbEntryRequested;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (animator == null)
        {
            Debug.LogError(
                "ClimbAnimationEntry: Animator is missing.",
                this);
        }

        if (actionRuntime == null)
        {
            Debug.LogError(
                "ClimbAnimationEntry: CharacterActionRuntime is missing.",
                this);
        }
    }

    private void Update()
    {
        if (animator == null || actionRuntime == null)
            return;

        bool executingClimb =
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentParkourActionType ==
                ParkourActionType.Climb;

        if (!executingClimb)
        {
            climbEntryRequested = false;
            return;
        }

        // Request entry only once per Climb action.
        if (climbEntryRequested)
            return;

        ParkourActionData actionData =
            actionRuntime.CurrentParkourActionData;

        if (actionData == null ||
            string.IsNullOrWhiteSpace(actionData.animationStateName))
        {
            Debug.LogError(
                "ClimbAnimationEntry: Climb animation state is not configured.",
                this);

            climbEntryRequested = true;
            return;
        }

        string stateName = actionData.animationStateName.Trim();

        // Animator.CrossFade expects a state path.
        string fullStatePath =
            stateName.StartsWith("Base Layer.")
                ? stateName
                : "Base Layer." + stateName;

        int stateHash =
            Animator.StringToHash(fullStatePath);

        if (!animator.HasState(0, stateHash))
        {
            Debug.LogError(
                $"ClimbAnimationEntry: Animator state not found. " +
                $"Expected '{fullStatePath}'. Check the configured " +
                $"animationStateName and Animator state path.",
                this);

            climbEntryRequested = true;
            return;
        }

        AnimatorStateInfo currentState =
            animator.GetCurrentAnimatorStateInfo(0);

        if (!currentState.IsName(stateName) &&
            !currentState.IsName(fullStatePath))
        {
            animator.CrossFadeInFixedTime(
                fullStatePath,
                transitionDuration,
                0,
                0f);

            if (logTransitions)
            {
                Debug.Log(
                    $"CLIMB ANIMATION ENTRY REQUESTED | " +
                    $"State={fullStatePath} | " +
                    $"RuntimeAction={actionRuntime.CurrentActionType} | " +
                    $"ActionTime={actionRuntime.ActionTime:F3}",
                    this);
            }
        }

        climbEntryRequested = true;
    }
}
