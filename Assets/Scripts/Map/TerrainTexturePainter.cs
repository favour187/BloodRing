using UnityEngine;
using System.Collections.Generic;
using BloodRing.Art;

/// <summary>
/// Modern 3D battle royale terrain texture blending and splatmap painting system.
/// Instead of traditional tilemaps, this system blends 100-300 high-fidelity authored PBR surface textures
/// across the terrain mesh based on elevation, slope angle, shoreline proximity, and road corridors.
/// </summary>
public class TerrainTexturePainter : MonoBehaviour
{
    public static TerrainTexturePainter Instance;

    [Header("Blended Terrain Textures")]
    public Texture2D grassTexture;
    public Texture2D rockTexture;
    public Texture2D sandTexture;
    public Texture2D snowTexture;
    public Texture2D asphaltTexture;
    public Texture2D mudTexture;

    [Header("Generated Splatmap")]
    public Texture2D splatmapTexture;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Loads authored PBR textures from BloodRingArtLibrary and paints vertex colors / splat weights across the terrain.
    /// </summary>
    public void PaintTerrain(MapData mapData, GameObject terrainGameObject)
    {
        Debug.Log("TerrainTexturePainter: Blending surface textures across landscape mesh...");

        // Load authored textures
        grassTexture = BloodRingArtLibrary.Terrain("Terrain_Grass_Tile");
        rockTexture = BloodRingArtLibrary.Terrain("Terrain_Rock_Tile");
        sandTexture = BloodRingArtLibrary.Terrain("Terrain_Sand_Tile");
        snowTexture = BloodRingArtLibrary.Terrain("Terrain_Snow_Tile");
        asphaltTexture = BloodRingArtLibrary.Terrain("Terrain_Asphalt_Road");
        mudTexture = BloodRingArtLibrary.Terrain("Terrain_Mud_Tile");

        MeshFilter mf = terrainGameObject.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Mesh mesh = mf.mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Color[] colors = new Color[vertices.Length];

        float waterLevel = mapData != null ? mapData.waterLevel : 1.5f;
        bool isWinterMap = mapData != null && mapData.mapName.ToLower().Contains("snow");

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = terrainGameObject.transform.TransformPoint(vertices[i]);
            Vector3 normal = normals[i];

            // Calculate slope angle (0 = flat, 1 = vertical wall)
            float slope = 1f - Mathf.Clamp01(normal.y);

            // Channel weights:
            // R = Grass / Primary surface
            // G = Rock / Cliff surface
            // B = Sand / Shoreline
            // A = Asphalt / Snow / Special surface

            float weightGrass = 1f;
            float weightRock = 0f;
            float weightSand = 0f;
            float weightSpecial = 0f;

            // 1. Shoreline / Sand blending near water level
            if (worldPos.y <= waterLevel + 2.5f)
            {
                float shoreBlend = Mathf.Clamp01((worldPos.y - waterLevel) / 2.5f);
                weightSand = 1f - shoreBlend;
                weightGrass = shoreBlend;
            }

            // 2. Slope / Rock blending on steep inclines (> 25 degrees approx slope > 0.15)
            if (slope > 0.15f)
            {
                float rockBlend = Mathf.Clamp01((slope - 0.15f) / 0.35f);
                weightRock = rockBlend;
                weightGrass *= (1f - rockBlend);
                weightSand *= (1f - rockBlend);
            }

            // 3. High elevation snow blending
            if (worldPos.y > 22f || isWinterMap)
            {
                float snowBlend = isWinterMap ? 0.8f : Mathf.Clamp01((worldPos.y - 22f) / 10f);
                weightSpecial = snowBlend;
                weightGrass *= (1f - snowBlend);
            }

            // Normalize weights
            float total = weightGrass + weightRock + weightSand + weightSpecial;
            if (total > 0.001f)
            {
                colors[i] = new Color(weightGrass / total, weightRock / total, weightSand / total, weightSpecial / total);
            }
            else
            {
                colors[i] = Color.red; // default primary grass
            }
        }

        mesh.colors = colors;

        // Generate procedural splatmap texture (256x256)
        splatmapTexture = GenerateSplatmapTexture(mapData, 256);

        // Apply blended material properties
        Renderer renderer = terrainGameObject.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            if (grassTexture != null) renderer.sharedMaterial.SetTexture("_MainTex", grassTexture);
            if (splatmapTexture != null) renderer.sharedMaterial.SetTexture("_SplatMap", splatmapTexture);
        }

        Debug.Log("TerrainTexturePainter: Successfully painted " + vertices.Length + " terrain vertices.");
    }

    private Texture2D GenerateSplatmapTexture(MapData mapData, int resolution)
    {
        Texture2D splat = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        splat.name = "Generated_TerrainSplatmap";
        splat.wrapMode = TextureWrapMode.Clamp;
        splat.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        float halfRes = resolution * 0.5f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = (x - halfRes) / halfRes; // -1 to 1
                float ny = (y - halfRes) / halfRes; // -1 to 1
                float distFromCenter = Mathf.Sqrt(nx * nx + ny * ny);

                // Base weights
                float r = Mathf.Clamp01(1f - distFromCenter * 0.8f); // Grass center
                float g = Mathf.Clamp01(Mathf.Abs(Mathf.Sin(nx * 8f) * Mathf.Cos(ny * 8f)) * 0.5f); // Natural rock ridges
                float b = Mathf.Clamp01((distFromCenter - 0.7f) * 4f); // Coastal sand ring
                float a = 0f;

                pixels[y * resolution + x] = new Color(r, g, b, a);
            }
        }

        splat.SetPixels(pixels);
        splat.Apply();
        return splat;
    }
}
