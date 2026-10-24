
using UnityEngine;

public class CharacterTagDetector : MonoBehaviour
{
    [Header("Cone Detection")]
    [SerializeField, Min(0.1f)] private float tagDistance = 2.5f;

    [Tooltip("Half-angle of the cone. 35 means 70 degrees total.")]
    [SerializeField, Range(1f, 85f)]
    private float tagHalfAngle = 35f;

    [SerializeField] private float originHeight = 1f;
    [SerializeField] private LayerMask tagLayers;

    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Tag Attempt Debug")]
    [SerializeField] private bool showConeOnTagAttempt = true;

    [SerializeField, Min(0.1f)]
    private float debugConeDuration = 1f;

    [SerializeField, Range(4, 48)]
    private int debugConeSegments = 24;

    public float TagDistance => tagDistance;
    public float TagHalfAngle => tagHalfAngle;
    public float OriginHeight => originHeight;

    public bool TryDetectTag(out Collider target)
    {
        target = null;

        Vector3 origin =
            transform.position + Vector3.up * originHeight;

        // Use the horizontal facing direction for a ground-based spray.
        Vector3 forward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        ).normalized;

        DrawDebugCone(origin, forward);

        if (forward.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning(
                $"TAG CONE FAILED | {name} has no horizontal forward direction.",
                this
            );
            return false;
        }

        Collider[] candidates = Physics.OverlapSphere(
            origin,
            tagDistance,
            tagLayers,
            QueryTriggerInteraction.Ignore
        );

        float bestDistance = float.MaxValue;

        foreach (Collider candidate in candidates)
        {
            if (candidate == null)
                continue;

            Vector3 closestPoint = candidate.ClosestPoint(origin);
            Vector3 toTarget = closestPoint - origin;
            Vector3 horizontalToTarget =
                Vector3.ProjectOnPlane(toTarget, Vector3.up);

            float horizontalDistance = horizontalToTarget.magnitude;

            float angle = horizontalDistance > 0.001f
                ? Vector3.Angle(forward, horizontalToTarget)
                : -1f;

            Debug.Log(
                $"TAG DEBUG CANDIDATE | " +
                $"Detector={name} | " +
                $"Collider={candidate.name} | " +
                $"Root={candidate.transform.root.name} | " +
                $"IsSelf={candidate.transform.root == transform.root} | " +
                $"Layer={LayerMask.LayerToName(candidate.gameObject.layer)} | " +
                $"BoundsCenter={candidate.bounds.center} | " +
                $"ClosestPoint={closestPoint} | " +
                $"HorizontalDistance={horizontalDistance:F2} | " +
                $"Angle={angle:F1} | " +
                $"Range={tagDistance:F2}",
                this
            );

            // Ignore colliders belonging to this character, but not
            // colliders belonging to another character under a shared parent.
            if (candidate.transform == transform ||
                candidate.transform.IsChildOf(transform))
            {
                Debug.Log(
                    $"TAG DEBUG REJECTED | Own character collider: {candidate.name}",
                    this
                );
                continue;
            }
            if (horizontalDistance < 0.001f)
            {
                Debug.Log("TAG DEBUG REJECTED | Horizontal distance is near zero.", this);
                continue;
            }

            if (horizontalDistance > tagDistance)
            {
                Debug.Log("TAG DEBUG REJECTED | Outside horizontal range.", this);
                continue;
            }

            if (angle > tagHalfAngle)
            {
                Debug.Log("TAG DEBUG REJECTED | Outside forward cone.", this);
                continue;
            }

            if (horizontalDistance < bestDistance)
            {
                bestDistance = horizontalDistance;
                target = candidate;
            }
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"TAG CONE MISS | Detector={name} | " +
                $"OverlapCandidates={candidates.Length} | " +
                $"TagLayers={tagLayers.value} | " +
                $"Origin={origin} | Forward={forward} | " +
                $"Range={tagDistance:F2} | " +
                $"HalfAngle={tagHalfAngle:F1}",
                this
            );

            return false;
        }

        Debug.Log(
            $"TAG CONE HIT | Tagger={transform.root.name} | " +
            $"Target={target.name} | Distance={bestDistance:F2}",
            this
        );

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        Vector3 origin =
            transform.position + Vector3.up * originHeight;

        Vector3 forward = transform.forward.normalized;

        float halfAngle = tagHalfAngle * Mathf.Deg2Rad;

        Vector3 leftBoundary =
            Quaternion.AngleAxis(-tagHalfAngle, transform.up)
            * forward;

        Vector3 rightBoundary =
            Quaternion.AngleAxis(tagHalfAngle, transform.up)
            * forward;

        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(origin, forward * tagDistance);
        Gizmos.DrawRay(origin, leftBoundary * tagDistance);
        Gizmos.DrawRay(origin, rightBoundary * tagDistance);

        // Draw the cone's outer arc.
        Vector3 previousPoint =
            origin + leftBoundary * tagDistance;

        const int segments = 24;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(
                -tagHalfAngle,
                tagHalfAngle,
                i / (float)segments
            );

            Vector3 direction =
                Quaternion.AngleAxis(
                    angle * Mathf.Rad2Deg,
                    transform.up
                ) * forward;

            Vector3 nextPoint =
                origin + direction * tagDistance;

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    private void DrawDebugCone(Vector3 origin, Vector3 forward)
    {
        if (!showConeOnTagAttempt)
            return;

        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.001f)
            return;

        Vector3 previousPoint = origin +
            Quaternion.AngleAxis(-tagHalfAngle, Vector3.up)
            * forward * tagDistance;

        // Draw both cone boundaries.
        Vector3 leftBoundary =
            Quaternion.AngleAxis(-tagHalfAngle, Vector3.up)
            * forward;

        Vector3 rightBoundary =
            Quaternion.AngleAxis(tagHalfAngle, Vector3.up)
            * forward;

        Debug.DrawRay(
            origin,
            leftBoundary * tagDistance,
            Color.yellow,
            debugConeDuration
        );

        Debug.DrawRay(
            origin,
            rightBoundary * tagDistance,
            Color.yellow,
            debugConeDuration
        );

        // Draw the outer arc and radial lines to reveal cone width.
        for (int i = 1; i <= debugConeSegments; i++)
        {
            float angle = Mathf.Lerp(
                -tagHalfAngle,
                tagHalfAngle,
                i / (float)debugConeSegments
            );

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up) * forward;

            Vector3 nextPoint = origin + direction * tagDistance;

            Debug.DrawLine(
                previousPoint,
                nextPoint,
                Color.cyan,
                debugConeDuration
            );

            // Radial lines make the cone easier to judge.
            if (i % 3 == 0)
            {
                Debug.DrawLine(
                    origin,
                    nextPoint,
                    Color.cyan,
                    debugConeDuration
                );
            }

            previousPoint = nextPoint;
        }

        // Highlight the center direction.
        Debug.DrawRay(
            origin,
            forward * tagDistance,
            Color.green,
            debugConeDuration
        );
    }
}
