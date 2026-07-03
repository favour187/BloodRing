using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UNIQUE FEATURE: Faction War System.
/// Players belong to one of 3 factions. Killing enemies of rival factions earns faction points.
/// The leading faction earns a map-wide buff. Factions compete for control zones on the map.
/// Unique to BloodRing Apex.
/// </summary>
public enum Faction
{
    IronWolves,     // Red — Combat bonuses
    ShadowPanthers, // Purple — Stealth bonuses
    StormHawks      // Blue — Speed bonuses
}

[System.Serializable]
public class FactionInfo
{
    public Faction faction;
    public string name;
    public Color color;
    public string buffName;
    public string buffDescription;
    public int totalPoints;

    public static FactionInfo Get(Faction f)
    {
        FactionInfo info = new FactionInfo();
        info.faction = f;
        info.totalPoints = 0;
        switch (f)
        {
            case Faction.IronWolves:
                info.name = "Iron Wolves"; info.color = new Color(0.9f, 0.2f, 0.1f);
                info.buffName = "Wolf's Fury"; info.buffDescription = "+10% damage for all Iron Wolves"; break;
            case Faction.ShadowPanthers:
                info.name = "Shadow Panthers"; info.color = new Color(0.6f, 0.2f, 0.8f);
                info.buffName = "Panther's Veil"; info.buffDescription = "-20% detection range vs Shadow Panthers"; break;
            case Faction.StormHawks:
                info.name = "Storm Hawks"; info.color = new Color(0.2f, 0.5f, 0.9f);
                info.buffName = "Hawk's Swiftness"; info.buffDescription = "+12% move speed for all Storm Hawks"; break;
        }
        return info;
    }
}

public class FactionWarSystem : MonoBehaviour
{
    public static FactionWarSystem Instance;

    private Dictionary<Faction, FactionInfo> factions = new Dictionary<Faction, FactionInfo>();
    private Faction playerFaction = Faction.IronWolves;
    private Faction leadingFaction = Faction.IronWolves;
    private List<FactionControlZone> controlZones = new List<FactionControlZone>();
    private float updateTimer = 0f;

    private void Awake() { Instance = this; }

    public void InitializeFactions()
    {
        factions[Faction.IronWolves] = FactionInfo.Get(Faction.IronWolves);
        factions[Faction.ShadowPanthers] = FactionInfo.Get(Faction.ShadowPanthers);
        factions[Faction.StormHawks] = FactionInfo.Get(Faction.StormHawks);

        // Assign player to a faction based on character choice
        string charChoice = PlayerPrefs.GetString("SelectedCharacter", "DJNeon");
        if (charChoice == "Ronin" || charChoice == "Bolt") playerFaction = Faction.IronWolves;
        else if (charChoice == "Mirage" || charChoice == "Pulse") playerFaction = Faction.ShadowPanthers;
        else playerFaction = Faction.StormHawks;

        PlayerPrefs.SetString("PlayerFaction", playerFaction.ToString());

        // Spawn 3 control zones on the map
        SpawnControlZones();

        Debug.Log("[FactionWar] Player joined: " + factions[playerFaction].name);
    }

    private void SpawnControlZones()
    {
        Vector3[] zonePositions = {
            new Vector3(80, 0, 80),
            new Vector3(-80, 0, 80),
            new Vector3(0, 0, -100)
        };
        string[] zoneNames = { "Alpha Point", "Bravo Point", "Charlie Point" };

        for (int i = 0; i < zonePositions.Length; i++)
        {
            GameObject zoneGo = new GameObject("ControlZone_" + zoneNames[i]);
            zoneGo.transform.position = zonePositions[i];

            FactionControlZone zone = zoneGo.AddComponent<FactionControlZone>();
            zone.Initialize(zoneNames[i], 15f);
            controlZones.Add(zone);

            // Visual marker — large translucent cylinder
            GameObject marker = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
            marker.name = "ZoneMarker";
            marker.transform.SetParent(zoneGo.transform);
            marker.transform.localPosition = new Vector3(0, 5f, 0);
            marker.transform.localScale = new Vector3(30f, 5f, 30f);
            Destroy(marker.GetComponent<Collider>());
            Material mat = new Material(ProceduralArt.GetSafeShader("Standard"));
            mat.color = new Color(1f, 1f, 1f, 0.08f);
            mat.SetFloat("_Mode", 3); mat.SetInt("_ZWrite", 0); mat.renderQueue = 3000;
            marker.GetComponent<Renderer>().material = mat;
            marker.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Flag pole
            GameObject pole = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
            pole.name = "FlagPole";
            pole.transform.SetParent(zoneGo.transform);
            pole.transform.localPosition = new Vector3(0, 4f, 0);
            pole.transform.localScale = new Vector3(0.2f, 4f, 0.2f);
            Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().material.color = Color.gray;

            // Zone label
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(zoneGo.transform);
            labelGo.transform.localPosition = new Vector3(0, 10f, 0);
            TextMesh tm = labelGo.AddComponent<TextMesh>();
            tm.text = zoneNames[i];
            tm.fontSize = 48; tm.characterSize = 0.12f;
            tm.color = Color.white; tm.anchor = TextAnchor.MiddleCenter;

            // Minimap blip
            MinimapBlip blip = zoneGo.AddComponent<MinimapBlip>();
            blip.blipColor = Color.white;
        }
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= 5f) // Update every 5 seconds
        {
            updateTimer = 0f;
            DetermineLead();
        }
    }

    public void AddFactionPoints(Faction faction, int points)
    {
        if (factions.ContainsKey(faction))
            factions[faction].totalPoints += points;
    }

    public void OnKill(Faction killerFaction, Faction victimFaction)
    {
        if (killerFaction != victimFaction)
        {
            AddFactionPoints(killerFaction, 10);
            if (GameHUD.Instance != null)
                GameHUD.Instance.AddKillFeedEntry("FACTION", factions[killerFaction].name + " +10 pts!");
        }
    }

    private void DetermineLead()
    {
        int maxPoints = 0;
        Faction newLead = Faction.IronWolves;
        foreach (var kvp in factions)
        {
            if (kvp.Value.totalPoints > maxPoints) { maxPoints = kvp.Value.totalPoints; newLead = kvp.Key; }
        }
        if (newLead != leadingFaction)
        {
            leadingFaction = newLead;
            if (GameHUD.Instance != null)
                GameHUD.Instance.AddKillFeedEntry("FACTION WAR",
                    factions[leadingFaction].name + " takes the lead! " + factions[leadingFaction].buffName + " ACTIVE!");
        }
    }

    public Faction GetPlayerFaction() { return playerFaction; }
    public FactionInfo GetFactionInfo(Faction f) { return factions.ContainsKey(f) ? factions[f] : null; }
    public Faction GetLeadingFaction() { return leadingFaction; }

    public float GetDamageMultiplier(Faction f) { return (f == Faction.IronWolves && leadingFaction == Faction.IronWolves) ? 1.10f : 1f; }
    public float GetSpeedMultiplier(Faction f) { return (f == Faction.StormHawks && leadingFaction == Faction.StormHawks) ? 1.12f : 1f; }
    public float GetDetectionMultiplier(Faction f) { return (f == Faction.ShadowPanthers && leadingFaction == Faction.ShadowPanthers) ? 0.80f : 1f; }
}

public class FactionControlZone : MonoBehaviour
{
    public string zoneName;
    public Faction controllingFaction = Faction.IronWolves;
    public float captureProgress = 0f;
    public float captureRadius = 15f;

    private float captureSpeed = 5f; // seconds to capture
    private Renderer zoneMarkerRenderer;

    public void Initialize(string name, float radius)
    {
        zoneName = name;
        captureRadius = radius;

        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;
    }

    private void Update()
    {
        // Rotate label to face camera
        Transform label = transform.Find("Label");
        if (label != null && Camera.main != null)
        {
            label.LookAt(Camera.main.transform);
            label.Rotate(0, 180, 0);
        }

        // Update zone marker color
        if (zoneMarkerRenderer == null)
        {
            Transform marker = transform.Find("ZoneMarker");
            if (marker != null) zoneMarkerRenderer = marker.GetComponent<Renderer>();
        }
        if (zoneMarkerRenderer != null && FactionWarSystem.Instance != null)
        {
            FactionInfo info = FactionWarSystem.Instance.GetFactionInfo(controllingFaction);
            if (info != null)
            {
                Color c = info.color; c.a = 0.12f;
                zoneMarkerRenderer.material.color = c;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc.IsOwner && FactionWarSystem.Instance != null)
        {
            Faction pFaction = FactionWarSystem.Instance.GetPlayerFaction();
            if (pFaction != controllingFaction)
            {
                captureProgress += Time.deltaTime / captureSpeed;
                if (captureProgress >= 1f)
                {
                    controllingFaction = pFaction;
                    captureProgress = 0f;
                    FactionWarSystem.Instance.AddFactionPoints(pFaction, 50);
                    if (GameHUD.Instance != null)
                        GameHUD.Instance.AddKillFeedEntry("ZONE CAPTURED", zoneName + " captured by " +
                            FactionWarSystem.Instance.GetFactionInfo(pFaction).name + "! +50 pts");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc.IsOwner) captureProgress = 0f;
    }
}


