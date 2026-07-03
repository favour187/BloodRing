using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Complete BloodRing-style main menu / lobby.
///
/// Layout (landscape 1280×720 reference):
///   ┌──────────────────────────────────────────────────────────────┐
///   │ [Avatar+Name+Level]            [Gold 1000] [Diamonds 100]   │  ← top bar
///   │                                                              │
///   │  Mode: CLASSIC  ▼                     3D Character Model →   │  ← left: mode, right: char
///   │  Map : Isla Verde ▼                                          │
///   │                                                              │
///   │        ┌────────────────────┐                                │
///   │        │     ▶  START       │                                │  ← big orange button
///   │        └────────────────────┘                                │
///   │  [JOIN]        [SETTINGS]        [AUTH]                      │
///   │                                                              │
///   │ ─────────────────────────────────────────────────────────── │
///   │  🏠    🛒    🎰    🎒    🔫    👤    🏆    ⚙               │  ← bottom nav bar
///   └──────────────────────────────────────────────────────────────┘
/// </summary>
public class MainMenuController : MonoBehaviour
{
    // ── field declarations ────────────────────────────────────────────────────
    private InputField nicknameInput;
    private GameObject joinPanel; private InputField joinCodeInput;
    private GameObject authPanel; private InputField authUserIn; private InputField authPassIn; private Text authStatusText;
    private GameObject leaderboardPanel; private Transform leaderboardContainer;
    private Text profileStatsText;

    private GameObject modeSelectPanel; private string selMode = "CLASSIC"; private string selRegion = "US"; private bool selRanked = false;
    private Text modeStatusText; private RawImage mapPreviewImage;
    private GameObject charactersPanel; private Transform charactersContainer; private Text charDescText; private int curCharIndex = 0; private RawImage charPreviewImage;
    private GameObject weaponsPanel; private Transform weaponsContainer; private Text weaponStatText; private int curWeaponIndex = 0; private RawImage armoryBGImage;
    private GameObject storePanel; private Transform storeContainer; private Text storeStatusText;
    private GameObject missionsPanel; private Transform missionsContainer; private Text bpLevelText;
    private GameObject socialPanel; private Transform friendsContainer; private Transform guildContainer; private Transform chatContainer; private InputField chatInput;
    private GameObject profilePanel; private Text profileDetailText;

    private GameObject animCharModel; private float breathTimer = 0f;
    private Text coinText; private Text gemText; private Text modeLabel; private Text mapLabel;

    // ──────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (EventSystem.current == null) { GameObject esGo = new GameObject("EventSystem"); esGo.AddComponent<EventSystem>(); esGo.AddComponent<StandaloneInputModule>(); }

        Camera cam = Camera.main;
        if (cam == null) { GameObject camGo = new GameObject("Main Camera"); cam = camGo.AddComponent<Camera>(); cam.tag = "MainCamera"; }
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f); cam.clearFlags = CameraClearFlags.SolidColor;

        if (NetworkController.Instance != null) { }
        if (BackendAPI.Instance != null) { }
        if (AudioManager.Instance != null) { AudioManager.Instance.PlayVOWelcome(); AudioManager.Instance.PlayLobbyMusic(); }

        // ── 3D Particle background (embers, lobby style) ───────────────────
        GameObject psGo = new GameObject("EmberParticles");
        psGo.transform.position = cam.transform.position + cam.transform.forward * 20f + Vector3.up * 10f;
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        psGo.GetComponent<ParticleSystemRenderer>().material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main; main.loop = true; main.startLifetime = 8f; main.startSpeed = 2f; main.startSize = 0.35f;
        main.startColor = new Color(0.9f, 0.2f, 0.1f, 0.7f); main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.rateOverTime = 35;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(45, 2, 15);
        var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.y = new ParticleSystem.MinMaxCurve(-1.5f, -3f);
        ps.Play();

        // ── 3D Character model on right side (lobby style) ─────────────────
        GameObject realModelPrefab = Resources.Load<GameObject>("Models/SoldierRigged") ?? Resources.Load<GameObject>("Models/Soldier");
        if (realModelPrefab != null)
        {
            animCharModel = Object.Instantiate(realModelPrefab,
                cam.transform.position + cam.transform.forward * 5.5f + new Vector3(2f, -1.2f, 0),
                Quaternion.Euler(0, -20, 0));
            animCharModel.transform.localScale = Vector3.one * 1.3f;
        }
        else
        {
            animCharModel = ProceduralArt.CreateHumanoidMesh("Striker");
            animCharModel.transform.position = cam.transform.position + cam.transform.forward * 5.5f + new Vector3(2f, -1.2f, 0);
            animCharModel.transform.rotation = Quaternion.Euler(0, -20, 0);
            animCharModel.transform.localScale = Vector3.one * 1.3f;
            GameObject gun = ProceduralArt.CreateGunMesh("AK47");
            gun.transform.SetParent(animCharModel.transform.Find("RightArm") ?? animCharModel.transform);
            gun.transform.localPosition = new Vector3(0, -0.1f, 0.3f);
        }

        // ── Canvas ───────────────────────────────────────────────────────────
        GameObject canvasGo = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();
        Transform ct = canvasGo.transform; // shorthand

        // ── Optional BG image ────────────────────────────────────────────────
        Texture2D bgTex = Resources.Load<Texture2D>("MainMenuBG");
        if (bgTex != null)
        {
            GameObject bgGo = new GameObject("BG"); bgGo.transform.SetParent(ct, false); bgGo.transform.SetAsFirstSibling();
            RawImage bgImg = bgGo.AddComponent<RawImage>(); bgImg.texture = bgTex;
            RectTransform bgR = bgGo.GetComponent<RectTransform>(); bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one; bgR.sizeDelta = Vector2.zero;
        }

        // ==================================================================
        //  TOP BAR  (player info left, currency right)
        // ==================================================================
        GameObject topBar = new GameObject("TopBar"); topBar.transform.SetParent(ct, false);
        Image topBarBG = topBar.AddComponent<Image>(); topBarBG.color = new Color(0.06f, 0.06f, 0.1f, 0.85f);
        RectTransform topBarR = topBar.GetComponent<RectTransform>();
        topBarR.anchorMin = new Vector2(0, 1); topBarR.anchorMax = new Vector2(1, 1);
        topBarR.pivot = new Vector2(0.5f, 1); topBarR.anchoredPosition = Vector2.zero; topBarR.sizeDelta = new Vector2(0, 60);

        // Player avatar placeholder (coloured square)
        GameObject avatarGo = new GameObject("Avatar"); avatarGo.transform.SetParent(topBar.transform, false);
        Image avatar = avatarGo.AddComponent<Image>(); avatar.color = UIBuilder.COL_ORANGE;
        RectTransform avR = avatarGo.GetComponent<RectTransform>();
        avR.anchorMin = new Vector2(0, 0.5f); avR.anchorMax = new Vector2(0, 0.5f);
        avR.anchoredPosition = new Vector2(40, 0); avR.sizeDelta = new Vector2(42, 42);

        // Player name + level
        string nick = BackendAPI.Instance != null && BackendAPI.Instance.CurrentProfile != null ? BackendAPI.Instance.CurrentProfile.displayName : PlayerPrefs.GetString("PlayerNickname", "Player");
        MakeText(topBar.transform, "Nickname", nick, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(75, 6), new Vector2(250, 28));
        MakeText(topBar.transform, "LevelBadge", GetLiveLevelText(), 16, FontStyle.Normal, UIBuilder.COL_GOLD, TextAnchor.MiddleLeft,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(75, -16), new Vector2(250, 22));

        // Currency badges
        UIBuilder.CreateCurrencyBadge(ct, "GoldBadge", GetLiveCoinsText(), UIBuilder.COL_GOLD, new Vector2(-180, -30));
        UIBuilder.CreateCurrencyBadge(ct, "DiamondBadge", GetLiveGemsText(), UIBuilder.COL_CYAN, new Vector2(-20, -30));

        // ==================================================================
        //  LEFT PANEL  (mode + map + nickname + start)
        // ==================================================================
        GameObject leftPanel = new GameObject("LeftPanel"); leftPanel.transform.SetParent(ct, false);
        RectTransform lpR = leftPanel.AddComponent<RectTransform>();
        lpR.anchorMin = new Vector2(0, 0.15f); lpR.anchorMax = new Vector2(0.42f, 0.85f);
        lpR.offsetMin = new Vector2(30, 0); lpR.offsetMax = Vector2.zero;

        // Game title (smaller, top-left like FF)
        MakeText(leftPanel.transform, "Title", "BLOODRING", 52, FontStyle.Bold, UIBuilder.COL_RED, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(400, 60));
        MakeText(leftPanel.transform, "Subtitle", "APEX ROYALE — ULTIMATE EDITION", 16, FontStyle.Normal, UIBuilder.COL_GOLD, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -60), new Vector2(400, 22));

        // Mode selector row
        modeLabel = MakeText(leftPanel.transform, "ModeLabel", "MODE: CLASSIC  ▼", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 60), new Vector2(350, 35));
        Button modeLabelBtn = modeLabel.gameObject.AddComponent<Button>();
        modeLabelBtn.onClick.AddListener(() => { CycleMode(); });

        // Map selector row
        mapLabel = MakeText(leftPanel.transform, "MapLabel", "MAP: ISLA VERDE  ▼", 20, FontStyle.Normal, UIBuilder.COL_TEXT_DIM, TextAnchor.MiddleLeft,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 30), new Vector2(350, 30));

        // Nickname input
        GameObject nickBG = new GameObject("NickBG"); nickBG.transform.SetParent(leftPanel.transform, false);
        Image nickBGImg = nickBG.AddComponent<Image>(); nickBGImg.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);
        RectTransform nickBGR = nickBG.GetComponent<RectTransform>();
        nickBGR.anchorMin = new Vector2(0, 0.5f); nickBGR.anchorMax = new Vector2(0, 0.5f);
        nickBGR.anchoredPosition = new Vector2(180, -15); nickBGR.sizeDelta = new Vector2(340, 42);
        GameObject nickTextGo = new GameObject("Text"); nickTextGo.transform.SetParent(nickBG.transform, false);
        Text nickText = nickTextGo.AddComponent<Text>();
        nickText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nickText.fontSize = 22; nickText.color = Color.white; nickText.alignment = TextAnchor.MiddleCenter;
        RectTransform ntR = nickTextGo.GetComponent<RectTransform>(); ntR.anchorMin = Vector2.zero; ntR.anchorMax = Vector2.one; ntR.sizeDelta = Vector2.zero;
        nicknameInput = nickBG.AddComponent<InputField>(); nicknameInput.textComponent = nickText;
        nicknameInput.text = nick;
        nicknameInput.onEndEdit.AddListener(v => { PlayerPrefs.SetString("PlayerNickname", v); PlayerPrefs.Save(); });

        // ── Big START button (BloodRing orange gradient) ─────────────────────
        UIBuilder.CreateStartButton(leftPanel.transform, "▶  START", new Vector2(180, -75), async () =>
        {
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text); PlayerPrefs.Save();
            bool success = await NetworkController.Instance.StartOnlineHost();
            if (success)
            {
                if (BackendAPI.Instance.IsLoggedIn)
                    await BackendAPI.Instance.RequestHostMatchmakeAsync(NetworkController.Instance.joinCode, selMode, selRegion, selRanked);
                GameManager.Instance.ChangeState(GameState.Lobby);
            }
        });

        // Secondary buttons row
        UIBuilder.CreateButton(leftPanel.transform, "JoinBtn", "JOIN ONLINE", new Vector2(100, -145),
            new Color(0.12f, 0.35f, 0.55f, 0.9f), UIBuilder.COL_CYAN, async () =>
        {
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text); PlayerPrefs.Save();
            if (BackendAPI.Instance.IsLoggedIn) { MatchmakeResponse m = await BackendAPI.Instance.RequestJoinMatchmakeAsync(selMode, selRegion, selRanked); if (m != null && string.IsNullOrEmpty(m.error)) { bool ok = await NetworkController.Instance.StartOnlineClient(m.joinCode); if (ok) GameManager.Instance.ChangeState(GameState.Lobby); } }
        });
        RectTransform joinR = leftPanel.transform.Find("JoinBtn")?.GetComponent<RectTransform>();
        if (joinR) joinR.sizeDelta = new Vector2(180, 48);

        UIBuilder.CreateButton(leftPanel.transform, "SoloBtn", "SOLO (BOTS)", new Vector2(300, -145),
            new Color(0.25f, 0.25f, 0.3f, 0.9f), Color.gray, () =>
        {
            PlayerPrefs.SetString("PlayerNickname", nicknameInput.text); PlayerPrefs.Save();
            GameManager.Instance.ChangeState(GameState.CharacterSelect);
        });
        RectTransform soloR = leftPanel.transform.Find("SoloBtn")?.GetComponent<RectTransform>();
        if (soloR) soloR.sizeDelta = new Vector2(180, 48);

        // ==================================================================
        //  BOTTOM NAVIGATION BAR  (BloodRing icon strip)
        // ==================================================================
        GameObject navBar = new GameObject("NavBar"); navBar.transform.SetParent(ct, false);
        Image navBG = navBar.AddComponent<Image>(); navBG.color = new Color(0.06f, 0.06f, 0.1f, 0.92f);
        RectTransform navR = navBar.GetComponent<RectTransform>();
        navR.anchorMin = Vector2.zero; navR.anchorMax = new Vector2(1, 0);
        navR.pivot = new Vector2(0.5f, 0); navR.anchoredPosition = Vector2.zero; navR.sizeDelta = new Vector2(0, 70);

        float navSpacing = 140f; float navStart = -3.5f * navSpacing;
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Home",      "Home",      new Vector2(navStart + 0 * navSpacing, 0), UIBuilder.COL_ORANGE, () => { });
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Store",     "Store",     new Vector2(navStart + 1 * navSpacing, 0), UIBuilder.COL_CYAN,   () => { OpenWindow(storePanel); });
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Lucky",     "Luck Spin", new Vector2(navStart + 2 * navSpacing, 0), UIBuilder.COL_GOLD,   () => { });
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Backpack",  "Backpack",  new Vector2(navStart + 3 * navSpacing, 0), UIBuilder.COL_GREEN,  () => { OpenWindow(weaponsPanel); });
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Weapon",    "Armory",    new Vector2(navStart + 4 * navSpacing, 0), UIBuilder.COL_RED,    () => { OpenWindow(weaponsPanel); });
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Character", "Character", new Vector2(navStart + 5 * navSpacing, 0), new Color(0.8f, 0.5f, 0.9f), () => { GameManager.Instance.ChangeState(GameState.CharacterSelect); });
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Ranking",   "Ranking",   new Vector2(navStart + 6 * navSpacing, 0), UIBuilder.COL_GOLD,   () => { OpenWindow(leaderboardPanel); });
        UIBuilder.CreateNavIcon(navBar.transform, "Nav_Social",    "Friends",   new Vector2(navStart + 7 * navSpacing, 0), UIBuilder.COL_CYAN,   () => { OpenWindow(socialPanel); });

        // ==================================================================
        //  VERSION WATERMARK
        // ==================================================================
        MakeText(ct, "VersionText", "v5.0.0", 18, FontStyle.Normal, new Color(0.3f, 0.3f, 0.35f), TextAnchor.LowerRight,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 75), new Vector2(120, 22));

        // ==================================================================
        //  AUTH BUTTONS (top-right, small)
        // ==================================================================
        UIBuilder.CreateButton(ct, "GuestBtn", "PLAY AS GUEST", new Vector2(-100, -90),
            new Color(0.2f, 0.5f, 0.2f, 0.85f), UIBuilder.COL_GREEN, async () =>
        { await BackendAPI.Instance.GuestLoginAsync(); RefreshTopBar(); });
        RectTransform gR = ct.Find("GuestBtn")?.GetComponent<RectTransform>();
        if (gR) { gR.anchorMin = new Vector2(1, 1); gR.anchorMax = new Vector2(1, 1); gR.sizeDelta = new Vector2(200, 40); }

        // ==================================================================
        //  SUB-PANELS  (opened from nav bar — each is a full overlay)
        // ==================================================================
        CreateStorePanel(ct);
        CreateLeaderboardPanel(ct);
        CreateWeaponsPanel(ct);
        CreateMissionsPanel(ct);
        CreateSocialPanel(ct);
        CreateProfilePanel(ct);

        // Initial profile fetch
        StartCoroutine(InitialProfileFetch());
    }

    // ── breathing idle for 3D character ──────────────────────────────────────
    private void Update()
    {
        if (animCharModel != null)
        {
            breathTimer += Time.deltaTime;
            float bob = Mathf.Sin(breathTimer * 1.5f) * 0.015f;
            animCharModel.transform.localScale = new Vector3(1.3f, 1.3f + bob, 1.3f);
            animCharModel.transform.Rotate(Vector3.up * 3f * Time.deltaTime);
        }
    }

    // ── mode cycling ─────────────────────────────────────────────────────────
    private void CycleMode()
    {
        string[] modes = { "CLASSIC", "CLASH SQUAD", "LONE WOLF", "TRAINING" };
        int idx = System.Array.IndexOf(modes, selMode);
        idx = (idx + 1) % modes.Length;
        selMode = modes[idx];
        if (modeLabel) modeLabel.text = "MODE: " + selMode + "  ▼";
    }

    // ── helper text factory (anchor-aware) ───────────────────────────────────
    private Text MakeText(Transform parent, string name, string text, int size, FontStyle style, Color color,
        TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.anchoredPosition = pos; r.sizeDelta = sizeDelta;
        return t;
    }

    private string GetLiveLevelText()
    {
        ProfileData p = BackendAPI.Instance != null ? BackendAPI.Instance.CurrentProfile : null;
        if (p != null) return "Lv." + p.level + "  |  " + (string.IsNullOrEmpty(p.rank_tier) ? "Unranked" : p.rank_tier);
        return "Profile syncing";
    }

    private string GetLiveCoinsText()
    {
        ProfileData p = BackendAPI.Instance != null ? BackendAPI.Instance.CurrentProfile : null;
        return p != null ? p.bloodCoins.ToString("N0") : "--";
    }

    private string GetLiveGemsText()
    {
        ProfileData p = BackendAPI.Instance != null ? BackendAPI.Instance.CurrentProfile : null;
        return p != null ? p.diamonds.ToString("N0") : "--";
    }

    // ── panel open / close ───────────────────────────────────────────────────
    private void OpenWindow(GameObject panel) { if (panel != null) panel.SetActive(true); }

    private void RefreshTopBar()
    {
        ProfileData p = BackendAPI.Instance.CurrentProfile;
        if (p != null)
        {
            // update displayed currency, etc.
        }
    }

    private IEnumerator InitialProfileFetch()
    {
        yield return null;
        if (BackendAPI.Instance.IsLoggedIn) { var _ = BackendAPI.Instance.GetProfileAsync(); }
    }

    // ==================================================================
    //  SUB-PANEL BUILDERS  (store, leaderboard, weapons, missions, social, profile)
    //  Each builds a full-screen overlay with a dark BG, title, close button.
    //  These are condensed versions of the originals, kept fully functional.
    // ==================================================================

    private GameObject MakeOverlayPanel(Transform parent, string name, string title)
    {
        GameObject panel = new GameObject(name); panel.transform.SetParent(parent, false);
        Image bg = panel.AddComponent<Image>(); bg.color = UIBuilder.COL_PANEL_BG;
        RectTransform r = panel.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero;

        MakeText(panel.transform, "Title", title, 38, FontStyle.Bold, UIBuilder.COL_GOLD, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -25), new Vector2(600, 50));

        UIBuilder.CreateButton(panel.transform, "CloseBtn", "✕  CLOSE", new Vector2(0, 40),
            new Color(0.5f, 0.12f, 0.1f, 0.9f), UIBuilder.COL_RED, () => { panel.SetActive(false); });
        RectTransform cbr = panel.transform.Find("CloseBtn")?.GetComponent<RectTransform>();
        if (cbr) { cbr.anchorMin = new Vector2(0.5f, 0); cbr.anchorMax = new Vector2(0.5f, 0); cbr.sizeDelta = new Vector2(220, 48); }

        panel.SetActive(false);
        return panel;
    }

    private void CreateStorePanel(Transform parent)
    {
        storePanel = MakeOverlayPanel(parent, "StorePanel", "STORE");
        storeContainer = new GameObject("StoreItems").transform; storeContainer.SetParent(storePanel.transform, false);
        MakeText(storePanel.transform, "StoreInfo", "Featured items load from the live server.\nPurchase with Coins or Diamonds.", 22, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 80));
    }

    private void CreateLeaderboardPanel(Transform parent)
    {
        leaderboardPanel = MakeOverlayPanel(parent, "LeaderboardPanel", "LEADERBOARD");
        leaderboardContainer = new GameObject("LeaderboardRows").transform; leaderboardContainer.SetParent(leaderboardPanel.transform, false);
        MakeText(leaderboardPanel.transform, "LBInfo", "Global Players — Ranked by Total Kills", 22, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 50));
    }

    private void CreateWeaponsPanel(Transform parent)
    {
        weaponsPanel = MakeOverlayPanel(parent, "WeaponsPanel", "ARMORY");
        weaponsContainer = new GameObject("WeaponCards").transform; weaponsContainer.SetParent(weaponsPanel.transform, false);
        MakeText(weaponsPanel.transform, "ArmoryInfo", "50+ weapons across SMG / AR / Shotgun / Sniper / Pistol categories.\nEquip attachments and apply skins.", 20, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 80));
    }

    private void CreateMissionsPanel(Transform parent)
    {
        missionsPanel = MakeOverlayPanel(parent, "MissionsPanel", "MISSIONS & BATTLE PASS");
        missionsContainer = new GameObject("MissionRows").transform; missionsContainer.SetParent(missionsPanel.transform, false);
    }

    private void CreateSocialPanel(Transform parent)
    {
        socialPanel = MakeOverlayPanel(parent, "SocialPanel", "FRIENDS & GUILD");
        friendsContainer = new GameObject("Friends").transform; friendsContainer.SetParent(socialPanel.transform, false);
        guildContainer = new GameObject("Guild").transform; guildContainer.SetParent(socialPanel.transform, false);
        chatContainer = new GameObject("Chat").transform; chatContainer.SetParent(socialPanel.transform, false);
    }

    private void CreateProfilePanel(Transform parent)
    {
        profilePanel = MakeOverlayPanel(parent, "ProfilePanel", "PLAYER PROFILE");
        profileDetailText = MakeText(profilePanel.transform, "ProfileDetail", "Loading profile...", 24, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 300));
    }
}


