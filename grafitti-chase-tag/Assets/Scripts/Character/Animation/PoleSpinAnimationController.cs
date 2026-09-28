using UnityEngine;

public class PoleSpinAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Pole Spin Animation")]
    [Range(0f, 1f)]
    [SerializeField] private float holdStart = 0.23f;

    [Range(0f, 1f)]
    [SerializeField] private float holdEnd = 0.518f;

    [Header("Animator State")]
    [SerializeField] private string poleSpinStateName = "PoleSpin";

    private bool controllingPoleSpin;
    private int poleSpinStateHash;

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

        poleSpinStateHash =
            Animator.StringToHash(poleSpinStateName);
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
        // FIRST FRAME OF POLE SPIN
        // -------------------------------------------------

        if (!controllingPoleSpin)
        {
            controllingPoleSpin = true;

            Debug.Log(
                $"POLE SPIN ANIMATION START | " +
                $"Playing start section 0.000->{holdStart:0.000}",
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
                0,
                0,
                holdStart
            );
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