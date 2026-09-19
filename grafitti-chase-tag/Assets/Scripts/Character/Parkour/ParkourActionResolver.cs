using UnityEngine;

public class ParkourActionResolver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourDetector parkourDetector;

    private void Awake()
    {
        if (parkourDetector == null)
        {
            parkourDetector =
                GetComponent<ParkourDetector>();
        }
    }

    public bool TryResolve(
        ParkourActionData actionData,
        out ParkourTarget target)
    {
        target = default;

        Debug.Log(
            $"=== PARKOUR RESOLVE STARTED: " +
            $"{actionData?.parkourActionType} ==="
        );

        // -------------------------------------------------
        // STEP 1 — ACTION DATA
        // -------------------------------------------------

        if (actionData == null)
        {
            Debug.Log(
                "PARKOUR RESOLVE FAILED | " +
                "STEP 1: ActionData is null."
            );

            return false;
        }

        Debug.Log(
            $"PARKOUR STEP 1 SUCCESS | " +
            $"Action: {actionData.parkourActionType}"
        );

        // -------------------------------------------------
        // STEP 2 — DETECTOR
        // -------------------------------------------------

        if (parkourDetector == null)
        {
            Debug.LogError(
                "PARKOUR RESOLVE FAILED | " +
                "STEP 2: ParkourDetector is NULL."
            );

            return false;
        }

        Debug.Log(
            "PARKOUR STEP 2 SUCCESS | " +
            "Calling ParkourDetector."
        );

        // -------------------------------------------------
        // STEP 3 — DETECT TARGET
        // -------------------------------------------------

        if (!parkourDetector.TryDetectAction(
            actionData,
            out target))
        {
            Debug.Log(
                "PARKOUR RESOLVE FAILED | " +
                "STEP 3: Detector found no valid target."
            );

            return false;
        }

        Debug.Log(
            "PARKOUR STEP 3 SUCCESS | " +
            "Detector found target."
        );

        // -------------------------------------------------
        // STEP 4 — VALIDATE TARGET
        // -------------------------------------------------

        if (!target.IsValid)
        {
            Debug.Log(
                "PARKOUR RESOLVE FAILED | " +
                "STEP 4: Target is invalid."
            );

            return false;
        }

        Debug.Log(
            "=== PARKOUR RESOLVE SUCCESS ==="
        );

        return true;
    }
}