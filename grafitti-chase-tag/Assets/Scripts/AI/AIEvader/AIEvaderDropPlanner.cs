
using UnityEngine;
using UnityEngine.AI;

public class AIEvaderDropPlanner
{
    private const float EdgeSearchDistance = 3f;
    private const float EdgeStep = 0.25f;
    private const float NavMeshSampleRadius = 0.5f;
    private const float LandingProbeHeight = 0.5f;
    private const float MinimumDropHeight = 0.75f;

    private static readonly float[] SearchAngles =
    {
        0f, -30f, 30f, -60f, 60f, -90f, 90f,
        -120f, 120f, 180f
    };

    public bool TryFindDropCandidate(
        Vector3 currentPosition,
        Vector3 escapeDirection,
        Transform selfTransform,
        LayerMask landingMask,
        float maximumDropHeight,
        out Vector3 approachPoint,
        out Vector3 landingPoint)
    {
        approachPoint = Vector3.zero;
        landingPoint = Vector3.zero;

        if (selfTransform == null ||
            maximumDropHeight <= MinimumDropHeight)
        {
            return false;
        }

        escapeDirection.y = 0f;

        if (escapeDirection.sqrMagnitude < 0.001f)
            escapeDirection = selfTransform.forward;

        escapeDirection.y = 0f;
        escapeDirection.Normalize();

        foreach (float angle in SearchAngles)
        {
            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) *
                escapeDirection;

            for (float distance = EdgeStep;
                 distance <= EdgeSearchDistance;
                 distance += EdgeStep)
            {
                Vector3 outsidePoint =
                    currentPosition + direction * distance;

                Vector3 sameHeightPoint = new Vector3(
                    outsidePoint.x,
                    currentPosition.y,
                    outsidePoint.z);

                // The point outside the edge should not be on
                // the same upper NavMesh surface.
                if (NavMesh.SamplePosition(
                    sameHeightPoint,
                    out NavMeshHit upperHit,
                    NavMeshSampleRadius,
                    NavMesh.AllAreas))
                {
                    if (Mathf.Abs(
                        upperHit.position.y - currentPosition.y) < 0.5f)
                    {
                        continue;
                    }
                }

                // Find a reachable approach point just inside
                // the edge, on the current platform.
                Vector3 insidePoint =
                    outsidePoint - direction * EdgeStep;

                if (!NavMesh.SamplePosition(
                    insidePoint,
                    out NavMeshHit approachHit,
                    NavMeshSampleRadius,
                    NavMesh.AllAreas))
                {
                    continue;
                }

                if (Mathf.Abs(
                    approachHit.position.y - currentPosition.y) > 0.5f)
                {
                    continue;
                }

                // Probe down from beyond the edge.
                Vector3 rayOrigin =
                    outsidePoint +
                    Vector3.up * LandingProbeHeight;

                RaycastHit[] hits = Physics.RaycastAll(
                    rayOrigin,
                    Vector3.down,
                    maximumDropHeight + LandingProbeHeight,
                    landingMask,
                    QueryTriggerInteraction.Ignore);

                System.Array.Sort(
                    hits,
                    (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hit in hits)
                {
                    // Ignore the AI's own colliders.
                    if (hit.transform == selfTransform ||
                        hit.transform.IsChildOf(selfTransform))
                    {
                        continue;
                    }

                    float dropHeight =
                        currentPosition.y - hit.point.y;

                    if (dropHeight < MinimumDropHeight ||
                        dropHeight > maximumDropHeight)
                    {
                        continue;
                    }

                    approachPoint = approachHit.position;
                    landingPoint = hit.point;

                    Debug.Log(
                        $"EVADER DROP CANDIDATE FOUND | " +
                        $"Approach={approachPoint} | " +
                        $"Landing={landingPoint} | " +
                        $"DropHeight={dropHeight:F2} | " +
                        $"Direction={direction}");

                    return true;
                }
            }
        }

        Debug.Log(
            $"EVADER DROP CANDIDATE NOT FOUND | " +
            $"Position={currentPosition}");

        return false;
    }
}
