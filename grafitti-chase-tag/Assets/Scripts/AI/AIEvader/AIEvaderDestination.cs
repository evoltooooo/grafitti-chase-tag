
using UnityEngine;
using UnityEngine.AI;

public class AIEvaderDestination
{
    private const float EscapeDistance = 8f;
    private const float SampleRadius = 2f;
    private const float PathLengthPenalty = 0.2f;

    private static readonly float[] CandidateAngles =
    {
        0f, -45f, 45f, -90f, 90f, -135f, 135f, 180f
    };

    public bool TryCalculate(
        AIContext context,
        Vector3 threatPosition,
        out Vector3 bestDestination)
    {
        bestDestination = context.Position;

        Vector3 awayDirection =
            context.Position - threatPosition;

        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.001f)
        {
            awayDirection = context.Forward;
            awayDirection.y = 0f;
        }

        if (awayDirection.sqrMagnitude < 0.001f)
            awayDirection = Vector3.forward;

        awayDirection.Normalize();

        float bestScore = float.NegativeInfinity;
        bool foundCandidate = false;

        NavMeshPath candidatePath = new NavMeshPath();

        foreach (float angle in CandidateAngles)
        {
            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) *
                awayDirection;

            Vector3 candidate =
                context.Position +
                direction * EscapeDistance;

            if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                SampleRadius,
                NavMesh.AllAreas))
            {
                continue;
            }

            if (!NavMesh.CalculatePath(
                context.Position,
                hit.position,
                NavMesh.AllAreas,
                candidatePath))
            {
                continue;
            }

            if (candidatePath.status !=
                NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            float pathLength = GetPathLength(candidatePath);

            float distanceFromThreat = Vector3.Distance(
                hit.position,
                threatPosition);

            float score =
                distanceFromThreat -
                pathLength * PathLengthPenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestDestination = hit.position;
                foundCandidate = true;
            }
        }

        return foundCandidate;
    }

    private float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        Vector3[] corners = path.corners;

        for (int i = 1; i < corners.Length; i++)
        {
            length += Vector3.Distance(
                corners[i - 1],
                corners[i]);
        }

        return length;
    }
}
