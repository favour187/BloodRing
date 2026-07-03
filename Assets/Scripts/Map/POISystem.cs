using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages Points of Interest on the map.
/// Shows location name popups when players enter POI zones,
/// provides POI data for minimap markers, and tracks contested areas.
/// </summary>
public class POISystem : MonoBehaviour
{
    public static POISystem Instance;

    private MapData currentMap;
    private List<POIData> activePOIs = new List<POIData>();

    // Player tracking
    private string currentPOIName = "";
    private float poiFadeTimer = 0f;
    private const float POI_DISPLAY_DURATION = 3f;

    // Minimap markers
    private Dictionary<string, GameObject> poiMarkers = new Dictionary<string, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Initialize POI system with map data.</summary>
    public void Initialize(MapData map)
    {
        currentMap = map;
        activePOIs = map.pointsOfInterest;
        Debug.Log($"[POISystem] Initialized with {activePOIs.Count} POIs on {map.displayName}");

        // Create minimap markers for each POI
        CreateMinimapMarkers();
    }

    /// <summary>Check which POI a position is inside. Returns null if outside all.</summary>
    public POIData GetPOIAtPosition(Vector3 worldPos)
    {
        foreach (POIData poi in activePOIs)
        {
            float dist = Vector3.Distance(
                new Vector3(worldPos.x, 0, worldPos.z),
                new Vector3(poi.position.x, 0, poi.position.z)
            );
            if (dist <= poi.radius) return poi;
        }
        return null;
    }

    /// <summary>Get the nearest POI to a position (even if outside its radius).</summary>
    public POIData GetNearestPOI(Vector3 worldPos)
    {
        POIData nearest = null;
        float nearestDist = float.MaxValue;

        foreach (POIData poi in activePOIs)
        {
            float dist = Vector3.Distance(
                new Vector3(worldPos.x, 0, worldPos.z),
                new Vector3(poi.position.x, 0, poi.position.z)
            );
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = poi;
            }
        }
        return nearest;
    }

    /// <summary>Get all POIs of a specific type.</summary>
    public List<POIData> GetPOIsByType(POIType type)
    {
        List<POIData> result = new List<POIData>();
        foreach (POIData poi in activePOIs)
        {
            if (poi.poiType == type) result.Add(poi);
        }
        return result;
    }

    /// <summary>Get loot density multiplier for a position based on POI and loot zones.</summary>
    public float GetLootDensityAt(Vector3 worldPos)
    {
        // Check POIs
        POIData poi = GetPOIAtPosition(worldPos);
        if (poi != null) return poi.lootDensity;

        // Check loot zones
        if (currentMap != null)
        {
            foreach (LootZone zone in currentMap.lootZones)
            {
                float dist = Vector3.Distance(
                    new Vector3(worldPos.x, 0, worldPos.z),
                    new Vector3(zone.center.x, 0, zone.center.z)
                );
                if (dist <= zone.radius)
                {
                    switch (zone.tier)
                    {
                        case LootTier.Legendary: return 1.0f;
                        case LootTier.Epic: return 0.85f;
                        case LootTier.Rare: return 0.7f;
                        case LootTier.Uncommon: return 0.5f;
                        default: return 0.3f;
                    }
                }
            }
        }

        return 0.2f; // Base density for wilderness
    }

    /// <summary>Get weapon bias for loot generation at a position.</summary>
    public AmmoType GetWeaponBiasAt(Vector3 worldPos)
    {
        if (currentMap == null) return AmmoType.RifleAmmo;

        foreach (LootZone zone in currentMap.lootZones)
        {
            float dist = Vector3.Distance(
                new Vector3(worldPos.x, 0, worldPos.z),
                new Vector3(zone.center.x, 0, zone.center.z)
            );
            if (dist <= zone.radius) return zone.weaponBias;
        }
        return AmmoType.RifleAmmo;
    }

    /// <summary>Get the loot tier at a position.</summary>
    public LootTier GetLootTierAt(Vector3 worldPos)
    {
        if (currentMap == null) return LootTier.Common;

        foreach (LootZone zone in currentMap.lootZones)
        {
            float dist = Vector3.Distance(
                new Vector3(worldPos.x, 0, worldPos.z),
                new Vector3(zone.center.x, 0, zone.center.z)
            );
            if (dist <= zone.radius) return zone.tier;
        }
        return LootTier.Common;
    }

    /// <summary>Update called by player controller to check POI entry.</summary>
    public string UpdatePlayerPosition(Vector3 worldPos)
    {
        POIData current = GetPOIAtPosition(worldPos);
        string newPOIName = current != null ? current.poiName : "";

        if (newPOIName != currentPOIName)
        {
            currentPOIName = newPOIName;
            poiFadeTimer = POI_DISPLAY_DURATION;

            if (!string.IsNullOrEmpty(currentPOIName))
            {
                Debug.Log($"[POISystem] Entered: {currentPOIName}");
                return currentPOIName;
            }
        }

        if (poiFadeTimer > 0) poiFadeTimer -= Time.deltaTime;

        return currentPOIName;
    }

    /// <summary>Get current POI name for HUD display.</summary>
    public string GetCurrentPOIName() => currentPOIName;

    /// <summary>Get POI display alpha (for fade effect).</summary>
    public float GetPOIDisplayAlpha()
    {
        if (string.IsNullOrEmpty(currentPOIName)) return 0f;
        if (poiFadeTimer > 1f) return 1f;
        return Mathf.Clamp01(poiFadeTimer);
    }

    /// <summary>Get all active POIs for minimap rendering.</summary>
    public List<POIData> GetAllPOIs() => activePOIs;

    /// <summary>Get POI color based on type for minimap markers.</summary>
    public static Color GetPOIColor(POIType type)
    {
        switch (type)
        {
            case POIType.HotDrop: return new Color(1f, 0.2f, 0.1f);      // Red — high danger
            case POIType.Military: return new Color(0.8f, 0.6f, 0f);     // Gold — good loot
            case POIType.Research: return new Color(0.4f, 0.8f, 1f);     // Cyan — tech loot
            case POIType.Industrial: return new Color(0.7f, 0.5f, 0.2f); // Brown — industrial
            case POIType.Town: return new Color(0.9f, 0.9f, 0.9f);       // White — urban
            case POIType.Village: return new Color(0.6f, 0.8f, 0.4f);    // Green — rural
            case POIType.Coastal: return new Color(0.3f, 0.5f, 0.9f);    // Blue — water
            case POIType.Forest: return new Color(0.2f, 0.6f, 0.2f);     // Dark green — nature
            case POIType.Landmark: return new Color(1f, 0.8f, 0f);       // Gold — special
            default: return Color.white;
        }
    }

    /// <summary>Get POI icon character for minimap labels.</summary>
    public static string GetPOIIcon(POIType type)
    {
        switch (type)
        {
            case POIType.HotDrop: return "⚠";
            case POIType.Military: return "★";
            case POIType.Research: return "◆";
            case POIType.Industrial: return "▪";
            case POIType.Town: return "■";
            case POIType.Village: return "▫";
            case POIType.Coastal: return "~";
            case POIType.Forest: return "♠";
            case POIType.Landmark: return "▲";
            default: return "●";
        }
    }

    private void CreateMinimapMarkers()
    {
        // Minimap markers are created dynamically by the minimap camera system
        // POI data is queried by the minimap renderer each frame
        Debug.Log($"[POISystem] {activePOIs.Count} POI markers ready for minimap");
    }
}
