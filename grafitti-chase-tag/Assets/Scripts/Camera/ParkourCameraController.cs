
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class ParkourCameraController : MonoBehaviour
{
    [Header("Cinemachine References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineInputAxisController inputAxisController;

    [Header("Pole Spin Camera")]
    [SerializeField] private float overheadHeight = 4.5f;
    [SerializeField] private float overheadRadius = 0.25f;
    [SerializeField] private float transitionDuration = 0.35f;

    private Transform playerTransform;
    private CharacterActionRuntime actionRuntime;

    // Tic Tac / Wall Rebound
    private bool isActionCameraActive;
    private bool wasInputControllerEnabled;
    private float horizontalAngleOffset;

    // Pole Spin
    private bool isPoleSpinCameraActive;
    private bool isPoleSpinCameraReturning;

    private float poleSpinTransitionTimer;

    private float normalVerticalAxisValue;
    private float poleSpinStartVerticalValue;
    private float poleSpinLockedHorizontalValue;

    private float poleSpinReturnStartVerticalValue;

    private Cinemachine3OrbitRig.Settings normalOrbits;
    private Cinemachine3OrbitRig.Orbit poleSpinStartTop;
    private Cinemachine3OrbitRig.Orbit poleSpinReturnStartTop;

    private void Awake()
    {
        if (cinemachineCamera == null)
            cinemachineCamera =
                GetComponent<CinemachineCamera>();

        if (orbitalFollow == null)
            orbitalFollow =
                GetComponent<CinemachineOrbitalFollow>();

        if (inputAxisController == null)
            inputAxisController =
                GetComponent<CinemachineInputAxisController>();
    }

    private void LateUpdate()
    {
        ResolvePlayerReferences();

        if (actionRuntime == null ||
            playerTransform == null ||
            orbitalFollow == null ||
            inputAxisController == null)
        {
            return;
        }

        bool poleSpinHeld =
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentParkourActionType ==
                ParkourActionType.PoleSpin &&
            !actionRuntime.IsReleasing;

        // -------------------------------------------------
        // POLE SPIN HELD
        // -------------------------------------------------

        if (poleSpinHeld)
        {
            if (!isPoleSpinCameraActive)
                BeginPoleSpinCamera();

            UpdatePoleSpinCamera();
            return;
        }

        // -------------------------------------------------
        // POLE SPIN RELEASED OR COMPLETED
        // -------------------------------------------------

        if (isPoleSpinCameraActive)
            BeginPoleSpinCameraReturn();

        if (isPoleSpinCameraReturning)
        {
            UpdatePoleSpinCameraReturn();
            return;
        }

        // -------------------------------------------------
        // TIC TAC / WALL REBOUND
        // -------------------------------------------------

        bool shouldFollowCharacterFacing =
            actionRuntime.IsExecuting &&
            (
                actionRuntime.CurrentParkourActionType ==
                    ParkourActionType.TicTac ||

                actionRuntime.CurrentActionType ==
                    CharacterActionType.WallRebound
            );

        if (shouldFollowCharacterFacing)
        {
            if (!isActionCameraActive)
                BeginActionCamera();

            UpdateActionCamera();
        }
        else
        {
            EndActionCamera();
        }
    }

    private void ResolvePlayerReferences()
    {
        if (cinemachineCamera == null)
            return;

        Transform trackingTarget =
            cinemachineCamera.Target.TrackingTarget;

        if (trackingTarget == playerTransform)
            return;

        RestorePoleSpinCameraImmediately();
        EndActionCamera();

        playerTransform = trackingTarget;

        actionRuntime = playerTransform != null
            ? playerTransform.GetComponent<CharacterActionRuntime>()
            : null;
    }

    // =====================================================
    // TIC TAC / WALL REBOUND CAMERA
    // =====================================================

    private void BeginActionCamera()
    {
        isActionCameraActive = true;

        wasInputControllerEnabled =
            inputAxisController.enabled;

        inputAxisController.enabled = false;

        horizontalAngleOffset = Mathf.DeltaAngle(
            playerTransform.eulerAngles.y,
            orbitalFollow.HorizontalAxis.Value
        );
    }

    private void UpdateActionCamera()
    {
        float targetAngle =
            playerTransform.eulerAngles.y +
            horizontalAngleOffset;

        orbitalFollow.HorizontalAxis.Value =
            Mathf.Repeat(targetAngle + 180f, 360f) - 180f;
    }

    private void EndActionCamera()
    {
        if (!isActionCameraActive)
            return;

        isActionCameraActive = false;

        if (inputAxisController != null)
        {
            inputAxisController.enabled =
                wasInputControllerEnabled;
        }
    }

    // =====================================================
    // POLE SPIN CAMERA
    // =====================================================

    private void BeginPoleSpinCamera()
    {
        isPoleSpinCameraActive = true;
        isPoleSpinCameraReturning = false;

        poleSpinTransitionTimer = 0f;

        // Save the existing camera configuration.
        normalOrbits = orbitalFollow.Orbits;

        normalVerticalAxisValue =
            orbitalFollow.VerticalAxis.Value;

        poleSpinStartVerticalValue =
            orbitalFollow.VerticalAxis.Value;

        poleSpinLockedHorizontalValue =
            orbitalFollow.HorizontalAxis.Value;

        poleSpinStartTop =
            normalOrbits.Top;

        // Preserve the current input controller state.
        wasInputControllerEnabled =
            inputAxisController.enabled;

        // Disable manual orbit input during Pole Spin.
        inputAxisController.enabled = false;

        Debug.Log(
            "POLE SPIN CAMERA | Overhead transition started.",
            this
        );
    }

    private void UpdatePoleSpinCamera()
    {
        poleSpinTransitionTimer +=
            Time.deltaTime;

        float duration =
            Mathf.Max(transitionDuration, 0.01f);

        float progress = Mathf.Clamp01(
            poleSpinTransitionTimer / duration
        );

        float easedProgress =
            Mathf.SmoothStep(0f, 1f, progress);

        // Move the vertical axis to its existing maximum.
        // The normal axis range is never modified.
        float overheadAxisValue =
            orbitalFollow.VerticalAxis.Range.y;

        var verticalAxis =
            orbitalFollow.VerticalAxis;

        verticalAxis.Value = Mathf.Lerp(
            poleSpinStartVerticalValue,
            overheadAxisValue,
            easedProgress
        );

        orbitalFollow.VerticalAxis = verticalAxis;

        // Gradually reshape the top orbit into an overhead orbit.
        var orbits = orbitalFollow.Orbits;

        var top = poleSpinStartTop;

        top.Height = Mathf.Lerp(
            poleSpinStartTop.Height,
            overheadHeight,
            easedProgress
        );

        top.Radius = Mathf.Lerp(
            poleSpinStartTop.Radius,
            overheadRadius,
            easedProgress
        );

        orbits.Top = top;

        orbitalFollow.Orbits = orbits;

        // Keep the horizontal orbit locked.
        var horizontalAxis =
            orbitalFollow.HorizontalAxis;

        horizontalAxis.Value =
            poleSpinLockedHorizontalValue;

        orbitalFollow.HorizontalAxis =
            horizontalAxis;
    }

    private void BeginPoleSpinCameraReturn()
    {
        isPoleSpinCameraActive = false;
        isPoleSpinCameraReturning = true;

        poleSpinTransitionTimer = 0f;

        poleSpinReturnStartVerticalValue =
            orbitalFollow.VerticalAxis.Value;

        poleSpinReturnStartTop =
            orbitalFollow.Orbits.Top;

        Debug.Log(
            "POLE SPIN CAMERA | Returning to normal orbit.",
            this
        );
    }

    private void UpdatePoleSpinCameraReturn()
    {
        poleSpinTransitionTimer +=
            Time.deltaTime;

        float duration =
            Mathf.Max(transitionDuration, 0.01f);

        float progress = Mathf.Clamp01(
            poleSpinTransitionTimer / duration
        );

        float easedProgress =
            Mathf.SmoothStep(0f, 1f, progress);

        // Restore the original vertical orbit position.
        var verticalAxis =
            orbitalFollow.VerticalAxis;

        verticalAxis.Value = Mathf.Lerp(
            poleSpinReturnStartVerticalValue,
            normalVerticalAxisValue,
            easedProgress
        );

        orbitalFollow.VerticalAxis = verticalAxis;

        // Restore the original top orbit.
        var orbits = orbitalFollow.Orbits;

        var top = poleSpinReturnStartTop;

        top.Height = Mathf.Lerp(
            poleSpinReturnStartTop.Height,
            normalOrbits.Top.Height,
            easedProgress
        );

        top.Radius = Mathf.Lerp(
            poleSpinReturnStartTop.Radius,
            normalOrbits.Top.Radius,
            easedProgress
        );

        orbits.Top = top;

        orbitalFollow.Orbits = orbits;

        if (progress >= 1f)
        {
            FinishPoleSpinCameraReturn();
        }
    }

    private void FinishPoleSpinCameraReturn()
    {
        orbitalFollow.Orbits = normalOrbits;

        var verticalAxis =
            orbitalFollow.VerticalAxis;

        verticalAxis.Value =
            normalVerticalAxisValue;

        orbitalFollow.VerticalAxis =
            verticalAxis;

        isPoleSpinCameraReturning = false;

        // Restore the original mouse orbit input state.
        inputAxisController.enabled =
            wasInputControllerEnabled;

        Debug.Log(
            "POLE SPIN CAMERA | Normal orbit restored.",
            this
        );
    }

    private void RestorePoleSpinCameraImmediately()
    {
        if (!isPoleSpinCameraActive &&
            !isPoleSpinCameraReturning)
        {
            return;
        }

        orbitalFollow.Orbits = normalOrbits;

        var verticalAxis =
            orbitalFollow.VerticalAxis;

        verticalAxis.Value =
            normalVerticalAxisValue;

        orbitalFollow.VerticalAxis =
            verticalAxis;

        isPoleSpinCameraActive = false;
        isPoleSpinCameraReturning = false;

        if (inputAxisController != null)
        {
            inputAxisController.enabled =
                wasInputControllerEnabled;
        }
    }

    private void OnDisable()
    {
        RestorePoleSpinCameraImmediately();
        EndActionCamera();
    }
}
