using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates road meshes on the map from RoadSegment data.
/// Roads are rendered as flat quad strips with different materials per road type.
/// Vehicles get a speed bonus on roads.
/// </summary>
public class RoadNetwork : MonoBehaviour
{
    public static RoadNetwork Instance;

    private MapData currentMap;
    private List<GameObject> roadObjects = new List<GameObject>();
    private Dictionary<RoadType, Material> roadMaterials = new Dictionary<RoadType, Material>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Generate all roads from map data.</summary>
    public void GenerateRoads(MapData map, Transform parent)
    {
        currentMap = map;
        CreateRoadMaterials();

        foreach (RoadSegment road in map.roads)
        {
            GameObject roadGo = CreateRoadMesh(road);
            roadGo.transform.SetParent(parent);
            roadObjects.Add(roadGo);
        }

        Debug.Log($"[RoadNetwork] Generated {roadObjects.Count} road segments on {map.displayName}");
    }

    /// <summary>Check if a position is on a road. Returns road type if on road, null otherwise.</summary>
    public RoadType? GetRoadAtPosition(Vector3 worldPos)
    {
        if (currentMap == null) return null;

        foreach (RoadSegment road in currentMap.roads)
        {
            float dist = DistanceToSegment(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(road.startPoint.x, road.startPoint.z),
                new Vector2(road.endPoint.x, road.endPoint.z)
            );
            if (dist <= road.width * 0.5f) return road.roadType;
        }
        return null;
    }

    /// <summary>Get speed multiplier for vehicles on roads.</summary>
    public float GetRoadSpeedMultiplier(Vector3 worldPos)
    {
        RoadType? roadType = GetRoadAtPosition(worldPos);
        if (roadType == null) return 1.0f;

        switch (roadType)
        {
            case RoadType.Highway: return 1.4f;
            case RoadType.Paved: return 1.25f;
            case RoadType.Dirt: return 1.1f;
            case RoadType.Bridge: return 1.2f;
            default: return 1.0f;
        }
    }

    private GameObject CreateRoadMesh(RoadSegment road)
    {
        Vector3 start = road.startPoint;
        Vector3 end = road.endPoint;
        float halfWidth = road.width * 0.5f;

        Vector3 dir = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

        // Create quad mesh
        Vector3[] vertices = new Vector3[]
        {
            start - right * halfWidth,
            start + right * halfWidth,
            end + right * halfWidth,
            end - right * halfWidth
        };

        int[] triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, Vector3.Distance(start, end) / road.width),
            new Vector2(0, Vector3.Distance(start, end) / road.width)
        };

        Mesh mesh = new Mesh();
        mesh.name = "Road_" + road.roadType;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        GameObject go = new GameObject("Road_" + road.roadType);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        if (roadMaterials.ContainsKey(road.roadType))
            renderer.material = roadMaterials[road.roadType];

        go.isStatic = true;
        go.layer = LayerMask.NameToLayer("Default");

        return go;
    }

    private void CreateRoadMaterials()
    {
        // Highway — dark asphalt with lane markings
        Material highwayMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        highwayMat.color = new Color(0.15f, 0.15f, 0.15f);
        highwayMat.mainTextureScale = new Vector2(2, 10);
        roadMaterials[RoadType.Highway] = highwayMat;

        // Paved — lighter asphalt
        Material pavedMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        pavedMat.color = new Color(0.25f, 0.25f, 0.22f);
        pavedMat.mainTextureScale = new Vector2(2, 8);
        roadMaterials[RoadType.Paved] = pavedMat;

        // Dirt — brown earth
        Material dirtMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        dirtMat.color = new Color(0.45f, 0.35f, 0.2f);
        dirtMat.mainTextureScale = new Vector2(2, 6);
        roadMaterials[RoadType.Dirt] = dirtMat;

        // Bridge — metal grate
        Material bridgeMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        bridgeMat.color = new Color(0.3f, 0.3f, 0.35f);
        bridgeMat.mainTextureScale = new Vector2(2, 4);
        roadMaterials[RoadType.Bridge] = bridgeMat;
    }

    /// <summary>Distance from point to line segment.</summary>
    private float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(point, closest);
    }

    /// <summary>Clean up all road objects.</summary>
    public void ClearRoads()
    {
        foreach (var go in roadObjects) if (go != null) Destroy(go);
        roadObjects.Clear();
    }
}
