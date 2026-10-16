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
            Debug.Log(
                $"CLIMB REJECTED | Invalid wall normal | " +
                $"Normal={wallHit.normal} | " +
                $"WallUpDot={wallUpDot:F2}"
            );

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
        {
            Debug.Log(
                $"CLIMB REJECTED | No landing surface found | " +
                $"Top={topHit.point} | " +
                $"Forward={forward} | " +
                $"SearchDistance={maxLandingSearchDistance:F2} | " +
                $"ClimbableMask={climbableLayers.value}"
            );

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
            Debug.Log(
                $"CLIMB REJECTED | Landing support invalid | " +
                $"Landing={landingPosition} | " +
                $"ColliderRadius=" +
                $"{(characterMotor != null ? characterMotor.ColliderRadius : -1f):F2}"
            );

            return false;
        }

        Debug.Log(
            $"CLIMB TARGET DEBUG | " +
            $"Start={transform.position} | " +
            $"Top={topHit.point} | " +
            $"Landing={landingPosition} | " +
            $"Forward={forward} | " +
            $"LandingOffset={actionData.landingForwardOffset}"
        );

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

    private bool HasLandingSupport(Vector3 landingPoint)
    {
        if (characterMotor == null)
        {
            Debug.LogWarning(
                "CLIMB LANDING CHECK FAILED | CharacterMotor missing",
                this
            );

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
                Debug.Log(
                    $"CLIMB LANDING REJECTED | " +
                    $"Unsupported sample={i} | " +
                    $"Point={rayOrigin}",
                    this
                );

                return false;
            }

            if (hit.normal.y < 0.7f)
            {
                Debug.Log(
                    $"CLIMB LANDING REJECTED | " +
                    $"Steep surface | Normal={hit.normal}",
                    this
                );

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