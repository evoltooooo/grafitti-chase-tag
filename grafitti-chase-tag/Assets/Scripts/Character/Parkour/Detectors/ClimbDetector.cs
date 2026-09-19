using UnityEngine;

public class ClimbDetector : MonoBehaviour
{
    [SerializeField] private ParkourEnvironmentDetector environmentDetector;

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

        if (!environmentDetector.TryDetectWall(
                actionData,
                parkourLayers,
                out RaycastHit wallHit))
        {
            return false;
        }

        float wallUpDot =
            environmentDetector.GetWallUpDot(
                wallHit.normal);

        if (wallUpDot > 0.5f)
            return false;

        float maxHeight =
            actionData.maxObstacleHeight;

        if (!environmentDetector.TryDetectTopSurface(
                wallHit.point + forward * 0.05f,
                maxHeight,
                maxHeight + 0.5f,
                parkourLayers,
                out RaycastHit topHit))
        {
            return false;
        }

        float obstacleHeight =
            environmentDetector.GetColliderHeight(
                wallHit.collider);

        if (!environmentDetector.IsHeightValid(
                obstacleHeight,
                actionData))
        {
            return false;
        }

        Vector3 landingBase =
            topHit.point + Vector3.up * 0.25f;

        RaycastHit landingHit = default;
        bool foundLanding = false;

        float maxLandingSearchDistance =
            actionData.landingForwardOffset;

        float landingSearchStep = 0.25f;

        for (float offset = landingSearchStep;
            offset <= maxLandingSearchDistance;
            offset += landingSearchStep)
        {
            Vector3 landingSearchOrigin =
                landingBase + forward * offset;

            if (environmentDetector.TryDetectLanding(
                    landingSearchOrigin,
                    0f,
                    5f,
                    parkourLayers,
                    out landingHit))
            {
                foundLanding = true;
                break;
            }
        }

        if (!foundLanding)
            return false;

        Vector3 interactionPosition =
            topHit.point;

        Vector3 landingPosition =
            landingHit.point;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                -wallHit.normal,
                Vector3.up);

        target = new ParkourTarget(
            ParkourActionType.Climb,
            transform.position,
            landingPosition,
            interactionPosition,
            Vector3.zero,
            wallHit.normal,
            targetRotation,
            obstacleHeight,
            wallHit.distance
        );

        return true;
    }
}