using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Runtime mobile/3D upgrade layer for the BloodRing Apex Royale project.
/// It preserves all existing scenes and scripts, then procedurally adds 3D splash,
/// loading, lobby, menu, character, inventory/shop presentation, mobile UX, safe areas,
/// and mid-range Android performance settings at runtime.
/// </summary>
public static class BRMobileRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyMobilePerformanceDefaults();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForFirstScene()
    {
        EnsureSceneSystems(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneSystems(scene);
    }

    private static void EnsureSceneSystems(Scene scene)
    {
        EnsureEventSystem();

        if (Object.FindObjectOfType<BRAndroidBackButton>() == null)
        {
            GameObject back = new GameObject("[BR Android Back Button]");
            Object.DontDestroyOnLoad(back);
            back.AddComponent<BRAndroidBackButton>();
        }

        if (Object.FindObjectOfType<BR3DSceneDirector>() == null)
        {
            GameObject director = new GameObject("[BR 3D Scene Director]");
            director.AddComponent<BR3DSceneDirector>();
        }

        BRMobileSafeArea.ApplyToAllRootCanvases();
    }

    public static void ApplyMobilePerformanceDefaults()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        QualitySettings.vSyncCount = 0;
        QualitySettings.lodBias = 0.75f;
        QualitySettings.maximumLODLevel = 0;
        QualitySettings.shadowDistance = 45f;
        QualitySettings.shadowCascades = 1;
        QualitySettings.pixelLightCount = 1;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.billboardsFaceCameraPosition = true;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}

/// <summary>Builds scene-specific 3D content without deleting user-created scene objects.</summary>
public class BR3DSceneDirector : MonoBehaviour
{
    private Camera cam;
    private GameObject root;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private Material matDark;
    private Material matGold;
    private Material matCyan;
    private Material matOrange;
    private Material matGreen;

    private void Start()
    {
        root = new GameObject("BR_Procedural_3D_Content");
        spawned.Add(root);
        PrepareMaterials();
        PrepareCamera();
        PrepareLighting();
        BuildForScene(SceneManager.GetActiveScene().name);
        BRMobileSafeArea.ApplyToAllRootCanvases();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }
    }

    private void PrepareMaterials()
    {
        matDark = MakeMat("BR_DarkMobile", new Color(0.04f, 0.045f, 0.06f));
        matGold = MakeMat("BR_GoldMobile", new Color(1f, 0.72f, 0.18f));
        matCyan = MakeMat("BR_CyanMobile", new Color(0.06f, 0.85f, 1f));
        matOrange = MakeMat("BR_OrangeMobile", new Color(1f, 0.38f, 0.06f));
        matGreen = MakeMat("BR_GreenMobile", new Color(0.2f, 0.85f, 0.32f));
    }

    private Material MakeMat(string name, Color color)
    {
        Shader shader = ProceduralArt.GetSafeShader("Mobile/Diffuse");
        Material m = new Material(shader);
        m.name = name;
        m.color = color;
        return m;
    }

    private void PrepareCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 54f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 350f;
        if (cam.GetComponent<PhysicsRaycaster>() == null) cam.gameObject.AddComponent<PhysicsRaycaster>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.007f;
        RenderSettings.fogColor = new Color(0.045f, 0.05f, 0.07f);
    }

    private void PrepareLighting()
    {
        GameObject sun = new GameObject("BR Mobile Key Light");
        sun.transform.SetParent(root.transform);
        Light l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.05f;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.55f;
        sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        GameObject fill = new GameObject("BR Cyan Fill Light");
        fill.transform.SetParent(root.transform);
        fill.transform.position = new Vector3(-4f, 3f, -3f);
        Light fl = fill.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.range = 12f;
        fl.intensity = 0.9f;
        fl.color = new Color(0.1f, 0.65f, 1f);
    }

    private void BuildForScene(string sceneName)
    {
        if (sceneName == "SplashScreen" || sceneName == "SplashLogo" || sceneName == "Splash") BuildSplashScene();
        else if (sceneName == "StartupInitialization") BuildStartupInitializationScene();
        else if (sceneName == "LoginScene" || sceneName == "Login") BuildLoginScene();
        else if (sceneName == "LoadingScene" || sceneName == "Loading") BuildPremiumLoadingScene();
        else if (sceneName == "MainMenu" || sceneName == "MainLobby" || sceneName == "Lobby") BuildMainLobbyPremiumScene();
        else if (sceneName == "EventsPage" || sceneName == "Events") BuildEventsScene();
        else if (sceneName == "StoreScene" || sceneName == "Store") BuildStorePremiumScene();
        else if (sceneName == "CharacterSelect" || sceneName == "CharacterPage" || sceneName == "Character") BuildCharacterScene();
        else if (sceneName == "InventoryScene" || sceneName == "Inventory") BuildInventoryPremiumScene();
        else if (sceneName == "SettingsScene" || sceneName == "Settings") BuildSettingsPremiumScene();
        else if (sceneName == "MatchmakingScene" || sceneName == "Matchmaking") BuildMatchmakingPremiumScene();
        else if (sceneName == "LobbyScene" || sceneName == "WaitingIsland") BuildWaitingIslandPremiumScene();
        else if (sceneName == "GameScene" || sceneName == "MainBattleRoyaleMap" || sceneName == "Gameplay") BuildGameEnvironmentScene();
        else if (sceneName == "TrainingGround") BuildTrainingGroundScene();
        else if (sceneName == "GameOver" || sceneName == "ResultVictoryScreen" || sceneName == "Results") BuildGameOverScene();
        else if (sceneName == "ClanSocial") BuildClanSocialScene();
        else if (sceneName == "ProfileScene") BuildProfilePremiumScene();
        else if (sceneName == "Aircraft" || sceneName == "AircraftScene") BuildAircraftPremiumScene();
        else if (sceneName == "Rankings" || sceneName == "RankingsScene") BuildRankingsPremiumScene();
        else if (sceneName == "ReconnectErrorScene") BuildReconnectErrorScene();
        else BuildLoadingLikeScene(sceneName);
    }

    private void SetCameraPose(Vector3 pos, Vector3 lookAt)
    {
        cam.transform.position = pos;
        cam.transform.LookAt(lookAt);
    }

    private void BuildSplashScene()
    {
        SetCameraPose(new Vector3(0f, 2.2f, -8f), new Vector3(0f, 1.2f, 0f));
        CreateHangarFloor(28f, 16f);
        GameObject logo = Create3DText("ACADEMY\nROYALE", 0.62f, TextAnchor.MiddleCenter, matGold);
        logo.name = "3D Splash Logo";
        logo.transform.SetParent(root.transform);
        logo.transform.position = new Vector3(0f, 2.25f, 0f);
        logo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        logo.AddComponent<BRSpinAndFloat>().Setup(18f, 0.08f, 1.2f);

        GameObject ring = CreateRing("Splash Energy Ring", 2.65f, 56, matCyan);
        ring.transform.SetParent(root.transform);
        ring.transform.position = new Vector3(0f, 1.95f, 0.18f);
        ring.AddComponent<BRSpinAndFloat>().Setup(-35f, 0.03f, 1f);

        CreateCrateRow(new Vector3(-4f, 0.25f, 2f), 4);
        CreateWorldCanvas("Splash3DCanvas", new Vector3(0f, 0.7f, 1.7f), Quaternion.Euler(18f, 0f, 0f), delegate(Canvas c)
        {
            CreateWorldButton(c.transform, "EnterBtn", "ENTER", new Vector2(0f, 0f), new Vector2(250f, 62f), delegate { LoadPremiumScene("StartupInitialization"); });
        });
    }

    private void BuildLoadingLikeScene(string sceneName)
    {
        SetCameraPose(new Vector3(0f, 2.2f, -7f), new Vector3(0f, 1.2f, 0f));
        CreateHangarFloor(24f, 12f);
        GameObject t = Create3DText("LOADING " + sceneName.ToUpper(), 0.32f, TextAnchor.MiddleCenter, matCyan);
        t.transform.SetParent(root.transform);
        t.transform.position = new Vector3(0f, 1.7f, 0f);
        t.AddComponent<BRSpinAndFloat>().Setup(8f, 0.08f, 1.1f);
    }

    private void BuildMainMenuScene()
    {
        SetCameraPose(new Vector3(-2.6f, 2.8f, -9.6f), new Vector3(0.8f, 1.5f, 0.2f));
        CreateHangarFloor(34f, 22f);
        CreateNeonGate(new Vector3(1.9f, 0f, 1.2f));
        GameObject soldier = CreateCharacterModel("LobbyHero", new Vector3(2.2f, 0f, 0.6f), Quaternion.Euler(0f, -25f, 0f), 1.15f);
        soldier.AddComponent<BRSpinAndFloat>().Setup(5f, 0.025f, 1f);
        CreateWeaponStand(new Vector3(-2.9f, 0.15f, 0.5f), "AK47");
        CreateWeaponStand(new Vector3(-1.9f, 0.15f, 1.15f), "SCAR");

        CreateWorldCanvas("MainMenu3DCanvas", new Vector3(-2.55f, 1.55f, 1.2f), Quaternion.Euler(0f, 22f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "BLOOD RING APEX", "Mobile 3D Battle Royale");
            CreateWorldButton(c.transform, "PlayBtn", "PLAY", new Vector2(0f, 72f), new Vector2(300f, 68f), delegate { GameManager.Instance.ChangeState(GameState.CharacterSelect); });
            CreateWorldButton(c.transform, "MatchBtn", "MATCHMAKING", new Vector2(0f, -4f), new Vector2(300f, 58f), delegate { GameManager.Instance.ChangeState(GameState.Lobby); });
            CreateWorldButton(c.transform, "InventoryBtn", "INVENTORY", new Vector2(-156f, -76f), new Vector2(145f, 52f), delegate { TogglePresentation("BR_InventoryPresentation", true); });
            CreateWorldButton(c.transform, "ShopBtn", "SHOP", new Vector2(156f, -76f), new Vector2(145f, 52f), delegate { TogglePresentation("BR_ShopPresentation", false); });
            CreateWorldButton(c.transform, "SettingsBtn", "SETTINGS", new Vector2(0f, -140f), new Vector2(300f, 48f), delegate { ToggleSettingsPanel(); });
        });

        BuildInventoryAndShopPresentations();
    }

    private void BuildCharacterScene()
    {
        SetCameraPose(new Vector3(0f, 2.3f, -8.5f), new Vector3(0f, 1.25f, 0f));
        CreateHangarFloor(30f, 18f);
        CreateCharacterModel("Agent_Axiom", new Vector3(-2.5f, 0f, 0.2f), Quaternion.Euler(0f, 18f, 0f), 0.92f);
        CreateCharacterModel("Agent_Pulse", new Vector3(0f, 0f, 0f), Quaternion.identity, 1.08f);
        CreateCharacterModel("Agent_Viper", new Vector3(2.5f, 0f, 0.2f), Quaternion.Euler(0f, -18f, 0f), 0.92f);
        CreateWorldCanvas("Character3DCanvas", new Vector3(0f, 2.55f, 1.45f), Quaternion.Euler(15f, 0f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "CHOOSE AGENT", "Swipe/tap a character, then deploy");
            CreateWorldButton(c.transform, "DeployBtn", "DEPLOY", new Vector2(0f, -82f), new Vector2(280f, 58f), delegate { GameManager.Instance.ChangeState(GameState.Lobby); });
            CreateWorldButton(c.transform, "BackBtn", "BACK", new Vector2(-210f, -82f), new Vector2(130f, 48f), delegate { GameManager.Instance.ChangeState(GameState.MainMenu); });
        });
    }

    private void BuildLobbyScene()
    {
        SetCameraPose(new Vector3(-1.3f, 2.6f, -9f), new Vector3(0.3f, 1.3f, 0.5f));
        CreateHangarFloor(36f, 20f);
        CreateDropShip(new Vector3(1.7f, 1.05f, 2.4f));
        for (int i = 0; i < 8; i++)
        {
            float x = -3.5f + i;
            CreateCharacterModel("LobbySlot" + i, new Vector3(x, 0f, -0.4f + Mathf.Abs(i - 3.5f) * 0.12f), Quaternion.Euler(0f, -8f + i * 2f, 0f), 0.68f);
        }
        CreateWorldCanvas("Lobby3DCanvas", new Vector3(0f, 2.4f, 1.2f), Quaternion.Euler(13f, 0f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "MATCHMAKING", "Filling squad with players and bots");
            CreateWorldButton(c.transform, "StartNowBtn", "START NOW", new Vector2(0f, -82f), new Vector2(280f, 58f), delegate { GameManager.Instance.ChangeState(GameState.Game); });
            CreateWorldButton(c.transform, "CancelBtn", "CANCEL", new Vector2(-225f, -82f), new Vector2(150f, 48f), delegate { GameManager.Instance.ChangeState(GameState.MainMenu); });
        });
    }

    private void BuildGameEnvironmentScene()
    {
        SetCameraPose(new Vector3(0f, 18f, -24f), new Vector3(0f, 0f, 0f));
        CreateIslandEnvironment();
        CreateWorldCanvas("InGame3DStartCanvas", new Vector3(0f, 3.6f, -4.2f), Quaternion.Euler(22f, 0f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "BATTLEFIELD READY", "Mobile controls are active");
            CreateWorldButton(c.transform, "ReturnLobbyBtn", "RETURN LOBBY", new Vector2(0f, -88f), new Vector2(260f, 52f), delegate { GameManager.Instance.ChangeState(GameState.MainMenu); });
        });

        BRMobileControlOverlay.Ensure();
        CreateRuntimePools();
    }

    private void BuildGameOverScene()
    {
        SetCameraPose(new Vector3(0f, 2.3f, -7.2f), new Vector3(0f, 1.25f, 0f));
        CreateHangarFloor(26f, 14f);
        CreateCharacterModel("Winner", new Vector3(0f, 0f, 0f), Quaternion.identity, 1.15f);
        CreateWorldCanvas("GameOver3DCanvas", new Vector3(0f, 2.3f, 1.3f), Quaternion.Euler(15f, 0f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "MATCH COMPLETE", "Rewards calculated locally and synced when online");
            CreateWorldButton(c.transform, "AgainBtn", "PLAY AGAIN", new Vector2(-150f, -82f), new Vector2(190f, 52f), delegate { GameManager.Instance.ChangeState(GameState.Lobby); });
            CreateWorldButton(c.transform, "HomeBtn", "HOME", new Vector2(150f, -82f), new Vector2(190f, 52f), delegate { GameManager.Instance.ChangeState(GameState.MainMenu); });
        });
    }


    private void LoadPremiumScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName)) SceneManager.LoadScene(sceneName);
        else if (sceneName == "MainLobby") GameManager.Instance.ChangeState(GameState.MainMenu);
        else if (sceneName == "CharacterPage") GameManager.Instance.ChangeState(GameState.CharacterSelect);
        else if (sceneName == "WaitingIsland") GameManager.Instance.ChangeState(GameState.Lobby);
        else if (sceneName == "MainBattleRoyaleMap") GameManager.Instance.ChangeState(GameState.Game);
        else if (sceneName == "ResultVictoryScreen") GameManager.Instance.ChangeState(GameState.GameOver);
        else GameManager.Instance.ChangeState(GameState.MainMenu);
    }

    private void BuildStartupInitializationScene()
    {
        SetCameraPose(new Vector3(0f, 2.2f, -8.2f), new Vector3(0f, 1.25f, 0f));
        CreateHangarFloor(30f, 18f);
        CreateServerCore(new Vector3(0f, 1.45f, 0.3f), matCyan, "INIT");
        CreateCrateRow(new Vector3(-3.4f, 0.25f, 1.9f), 5);
        CreateWorldCanvas("Startup3DCanvas", new Vector3(0f, 2.55f, 1.45f), Quaternion.Euler(15f, 0f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "STARTUP / INITIALIZATION", "Loading player data, graphics profile, services and resources");
            CreateStatusRows(c.transform, new string[] { "Assets mounted", "Android safe area ready", "Object pools warmed", "Reconnect handler ready" });
            CreateWorldButton(c.transform, "ContinueLogin", "CONTINUE", new Vector2(0f, -158f), new Vector2(260f, 52f), delegate { LoadPremiumScene("LoginScene"); });
        });
    }

    private void BuildLoginScene()
    {
        SetCameraPose(new Vector3(-1.2f, 2.3f, -8.8f), new Vector3(0.2f, 1.25f, 0.2f));
        CreateHangarFloor(30f, 18f);
        CreateCharacterModel("LoginHero", new Vector3(2.15f, 0f, 0.55f), Quaternion.Euler(0f, -22f, 0f), 1.05f);
        CreateServerCore(new Vector3(-2.1f, 1.1f, 0.7f), matGold, "AUTH");
        CreateWorldCanvas("Login3DCanvas", new Vector3(-1.15f, 2.05f, 1.55f), Quaternion.Euler(8f, 18f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "LOGIN", "Secure player access — guest mode works offline");
            CreateWorldButton(c.transform, "GuestLogin", "PLAY AS GUEST", new Vector2(0f, 20f), new Vector2(310f, 60f), delegate { LoadPremiumScene("LoadingScene"); });
            CreateWorldButton(c.transform, "RetryOnline", "ONLINE LOGIN", new Vector2(0f, -52f), new Vector2(310f, 52f), delegate { LoadPremiumScene("LoadingScene"); });
            CreateWorldButton(c.transform, "BackSplash", "BACK", new Vector2(-205f, -134f), new Vector2(130f, 46f), delegate { LoadPremiumScene("SplashLogo"); });
        });
    }

    private void BuildPremiumLoadingScene()
    {
        SetCameraPose(new Vector3(0f, 2.2f, -7.8f), new Vector3(0f, 1.2f, 0f));
        CreateHangarFloor(28f, 16f);
        GameObject ring = CreateRing("Premium 3D Loading Ring", 2.1f, 80, matOrange);
        ring.transform.SetParent(root.transform);
        ring.transform.position = new Vector3(0f, 1.65f, 0.1f);
        ring.AddComponent<BRSpinAndFloat>().Setup(42f, 0.05f, 1.2f);
        CreateServerCore(new Vector3(0f, 1.35f, 0f), matCyan, "LOAD");
        CreateWorldCanvas("Loading3DCanvas", new Vector3(0f, 0.72f, 1.6f), Quaternion.Euler(18f, 0f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "LOADING SCENE", "Preparing premium lobby, shop, profile and matchmaking modules");
            CreateWorldButton(c.transform, "EnterLobby", "ENTER LOBBY", new Vector2(0f, -92f), new Vector2(280f, 54f), delegate { LoadPremiumScene("MainLobby"); });
        });
    }

    private void BuildMainLobbyPremiumScene()
    {
        BuildMainMenuScene();
        CreateWorldCanvas("PremiumLobbyNavCanvas", new Vector3(2.6f, 2.1f, 2.0f), Quaternion.Euler(8f, -24f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "MAIN LOBBY", "Premium 3D mobile command hub");
            string[] names = { "EVENTS", "STORE", "CHARACTER", "INVENTORY", "PROFILE", "CLAN", "SETTINGS", "MATCH" };
            string[] scenes = { "EventsPage", "StoreScene", "CharacterPage", "InventoryScene", "ProfileScene", "ClanSocial", "SettingsScene", "MatchmakingScene" };
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                float x = (i % 2 == 0) ? -150f : 150f;
                float y = 48f - (i / 2) * 56f;
                CreateWorldButton(c.transform, names[i] + "Btn", names[i], new Vector2(x, y), new Vector2(230f, 44f), delegate { LoadPremiumScene(scenes[idx]); });
            }
            CreateWorldButton(c.transform, "TrainingBtn", "TRAINING", new Vector2(0f, -180f), new Vector2(260f, 44f), delegate { LoadPremiumScene("TrainingGround"); });
        });
    }

    private void BuildEventsScene()
    {
        BuildPremiumPageScene("EVENTS PAGE", "Limited-time missions, battle pass panels, daily rewards and tournament banners", matGold, "EVENT", new string[] { "Daily Login Reward", "Weekend Royale Cup", "Elite Pass Missions", "Return to Lobby" }, new string[] { "EventsPage", "MatchmakingScene", "EventsPage", "MainLobby" });
    }

    private void BuildStorePremiumScene()
    {
        SetCameraPose(new Vector3(-2.4f, 2.4f, -8.8f), new Vector3(0.5f, 1.1f, 0.8f));
        CreateHangarFloor(32f, 18f);
        string[] shopNames = { "SCAR", "AWM", "M1887", "P90", "Vector" };
        for (int i = 0; i < shopNames.Length; i++) CreateWeaponStand(new Vector3(-2.8f + i * 1.25f, 0.2f, 0.8f), shopNames[i]);
        BuildPremiumPageCanvas("STORE", "3D featured weapons and bundles using project resources", "SHOP", matGold, new string[] { "Buy Featured", "Preview Bundle", "Inventory", "Lobby" }, new string[] { "StoreScene", "StoreScene", "InventoryScene", "MainLobby" });
    }

    private void BuildInventoryPremiumScene()
    {
        SetCameraPose(new Vector3(0f, 2.4f, -8.5f), new Vector3(0f, 1.2f, 0.8f));
        CreateHangarFloor(32f, 18f);
        string[] inv = { "AK47", "UMP", "Kar98k", "Grenade", "Pistol", "Shotgun" };
        for (int i = 0; i < inv.Length; i++) CreateWeaponStand(new Vector3(-3.1f + i * 1.22f, 0.2f, 0.8f + (i % 2) * 0.35f), inv[i]);
        BuildPremiumPageCanvas("INVENTORY", "3D backpack, armory and loadout presentation", "BAG", matGreen, new string[] { "Equip Loadout", "Open Store", "Character", "Lobby" }, new string[] { "InventoryScene", "StoreScene", "CharacterPage", "MainLobby" });
    }

    private void BuildSettingsPremiumScene()
    {
        BuildPremiumPageScene("SETTINGS", "Graphics, audio, controls, sensitivity, privacy and account settings", matCyan, "CFG", new string[] { "60 FPS", "Controls", "Reconnect Test", "Lobby" }, new string[] { "SettingsScene", "SettingsScene", "ReconnectErrorScene", "MainLobby" });
    }

    private void BuildMatchmakingPremiumScene()
    {
        SetCameraPose(new Vector3(-1.4f, 2.45f, -8.8f), new Vector3(0.2f, 1.2f, 0.7f));
        CreateHangarFloor(34f, 20f);
        CreateDropShip(new Vector3(1.6f, 1.1f, 2.2f));
        CreateServerCore(new Vector3(-2.2f, 1.05f, 0.7f), matOrange, "QUEUE");
        CreateWorldCanvas("Matchmaking3DCanvas", new Vector3(-0.6f, 2.2f, 1.55f), Quaternion.Euler(9f, 10f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, "MATCHMAKING", "Select your battlefield map and enter session");
            CreateStatusRows(c.transform, new string[] { "Mode: Squad Clash & Classic BR", "Map Select: Isla Verde / Red Sands / Iron Gorge", "Region: Auto", "Server squad fill enabled" });
            CreateWorldButton(c.transform, "SelectIslaVerde", "MAP: ISLA VERDE", new Vector2(-170f, -80f), new Vector2(160f, 44f), delegate { PlayerPrefs.SetString("SelectedMap", "IslaVerde"); });
            CreateWorldButton(c.transform, "SelectRedSands", "MAP: RED SANDS", new Vector2(0f, -80f), new Vector2(160f, 44f), delegate { PlayerPrefs.SetString("SelectedMap", "RedSands"); });
            CreateWorldButton(c.transform, "SelectIronGorge", "MAP: IRON GORGE", new Vector2(170f, -80f), new Vector2(160f, 44f), delegate { PlayerPrefs.SetString("SelectedMap", "IronGorge"); });
            CreateWorldButton(c.transform, "StartQueue", "ENTER WAITING ISLAND", new Vector2(0f, -154f), new Vector2(330f, 52f), delegate { LoadPremiumScene("WaitingIsland"); });
            CreateWorldButton(c.transform, "CancelQueue", "CANCEL", new Vector2(-245f, -154f), new Vector2(130f, 46f), delegate { LoadPremiumScene("MainLobby"); });
        });
    }

    private void BuildWaitingIslandPremiumScene()
    {
        BuildLobbyScene();
        CreateWorldCanvas("WaitingIsland3DExtraCanvas", new Vector3(2.9f, 2.2f, 1.5f), Quaternion.Euler(10f, -24f, 0f), delegate(Canvas c)
        {
            string curMap = PlayerPrefs.GetString("SelectedMap", "IslaVerde");
            CreateHeader(c.transform, "WAITING ISLAND (" + curMap.ToUpper() + ")", "Warm up while players join session");
            CreateWorldButton(c.transform, "LaunchBR", "LAUNCH BATTLE (" + curMap.ToUpper() + ")", new Vector2(0f, -84f), new Vector2(280f, 54f), delegate { LoadPremiumScene("MainBattleRoyaleMap"); });
        });
    }

    private void BuildTrainingGroundScene()
    {
        SetCameraPose(new Vector3(0f, 10f, -18f), new Vector3(0f, 1f, 0f));
        CreateIslandEnvironment();
        for (int i = 0; i < 12; i++)
        {
            GameObject target = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
            target.name = "3D Training Target";
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(-12f + i * 2.2f, 1f, 3f + Mathf.Sin(i) * 4f);
            target.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
            target.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? matOrange : matCyan;
            EnsureLOD(target);
        }
        BRMobileControlOverlay.Ensure();
        BuildPremiumPageCanvas("TRAINING GROUND", "Practice movement, aiming, recoil and item use", "AIM", matCyan, new string[] { "Reset Targets", "Start Match", "Lobby" }, new string[] { "TrainingGround", "MatchmakingScene", "MainLobby" });
    }

    private void BuildClanSocialScene()
    {
        BuildPremiumPageScene("CLAN / SOCIAL", "Guild hall, friends, invites, chat and squad creation", matGreen, "CLAN", new string[] { "Create Squad", "Guild Board", "Profile", "Lobby" }, new string[] { "MatchmakingScene", "ClanSocial", "ProfileScene", "MainLobby" });
    }

    private string GetProfileBadge()
    {
        if (BackendAPI.Instance != null && BackendAPI.Instance.CurrentProfile != null)
        {
            ProfileData p = BackendAPI.Instance.CurrentProfile;
            if (p.level > 0) return "LV." + p.level.ToString();
        }
        return "LIVE";
    }

    private void BuildProfilePremiumScene()
    {
        SetCameraPose(new Vector3(-1.3f, 2.35f, -8.4f), new Vector3(0.3f, 1.15f, 0.4f));
        CreateHangarFloor(30f, 17f);
        CreateCharacterModel("ProfileHero", new Vector3(2.0f, 0f, 0.4f), Quaternion.Euler(0f, -20f, 0f), 1.08f);
        BuildPremiumPageCanvas("PROFILE", "Live stats, rank, badges, achievements and account summary", GetProfileBadge(), matGold, new string[] { "Achievements", "Clan", "Inventory", "Lobby" }, new string[] { "ProfileScene", "ClanSocial", "InventoryScene", "MainLobby" });
    }

    private void BuildReconnectErrorScene()
    {
        SetCameraPose(new Vector3(0f, 2.2f, -7.5f), new Vector3(0f, 1.1f, 0f));
        CreateHangarFloor(24f, 14f);
        CreateServerCore(new Vector3(0f, 1.3f, 0.2f), matOrange, "ERR");
        BuildPremiumPageCanvas("RECONNECT / ERROR", "Connection interrupted. Your session can retry or return safely.", "NET", matOrange, new string[] { "Reconnect", "Retry Login", "Lobby" }, new string[] { "LoadingScene", "LoginScene", "MainLobby" });
    }

    private void BuildAircraftPremiumScene()
    {
        SetCameraPose(new Vector3(0f, 15f, -20f), new Vector3(0f, 0f, 0f));
        CreateHangarFloor(80f, 80f);
        GameObject plane = CreateDropShip(new Vector3(-10f, 8f, 0f));
        plane.name = "DropPlane_Model";
        BuildPremiumPageCanvas("AIRCRAFT DROP", "Transport aircraft flying over 3D battle royale island terrain. Choose your trajectory and deploy parachute.", "FLY", matCyan, new string[] { "Eject Now", "Check Map", "Cabin View", "Lobby" }, new string[] { "GameScene", "GameScene", "WaitingIsland", "MainLobby" });
    }

    private void BuildRankingsPremiumScene()
    {
        SetCameraPose(new Vector3(0f, 2.4f, -8.5f), new Vector3(0f, 1.2f, 0f));
        CreateHangarFloor(32f, 18f);
        CreateCrateRow(new Vector3(-3f, 0f, 3f), 3);
        BuildPremiumPageCanvas("GLOBAL RANKINGS", "Top BloodRing Apex Champions, seasonal leaderboard, guild tier and PvP rankings.", "APEX", matGold, new string[] { "Global Top 100", "Friends Rank", "My Profile", "Lobby" }, new string[] { "RankingsScene", "ClanSocial", "ProfileScene", "MainLobby" });
    }

    private void BuildPremiumPageScene(string title, string subtitle, Material accent, string token, string[] buttonLabels, string[] sceneTargets)
    {
        SetCameraPose(new Vector3(-1.2f, 2.3f, -8.4f), new Vector3(0.25f, 1.15f, 0.4f));
        CreateHangarFloor(30f, 17f);
        CreateServerCore(new Vector3(1.8f, 1.15f, 0.65f), accent, token);
        CreateCrateRow(new Vector3(-3.5f, 0.25f, 1.7f), 4);
        BuildPremiumPageCanvas(title, subtitle, token, accent, buttonLabels, sceneTargets);
    }

    private void BuildPremiumPageCanvas(string title, string subtitle, string token, Material accent, string[] buttonLabels, string[] sceneTargets)
    {
        CreateWorldCanvas(title.Replace(" ", "") + "Canvas", new Vector3(-1.35f, 2.05f, 1.55f), Quaternion.Euler(8f, 17f, 0f), delegate(Canvas c)
        {
            CreateHeader(c.transform, title, subtitle);
            for (int i = 0; i < buttonLabels.Length && i < sceneTargets.Length; i++)
            {
                int idx = i;
                float x = (i % 2 == 0) ? -145f : 145f;
                float y = 35f - (i / 2) * 68f;
                CreateWorldButton(c.transform, buttonLabels[i].Replace(" ", "") + "Btn", buttonLabels[i], new Vector2(x, y), new Vector2(235f, 52f), delegate { LoadPremiumScene(sceneTargets[idx]); });
            }
            GameObject badge = Create3DText(token, 0.16f, TextAnchor.MiddleCenter, accent);
            badge.transform.SetParent(root.transform);
            badge.transform.position = new Vector3(-3.2f, 2.55f, 0.4f);
        });
    }

    private void CreateStatusRows(Transform parent, string[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            GameObject row = new GameObject("StatusRow" + i);
            row.transform.SetParent(parent, false);
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.025f, 0.035f, 0.74f);
            RectTransform rr = row.GetComponent<RectTransform>();
            rr.anchoredPosition = new Vector2(0f, 42f - i * 40f);
            rr.sizeDelta = new Vector2(500f, 30f);
            CreateUIText(row.transform, "Text", "✓  " + rows[i], 18, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(470f, 28f));
        }
    }

    private void CreateServerCore(Vector3 pos, Material accent, string token)
    {
        GameObject core = new GameObject("Premium 3D Core " + token);
        core.transform.SetParent(root.transform);
        core.transform.position = pos;
        GameObject cube = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        cube.transform.SetParent(core.transform);
        cube.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        cube.GetComponent<Renderer>().sharedMaterial = matDark;
        GameObject ring = CreateRing("Core Ring", 0.95f, 36, accent);
        ring.transform.SetParent(core.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.AddComponent<BRSpinAndFloat>().Setup(55f, 0.02f, 1.1f);
        GameObject label = Create3DText(token, 0.13f, TextAnchor.MiddleCenter, accent);
        label.transform.SetParent(core.transform);
        label.transform.localPosition = new Vector3(0f, 0f, -0.62f);
        core.AddComponent<BRSpinAndFloat>().Setup(12f, 0.05f, 0.9f);
    }

    private void CreateHangarFloor(float width, float depth)
    {
        GameObject floor = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        floor.name = "3D Mobile Hangar Floor";
        floor.transform.SetParent(root.transform);
        floor.transform.position = new Vector3(0f, -0.05f, 1f);
        floor.transform.localScale = new Vector3(width, 0.1f, depth);
        floor.GetComponent<Renderer>().sharedMaterial = matDark;
        AddStaticOptimisation(floor);

        for (int i = 0; i < 9; i++)
        {
            GameObject strip = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
            strip.name = "Neon Floor Strip";
            strip.transform.SetParent(root.transform);
            strip.transform.position = new Vector3(-width * 0.45f + i * width * 0.112f, 0.012f, 1f);
            strip.transform.localScale = new Vector3(0.035f, 0.015f, depth * 0.92f);
            strip.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? matCyan : matOrange;
            AddStaticOptimisation(strip);
        }
    }

    private void CreateNeonGate(Vector3 pos)
    {
        GameObject gate = new GameObject("3D Lobby Neon Gate");
        gate.transform.SetParent(root.transform);
        gate.transform.position = pos;
        for (int i = 0; i < 2; i++)
        {
            GameObject pillar = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
            pillar.transform.SetParent(gate.transform);
            pillar.transform.localPosition = new Vector3(i == 0 ? -1.6f : 1.6f, 1.3f, 0f);
            pillar.transform.localScale = new Vector3(0.18f, 2.6f, 0.18f);
            pillar.GetComponent<Renderer>().sharedMaterial = matCyan;
        }
        GameObject top = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        top.transform.SetParent(gate.transform);
        top.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        top.transform.localScale = new Vector3(3.5f, 0.18f, 0.18f);
        top.GetComponent<Renderer>().sharedMaterial = matOrange;
    }

    private GameObject CreateCharacterModel(string name, Vector3 position, Quaternion rotation, float scale)
    {
        GameObject prefab = Resources.Load<GameObject>("Models/Soldier");
        GameObject character;
        if (prefab != null) character = Instantiate(prefab);
        else character = ProceduralArt.CreateHumanoidMesh("Striker");
        character.name = name;
        character.transform.SetParent(root.transform);
        character.transform.position = position;
        character.transform.rotation = rotation;
        character.transform.localScale = Vector3.one * scale;
        EnsureLOD(character);
        return character;
    }

    private void CreateWeaponStand(Vector3 position, string weaponName)
    {
        GameObject stand = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
        stand.name = "3D Weapon Stand " + weaponName;
        stand.transform.SetParent(root.transform);
        stand.transform.position = position;
        stand.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
        stand.GetComponent<Renderer>().sharedMaterial = matGold;

        GameObject prefab = Resources.Load<GameObject>("Models/" + weaponName);
        GameObject weapon = prefab != null ? Instantiate(prefab) : ProceduralArt.CreateGunMesh(weaponName);
        weapon.name = "Presented_" + weaponName;
        weapon.transform.SetParent(root.transform);
        weapon.transform.position = position + new Vector3(0f, 0.48f, 0f);
        weapon.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        weapon.transform.localScale = Vector3.one * 1.25f;
        weapon.AddComponent<BRSpinAndFloat>().Setup(28f, 0.05f, 1.4f);
        EnsureLOD(weapon);
    }

    private void BuildInventoryAndShopPresentations()
    {
        GameObject inventory = new GameObject("BR_InventoryPresentation");
        inventory.transform.SetParent(root.transform);
        inventory.SetActive(false);
        for (int i = 0; i < 5; i++) CreateWeaponCard3D(inventory.transform, new Vector3(-2f + i, 1f, 2.6f), WeaponData.GetAllWeaponNames()[i]);

        GameObject shop = new GameObject("BR_ShopPresentation");
        shop.transform.SetParent(root.transform);
        shop.SetActive(false);
        string[] shopNames = { "SCAR", "AWM", "M1887", "P90", "Vector" };
        for (int i = 0; i < shopNames.Length; i++) CreateWeaponCard3D(shop.transform, new Vector3(-2f + i, 1f, 3.15f), shopNames[i]);
    }

    private void CreateWeaponCard3D(Transform parent, Vector3 pos, string weaponName)
    {
        GameObject back = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        back.name = "3D Card " + weaponName;
        back.transform.SetParent(parent);
        back.transform.position = pos;
        back.transform.localScale = new Vector3(0.8f, 1.1f, 0.04f);
        back.GetComponent<Renderer>().sharedMaterial = matDark;
        CreateWeaponStand(pos + new Vector3(0f, -0.42f, -0.12f), weaponName);
        GameObject label = Create3DText(weaponName, 0.12f, TextAnchor.MiddleCenter, matGold);
        label.transform.SetParent(parent);
        label.transform.position = pos + new Vector3(0f, 0.42f, -0.08f);
    }

    private void TogglePresentation(string targetName, bool inventory)
    {
        Transform inv = root.transform.Find("BR_InventoryPresentation");
        Transform shop = root.transform.Find("BR_ShopPresentation");
        if (inv != null) inv.gameObject.SetActive(targetName == inv.name && !inv.gameObject.activeSelf);
        if (shop != null) shop.gameObject.SetActive(targetName == shop.name && !shop.gameObject.activeSelf);
    }

    private void ToggleSettingsPanel()
    {
        Transform p = root.transform.Find("BR_Settings3DPanel");
        if (p != null) { p.gameObject.SetActive(!p.gameObject.activeSelf); return; }
        GameObject panel = new GameObject("BR_Settings3DPanel");
        panel.transform.SetParent(root.transform);
        CreateWorldCanvas("Settings3DCanvas", new Vector3(0f, 2.1f, 2.1f), Quaternion.Euler(10f, 0f, 0f), delegate(Canvas c)
        {
            c.transform.SetParent(panel.transform);
            CreateHeader(c.transform, "SETTINGS", "60 FPS, safe area, mobile shaders enabled");
            CreateWorldButton(c.transform, "CloseSettings", "CLOSE", new Vector2(0f, -90f), new Vector2(220f, 52f), delegate { panel.SetActive(false); });
        });
    }

    private GameObject Create3DText(string text, float size, TextAnchor anchor, Material material)
    {
        GameObject go = new GameObject("3DText");
        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 90;
        mesh.characterSize = size;
        mesh.anchor = anchor;
        mesh.alignment = TextAlignment.Center;
        mesh.color = material.color;
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = material;
        return go;
    }

    private GameObject CreateRing(string name, float radius, int pieces, Material material)
    {
        GameObject ring = new GameObject(name);
        for (int i = 0; i < pieces; i++)
        {
            float a = (Mathf.PI * 2f * i) / pieces;
            GameObject p = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
            p.transform.SetParent(ring.transform);
            p.transform.localPosition = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            p.transform.localRotation = Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg);
            p.transform.localScale = new Vector3(0.18f, 0.045f, 0.045f);
            p.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(p.GetComponent<Collider>());
        }
        return ring;
    }

    private void CreateWorldCanvas(string name, Vector3 pos, Quaternion rot, System.Action<Canvas> builder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root.transform);
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = Vector3.one * 0.004f;
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        go.AddComponent<GraphicRaycaster>();
        RectTransform r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(720f, 420f);
        if (builder != null) builder(canvas);
    }

    private void CreateHeader(Transform parent, string title, string subtitle)
    {
        GameObject panel = new GameObject("HeaderPanel");
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchoredPosition = new Vector2(0f, 120f);
        pr.sizeDelta = new Vector2(620f, 95f);
        CreateUIText(panel.transform, "Title", title, 38, FontStyle.Bold, matGold.color, new Vector2(0f, 16f), new Vector2(580f, 44f));
        CreateUIText(panel.transform, "Subtitle", subtitle, 18, FontStyle.Normal, Color.white, new Vector2(0f, -24f), new Vector2(580f, 28f));
    }

    private Button CreateWorldButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        bool isPrimary = label.Contains("PLAY") || label.Contains("START") || label.Contains("DEPLOY") || label.Contains("ENTER") || label.Contains("LAUNCH");
        Color top = isPrimary ? new Color(1f, 0.78f, 0.16f, 1f) : new Color(0.12f, 0.18f, 0.24f, 0.96f);
        Color bottom = isPrimary ? new Color(1f, 0.28f, 0.02f, 1f) : new Color(0.04f, 0.07f, 0.11f, 0.96f);
        Color border = isPrimary ? new Color(1f, 0.95f, 0.34f, 1f) : new Color(0.09f, 0.82f, 1f, 1f);

        GameObject shadow = new GameObject("DeepShadow");
        shadow.transform.SetParent(go.transform, false);
        Image shadowImg = shadow.AddComponent<Image>();
        shadowImg.sprite = BRPremiumButtonArt.GetButtonSprite(new Color(0f, 0f, 0f, 0.62f), new Color(0f, 0f, 0f, 0.42f), new Color(0f, 0f, 0f, 0.15f), 20);
        shadowImg.type = Image.Type.Sliced;
        shadowImg.raycastTarget = false;
        RectTransform sr = shadow.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
        sr.offsetMin = new Vector2(4f, -7f); sr.offsetMax = new Vector2(8f, -3f);

        GameObject glow = new GameObject("OuterGlow");
        glow.transform.SetParent(go.transform, false);
        Image glowImg = glow.AddComponent<Image>();
        glowImg.sprite = BRPremiumButtonArt.GetButtonSprite(new Color(border.r, border.g, border.b, 0.16f), new Color(border.r, border.g, border.b, 0.04f), new Color(border.r, border.g, border.b, 0.35f), 26);
        glowImg.type = Image.Type.Sliced;
        glowImg.raycastTarget = false;
        RectTransform gr = glow.GetComponent<RectTransform>();
        gr.anchorMin = Vector2.zero; gr.anchorMax = Vector2.one;
        gr.offsetMin = new Vector2(-10f, -10f); gr.offsetMax = new Vector2(10f, 10f);

        GameObject face = new GameObject("PremiumFace");
        face.transform.SetParent(go.transform, false);
        Image img = face.AddComponent<Image>();
        img.sprite = BRPremiumButtonArt.GetButtonSprite(top, bottom, border, 18);
        img.type = Image.Type.Sliced;
        RectTransform fr = face.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;

        GameObject shine = new GameObject("TopShine");
        shine.transform.SetParent(face.transform, false);
        Image shineImg = shine.AddComponent<Image>();
        shineImg.sprite = BRPremiumButtonArt.GetShineSprite();
        shineImg.type = Image.Type.Sliced;
        shineImg.color = new Color(1f, 1f, 1f, isPrimary ? 0.28f : 0.16f);
        shineImg.raycastTarget = false;
        RectTransform shr = shine.GetComponent<RectTransform>();
        shr.anchorMin = new Vector2(0.04f, 0.54f); shr.anchorMax = new Vector2(0.96f, 0.96f);
        shr.offsetMin = Vector2.zero; shr.offsetMax = Vector2.zero;

        GameObject leftCut = new GameObject("AngledAccentLeft");
        leftCut.transform.SetParent(face.transform, false);
        Image leftImg = leftCut.AddComponent<Image>();
        leftImg.color = border;
        leftImg.raycastTarget = false;
        RectTransform lr = leftCut.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0f); lr.anchorMax = new Vector2(0f, 1f);
        lr.anchoredPosition = new Vector2(10f, 0f); lr.sizeDelta = new Vector2(6f, -14f);
        lr.localRotation = Quaternion.Euler(0f, 0f, -14f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.06f;
        btn.colors = cb;

        BRAnimatedButton anim = go.AddComponent<BRAnimatedButton>();
        anim.SetGraphics(img, glowImg, shine.transform, isPrimary);

        CreateUIText(face.transform, "TextShadow", label, Mathf.RoundToInt(size.y * 0.42f), FontStyle.Bold, new Color(0f, 0f, 0f, 0.68f), new Vector2(2f, -2f), size);
        Text txt = CreateUIText(face.transform, "Text", label, Mathf.RoundToInt(size.y * 0.42f), FontStyle.Bold, Color.white, Vector2.zero, size);
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 12;
        txt.resizeTextMaxSize = Mathf.RoundToInt(size.y * 0.46f);
        return btn;
    }

    private Text CreateUIText(Transform parent, string name, string text, int size, FontStyle style, Color color, Vector2 pos, Vector2 rectSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = rectSize;
        return t;
    }

    private void CreateCrateRow(Vector3 start, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject c = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
            c.name = "3D Supply Crate";
            c.transform.SetParent(root.transform);
            c.transform.position = start + new Vector3(i * 0.9f, 0f, 0f);
            c.transform.localScale = new Vector3(0.75f, 0.5f, 0.75f);
            c.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? matGreen : matOrange;
            AddStaticOptimisation(c);
        }
    }

    private GameObject CreateDropShip(Vector3 pos)
    {
        GameObject ship = new GameObject("3D Drop Ship");
        ship.transform.SetParent(root.transform);
        ship.transform.position = pos;
        GameObject body = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        body.transform.SetParent(ship.transform);
        body.transform.localScale = new Vector3(4.2f, 0.55f, 1.1f);
        body.GetComponent<Renderer>().sharedMaterial = matDark;
        for (int i = 0; i < 2; i++)
        {
            GameObject wing = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
            wing.transform.SetParent(ship.transform);
            wing.transform.localPosition = new Vector3(0f, 0f, i == 0 ? -1f : 1f);
            wing.transform.localScale = new Vector3(3.1f, 0.12f, 1.1f);
            wing.GetComponent<Renderer>().sharedMaterial = matCyan;
        }
        ship.AddComponent<BRSpinAndFloat>().Setup(4f, 0.08f, 0.8f);
        return ship;
    }

    private void CreateIslandEnvironment()
    {
        Terrain terrain = null;
        GameObject terrainGo = new GameObject("3D Battle Royale Terrain");
        terrainGo.transform.SetParent(root.transform);
        TerrainData td = new TerrainData();
        td.heightmapResolution = 65;
        td.size = new Vector3(180f, 18f, 180f);
        float[,] h = new float[65, 65];
        for (int x = 0; x < 65; x++)
        {
            for (int y = 0; y < 65; y++)
            {
                float nx = (x - 32f) / 32f;
                float ny = (y - 32f) / 32f;
                float island = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                h[y, x] = island * (0.04f + Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.08f);
            }
        }
        td.SetHeights(0, 0, h);
        terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = td;
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = td;
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 16f;
        terrain.basemapDistance = 120f;
        terrainGo.transform.position = new Vector3(-90f, -0.12f, -90f);

        for (int i = 0; i < 80; i++)
        {
            Vector2 p = Random.insideUnitCircle * 70f;
            if (Random.value < 0.62f) CreateTreeOrRock(new Vector3(p.x, 0f, p.y), i % 3 == 0);
            else CreateBuildingCluster(new Vector3(p.x, 0f, p.y));
        }

        GameObject zone = CreateRing("3D Safe Zone Ring", 22f, 96, matCyan);
        zone.transform.SetParent(root.transform);
        zone.transform.position = new Vector3(0f, 0.18f, 0f);
        zone.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void CreateTreeOrRock(Vector3 pos, bool rock)
    {
        GameObject obj = rock ? ProceduralArt.CreateRockMesh() : ProceduralArt.CreateTreeMesh();
        obj.transform.SetParent(root.transform);
        obj.transform.position = pos;
        obj.transform.localScale = Vector3.one * Random.Range(0.45f, 0.9f);
        EnsureLOD(obj);
        AddStaticOptimisation(obj);
    }

    private void CreateBuildingCluster(Vector3 pos)
    {
        GameObject b = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        b.name = "3D Low Poly Building";
        b.transform.SetParent(root.transform);
        b.transform.position = pos + Vector3.up * Random.Range(0.8f, 1.6f);
        b.transform.localScale = new Vector3(Random.Range(2f, 5f), Random.Range(1.6f, 3.2f), Random.Range(2f, 5f));
        b.GetComponent<Renderer>().sharedMaterial = matDark;
        EnsureLOD(b);
        AddStaticOptimisation(b);
    }

    private void CreateRuntimePools()
    {
        GameObject pools = new GameObject("BR_ObjectPools_Projectiles_Effects_Loot");
        pools.transform.SetParent(root.transform);
        pools.AddComponent<BRObjectPoolHub>();
    }

    private void EnsureLOD(GameObject obj)
    {
        if (obj == null || obj.GetComponent<LODGroup>() != null) return;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;
        LODGroup lod = obj.AddComponent<LODGroup>();
        LOD[] lods = new LOD[2];
        lods[0] = new LOD(0.55f, renderers);
        lods[1] = new LOD(0.12f, renderers);
        lod.SetLODs(lods);
        lod.RecalculateBounds();
    }

    private void AddStaticOptimisation(GameObject obj)
    {
        obj.isStatic = true;
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }
}

public class BRSpinAndFloat : MonoBehaviour
{
    private float spinSpeed = 20f;
    private float floatAmount = 0.05f;
    private float floatSpeed = 1f;
    private Vector3 startPos;

    public void Setup(float spin, float amount, float speed)
    {
        spinSpeed = spin;
        floatAmount = amount;
        floatSpeed = speed;
    }

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmount);
    }
}

public static class BRPremiumButtonArt
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite GetButtonSprite(Color top, Color bottom, Color border, int radius)
    {
        string key = top.ToString() + bottom.ToString() + border.ToString() + radius;
        if (cache.ContainsKey(key)) return cache[key];
        Sprite s = BloodRing.Art.BloodRingArtLibrary.Button("Btn_Play_Large") ?? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        cache[key] = s;
        return s;
    }

    public static Sprite GetShineSprite()
    {
        string key = "shine";
        if (cache.ContainsKey(key)) return cache[key];
        Sprite s = BloodRing.Art.BloodRingArtLibrary.Button("Btn_Settings") ?? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        cache[key] = s;
        return s;
    }

    private static float DistanceToRoundedRect(float x, float y, float w, float h, float r)
    {
        float px = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
        float py = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
        float ox = Mathf.Max(px, 0f);
        float oy = Mathf.Max(py, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) - r;
    }
}

public class BRAnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 baseScale;
    private Vector3 targetScale;
    private Image faceImage;
    private Image glowImage;
    private Transform shine;
    private Color baseColor = Color.white;
    private Color glowBase = Color.white;
    private bool primary;
    private bool pressed;
    private float shineStartX;

    public void SetGraphics(Image face, Image glow, Transform shineTransform, bool isPrimary)
    {
        faceImage = face;
        glowImage = glow;
        shine = shineTransform;
        primary = isPrimary;
        if (faceImage != null) baseColor = faceImage.color;
        if (glowImage != null) glowBase = glowImage.color;
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
        if (faceImage == null) faceImage = GetComponent<Image>();
        if (faceImage != null) baseColor = faceImage.color;
    }

    private void Start()
    {
        if (shine != null) shineStartX = shine.localPosition.x;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 14f);
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * (primary ? 5.2f : 3.5f)) * 0.5f;

        if (faceImage != null)
        {
            float boost = primary ? Mathf.Lerp(0.04f, 0.15f, pulse) : Mathf.Lerp(0.02f, 0.07f, pulse);
            faceImage.color = Color.Lerp(faceImage.color, baseColor + new Color(boost, boost * 0.62f, boost * 0.16f, 0f), Time.unscaledDeltaTime * 5f);
        }
        if (glowImage != null)
        {
            Color g = glowBase;
            g.a = primary ? Mathf.Lerp(0.18f, 0.42f, pulse) : Mathf.Lerp(0.10f, 0.26f, pulse);
            glowImage.color = Color.Lerp(glowImage.color, g, Time.unscaledDeltaTime * 5f);
        }
        if (shine != null)
        {
            Vector3 p = shine.localPosition;
            p.x = shineStartX + Mathf.Sin(Time.unscaledTime * 1.7f) * 12f;
            shine.localPosition = p;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        targetScale = baseScale * 0.92f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        targetScale = baseScale * 1.06f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!pressed) targetScale = baseScale * 1.06f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pressed = false;
        targetScale = baseScale;
    }
}

public class BRAndroidBackButton : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (CloseTopOverlayPanel()) return;

        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu")
        {
            Application.Quit();
        }
        else if (scene == "GameScene" || scene == "LobbyScene" || scene == "CharacterSelect" || scene == "GameOver")
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
        else
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }

    private bool CloseTopOverlayPanel()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        for (int i = canvases.Length - 1; i >= 0; i--)
        {
            Transform[] children = canvases[i].GetComponentsInChildren<Transform>(true);
            for (int j = children.Length - 1; j >= 0; j--)
            {
                GameObject go = children[j].gameObject;
                if (!go.activeInHierarchy) continue;
                string n = go.name.ToLowerInvariant();
                if ((n.Contains("panel") || n.Contains("presentation")) && go != canvases[i].gameObject)
                {
                    go.SetActive(false);
                    return true;
                }
            }
        }
        return false;
    }
}

public static class BRMobileSafeArea
{
    public static void ApplyToAllRootCanvases()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode != RenderMode.ScreenSpaceOverlay) continue;
            RectTransform rect = canvases[i].GetComponent<RectTransform>();
            if (rect == null) continue;
            if (canvases[i].GetComponent<BRSafeAreaFitter>() == null) canvases[i].gameObject.AddComponent<BRSafeAreaFitter>();
        }
    }
}

public class BRSafeAreaFitter : MonoBehaviour
{
    private Rect lastSafeArea;
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea) Apply();
    }

    private void Apply()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (rect == null) return;
        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public class BRMobileControlOverlay : MonoBehaviour
{
    public static void Ensure()
    {
        if (Object.FindObjectOfType<BRMobileControlOverlay>() != null) return;
        GameObject go = new GameObject("BR Mobile Touch Controls");
        go.AddComponent<BRMobileControlOverlay>();
    }

    private void Start()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        gameObject.AddComponent<GraphicRaycaster>();

        CreatePad("MoveStick", new Vector2(135f, 135f), new Vector2(170f, 170f), new Color(1f, 1f, 1f, 0.12f));
        CreatePad("AimStick", new Vector2(-135f, 135f), new Vector2(170f, 170f), new Color(1f, 1f, 1f, 0.12f));
        CreateAction("Fire", new Vector2(-105f, 245f), new Vector2(94f, 94f), new Color(1f, 0.18f, 0.08f, 0.62f));
        CreateAction("Jump", new Vector2(-220f, 120f), new Vector2(76f, 76f), new Color(0.1f, 0.75f, 1f, 0.54f));
        CreateAction("Crouch", new Vector2(-320f, 80f), new Vector2(68f, 68f), new Color(1f, 0.72f, 0.08f, 0.48f));
        gameObject.AddComponent<BRSafeAreaFitter>();
    }

    private void CreatePad(string name, Vector2 anchored, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = name == "MoveStick" ? Vector2.zero : new Vector2(1f, 0f);
        r.anchorMax = r.anchorMin;
        r.anchoredPosition = anchored;
        r.sizeDelta = size;
    }

    private void CreateAction(string label, Vector2 anchored, Vector2 size, Color color)
    {
        GameObject go = new GameObject(label + "Button");
        go.transform.SetParent(transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<Button>();
        go.AddComponent<BRAnimatedButton>();
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1f, 0f);
        r.anchorMax = new Vector2(1f, 0f);
        r.anchoredPosition = anchored;
        r.sizeDelta = size;
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        Text t = textGo.AddComponent<Text>();
        t.text = label.ToUpperInvariant();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 16;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        RectTransform tr = textGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }
}

public class BRObjectPoolHub : MonoBehaviour
{
    private readonly Queue<GameObject> projectilePool = new Queue<GameObject>();
    private readonly Queue<GameObject> hitFxPool = new Queue<GameObject>();

    private void Awake()
    {
        WarmPool(projectilePool, "PooledProjectile", 24, PrimitiveType.Sphere, new Color(1f, 0.78f, 0.1f));
        WarmPool(hitFxPool, "PooledHitFX", 16, PrimitiveType.Cube, new Color(1f, 0.1f, 0.05f));
    }

    private void WarmPool(Queue<GameObject> pool, string name, int count, PrimitiveType type, Color color)
    {
        Material mat = new Material(ProceduralArt.GetSafeShader("Mobile/Diffuse"));
        mat.color = color;
        for (int i = 0; i < count; i++)
        {
            GameObject go = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D(type.ToString() + ".obj");
            go.name = name + "_" + i;
            go.transform.SetParent(transform);
            go.transform.localScale = Vector3.one * 0.12f;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public GameObject GetProjectile()
    {
        return GetFromPool(projectilePool);
    }

    public void ReleaseProjectile(GameObject go)
    {
        ReleaseToPool(projectilePool, go);
    }

    private GameObject GetFromPool(Queue<GameObject> pool)
    {
        if (pool.Count == 0) return null;
        GameObject go = pool.Dequeue();
        go.SetActive(true);
        return go;
    }

    private void ReleaseToPool(Queue<GameObject> pool, GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        pool.Enqueue(go);
    }
}


