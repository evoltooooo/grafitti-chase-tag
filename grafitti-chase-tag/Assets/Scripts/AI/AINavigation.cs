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
        if (!NavMesh.SamplePosition(
            startPosition,
            out NavMeshHit startHit,
            2f,
            NavMesh.AllAreas))
        {
            return false;
        }

        if (!NavMesh.SamplePosition(
            destination,
            out NavMeshHit destinationHit,
            2f,
            NavMesh.AllAreas))
        {
            return false;
        }

        NavMeshPath newPath = new NavMeshPath();
        

        bool pathFound =
            NavMesh.CalculatePath(
                startHit.position,
                destinationHit.position,
                NavMesh.AllAreas,
                newPath
            );

        if (!pathFound ||
            newPath.status != NavMeshPathStatus.PathComplete ||
            newPath.corners.Length < 2)
        {
            return false;
        }

        // New path is valid.
        // Now replace the current path.

        corners = newPath.corners;
        currentCornerIndex = 1;

        CurrentWaypoint =
            corners[currentCornerIndex];
        
        for (int i = 0; i < newPath.corners.Length - 1; i++)
        {
            Debug.DrawLine(
                newPath.corners[i],
                newPath.corners[i + 1],
                Color.cyan,
                1f
            );
        }

        HasPath = true;

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
        CurrentWaypoint = Vector3.zero;

        currentCornerIndex = 0;
        corners = new Vector3[0];
    }
}