
using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        Transform lookTarget = target;

        if (lookTarget == null)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            lookTarget = mainCamera.transform;
        }

        // Point the billboard toward the camera.
        Vector3 direction =
            transform.position - lookTarget.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation =
            Quaternion.LookRotation(direction, Vector3.up);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
