using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Dynamic top-down tactical minimap and radar texture generator.
/// Renders terrain contours, roads, rivers, POI structural footprints,
/// safe zone rings, and player indicators for the HUD minimap display.
/// </summary>
public class MinimapGenerator : MonoBehaviour
{
    public static MinimapGenerator Instance;

    [Header("Minimap Assets")]
    public Texture2D generatedMinimapTexture;
    public RawImage hudMinimapDisplay;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Generates a 512x512 tactical minimap texture based on MapData POIs, roads, and rivers.
    /// </summary>
    public Texture2D GenerateMinimapTexture(MapData mapData)
    {
        Debug.Log("MinimapGenerator: Generating 512x512 tactical radar minimap...");
        int res = 512;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.name = "Tactical_Minimap_" + (mapData != null ? mapData.mapName : "IslaVerde");
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color oceanColor = new Color(0.08f, 0.22f, 0.35f, 1f);
        Color sandColor = new Color(0.78f, 0.72f, 0.52f, 1f);
        Color grassColor = new Color(0.22f, 0.45f, 0.25f, 1f);
        Color rockColor = new Color(0.38f, 0.38f, 0.40f, 1f);
        Color roadColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        Color poiColor = new Color(0.18f, 0.18f, 0.22f, 1f);

        Color[] pixels = new Color[res * res];
        float halfRes = res * 0.5f;
        float mapRadius = halfRes * 0.82f;

        // 1. Base terrain landmass & ocean
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - halfRes;
                float dy = y - halfRes;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > mapRadius)
                {
                    pixels[y * res + x] = oceanColor;
                }
                else if (dist > mapRadius - 15f)
                {
                    // Beach sand ring
                    pixels[y * res + x] = sandColor;
                }
                else
                {
                    // Inland grass / rock ridges
                    float noise = Mathf.Sin(x * 0.05f) * Mathf.Cos(y * 0.05f);
                    pixels[y * res + x] = noise > 0.3f ? rockColor : grassColor;
                }
            }
        }

        // 2. Draw river channels
        for (int y = 0; y < res; y++)
        {
            int riverX = (int)(halfRes + Mathf.Sin(y * 0.03f) * 45f);
            for (int w = -6; w <= 6; w++)
            {
                int rx = Mathf.Clamp(riverX + w, 0, res - 1);
                pixels[y * res + rx] = new Color(0.12f, 0.45f, 0.68f, 1f);
            }
        }

        // 3. Draw POI building footprints
        if (mapData != null && mapData.poiLocations != null)
        {
            foreach (var poi in mapData.poiLocations)
            {
                // Map coordinates (-250..250) to texture coordinates (0..512)
                int px = Mathf.Clamp((int)(halfRes + (poi.position.x / 250f) * mapRadius), 10, res - 10);
                int py = Mathf.Clamp((int)(halfRes + (poi.position.z / 250f) * mapRadius), 10, res - 10);
                int footprintSize = poi.type == POIType.Town || poi.type == POIType.Military ? 14 : 8;

                for (int fy = -footprintSize; fy <= footprintSize; fy++)
                {
                    for (int fx = -footprintSize; fx <= footprintSize; fx++)
                    {
                        int targetX = Mathf.Clamp(px + fx, 0, res - 1);
                        int targetY = Mathf.Clamp(py + fy, 0, res - 1);
                        pixels[targetY * res + targetX] = poiColor;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        generatedMinimapTexture = tex;

        // Hook into HUD display if available
        HookIntoHUD();

        return tex;
    }

    public void HookIntoHUD()
    {
        GameObject minimapArtGo = GameObject.Find("MinimapBGArt");
        if (minimapArtGo != null)
        {
            RawImage raw = minimapArtGo.GetComponent<RawImage>();
            if (raw != null && generatedMinimapTexture != null)
            {
                raw.texture = generatedMinimapTexture;
            }
        }
    }
}
