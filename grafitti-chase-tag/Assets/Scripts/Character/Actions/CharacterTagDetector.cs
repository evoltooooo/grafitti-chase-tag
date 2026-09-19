using UnityEngine;

public class CharacterTagDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float tagDistance = 1.5f;
    [SerializeField] private float tagRadius = 0.5f;
    [SerializeField] private LayerMask tagLayers;

    public bool TryDetectTag(out Collider target)
    {
        target = null;

        Vector3 origin =
            transform.position +
            Vector3.up * 1f;

        Vector3 direction =
            transform.forward;

        if (Physics.SphereCast(
                origin,
                tagRadius,
                direction,
                out RaycastHit hit,
                tagDistance,
                tagLayers,
                QueryTriggerInteraction.Ignore))
        {
            target = hit.collider;
            return true;
        }

        return false;
    }
}