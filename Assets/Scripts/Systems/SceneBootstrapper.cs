using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// SceneBootstrapper — Auto-creates required controllers and UI for every scene.
/// This is the "glue" that makes empty scene files functional at runtime.
/// Runs automatically via [RuntimeInitializeOnLoadMethod].
/// 
/// Pattern: scenes are lightweight, systems bootstrap themselves at runtime.
/// </summary>
public class SceneBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[Bootstrapper] Scene bootstrapper initialized");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureFirstScene()
    {
        BootstrapScene(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootstrapScene(scene.name);
    }

    private static void BootstrapScene(string sceneName)
    {
        Debug.Log("[Bootstrapper] Bootstrapping: " + sceneName);

        // Ensure persistent singletons exist
        EnsureSingleton<GameManager>("[GameManager]");
        EnsureSingleton<AudioManager>("[AudioManager]");
        EnsureSingleton<BackendAPI>("[BackendAPI]");
        EnsureSingleton<LiveOpsManager>("[LiveOpsManager]");
        EnsureSingleton<StoreRotationManager>("[StoreRotationManager]");

        // Scene-specific bootstrapping
        switch (sceneName)
        {
            // ── Splash / Loading ──────────────────────────────────────
            case "SplashScreen":
            case "SplashLogo":
            case "Splash":
            case "StartupInitialization":
                EnsureController<SplashController>();
                SetupSplashUI(sceneName);
                break;

            case "LoadingScene":
                SetupLoadingUI();
                break;

            // ── Auth ──────────────────────────────────────────────────
            case "LoginScene":
                SetupLoginUI();
                break;

            // ── Main Menu ─────────────────────────────────────────────
            case "MainMenu":
                EnsureController<MainMenuController>();
                break;

            case "MainLobby":
            case "LobbyScene":
                EnsureController<LobbyController>();
                break;

            // ── Character / Store ─────────────────────────────────────
            case "CharacterSelect":
            case "CharacterPage":
                EnsureController<CharacterSelectController>();
                break;

            case "StoreScene":
                SetupStoreUI();
                break;

            case "InventoryScene":
                SetupInventoryUI();
                break;

            // ── Settings / Profile ────────────────────────────────────
            case "SettingsScene":
                SetupSettingsUI();
                break;

            case "ProfileScene":
                SetupProfileUI();
                break;

            // ── Rankings / Social ─────────────────────────────────────
            case "RankingsScene":
            case "Rankings":
                SetupRankingsUI();
                break;

            case "ClanSocial":
                SetupSocialUI();
                break;

            case "EventsPage":
                SetupEventsUI();
                break;

            // ── Match Flow ────────────────────────────────────────────
            case "MatchmakingScene":
                SetupMatchmakingUI();
                break;

            case "WaitingIsland":
                SetupWaitingIsland();
                break;

            case "AircraftScene":
            case "Aircraft":
                SetupAircraftScene();
                break;

            // ── Gameplay ──────────────────────────────────────────────
            case "MainBattleRoyaleMap":
            case "GameScene":
            case "Gameplay":
                EnsureController<GameSceneController>();
                SetupGameplayScene();
                break;

            case "TrainingGround":
                SetupTrainingGround();
                break;

            // ── Results ───────────────────────────────────────────────
            case "ResultVictoryScreen":
            case "Results":
            case "GameOver":
                EnsureController<GameOverController>();
                SetupGameOverUI(sceneName);
                break;

            // ── Error ─────────────────────────────────────────────────
            case "ReconnectErrorScene":
                SetupReconnectUI();
                break;

            default:
                Debug.LogWarning("[Bootstrapper] No bootstrap handler for scene: " + sceneName);
                break;
        }
    }

    // ── Singleton Helpers ─────────────────────────────────────────────

    private static void EnsureSingleton<T>() where T : MonoBehaviour
    {
        if (Object.FindObjectOfType<T>() == null)
        {
            GameObject go = new GameObject($"[{typeof(T).Name}]");
            go.AddComponent<T>();
            Object.DontDestroyOnLoad(go);
        }
    }

    private static void EnsureSingleton<T>(string name) where T : MonoBehaviour
    {
        if (Object.FindObjectOfType<T>() == null)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<T>();
            Object.DontDestroyOnLoad(go);
        }
    }

    private static void EnsureController<T>() where T : MonoBehaviour
    {
        if (Object.FindObjectOfType<T>() == null)
        {
            GameObject go = new GameObject($"[{typeof(T).Name}]");
            go.AddComponent<T>();
        }
    }

    // ── Scene UI Factories ────────────────────────────────────────────

    private static Canvas CreateCanvas(string name, int sortOrder = 0)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        CreateText(rt, "Label", label, 24, Color.white, Vector2.zero, sizeDelta);
        return btn;
    }

    // ── Splash / Loading ──────────────────────────────────────────────

    private static void SetupSplashUI(string sceneName)
    {
        Canvas canvas = CreateCanvas("SplashCanvas");
        CreateText(canvas.transform, "Title", "BLOOD RING", 72, new Color(0.85f, 0.1f, 0.05f), new Vector2(0, 100), new Vector2(600, 100));
        CreateText(canvas.transform, "Subtitle", "APEX ROYALE", 36, new Color(1f, 0.75f, 0.1f), new Vector2(0, 30), new Vector2(400, 50));
        CreateText(canvas.transform, "Version", "v5.0.0", 18, Color.gray, new Vector2(0, -200), new Vector2(200, 30));
    }

    private static void SetupLoadingUI()
    {
        Canvas canvas = CreateCanvas("LoadingCanvas");
        CreateText(canvas.transform, "LoadingTitle", "Loading...", 48, Color.white, new Vector2(0, 50), new Vector2(400, 80));

        // Progress bar background
        GameObject barBG = new GameObject("ProgressBarBG");
        barBG.transform.SetParent(canvas.transform, false);
        Image bgImg = barBG.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRT = barBG.GetComponent<RectTransform>();
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(600, 30);

        // Progress bar fill
        GameObject barFill = new GameObject("ProgressFill");
        barFill.transform.SetParent(barBG.transform, false);
        Image fillImg = barFill.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.55f, 0f);
        RectTransform fillRT = barFill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0.1f, 1f);
        fillRT.sizeDelta = Vector2.zero;
    }

    // ── Auth ──────────────────────────────────────────────────────────

    private static void SetupLoginUI()
    {
        Canvas canvas = CreateCanvas("LoginCanvas");
        CreateText(canvas.transform, "Title", "BLOOD RING", 64, new Color(0.85f, 0.1f, 0.05f), new Vector2(0, 200), new Vector2(600, 80));
        CreateText(canvas.transform, "Subtitle", "APEX ROYALE", 28, new Color(1f, 0.75f, 0.1f), new Vector2(0, 150), new Vector2(400, 40));

        CreateButton(canvas.transform, "LoginBtn", "LOGIN", new Vector2(-120, -50), new Vector2(200, 60), new Color(0.9f, 0.15f, 0.1f));
        CreateButton(canvas.transform, "RegisterBtn", "REGISTER", new Vector2(120, -50), new Vector2(200, 60), new Color(0.15f, 0.5f, 0.9f));
        CreateButton(canvas.transform, "GuestBtn", "PLAY AS GUEST", new Vector2(0, -130), new Vector2(300, 50), new Color(0.3f, 0.3f, 0.3f));
    }

    // ── Store / Inventory ─────────────────────────────────────────────

    private static void SetupStoreUI()
    {
        Canvas canvas = CreateCanvas("StoreCanvas");
        CreateText(canvas.transform, "StoreTitle", "STORE", 48, Color.white, new Vector2(0, 450), new Vector2(400, 80));
        CreateButton(canvas.transform, "FeaturedTab", "FEATURED", new Vector2(-200, 380), new Vector2(180, 50), new Color(0.9f, 0.55f, 0f));
        CreateButton(canvas.transform, "DailyTab", "DAILY DEALS", new Vector2(0, 380), new Vector2(180, 50), new Color(0.2f, 0.6f, 0.2f));
        CreateButton(canvas.transform, "LuckyDrawTab", "LUCKY DRAW", new Vector2(200, 380), new Vector2(180, 50), new Color(0.7f, 0.2f, 0.8f));
    }

    private static void SetupInventoryUI()
    {
        Canvas canvas = CreateCanvas("InventoryCanvas");
        CreateText(canvas.transform, "Title", "INVENTORY", 48, Color.white, new Vector2(0, 450), new Vector2(400, 80));
        CreateText(canvas.transform, "Desc", "Your items, skins, and characters", 20, Color.gray, new Vector2(0, 390), new Vector2(500, 30));
    }

    // ── Settings / Profile ────────────────────────────────────────────

    private static void SetupSettingsUI()
    {
        Canvas canvas = CreateCanvas("SettingsCanvas");
        CreateText(canvas.transform, "Title", "SETTINGS", 48, Color.white, new Vector2(0, 400), new Vector2(400, 80));
        CreateButton(canvas.transform, "AudioBtn", "AUDIO", new Vector2(0, 250), new Vector2(300, 60), new Color(0.3f, 0.3f, 0.3f));
        CreateButton(canvas.transform, "GraphicsBtn", "GRAPHICS", new Vector2(0, 170), new Vector2(300, 60), new Color(0.3f, 0.3f, 0.3f));
        CreateButton(canvas.transform, "ControlsBtn", "CONTROLS", new Vector2(0, 90), new Vector2(300, 60), new Color(0.3f, 0.3f, 0.3f));
        CreateButton(canvas.transform, "BackBtn", "BACK", new Vector2(0, -200), new Vector2(200, 50), new Color(0.6f, 0.1f, 0.1f));
    }

    private static void SetupProfileUI()
    {
        Canvas canvas = CreateCanvas("ProfileCanvas");
        CreateText(canvas.transform, "Title", "PROFILE", 48, Color.white, new Vector2(0, 400), new Vector2(400, 80));
        string name = PlayerPrefs.GetString("PlayerNickname", "Player");
        CreateText(canvas.transform, "PlayerName", name, 32, new Color(1f, 0.75f, 0.1f), new Vector2(0, 300), new Vector2(400, 50));
    }

    // ── Rankings / Social ─────────────────────────────────────────────

    private static void SetupRankingsUI()
    {
        Canvas canvas = CreateCanvas("RankingsCanvas");
        CreateText(canvas.transform, "Title", "RANKINGS", 48, Color.white, new Vector2(0, 400), new Vector2(400, 80));
        CreateButton(canvas.transform, "GlobalTab", "GLOBAL", new Vector2(-100, 330), new Vector2(160, 50), new Color(0.9f, 0.75f, 0.1f));
        CreateButton(canvas.transform, "FriendsTab", "FRIENDS", new Vector2(100, 330), new Vector2(160, 50), new Color(0.3f, 0.6f, 0.9f));
    }

    private static void SetupSocialUI()
    {
        Canvas canvas = CreateCanvas("SocialCanvas");
        CreateText(canvas.transform, "Title", "SOCIAL", 48, Color.white, new Vector2(0, 400), new Vector2(400, 80));
        CreateButton(canvas.transform, "FriendsTab", "FRIENDS", new Vector2(-150, 330), new Vector2(160, 50), new Color(0.3f, 0.6f, 0.9f));
        CreateButton(canvas.transform, "GuildTab", "GUILD", new Vector2(0, 330), new Vector2(160, 50), new Color(0.2f, 0.7f, 0.3f));
        CreateButton(canvas.transform, "ChatTab", "CHAT", new Vector2(150, 330), new Vector2(160, 50), new Color(0.7f, 0.5f, 0.9f));
    }

    private static void SetupEventsUI()
    {
        Canvas canvas = CreateCanvas("EventsCanvas");
        CreateText(canvas.transform, "Title", "EVENTS", 48, new Color(1f, 0.4f, 0f), new Vector2(0, 400), new Vector2(400, 80));
        CreateText(canvas.transform, "SeasonInfo", "Season 1: Blood Storm", 28, new Color(0.85f, 0.1f, 0.05f), new Vector2(0, 330), new Vector2(500, 40));
    }

    // ── Match Flow ────────────────────────────────────────────────────

    private static void SetupMatchmakingUI()
    {
        Canvas canvas = CreateCanvas("MatchmakingCanvas");
        CreateText(canvas.transform, "Title", "FINDING MATCH", 48, Color.white, new Vector2(0, 100), new Vector2(500, 80));
        CreateText(canvas.transform, "Mode", "Classic Battle Royale — Solo", 24, Color.gray, new Vector2(0, 30), new Vector2(400, 40));

        // Spinning indicator text
        Text statusText = CreateText(canvas.transform, "Status", "Searching for players...", 20, new Color(0.6f, 0.6f, 0.6f), new Vector2(0, -30), new Vector2(400, 30));

        CreateButton(canvas.transform, "CancelBtn", "CANCEL", new Vector2(0, -150), new Vector2(200, 50), new Color(0.6f, 0.1f, 0.1f));
    }

    private static void SetupWaitingIsland()
    {
        Canvas canvas = CreateCanvas("WaitingIslandCanvas");
        CreateText(canvas.transform, "Title", "WAITING ISLAND", 36, Color.white, new Vector2(0, 400), new Vector2(400, 60));
        CreateText(canvas.transform, "Timer", "Match starts in: 60s", 28, new Color(1f, 0.75f, 0.1f), new Vector2(0, 340), new Vector2(300, 40));
        CreateText(canvas.transform, "Players", "Players: 0/50", 22, Color.gray, new Vector2(0, 290), new Vector2(300, 30));
    }

    private static void SetupAircraftScene()
    {
        Canvas canvas = CreateCanvas("AircraftCanvas");
        CreateText(canvas.transform, "Title", "TAP TO JUMP", 36, new Color(1f, 0.85f, 0.1f), new Vector2(0, -300), new Vector2(400, 60));
        CreateText(canvas.transform, "Altitude", "ALT: 1000m", 22, Color.white, new Vector2(700, -400), new Vector2(200, 30));
    }

    // ── Gameplay ──────────────────────────────────────────────────────

    private static void SetupGameplayScene()
    {
        // Ensure gameplay systems exist
        EnsureController<GameSceneController>();

        // Create minimal game HUD if not present
        if (Object.FindObjectOfType<GameHUD>() == null)
        {
            Canvas canvas = CreateCanvas("GameHUD", 10);
            // Health bar
            GameObject healthBG = new GameObject("HealthBG");
            healthBG.transform.SetParent(canvas.transform, false);
            Image hbgImg = healthBG.AddComponent<Image>();
            hbgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            RectTransform hbgRT = healthBG.GetComponent<RectTransform>();
            hbgRT.anchorMin = new Vector2(0.02f, 0.02f);
            hbgRT.anchorMax = new Vector2(0.35f, 0.02f);
            hbgRT.sizeDelta = new Vector2(0, 20);

            GameObject healthFill = new GameObject("HealthFill");
            healthFill.transform.SetParent(healthBG.transform, false);
            Image hfImg = healthFill.AddComponent<Image>();
            hfImg.color = new Color(0.2f, 0.8f, 0.2f);
            RectTransform hfRT = healthFill.GetComponent<RectTransform>();
            hfRT.anchorMin = Vector2.zero;
            hfRT.anchorMax = Vector2.one;
            hfRT.sizeDelta = Vector2.zero;

            // Kill count
            CreateText(canvas.transform, "KillCount", "Kills: 0", 20, Color.white, new Vector2(850, -50), new Vector2(200, 30));

            // Alive count
            CreateText(canvas.transform, "AliveCount", "Alive: 50", 20, new Color(1f, 0.75f, 0.1f), new Vector2(850, -80), new Vector2(200, 30));

            // Zone timer
            CreateText(canvas.transform, "ZoneTimer", "Zone closes in: 2:00", 18, new Color(0.9f, 0.3f, 0.1f), new Vector2(0, 480), new Vector2(300, 30));
        }
    }

    private static void SetupTrainingGround()
    {
        Canvas canvas = CreateCanvas("TrainingCanvas");
        CreateText(canvas.transform, "Title", "TRAINING GROUND", 36, Color.white, new Vector2(0, 450), new Vector2(400, 60));
        CreateText(canvas.transform, "Hint", "Practice your aim and movement!", 20, Color.gray, new Vector2(0, 400), new Vector2(500, 30));
        CreateButton(canvas.transform, "ExitBtn", "EXIT TRAINING", new Vector2(0, -400), new Vector2(250, 50), new Color(0.6f, 0.1f, 0.1f));
    }

    // ── Results ───────────────────────────────────────────────────────

    private static void SetupGameOverUI(string sceneName)
    {
        Canvas canvas = CreateCanvas("GameOverCanvas");
        bool isVictory = sceneName == "ResultVictoryScreen";

        Color titleColor = isVictory ? new Color(1f, 0.85f, 0.1f) : new Color(0.9f, 0.15f, 0.1f);
        string titleText = isVictory ? "🏆 APEX VICTORY!" : "ELIMINATED";

        CreateText(canvas.transform, "Title", titleText, 56, titleColor, new Vector2(0, 250), new Vector2(600, 80));

        // Match stats
        int kills = PlayerPrefs.GetInt("LastMatchKills", 0);
        float damage = PlayerPrefs.GetFloat("LastMatchDamage", 0f);
        int placement = PlayerPrefs.GetInt("LastMatchPlacement", 0);
        float survival = PlayerPrefs.GetFloat("LastMatchSurvival", 0f);

        CreateText(canvas.transform, "Placement", $"#{placement}", 72, titleColor, new Vector2(0, 130), new Vector2(200, 80));
        CreateText(canvas.transform, "Kills", $"Kills: {kills}", 28, Color.white, new Vector2(-150, 30), new Vector2(200, 40));
        CreateText(canvas.transform, "Damage", $"Damage: {damage:F0}", 28, Color.white, new Vector2(0, 30), new Vector2(200, 40));
        CreateText(canvas.transform, "Survival", $"Survival: {survival:F0}s", 28, Color.white, new Vector2(150, 30), new Vector2(200, 40));

        // XP earned
        CreateText(canvas.transform, "XP", "+850 XP", 32, new Color(0.3f, 0.8f, 1f), new Vector2(0, -50), new Vector2(200, 40));
        CreateText(canvas.transform, "Coins", "+520 Coins", 32, new Color(1f, 0.75f, 0.1f), new Vector2(0, -100), new Vector2(200, 40));

        CreateButton(canvas.transform, "BackToLobby", "BACK TO LOBBY", new Vector2(0, -250), new Vector2(250, 55), new Color(0.2f, 0.6f, 0.2f));
    }

    // ── Error / Reconnect ─────────────────────────────────────────────

    private static void SetupReconnectUI()
    {
        Canvas canvas = CreateCanvas("ReconnectCanvas");
        CreateText(canvas.transform, "Title", "CONNECTION LOST", 48, new Color(0.9f, 0.15f, 0.1f), new Vector2(0, 100), new Vector2(500, 80));
        CreateText(canvas.transform, "Message", "Please check your internet connection and try again.", 20, Color.gray, new Vector2(0, 30), new Vector2(500, 40));
        CreateButton(canvas.transform, "RetryBtn", "RETRY", new Vector2(-100, -80), new Vector2(180, 50), new Color(0.2f, 0.6f, 0.2f));
        CreateButton(canvas.transform, "QuitBtn", "QUIT", new Vector2(100, -80), new Vector2(180, 50), new Color(0.6f, 0.1f, 0.1f));
    }
}
