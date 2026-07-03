using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BloodRing-style in-game HUD.
///
///   ┌──────────────────────────────────────────────────────────────┐
///   │  [Minimap]  ←compass→                 ALIVE:15  ZONE:2:00  │
///   │                                                              │
///   │  Kill                                                        │
///   │  Feed                            +  crosshair               │
///   │                                                              │
///   │                                                              │
///   │ [HP ████████░░]  [ARMOR █████░░░]     [WeaponName] 30/90    │
///   │  [Slot1] [Slot2] [Consumable]     [FIRE🔴] [SCOPE] [CROUCH]│
///   └──────────────────────────────────────────────────────────────┘
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance;

    private Image healthBarFill; private Text healthText; private float targetHealthRatio = 1f;
    private Image armorBarFill;  private Text armorText;  private float targetArmorRatio  = 0.5f;
    private Text playerNameText;

    private Text weaponNameText; private Text ammoCountText; private Image weaponIconImage;
    private Text aliveCountText; private Text zoneStatusText;

    private Transform killFeedContainer; private List<GameObject> killFeedItems = new List<GameObject>();
    private Text slot1Text; private Image slot1BG;
    private Text slot2Text; private Image slot2BG;
    private Text consumableText; private Image consumableBG;

    private GameObject powerBarGo; private Text powerNameText; private Image powerBarFill; private Image powerIconImage;
    private Camera minimapCam; private Transform playerTransform;

    private List<RectTransform> crosshairLines;
    private RawImage damageOverlayImage; private float damageOverlayAlpha = 0f;

    private void Awake() { Instance = this; }

    public void InitializeHUD(Transform canvasTransform, Transform player)
    {
        Debug.Log("Initializing BloodRing-style Game HUD (v5.0)...");
        playerTransform = player;
        Transform ct = canvasTransform;

        // ── Full-screen damage overlay (red vignette on hit) ─────────────────
        Texture2D dmgTex = Resources.Load<Texture2D>("HUD_DamageOverlay");
        if (dmgTex != null)
        {
            GameObject dmgGo = new GameObject("DamageOverlay"); dmgGo.transform.SetParent(ct, false); dmgGo.transform.SetAsFirstSibling();
            damageOverlayImage = dmgGo.AddComponent<RawImage>(); damageOverlayImage.texture = dmgTex; damageOverlayImage.color = new Color(1, 1, 1, 0);
            RectTransform dr = dmgGo.GetComponent<RectTransform>(); dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.sizeDelta = Vector2.zero;
        }

        // ==================================================================
        //  TOP-LEFT — circular minimap
        // ==================================================================
        RenderTexture rt = new RenderTexture(256, 256, 16); rt.name = "MinimapRT";
        GameObject mmCamGo = new GameObject("MinimapCamera");
        minimapCam = mmCamGo.AddComponent<Camera>(); minimapCam.orthographic = true; minimapCam.orthographicSize = 60f;
        minimapCam.targetTexture = rt; minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0.08f, 0.2f, 0.08f); minimapCam.transform.rotation = Quaternion.Euler(90, 0, 0);
        minimapCam.cullingMask = 1 << LayerMask.NameToLayer("UI");

        GameObject mmContainer = new GameObject("MinimapContainer"); mmContainer.transform.SetParent(ct, false);
        RectTransform mmR = mmContainer.AddComponent<RectTransform>();
        mmR.anchorMin = new Vector2(0, 1); mmR.anchorMax = new Vector2(0, 1);
        mmR.anchoredPosition = new Vector2(130, -130); mmR.sizeDelta = new Vector2(200, 200);
        // Circular mask
        Image maskBg = mmContainer.AddComponent<Image>(); maskBg.color = Color.black;
        Mask mask = mmContainer.AddComponent<Mask>(); mask.showMaskGraphic = false;
        // Map bg art
        Texture2D mmBG = Resources.Load<Texture2D>("HUD_MinimapBG");
        if (mmBG != null) { GameObject mgGo = MakeRawImageChild(mmContainer.transform, "MinimapBGArt", mmBG, Vector2.zero, new Vector2(200, 200)); }
        // Render target
        MakeRawImageChild(mmContainer.transform, "MinimapRender", rt, Vector2.zero, new Vector2(200, 200));
        // Border ring
        GameObject borderGo = new GameObject("Border"); borderGo.transform.SetParent(mmContainer.transform, false);
        Image border = borderGo.AddComponent<Image>(); border.color = new Color(0.25f, 0.25f, 0.3f, 0.7f);
        Outline outline = borderGo.AddComponent<Outline>(); outline.effectColor = new Color(0.9f, 0.5f, 0f, 0.6f); outline.effectDistance = new Vector2(2, -2);
        RectTransform bR = borderGo.GetComponent<RectTransform>(); bR.anchorMin = Vector2.zero; bR.anchorMax = Vector2.one; bR.sizeDelta = Vector2.zero;
        border.color = new Color(0, 0, 0, 0); // transparent fill, outline only

        // ==================================================================
        //  TOP-RIGHT — alive count + zone timer
        // ==================================================================
        GameObject trGo = new GameObject("TopRight"); trGo.transform.SetParent(ct, false);
        RectTransform trR = trGo.AddComponent<RectTransform>();
        trR.anchorMin = new Vector2(1, 1); trR.anchorMax = new Vector2(1, 1); trR.anchoredPosition = new Vector2(-20, -20);

        // Alive count (with player icon)
        GameObject aliveGo = new GameObject("AlivePanel"); aliveGo.transform.SetParent(trGo.transform, false);
        Image aliveBG = aliveGo.AddComponent<Image>(); aliveBG.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        RectTransform aliveR = aliveGo.GetComponent<RectTransform>(); aliveR.anchoredPosition = new Vector2(-80, -5); aliveR.sizeDelta = new Vector2(130, 40);
        // Icon
        GameObject pIconGo = new GameObject("PeopleIcon"); pIconGo.transform.SetParent(aliveGo.transform, false);
        Image pIcon = pIconGo.AddComponent<Image>(); pIcon.color = Color.white;
        RectTransform piR = pIconGo.GetComponent<RectTransform>(); piR.anchoredPosition = new Vector2(-42, 0); piR.sizeDelta = new Vector2(22, 22);
        aliveCountText = MakeText(aliveGo.transform, "AliveCount", "20", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(80, 36));

        // Zone status
        zoneStatusText = MakeText(trGo.transform, "ZoneStatus", "ZONE STABLE", 20, FontStyle.Bold, UIBuilder.COL_CYAN, TextAnchor.MiddleRight, new Vector2(-80, -45), new Vector2(220, 28));

        // ==================================================================
        //  LEFT-CENTER — kill feed
        // ==================================================================
        GameObject feedGo = new GameObject("KillFeed"); feedGo.transform.SetParent(ct, false);
        RectTransform feedR = feedGo.AddComponent<RectTransform>();
        feedR.anchorMin = new Vector2(0, 0.5f); feedR.anchorMax = new Vector2(0, 0.5f);
        feedR.anchoredPosition = new Vector2(200, 80); feedR.sizeDelta = new Vector2(360, 200);
        killFeedContainer = feedGo.transform;

        // ==================================================================
        //  BOTTOM-LEFT — HP + Armor bars + player name
        // ==================================================================
        GameObject blGo = new GameObject("BottomLeftHUD"); blGo.transform.SetParent(ct, false);
        RectTransform blR = blGo.AddComponent<RectTransform>();
        blR.anchorMin = Vector2.zero; blR.anchorMax = Vector2.zero; blR.anchoredPosition = new Vector2(280, 55);

        playerNameText = MakeText(blGo.transform, "PlayerName", PlayerPrefs.GetString("PlayerNickname", "Striker"), 20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, new Vector2(-230, 50), new Vector2(250, 25));

        // HP bar (red, BloodRing-style)
        healthBarFill = UIBuilder.CreateBar(blGo.transform, "HP", new Vector2(0, 22), new Color(0.85f, 0.15f, 0.1f), out healthText);
        // Armor / EP bar (blue)
        armorBarFill = UIBuilder.CreateBar(blGo.transform, "Armor", new Vector2(0, -8), new Color(0.2f, 0.5f, 0.9f), out armorText);

        // ==================================================================
        //  BOTTOM-RIGHT — weapon info
        // ==================================================================
        GameObject brGo = new GameObject("BottomRightHUD"); brGo.transform.SetParent(ct, false);
        RectTransform brR = brGo.AddComponent<RectTransform>();
        brR.anchorMin = new Vector2(1, 0); brR.anchorMax = new Vector2(1, 0); brR.anchoredPosition = new Vector2(-280, 55);

        weaponNameText = MakeText(brGo.transform, "WeaponName", "UNARMED", 24, FontStyle.Bold, UIBuilder.COL_GOLD, TextAnchor.MiddleRight, new Vector2(80, 20), new Vector2(200, 30));
        ammoCountText  = MakeText(brGo.transform, "AmmoCount",  "0 / 0",   20, FontStyle.Bold, Color.white,        TextAnchor.MiddleRight, new Vector2(80, -10), new Vector2(200, 25));

        // Weapon icon square
        GameObject wIconGo = new GameObject("WeaponIcon"); wIconGo.transform.SetParent(brGo.transform, false);
        weaponIconImage = wIconGo.AddComponent<Image>(); weaponIconImage.color = Color.gray;
        RectTransform wiR = wIconGo.GetComponent<RectTransform>(); wiR.anchoredPosition = new Vector2(200, 5); wiR.sizeDelta = new Vector2(50, 50);

        // ==================================================================
        //  BOTTOM-CENTER — weapon slot strip
        // ==================================================================
        GameObject invGo = new GameObject("WeaponSlots"); invGo.transform.SetParent(ct, false);
        RectTransform invR = invGo.AddComponent<RectTransform>();
        invR.anchorMin = new Vector2(0.5f, 0); invR.anchorMax = new Vector2(0.5f, 0); invR.anchoredPosition = new Vector2(0, 55);

        slot1BG      = MakeSlot(invGo.transform, "Slot1",       new Vector2(-120, 0), out slot1Text);
        slot2BG      = MakeSlot(invGo.transform, "Slot2",       new Vector2(  0,  0), out slot2Text);
        consumableBG = MakeSlot(invGo.transform, "Consumable",  new Vector2( 120, 0), out consumableText);

        // ==================================================================
        //  POWER-UP BAR (appears when active)
        // ==================================================================
        powerBarGo = new GameObject("PowerBar"); powerBarGo.transform.SetParent(ct, false);
        RectTransform pbR = powerBarGo.AddComponent<RectTransform>();
        pbR.anchorMin = new Vector2(0.5f, 0); pbR.anchorMax = new Vector2(0.5f, 0); pbR.anchoredPosition = new Vector2(0, 140);
        Image pBg = powerBarGo.AddComponent<Image>(); pBg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f); pbR.sizeDelta = new Vector2(280, 28);

        GameObject pfGo = new GameObject("Fill"); pfGo.transform.SetParent(powerBarGo.transform, false);
        powerBarFill = pfGo.AddComponent<Image>(); powerBarFill.color = Color.magenta;
        RectTransform pfR = pfGo.GetComponent<RectTransform>(); pfR.anchorMin = Vector2.zero; pfR.anchorMax = new Vector2(1, 1); pfR.sizeDelta = Vector2.zero; pfR.pivot = new Vector2(0, 0.5f);

        powerNameText = MakeText(powerBarGo.transform, "PowerLabel", "", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(260, 25));

        GameObject piGo = new GameObject("PowerIcon"); piGo.transform.SetParent(powerBarGo.transform, false);
        powerIconImage = piGo.AddComponent<Image>();
        RectTransform ppR = piGo.GetComponent<RectTransform>(); ppR.anchoredPosition = new Vector2(-165, 0); ppR.sizeDelta = new Vector2(40, 40);
        powerBarGo.SetActive(false);

        // ==================================================================
        //  CROSSHAIR
        // ==================================================================
        crosshairLines = UIBuilder.CreateDynamicCrosshair(ct);
    }

    // ── Update loop ──────────────────────────────────────────────────────────
    private void Update()
    {
        if (minimapCam != null && playerTransform != null)
            minimapCam.transform.position = new Vector3(playerTransform.position.x, 100f, playerTransform.position.z);

        if (ZoneController.Instance != null)
            zoneStatusText.text = ZoneController.Instance.GetZoneStatusText();

        // Smooth bar lerp
        healthBarFill.rectTransform.anchorMax = new Vector2(Mathf.Lerp(healthBarFill.rectTransform.anchorMax.x, targetHealthRatio, Time.deltaTime * 10f), 1);
        armorBarFill.rectTransform.anchorMax  = new Vector2(Mathf.Lerp(armorBarFill.rectTransform.anchorMax.x,  targetArmorRatio,  Time.deltaTime * 10f), 1);

        // Damage overlay fade
        if (damageOverlayImage != null && damageOverlayAlpha > 0)
        { damageOverlayAlpha = Mathf.Max(0, damageOverlayAlpha - Time.deltaTime * 1.5f); damageOverlayImage.color = new Color(1, 1, 1, damageOverlayAlpha); }

        // Dynamic crosshair spread
        if (crosshairLines != null && crosshairLines.Count == 4)
        {
            float spread = (TouchControls.Instance != null && (TouchControls.Instance.IsFiring || TouchControls.Instance.MoveInput.magnitude > 0.1f)) ? 30f : 12f;
            crosshairLines[0].anchoredPosition = Vector2.Lerp(crosshairLines[0].anchoredPosition, new Vector2(0, spread), Time.deltaTime * 15f);
            crosshairLines[1].anchoredPosition = Vector2.Lerp(crosshairLines[1].anchoredPosition, new Vector2(0, -spread), Time.deltaTime * 15f);
            crosshairLines[2].anchoredPosition = Vector2.Lerp(crosshairLines[2].anchoredPosition, new Vector2(-spread, 0), Time.deltaTime * 15f);
            crosshairLines[3].anchoredPosition = Vector2.Lerp(crosshairLines[3].anchoredPosition, new Vector2(spread, 0), Time.deltaTime * 15f);
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void TriggerDamageOverlay() { damageOverlayAlpha = 0.75f; }

    public void UpdateHealthArmor(float hp, float maxHp, float armor, float maxArmor)
    {
        if (hp < maxHp * healthBarFill.rectTransform.anchorMax.x) TriggerDamageOverlay();
        targetHealthRatio = Mathf.Clamp01(hp / maxHp);
        healthText.text = Mathf.RoundToInt(hp) + " / " + Mathf.RoundToInt(maxHp);
        targetArmorRatio = Mathf.Clamp01(armor / maxArmor);
        armorText.text = Mathf.RoundToInt(armor) + " / " + Mathf.RoundToInt(maxArmor);
    }

    public void UpdateWeaponAmmo(string wName, int curAmmo, int maxAmmo, Color iconColor)
    {
        weaponNameText.text = wName.ToUpper();
        ammoCountText.text = curAmmo + " / " + maxAmmo;
        Sprite artSprite = BloodRing.Art.BloodRingArtLibrary.Weapon(wName);
        if (artSprite != null) { weaponIconImage.sprite = artSprite; weaponIconImage.color = Color.white; }
        else { weaponIconImage.sprite = null; weaponIconImage.color = iconColor; }
    }

    public void UpdateAliveCount(int alive) { aliveCountText.text = alive.ToString(); }

    public void UpdateInventory(string w1, string w2, int activeSlot, string consumableName, int consumableCount)
    {
        slot1Text.text = w1; slot1BG.color = activeSlot == 0 ? new Color(0.85f, 0.3f, 0.1f, 0.85f) : new Color(0.15f, 0.15f, 0.2f, 0.8f);
        slot2Text.text = w2; slot2BG.color = activeSlot == 1 ? new Color(0.85f, 0.3f, 0.1f, 0.85f) : new Color(0.15f, 0.15f, 0.2f, 0.8f);
        consumableText.text = consumableName + " ×" + consumableCount;
        consumableBG.color = consumableCount > 0 ? new Color(0.1f, 0.5f, 0.15f, 0.8f) : new Color(0.15f, 0.15f, 0.2f, 0.8f);
    }

    public void UpdatePowerUpBar(string pName, float remaining, float total, Color color)
    {
        if (remaining <= 0) { powerBarGo.SetActive(false); return; }
        if (!powerBarGo.activeSelf) powerBarGo.SetActive(true);
        powerNameText.text = pName.ToUpper() + "  " + Mathf.CeilToInt(remaining) + "s";
        powerBarFill.color = color;
        powerBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(remaining / total), 1);
        powerIconImage.sprite = Sprite.Create(ProceduralArt.GeneratePowerIcon(pName), new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
    }

    public void ShowToast(string message)
    {
        AddKillFeedEntry("SYSTEM", message);
    }

    public void AddKillFeedEntry(string killer, string victim)
    {
        GameObject item = new GameObject("KFItem"); item.transform.SetParent(killFeedContainer, false);
        Image bg = item.AddComponent<Image>(); bg.color = new Color(0.08f, 0.08f, 0.1f, 0.75f);
        RectTransform r = item.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(350, 32);

        GameObject tGo = new GameObject("T"); tGo.transform.SetParent(item.transform, false);
        Text t = tGo.AddComponent<Text>();
        t.text = "<color=#FF5533>" + killer + "</color> ▸ <color=#FFFFFF>" + victim + "</color>";
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = 18; t.alignment = TextAnchor.MiddleLeft;
        RectTransform tr = tGo.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(8, 0); tr.offsetMax = Vector2.zero;

        killFeedItems.Add(item);
        if (killFeedItems.Count > 5) { Destroy(killFeedItems[0]); killFeedItems.RemoveAt(0); }
        for (int i = 0; i < killFeedItems.Count; i++)
            killFeedItems[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (killFeedItems.Count - 1 - i) * 36);

        StartCoroutine(FadeKillEntry(item));
    }

    // ── internal helpers ─────────────────────────────────────────────────────
    private IEnumerator FadeKillEntry(GameObject item) { yield return new WaitForSeconds(5f); if (item != null) { killFeedItems.Remove(item); Destroy(item); } }

    private Text MakeText(Transform parent, string name, string text, int size, FontStyle style, Color color, TextAnchor align, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>(); t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
        RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = pos; r.sizeDelta = sizeDelta;
        return t;
    }

    private Image MakeSlot(Transform parent, string name, Vector2 pos, out Text label)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>(); bg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = pos; r.sizeDelta = new Vector2(110, 48);

        GameObject tGo = new GameObject("Label"); tGo.transform.SetParent(go.transform, false);
        label = tGo.AddComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16; label.fontStyle = FontStyle.Bold; label.color = Color.white; label.alignment = TextAnchor.MiddleCenter;
        RectTransform tr = tGo.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
        return bg;
    }

    private GameObject MakeRawImageChild(Transform parent, string name, Texture tex, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        RawImage ri = go.AddComponent<RawImage>(); ri.texture = tex;
        RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = pos; r.sizeDelta = size;
        return go;
    }
}


