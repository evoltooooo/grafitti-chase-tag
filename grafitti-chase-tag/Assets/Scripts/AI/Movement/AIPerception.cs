using UnityEngine;

public class AIPerception : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("AI")]
    [SerializeField] private CharacterMotor characterMotor;

    [Header("Opponent")]
    [SerializeField] private Transform opponentTransform;
    [SerializeField] private CharacterMotor opponentMotor;

    [Header("Vision")]
    [SerializeField] private float visionRange = 30f;
    [SerializeField] private LayerMask visionBlockingLayers;

    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float targetHeight = 1.0f;


    // =========================================================
    // CURRENT PERCEPTION
    // =========================================================

    public bool OpponentVisible { get; private set; }

    public Vector3 OpponentPosition { get; private set; }

    public Vector3 OpponentVelocity { get; private set; }


    // =========================================================
    // MEMORY
    // =========================================================

    public Vector3 LastKnownOpponentPosition { get; private set; }

    public Vector3 LastKnownOpponentVelocity { get; private set; }

    public float TimeSinceOpponentSeen { get; private set; }


    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();

        TimeSinceOpponentSeen = Mathf.Infinity;
    }


    private void Update()
    {
        UpdatePerception();
    }


    // =========================================================
    // PERCEPTION
    // =========================================================

    private void UpdatePerception()
    {
        if (opponentTransform == null)
        {
            OpponentVisible = false;
            TimeSinceOpponentSeen += Time.deltaTime;
            return;
        }

        OpponentPosition =
            opponentTransform.position;

        if (opponentMotor != null)
        {
            OpponentVelocity =
                opponentMotor.Velocity;
        }

        bool inRange =
            IsOpponentInRange();

        Collider blockingCollider = null;

        bool hasLineOfSight =
            inRange && HasLineOfSight(out blockingCollider);

        if (OpponentVisible != hasLineOfSight)
        {
            float distance = Vector3.Distance(
                characterMotor.transform.position,
                opponentTransform.position
            );

            Debug.Log(
                $"AI PERCEPTION VISIBILITY CHANGE | " +
                $"Visible={hasLineOfSight} | " +
                $"InRange={inRange} | " +
                $"Distance={distance:F2} | " +
                $"Blocker={(blockingCollider != null ? blockingCollider.name : "None")} | " +
                $"AI={characterMotor.transform.position} | " +
                $"Player={opponentTransform.position}"
            );
        }

        OpponentVisible = hasLineOfSight;

        if (OpponentVisible)
        {
            LastKnownOpponentPosition =
                OpponentPosition;

            LastKnownOpponentVelocity =
                OpponentVelocity;

            TimeSinceOpponentSeen = 0f;
        }
        else
        {
            TimeSinceOpponentSeen +=
                Time.deltaTime;
        }
    }


    // =========================================================
    // RANGE
    // =========================================================

    private bool IsOpponentInRange()
    {
        float distance =
            Vector3.Distance(
                characterMotor.transform.position,
                opponentTransform.position
            );

        return distance <= visionRange;
    }


    // =========================================================
    // LINE OF SIGHT
    // =========================================================

    private bool HasLineOfSight(out Collider blockingCollider)
    {
        blockingCollider = null;

        Vector3 origin =
            characterMotor.transform.position +
            Vector3.up * eyeHeight;

        Vector3 target =
            opponentTransform.position +
            Vector3.up * targetHeight;

        Vector3 direction = target - origin;

        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction.Normalize();

        Debug.DrawLine(
            origin,
            target,
            Color.green
        );

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            distance,
            visionBlockingLayers,
            QueryTriggerInteraction.Ignore))
        {
            blockingCollider = hit.collider;
            return false;
        }

        return true;
    }
}