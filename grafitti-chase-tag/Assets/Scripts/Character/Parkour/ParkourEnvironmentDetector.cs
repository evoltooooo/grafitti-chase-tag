using UnityEngine;

public class ParkourEnvironmentDetector : MonoBehaviour
{
    [SerializeField] 
    [Min(0f)]
    private float wallDetectionRadius = 0.25f;

    public Vector3 GetHorizontalForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return forward.normalized;
    }

    public bool TryDetectLanding(
        Vector3 origin,
        float rayHeight,
        float rayDistance,
        LayerMask layers,
        out RaycastHit landingHit)
    {
        landingHit = default;

        Vector3 rayOrigin =
            origin +
            Vector3.up * rayHeight;

        return Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out landingHit,
            rayDistance,
            layers,
            QueryTriggerInteraction.Ignore
        );
    }

    public bool TryDetectTopSurface(
        Vector3 origin,
        float rayHeight,
        float rayDistance,
        LayerMask layers,
        out RaycastHit topHit)
    {
        topHit = default;

        Vector3 rayOrigin =
            origin +
            Vector3.up * rayHeight;

        return Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out topHit,
            rayDistance,
            layers,
            QueryTriggerInteraction.Ignore
        );
    }

    public float GetColliderHeight(Collider collider)
    {
        if (collider == null)
            return 0f;

        return collider.bounds.max.y -
            collider.bounds.min.y;
    }

    public bool IsHeightValid(
        float height,
        ParkourActionData actionData)
    {
        if (actionData == null)
            return false;

        return height >= actionData.minObstacleHeight &&
            height <= actionData.maxObstacleHeight;
    }

    public float GetWallUpDot(Vector3 wallNormal)
    {
        return Vector3.Dot(
            wallNormal,
            Vector3.up
        );
    }

    public Vector3 GetHorizontalDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return direction.normalized;
    }

    public bool TryDetectWall(
        ParkourActionData actionData,
        LayerMask parkourLayers,
        out RaycastHit wallHit)
    {
        wallHit = default;

        if (actionData == null)
            return false;

        Vector3 forward =
            GetHorizontalForward();

        float castBackOffset =
            actionData.detectionCastBackOffset;

        Vector3 origin =
            transform.position +
            Vector3.up * 0.5f -
            forward * castBackOffset;

        float detectionDistance =
            actionData.maxTriggerDistance +
            castBackOffset;

        return Physics.SphereCast(
            origin,
            wallDetectionRadius,
            forward,
            out wallHit,
            detectionDistance,
            parkourLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    public bool TryDetectObstacle(
        Vector3 origin,
        float radius,
        Vector3 direction,
        float distance,
        LayerMask layers,
        out RaycastHit obstacleHit)
    {
        obstacleHit = default;

        return Physics.SphereCast(
            origin,
            radius,
            direction,
            out obstacleHit,
            distance,
            layers,
            QueryTriggerInteraction.Ignore
        );
    }
}