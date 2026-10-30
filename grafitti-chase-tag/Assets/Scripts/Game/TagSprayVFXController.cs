
using UnityEngine;

public class TagSprayVFXController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterTagDetector tagDetector;
    [SerializeField] private CharacterActionRuntime actionRuntime;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private MeshFilter sprayMeshFilter;

    [Header("Visual Settings")]
    [SerializeField] private float verticalOffset = 1f;
    [SerializeField] private float forwardOffset = 0f;
    [SerializeField, Range(4, 64)] private int arcSegments = 32;

    private Mesh sprayMesh;

    private void Awake()
    {
        if (tagDetector == null)
            tagDetector = GetComponent<CharacterTagDetector>();

        if (actionRuntime == null)
            actionRuntime = GetComponent<CharacterActionRuntime>();

        if (visualRoot == null || sprayMeshFilter == null)
        {
            Debug.LogError(
                "TAG VFX | Visual Root or Spray Mesh Filter is missing.",
                this
            );
            enabled = false;
            return;
        }

        sprayMesh = new Mesh
        {
            name = "Runtime Tag Spray Mesh"
        };

        sprayMeshFilter.sharedMesh = sprayMesh;

        visualRoot.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        bool isSpraying =
            actionRuntime != null &&
            actionRuntime.IsExecuting &&
            actionRuntime.CurrentActionType == CharacterActionType.Tag;

        if (!isSpraying)
        {
            if (visualRoot.gameObject.activeSelf)
                visualRoot.gameObject.SetActive(false);

            return;
        }

        UpdateSprayGeometry();

        if (!visualRoot.gameObject.activeSelf)
            visualRoot.gameObject.SetActive(true);
    }

    private void UpdateSprayGeometry()
    {
        float range = tagDetector.EffectiveTagDistance;
        float halfAngle = tagDetector.EffectiveTagHalfAngle;

        int vertexCount = arcSegments + 2;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[arcSegments * 3];

        // The first vertex is the spray origin.
        vertices[0] = Vector3.zero;

        // Generate the outer arc.
        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;

            float angle = Mathf.Lerp(
                -halfAngle,
                halfAngle,
                t
            );

            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radians) * range;
            float z = Mathf.Cos(radians) * range;

            vertices[i + 1] = new Vector3(x, 0f, z);
        }

        // Connect the origin to each pair of adjacent arc vertices.
        for (int i = 0; i < arcSegments; i++)
        {
            int triangleIndex = i * 3;

            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        sprayMesh.Clear();
        sprayMesh.vertices = vertices;
        sprayMesh.triangles = triangles;
        sprayMesh.RecalculateNormals();
        sprayMesh.RecalculateBounds();

        visualRoot.localPosition = new Vector3(
            0f,
            verticalOffset,
            forwardOffset
        );

        // The mesh itself defines the dimensions.
        visualRoot.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (sprayMesh != null)
            Destroy(sprayMesh);
    }
}
