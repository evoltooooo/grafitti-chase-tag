
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

        int sampleFailures = 0;
        int pathCalculationFailures = 0;
        int incompletePaths = 0;
        int completePaths = 0;

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
                sampleFailures++;

                Debug.Log(
                    $"EVADER CANDIDATE SAMPLE FAILED | " +
                    $"Angle={angle} | Candidate={candidate}");

                continue;
            }

            Debug.Log(
                $"EVADER PATH START CHECK | " +
                $"Start={context.Position} | " +
                $"StartOnNavMesh={NavMesh.SamplePosition(context.Position, out NavMeshHit startHit, 0.5f, NavMesh.AllAreas)} | " +
                $"SampledStart={startHit.position} | " +
                $"Destination={hit.position}"
            );

            bool pathCalculated = NavMesh.CalculatePath(
                context.Position,
                hit.position,
                NavMesh.AllAreas,
                candidatePath);
            
            Debug.Log(
                $"EVADER PATH COMPARISON | " +
                $"RawStart={context.Position} | " +
                $"RawDestination={hit.position} | " +
                $"RawStatus={candidatePath.status} | " +
                $"RawCorners={candidatePath.corners.Length}"
            );

            AINavigation testNavigation = new AINavigation();

            bool sharedPathFound = testNavigation.TryBuildPath(
                context.Position,
                hit.position
            );

            if (!pathCalculated)
            {
                pathCalculationFailures++;

                Debug.Log(
                    $"EVADER CANDIDATE PATH CALCULATION FAILED | " +
                    $"Angle={angle} | Destination={hit.position}");

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
                    Debug.Log(
                        $"EVADER PARTIAL CANDIDATE REJECTED | " +
                        $"Endpoint={reachableDestination} | " +
                        $"CurrentThreatDistance={currentDistanceFromThreat:F2} | " +
                        $"EndpointThreatDistance={reachableDistanceFromThreat:F2}");

                    continue;
                }

                float partialPathLength = GetPathLength(candidatePath);

                float partialDistanceFromThreat = Vector3.Distance(
                    reachableDestination,
                    threatPosition);

                float partialScore =
                    partialDistanceFromThreat -
                    partialPathLength * PathLengthPenalty;

                Debug.Log(
                    $"EVADER PARTIAL CANDIDATE | " +
                    $"Angle={angle} | " +
                    $"ReachableDestination={reachableDestination} | " +
                    $"Score={partialScore:F2}");

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

            float score =
                distanceFromThreat -
                pathLength * PathLengthPenalty;

            Debug.Log(
                $"EVADER CANDIDATE VALID | " +
                $"Angle={angle} | " +
                $"Destination={hit.position} | " +
                $"PathLength={pathLength:F2} | " +
                $"Score={score:F2}");

            if (score > bestScore)
            {
                bestScore = score;
                bestDestination = hit.position;
                foundCandidate = true;
            }
        }

        Debug.Log(
            $"EVADER CANDIDATE SUMMARY | " +
            $"SampleFailures={sampleFailures} | " +
            $"PathCalculationFailures={pathCalculationFailures} | " +
            $"IncompletePaths={incompletePaths} | " +
            $"CompletePaths={completePaths} | " +
            $"FoundCandidate={foundCandidate}");

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
