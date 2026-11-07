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

        // -------------------------------------------------
        // STEP 1 — ACTION DATA
        // -------------------------------------------------

        if (actionData == null)
        {
            return false;
        }

        // -------------------------------------------------
        // STEP 2 — DETECTOR
        // -------------------------------------------------

        if (parkourDetector == null)
        {
            return false;
        }

        // -------------------------------------------------
        // STEP 3 — DETECT TARGET
        // -------------------------------------------------

        if (!parkourDetector.TryDetectAction(
            actionData,
            out target))
        {
            return false;
        }

        // -------------------------------------------------
        // STEP 4 — VALIDATE TARGET
        // -------------------------------------------------

        if (!target.IsValid)
        {
            return false;
        }

        return true;
    }
}