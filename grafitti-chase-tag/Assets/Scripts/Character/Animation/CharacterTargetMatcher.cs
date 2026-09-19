using UnityEngine;

public class CharacterTargetMatcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Debug")]
    [SerializeField] private bool logMatching = true;

    private bool hasStartedMatching;

    private CharacterActionType lastActionType =
        CharacterActionType.None;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();
    }

    private void Update()
    {
        if (animator == null ||
            actionRuntime == null)
        {
            return;
        }

        // -------------------------------------------------
        // ACTION RESET
        // -------------------------------------------------

        if (!actionRuntime.IsExecuting)
        {
            hasStartedMatching = false;
            lastActionType = CharacterActionType.None;
            return;
        }

        // New action started.
        if (lastActionType !=
            actionRuntime.CurrentActionType)
        {
            lastActionType =
                actionRuntime.CurrentActionType;

            hasStartedMatching = false;

            if (logMatching)
            {
                Debug.Log(
                    $"TARGET MATCH ACTION DETECTED | " +
                    $"Action={actionRuntime.CurrentActionType} | " +
                    $"Motion={actionRuntime.CurrentMotionType} | " +
                    $"Parkour={actionRuntime.CurrentParkourActionType}"
                );
            }
        }

        // -------------------------------------------------
        // MOTION TYPE
        // -------------------------------------------------

        // Only parkour actions can use the current
        // target-matching implementation.
        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.None)
        {
            return;
        }

        if (actionRuntime.CurrentMotionType !=
                ActionMotionType.TargetMatch &&
            actionRuntime.CurrentMotionType !=
                ActionMotionType.Hybrid)
        {
            return;
        }

        TryMatchCurrentAction();
    }

    private void TryMatchCurrentAction()
    {
        if (actionRuntime == null ||
            !actionRuntime.IsExecuting)
        {
            return;
        }

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.None)
        {
            return;
        }

        if (actionRuntime.CurrentMotionType != ActionMotionType.TargetMatch &&
            actionRuntime.CurrentMotionType != ActionMotionType.Hybrid)
        {
            return;
        }

        if (hasStartedMatching)
            return;

        ParkourActionData parkourActionData =
            actionRuntime.CurrentParkourActionData;

        if (parkourActionData == null)
        {
            Debug.LogWarning(
                $"TARGET MATCH FAILED | " +
                $"Runtime has no ParkourActionData for " +
                $"{actionRuntime.CurrentParkourActionType}."
            );
            return;
        }

        if (!parkourActionData.useTargetMatching)
        {
            Debug.Log(
                $"TARGET MATCH FAILED | " +
                $"Target Matching disabled for " +
                $"{parkourActionData.actionType}"
            );
            return;
        }

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        if (!target.IsValid)
        {
            Debug.LogWarning(
                $"TARGET MATCH FAILED | " +
                $"ParkourTarget is invalid."
            );
            return;
        }

        // -------------------------------------------------
        // ANIMATION STATE
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                parkourActionData.animationStateName))
        {
            Debug.LogWarning(
                $"TARGET MATCH FAILED | " +
                $"Animation state name is empty."
            );
            return;
        }

        if (animator.IsInTransition(0))
            return;

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);

        Debug.Log(
            $"VAULT STATE DEBUG | " +
            $"Configured={parkourActionData.animationStateName} | " +
            $"IsName={stateInfo.IsName(parkourActionData.animationStateName)} | " +
            $"ShortHash={stateInfo.shortNameHash} | " +
            $"FullHash={stateInfo.fullPathHash}"
        );

        bool correctState =
            stateInfo.IsName(
                parkourActionData.animationStateName
            );

        if (!correctState)
        {
            if (logMatching)
            {
                Debug.Log(
                    $"TARGET MATCH STATE WAIT | " +
                    $"Action={actionRuntime.CurrentActionType} | " +
                    $"Expected={parkourActionData.animationStateName} | " +
                    $"ShortHash={stateInfo.shortNameHash} | " +
                    $"FullHash={stateInfo.fullPathHash}"
                );
            }

            return;
        }

        // -------------------------------------------------
        // ANIMATION TIME
        // -------------------------------------------------

        float normalizedTime =
            stateInfo.normalizedTime % 1f;

        Debug.Log(
            $"VAULT MATCH TIME | " +
            $"Normalized={normalizedTime:0.000} | " +
            $"Start={parkourActionData.matchStartTime:0.000} | " +
            $"End={parkourActionData.matchEndTime:0.000}"
        );

        float matchStart =
            parkourActionData.matchStartTime;

        float matchEnd =
            parkourActionData.matchEndTime;

        if (normalizedTime < matchStart ||
            normalizedTime > matchEnd)
        {
            return;
        }

        if (animator.isMatchingTarget)
        {
            hasStartedMatching = true;
            return;
        }

        // -------------------------------------------------
        // TARGET
        // -------------------------------------------------

        Vector3 targetPosition =
            target.InteractionPosition +
            parkourActionData.targetPositionOffset;

        Quaternion targetRotation =
            parkourActionData.matchRotation
                ? target.TargetRotation
                : transform.rotation;

        MatchTargetWeightMask weightMask =
            new MatchTargetWeightMask(
                Vector3.one,
                parkourActionData.matchRotation
                    ? 1f
                    : 0f
            );

        // -------------------------------------------------
        // START TARGET MATCH
        // -------------------------------------------------

        animator.MatchTarget(
            targetPosition,
            targetRotation,
            parkourActionData.matchBodyPart,
            weightMask,
            matchStart,
            matchEnd
        );

        Debug.Log(
            $"VAULT MATCH POSITION DEBUG | " +
            $"Player={transform.position} | " +
            $"Target={targetPosition} | " +
            $"Distance={Vector3.Distance(transform.position, targetPosition):0.00}"
        );

        hasStartedMatching = true;

        Debug.Log(
            $"TARGET MATCHING STARTED\n" +
            $"Action: {actionRuntime.CurrentActionType}\n" +
            $"Parkour: {actionRuntime.CurrentParkourActionType}\n" +
            $"Animation: {parkourActionData.animationStateName}\n" +
            $"Body Part: {parkourActionData.matchBodyPart}\n" +
            $"Normalized Time: {normalizedTime:0.00}\n" +
            $"Window: {matchStart:0.00} -> {matchEnd:0.00}\n" +
            $"Target: {targetPosition}",
            this
        );
    }
}