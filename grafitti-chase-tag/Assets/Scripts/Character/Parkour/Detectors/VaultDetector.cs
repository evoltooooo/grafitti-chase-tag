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

        float obstacleHeight =
            topHit.point.y -
            transform.position.y;

        if (!environmentDetector.IsHeightValid(
                obstacleHeight,
                actionData))
        {
            return false;
        }

        Vector3 landingOrigin =
            topHit.point +
            forward * actionData.landingDistance;

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