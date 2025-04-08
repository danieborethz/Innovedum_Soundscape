using UnityEngine;

[ExecuteInEditMode]
public class ReverbZone : MonoBehaviour
{
    [Header("Audio Parameters")]
    [Range(10f, 50000f)]
    public float RoomSize = 10f;

    [Range(0f, 3f)]
    public float DecayTime = 0f;

    [Range(0f, 1f)]
    public float WetDryMix = 0f;

    [Range(0f, -20f)]
    public float Eq = 0f;

    [Header("Zone Settings")]
    // Defines the "half-size" of the ellipsoid along each axis for the weighting calculation.
    public Vector3 radii = Vector3.one;

    // The extra distance (beyond the ellipsoid boundary at distance=1) over which reverb fades to 0.
    [Range(0f, 5f)]
    public float fadeRadius = 1f;

    private Mesh sphereMesh;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Material zoneMaterial;

    private void Awake()
    {
        InitializeComponents();
        // We do NOT set transform.localScale = radii * 2f here. We keep localScale = (1,1,1).
    }

    private void OnValidate()
    {
        InitializeComponents();
    }

    /// <summary>
    /// Creates/Assigns the mesh, renderer, and collider if missing.
    /// These are optional for simple visualization/triggers.
    /// </summary>
    private void InitializeComponents()
    {
        // Try to load a built-in Unity sphere mesh
        if (sphereMesh == null)
        {
            sphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            if (sphereMesh == null)
            {
                // Fallback if builtin mesh not found
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(tempSphere);
            }
        }

        // MeshFilter
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshFilter.sharedMesh = sphereMesh;

        // MeshRenderer
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Simple semi-transparent material
        if (zoneMaterial == null)
        {
            zoneMaterial = new Material(Shader.Find("Standard"));
            zoneMaterial.color = new Color(0, 1, 1, 0.3f); // Semi-transparent cyan
            zoneMaterial.SetFloat("_Mode", 2); // Fade mode
            zoneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            zoneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            zoneMaterial.SetInt("_ZWrite", 0);
            zoneMaterial.DisableKeyword("_ALPHATEST_ON");
            zoneMaterial.EnableKeyword("_ALPHABLEND_ON");
            zoneMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            zoneMaterial.renderQueue = 3000; // Transparent queue
        }
        meshRenderer.sharedMaterial = zoneMaterial;

        // Optional MeshCollider (Trigger)
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        meshCollider.sharedMesh = sphereMesh;
        meshCollider.convex = true;
        meshCollider.isTrigger = true;
    }


    /// <summary>
    /// Draw ellipsoid & fade region in the Scene view for clarity.
    /// The transform itself remains unscaled (scale=(1,1,1)).
    /// </summary>
    private void OnDrawGizmos()
    {
        // Core ellipsoid boundary => distance=1 in 'normalized' space
        // We'll draw as a wireframe, color cyan
        Gizmos.color = new Color(0, 1, 1, 0.5f);

        // Construct a matrix to position/orient/scale the gizmo
        // The "core" boundary has scale = (2*radii)
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Matrix4x4 coreMatrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            radii * 2f // diameter
        );
        Gizmos.matrix = coreMatrix;
        // The base sphere is radius=0.5 in local coords => becomes an ellipsoid
        Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
        Gizmos.matrix = oldMatrix;

        // If fadeRadius>0, draw a second boundary for the outer fade region
        if (fadeRadius > 0f)
        {
            // color red for outer boundary
            Gizmos.color = new Color(1, 0, 0, 0.5f);

            float fadeScale = 1f + fadeRadius; // if distance=1 is boundary, 1+fadeRadius is outer
            // e.g., if fadeRadius=1, outer boundary is distance=2 in normalized coords
            Vector3 outerDiameter = radii * 2f * fadeScale;

            Matrix4x4 fadeMatrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                outerDiameter
            );
            Gizmos.matrix = fadeMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
            Gizmos.matrix = oldMatrix;
        }
    }
}
