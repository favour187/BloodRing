using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// BloodRing-style splash screen — BloodRing-style publisher logo, then game logo with
/// animated fire/ember particles, a horizontal wipe progress bar, and version text.
/// </summary>
public class SplashController : MonoBehaviour
{
    private Image dripImage;

    private void Start()
    {
        Camera cam = Camera.main;
        if (cam == null) { GameObject camGo = new GameObject("Main Camera"); cam = camGo.AddComponent<Camera>(); cam.tag = "MainCamera"; }
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;

        GameObject canvasGo = new GameObject("SplashCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Full-screen black background ─────────────────────────────────────
        CreateFullscreenImage(canvasGo.transform, "Background", Color.black);

        // ── Publisher logo phase (like BloodRing Studio splash) ────────────────────────
        GameObject publisherGo = new GameObject("PublisherContainer");
        publisherGo.transform.SetParent(canvasGo.transform, false);
        CanvasGroup pubCG = publisherGo.AddComponent<CanvasGroup>(); pubCG.alpha = 0f;
        RectTransform pubRect = publisherGo.AddComponent<RectTransform>();
        pubRect.anchoredPosition = Vector2.zero;

        // Studio emblem (a procedural shield icon)
        GameObject emblemGo = new GameObject("Emblem");
        emblemGo.transform.SetParent(publisherGo.transform, false);
        Image emblem = emblemGo.AddComponent<Image>();
        emblem.color = new Color(0.9f, 0.15f, 0.1f);
        emblem.sprite = Sprite.Create(ProceduralArt.GenerateButtonTexture(new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.6f, 0f)), new Rect(0, 0, 128, 64), Vector2.one * 0.5f);
        RectTransform embRect = emblemGo.GetComponent<RectTransform>(); embRect.anchoredPosition = new Vector2(0, 40); embRect.sizeDelta = new Vector2(120, 120);

        CreateText(publisherGo.transform, "StudioName", "BLOODRING STUDIO", Vector2.zero, 36, FontStyle.Bold, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter, new Vector2(500, 50));
        CreateText(publisherGo.transform, "StudioSub", "PRESENTS", new Vector2(0, -40), 20, FontStyle.Normal, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleCenter, new Vector2(300, 30));

        // ── Main logo phase ──────────────────────────────────────────────────
        GameObject logoContainer = new GameObject("LogoContainer");
        logoContainer.transform.SetParent(canvasGo.transform, false);
        CanvasGroup logoCG = logoContainer.AddComponent<CanvasGroup>(); logoCG.alpha = 0f;
        RectTransform logoContRect = logoContainer.AddComponent<RectTransform>();
        logoContRect.anchoredPosition = Vector2.zero;

        // Dripping blood effect behind title
        GameObject dripGo = new GameObject("DripImage");
        dripGo.transform.SetParent(logoContainer.transform, false);
        dripImage = dripGo.AddComponent<Image>();
        RectTransform dripRect = dripGo.GetComponent<RectTransform>(); dripRect.anchoredPosition = new Vector2(0, 50); dripRect.sizeDelta = new Vector2(700, 180);
        dripImage.sprite = BloodRing.Art.BloodRingArtLibrary.Icon("BloodDrip") ?? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        dripImage.color = new Color(0.7f, 0.1f, 0.1f, 0.85f);

        // Game title
        CreateText(logoContainer.transform, "TitleBig", "BLOODRING", new Vector2(0, 60), 110, FontStyle.Bold, new Color(0.85f, 0.1f, 0.05f), TextAnchor.MiddleCenter, new Vector2(800, 140));
        CreateText(logoContainer.transform, "TitleSub", "APEX  ROYALE", new Vector2(0, -30), 40, FontStyle.Bold, new Color(1f, 0.75f, 0.1f), TextAnchor.MiddleCenter, new Vector2(500, 50));

        // Fire ember particles behind logo
        SpawnEmberParticles(cam.transform);

        // ── Bottom progress bar (BloodRing-style horizontal wipe) ────────────
        GameObject barBG = new GameObject("ProgressBarBG");
        barBG.transform.SetParent(canvasGo.transform, false);
        Image barBGImg = barBG.AddComponent<Image>(); barBGImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        RectTransform barBGRect = barBG.GetComponent<RectTransform>();
        barBGRect.anchorMin = new Vector2(0.15f, 0); barBGRect.anchorMax = new Vector2(0.85f, 0);
        barBGRect.anchoredPosition = new Vector2(0, 60); barBGRect.sizeDelta = new Vector2(0, 14);

        GameObject barFill = new GameObject("ProgressBarFill");
        barFill.transform.SetParent(barBG.transform, false);
        Image barFillImg = barFill.AddComponent<Image>(); barFillImg.color = new Color(1f, 0.55f, 0f);
        RectTransform barFillRect = barFill.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero; barFillRect.anchorMax = new Vector2(0, 1);
        barFillRect.sizeDelta = Vector2.zero; barFillRect.pivot = new Vector2(0, 0.5f);

        CreateText(canvasGo.transform, "LoadingLabel", "Loading resources...", new Vector2(0, 85), 18, FontStyle.Normal, new Color(0.6f, 0.6f, 0.6f), TextAnchor.LowerCenter, new Vector2(400, 25), new Vector2(0.5f, 0), new Vector2(0.5f, 0));

        // Version at bottom-right
        CreateText(canvasGo.transform, "VersionText", "v5.0.0", new Vector2(-15, 15), 18, FontStyle.Normal, new Color(0.35f, 0.35f, 0.35f), TextAnchor.LowerRight, new Vector2(150, 25), new Vector2(1, 0), new Vector2(1, 0));

        StartCoroutine(SplashSequence(pubCG, logoCG, barFillRect));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private IEnumerator SplashSequence(CanvasGroup pubCG, CanvasGroup logoCG, RectTransform barFill)
    {
        // Phase 1 — publisher logo fade in / out
        pubCG.DOFade(1f, 0.8f); yield return new WaitForSeconds(1.8f);
        pubCG.DOFade(0f, 0.6f); yield return new WaitForSeconds(0.8f);

        // Phase 2 — game logo + drip animation + progress bar
        logoCG.DOFade(1f, 0.5f);
        float elapsed = 0f;
        float totalLoad = 3f;
        while (elapsed < totalLoad)
        {
            elapsed += Time.deltaTime;
            // Progress bar
            float p = Mathf.Clamp01(elapsed / totalLoad);
            barFill.anchorMax = new Vector2(p, 1);
            logoCG.alpha = 0.85f + Mathf.Sin(elapsed * 5f) * 0.15f;
            yield return null;
        }

        GameManager.Instance.ChangeState(GameState.MainMenu);
    }

    private void SpawnEmberParticles(Transform camTransform)
    {
        GameObject psGo = new GameObject("EmberParticles");
        psGo.transform.position = camTransform.position + camTransform.forward * 15f + Vector3.up * 8f;
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var m = ps.main; m.loop = true; m.startLifetime = 5f; m.startSpeed = 1.5f; m.startSize = 0.25f;
        m.startColor = new Color(1f, 0.35f, 0.05f, 0.75f); m.maxParticles = 400;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        var e = ps.emission; e.rateOverTime = 30;
        var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(30, 1, 10);
        var v = ps.velocityOverLifetime; v.enabled = true; v.y = new ParticleSystem.MinMaxCurve(-1f, -3f);
        ps.Play();
    }

    // ── UI factory helpers ────────────────────────────────────────────────────

    private void CreateFullscreenImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>(); img.color = color;
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero;
    }

    private Text CreateText(Transform parent, string name, string text, Vector2 pos, int size, FontStyle style, Color color, TextAnchor align, Vector2 sizeDelta, Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
        RectTransform r = go.GetComponent<RectTransform>();
        if (anchorMin.HasValue) r.anchorMin = anchorMin.Value;
        if (anchorMax.HasValue) r.anchorMax = anchorMax.Value;
        r.anchoredPosition = pos; r.sizeDelta = sizeDelta;
        return t;
    }
}


