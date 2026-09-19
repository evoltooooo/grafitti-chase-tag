using UnityEngine;

public class ParkourDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask parkourLayers;
    [SerializeField] private VaultDetector vaultDetector;
    [SerializeField] private ClimbDetector climbDetector;
    [SerializeField] private TicTacDetector ticTacDetector;
    [SerializeField] private PoleSpinDetector poleSpinDetector;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;
    [SerializeField] private ParkourActionData actionDataForDebug;

    private ParkourTarget debugTarget;
    private bool hasDebugTarget;

    private void Awake()
    {
        if (vaultDetector == null)
        {
            vaultDetector =
                GetComponent<VaultDetector>();
        }

        if (climbDetector == null)
        {
            climbDetector =
                GetComponent<ClimbDetector>();
        }

        if (ticTacDetector == null)
        {
            ticTacDetector =
                GetComponent<TicTacDetector>();
        }

        if (poleSpinDetector == null)
        {
            poleSpinDetector =
                GetComponent<PoleSpinDetector>();
        }
    }

    public bool TryDetectAction(
        ParkourActionData actionData,
        out ParkourTarget target)
    {
        target = default;

        if (actionData == null)
            return false;

        switch (actionData.parkourActionType)
        {
            case ParkourActionType.Vault:
                return vaultDetector.TryDetect(
                    actionData,
                    parkourLayers,
                    out target);

            case ParkourActionType.Climb:
                return climbDetector.TryDetect(
                    actionData,
                    parkourLayers,
                    out target);

            case ParkourActionType.TicTac:
                return ticTacDetector.TryDetect(
                    actionData,
                    parkourLayers,
                    out target);

            case ParkourActionType.PoleSpin:
                return poleSpinDetector.TryDetect(
                    actionData,
                    out target);

            default:
                return false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        // =====================================================
        // COMMON
        // =====================================================

        Vector3 origin =
            transform.position +
            Vector3.up * 0.5f;

        // =====================================================
        // CLIMB DETECTION DEBUG
        // =====================================================

        if (actionDataForDebug != null &&
            actionDataForDebug.parkourActionType == ParkourActionType.Climb)
        {
            float maxDistance =
                actionDataForDebug.maxTriggerDistance;

            float radius = 0.25f;

            float maxHeight =
                actionDataForDebug.maxObstacleHeight;

            float castBackOffset =
                actionDataForDebug.detectionCastBackOffset;

            // Actual origin used by Climb detection
            Vector3 climbOrigin =
                origin -
                transform.forward *
                castBackOffset;

            // -------------------------------------------------
            // 1. WALL SPHERE CAST
            // -------------------------------------------------

            Gizmos.color = Color.yellow;

            // Start sphere
            Gizmos.DrawWireSphere(
                climbOrigin,
                radius
            );

            // End sphere
            Gizmos.DrawWireSphere(
                climbOrigin +
                transform.forward * maxDistance,
                radius
            );

            // Capsule/sweep guide lines
            Gizmos.DrawLine(
                climbOrigin + Vector3.up * radius,
                climbOrigin +
                transform.forward * maxDistance +
                Vector3.up * radius
            );

            Gizmos.DrawLine(
                climbOrigin - Vector3.up * radius,
                climbOrigin +
                transform.forward * maxDistance -
                Vector3.up * radius
            );

            Gizmos.DrawLine(
                climbOrigin - transform.right * radius,
                climbOrigin +
                transform.forward * maxDistance -
                transform.right * radius
            );

            Gizmos.DrawLine(
                climbOrigin + transform.right * radius,
                climbOrigin +
                transform.forward * maxDistance +
                transform.right * radius
            );

            // Direction indicator
            Gizmos.color = Color.red;

            Gizmos.DrawRay(
                climbOrigin,
                transform.forward * maxDistance
            );

            // -------------------------------------------------
            // 2. TOP / HEIGHT DEBUG
            // -------------------------------------------------

            // Visualize maximum obstacle height
            Gizmos.color = Color.magenta;

            Gizmos.DrawLine(
                climbOrigin,
                climbOrigin + Vector3.up * maxHeight
            );

            // -------------------------------------------------
            // 3. DEBUG TARGET
            // -------------------------------------------------

            if (hasDebugTarget)
            {
                // -------------------------------------------------
                // Interaction / top target
                // -------------------------------------------------

                Gizmos.color = Color.red;

                Gizmos.DrawSphere(
                    debugTarget.InteractionPosition,
                    0.12f
                );

                // -------------------------------------------------
                // Surface normal
                // -------------------------------------------------

                Gizmos.color = Color.blue;

                Gizmos.DrawRay(
                    debugTarget.InteractionPosition,
                    debugTarget.SurfaceNormal * 0.75f
                );

                // -------------------------------------------------
                // Takeoff -> interaction
                // -------------------------------------------------

                Gizmos.color = Color.red;

                Gizmos.DrawLine(
                    debugTarget.TakeoffPosition,
                    debugTarget.InteractionPosition
                );

                // -------------------------------------------------
                // Landing
                // -------------------------------------------------

                Gizmos.color = Color.green;

                Gizmos.DrawSphere(
                    debugTarget.LandingPosition,
                    0.12f
                );

                Gizmos.DrawLine(
                    debugTarget.InteractionPosition,
                    debugTarget.LandingPosition
                );

                // -------------------------------------------------
                // Target facing direction
                // -------------------------------------------------

                Gizmos.color = Color.cyan;

                Gizmos.DrawRay(
                    debugTarget.InteractionPosition,
                    debugTarget.TargetRotation *
                    Vector3.forward *
                    0.75f
                );
            }

            return;
        }

        // =====================================================
        // NORMAL / VAULT DEBUG
        // =====================================================

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            origin,
            origin +
            transform.forward *
            1.5f
        );

        Gizmos.DrawWireSphere(
            origin +
            transform.forward *
            1.5f,
            0.3f
        );

        // -----------------------------------------------------
        // Landing detection
        // -----------------------------------------------------

        Vector3 landingPosition =
            transform.position +
            transform.forward *
            1.5f;

        Gizmos.color = Color.green;

        Gizmos.DrawLine(
            landingPosition +
            Vector3.up * 1.5f,
            landingPosition -
            Vector3.up * 1.5f
        );

        // -----------------------------------------------------
        // Existing debug target
        // -----------------------------------------------------

        if (hasDebugTarget)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawSphere(
                debugTarget.InteractionPosition,
                0.12f
            );

            Gizmos.color = Color.green;

            Gizmos.DrawSphere(
                debugTarget.LandingPosition,
                0.12f
            );

            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                debugTarget.TakeoffPosition,
                debugTarget.InteractionPosition
            );

            Gizmos.color = Color.green;

            Gizmos.DrawLine(
                debugTarget.InteractionPosition,
                debugTarget.LandingPosition
            );

            Gizmos.color = Color.blue;

            Gizmos.DrawRay(
                debugTarget.InteractionPosition,
                debugTarget.SurfaceNormal * 0.5f
            );
        }
    }
}