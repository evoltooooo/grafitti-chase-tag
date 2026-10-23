using UnityEngine;
using UnityEngine.AI;

public class AINavigation
{
    private readonly NavMeshPath path =
        new NavMeshPath();

    private Vector3[] corners =
        new Vector3[0];

    private int currentCornerIndex;

    public bool HasPath { get; private set; }
    public bool IsPartialPath { get; private set; }
    public Vector3 PartialPathEndpoint { get; private set; }
    public bool ReachedPartialPathEnd { get; private set; }

    public Vector3 CurrentWaypoint { get; private set; }

    public int CurrentCornerIndex => currentCornerIndex;

    public bool IsFinalWaypoint =>
        HasPath &&
        corners != null &&
        currentCornerIndex == corners.Length - 1;

    public Vector3 NextWaypoint
    {
        get
        {
            if (corners == null ||
                currentCornerIndex + 1 >= corners.Length)
            {
                return CurrentWaypoint;
            }

            return corners[currentCornerIndex + 1];
        }
    }
    

    public bool TryBuildPath(
        Vector3 startPosition,
        Vector3 destination)
    {
        if (!NavMesh.SamplePosition(
                startPosition,
                out NavMeshHit startHit,
                2f,
                NavMesh.AllAreas))
        {
            ResetPathState();

            Debug.Log(
                $"AI NAV DEBUG | Start Sample FAILED | " +
                $"Position={startPosition}"
            );

            return false;
        }
        if (!NavMesh.SamplePosition(
            destination,
            out NavMeshHit destinationHit,
            2f,
            NavMesh.AllAreas))
        {
            ResetPathState();

            Debug.Log(
                $"AI NAV DEBUG | Destination Sample FAILED | " +
                $"Destination={destination}"
            );

            return false;
        }

        NavMeshPath newPath =
            new NavMeshPath();

        bool pathFound =
            NavMesh.CalculatePath(
                startHit.position,
                destinationHit.position,
                NavMesh.AllAreas,
                newPath
            );

        Debug.Log(
            $"AI NAV DEBUG | " +
            $"StartHit={startHit.position} | " +
            $"DestinationHit={destinationHit.position} | " +
            $"PathFound={pathFound} | " +
            $"Status={newPath.status} | " +
            $"Corners={newPath.corners.Length}"
        );

        if (!pathFound ||
            newPath.corners.Length < 2)
        {
            ResetPathState();

            Debug.Log(
                $"AI NAV DEBUG | Path Build FAILED | " +
                $"PathFound={pathFound} | " +
                $"Status={newPath.status} | " +
                $"Corners={newPath.corners.Length}");

            return false;
        }

        IsPartialPath =
            newPath.status == NavMeshPathStatus.PathPartial;
        
        PartialPathEndpoint = IsPartialPath
            ? newPath.corners[newPath.corners.Length - 1]
            : Vector3.zero;

        corners = newPath.corners;
        currentCornerIndex = 1;

        CurrentWaypoint =
            corners[currentCornerIndex];

        HasPath = true;

        Debug.Log(
            $"AI NAV PATH ACCEPTED | " +
            $"Status={newPath.status} | " +
            $"Waypoint={CurrentWaypoint}"
        );

        return true;
    }

    public bool UpdateWaypoint(
        Vector3 currentPosition,
        float waypointReachedDistance = 0.75f)
    {
        if (!HasPath ||
            corners == null ||
            corners.Length == 0)
        {
            return false;
        }

        Debug.Log(
            $"AI NAV WAYPOINT DEBUG | " +
            $"Position={currentPosition} | " +
            $"CornerIndex={currentCornerIndex} | " +
            $"CornerCount={(corners == null ? 0 : corners.Length)} | " +
            $"HasPath={HasPath} | " +
            $"Waypoint={(currentCornerIndex < corners.Length ? corners[currentCornerIndex].ToString() : "NONE")}");

        while (
            currentCornerIndex < corners.Length &&
            Vector3.Distance(
                currentPosition,
                corners[currentCornerIndex]
            ) <= waypointReachedDistance
        )
        {
            currentCornerIndex++;
        }

        if (currentCornerIndex >= corners.Length)
        {
            ReachedPartialPathEnd = IsPartialPath;

            Debug.LogWarning(
                $"AI NAV FINAL WAYPOINT REACHED | " +
                $"Position={currentPosition} | " +
                $"FinalCorner={corners[corners.Length - 1]} | " +
                $"Distance={Vector3.Distance(currentPosition, corners[corners.Length - 1]):F2} | " +
                $"Partial={IsPartialPath} | " +
                $"ReachedDistance={waypointReachedDistance}");

            Debug.Log(
                $"AI NAV PATH ENDED | " +
                $"Partial={IsPartialPath} | " +
                $"Corners={corners.Length} | " +
                $"LastCorner={corners[corners.Length - 1]} | " +
                $"Position={currentPosition} | " +
                $"DistanceToLastCorner=" +
                $"{Vector3.Distance(currentPosition, corners[corners.Length - 1]):F2}"
            );

            float distanceToFinalCorner = Vector3.Distance(
                currentPosition,
                corners[corners.Length - 1]);

            Debug.Log(
                $"AI NAV COMPLETE PATH ENDED | " +
                $"Position={currentPosition} | " +
                $"FinalCorner={corners[corners.Length - 1]} | " +
                $"DistanceToFinalCorner={distanceToFinalCorner:F2} | " +
                $"ReachedDistance={waypointReachedDistance} | " +
                $"CornerIndex={currentCornerIndex} | " +
                $"CornerCount={corners.Length} | " +
                $"Partial={IsPartialPath}");

            // The current path has no more waypoints.
            // Keep the partial-path result available to the caller.
            HasPath = false;
            CurrentWaypoint = Vector3.zero;

            return false;
        }

        CurrentWaypoint =
            corners[currentCornerIndex];

        return true;
    }

    public void DrawDebugPath(
        Vector3 currentPosition,
        Color color)
    {
        if (!HasPath ||
            corners == null ||
            corners.Length < 2 ||
            currentCornerIndex >= corners.Length)
        {
            return;
        }

        // AI position to its next waypoint.
        Debug.DrawLine(
            currentPosition,
            corners[currentCornerIndex],
            color
        );

        // Remaining route through the NavMesh corners.
        for (int i = currentCornerIndex;
            i < corners.Length - 1;
            i++)
        {
            Debug.DrawLine(
                corners[i],
                corners[i + 1],
                color
            );
        }
    }

    public void ConsumeReachedPartialPathEnd()
    {
        ReachedPartialPathEnd = false;
    }

    private void ResetPathState()
    {
        HasPath = false;
        IsPartialPath = false;
        ReachedPartialPathEnd = false;
        PartialPathEndpoint = Vector3.zero;
        CurrentWaypoint = Vector3.zero;

        currentCornerIndex = 0;
        corners = new Vector3[0];
    }

    public void ClearPath()
    {
        ResetPathState();
    }
}