
using UnityEngine;

public class CharacterTagDetector : MonoBehaviour
{
    [Header("Cone Detection")]
    [SerializeField, Min(0.1f)]
    private float tagDistance = 2.5f;

    [Tooltip("Base half-angle of the cone. 35 means 70 degrees total.")]
    [SerializeField, Range(1f, 85f)]
    private float tagHalfAngle = 35f;

    [SerializeField] private float originHeight = 1f;
    [SerializeField] private LayerMask tagLayers;

    [Header("Buff / Nerf")]
    [SerializeField] private CharacterBuffNerf buffNerf;

    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Tag Attempt Debug")]
    [SerializeField] private bool showConeOnTagAttempt = true;

    [SerializeField, Min(0.1f)]
    private float debugConeDuration = 1f;

    [SerializeField, Range(4, 48)]
    private int debugConeSegments = 24;

    // Base geometry
    public float BaseTagDistance => tagDistance;

    public float BaseTagRadius =>
        tagDistance * Mathf.Tan(tagHalfAngle * Mathf.Deg2Rad);

    // Effective geometry after Buff/Nerf modifiers
    public float EffectiveTagDistance =>
        tagDistance * GetRangeMultiplier();

    public float EffectiveTagRadius =>
        BaseTagRadius * GetRadiusMultiplier();

    public float EffectiveTagHalfAngle =>
        Mathf.Atan2(
            EffectiveTagRadius,
            EffectiveTagDistance
        ) * Mathf.Rad2Deg;

    // Compatibility properties
    public float TagDistance => EffectiveTagDistance;
    public float TagHalfAngle => EffectiveTagHalfAngle;
    public float OriginHeight => originHeight;

    private void Awake()
    {
        if (buffNerf == null)
        {
            buffNerf = GetComponent<CharacterBuffNerf>();
        }
    }

    private float GetRangeMultiplier()
    {
        return buffNerf != null
            ? buffNerf.GetMultiplier(
                BuffNerfEffectType.TagSprayRangeMultiplier)
            : 1f;
    }

    private float GetRadiusMultiplier()
    {
        return buffNerf != null
            ? buffNerf.GetMultiplier(
                BuffNerfEffectType.TagSprayRadiusMultiplier)
            : 1f;
    }

    public bool TryDetectTag(out Collider target)
    {
        target = null;

        float effectiveDistance = EffectiveTagDistance;
        float effectiveHalfAngle = EffectiveTagHalfAngle;

        Vector3 origin =
            transform.position + Vector3.up * originHeight;

        // Use the horizontal facing direction for a ground-based spray.
        Vector3 forward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        ).normalized;

        DrawDebugCone(
            origin,
            forward,
            effectiveDistance,
            effectiveHalfAngle
        );

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
            effectiveDistance,
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

            float horizontalDistance =
                horizontalToTarget.magnitude;

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
                $"Range={effectiveDistance:F2} | " +
                $"Radius={EffectiveTagRadius:F2} | " +
                $"HalfAngle={effectiveHalfAngle:F1}",
                this
            );

            // Ignore colliders belonging to this character.
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
                Debug.Log(
                    "TAG DEBUG REJECTED | Horizontal distance is near zero.",
                    this
                );

                continue;
            }

            if (horizontalDistance > effectiveDistance)
            {
                Debug.Log(
                    "TAG DEBUG REJECTED | Outside horizontal range.",
                    this
                );

                continue;
            }

            if (angle > effectiveHalfAngle)
            {
                Debug.Log(
                    "TAG DEBUG REJECTED | Outside forward cone.",
                    this
                );

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
                $"Range={effectiveDistance:F2} | " +
                $"Radius={EffectiveTagRadius:F2} | " +
                $"HalfAngle={effectiveHalfAngle:F1}",
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

        float effectiveDistance = EffectiveTagDistance;
        float effectiveHalfAngle = EffectiveTagHalfAngle;

        Vector3 origin =
            transform.position + Vector3.up * originHeight;

        Vector3 forward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        ).normalized;

        if (forward.sqrMagnitude < 0.001f)
            return;

        Vector3 leftBoundary =
            Quaternion.AngleAxis(-effectiveHalfAngle, Vector3.up)
            * forward;

        Vector3 rightBoundary =
            Quaternion.AngleAxis(effectiveHalfAngle, Vector3.up)
            * forward;

        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(origin, forward * effectiveDistance);
        Gizmos.DrawRay(origin, leftBoundary * effectiveDistance);
        Gizmos.DrawRay(origin, rightBoundary * effectiveDistance);

        Vector3 previousPoint =
            origin + leftBoundary * effectiveDistance;

        const int segments = 24;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(
                -effectiveHalfAngle,
                effectiveHalfAngle,
                i / (float)segments
            );

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up)
                * forward;

            Vector3 nextPoint =
                origin + direction * effectiveDistance;

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    private void DrawDebugCone(
        Vector3 origin,
        Vector3 forward,
        float effectiveDistance,
        float effectiveHalfAngle)
    {
        if (!showConeOnTagAttempt)
            return;

        forward = Vector3.ProjectOnPlane(
            forward,
            Vector3.up
        ).normalized;

        if (forward.sqrMagnitude < 0.001f)
            return;

        Vector3 leftBoundary =
            Quaternion.AngleAxis(-effectiveHalfAngle, Vector3.up)
            * forward;

        Vector3 rightBoundary =
            Quaternion.AngleAxis(effectiveHalfAngle, Vector3.up)
            * forward;

        Vector3 previousPoint =
            origin + leftBoundary * effectiveDistance;

        Debug.DrawRay(
            origin,
            leftBoundary * effectiveDistance,
            Color.yellow,
            debugConeDuration
        );

        Debug.DrawRay(
            origin,
            rightBoundary * effectiveDistance,
            Color.yellow,
            debugConeDuration
        );

        for (int i = 1; i <= debugConeSegments; i++)
        {
            float angle = Mathf.Lerp(
                -effectiveHalfAngle,
                effectiveHalfAngle,
                i / (float)debugConeSegments
            );

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up)
                * forward;

            Vector3 nextPoint =
                origin + direction * effectiveDistance;

            Debug.DrawLine(
                previousPoint,
                nextPoint,
                Color.cyan,
                debugConeDuration
            );

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

        Debug.DrawRay(
            origin,
            forward * effectiveDistance,
            Color.green,
            debugConeDuration
        );
    }
}
