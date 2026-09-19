using UnityEngine;

public class CharacterStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    public CharacterState CurrentState { get; private set; }

    public bool IsBusy => CurrentState != CharacterState.Locomotion;

    private void Awake()
    {
        if (characterMotor == null)
        {
            characterMotor = GetComponent<CharacterMotor>();
        }

        if (actionRuntime == null)
        {
            actionRuntime = GetComponent<CharacterActionRuntime>();
        }

        CurrentState =
            CharacterState.Locomotion;

        Debug.Log(
            $"[{name}] Character State: {CurrentState}",
            this
        );
    }

    private void OnEnable()
    {
        if (actionRuntime != null)
            actionRuntime.ActionCompleted += OnActionCompleted;
    }

    private void OnDisable()
    {
        if (actionRuntime != null)
            actionRuntime.ActionCompleted -= OnActionCompleted;
    }

    private void Update()
    {
        if (characterMotor == null)
            return;

        UpdateMovementState();
    }

    private void UpdateMovementState()
    {
        switch (CurrentState)
        {
            case CharacterState.Jumping:
                if (characterMotor.VerticalVelocity < 0f)
                    SetState(CharacterState.Falling);
                break;

            case CharacterState.Falling:
                if (characterMotor.IsGrounded)
                    SetState(CharacterState.Landing);
                break;

            case CharacterState.Landing:
                if (characterMotor.IsGrounded)
                    SetState(CharacterState.Locomotion);
                break;
        }
    }

    public void SetState(CharacterState newState)
    {
        if (CurrentState == newState)
            return;

        CharacterState previousState = CurrentState;

        CurrentState = newState;

        Debug.Log(
            $"[{name}] Character State: " +
            $"{previousState} → {CurrentState}",
            this
        );
    }

    public bool CanEnterState(CharacterState newState)
    {
        if (CurrentState == newState)
            return false;

        if (IsBusy)
            return false;

        return true;
    }

    private void OnActionCompleted()
    {
        if (CurrentState == CharacterState.Locomotion)
            return;

        SetState(CharacterState.Locomotion);
    }
}