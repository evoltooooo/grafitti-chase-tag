using UnityEngine;

public class ClimbDetector : MonoBehaviour
{
    [SerializeField] private ParkourEnvironmentDetector environmentDetector;
    [SerializeField] private LayerMask climbableLayers;

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

        // -------------------------------------------------
        // WALL
        // -------------------------------------------------

        if (!environmentDetector.TryDetectWall(
                actionData,
                climbableLayers,
                out RaycastHit wallHit))
        {
            Debug.Log(
                $"CLIMB WALL NOT DETECTED | " +
                $"Position={transform.position} | " +
                $"Forward={environmentDetector.GetHorizontalForward()} | " +
                $"MaxDistance={actionData.maxTriggerDistance} | " +
                $"CastBack={actionData.detectionCastBackOffset} | " +
                $"ClimbableMask={climbableLayers.value}"
            );

            return false;
        }

        float wallUpDot =
            environmentDetector.GetWallUpDot(
                wallHit.normal);

        if (wallUpDot > 0.5f)
        {
            return false;
        }

        // -------------------------------------------------
        // TOP
        // -------------------------------------------------

        float maxHeight =
            actionData.maxObstacleHeight;

        if (!environmentDetector.TryDetectTopSurface(
                wallHit.point + forward * 0.05f,
                maxHeight,
                maxHeight + 0.5f,
                climbableLayers,
                out RaycastHit topHit))
        {
            Debug.Log("CLIMB DETECTION FAILED | Top surface not found");
            return false;
        }

        // -------------------------------------------------
        // HEIGHT
        // -------------------------------------------------

        float obstacleHeight =
            environmentDetector.GetColliderHeight(
                wallHit.collider);

        if (!environmentDetector.IsHeightValid(
            obstacleHeight,
            actionData))
        {
            Debug.Log(
                $"CLIMB DETECTION FAILED | " +
                $"Height={obstacleHeight:F2} | " +
                $"Min={actionData.minObstacleHeight:F2} | " +
                $"Max={actionData.maxObstacleHeight:F2}"
            );
            return false;
        }

        // -------------------------------------------------
        // LANDING
        // -------------------------------------------------

        Vector3 landingBase =
            topHit.point + Vector3.up * 0.25f;

        RaycastHit landingHit = default;
        bool foundLanding = false;

        float maxLandingSearchDistance =
            actionData.landingForwardOffset;

        float landingSearchStep = 0.25f;

        for (
            float offset = landingSearchStep;
            offset <= maxLandingSearchDistance;
            offset += landingSearchStep)
        {
            Vector3 landingSearchOrigin =
                landingBase + forward * offset;

            if (environmentDetector.TryDetectLanding(
                landingSearchOrigin,
                0f,
                5f,
                climbableLayers,
                out landingHit))
            {
                foundLanding = true;
                break;
            }
        }

        if (!foundLanding)
            return false;

        // -------------------------------------------------
        // TARGET
        // -------------------------------------------------

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