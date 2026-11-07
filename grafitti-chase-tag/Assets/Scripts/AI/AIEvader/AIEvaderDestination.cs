using UnityEngine;
using UnityEngine.AI;

public class AIEvaderDestination
{
    private const float EscapeDistance = 4f;
    private const float SampleRadius = 2f;
    private const float PathLengthPenalty = 0.2f;

    private static readonly float[] CandidateAngles =
    {
        0f, -15f, 15f, -30f, 30f, -45f, 45f,
        -90f, 90f, -135f, 135f, 180f
    };

    public bool TryCalculate(
        AIContext context,
        Vector3 threatPosition,
        out Vector3 bestDestination)
    {
        bestDestination = context.Position;

        bool startOnNavMesh = NavMesh.SamplePosition(
            context.Position,
            out NavMeshHit startHit,
            1.0f,
            NavMesh.AllAreas);
        
        if (startOnNavMesh)
        {
            Vector3 difference = startHit.position - context.Position;
        }

        LogVerticalNavMeshSamples(context.Position);

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

        int sampleFailures = 0;
        int pathCalculationFailures = 0;
        int incompletePaths = 0;
        int completePaths = 0;

        foreach (float angle in CandidateAngles)
        {
            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) *
                awayDirection;

            Vector3 candidate = context.Position + direction * EscapeDistance;

            Vector3 flatStart = startHit.position;
            Vector3 flatCandidate = candidate;

            flatStart.y = 0f;
            flatCandidate.y = 0f;

            float candidateDistance = Vector3.Distance(flatStart, flatCandidate);

            if (candidateDistance < 1.25f)
            {
                continue;
            }

            if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                SampleRadius,
                NavMesh.AllAreas))
            {
                sampleFailures++;
 
            float horizontalDistance = Vector2.Distance(
                new Vector2(candidate.x, candidate.z),
                new Vector2(context.Position.x, context.Position.z));

                // ADD THIS LINE HERE
                LogVerticalNavMeshSamples(candidate);

                continue;
            }

            Vector3 flatSampledDestination = hit.position;
            flatSampledDestination.y = 0f;

            float sampledDestinationDistance = Vector3.Distance(
                flatStart,
                flatSampledDestination);

            if (sampledDestinationDistance < 1.25f)
            {
                continue;
            }

            float verticalDifference =
                hit.position.y - context.Position.y;

            string floorType = verticalDifference < -1.0f
                ? "Lower"
                : verticalDifference > 1.0f
                    ? "Higher"
                    : "SameFloor";

            Vector3 sameHeightCandidate = new Vector3(
                candidate.x,
                context.Position.y,
                candidate.z);

            bool sameHeightFound = NavMesh.SamplePosition(
                sameHeightCandidate,
                out NavMeshHit sameHeightHit,
                0.5f,
                NavMesh.AllAreas);

            bool pathCalculated = NavMesh.CalculatePath(
                startHit.position,
                hit.position,
                NavMesh.AllAreas,
                candidatePath);

            if (!pathCalculated)
            {
                pathCalculationFailures++;

                continue;
            }

            if (candidatePath.status == NavMeshPathStatus.PathPartial)
            {
                incompletePaths++;

                Vector3[] partialCorners = candidatePath.corners;

                if (partialCorners.Length < 2)
                    continue;

                Vector3 reachableDestination =
                    partialCorners[partialCorners.Length - 1];

                float currentDistanceFromThreat = Vector3.Distance(
                    context.Position,
                    threatPosition);

                float reachableDistanceFromThreat = Vector3.Distance(
                    reachableDestination,
                    threatPosition);

                // Don't choose a partial-path endpoint that brings the
                // Evader closer to the threat.
                if (reachableDistanceFromThreat <= currentDistanceFromThreat + 0.25f)
                {
                    continue;
                }

                float partialPathLength = GetPathLength(candidatePath);

                float partialDistanceFromThreat = Vector3.Distance(
                    reachableDestination,
                    threatPosition);

                Vector3 partialDirection =
                    reachableDestination - context.Position;

                partialDirection.y = 0f;

                float partialAlignment = 0f;

                if (partialDirection.sqrMagnitude > 0.001f)
                {
                    partialAlignment = Vector3.Dot(
                        awayDirection,
                        partialDirection.normalized);
                }

                float partialScore =
                    partialDistanceFromThreat -
                    partialPathLength * PathLengthPenalty +
                    partialAlignment * 1.5f;

                if (partialScore > bestScore)
                {
                    bestScore = partialScore;
                    bestDestination = reachableDestination;
                    foundCandidate = true;
                }

                continue;
            }

            if (candidatePath.status != NavMeshPathStatus.PathComplete)
                continue;

            completePaths++;

            float pathLength = GetPathLength(candidatePath);

            float distanceFromThreat = Vector3.Distance(
                hit.position,
                threatPosition);

            Vector3 candidateDirection = hit.position - context.Position;
            candidateDirection.y = 0f;

            float directionalAlignment = 0f;

            if (candidateDirection.sqrMagnitude > 0.001f)
            {
                directionalAlignment = Vector3.Dot(
                    awayDirection,
                    candidateDirection.normalized);
            }

            Vector3 flatSampledStart = startHit.position;
            Vector3 flatValidDestination = hit.position;

            flatSampledStart.y = 0f;
            flatValidDestination.y = 0f;

            float validDestinationDistance = Vector3.Distance(
                flatSampledStart,
                flatValidDestination);

            if (validDestinationDistance < 1.25f)
            {
                continue;
            }

            float score =
                distanceFromThreat -
                pathLength * PathLengthPenalty +
                directionalAlignment * 1.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDestination = hit.position;
                foundCandidate = true;
            }
        }

        if (foundCandidate)
        {
            Vector3 flatStart = context.Position;
            Vector3 flatDestination = bestDestination;

            flatStart.y = 0f;
            flatDestination.y = 0f;

            float selectedDistance =
                Vector3.Distance(flatStart, flatDestination);

            if (selectedDistance < 1.25f)
            {
                foundCandidate = false;
                bestDestination = context.Position;
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

    private void LogVerticalNavMeshSamples(Vector3 candidate)
    {
        float[] verticalOffsets = { 0f, 2f, 5f, 10f };

        foreach (float offset in verticalOffsets)
        {
            Vector3 samplePosition =
                candidate + Vector3.down * offset;

            bool found = NavMesh.SamplePosition(
                samplePosition,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas);
        }
    }
}
