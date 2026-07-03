using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates river meshes on the map from RiverSegment data.
/// Rivers have animated flowing water, slow player movement,
/// and block vehicle passage at deep points.
/// </summary>
public class RiverSystem : MonoBehaviour
{
    public static RiverSystem Instance;

    private MapData currentMap;
    private List<GameObject> riverObjects = new List<GameObject>();
    private List<RiverSegment> activeRivers = new List<RiverSegment>();
    private Material riverMat;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Generate all rivers from map data.</summary>
    public void GenerateRivers(MapData map, Transform parent)
    {
        currentMap = map;
        activeRivers = map.rivers;
        CreateRiverMaterial();

        foreach (RiverSegment river in map.rivers)
        {
            GameObject riverGo = CreateRiverMesh(river);
            riverGo.transform.SetParent(parent);
            riverObjects.Add(riverGo);
        }

        Debug.Log($"[RiverSystem] Generated {riverObjects.Count} river segments on {map.displayName}");
    }

    private void Update()
    {
        // Animate river UV flow
        if (riverMat != null)
        {
            Vector2 offset = riverMat.mainTextureOffset;
            offset.y += Time.deltaTime * 0.3f;
            riverMat.mainTextureOffset = offset;
        }
    }

    /// <summary>Check if a position is in a river. Returns river data if in water, null otherwise.</summary>
    public RiverSegment GetRiverAtPosition(Vector3 worldPos)
    {
        foreach (RiverSegment river in activeRivers)
        {
            float dist = DistanceToSegment(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(river.startPoint.x, river.startPoint.z),
                new Vector2(river.endPoint.x, river.endPoint.z)
            );
            if (dist <= river.width * 0.5f) return river;
        }
        return null;
    }

    /// <summary>Get movement speed multiplier when in a river.</summary>
    public float GetRiverSpeedMultiplier(Vector3 worldPos)
    {
        RiverSegment river = GetRiverAtPosition(worldPos);
        if (river == null) return 1.0f;

        // Deeper rivers slow more
        float depthFactor = Mathf.Clamp01(river.depth / 1f);
        return Mathf.Lerp(0.7f, 0.4f, depthFactor);
    }

    /// <summary>Check if vehicles can cross at this position (shallow rivers only).</summary>
    public bool CanVehicleCross(Vector3 worldPos)
    {
        RiverSegment river = GetRiverAtPosition(worldPos);
        if (river == null) return true;
        return river.depth < 0.4f; // Shallow rivers allow vehicle crossing
    }

    /// <summary>Get water depth at a position (0 if not in river).</summary>
    public float GetWaterDepth(Vector3 worldPos)
    {
        RiverSegment river = GetRiverAtPosition(worldPos);
        if (river == null) return 0f;
        return river.depth;
    }

    private GameObject CreateRiverMesh(RiverSegment river)
    {
        Vector3 start = river.startPoint;
        Vector3 end = river.endPoint;
        float halfWidth = river.width * 0.5f;

        Vector3 dir = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

        // Create slightly below terrain for natural look
        start.y = -0.1f;
        end.y = -0.1f;

        Vector3[] vertices = new Vector3[]
        {
            start - right * halfWidth,
            start + right * halfWidth,
            end + right * halfWidth,
            end - right * halfWidth
        };

        int[] triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        float length = Vector3.Distance(start, end);
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, length / river.width),
            new Vector2(0, length / river.width)
        };

        Mesh mesh = new Mesh();
        mesh.name = "River";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        GameObject go = new GameObject("River");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.material = riverMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Water trigger for player detection
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(river.width, 1f, length);
        col.center = new Vector3(0, 0.5f, length * 0.5f);

        go.isStatic = true;
        return go;
    }

    private void CreateRiverMaterial()
    {
        riverMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        riverMat.color = currentMap != null ? currentMap.waterTint : new Color(0.1f, 0.35f, 0.75f, 0.75f);
        riverMat.SetFloat("_Mode", 3); // Transparent
        riverMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        riverMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        riverMat.SetInt("_ZWrite", 0);
        riverMat.DisableKeyword("_ALPHATEST_ON");
        riverMat.EnableKeyword("_ALPHABLEND_ON");
        riverMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        riverMat.renderQueue = 3000;
        riverMat.mainTextureScale = new Vector2(3, 8);
    }

    private float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(point, closest);
    }

    /// <summary>Clean up all river objects.</summary>
    public void ClearRivers()
    {
        foreach (var go in riverObjects) if (go != null) Destroy(go);
        riverObjects.Clear();
        activeRivers.Clear();
    }
}
