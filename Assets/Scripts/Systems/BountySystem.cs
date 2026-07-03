using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// UNIQUE FEATURE #3: Bounty Hunter System (Unique to BloodRing this!)
/// Players who get multiple kills earn a bounty on their head.
/// Other players can see bounty targets on their minimap (within range).
/// Killing a bounty target gives bonus XP, coins, and loot.
/// The higher the kill streak, the higher the bounty reward.
/// </summary>
public class BountySystem : MonoBehaviour
{
    public static BountySystem Instance;

    [System.Serializable]
    public class BountyTarget
    {
        public string playerName;
        public ulong clientId;
        public int kills;
        public int bountyTier; // 1=Low, 2=Medium, 3=High, 4=Legendary
        public float bountyCoins;
        public float bountyXP;
        public Vector3 lastKnownPosition;
        public float lastUpdateTime;

        public string GetTierName()
        {
            switch (bountyTier)
            {
                case 1: return "WANTED";
                case 2: return "HIGH VALUE";
                case 3: return "MOST WANTED";
                case 4: return "LEGENDARY BOUNTY";
                default: return "UNKNOWN";
            }
        }

        public Color GetTierColor()
        {
            switch (bountyTier)
            {
                case 1: return Color.yellow;
                case 2: return new Color(1f, 0.5f, 0f);
                case 3: return Color.red;
                case 4: return Color.magenta;
                default: return Color.white;
            }
        }
    }

    private Dictionary<ulong, BountyTarget> activeBounties = new Dictionary<ulong, BountyTarget>();
    private Dictionary<ulong, int> playerKillCounts = new Dictionary<ulong, int>();
    private int bountyThreshold = 3; // Kills needed to trigger a bounty
    private Dictionary<ulong, GameObject> bountyMarkers = new Dictionary<ulong, GameObject>();

    private void Awake() { Instance = this; }

    /// <summary>
    /// Called when a player or bot gets a kill. Updates their kill count and potentially triggers a bounty.
    /// </summary>
    public void RegisterKill(ulong killerClientId, string killerName)
    {
        if (!playerKillCounts.ContainsKey(killerClientId))
            playerKillCounts[killerClientId] = 0;

        playerKillCounts[killerClientId]++;
        int kills = playerKillCounts[killerClientId];

        // Check if bounty should be placed/upgraded
        if (kills >= bountyThreshold)
        {
            int tier = 1;
            float coins = 200f;
            float xp = 150f;

            if (kills >= 10) { tier = 4; coins = 1000f; xp = 800f; }
            else if (kills >= 7) { tier = 3; coins = 600f; xp = 500f; }
            else if (kills >= 5) { tier = 2; coins = 400f; xp = 300f; }

            BountyTarget bounty;
            if (activeBounties.ContainsKey(killerClientId))
            {
                bounty = activeBounties[killerClientId];
                bounty.kills = kills;
                bounty.bountyTier = tier;
                bounty.bountyCoins = coins;
                bounty.bountyXP = xp;
            }
            else
            {
                bounty = new BountyTarget
                {
                    playerName = killerName,
                    clientId = killerClientId,
                    kills = kills,
                    bountyTier = tier,
                    bountyCoins = coins,
                    bountyXP = xp,
                    lastKnownPosition = Vector3.zero,
                    lastUpdateTime = Time.time
                };
                activeBounties[killerClientId] = bounty;
            }

            // Announce bounty
            if (GameHUD.Instance != null)
            {
                string msg = "🎯 " + bounty.GetTierName() + ": " + killerName + " (" + kills + " kills) — Reward: " + coins + " coins!";
                GameHUD.Instance.AddKillFeedEntry("BOUNTY", msg);
            }

            UpdateBountyMarker(bounty);
        }
    }

    /// <summary>
    /// Called when a bounty target is killed. Returns the bounty reward.
    /// </summary>
    public BountyTarget ClaimBounty(ulong targetClientId, string hunterName)
    {
        if (!activeBounties.ContainsKey(targetClientId)) return null;

        BountyTarget bounty = activeBounties[targetClientId];
        activeBounties.Remove(targetClientId);
        playerKillCounts.Remove(targetClientId);

        // Remove marker
        if (bountyMarkers.ContainsKey(targetClientId))
        {
            Destroy(bountyMarkers[targetClientId]);
            bountyMarkers.Remove(targetClientId);
        }

        // Announce claim
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.AddKillFeedEntry("BOUNTY CLAIMED", hunterName + " claimed the bounty on " + bounty.playerName + "! +" + bounty.bountyCoins + " coins!");
        }

        return bounty;
    }

    /// <summary>
    /// Updates the position of a bounty target's world marker.
    /// Should be called periodically by the server.
    /// </summary>
    public void UpdateBountyPosition(ulong clientId, Vector3 position)
    {
        if (!activeBounties.ContainsKey(clientId)) return;
        activeBounties[clientId].lastKnownPosition = position;
        activeBounties[clientId].lastUpdateTime = Time.time;

        if (bountyMarkers.ContainsKey(clientId))
        {
            bountyMarkers[clientId].transform.position = new Vector3(position.x, 45f, position.z);
        }
    }

    private void UpdateBountyMarker(BountyTarget bounty)
    {
        if (bountyMarkers.ContainsKey(bounty.clientId))
        {
            // Update existing marker color
            Renderer r = bountyMarkers[bounty.clientId].GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = bounty.GetTierColor();
            return;
        }

        // Create bounty marker on minimap
        GameObject markerGo = new GameObject("BountyMarker_" + bounty.playerName);
        markerGo.transform.position = new Vector3(bounty.lastKnownPosition.x, 45f, bounty.lastKnownPosition.z);

        // Skull icon (represented as a distinctive quad)
        GameObject skull = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Quad.obj");
        skull.name = "SkullIcon";
        skull.transform.SetParent(markerGo.transform);
        skull.transform.localPosition = Vector3.zero;
        skull.transform.rotation = Quaternion.Euler(90, 0, 0);
        skull.transform.localScale = new Vector3(12f, 12f, 1f);
        Destroy(skull.GetComponent<Collider>());
        Material mat = new Material(ProceduralArt.GetSafeShader("Unlit/Color"));
        mat.color = bounty.GetTierColor();
        skull.GetComponent<Renderer>().material = mat;

        // Pulsing light
        Light light = markerGo.AddComponent<Light>();
        light.color = bounty.GetTierColor();
        light.intensity = 5f;
        light.range = 20f;

        bountyMarkers[bounty.clientId] = markerGo;
    }

    public List<BountyTarget> GetActiveBounties()
    {
        return new List<BountyTarget>(activeBounties.Values);
    }

    public bool HasBounty(ulong clientId)
    {
        return activeBounties.ContainsKey(clientId);
    }

    public int GetPlayerKills(ulong clientId)
    {
        return playerKillCounts.ContainsKey(clientId) ? playerKillCounts[clientId] : 0;
    }

    private void Update()
    {
        // Pulse bounty markers
        foreach (var kvp in bountyMarkers)
        {
            if (kvp.Value != null)
            {
                Light l = kvp.Value.GetComponent<Light>();
                if (l != null)
                {
                    float pulse = Mathf.Abs(Mathf.Sin(Time.time * 2f));
                    l.intensity = 3f + pulse * 5f;
                }
            }
        }
    }
}


