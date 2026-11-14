using UnityEngine;

public class CharacterTaggedVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer[] characterRenderers;
    [SerializeField] private Material taggedMaterial;

    private Material[][] originalMaterials;
    private bool isInitialized;

    private void Awake()
    {
        if (characterRenderers == null ||
            characterRenderers.Length == 0)
        {
            characterRenderers =
                GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        if (taggedMaterial == null)
        {
            Debug.LogError(
                "TAGGED VISUAL | Tagged material is missing.",
                this
            );

            return;
        }

        originalMaterials =
            new Material[characterRenderers.Length][];

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] == null)
                continue;

            originalMaterials[i] =
                characterRenderers[i].sharedMaterials;
        }

        isInitialized = true;
    }

    public void ApplyTaggedVisual()
    {
        if (!isInitialized)
            return;

        foreach (SkinnedMeshRenderer characterRenderer
                 in characterRenderers)
        {
            if (characterRenderer == null)
                continue;

            Material[] materials =
                new Material[characterRenderer.sharedMaterials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = taggedMaterial;
            }

            characterRenderer.sharedMaterials = materials;
        }

        Debug.Log(
            $"TAGGED VISUAL | Applied to {gameObject.name}.",
            this
        );
    }

    public void ResetVisual()
    {
        if (!isInitialized)
            return;

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] == null ||
                originalMaterials[i] == null)
            {
                continue;
            }

            characterRenderers[i].sharedMaterials =
                originalMaterials[i];
        }

        Debug.Log(
            $"TAGGED VISUAL | Restored {gameObject.name}.",
            this
        );
    }
}
