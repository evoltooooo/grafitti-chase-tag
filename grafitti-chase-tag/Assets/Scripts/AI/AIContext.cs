using UnityEngine;

public class AIContext
{
    // =========================================================
    // SELF
    // =========================================================

    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 HorizontalVelocity { get; private set; }

    public float HorizontalSpeed { get; private set; }

    public bool IsGrounded { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsSprinting { get; private set; }

    public Vector3 Forward { get; private set; }


    // =========================================================
    // STAMINA
    // =========================================================

    public float CurrentStamina { get; private set; }
    public float MaxStamina { get; private set; }
    public float NormalizedStamina { get; private set; }

    public bool IsStaminaEmpty { get; private set; }
    public bool IsStaminaFull { get; private set; }


    // =========================================================
    // ROLE
    // =========================================================

    public CharacterRoleType Role { get; private set; }

    public bool IsChaser =>
        Role == CharacterRoleType.Chaser;

    public bool IsEvader =>
        Role == CharacterRoleType.Evader;


    // =========================================================
    // OPPONENT
    // =========================================================

    public Vector3 OpponentPosition { get; private set; }
    public Vector3 OpponentVelocity { get; private set; }

    public bool OpponentVisible { get; private set; }

    public Vector3 LastKnownOpponentPosition { get; private set; }
    public Vector3 LastKnownOpponentVelocity { get; private set; }

    public float TimeSinceOpponentSeen { get; private set; }


    // =========================================================
    // CURRENT ACTION
    // =========================================================

    public bool IsExecutingAction { get; private set; }

    public CharacterActionType CurrentActionType { get; private set; }

    public ParkourActionType CurrentParkourActionType
    {
        get;
        private set;
    }

    public ActionMotionType CurrentMotionType
    {
        get;
        private set;
    }

    public float ActionNormalizedTime { get; private set; }


    // =========================================================
    // UPDATE
    // =========================================================

    public void Update(
        CharacterMotor selfMotor,
        CharacterStamina selfStamina,
        CharacterActionRuntime selfActionRuntime,
        CharacterRole selfRole,

        Transform opponentTransform,
        CharacterMotor opponentMotor,

        bool opponentVisible,
        Vector3 lastKnownOpponentPosition,
        Vector3 lastKnownOpponentVelocity,
        float timeSinceOpponentSeen)
    {
        // -----------------------------------------------------
        // SELF
        // -----------------------------------------------------

        Position = selfMotor.transform.position;

        Velocity = selfMotor.Velocity;

        HorizontalVelocity = new Vector3(
            Velocity.x,
            0f,
            Velocity.z
        );

        HorizontalSpeed = HorizontalVelocity.magnitude;

        Forward = selfMotor.transform.forward;

        IsGrounded = selfMotor.IsGrounded;

        IsJumping = !IsGrounded;


        // -----------------------------------------------------
        // STAMINA
        // -----------------------------------------------------

        CurrentStamina = selfStamina.CurrentStamina;
        MaxStamina = selfStamina.MaxStamina;
        NormalizedStamina = selfStamina.NormalizedStamina;

        IsStaminaEmpty = selfStamina.IsEmpty;
        IsStaminaFull = selfStamina.IsFull;


        // -----------------------------------------------------
        // ROLE
        // -----------------------------------------------------

        Role = selfRole.CurrentRole;


        // -----------------------------------------------------
        // OPPONENT
        // -----------------------------------------------------

        if (opponentTransform != null)
        {
            OpponentPosition =
                opponentTransform.position;
        }

        if (opponentMotor != null)
        {
            OpponentVelocity =
                opponentMotor.Velocity;
        }

        OpponentVisible = opponentVisible;

        LastKnownOpponentPosition =
            lastKnownOpponentPosition;

        LastKnownOpponentVelocity =
            lastKnownOpponentVelocity;

        TimeSinceOpponentSeen =
            timeSinceOpponentSeen;


        // -----------------------------------------------------
        // CURRENT ACTION
        // -----------------------------------------------------

        IsExecutingAction =
            selfActionRuntime.IsExecuting;

        CurrentActionType =
            selfActionRuntime.CurrentActionType;

        CurrentParkourActionType =
            selfActionRuntime.CurrentParkourActionType;

        CurrentMotionType =
            selfActionRuntime.CurrentMotionType;

        ActionNormalizedTime =
            selfActionRuntime.ActionNormalizedTime;
    }
}