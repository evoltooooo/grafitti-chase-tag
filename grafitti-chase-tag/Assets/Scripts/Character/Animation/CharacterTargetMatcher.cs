using UnityEngine;

public class CharacterTargetMatcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Debug")]
    [SerializeField] private bool logMatching = true;

    private CharacterActionType lastActionType =
        CharacterActionType.None;

    private int currentPhaseIndex = -1;
    private int lastQueuedLoop = -1;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (actionRuntime != null)
            actionRuntime.ActionCompleted += OnActionCompleted;
    }

    private void OnDestroy()
    {
        if (actionRuntime != null)
            actionRuntime.ActionCompleted -= OnActionCompleted;
    }

    private void OnActionCompleted()
    {
        ResetMatcher();
    }

    private void Update()
    {
        if (animator == null ||
            actionRuntime == null)
        {
            return;
        }

        // TEMPORARY CLIMB DIAGNOSTIC
        if (actionRuntime.IsExecuting &&
            actionRuntime.CurrentParkourActionType ==
                ParkourActionType.Climb)
        {
            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);

            CharacterMotor motor =
                GetComponent<CharacterMotor>();
        }

        // -------------------------------------------------
        // ACTION RESET
        // -------------------------------------------------

        if (!actionRuntime.IsExecuting)
        {
            ResetMatcher();
            return;
        }

        // -------------------------------------------------
        // NEW ACTION
        // -------------------------------------------------

        if (lastActionType !=
            actionRuntime.CurrentActionType)
        {
            lastActionType =
                actionRuntime.CurrentActionType;

            currentPhaseIndex = -1;
        }

        // -------------------------------------------------
        // PARKOUR CHECK
        // -------------------------------------------------

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.None)
        {
            return;
        }

        if (actionRuntime.CurrentMotionType !=
                ActionMotionType.TargetMatch &&
            actionRuntime.CurrentMotionType !=
                ActionMotionType.Hybrid &&
            actionRuntime.CurrentMotionType !=
                ActionMotionType.Specialized)
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

        ParkourActionData parkourActionData =
            actionRuntime.CurrentParkourActionData;

        if (parkourActionData == null)
        {
            return;
        }

        if (!parkourActionData.useTargetMatching)
        {
            return;
        }

        ParkourTarget target =
            actionRuntime.CurrentParkourTarget;

        if (!target.IsValid)
        {
            return;
        }

        TargetMatchPhase[] phases =
            parkourActionData.TargetMatchPhases;

        if (phases == null ||
            phases.Length == 0)
        {
            return;
        }

        // -------------------------------------------------
        // UNITY CAN ONLY HAVE ONE MATCH ACTIVE
        // -------------------------------------------------

        if (animator.isMatchingTarget)
        {
            return;
        }

        // -------------------------------------------------
        // FIND CURRENT ANIMATION PHASE
        // -------------------------------------------------

        if (animator.IsInTransition(0))
        {
            return;
        }

        
        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);

        // TEMPORARY: inspect the current and next Animator states.
        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.Climb)
        {
            bool inTransition = animator.IsInTransition(0);

            AnimatorStateInfo nextState =
                animator.GetNextAnimatorStateInfo(0);
        }

        // Keep your existing CLIMB ACTIVE CLIP diagnostic below.

        
        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.Climb)
        {
            AnimatorClipInfo[] clips =
                animator.GetCurrentAnimatorClipInfo(0);

            string clipNames = "";

            foreach (AnimatorClipInfo clip in clips)
            {
                clipNames +=
                    clip.clip.name + " (" +
                    clip.weight.ToString("F2") + ") ";
            }
        }
        
        int phaseIndex =
            FindMatchingPhase(phases, stateInfo);

        if (phaseIndex == -1)
        {
            return;
        }

        TargetMatchPhase phase =
            phases[phaseIndex];

        // -------------------------------------------------
        // STATE CHANGED
        // -------------------------------------------------

        if (currentPhaseIndex != phaseIndex)
        {
            currentPhaseIndex = phaseIndex;

            // Allow this phase to queue its match once.
            lastQueuedLoop = -1;
        }

        // -------------------------------------------------
        // ANIMATION TIME
        // -------------------------------------------------

        float normalizedTime =
            stateInfo.normalizedTime % 1f;

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.Vault)
        {
            Debug.Log(
                $"VAULT MATCH CHECK | " +
                $"State={stateInfo.fullPathHash} | " +
                $"NormalizedTime={normalizedTime:F3} | " +
                $"Start={phase.matchStartTime:F3} | " +
                $"End={phase.matchEndTime:F3} | " +
                $"IsMatching={animator.isMatchingTarget}"
            );
        }

        float matchStart =
            phase.matchStartTime;

        float matchEnd =
            phase.matchEndTime;

        if (normalizedTime < matchStart)
        {
            return;
        }

        if (normalizedTime > matchEnd)
        {
            return;
        }

        // Non-repeating phases may only queue one match.
        if (!phase.repeatWhileActionActive &&
            lastQueuedLoop == phaseIndex)
        {
            return;
        }

        // -------------------------------------------------
        // TARGET POSITION
        // -------------------------------------------------

        Vector3 targetPosition =
            target.InteractionPosition +
            phase.targetPositionOffset;

        Quaternion targetRotation =
            phase.matchRotation
                ? target.TargetRotation
                : transform.rotation;

        MatchTargetWeightMask weightMask =
            new MatchTargetWeightMask(
                Vector3.one,
                phase.matchRotation
                    ? 1f
                    : 0f
            );

        // -------------------------------------------------
        // START MATCH
        // -------------------------------------------------

        float targetStartTime = matchStart;
        float targetEndTime = matchEnd;

        if (phase.repeatWhileActionActive)
        {
            int currentLoop =
                Mathf.FloorToInt(normalizedTime);

            targetStartTime =
                currentLoop + matchStart;

            targetEndTime =
                currentLoop + matchEnd;

            // If we have already passed this loop's
            // matching window, schedule the NEXT loop.
            if (normalizedTime > targetEndTime)
            {
                currentLoop++;

                targetStartTime =
                    currentLoop + matchStart;

                targetEndTime =
                    currentLoop + matchEnd;
            }
        }

        ApplyTargetMatch(
            targetPosition,
            targetRotation,
            phase.matchBodyPart,
            weightMask,
            targetStartTime,
            targetEndTime
        );

        if (!phase.repeatWhileActionActive)
        {
            lastQueuedLoop = phaseIndex;
        }
    }

    private void ApplyTargetMatch(
        Vector3 targetPosition,
        Quaternion targetRotation,
        AvatarTarget bodyPart,
        MatchTargetWeightMask weightMask,
        float startTime,
        float endTime)
    {
        if (animator == null)
            return;

        if (!animator.applyRootMotion)
        {
            return;
        }

        if (animator.isMatchingTarget)
            return;
        
        Debug.Log(
            $"VAULT MATCH STARTED | " +
            $"TargetPosition={targetPosition:F3} | " +
            $"BodyPart={bodyPart} | " +
            $"Start={startTime:F3} | " +
            $"End={endTime:F3}"
        );

        if (actionRuntime.CurrentParkourActionType ==
            ParkourActionType.Vault)
        {
            Transform leftHand =
                animator.GetBoneTransform(
                    HumanBodyBones.LeftHand
                );

            Debug.Log(
                $"VAULT TARGET POSITION CHECK | " +
                $"Root={transform.position:F3} | " +
                $"LeftHand={(leftHand != null ? leftHand.position.ToString("F3") : "NULL")} | " +
                $"Target={targetPosition:F3} | " +
                $"HandToTargetDistance=" +
                $"{(leftHand != null ? Vector3.Distance(leftHand.position, targetPosition) : -1f):F3}"
            );
        }

        animator.MatchTarget(
            targetPosition,
            targetRotation,
            bodyPart,
            weightMask,
            startTime,
            endTime
        );
    }

    private int FindMatchingPhase(
        TargetMatchPhase[] phases,
        AnimatorStateInfo stateInfo)
    {
        for (int i = 0; i < phases.Length; i++)
        {
            TargetMatchPhase phase = phases[i];

            if (phase == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                phase.animationStateName))
            {
                continue;
            }

            if (stateInfo.IsName(
                phase.animationStateName))
            {
                return i;
            }
        }

        return -1;
    }

    private void ResetMatcher()
    {
        lastActionType =
            CharacterActionType.None;

        currentPhaseIndex = -1;

        lastQueuedLoop = -1;
    }
}