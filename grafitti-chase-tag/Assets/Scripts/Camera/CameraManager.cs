using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private InputManager inputManager;

    [Header("References")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraTransform;

    [Header("Collision")]
    [SerializeField] private LayerMask collisonLayers;
    [SerializeField] private float cameraCollisionOffset = 0.2f;
    [SerializeField] private float minimumCollisionOffset = 0.2f;
    [SerializeField] private float cameraCollisionRadius = 2f;

    [Header("Follow")]
    [SerializeField] private float cameraFollowSpeed = 0.2f;

    [Header("Look")]
    [SerializeField] private float cameraLookSpeed = 2f;
    [SerializeField] private float cameraPivotSpeed = 2f;

    [Header("Pivot Limits")]
    [SerializeField] private float minimumPivotAngle = -35f;
    [SerializeField] private float maximumPivotAngle = 35f;

    private float defaultPosition;

    private Vector3 cameraFollowVelocity;
    private Vector3 cameraVectorPosition;

    private float lookAngle;
    private float pivotAngle;

    private void Awake()
    {
        inputManager =
            FindAnyObjectByType<InputManager>();

        if (targetTransform == null)
        {
            PlayerManager player =
                FindAnyObjectByType<PlayerManager>();

            if (player != null)
            {
                targetTransform = player.transform;
            }
        }

        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                cameraTransform =
                    mainCamera.transform;
            }
        }

        if (cameraTransform != null)
        {
            defaultPosition =
                cameraTransform.localPosition.z;
        }
    }

    public void HandleAllCameraMovement()
    {
        if (inputManager == null ||
            targetTransform == null ||
            cameraPivot == null ||
            cameraTransform == null)
        {
            return;
        }

        FollowTarget();
        RotateCamera();
        HandleCameraCollisions();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition =
            Vector3.SmoothDamp(
                transform.position,
                targetTransform.position,
                ref cameraFollowVelocity,
                cameraFollowSpeed
            );

        transform.position =
            targetPosition;
    }

    private void RotateCamera()
    {
        Vector2 cameraInput =
            inputManager.CameraInput;

        lookAngle +=
            cameraInput.x *
            cameraLookSpeed;

        pivotAngle -=
            cameraInput.y *
            cameraPivotSpeed;

        pivotAngle =
            Mathf.Clamp(
                pivotAngle,
                minimumPivotAngle,
                maximumPivotAngle
            );

        transform.rotation =
            Quaternion.Euler(
                0f,
                lookAngle,
                0f
            );

        cameraPivot.localRotation =
            Quaternion.Euler(
                pivotAngle,
                0f,
                0f
            );
    }

    private void HandleCameraCollisions()
    {
        float targetPosition =
            defaultPosition;

        Vector3 direction =
            cameraTransform.position -
            cameraPivot.position;

        direction.Normalize();

        if (Physics.SphereCast(
            cameraPivot.position,
            cameraCollisionRadius,
            direction,
            out RaycastHit hit,
            Mathf.Abs(defaultPosition),
            collisonLayers))
        {
            targetPosition =
                -hit.distance +
                cameraCollisionOffset;
        }

        if (Mathf.Abs(targetPosition) <
            minimumCollisionOffset)
        {
            targetPosition =
                -minimumCollisionOffset;
        }

        cameraVectorPosition.z =
            Mathf.Lerp(
                cameraTransform.localPosition.z,
                targetPosition,
                0.2f
            );

        cameraTransform.localPosition =
            cameraVectorPosition;
    }
}