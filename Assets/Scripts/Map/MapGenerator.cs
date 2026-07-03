using UnityEngine;
using Unity.AI.Navigation;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Enhanced map generator using MapData ScriptableObjects.
/// Generates terrain, POIs, roads, rivers, buildings, interactive objects,
/// vehicles, ziplines, and boundary walls.
/// Supports 3 maps: IslaVerde, RedSands, IronGorge.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public NavMeshSurface navMeshSurface;
    private MapData currentMapData;

    public void GenerateMap(string mapChoice = "IslaVerde")
    {
        Debug.Log("Generating BloodRing Map: " + mapChoice);
        ProceduralArt.SetupSkybox();

        // Load map data
        currentMapData = MapData.GetDefaultMap(mapChoice);

        // Initialize POI system
        if (POISystem.Instance != null) POISystem.Instance.Initialize(currentMapData);

        // ── TERRAIN ──────────────────────────────────────────────
        GameObject terrain = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Plane.obj");
        terrain.name = "Terrain";
        terrain.transform.position = Vector3.zero;
        terrain.transform.localScale = new Vector3(50f, 1f, 50f);

        Material groundMat = ProceduralArt.GetMaterial("Mat_" + mapChoice, ProceduralArt.GenerateGroundTexture());
        groundMat.color = currentMapData.groundTint;
        groundMat.mainTextureScale = new Vector2(50, 50);
        terrain.GetComponent<Renderer>().material = groundMat;
        terrain.isStatic = true;
        terrain.layer = LayerMask.NameToLayer("Default");

        RenderSettings.ambientSkyColor = currentMapData.skyHorizon;

        // ── WATER PLANE ──────────────────────────────────────────
        GameObject water = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Plane.obj");
        water.name = "WaterPlane";
        water.transform.position = new Vector3(0, currentMapData.waterLevel, 0);
        water.transform.localScale = new Vector3(80f, 1f, 80f);
        Material waterMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        waterMat.color = currentMapData.waterTint;
        waterMat.SetFloat("_Mode", 3);
        waterMat.SetInt("_ZWrite", 0);
        waterMat.renderQueue = 3000;
        water.GetComponent<Renderer>().material = waterMat;
        Destroy(water.GetComponent<Collider>());
        BoxCollider wCol = water.AddComponent<BoxCollider>();
        wCol.isTrigger = true;
        wCol.size = new Vector3(10f, 1f, 10f);

        // ── NAVMESH ──────────────────────────────────────────────
        navMeshSurface = terrain.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.All;

        GameObject envContainer = new GameObject("Environment");
        Material buildingMat = ProceduralArt.GetMaterial("Mat_Building", ProceduralArt.GenerateBuildingTexture());
        buildingMat.mainTextureScale = new Vector2(2, 2);

        // ── TREES ────────────────────────────────────────────────
        for (int i = 0; i < currentMapData.treeCount; i++)
        {
            Vector3 pos = GetRandomMapPosition();
            GameObject tree = ProceduralArt.CreateTreeMesh();
            tree.name = "Tree_" + i;
            tree.transform.position = pos;
            tree.transform.SetParent(envContainer.transform);
            tree.isStatic = true;
        }

        // ── ROCKS ────────────────────────────────────────────────
        for (int i = 0; i < currentMapData.rockCount; i++)
        {
            Vector3 pos = GetRandomMapPosition();
            GameObject rock = ProceduralArt.CreateRockMesh();
            rock.name = "Rock_" + i;
            rock.transform.position = pos;
            rock.transform.SetParent(envContainer.transform);
            rock.isStatic = true;
            GameObject ledge = new GameObject("Ledge");
            ledge.transform.SetParent(rock.transform);
            ledge.transform.localPosition = new Vector3(0, 1.5f, 0);
            ledge.AddComponent<LedgeClimb>();
        }

        // ── BUILDINGS ────────────────────────────────────────────
        GameObject realBuildingPrefab = Resources.Load<GameObject>("Models/Building");
        for (int i = 0; i < currentMapData.buildingCount; i++)
        {
            Vector3 pos = GetRandomMapPosition();
            if (Vector3.Distance(pos, Vector3.zero) < 20f) pos += new Vector3(25f, 0, 25f);
            GameObject building = new GameObject("Building_" + i);
            building.transform.position = pos;
            building.transform.SetParent(envContainer.transform);
            building.isStatic = true;

            if (realBuildingPrefab != null)
            {
                GameObject realB = Object.Instantiate(realBuildingPrefab, building.transform);
                realB.name = "RealBuildingMesh";
                realB.transform.localPosition = Vector3.zero;
                realB.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
                foreach (Renderer r in realB.GetComponentsInChildren<Renderer>()) r.material = buildingMat;
                GameObject ledge = new GameObject("Ledge");
                ledge.transform.SetParent(building.transform);
                ledge.transform.localPosition = new Vector3(0, 6.2f, 0);
                ledge.AddComponent<LedgeClimb>();
            }
            else
            {
                float w = 10f; float h = 6f; float l = 10f;
                CreateWall(building.transform, new Vector3(0, h/2, l/2), new Vector3(w, h, 0.5f), buildingMat);
                CreateWall(building.transform, new Vector3(-w/2, h/2, 0), new Vector3(0.5f, h, l), buildingMat);
                CreateWall(building.transform, new Vector3(w/2, h/2, 0), new Vector3(0.5f, h, l), buildingMat);
                CreateWall(building.transform, new Vector3(-w/4 - 0.5f, h/2, -l/2), new Vector3(w/2 - 1f, h, 0.5f), buildingMat);
                CreateWall(building.transform, new Vector3(w/4 + 0.5f, h/2, -l/2), new Vector3(w/2 - 1f, h, 0.5f), buildingMat);
                CreateWall(building.transform, new Vector3(0, h, 0), new Vector3(w + 0.5f, 0.5f, l + 0.5f), buildingMat);
                GameObject ledge = new GameObject("Ledge");
                ledge.transform.SetParent(building.transform);
                ledge.transform.localPosition = new Vector3(0, h + 0.2f, 0);
                ledge.AddComponent<LedgeClimb>();
            }

            // Add interactive doors and windows to buildings
            AddInteractiveObjects(building.transform, buildingMat);
        }

        // ── POI MARKERS ──────────────────────────────────────────
        SpawnPOIMarkers(envContainer.transform);

        // ── ROAD NETWORK ─────────────────────────────────────────
        if (RoadNetwork.Instance != null)
            RoadNetwork.Instance.GenerateRoads(currentMapData, envContainer.transform);

        // ── RIVER SYSTEM ─────────────────────────────────────────
        if (RiverSystem.Instance != null)
            RiverSystem.Instance.GenerateRivers(currentMapData, envContainer.transform);

        // ── INTERACTIVE OBJECTS (scattered) ──────────────────────
        SpawnScatteredInteractiveObjects(envContainer.transform);

        // ── ZIPLINES ─────────────────────────────────────────────
        for (int i = 0; i < currentMapData.ziplineCount; i++)
        {
            Vector3 start = GetRandomMapPosition() + new Vector3(0, 10f, 0);
            Vector3 end = start + new Vector3(Random.Range(30, 60), -8f, Random.Range(30, 60));
            GameObject zipGo = new GameObject("Zipline_" + i);
            zipGo.transform.position = start;
            zipGo.transform.SetParent(envContainer.transform);
            Zipline z = zipGo.AddComponent<Zipline>();
            z.endPos = end;
        }

        // ── VEHICLES ─────────────────────────────────────────────
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            VehicleType[] vTypes = new VehicleType[] { VehicleType.Car, VehicleType.Truck, VehicleType.Motorbike };
            for (int i = 0; i < currentMapData.vehicleCount; i++)
            {
                Vector3 pos = GetRandomMapPosition();
                GameObject vGo = new GameObject("Vehicle_" + i);
                vGo.transform.position = pos + new Vector3(0, 0.5f, 0);
                vGo.transform.SetParent(envContainer.transform);
                NetworkObject netObj = vGo.AddComponent<NetworkObject>();
                Vehicle v = vGo.AddComponent<Vehicle>();
                v.vType = vTypes[Random.Range(0, vTypes.Length)];
                netObj.Spawn(true);
            }
        }

        // ── BOUNDARY WALLS ───────────────────────────────────────
        Material boundMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        boundMat.color = new Color(0.8f, 0.1f, 0.1f, 0.5f);
        boundMat.SetFloat("_Mode", 3);
        boundMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        boundMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        boundMat.SetInt("_ZWrite", 0);
        boundMat.EnableKeyword("_ALPHABLEND_ON");
        boundMat.renderQueue = 3000;
        float mapSize = currentMapData.mapRadius * 2f;
        float boundH = 50f; float boundThick = 2f;
        CreateBoundaryWall(new Vector3(0, boundH/2, mapSize/2), new Vector3(mapSize, boundH, boundThick), boundMat, envContainer.transform);
        CreateBoundaryWall(new Vector3(0, boundH/2, -mapSize/2), new Vector3(mapSize, boundH, boundThick), boundMat, envContainer.transform);
        CreateBoundaryWall(new Vector3(mapSize/2, boundH/2, 0), new Vector3(boundThick, boundH, mapSize), boundMat, envContainer.transform);
        CreateBoundaryWall(new Vector3(-mapSize/2, boundH/2, 0), new Vector3(boundThick, boundH, mapSize), boundMat, envContainer.transform);

        Debug.Log("Baking NavMesh at runtime...");
        navMeshSurface.BuildNavMesh();
        Debug.Log($"Map '{currentMapData.displayName}' generated: {currentMapData.pointsOfInterest.Count} POIs, {currentMapData.roads.Count} roads, {currentMapData.rivers.Count} rivers");
    }

    /// <summary>Spawn visual markers for POIs on the minimap.</summary>
    private void SpawnPOIMarkers(Transform parent)
    {
        foreach (POIData poi in currentMapData.pointsOfInterest)
        {
            GameObject marker = new GameObject("POI_" + poi.poiName);
            marker.transform.position = poi.position + Vector3.up * 0.5f;
            marker.transform.SetParent(parent);

            // POI visual — glowing ring on ground
            GameObject ring = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
            ring.transform.SetParent(marker.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(poi.radius * 2f, 0.05f, poi.radius * 2f);
            Destroy(ring.GetComponent<Collider>());

            Material ringMat = new Material(ProceduralArt.GetSafeShader("Standard"));
            Color poiColor = POISystem.GetPOIColor(poi.poiType);
            ringMat.color = new Color(poiColor.r, poiColor.g, poiColor.b, 0.15f);
            ringMat.SetFloat("_Mode", 3);
            ringMat.SetInt("_ZWrite", 0);
            ringMat.renderQueue = 3000;
            ring.GetComponent<Renderer>().material = ringMat;
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Minimap blip
            MinimapBlip blip = marker.AddComponent<MinimapBlip>();
            blip.blipColor = poiColor;
        }
    }

    /// <summary>Add interactive doors and windows to a building.</summary>
    private void AddInteractiveObjects(Transform buildingTransform, Material buildingMat)
    {
        // Front door
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            GameObject doorGo = new GameObject("Door");
            doorGo.transform.SetParent(buildingTransform);
            doorGo.transform.localPosition = new Vector3(0, 1.25f, 5.1f);
            doorGo.AddComponent<NetworkObject>();
            InteractiveObject door = doorGo.AddComponent<InteractiveObject>();
            door.objectType = InteractiveType.Door;
        }

        // Windows on sides
        for (int w = 0; w < 2; w++)
        {
            GameObject winGo = new GameObject("Window_" + w);
            winGo.transform.SetParent(buildingTransform);
            winGo.transform.localPosition = new Vector3(w == 0 ? -5.1f : 5.1f, 2f, 0);
            winGo.AddComponent<NetworkObject>();
            InteractiveObject win = winGo.AddComponent<InteractiveObject>();
            win.objectType = InteractiveType.Window;
        }
    }

    /// <summary>Spawn scattered interactive objects across the map.</summary>
    private void SpawnScatteredInteractiveObjects(Transform parent)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // Explosive barrels near buildings and compounds
        for (int i = 0; i < 15; i++)
        {
            Vector3 pos = GetRandomMapPosition();
            GameObject barrelGo = new GameObject("ExplosiveBarrel_" + i);
            barrelGo.transform.position = pos;
            barrelGo.transform.SetParent(parent);
            barrelGo.AddComponent<NetworkObject>();
            InteractiveObject barrel = barrelGo.AddComponent<InteractiveObject>();
            barrel.objectType = InteractiveType.ExplosiveBarrel;
        }

        // Loot crates scattered around
        for (int i = 0; i < 20; i++)
        {
            Vector3 pos = GetRandomMapPosition();
            GameObject crateGo = new GameObject("LootCrate_" + i);
            crateGo.transform.position = pos;
            crateGo.transform.SetParent(parent);
            crateGo.AddComponent<NetworkObject>();
            InteractiveObject crate = crateGo.AddComponent<InteractiveObject>();
            crate.objectType = InteractiveType.LootCrate;
        }

        // Health stations at POIs
        foreach (POIData poi in currentMapData.pointsOfInterest)
        {
            if (poi.poiType == POIType.Town || poi.poiType == POIType.Military)
            {
                Vector3 pos = poi.position + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
                GameObject healthGo = new GameObject("HealthStation_" + poi.poiName);
                healthGo.transform.position = pos;
                healthGo.transform.SetParent(parent);
                healthGo.AddComponent<NetworkObject>();
                InteractiveObject health = healthGo.AddComponent<InteractiveObject>();
                health.objectType = InteractiveType.HealthStation;
            }
        }

        // Ammo crates at military/research POIs
        foreach (POIData poi in currentMapData.pointsOfInterest)
        {
            if (poi.poiType == POIType.Military || poi.poiType == POIType.Research)
            {
                Vector3 pos = poi.position + new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f));
                GameObject ammoGo = new GameObject("AmmoCrate_" + poi.poiName);
                ammoGo.transform.position = pos;
                ammoGo.transform.SetParent(parent);
                ammoGo.AddComponent<NetworkObject>();
                InteractiveObject ammo = ammoGo.AddComponent<InteractiveObject>();
                ammo.objectType = InteractiveType.AmmoCrate;
            }
        }

        // Launch pads at landmarks
        foreach (POIData poi in currentMapData.pointsOfInterest)
        {
            if (poi.poiType == POIType.Landmark || poi.poiType == POIType.HotDrop)
            {
                GameObject padGo = new GameObject("LaunchPad_" + poi.poiName);
                padGo.transform.position = poi.position;
                padGo.transform.SetParent(parent);
                padGo.AddComponent<NetworkObject>();
                InteractiveObject pad = padGo.AddComponent<InteractiveObject>();
                pad.objectType = InteractiveType.LaunchPad;
            }
        }
    }

    private Vector3 GetRandomMapPosition()
    {
        float radius = currentMapData != null ? currentMapData.mapRadius * 0.9f : 220f;
        float x = Random.Range(-radius, radius);
        float z = Random.Range(-radius, radius);
        return new Vector3(x, 0, z);
    }

    private void CreateWall(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject wall = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = mat;
        wall.isStatic = true;
        wall.layer = LayerMask.NameToLayer("Default");
    }

    private void CreateBoundaryWall(Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject wall = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        wall.name = "BoundaryWall";
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.transform.SetParent(parent);
        wall.GetComponent<Renderer>().material = mat;
        wall.isStatic = true;
    }
}


