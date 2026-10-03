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
    public bool ReachedPartialPathEnd { get; private set; }

    public Vector3 CurrentWaypoint { get; private set; }

    public int CurrentCornerIndex => currentCornerIndex;

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
        ReachedPartialPathEnd = false;

        if (!NavMesh.SamplePosition(
                startPosition,
                out NavMeshHit startHit,
                2f,
                NavMesh.AllAreas))
        {
            IsPartialPath = false;

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
            IsPartialPath = false;
            return false;
        }

        IsPartialPath =
            newPath.status == NavMeshPathStatus.PathPartial;

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
        ReachedPartialPathEnd = false;

        if (!HasPath ||
            corners == null ||
            corners.Length == 0)
        {
            return false;
        }

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

            HasPath = false;
            CurrentWaypoint = Vector3.zero;

            return false;
        }

        CurrentWaypoint =
            corners[currentCornerIndex];

        return true;
    }

    public void ClearPath()
    {
        HasPath = false;
        IsPartialPath = false;
        CurrentWaypoint = Vector3.zero;

        currentCornerIndex = 0;
        corners = new Vector3[0];
    }
}