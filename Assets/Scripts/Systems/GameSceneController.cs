using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameSceneController : MonoBehaviour
{
    private string[] botNames = new string[]
    {
        "Chinedu","Ngozi","Emeka","Fatima","Ade","Binta","Obi","Amaka","Kelechi","Yusuf",
        "Chinwe","Babajide","Chika","Dayo","Funke","Gbenga","Ibrahim","Nkechi","Oluwaseun","Simi",
        "Tunde","Uzoma","Chijioke","Ezinne","Idris","Kemi","Okonkwo","Titilayo","Abubakar","Adaora"
    };

    private void Start()
    {
        Debug.Log("Starting BloodRing Apex Online Battle Royale Match (v5.0 — Ultimate Edition)...");

        if (EventSystem.current == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        light.intensity = 1.2f;

        GameObject canvasGo = new GameObject("GameCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBattleMusic();

        // ── Core Systems ─────────────────────────────────────────────────────
        GameObject systemsGo = new GameObject("GameSystems");

        KillSystem killSys = systemsGo.AddComponent<KillSystem>();
        killSys.InitializeKillSystem();

        ZoneController zoneSys = systemsGo.AddComponent<ZoneController>();
        LootSpawner lootSys   = systemsGo.AddComponent<LootSpawner>();

        TouchControls touchSys = systemsGo.AddComponent<TouchControls>();
        touchSys.InitializeControls(canvasGo.transform);

        GameHUD hudSys = systemsGo.AddComponent<GameHUD>();

        // ── Battle-royale-grade Systems ──────────────────────────────────────────
        ThrowableSystem throwSys = systemsGo.AddComponent<ThrowableSystem>();
        ReviveSystem reviveSys   = systemsGo.AddComponent<ReviveSystem>();
        PingSystem pingSys       = systemsGo.AddComponent<PingSystem>();
        PetSystem petSys         = systemsGo.AddComponent<PetSystem>();

        // ── Unique Systems (Unique to BloodRing these) ────────────────────
        WeatherSystem weatherSys       = systemsGo.AddComponent<WeatherSystem>();
        TrapSystem trapSys             = systemsGo.AddComponent<TrapSystem>();
        BountySystem bountySys         = systemsGo.AddComponent<BountySystem>();
        TalentTreeSystem talentSys     = systemsGo.AddComponent<TalentTreeSystem>();
        ReplaySystem replaySys         = systemsGo.AddComponent<ReplaySystem>();
        DestructibleSystem destructSys = systemsGo.AddComponent<DestructibleSystem>();
        FactionWarSystem factionSys    = systemsGo.AddComponent<FactionWarSystem>();
        BarricadeSystem barricadeSys   = systemsGo.AddComponent<BarricadeSystem>();

        // ── Map ───────────────────────────────────────────────────────────────
        GameObject mapGo = new GameObject("MapGenerator");
        MapGenerator mapGen = mapGo.AddComponent<MapGenerator>();
        string selectedMap = PlayerPrefs.GetString("SelectedMap", "IslaVerde");
        mapGen.GenerateMap(selectedMap);

        // ── Initialize ────────────────────────────────────────────────────────
        zoneSys.InitializeZone();
        weatherSys.InitializeWeather();
        talentSys.InitializeTalentTree();
        factionSys.InitializeFactions();

        // ── Network-aware player/bot spawning ─────────────────────────────────
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        bool isClient = NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost;

        if (isServer)
        {
            lootSys.SpawnInitialLoot();
            int onlineCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            Transform localPlayerTransform = null;

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                string nickname = NetworkController.Instance != null
                    ? NetworkController.Instance.GetPlayerNickname(clientId)
                    : "Player_" + clientId;

                GameObject playerGo = new GameObject("Player_" + nickname);
                playerGo.tag = "Player";
                playerGo.transform.position = new Vector3(Random.Range(-50f, 50f), 1f, Random.Range(-50f, 50f));

                NetworkObject netObj     = playerGo.AddComponent<NetworkObject>();
                PlayerController pCtrl   = playerGo.AddComponent<PlayerController>();
                playerGo.AddComponent<PlayerReviveHandler>();
                playerGo.AddComponent<EmoteSystem>();

                netObj.SpawnWithOwnership(clientId, true);

                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    localPlayerTransform = playerGo.transform;
                    hudSys.InitializeHUD(canvasGo.transform, playerGo.transform);
                    SpawnPetForPlayer(petSys, playerGo.transform);

                    ParachuteDrop dropSys = systemsGo.AddComponent<ParachuteDrop>();
                    dropSys.InitializePlaneDropSequence(playerGo.transform);
                }
            }

            int botsToSpawn = Mathf.Max(0, 20 - onlineCount);
            SpawnAIBots(botsToSpawn, localPlayerTransform, systemsGo.transform);
        }
        else if (isClient)
        {
            StartCoroutine(WaitForLocalPlayerSpawn(hudSys, canvasGo.transform, systemsGo, petSys));
        }
        else
        {
            // Solo / offline
            lootSys.SpawnInitialLoot();
            GameObject playerGo = SpawnLocalPlayer();
            hudSys.InitializeHUD(canvasGo.transform, playerGo.transform);
            SpawnAIBots(19, playerGo.transform, systemsGo.transform);
            SpawnPetForPlayer(petSys, playerGo.transform);

            ParachuteDrop dropSys = systemsGo.AddComponent<ParachuteDrop>();
            dropSys.InitializePlaneDropSequence(playerGo.transform);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SpawnPetForPlayer(PetSystem petSys, Transform playerTransform)
    {
        string petChoice = PlayerPrefs.GetString("SelectedPet", "Shiba");
        PetType selectedPet = PetType.Shiba;
        System.Enum.TryParse(petChoice, out selectedPet);
        petSys.SpawnPet(selectedPet, playerTransform);
    }

    private IEnumerator WaitForLocalPlayerSpawn(GameHUD hudSys, Transform canvasTransform, GameObject systemsGo, PetSystem petSys)
    {
        PlayerController localPlayer = null;
        float timeout = 10f;
        while (localPlayer == null && timeout > 0f)
        {
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            foreach (PlayerController p in players)
                if (p.IsOwner) { localPlayer = p; break; }
            yield return null;
            timeout -= Time.deltaTime;
        }
        if (localPlayer != null)
        {
            hudSys.InitializeHUD(canvasTransform, localPlayer.transform);
            SpawnPetForPlayer(petSys, localPlayer.transform);
            ParachuteDrop dropSys = systemsGo.AddComponent<ParachuteDrop>();
            dropSys.InitializePlaneDropSequence(localPlayer.transform);
        }
        else
            Debug.LogWarning("[GameSceneController] Local player never spawned – HUD skipped.");
    }

    private GameObject SpawnLocalPlayer()
    {
        string nickname = PlayerPrefs.GetString("PlayerNickname", "Striker_Hero");
        GameObject playerGo = new GameObject("Player_" + nickname);
        playerGo.tag = "Player";
        playerGo.transform.position = new Vector3(Random.Range(-30f, 30f), 1f, Random.Range(-30f, 30f));
        playerGo.AddComponent<PlayerController>();
        playerGo.AddComponent<PlayerReviveHandler>();
        playerGo.AddComponent<EmoteSystem>();
        return playerGo;
    }

    private void SpawnAIBots(int count, Transform playerTransform, Transform parent)
    {
        if (count <= 0) return;
        Debug.Log("[GameSceneController] Spawning " + count + " AI bots...");

        List<string> names = new List<string>(botNames);
        for (int i = 0; i < names.Count; i++)
        { int r = Random.Range(i, names.Count); (names[i], names[r]) = (names[r], names[i]); }

        GameObject botsContainer = new GameObject("AIBotsContainer");
        botsContainer.transform.SetParent(parent);

        for (int i = 0; i < Mathf.Min(count, names.Count); i++)
        {
            Vector3 pos = new Vector3(Random.Range(-200f, 200f), 1f, Random.Range(-200f, 200f));
            if (playerTransform != null && Vector3.Distance(pos, playerTransform.position) < 30f)
                pos += new Vector3(50f, 0, 50f);

            GameObject botGo = new GameObject("Bot_" + names[i]);
            botGo.transform.position = pos;
            botGo.transform.SetParent(botsContainer.transform);

            AIBot bot = botGo.AddComponent<AIBot>();
            bot.InitializeBot(names[i], playerTransform);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkObject netObj = botGo.AddComponent<NetworkObject>();
                netObj.Spawn(true);
            }
        }
    }
}


