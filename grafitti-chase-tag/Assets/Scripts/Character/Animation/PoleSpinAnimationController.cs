using UnityEngine;

public class PoleSpinAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private CharacterMotor characterMotor;

    [Header("Pole Spin Animation")]
    [Range(0f, 1f)]
    [SerializeField] private float startEnd = 0.05f;

    [Range(0f, 1f)]
    [SerializeField] private float holdEnd = 0.23f;

    [Range(0f, 1f)]
    [SerializeField] private float releaseStart = 0.518f;

    [Header("Animator State")]
    [SerializeField] private string poleSpinStateName = "PoleSpin";

    private bool controllingPoleSpin;
    
    private bool releaseStarted;

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (actionRuntime == null)
        {
            actionRuntime =
                GetComponent<CharacterActionRuntime>();
        }

        if (characterMotor == null)
        {
            characterMotor = GetComponent<CharacterMotor>();
        }
    }

    private void Update()
    {
        if (animator == null ||
            actionRuntime == null)
        {
            return;
        }

        // -------------------------------------------------
        // NOT POLE SPIN
        // -------------------------------------------------

        if (!IsPoleSpinExecuting())
        {
            controllingPoleSpin = false;

            if (animator.speed == 0f)
                animator.speed = 1f;

            return;
        }

        // -------------------------------------------------
        // WAIT UNTIL POLE SPIN STATE IS ACTIVE
        // -------------------------------------------------

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(poleSpinStateName))
        {
            return;
        }

        // -------------------------------------------------
        // RELEASE
        // -------------------------------------------------

        if (actionRuntime.IsReleasing)
        {
            if (!releaseStarted)
            {
                releaseStarted = true;

                animator.speed = 1f;

                animator.Play(
                    poleSpinStateName,
                    0,
                    releaseStart
                );

                Debug.Log(
                    $"POLE SPIN RELEASE START | Normalized={releaseStart:0.000}",
                    this
                );
            }

            AnimatorStateInfo releaseState =
                animator.GetCurrentAnimatorStateInfo(0);

            if (releaseState.normalizedTime >= 1f)
            {
                if (characterMotor != null)
                {
                    characterMotor.StopHorizontalMovement();
                }

                actionRuntime.CompleteAction();

                controllingPoleSpin = false;
                releaseStarted = false;
            }

            return;
        }

        // -------------------------------------------------
        // FIRST FRAME OF POLE SPIN
        // -------------------------------------------------

        if (!controllingPoleSpin)
        {
            controllingPoleSpin = true;

            Debug.Log(
                $"POLE SPIN ANIMATION START | " +
                $"Playing start section 0.000->{startEnd:0.000}",
                this
            );

            return;
        }

        // -------------------------------------------------
        // HOLD LOOP
        // -------------------------------------------------

        float normalizedTime =
            stateInfo.normalizedTime % 1f;

        if (normalizedTime >= holdEnd)
        {
            animator.Play(
                poleSpinStateName,
                0,
                holdEnd
            );

            animator.speed = 0f;

            return;
        }
    }

    private bool IsPoleSpinExecuting()
    {
        return
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentParkourActionType ==
            ParkourActionType.PoleSpin;
    }
}