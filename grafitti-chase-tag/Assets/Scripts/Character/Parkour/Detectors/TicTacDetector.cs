using UnityEngine;

public class TicTacDetector : MonoBehaviour
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

        if (wallUpDot > 0.5f ||
            wallUpDot < -0.5f)
        {
            return false;
        }

        Vector3 interactionPosition =
            wallHit.point +
            wallHit.normal * 0.05f;

        Vector3 launchDirection =
            environmentDetector.GetHorizontalDirection(
                wallHit.normal);

        if (launchDirection == Vector3.zero)
            return false;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                launchDirection,
                Vector3.up);

        target =
            new ParkourTarget(
                ParkourActionType.TicTac,
                transform.position,
                transform.position,
                interactionPosition,
                Vector3.zero,
                wallHit.normal,
                targetRotation,
                0f,
                wallHit.distance
            );

        return true;
    }
}