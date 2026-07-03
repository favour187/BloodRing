using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameFeel : MonoBehaviour
{
    private static GameFeel instance;
    public static GameFeel Instance { get { if (instance == null) { GameObject go = new GameObject("[GameFeel]"); instance = go.AddComponent<GameFeel>(); DontDestroyOnLoad(go); } return instance; } }

    private Coroutine shakeCoroutine; private Coroutine hitStopCoroutine;
    private Canvas damageNumberCanvas; private GameObject zoneVignetteGo; private Image zoneVignetteImg;

    // Elite Object Pools (No GC Allocation Spikes)
    private List<GameObject> decalPool = new List<GameObject>();
    private List<GameObject> damageTextPool = new List<GameObject>();
    private List<LineRenderer> tracerPool = new List<LineRenderer>();
    private List<ParticleSystem> impactPool = new List<ParticleSystem>();

    private void Awake() { if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); InitializeCanvas(); InitializePools(); } else if (instance != this) { Destroy(gameObject); } }

    private void InitializeCanvas()
    {
        if (damageNumberCanvas != null) return;
        GameObject canvasGo = new GameObject("DamageNumberCanvas"); DontDestroyOnLoad(canvasGo);
        damageNumberCanvas = canvasGo.AddComponent<Canvas>(); damageNumberCanvas.renderMode = RenderMode.WorldSpace; damageNumberCanvas.sortingOrder = 5000;

        // Zone Vignette Overlay (Screen space)
        GameObject vigCanvasGo = new GameObject("ZoneVignetteCanvas"); DontDestroyOnLoad(vigCanvasGo);
        Canvas vigCanvas = vigCanvasGo.AddComponent<Canvas>(); vigCanvas.renderMode = RenderMode.ScreenSpaceOverlay; vigCanvas.sortingOrder = 4000;
        zoneVignetteGo = new GameObject("ZoneVignette"); zoneVignetteGo.transform.SetParent(vigCanvasGo.transform, false);
        zoneVignetteImg = zoneVignetteGo.AddComponent<Image>(); zoneVignetteImg.color = new Color(0.8f, 0.1f, 0.1f, 0f);
        RectTransform rect = zoneVignetteGo.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        zoneVignetteGo.SetActive(false);
    }

    private void InitializePools()
    {
        // Decals
        GameObject decalHolder = new GameObject("DecalPool"); DontDestroyOnLoad(decalHolder);
        Material decalMat = new Material(ProceduralArt.GetSafeShader("Unlit/Color")); decalMat.color = Color.black;
        for (int i = 0; i < 30; i++) { GameObject quad = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Quad.obj"); quad.transform.SetParent(decalHolder.transform); quad.name = "Decal_" + i; quad.GetComponent<Renderer>().material = decalMat; Destroy(quad.GetComponent<Collider>()); quad.SetActive(false); decalPool.Add(quad); }

        // Tracers
        GameObject tracerHolder = new GameObject("TracerPool"); DontDestroyOnLoad(tracerHolder);
        Material tracerMat = new Material(ProceduralArt.GetSafeShader("Unlit/Color")); tracerMat.color = Color.yellow;
        for (int i = 0; i < 15; i++) { GameObject lineGo = new GameObject("Tracer_" + i); lineGo.transform.SetParent(tracerHolder.transform); LineRenderer lr = lineGo.AddComponent<LineRenderer>(); lr.material = tracerMat; lr.startWidth = 0.05f; lr.endWidth = 0.05f; lr.positionCount = 2; lr.enabled = false; tracerPool.Add(lr); }

        // Impacts
        GameObject impactHolder = new GameObject("ImpactPool"); DontDestroyOnLoad(impactHolder);
        Material impMat = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        for (int i = 0; i < 15; i++) { GameObject impGo = new GameObject("Impact_" + i); impGo.transform.SetParent(impactHolder.transform); ParticleSystem ps = impGo.AddComponent<ParticleSystem>(); ParticleSystemRenderer pr = impGo.GetComponent<ParticleSystemRenderer>(); pr.material = impMat; var main = ps.main; main.duration = 0.2f; main.loop = false; main.startSize = 0.2f; main.startSpeed = 6f; main.startLifetime = 0.3f; var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) }); var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 25f; impGo.SetActive(false); impactPool.Add(ps); }

        // Dust Clouds
        GameObject dustHolder = new GameObject("DustPool"); DontDestroyOnLoad(dustHolder);
        for (int i = 0; i < 10; i++) { GameObject dustGo = new GameObject("Dust_" + i); dustGo.transform.SetParent(dustHolder.transform); ParticleSystem ps = dustGo.AddComponent<ParticleSystem>(); ParticleSystemRenderer pr = dustGo.GetComponent<ParticleSystemRenderer>(); pr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default")); var main = ps.main; main.duration = 0.5f; main.loop = false; main.startColor = new Color(0.8f, 0.8f, 0.7f, 0.5f); main.startSize = 1f; main.startSpeed = 2f; main.startLifetime = 0.5f; var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) }); var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.5f; dustGo.SetActive(false); impactPool.Add(ps); }
    }


    #region Screen Shake & Hit Stop
    public void TriggerScreenShake(float duration = 0.3f, float magnitude = 0.2f) { if (Camera.main == null) return; if (shakeCoroutine != null) StopCoroutine(shakeCoroutine); shakeCoroutine = StartCoroutine(ScreenShakeCoroutine(Camera.main.transform, duration, magnitude)); }
    private IEnumerator ScreenShakeCoroutine(Transform camTransform, float duration, float magnitude) { Vector3 origPos = camTransform.localPosition; float elapsed = 0f; while (elapsed < duration) { if (camTransform == null) yield break; float x = Random.Range(-1f, 1f) * magnitude; float y = Random.Range(-1f, 1f) * magnitude; camTransform.localPosition = origPos + new Vector3(x, y, 0); elapsed += Time.deltaTime; yield return null; } if (camTransform != null) camTransform.localPosition = origPos; }

    public void TriggerHitStop(float timeScale = 0.1f, float duration = 0.05f) { if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine); hitStopCoroutine = StartCoroutine(HitStopCoroutine(timeScale, duration)); }
    private IEnumerator HitStopCoroutine(float timeScale, float duration) { Time.timeScale = timeScale; yield return new WaitForSecondsRealtime(duration); Time.timeScale = 1f; }
    #endregion

    #region VFX Pools & Tracers
    public void SpawnDecal(Vector3 pos, Vector3 normal)
    {
        foreach (GameObject d in decalPool) { if (!d.activeSelf) { d.transform.position = pos + normal * 0.01f; d.transform.rotation = Quaternion.LookRotation(normal); d.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); d.SetActive(true); StartCoroutine(DisableAfterTime(d, 5f)); break; } }
    }

    public void SpawnTracer(Vector3 start, Vector3 end)
    {
        foreach (LineRenderer lr in tracerPool) { if (!lr.enabled) { lr.SetPosition(0, start); lr.SetPosition(1, end); lr.enabled = true; StartCoroutine(DisableLineAfterTime(lr, 0.06f)); break; } }
    }

    public void SpawnImpact(Vector3 pos, Vector3 normal, string surfaceType)
    {
        foreach (ParticleSystem ps in impactPool)
        {
            if (!ps.gameObject.activeSelf)
            {
                ps.transform.position = pos; ps.transform.rotation = Quaternion.LookRotation(normal);
                var main = ps.main;
                if (surfaceType == "Blood") main.startColor = new Color(0.8f, 0.1f, 0.1f);
                else if (surfaceType == "Metal") main.startColor = Color.yellow;
                else if (surfaceType == "Wood") main.startColor = new Color(0.6f, 0.4f, 0.2f);
                else main.startColor = new Color(0.4f, 0.3f, 0.2f); // Dirt puff
                ps.gameObject.SetActive(true); ps.Play(); StartCoroutine(DisableAfterTime(ps.gameObject, 1f)); break;
            }
        }
    }

    public void SpawnDustCloud(Vector3 pos)
    {
        foreach (ParticleSystem ps in impactPool)
        {
            if (!ps.gameObject.activeSelf && ps.main.startColor == new Color(0.8f, 0.8f, 0.7f, 0.5f))
            {
                ps.transform.position = pos; ps.transform.rotation = Quaternion.identity;
                ps.gameObject.SetActive(true); ps.Play(); StartCoroutine(DisableAfterTime(ps.gameObject, 0.5f)); break;
            }
        }
    }

    public void TriggerMeleeImpact(Vector3 pos, Vector3 normal)
    {
        TriggerScreenShake(0.1f, 0.1f);
        TriggerHitStop(0.2f, 0.03f);
        SpawnImpact(pos, normal, "Blood");
    }


    public void SpawnDamageNumber(Vector3 pos, float damage, bool isCritical, bool isBoosted)
    {
        if (damageNumberCanvas == null) InitializeCanvas();
        GameObject textGo = null;
        foreach (GameObject t in damageTextPool) { if (!t.activeSelf) { textGo = t; textGo.SetActive(true); break; } }
        if (textGo == null) { textGo = new GameObject("DamageNumber"); textGo.transform.SetParent(damageNumberCanvas.transform, false); textGo.AddComponent<Text>(); damageTextPool.Add(textGo); }

        textGo.transform.position = pos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.2f, 0.5f), Random.Range(-0.2f, 0.2f)); textGo.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity; textGo.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        Text text = textGo.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 40; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter; text.text = Mathf.RoundToInt(damage).ToString();

        if (isCritical) text.color = Color.red; else if (isBoosted) text.color = Color.yellow; else text.color = Color.white;
        StartCoroutine(AnimateDamageNumber(textGo, text));
    }

    private IEnumerator AnimateDamageNumber(GameObject target, Text text)
    {
        Vector3 startPos = target.transform.position; Vector3 endPos = startPos + new Vector3(0, 1.5f, 0); float startTime = Time.time; float duration = 0.8f; Color startCol = text.color;
        while (Time.time - startTime < duration) { if (target == null) yield break; float t = (Time.time - startTime) / duration; target.transform.position = Vector3.Lerp(startPos, endPos, t); if (Camera.main != null) target.transform.rotation = Camera.main.transform.rotation; Color c = startCol; c.a = 1f - t; text.color = c; yield return null; }
        if (target != null) target.SetActive(false);
    }

    public void SetZoneVignetteActive(bool active) { if (zoneVignetteGo != null) { zoneVignetteGo.SetActive(active); if (active) { float alpha = 0.3f + Mathf.Sin(Time.time * 6f) * 0.2f; zoneVignetteImg.color = new Color(0.8f, 0.1f, 0.1f, alpha); } } }

    private IEnumerator DisableAfterTime(GameObject go, float t) { yield return new WaitForSeconds(t); if (go != null) go.SetActive(false); }
    private IEnumerator DisableLineAfterTime(LineRenderer lr, float t) { yield return new WaitForSeconds(t); if (lr != null) lr.enabled = false; }
    #endregion
}


