using UnityEngine;

public class ClimbDetector : MonoBehaviour
{
    [SerializeField] private ParkourEnvironmentDetector environmentDetector;
    [SerializeField] private LayerMask climbableLayers;
    [SerializeField] private CharacterMotor characterMotor;

    private void Awake()
    {
        if (environmentDetector == null)
        {
            environmentDetector =
                GetComponent<ParkourEnvironmentDetector>();
        }

        if (characterMotor == null)
        {
            characterMotor = GetComponent<CharacterMotor>();
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
        {
            return false;
        }

        // -------------------------------------------------
        // TARGET
        // -------------------------------------------------

        Vector3 interactionPosition =
            topHit.point;

        Vector3 landingPosition =
            landingHit.point;

        if (!HasLandingSupport(landingPosition))
        {
            return false;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                -wallHit.normal,
                Vector3.up);
        
        bool landingOnNavMesh = UnityEngine.AI.NavMesh.SamplePosition(
            landingPosition,
            out UnityEngine.AI.NavMeshHit navHit,
            0.1f,
            UnityEngine.AI.NavMesh.AllAreas);

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

    private bool HasLandingSupport(Vector3 landingPoint)
    {
        if (characterMotor == null)
        {
            return false;
        }

        float radius = characterMotor.ColliderRadius;

        if (radius <= 0f)
            return false;

        // Check eight points around the character's footprint.
        // Keep a small margin inside the capsule's outer radius.
        float sampleRadius = radius * 0.85f;
        const float rayStartHeight = 0.15f;
        const float rayDistance = 0.5f;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            ) * sampleRadius;

            Vector3 rayOrigin =
                landingPoint +
                offset +
                Vector3.up * rayStartHeight;

            if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                climbableLayers,
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.normal.y < 0.7f)
            {
                return false;
            }

            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                return false;
            }
        }

        return true;
    }
}