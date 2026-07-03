using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BloodRing-style UI factory.  Provides reusable helpers for every screen:
/// gradient buttons, stat bars, icon badges, bottom nav bars, panel windows, etc.
/// Every colour value and proportion was eye-matched to BloodRing Apex Royale.
/// </summary>
public class MinimapBlip : MonoBehaviour
{
    public Color blipColor = Color.white;
    private GameObject blipQuad;

    private void Start()
    {
        blipQuad = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Quad.obj");
        blipQuad.name = "Blip_" + gameObject.name;
        blipQuad.transform.SetParent(transform);
        blipQuad.transform.localPosition = new Vector3(0, 45f, 0);
        blipQuad.transform.rotation = Quaternion.Euler(90, 0, 0);
        blipQuad.transform.localScale = new Vector3(8f, 8f, 1f);
        Destroy(blipQuad.GetComponent<Collider>());
        Material mat = new Material(ProceduralArt.GetSafeShader("Unlit/Color"));
        mat.color = blipColor;
        blipQuad.GetComponent<Renderer>().material = mat;
        blipQuad.layer = LayerMask.NameToLayer("UI");
    }

    private void Update()
    {
        if (blipQuad != null)
        {
            blipQuad.transform.rotation = Quaternion.Euler(90, 0, 0);
            blipQuad.transform.position = new Vector3(transform.position.x, 45f, transform.position.z);
        }
    }
}

public static class UIBuilder
{
    // ── Colour palette (BloodRing inspired) ──────────────────────────────────
    public static readonly Color COL_ORANGE      = new Color(1f, 0.55f, 0.05f);
    public static readonly Color COL_ORANGE_DARK = new Color(0.85f, 0.35f, 0f);
    public static readonly Color COL_RED         = new Color(0.85f, 0.12f, 0.08f);
    public static readonly Color COL_GOLD        = new Color(1f, 0.82f, 0.15f);
    public static readonly Color COL_CYAN        = new Color(0.2f, 0.85f, 1f);
    public static readonly Color COL_GREEN       = new Color(0.15f, 0.85f, 0.25f);
    public static readonly Color COL_PANEL_BG    = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    public static readonly Color COL_BAR_BG      = new Color(0.12f, 0.12f, 0.15f);
    public static readonly Color COL_TEXT_DIM     = new Color(0.55f, 0.55f, 0.6f);

    // ── Premium gradient button with rounded corners, glow, bevel, and drop shadow ──
    public static GameObject CreateButton(Transform parent, string name, string text,
        Vector2 pos, Color bgCol, Color borderCol, UnityEngine.Events.UnityAction action)
    {
        // Container (holds shadow + main)
        GameObject btnGo = new GameObject(name); btnGo.transform.SetParent(parent, false);
        RectTransform rect = btnGo.AddComponent<RectTransform>();
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(300, 60);

        // Drop shadow (dark copy offset down-right)
        Texture2D shadowTex = ProceduralArt.GenerateButtonTexture(new Color(0, 0, 0, 0.45f), new Color(0, 0, 0, 0.25f));
        GameObject shadowGo = new GameObject("Shadow"); shadowGo.transform.SetParent(btnGo.transform, false);
        Image shadowImg = shadowGo.AddComponent<Image>();
        shadowImg.sprite = Sprite.Create(shadowTex, new Rect(0, 0, shadowTex.width, shadowTex.height),
            Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
        shadowImg.type = Image.Type.Sliced;
        shadowImg.raycastTarget = false;
        RectTransform sr = shadowGo.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
        sr.offsetMin = new Vector2(-2, -4); sr.offsetMax = new Vector2(4, 0);

        // Main button face
        Texture2D btnTex = ProceduralArt.GenerateButtonTexture(bgCol, borderCol);
        GameObject faceGo = new GameObject("Face"); faceGo.transform.SetParent(btnGo.transform, false);
        Image bg = faceGo.AddComponent<Image>();
        bg.sprite = Sprite.Create(btnTex, new Rect(0, 0, btnTex.width, btnTex.height),
            Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
        bg.type = Image.Type.Sliced;
        RectTransform fr = faceGo.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.sizeDelta = Vector2.zero; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;

        // Text with drop-shadow effect (two Text layers)
        GameObject textShadowGo = new GameObject("TextShadow"); textShadowGo.transform.SetParent(faceGo.transform, false);
        Text tShadow = textShadowGo.AddComponent<Text>();
        tShadow.text = text; tShadow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tShadow.fontSize = 28; tShadow.fontStyle = FontStyle.Bold;
        tShadow.color = new Color(0, 0, 0, 0.5f); tShadow.alignment = TextAnchor.MiddleCenter;
        tShadow.raycastTarget = false;
        RectTransform tsr = textShadowGo.GetComponent<RectTransform>();
        tsr.anchorMin = Vector2.zero; tsr.anchorMax = Vector2.one; tsr.sizeDelta = Vector2.zero;
        tsr.anchoredPosition = new Vector2(1, -1);

        GameObject textGo = new GameObject("Text"); textGo.transform.SetParent(faceGo.transform, false);
        Text btnText = textGo.AddComponent<Text>();
        btnText.text = text; btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 28; btnText.fontStyle = FontStyle.Bold;
        btnText.color = Color.white; btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.sizeDelta = Vector2.zero;

        // Button component on outer container
        Button btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick(); });
        btn.onClick.AddListener(action);

        // Colour tint transition (normal → highlight → pressed)
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        // Scale-punch on pointer down
        EventTrigger trigger = btnGo.AddComponent<EventTrigger>();
        EventTrigger.Entry downEntry = new EventTrigger.Entry(); downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => { if (btnGo != null) { var mb = btnGo.GetComponent<MonoBehaviour>(); if (mb != null) mb.StartCoroutine(ScalePunchCoroutine(btnGo.transform)); } });
        trigger.triggers.Add(downEntry);

        return btnGo;
    }

    /// <summary>Extra-wide premium START button with pulsing glow animation.</summary>
    public static GameObject CreateStartButton(Transform parent, string text, Vector2 pos,
        UnityEngine.Events.UnityAction action)
    {
        GameObject btn = CreateButton(parent, "StartBtn", text, pos, COL_ORANGE, COL_GOLD, action);
        RectTransform r = btn.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(380, 75);
        // Make text larger and bolder
        foreach (Text t in btn.GetComponentsInChildren<Text>())
        {
            if (t.gameObject.name == "Text") t.fontSize = 36;
            else if (t.gameObject.name == "TextShadow") t.fontSize = 36;
        }
        // Add a subtle pulsing glow script
        btn.AddComponent<ButtonPulseGlow>();
        return btn;
    }

    // ── HP / Armor bar (BloodRing-style — rounded, two-tone) ─────────────────
    public static Image CreateBar(Transform parent, string name, Vector2 pos, Color col, out Text labelText)
    {
        GameObject bgGo = new GameObject(name + "_BG"); bgGo.transform.SetParent(parent, false);
        Image bg = bgGo.AddComponent<Image>(); bg.color = COL_BAR_BG;
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchoredPosition = pos; bgRect.sizeDelta = new Vector2(260, 26);

        GameObject fillGo = new GameObject(name + "_Fill"); fillGo.transform.SetParent(bgGo.transform, false);
        Image fill = fillGo.AddComponent<Image>(); fill.color = col;
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2); fillRect.offsetMax = new Vector2(-2, -2);
        fillRect.pivot = new Vector2(0, 0.5f);

        GameObject textGo = new GameObject(name + "_Text"); textGo.transform.SetParent(bgGo.transform, false);
        labelText = textGo.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 18; labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white; labelText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.sizeDelta = Vector2.zero;

        return fill;
    }

    // ── Dynamic crosshair (4 lines that spread when firing) ──────────────────
    public static List<RectTransform> CreateDynamicCrosshair(Transform canvas)
    {
        List<RectTransform> lines = new List<RectTransform>();
        GameObject chGo = new GameObject("DynamicCrosshair"); chGo.transform.SetParent(canvas, false);
        chGo.AddComponent<RectTransform>().anchoredPosition = Vector2.zero;

        Vector2[] pos  = { new Vector2(0, 15), new Vector2(0, -15), new Vector2(-15, 0), new Vector2(15, 0) };
        Vector2[] size = { new Vector2(3, 14), new Vector2(3, 14), new Vector2(14, 3), new Vector2(14, 3) };

        for (int i = 0; i < 4; i++)
        {
            GameObject lineGo = new GameObject("Line_" + i); lineGo.transform.SetParent(chGo.transform, false);
            Image img = lineGo.AddComponent<Image>(); img.color = Color.white;
            RectTransform lRect = lineGo.GetComponent<RectTransform>();
            lRect.anchoredPosition = pos[i]; lRect.sizeDelta = size[i];
            lines.Add(lRect);
        }
        // Center dot
        GameObject dotGo = new GameObject("CenterDot"); dotGo.transform.SetParent(chGo.transform, false);
        Image dot = dotGo.AddComponent<Image>(); dot.color = Color.white;
        RectTransform dRect = dotGo.GetComponent<RectTransform>(); dRect.anchoredPosition = Vector2.zero; dRect.sizeDelta = new Vector2(4, 4);

        return lines;
    }

    // ── Bottom navigation icon with gradient circle and label ──────────────
    public static GameObject CreateNavIcon(Transform parent, string name, string label, Vector2 pos,
        Color iconColor, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>(); rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(90, 75);

        // Circular gradient icon
        Texture2D circTex = ProceduralArt.GenerateCircleButtonTexture(iconColor, 64);
        GameObject iconGo = new GameObject("Icon"); iconGo.transform.SetParent(go.transform, false);
        Image icon = iconGo.AddComponent<Image>();
        icon.sprite = Sprite.Create(circTex, new Rect(0, 0, circTex.width, circTex.height), Vector2.one * 0.5f);
        RectTransform iRect = iconGo.GetComponent<RectTransform>(); iRect.anchoredPosition = new Vector2(0, 10); iRect.sizeDelta = new Vector2(40, 40);

        // Label text
        GameObject labelGo = new GameObject("Label"); labelGo.transform.SetParent(go.transform, false);
        Text t = labelGo.AddComponent<Text>();
        t.text = label; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 13; t.color = new Color(0.75f, 0.75f, 0.8f); t.alignment = TextAnchor.MiddleCenter;
        RectTransform lRect = labelGo.GetComponent<RectTransform>(); lRect.anchoredPosition = new Vector2(0, -22); lRect.sizeDelta = new Vector2(90, 20);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = icon;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick(); });
        btn.onClick.AddListener(action);

        return go;
    }

    // ── Currency badge (top-right coins / diamonds) ──────────────────────────
    public static GameObject CreateCurrencyBadge(Transform parent, string name, string value, Color iconColor, Vector2 pos)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1); rect.anchorMax = new Vector2(1, 1);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(160, 35);

        Image bg = go.AddComponent<Image>(); bg.color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

        GameObject iconGo = new GameObject("Icon"); iconGo.transform.SetParent(go.transform, false);
        Image icon = iconGo.AddComponent<Image>(); icon.color = iconColor;
        RectTransform iR = iconGo.GetComponent<RectTransform>(); iR.anchoredPosition = new Vector2(-55, 0); iR.sizeDelta = new Vector2(22, 22);

        GameObject textGo = new GameObject("Value"); textGo.transform.SetParent(go.transform, false);
        Text t = textGo.AddComponent<Text>(); t.text = value;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18; t.fontStyle = FontStyle.Bold; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        RectTransform tR = textGo.GetComponent<RectTransform>();
        tR.anchoredPosition = new Vector2(10, 0); tR.sizeDelta = new Vector2(100, 30);

        return go;
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    /// <summary>Generates a nice circular HUD touch-button with gradient, gloss, and glow ring.</summary>
    public static GameObject CreateHUDButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, Color color, bool isCenter = false)
    {
        GameObject btnGo = new GameObject(name); btnGo.transform.SetParent(parent, false);
        Image bg = btnGo.AddComponent<Image>();
        Texture2D circTex = ProceduralArt.GenerateCircleButtonTexture(color, 128);
        bg.sprite = Sprite.Create(circTex, new Rect(0, 0, circTex.width, circTex.height), Vector2.one * 0.5f);
        bg.preserveAspect = size.x == size.y; // keep circular if square size

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        if (isCenter) { rect.anchorMin = new Vector2(0.5f, 0); rect.anchorMax = new Vector2(0.5f, 0); }
        else { rect.anchorMin = new Vector2(1, 0); rect.anchorMax = new Vector2(1, 0); }
        rect.anchoredPosition = pos; rect.sizeDelta = size;

        // Label text with small shadow
        GameObject tShadow = new GameObject("TextShadow"); tShadow.transform.SetParent(btnGo.transform, false);
        Text ts = tShadow.AddComponent<Text>(); ts.text = label;
        ts.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ts.fontSize = 18; ts.fontStyle = FontStyle.Bold; ts.color = new Color(0, 0, 0, 0.55f);
        ts.alignment = TextAnchor.MiddleCenter; ts.raycastTarget = false;
        RectTransform tsr2 = tShadow.GetComponent<RectTransform>();
        tsr2.anchorMin = Vector2.zero; tsr2.anchorMax = Vector2.one;
        tsr2.sizeDelta = Vector2.zero; tsr2.anchoredPosition = new Vector2(1, -1);

        GameObject textGo = new GameObject("Text"); textGo.transform.SetParent(btnGo.transform, false);
        Text txt = textGo.AddComponent<Text>(); txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18; txt.fontStyle = FontStyle.Bold; txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        RectTransform tr = textGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;

        return btnGo;
    }

    private static IEnumerator ScalePunchCoroutine(Transform target)
    {
        float startTime = Time.time; float duration = 0.15f;
        while (Time.time - startTime < duration)
        {
            float t2 = (Time.time - startTime) / duration;
            float s = t2 < 0.5f ? Mathf.Lerp(1.0f, 1.08f, t2 * 2f) : Mathf.Lerp(1.08f, 1.0f, (t2 - 0.5f) * 2f);
            target.localScale = new Vector3(s, s, s);
            yield return null;
        }
        target.localScale = Vector3.one;
    }
}

/// <summary>
/// Attached to the START button to create a subtle, continuous pulsing scale + brightness animation.
/// </summary>
public class ButtonPulseGlow : MonoBehaviour
{
    private float timer = 0f;
    private Vector3 baseScale;
    private Image[] images;

    private void Start()
    {
        baseScale = transform.localScale;
        images = GetComponentsInChildren<Image>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        // Gentle scale pulse: 1.0 → 1.03 → 1.0
        float pulse = 1f + Mathf.Sin(timer * 2.5f) * 0.025f;
        transform.localScale = baseScale * pulse;

        // Subtle brightness pulse on the face image
        if (images != null)
        {
            float brightness = 1f + Mathf.Sin(timer * 3f) * 0.06f;
            foreach (Image img in images)
            {
                if (img.gameObject.name == "Face")
                {
                    Color c = img.color;
                    c.r = Mathf.Clamp01(brightness);
                    c.g = Mathf.Clamp01(brightness);
                    c.b = Mathf.Clamp01(brightness);
                    img.color = c;
                }
            }
        }
    }
}


