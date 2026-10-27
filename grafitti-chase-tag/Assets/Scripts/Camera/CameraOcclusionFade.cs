
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraOcclusionFade : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Transform viewTarget;
    [SerializeField] private Renderer[] playerRenderers;

    [Header("Obstacle Detection")]
    [SerializeField] private LayerMask fadeObstacleLayers;
    [SerializeField] private float targetHeight = 1.4f;
    [SerializeField] private float raycastRadius = 0.12f;
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 5f;

    [SerializeField, Range(0.05f, 1f)]
    private float obstacleOpacity = 0.3f;

    [Header("Player Fade")]
    [SerializeField] private float playerFadeDistance = 1.1f;
    [SerializeField] private float playerFadeSpeed = 8f;

    [SerializeField, Range(0.05f, 1f)]
    private float playerOpacity = 0.25f;

    private readonly Dictionary<Renderer, FadeState> obstacleStates = new();
    private readonly HashSet<Renderer> hitThisFrame = new();
    private readonly List<Renderer> cleanupList = new();

    private FadeState[] playerStates;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private class FadeState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] fadeMaterials;
        public int[] colorPropertyIds;
        public Color[] originalColors;
        public float opacity = 1f;
        public bool usingFadeMaterials;
    }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (player != null && viewTarget == null)
            viewTarget = player;

        playerStates = CreateStates(playerRenderers);
    }

    private void LateUpdate()
    {
        if (targetCamera == null || player == null || viewTarget == null)
            return;

        UpdateObstacleFades();
        UpdatePlayerFade();
    }

    private void UpdateObstacleFades()
    {
        hitThisFrame.Clear();

        Vector3 origin = targetCamera.transform.position;
        Vector3 target = viewTarget.position + Vector3.up * targetHeight;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance > 0.01f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                raycastRadius,
                direction / distance,
                distance,
                fadeObstacleLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                CameraFadeTarget fadeTarget =
                    hit.collider.GetComponent<CameraFadeTarget>();

                Renderer[] renderers;

                if (fadeTarget != null)
                {
                    // Use explicitly assigned visual renderers.
                    renderers = fadeTarget.TargetRenderers;
                }
                else
                {
                    // Keep the existing behavior for objects
                    // whose renderers are on the collider or its children.
                    renderers = hit.collider.GetComponentsInChildren<Renderer>();
                }

                foreach (Renderer rend in renderers)
                {
                    if (rend == null || !rend.enabled)
                        continue;

                    if (!obstacleStates.TryGetValue(rend, out FadeState state))
                    {
                        state = CreateState(rend);
                        if (state == null)
                            continue;

                        obstacleStates.Add(rend, state);
                    }

                    hitThisFrame.Add(rend);
                }

                foreach (Renderer rend in renderers)
                {
                    if (rend == null || !rend.enabled)
                        continue;

                    if (!obstacleStates.TryGetValue(rend, out FadeState state))
                    {
                        state = CreateState(rend);

                        if (state == null)
                            continue;

                        obstacleStates.Add(rend, state);
                    }

                    hitThisFrame.Add(rend);
                }
            }
        }

        foreach (var pair in obstacleStates)
        {
            FadeState state = pair.Value;

            float targetOpacity = hitThisFrame.Contains(pair.Key)
                ? obstacleOpacity
                : 1f;

            float speed = targetOpacity < state.opacity
                ? fadeInSpeed
                : fadeOutSpeed;

            state.opacity = Mathf.MoveTowards(
                state.opacity,
                targetOpacity,
                speed * Time.deltaTime
            );

            ApplyOpacity(state, state.opacity);
        }

        cleanupList.Clear();

        foreach (var pair in obstacleStates)
        {
            if (pair.Key == null)
                cleanupList.Add(pair.Key);
        }

        foreach (Renderer rend in cleanupList)
        {
            DestroyFadeMaterials(obstacleStates[rend]);
            obstacleStates.Remove(rend);
        }
    }

    private void UpdatePlayerFade()
    {
        if (playerStates == null)
            return;

        float distance = Vector3.Distance(
            targetCamera.transform.position,
            player.position
        );

        float targetOpacity = distance < playerFadeDistance
            ? playerOpacity
            : 1f;

        foreach (FadeState state in playerStates)
        {
            if (state == null || state.renderer == null)
                continue;

            float speed = targetOpacity < state.opacity
                ? playerFadeSpeed
                : fadeOutSpeed;

            state.opacity = Mathf.MoveTowards(
                state.opacity,
                targetOpacity,
                speed * Time.deltaTime
            );

            ApplyOpacity(state, state.opacity);
        }
    }

    private FadeState[] CreateStates(Renderer[] renderers)
    {
        if (renderers == null)
            return System.Array.Empty<FadeState>();

        var states = new List<FadeState>();

        foreach (Renderer rend in renderers)
        {
            FadeState state = CreateState(rend);

            if (state != null)
                states.Add(state);
        }

        return states.ToArray();
    }

    private FadeState CreateState(Renderer rend)
    {
        if (rend == null)
            return null;

        Material[] originals = rend.sharedMaterials;
        Material[] fadeMaterials = new Material[originals.Length];
        int[] colorIds = new int[originals.Length];
        Color[] originalColors = new Color[originals.Length];

        bool foundColor = false;

        for (int i = 0; i < originals.Length; i++)
        {
            Material original = originals[i];

            if (original == null)
                continue;

            int colorId = original.HasProperty(BaseColorId)
                ? BaseColorId
                : original.HasProperty(ColorId)
                    ? ColorId
                    : 0;

            colorIds[i] = colorId;

            if (colorId != 0)
            {
                originalColors[i] = original.GetColor(colorId);
                foundColor = true;
            }

            Material fade = new Material(original);
            fade.name = original.name + "_RuntimeFade";
            ConfigureTransparentMaterial(fade);

            fadeMaterials[i] = fade;
        }

        if (!foundColor)
        {
            foreach (Material mat in fadeMaterials)
            {
                if (mat != null)
                    Destroy(mat);
            }

            return null;
        }

        return new FadeState
        {
            renderer = rend,
            originalMaterials = originals,
            fadeMaterials = fadeMaterials,
            colorPropertyIds = colorIds,
            originalColors = originalColors
        };
    }

    private void ConfigureTransparentMaterial(Material mat)
    {
        // Configure URP/Lit or another shader that exposes
        // the standard surface/blending properties.
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);

        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);

        mat.SetOverrideTag("RenderType", "Transparent");

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHAMODULATE_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    private void ApplyOpacity(FadeState state, float opacity)
    {
        Renderer rend = state.renderer;

        if (rend == null)
            return;

        // Use the original materials whenever fully visible.
        if (opacity >= 0.999f)
        {
            if (state.usingFadeMaterials)
            {
                rend.sharedMaterials = state.originalMaterials;
                state.usingFadeMaterials = false;
            }

            return;
        }

        if (!state.usingFadeMaterials)
        {
            rend.sharedMaterials = state.fadeMaterials;
            state.usingFadeMaterials = true;
        }

        for (int i = 0; i < state.fadeMaterials.Length; i++)
        {
            Material mat = state.fadeMaterials[i];

            if (mat == null || state.colorPropertyIds[i] == 0)
                continue;

            Color color = state.originalColors[i];
            color.a *= opacity;

            mat.SetColor(state.colorPropertyIds[i], color);
        }
    }

    private void DestroyFadeMaterials(FadeState state)
    {
        if (state == null)
            return;

        if (state.renderer != null && state.usingFadeMaterials)
            state.renderer.sharedMaterials = state.originalMaterials;

        if (state.fadeMaterials == null)
            return;

        foreach (Material mat in state.fadeMaterials)
        {
            if (mat == null)
                continue;

            if (Application.isPlaying)
                Destroy(mat);
            else
                DestroyImmediate(mat);
        }
    }

    private void OnDisable()
    {
        foreach (FadeState state in obstacleStates.Values)
            DestroyFadeMaterials(state);

        obstacleStates.Clear();

        if (playerStates != null)
        {
            foreach (FadeState state in playerStates)
                DestroyFadeMaterials(state);
        }
    }
}
