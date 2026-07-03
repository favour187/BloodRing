using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-authoritative vertical-slice battle royale loop:
/// Warmup -> DropPlane -> ActiveMatch -> FinalCircle -> Results.
/// Handles drop plane path, loot spawning, bot spawning, safe-zone phases,
/// win/loss condition, match result payload, reconnect tokens and anti-cheat signals.
/// Attach to GameScene or let SceneBootstrapper create it.
/// </summary>
public class ProductionBattleRoyaleLoop : NetworkBehaviour
{
    public static ProductionBattleRoyaleLoop Instance { get; private set; }

    public enum MatchPhase { Warmup, DropPlane, ActiveMatch, FinalCircle, Results }

    [Header("Match Rules")]
    public int targetPlayers = 50;
    public int botFillCount = 24;
    public float warmupSeconds = 12f;
    public float dropPlaneSeconds = 28f;
    public float resultSeconds = 10f;

    [Header("Island")]
    public Vector3 islandCenter = Vector3.zero;
    public float initialZoneRadius = 380f;
    public Transform dropPlaneVisual;
    public Vector3 dropStart = new Vector3(-420, 155, -420);
    public Vector3 dropEnd = new Vector3(420, 155, 420);

    public NetworkVariable<MatchPhase> Phase = new NetworkVariable<MatchPhase>(MatchPhase.Warmup);
    public NetworkVariable<float> MatchTime = new NetworkVariable<float>(0);
    public NetworkVariable<float> ZoneRadius = new NetworkVariable<float>(380);
    public NetworkVariable<Vector3> ZoneCenter = new NetworkVariable<Vector3>(Vector3.zero);
    public NetworkVariable<int> AliveCount = new NetworkVariable<int>(0);

    private readonly Dictionary<ulong, MatchPlayerState> players = new Dictionary<ulong, MatchPlayerState>();
    private readonly List<GameObject> spawnedLoot = new List<GameObject>();
    private readonly List<GameObject> spawnedBots = new List<GameObject>();
    private readonly List<AntiCheatSignal> antiCheatSignals = new List<AntiCheatSignal>();
    private bool loopStarted;

    [System.Serializable]
    public class MatchPlayerState
    {
        public ulong clientId;
        public string nickname;
        public bool alive = true;
        public int kills;
        public int assists;
        public float damage;
        public float survivalSeconds;
        public string reconnectToken;
        public Vector3 lastServerPosition;
    }

    [System.Serializable]
    public struct AntiCheatSignal
    {
        public ulong clientId;
        public string type;
        public float value;
        public float serverTime;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        if (!loopStarted) StartCoroutine(ServerMatchLoop());
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer || players.ContainsKey(clientId)) return;
        players[clientId] = new MatchPlayerState
        {
            clientId = clientId,
            nickname = NetworkController.Instance != null ? NetworkController.Instance.GetPlayerNickname(clientId) : "Player_" + clientId,
            reconnectToken = System.Guid.NewGuid().ToString("N"),
            alive = true
        };
        RecalculateAlive();
        SendReconnectTokenClientRpc(clientId, players[clientId].reconnectToken);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer || !players.ContainsKey(clientId)) return;
        // Keep state alive for reconnect window; bot/autopilot can be attached later.
        players[clientId].lastServerPosition = GetPlayerPosition(clientId);
    }

    private IEnumerator ServerMatchLoop()
    {
        loopStarted = true;
        ZoneCenter.Value = islandCenter;
        ZoneRadius.Value = initialZoneRadius;
        Phase.Value = MatchPhase.Warmup;
        MatchTime.Value = 0;

        SpawnIslandLoot();
        FillBots();

        yield return Countdown(warmupSeconds);

        Phase.Value = MatchPhase.DropPlane;
        SpawnDropPlane();
        NotifyPhaseClientRpc("DROP_PLANE", "Jump when ready. Loot fast. The ring is closing.");
        yield return Countdown(dropPlaneSeconds);

        Phase.Value = MatchPhase.ActiveMatch;
        NotifyPhaseClientRpc("MATCH_START", "Survive the Blood Ring.");
        yield return StartCoroutine(ZoneLoop());

        Phase.Value = MatchPhase.FinalCircle;
        NotifyPhaseClientRpc("FINAL_RING", "Final ring. Fight for victory.");
        while (AliveCount.Value > 1 && MatchTime.Value < 1500f)
        {
            TickSurvival();
            yield return new WaitForSeconds(1f);
            MatchTime.Value += 1f;
        }

        Phase.Value = MatchPhase.Results;
        BroadcastResults();
        yield return new WaitForSeconds(resultSeconds);
    }

    private IEnumerator Countdown(float seconds)
    {
        float t = 0;
        while (t < seconds)
        {
            t += 1f;
            MatchTime.Value += 1f;
            if (Phase.Value == MatchPhase.DropPlane && dropPlaneVisual != null)
            {
                float k = Mathf.Clamp01(t / seconds);
                dropPlaneVisual.position = Vector3.Lerp(dropStart, dropEnd, k);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator ZoneLoop()
    {
        float[] durations = { 90, 85, 75, 65, 55, 45 };
        float[] radii = { 300, 230, 165, 105, 62, 32 };
        for (int i = 0; i < durations.Length; i++)
        {
            Vector3 startCenter = ZoneCenter.Value;
            Vector3 nextCenter = islandCenter + new Vector3(Random.Range(-85, 85), 0, Random.Range(-85, 85)) * (1f - i * 0.12f);
            float startRadius = ZoneRadius.Value;
            float nextRadius = radii[i];
            float t = 0;
            NotifyPhaseClientRpc("ZONE_PHASE_" + (i + 1), "Safe zone shrinking.");
            while (t < durations[i] && AliveCount.Value > 1)
            {
                t += 1f;
                MatchTime.Value += 1f;
                float k = Mathf.SmoothStep(0, 1, t / durations[i]);
                ZoneCenter.Value = Vector3.Lerp(startCenter, nextCenter, k);
                ZoneRadius.Value = Mathf.Lerp(startRadius, nextRadius, k);
                ApplyZoneDamage(0.7f + i * 0.55f);
                TickSurvival();
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void SpawnDropPlane()
    {
        if (dropPlaneVisual == null)
        {
            GameObject loaded = Resources.Load<GameObject>("Art/Vehicles/BR_DropPlane");
            dropPlaneVisual = loaded != null ? Instantiate(loaded).transform : BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj").transform;
            dropPlaneVisual.name = "BR_DropPlane_Runtime";
            dropPlaneVisual.localScale = new Vector3(16, 3, 18);
        }
        dropPlaneVisual.position = dropStart;
        dropPlaneVisual.rotation = Quaternion.LookRotation(dropEnd - dropStart, Vector3.up);
    }

    private void SpawnIslandLoot()
    {
        if (!IsServer) return;
        for (int i = 0; i < 90; i++)
        {
            Vector2 p = Random.insideUnitCircle * 340f;
            GameObject loot = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Capsule.obj");
            loot.name = "BR_Loot_Server_" + i.ToString("000");
            loot.transform.position = new Vector3(p.x, 2f, p.y);
            loot.transform.localScale = Vector3.one * 0.55f;
            var item = loot.AddComponent<ProductionLootItem>();
            item.itemId = i % 5 == 0 ? "MedKit" : (i % 3 == 0 ? "Armor" : "BR_Rifle_01");
            item.rarity = Random.Range(1, 5);
            spawnedLoot.Add(loot);
        }
    }

    private void FillBots()
    {
        if (!IsServer) return;
        for (int i = 0; i < botFillCount; i++)
        {
            Vector2 p = Random.insideUnitCircle * 320f;
            GameObject bot = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Capsule.obj");
            bot.name = "BR_ServerBot_" + i.ToString("000");
            bot.transform.position = new Vector3(p.x, 2f, p.y);
            var ai = bot.AddComponent<ProductionBRBot>();
            ai.skillTier = Mathf.Clamp(1 + i / 6, 1, 5);
            ai.zoneProvider = this;
            spawnedBots.Add(bot);
        }
        AliveCount.Value += spawnedBots.Count;
    }

    private void ApplyZoneDamage(float dps)
    {
        foreach (var bot in spawnedBots)
        {
            if (bot == null || !bot.activeSelf) continue;
            if (Vector3.Distance(new Vector3(bot.transform.position.x, 0, bot.transform.position.z), new Vector3(ZoneCenter.Value.x, 0, ZoneCenter.Value.z)) > ZoneRadius.Value)
            {
                var ai = bot.GetComponent<ProductionBRBot>();
                ai.health -= dps;
                if (ai.health <= 0) { bot.SetActive(false); RecalculateAlive(); }
            }
        }
    }

    private void TickSurvival()
    {
        foreach (var kv in players)
        {
            if (kv.Value.alive) kv.Value.survivalSeconds += 1f;
        }
    }

    private void RecalculateAlive()
    {
        int alive = 0;
        foreach (var p in players.Values) if (p.alive) alive++;
        foreach (var b in spawnedBots) if (b != null && b.activeSelf) alive++;
        AliveCount.Value = alive;
    }

    private Vector3 GetPlayerPosition(ulong clientId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var c) && c.PlayerObject != null)
            return c.PlayerObject.transform.position;
        return Vector3.zero;
    }

    public bool IsOutsideZone(Vector3 position)
    {
        return Vector3.Distance(new Vector3(position.x, 0, position.z), new Vector3(ZoneCenter.Value.x, 0, ZoneCenter.Value.z)) > ZoneRadius.Value;
    }

    public void RegisterKill(ulong killer, ulong victim, float damage)
    {
        if (!IsServer) return;
        if (players.ContainsKey(killer)) { players[killer].kills++; players[killer].damage += damage; }
        if (players.ContainsKey(victim)) players[victim].alive = false;
        RecalculateAlive();
    }

    public void ReportAntiCheat(ulong clientId, string type, float value)
    {
        if (!IsServer) return;
        antiCheatSignals.Add(new AntiCheatSignal { clientId = clientId, type = type, value = value, serverTime = MatchTime.Value });
        if (antiCheatSignals.Count > 200) antiCheatSignals.RemoveAt(0);
    }

    private void BroadcastResults()
    {
        string winner = "BOT";
        foreach (var p in players.Values) if (p.alive) { winner = p.nickname; break; }
        NotifyPhaseClientRpc("RESULTS", "Winner: " + winner);
        foreach (var p in players.Values)
        {
            SendResultClientRpc(p.clientId, winner, p.kills, p.damage, p.survivalSeconds);
        }
    }

    [ClientRpc]
    private void NotifyPhaseClientRpc(string code, string message)
    {
        Debug.Log("[BR Loop] " + code + " - " + message);
        if (GameHUD.Instance != null) GameHUD.Instance.ShowToast(message);
    }

    [ClientRpc]
    private void SendResultClientRpc(ulong targetClientId, string winner, int kills, float damage, float survival)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != targetClientId) return;
        PlayerPrefs.SetString("LastMatchWinner", winner);
        PlayerPrefs.SetInt("LastMatchKills", kills);
        PlayerPrefs.SetFloat("LastMatchDamage", damage);
        PlayerPrefs.SetFloat("LastMatchSurvival", survival);
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameState.GameOver);
    }

    [ClientRpc]
    private void SendReconnectTokenClientRpc(ulong targetClientId, string token)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != targetClientId) return;
        PlayerPrefs.SetString("BR_ReconnectToken", token);
    }
}

public class ProductionLootItem : MonoBehaviour
{
    public string itemId;
    public int rarity = 1;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            gameObject.SetActive(false);
            if (GameHUD.Instance != null) GameHUD.Instance.ShowToast("Picked up " + itemId);
        }
    }
}

public class ProductionBRBot : MonoBehaviour
{
    public int skillTier = 1;
    public float health = 100f;
    public ProductionBattleRoyaleLoop zoneProvider;
    private Vector3 target;
    private float think;

    private void Start() { PickTarget(); }
    private void Update()
    {
        think -= Time.deltaTime;
        if (think <= 0 || Vector3.Distance(transform.position, target) < 5f) PickTarget();
        transform.position = Vector3.MoveTowards(transform.position, target, (2.6f + skillTier * .35f) * Time.deltaTime);
        if (zoneProvider != null && zoneProvider.IsOutsideZone(transform.position))
            target = zoneProvider.ZoneCenter.Value;
    }
    private void PickTarget()
    {
        think = Random.Range(2f, 5f);
        Vector2 p = Random.insideUnitCircle * Mathf.Max(20, zoneProvider != null ? zoneProvider.ZoneRadius.Value * .75f : 180f);
        Vector3 c = zoneProvider != null ? zoneProvider.ZoneCenter.Value : Vector3.zero;
        target = c + new Vector3(p.x, 0, p.y);
    }
}


