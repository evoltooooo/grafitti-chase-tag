using UnityEngine;

public class PoleSpinDetector : MonoBehaviour
{
    [SerializeField] private ParkourEnvironmentDetector environmentDetector;
    [SerializeField] private LayerMask poleLayers;

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
        out ParkourTarget target)
    {
        target = default;

        if (actionData == null)
            return false;

        Vector3 forward =
            environmentDetector.GetHorizontalForward();

        float maxDistance =
            actionData.maxTriggerDistance;

        Vector3 origin =
            transform.position +
            Vector3.up * 0.5f;

        if (!environmentDetector.TryDetectObstacle(
                origin,
                actionData.detectionRadius,
                forward,
                maxDistance,
                poleLayers,
                out RaycastHit poleHit))
        {
            return false;
        }

        Vector3 poleUp =
            poleHit.collider.transform.up;

        float verticalDot =
            GetVerticalAlignment(poleUp);

        if (verticalDot < 0.8f)
            return false;

        Vector3 poleCenter =
            poleHit.collider.bounds.center;

        Vector3 interactionPosition =
            poleHit.point;

        Vector3 playerToPole =
            environmentDetector.GetHorizontalDirection(
                poleCenter - transform.position);

        if (playerToPole == Vector3.zero)
            return false;

        Vector3 surfaceNormal =
            -playerToPole;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                -surfaceNormal,
                Vector3.up);

        target =
            new ParkourTarget(
                ParkourActionType.PoleSpin,
                transform.position,
                transform.position,
                interactionPosition,
                poleCenter,
                surfaceNormal,
                targetRotation,
                0f,
                poleHit.distance
            );

        return true;
    }

    private float GetVerticalAlignment(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return 0f;

        return Mathf.Abs(
            Vector3.Dot(
                direction.normalized,
                Vector3.up
            )
        );
    }
}