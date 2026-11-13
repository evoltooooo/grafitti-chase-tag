using UnityEngine;

public class VaultDetector : MonoBehaviour
{
    [SerializeField] private ParkourEnvironmentDetector environmentDetector;
    [SerializeField] private LayerMask groundLayers;

    private void Awake()
    {
        if (environmentDetector == null)
        {
            environmentDetector =
                GetComponent<ParkourEnvironmentDetector>();
        }
    }

    public bool TryDetect(
        ParkourActionData actionData,
        LayerMask parkourLayers,
        out ParkourTarget target)
    {
        target = default;

        if (actionData == null)
            return false;

        Vector3 forward =
            environmentDetector.GetHorizontalForward();

        float maxDistance =
            actionData.maxTriggerDistance;

        float maxHeight =
            actionData.maxObstacleHeight;

        Vector3 origin =
            transform.position +
            Vector3.up * 0.5f;

        if (!environmentDetector.TryDetectObstacle(
                origin,
                actionData.detectionRadius,
                forward,
                maxDistance,
                parkourLayers,
                out RaycastHit obstacleHit))
        {
            return false;
        }

        if (!environmentDetector.TryDetectTopSurface(
                obstacleHit.point,
                maxHeight,
                maxHeight + 1f,
                parkourLayers,
                out RaycastHit topHit))
        {
            return false;
        }

        Debug.Log(
            $"VAULT DETECTION GEOMETRY | " +
            $"Character={transform.position:F3} | " +
            $"Forward={forward:F3} | " +
            $"ObstacleHit={obstacleHit.point:F3} | " +
            $"ObstacleNormal={obstacleHit.normal:F3} | " +
            $"TopHit={topHit.point:F3}"
        );

        float obstacleHeight =
            topHit.point.y -
            transform.position.y;

        if (!environmentDetector.IsHeightValid(
                obstacleHeight,
                actionData))
        {
            return false;
        }

        // Calculate the obstacle's far edge along the
        // character's forward direction.
        Bounds obstacleBounds =
            obstacleHit.collider.bounds;

        Vector3 boundsCenter =
            obstacleBounds.center;

        Vector3 boundsExtents =
            obstacleBounds.extents;

        // Project the collider's bounds onto the
        // horizontal forward direction.
        float projectedHalfDepth =
            Mathf.Abs(forward.x) * boundsExtents.x +
            Mathf.Abs(forward.z) * boundsExtents.z;

        float obstacleCenterDistance =
            Vector3.Dot(
                boundsCenter - transform.position,
                forward
            );

        float farEdgeDistance =
            obstacleCenterDistance + projectedHalfDepth;

        // Add the configurable landing distance beyond
        // the obstacle's far edge.
        Vector3 landingOrigin =
            transform.position +
            forward * (farEdgeDistance + actionData.landingDistance);

        landingOrigin.y = topHit.point.y;

        if (!environmentDetector.TryDetectLanding(
                landingOrigin,
                1.5f,
                3f,
                groundLayers,
                out RaycastHit landingHit))
        {
            return false;
        }

        Vector3 takeoffPosition =
            transform.position;

        Vector3 landingPosition =
            landingHit.point;

        Vector3 interactionPosition =
            topHit.point;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                forward,
                Vector3.up
            );

        target =
            new ParkourTarget(
                ParkourActionType.Vault,
                takeoffPosition,
                landingPosition,
                interactionPosition,
                Vector3.zero,
                obstacleHit.normal,
                targetRotation,
                obstacleHeight,
                obstacleHit.distance
            );

        return true;
    }
}