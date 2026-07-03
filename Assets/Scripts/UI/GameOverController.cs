using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;

/// <summary>
/// BloodRing-style Game Over / Match Summary screen.
/// Winner gets "VICTORY!" with confetti. Losers get placement with stats breakdown.
/// Layout mirrors FF's post-match summary with XP/rank/coin rewards.
/// </summary>
public class GameOverController : MonoBehaviour
{
    private Text rewardsText;
    private GameObject rankUpPanel; private Text rankUpText;

    private async void Start()
    {
        if (EventSystem.current == null) { GameObject es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }

        Camera cam = Camera.main;
        if (cam == null) { GameObject cg = new GameObject("Main Camera"); cam = cg.AddComponent<Camera>(); cam.tag = "MainCamera"; }
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f); cam.clearFlags = CameraClearFlags.SolidColor;

        MatchData data = MatchData.Load();
        bool isWin = data.placement == 1;

        GameObject canvasGo = new GameObject("GameOverCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();
        Transform ct = canvasGo.transform;

        // ── Win / Lose effects ───────────────────────────────────────────────
        if (isWin)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWinSound();
            ParticleSystem ps = ProceduralArt.CreateConfetti(); ps.gameObject.SetActive(true); ps.Play();
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLoseSound();
            // Dark red overlay
            GameObject lo = new GameObject("LossOverlay"); lo.transform.SetParent(ct, false); lo.transform.SetAsFirstSibling();
            Image li = lo.AddComponent<Image>(); li.color = new Color(0.12f, 0, 0, 0.55f);
            RectTransform lr = lo.GetComponent<RectTransform>(); lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
        }

        // ── Big title (VICTORY! or #Placement) ───────────────────────────────
        string titleStr = isWin ? "VICTORY!" : "#" + data.placement;
        Color titleCol  = isWin ? new Color(1f, 0.82f, 0f) : new Color(0.7f, 0.7f, 0.7f);

        MkText(ct, "BigTitle", titleStr, 96, FontStyle.Bold, titleCol, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(700, 110));

        if (!isWin)
            MkText(ct, "BetterLuck", "BETTER LUCK NEXT TIME", 22, FontStyle.Normal, UIBuilder.COL_TEXT_DIM, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(400, 30));

        // ── Stats panel (dark card, centred) ─────────────────────────────────
        GameObject statsPanel = new GameObject("StatsPanel"); statsPanel.transform.SetParent(ct, false);
        Image spBG = statsPanel.AddComponent<Image>(); spBG.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        RectTransform spR = statsPanel.GetComponent<RectTransform>();
        spR.anchoredPosition = new Vector2(0, -10); spR.sizeDelta = new Vector2(600, 300);

        float rowY = 110;
        StatRow(statsPanel.transform, "PLACEMENT",      "#" + data.placement,                      new Vector2(0, rowY)); rowY -= 55;
        StatRow(statsPanel.transform, "KILLS",           data.kills.ToString(),                     new Vector2(0, rowY)); rowY -= 55;
        StatRow(statsPanel.transform, "DAMAGE DEALT",    Mathf.RoundToInt(data.damageDealt) + " HP", new Vector2(0, rowY)); rowY -= 55;
        StatRow(statsPanel.transform, "SURVIVAL TIME",   FormatTime(data.matchDuration),            new Vector2(0, rowY)); rowY -= 55;

        // Rewards line
        GameObject rGo = new GameObject("Rewards"); rGo.transform.SetParent(statsPanel.transform, false);
        rewardsText = rGo.AddComponent<Text>();
        rewardsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rewardsText.fontSize = 24; rewardsText.fontStyle = FontStyle.Bold;
        rewardsText.color = UIBuilder.COL_CYAN; rewardsText.alignment = TextAnchor.MiddleCenter;
        RectTransform rR = rGo.GetComponent<RectTransform>(); rR.anchoredPosition = new Vector2(0, rowY); rR.sizeDelta = new Vector2(560, 35);
        rewardsText.text = "CALCULATING REWARDS...";

        // ── Action buttons (bottom) ──────────────────────────────────────────
        UIBuilder.CreateStartButton(ct, "▶  PLAY AGAIN", new Vector2(-180, -230), () => { GameManager.Instance.ChangeState(GameState.CharacterSelect); });
        RectTransform paR = ct.Find("StartBtn")?.GetComponent<RectTransform>();
        if (paR) paR.sizeDelta = new Vector2(280, 65);

        UIBuilder.CreateButton(ct, "MenuBtn", "MAIN MENU", new Vector2(180, -230),
            new Color(0.2f, 0.2f, 0.25f, 0.9f), Color.white, () => { GameManager.Instance.ChangeState(GameState.MainMenu); });
        RectTransform mbR = ct.Find("MenuBtn")?.GetComponent<RectTransform>();
        if (mbR) mbR.sizeDelta = new Vector2(280, 65);

        // ── Rank-up overlay (hidden until server response) ───────────────────
        CreateRankUpPanel(ct);

        // ── Server submission ────────────────────────────────────────────────
        if (BackendAPI.Instance != null && BackendAPI.Instance.IsLoggedIn)
        {
            MatchResultResponse res = await BackendAPI.Instance.SubmitMatchResultAsync(
                data.kills, data.placement, data.damageDealt, data.matchDuration, "CLASSIC", true);
            if (res != null && string.IsNullOrEmpty(res.error))
            {
                rewardsText.text = "+" + res.earnedXP + " XP   |   +" + res.earnedCoins + " COINS   |   Lv." + res.newLevel;
                if (data.placement <= 5 && res.newRankTier != "Bronze I")
                {
                    rankUpText.text = "PROMOTED TO\n<color=yellow>" + res.newRankTier + "</color>\n" + res.newRankPoints + " RP  (" + res.winStreak + " Win Streak)";
                    rankUpPanel.SetActive(true);
                }
            }
            else { rewardsText.text = "REWARDS LOGGED LOCALLY"; }
        }
        else { rewardsText.text = "GUEST MATCH COMPLETE"; }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private void CreateRankUpPanel(Transform parent)
    {
        rankUpPanel = new GameObject("RankUpPanel"); rankUpPanel.transform.SetParent(parent, false);
        Image bg = rankUpPanel.AddComponent<Image>(); bg.color = UIBuilder.COL_PANEL_BG;
        RectTransform r = rankUpPanel.GetComponent<RectTransform>(); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(600, 380);

        MkText(rankUpPanel.transform, "RankTitle", "RANK UP!", 48, FontStyle.Bold, UIBuilder.COL_GOLD, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(400, 60));

        GameObject tGo = new GameObject("RankText"); tGo.transform.SetParent(rankUpPanel.transform, false);
        rankUpText = tGo.AddComponent<Text>();
        rankUpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rankUpText.fontSize = 30; rankUpText.color = Color.white; rankUpText.alignment = TextAnchor.MiddleCenter;
        rankUpText.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform rt2 = tGo.GetComponent<RectTransform>(); rt2.anchoredPosition = new Vector2(0, 10); rt2.sizeDelta = new Vector2(550, 160);

        UIBuilder.CreateButton(rankUpPanel.transform, "CloseRank", "CONTINUE", new Vector2(0, -140),
            UIBuilder.COL_ORANGE_DARK, UIBuilder.COL_GOLD, () => { rankUpPanel.SetActive(false); });

        rankUpPanel.SetActive(false);
    }

    private void StatRow(Transform parent, string label, string value, Vector2 pos)
    {
        MkText(parent, "L_" + label, label, 24, FontStyle.Normal, UIBuilder.COL_TEXT_DIM, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos + new Vector2(-150, 0), new Vector2(220, 35));
        MkText(parent, "V_" + label, value, 26, FontStyle.Bold, UIBuilder.COL_GOLD, TextAnchor.MiddleRight,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos + new Vector2(150, 0), new Vector2(220, 35));
    }

    private Text MkText(Transform p, string n, string text, int sz, FontStyle st, Color c, TextAnchor a,
        Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sd)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        Text t = go.AddComponent<Text>(); t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = sz; t.fontStyle = st; t.color = c; t.alignment = a;
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = aMin; r.anchorMax = aMax;
        r.anchoredPosition = pos; r.sizeDelta = sd; return t;
    }

    private string FormatTime(float s) { return string.Format("{0:00}:{1:00}", Mathf.FloorToInt(s / 60f), Mathf.FloorToInt(s % 60f)); }
}


