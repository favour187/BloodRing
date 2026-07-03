using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject defining a complete battle royale map layout.
/// Contains POIs, roads, rivers, terrain features, loot zones, and spawn data.
/// Create via Assets > Create > BloodRing > MapData
/// </summary>
[CreateAssetMenu(fileName = "NewMap", menuName = "BloodRing/MapData")]
public class MapData : ScriptableObject
{
    public string mapName = "IslaVerde";
    public string displayName = "Isla Verde";
    public string description = "A lush tropical island with dense jungle, coastal towns, and hidden military compounds.";
    public float mapRadius = 500f;
    public Vector3 mapCenter = Vector3.zero;

    [Header("Terrain")]
    public Color groundTint = new Color(0.25f, 0.45f, 0.2f);
    public Color waterTint = new Color(0.1f, 0.35f, 0.75f, 0.8f);
    public Color skyHorizon = new Color(0.85f, 0.65f, 0.4f);
    public float waterLevel = -0.2f;

    [Header("POIs")]
    public List<POIData> pointsOfInterest = new List<POIData>();

    [Header("Roads")]
    public List<RoadSegment> roads = new List<RoadSegment>();

    [Header("Rivers")]
    public List<RiverSegment> rivers = new List<RiverSegment>();

    [Header("Loot Zones")]
    public List<LootZone> lootZones = new List<LootZone>();

    [Header("Spawn Config")]
    public int treeCount = 100;
    public int rockCount = 50;
    public int buildingCount = 20;
    public int vehicleCount = 10;
    public int ziplineCount = 6;

    /// <summary>Get the default IslaVerde map with all data populated.</summary>
    public static MapData GetDefaultMap(string mapName = "IslaVerde")
    {
        MapData m = CreateInstance<MapData>();
        m.mapName = mapName;

        switch (mapName)
        {
            case "RedSands":
                m.displayName = "Red Sands";
                m.description = "A scorched desert canyon with sandstorms, oases, and ancient ruins.";
                m.groundTint = new Color(0.7f, 0.55f, 0.3f);
                m.waterTint = new Color(0.2f, 0.5f, 0.4f, 0.8f);
                m.skyHorizon = new Color(0.95f, 0.6f, 0.2f);
                m.treeCount = 30;
                m.rockCount = 80;
                m.pointsOfInterest = GetRedSandsPOIs();
                m.roads = GetRedSandsRoads();
                m.rivers = GetRedSandsRivers();
                m.lootZones = GetRedSandsLootZones();
                break;

            case "IronGorge":
                m.displayName = "Iron Gorge";
                m.description = "A dark industrial wasteland with factories, mines, and toxic rivers.";
                m.groundTint = new Color(0.2f, 0.22f, 0.18f);
                m.waterTint = new Color(0.1f, 0.4f, 0.1f, 0.8f);
                m.skyHorizon = new Color(0.3f, 0.2f, 0.4f);
                m.treeCount = 20;
                m.rockCount = 60;
                m.buildingCount = 30;
                m.pointsOfInterest = GetIronGorgePOIs();
                m.roads = GetIronGorgeRoads();
                m.rivers = GetIronGorgeRivers();
                m.lootZones = GetIronGorgeLootZones();
                break;

            default: // IslaVerde
                m.displayName = "Isla Verde";
                m.description = "A lush tropical island with dense jungle, coastal towns, and hidden military compounds.";
                m.groundTint = new Color(0.25f, 0.45f, 0.2f);
                m.waterTint = new Color(0.1f, 0.35f, 0.75f, 0.8f);
                m.skyHorizon = new Color(0.85f, 0.65f, 0.4f);
                m.pointsOfInterest = GetIslaVerdePOIs();
                m.roads = GetIslaVerdeRoads();
                m.rivers = GetIslaVerdeRivers();
                m.lootZones = GetIslaVerdeLootZones();
                break;
        }

        return m;
    }

    // ═══════════════════════════════════════════════════════════
    //  ISLA VERDE — Tropical island map
    // ═══════════════════════════════════════════════════════════

    private static List<POIData> GetIslaVerdePOIs()
    {
        return new List<POIData>
        {
            new POIData { poiName = "Crimson Bay", position = new Vector3(0, 0, 200), radius = 60f, lootDensity = 0.8f, poiType = POIType.Town, description = "Coastal town with a harbor and market district." },
            new POIData { poiName = "Phantom Ridge", position = new Vector3(-180, 0, 120), radius = 50f, lootDensity = 0.9f, poiType = POIType.Military, description = "Hidden military compound deep in the jungle." },
            new POIData { poiName = "Neon Outpost", position = new Vector3(150, 0, -80), radius = 45f, lootDensity = 0.7f, poiType = POIType.Research, description = "Abandoned research station with high-tech loot." },
            new POIData { poiName = "Viper Village", position = new Vector3(-100, 0, -150), radius = 55f, lootDensity = 0.6f, poiType = POIType.Village, description = "Small farming village on the southern plains." },
            new POIData { poiName = "Sky Pillar", position = new Vector3(80, 0, 160), radius = 40f, lootDensity = 1.0f, poiType = POIType.Landmark, description = "Ancient watchtower on the highest peak." },
            new POIData { poiName = "Blood Reef", position = new Vector3(250, 0, 50), radius = 35f, lootDensity = 0.5f, poiType = POIType.Coastal, description = "Rocky coastline with shipwrecks and hidden caves." },
            new POIData { poiName = "Dusk Hollow", position = new Vector3(-200, 0, -50), radius = 50f, lootDensity = 0.7f, poiType = POIType.Forest, description = "Dense forest clearing with a crashed drop plane." },
            new POIData { poiName = "Plasma Fields", position = new Vector3(50, 0, -200), radius = 45f, lootDensity = 0.85f, poiType = POIType.Industrial, description = "Power plant with energy weapon spawns." },
            new POIData { poiName = "Shrine of Echoes", position = new Vector3(-50, 0, 80), radius = 30f, lootDensity = 0.6f, poiType = POIType.Landmark, description = "Ancient temple ruins with rare loot." },
            new POIData { poiName = "Iron Docks", position = new Vector3(180, 0, 180), radius = 50f, lootDensity = 0.75f, poiType = POIType.Industrial, description = "Shipping yard with warehouses and cranes." },
            new POIData { poiName = "Cryo Station", position = new Vector3(-120, 0, 200), radius = 35f, lootDensity = 0.9f, poiType = POIType.Research, description = "Arctic research outpost with cryo-barrier tech." },
            new POIData { poiName = "Apex Arena", position = new Vector3(0, 0, 0), radius = 70f, lootDensity = 1.0f, poiType = POIType.HotDrop, description = "Central arena — highest loot density, highest danger." },
        };
    }

    private static List<RoadSegment> GetIslaVerdeRoads()
    {
        return new List<RoadSegment>
        {
            new RoadSegment { startPoint = new Vector3(0, 0.05f, -250), endPoint = new Vector3(0, 0.05f, 250), width = 6f, roadType = RoadType.Highway },
            new RoadSegment { startPoint = new Vector3(-250, 0.05f, 0), endPoint = new Vector3(250, 0.05f, 0), width = 6f, roadType = RoadType.Highway },
            new RoadSegment { startPoint = new Vector3(0, 0.05f, 200), endPoint = new Vector3(180, 0.05f, 180), width = 4f, roadType = RoadType.Paved },
            new RoadSegment { startPoint = new Vector3(-180, 0.05f, 120), endPoint = new Vector3(-50, 0.05f, 80), width = 3.5f, roadType = RoadType.Dirt },
            new RoadSegment { startPoint = new Vector3(150, 0.05f, -80), endPoint = new Vector3(50, 0.05f, -200), width = 4f, roadType = RoadType.Paved },
            new RoadSegment { startPoint = new Vector3(-100, 0.05f, -150), endPoint = new Vector3(0, 0.05f, 0), width = 4f, roadType = RoadType.Paved },
            new RoadSegment { startPoint = new Vector3(0, 0.05f, 0), endPoint = new Vector3(80, 0.05f, 160), width = 3.5f, roadType = RoadType.Dirt },
            new RoadSegment { startPoint = new Vector3(-200, 0.05f, -50), endPoint = new Vector3(-120, 0.05f, 200), width = 3f, roadType = RoadType.Dirt },
        };
    }

    private static List<RiverSegment> GetIslaVerdeRivers()
    {
        return new List<RiverSegment>
        {
            new RiverSegment { startPoint = new Vector3(-200, -0.15f, 180), endPoint = new Vector3(100, -0.15f, -50), width = 12f, depth = 0.5f, flowSpeed = 0.3f },
            new RiverSegment { startPoint = new Vector3(100, -0.15f, -50), endPoint = new Vector3(250, -0.15f, -180), width = 10f, depth = 0.4f, flowSpeed = 0.4f },
            new RiverSegment { startPoint = new Vector3(-80, -0.15f, -200), endPoint = new Vector3(50, -0.15f, -200), width = 8f, depth = 0.3f, flowSpeed = 0.2f },
        };
    }

    private static List<LootZone> GetIslaVerdeLootZones()
    {
        return new List<LootZone>
        {
            new LootZone { center = new Vector3(0, 0, 0), radius = 70f, tier = LootTier.Legendary, weaponBias = AmmoType.RifleAmmo },
            new LootZone { center = new Vector3(-180, 0, 120), radius = 50f, tier = LootTier.Epic, weaponBias = AmmoType.SniperAmmo },
            new LootZone { center = new Vector3(150, 0, -80), radius = 45f, tier = LootTier.Epic, weaponBias = AmmoType.EnergyAmmo },
            new LootZone { center = new Vector3(0, 0, 200), radius = 60f, tier = LootTier.Rare, weaponBias = AmmoType.SMGAmmo },
            new LootZone { center = new Vector3(-100, 0, -150), radius = 55f, tier = LootTier.Uncommon, weaponBias = AmmoType.ShotgunAmmo },
            new LootZone { center = new Vector3(80, 0, 160), radius = 40f, tier = LootTier.Legendary, weaponBias = AmmoType.SniperAmmo },
            new LootZone { center = new Vector3(250, 0, 50), radius = 35f, tier = LootTier.Rare, weaponBias = AmmoType.RifleAmmo },
            new LootZone { center = new Vector3(-200, 0, -50), radius = 50f, tier = LootTier.Uncommon, weaponBias = AmmoType.PistolAmmo },
            new LootZone { center = new Vector3(50, 0, -200), radius = 45f, tier = LootTier.Epic, weaponBias = AmmoType.EnergyAmmo },
            new LootZone { center = new Vector3(-50, 0, 80), radius = 30f, tier = LootTier.Rare, weaponBias = AmmoType.SniperAmmo },
            new LootZone { center = new Vector3(180, 0, 180), radius = 50f, tier = LootTier.Uncommon, weaponBias = AmmoType.RifleAmmo },
            new LootZone { center = new Vector3(-120, 0, 200), radius = 35f, tier = LootTier.Epic, weaponBias = AmmoType.EnergyAmmo },
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  RED SANDS — Desert canyon map
    // ═══════════════════════════════════════════════════════════

    private static List<POIData> GetRedSandsPOIs()
    {
        return new List<POIData>
        {
            new POIData { poiName = "Canyon Pass", position = new Vector3(0, 0, 150), radius = 60f, lootDensity = 0.8f, poiType = POIType.Town },
            new POIData { poiName = "Oasis Camp", position = new Vector3(-160, 0, 80), radius = 40f, lootDensity = 0.7f, poiType = POIType.Village },
            new POIData { poiName = "Scorpion Den", position = new Vector3(120, 0, -100), radius = 50f, lootDensity = 0.9f, poiType = POIType.Military },
            new POIData { poiName = "Dust Bowl", position = new Vector3(0, 0, 0), radius = 80f, lootDensity = 0.6f, poiType = POIType.HotDrop },
            new POIData { poiName = "Ancient Tomb", position = new Vector3(-80, 0, -180), radius = 35f, lootDensity = 1.0f, poiType = POIType.Landmark },
            new POIData { poiName = "Sand Bridge", position = new Vector3(200, 0, 100), radius = 30f, lootDensity = 0.5f, poiType = POIType.Coastal },
            new POIData { poiName = "Rust Town", position = new Vector3(-200, 0, -80), radius = 45f, lootDensity = 0.7f, poiType = POIType.Industrial },
            new POIData { poiName = "Mirage Station", position = new Vector3(100, 0, 200), radius = 40f, lootDensity = 0.8f, poiType = POIType.Research },
        };
    }

    private static List<RoadSegment> GetRedSandsRoads()
    {
        return new List<RoadSegment>
        {
            new RoadSegment { startPoint = new Vector3(0, 0.05f, -250), endPoint = new Vector3(0, 0.05f, 250), width = 7f, roadType = RoadType.Highway },
            new RoadSegment { startPoint = new Vector3(-250, 0.05f, 0), endPoint = new Vector3(250, 0.05f, 0), width = 7f, roadType = RoadType.Highway },
            new RoadSegment { startPoint = new Vector3(-160, 0.05f, 80), endPoint = new Vector3(0, 0.05f, 0), width = 4f, roadType = RoadType.Dirt },
            new RoadSegment { startPoint = new Vector3(120, 0.05f, -100), endPoint = new Vector3(0, 0.05f, 150), width = 4f, roadType = RoadType.Paved },
        };
    }

    private static List<RiverSegment> GetRedSandsRivers()
    {
        return new List<RiverSegment>
        {
            new RiverSegment { startPoint = new Vector3(-250, -0.15f, 0), endPoint = new Vector3(0, -0.15f, -180), width = 6f, depth = 0.3f, flowSpeed = 0.15f },
        };
    }

    private static List<LootZone> GetRedSandsLootZones()
    {
        return new List<LootZone>
        {
            new LootZone { center = Vector3.zero, radius = 80f, tier = LootTier.Epic, weaponBias = AmmoType.RifleAmmo },
            new LootZone { center = new Vector3(120, 0, -100), radius = 50f, tier = LootTier.Legendary, weaponBias = AmmoType.SniperAmmo },
            new LootZone { center = new Vector3(-80, 0, -180), radius = 35f, tier = LootTier.Legendary, weaponBias = AmmoType.SniperAmmo },
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  IRON GORGE — Industrial wasteland map
    // ═══════════════════════════════════════════════════════════

    private static List<POIData> GetIronGorgePOIs()
    {
        return new List<POIData>
        {
            new POIData { poiName = "Forge Central", position = new Vector3(0, 0, 0), radius = 70f, lootDensity = 1.0f, poiType = POIType.HotDrop },
            new POIData { poiName = "Toxic Marsh", position = new Vector3(-150, 0, 100), radius = 50f, lootDensity = 0.6f, poiType = POIType.Coastal },
            new POIData { poiName = "Crusher Plant", position = new Vector3(180, 0, -60), radius = 55f, lootDensity = 0.85f, poiType = POIType.Industrial },
            new POIData { poiName = "Rust Ridge", position = new Vector3(-80, 0, -170), radius = 45f, lootDensity = 0.7f, poiType = POIType.Military },
            new POIData { poiName = "Smelter Yard", position = new Vector3(100, 0, 180), radius = 40f, lootDensity = 0.8f, poiType = POIType.Industrial },
            new POIData { poiName = "Bunker Alpha", position = new Vector3(-200, 0, -50), radius = 35f, lootDensity = 0.95f, poiType = POIType.Military },
            new POIData { poiName = "Scrap Town", position = new Vector3(50, 0, -120), radius = 50f, lootDensity = 0.65f, poiType = POIType.Village },
            new POIData { poiName = "Chimney Peak", position = new Vector3(-50, 0, 180), radius = 30f, lootDensity = 0.75f, poiType = POIType.Landmark },
        };
    }

    private static List<RoadSegment> GetIronGorgeRoads()
    {
        return new List<RoadSegment>
        {
            new RoadSegment { startPoint = new Vector3(0, 0.05f, -250), endPoint = new Vector3(0, 0.05f, 250), width = 8f, roadType = RoadType.Highway },
            new RoadSegment { startPoint = new Vector3(-250, 0.05f, 0), endPoint = new Vector3(250, 0.05f, 0), width = 8f, roadType = RoadType.Highway },
            new RoadSegment { startPoint = new Vector3(180, 0.05f, -60), endPoint = new Vector3(0, 0.05f, 0), width = 5f, roadType = RoadType.Paved },
            new RoadSegment { startPoint = new Vector3(-200, 0.05f, -50), endPoint = new Vector3(-80, 0.05f, -170), width = 4f, roadType = RoadType.Dirt },
        };
    }

    private static List<RiverSegment> GetIronGorgeRivers()
    {
        return new List<RiverSegment>
        {
            new RiverSegment { startPoint = new Vector3(-250, -0.15f, 150), endPoint = new Vector3(0, -0.15f, 0), width = 15f, depth = 0.8f, flowSpeed = 0.5f },
            new RiverSegment { startPoint = new Vector3(0, -0.15f, 0), endPoint = new Vector3(250, -0.15f, -150), width = 12f, depth = 0.6f, flowSpeed = 0.4f },
        };
    }

    private static List<LootZone> GetIronGorgeLootZones()
    {
        return new List<LootZone>
        {
            new LootZone { center = Vector3.zero, radius = 70f, tier = LootTier.Legendary, weaponBias = AmmoType.RifleAmmo },
            new LootZone { center = new Vector3(-200, 0, -50), radius = 35f, tier = LootTier.Legendary, weaponBias = AmmoType.SniperAmmo },
            new LootZone { center = new Vector3(180, 0, -60), radius = 55f, tier = LootTier.Epic, weaponBias = AmmoType.EnergyAmmo },
        };
    }

    /// <summary>Get all map names available.</summary>
    public static List<string> GetAllMapNames()
    {
        return new List<string> { "IslaVerde", "RedSands", "IronGorge" };
    }
}

// ═══════════════════════════════════════════════════════════
//  DATA CLASSES
// ═══════════════════════════════════════════════════════════

public enum POIType { Town, Village, Military, Research, Industrial, Coastal, Forest, Landmark, HotDrop }
public enum RoadType { Highway, Paved, Dirt, Bridge }
public enum LootTier { Common, Uncommon, Rare, Epic, Legendary }

[System.Serializable]
public class POIData
{
    public string poiName;
    public Vector3 position;
    public float radius = 50f;
    public float lootDensity = 0.5f;
    public POIType poiType;
    public string description;
}

[System.Serializable]
public class RoadSegment
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public float width = 4f;
    public RoadType roadType;
}

[System.Serializable]
public class RiverSegment
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public float width = 10f;
    public float depth = 0.5f;
    public float flowSpeed = 0.3f;
}

[System.Serializable]
public class LootZone
{
    public Vector3 center;
    public float radius = 50f;
    public LootTier tier = LootTier.Common;
    public AmmoType weaponBias = AmmoType.RifleAmmo;
}
