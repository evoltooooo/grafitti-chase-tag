
using UnityEngine;

public class CameraFadeTarget : MonoBehaviour
{
    [Tooltip("Visible renderers that should fade when this collider is hit.")]
    [SerializeField] private Renderer[] targetRenderers;

    public Renderer[] TargetRenderers => targetRenderers;
}
