using UnityEngine;

public class AIChaserCommitment
{
    private AIChaserState committedState;
    private float commitmentTimer;

    private readonly float minimumCommitmentTime;

    public AIChaserCommitment(
        float minimumCommitmentTime)
    {
        this.minimumCommitmentTime =
            minimumCommitmentTime;
    }

    public void SetInitialState(
        AIChaserState state)
    {
        committedState = state;
        commitmentTimer = minimumCommitmentTime;
    }

    public AIChaserState CurrentState =>
        committedState;

    public bool CanChangeState =>
        commitmentTimer <= 0f;

    public void Update(float deltaTime)
    {
        if (commitmentTimer <= 0f)
            return;

        commitmentTimer -= deltaTime;
    }

    public bool TryChangeState(
        AIChaserState desiredState)
    {
        if (desiredState == committedState)
            return false;

        if (!CanChangeState)
            return false;

        committedState = desiredState;
        commitmentTimer =
            minimumCommitmentTime;

        return true;
    }

    public void ForceState(
        AIChaserState state)
    {
        committedState = state;
        commitmentTimer =
            minimumCommitmentTime;
    }
}