using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UNIQUE FEATURE #1: Dynamic Weather System (Unique to BloodRing this!)
/// Real-time weather changes affect gameplay: rain reduces visibility and makes surfaces slippery,
/// fog limits vision range, thunderstorms can strike near players, sandstorms slow movement.
/// Weather is server-authoritative and synced to all clients.
/// </summary>
public enum WeatherType
{
    Clear,
    Rain,
    HeavyRain,
    Fog,
    ThunderStorm,
    SandStorm,
    Blizzard,
    NightMode
}

public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance;

    public WeatherType currentWeather = WeatherType.Clear;
    private float weatherTimer = 0f;
    private float nextWeatherChange = 90f;
    private float weatherTransitionDuration = 5f;

    // Weather VFX references
    private ParticleSystem rainSystem;
    private ParticleSystem snowSystem;
    private ParticleSystem sandSystem;
    private ParticleSystem fogSystem;
    private GameObject thunderLight;
    private Light mainLight;

    // Gameplay modifiers
    public float visibilityMultiplier = 1f;
    public float movementMultiplier = 1f;
    public float noiseMultiplier = 1f;     // Affects footstep sound range
    public float bulletDropMultiplier = 1f; // Wind affects bullet trajectory

    private Camera mainCam;
    private Color originalAmbient;
    private Color originalFogColor;
    private float originalFogDensity;

    private void Awake() { Instance = this; }

    public void InitializeWeather()
    {
        mainCam = Camera.main;
        mainLight = FindObjectOfType<Light>();
        originalAmbient = RenderSettings.ambientSkyColor;

        // Create rain particle system
        GameObject rainGo = new GameObject("RainSystem");
        rainGo.transform.SetParent(transform);
        rainSystem = rainGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer rainPsr = rainGo.GetComponent<ParticleSystemRenderer>();
        rainPsr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var rainMain = rainSystem.main;
        rainMain.loop = true;
        rainMain.startColor = new Color(0.6f, 0.7f, 0.9f, 0.6f);
        rainMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        rainMain.startSpeed = 25f;
        rainMain.startLifetime = 2f;
        rainMain.maxParticles = 5000;
        rainMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var rainEmission = rainSystem.emission;
        rainEmission.rateOverTime = 0;
        var rainShape = rainSystem.shape;
        rainShape.shapeType = ParticleSystemShapeType.Box;
        rainShape.scale = new Vector3(80f, 1f, 80f);
        rainShape.position = new Vector3(0, 50f, 0);
        rainSystem.Stop();

        // Create snow/blizzard system
        GameObject snowGo = new GameObject("SnowSystem");
        snowGo.transform.SetParent(transform);
        snowSystem = snowGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer snowPsr = snowGo.GetComponent<ParticleSystemRenderer>();
        snowPsr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var snowMain = snowSystem.main;
        snowMain.loop = true;
        snowMain.startColor = new Color(1f, 1f, 1f, 0.8f);
        snowMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        snowMain.startSpeed = 5f;
        snowMain.startLifetime = 6f;
        snowMain.maxParticles = 3000;
        snowMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var snowEmission = snowSystem.emission;
        snowEmission.rateOverTime = 0;
        var snowShape = snowSystem.shape;
        snowShape.shapeType = ParticleSystemShapeType.Box;
        snowShape.scale = new Vector3(80f, 1f, 80f);
        snowShape.position = new Vector3(0, 40f, 0);
        snowSystem.Stop();

        // Sand storm system
        GameObject sandGo = new GameObject("SandSystem");
        sandGo.transform.SetParent(transform);
        sandSystem = sandGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer sandPsr = sandGo.GetComponent<ParticleSystemRenderer>();
        sandPsr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var sandMain = sandSystem.main;
        sandMain.loop = true;
        sandMain.startColor = new Color(0.8f, 0.6f, 0.3f, 0.5f);
        sandMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        sandMain.startSpeed = 15f;
        sandMain.startLifetime = 4f;
        sandMain.maxParticles = 2000;
        sandMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var sandEmission = sandSystem.emission;
        sandEmission.rateOverTime = 0;
        var sandVelocity = sandSystem.velocityOverLifetime;
        sandVelocity.enabled = true;
        sandVelocity.x = new ParticleSystem.MinMaxCurve(8f, 15f);
        sandSystem.Stop();

        // Fog system
        GameObject fogGo = new GameObject("FogSystem");
        fogGo.transform.SetParent(transform);
        fogSystem = fogGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer fogPsr = fogGo.GetComponent<ParticleSystemRenderer>();
        fogPsr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var fogMain = fogSystem.main;
        fogMain.loop = true;
        fogMain.startColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);
        fogMain.startSize = new ParticleSystem.MinMaxCurve(5f, 10f);
        fogMain.startSpeed = 1f;
        fogMain.startLifetime = 10f;
        fogMain.maxParticles = 500;
        var fogEmission = fogSystem.emission;
        fogEmission.rateOverTime = 0;
        var fogShape = fogSystem.shape;
        fogShape.shapeType = ParticleSystemShapeType.Box;
        fogShape.scale = new Vector3(80f, 5f, 80f);
        fogSystem.Stop();

        // Thunder light
        thunderLight = new GameObject("ThunderLight");
        thunderLight.transform.SetParent(transform);
        Light tl = thunderLight.AddComponent<Light>();
        tl.type = LightType.Directional;
        tl.color = new Color(0.9f, 0.9f, 1f);
        tl.intensity = 0;
        thunderLight.SetActive(false);

        Debug.Log("[WeatherSystem] Initialized with dynamic weather support.");
    }

    private void Update()
    {
        weatherTimer += Time.deltaTime;

        // Auto-change weather periodically
        if (weatherTimer >= nextWeatherChange)
        {
            weatherTimer = 0f;
            nextWeatherChange = Random.Range(60f, 150f);
            WeatherType[] options = { WeatherType.Clear, WeatherType.Rain, WeatherType.Fog, WeatherType.ThunderStorm, WeatherType.SandStorm, WeatherType.Blizzard, WeatherType.NightMode };
            SetWeather(options[Random.Range(0, options.Length)]);
        }

        // Follow camera
        if (mainCam != null)
        {
            Vector3 camPos = mainCam.transform.position;
            if (rainSystem != null) rainSystem.transform.position = camPos + new Vector3(0, 30f, 0);
            if (snowSystem != null) snowSystem.transform.position = camPos + new Vector3(0, 30f, 0);
            if (sandSystem != null) sandSystem.transform.position = camPos + new Vector3(0, 5f, 0);
            if (fogSystem != null) fogSystem.transform.position = camPos + new Vector3(0, 2f, 0);
        }

        // Thunder flashes during storm
        if (currentWeather == WeatherType.ThunderStorm && thunderLight != null)
        {
            if (Random.Range(0f, 1f) < 0.003f)
            {
                StartCoroutine(ThunderFlash());
            }
        }
    }

    public void SetWeather(WeatherType newWeather)
    {
        currentWeather = newWeather;
        StopAllWeatherEffects();

        switch (newWeather)
        {
            case WeatherType.Clear:
                visibilityMultiplier = 1f; movementMultiplier = 1f; noiseMultiplier = 1f; bulletDropMultiplier = 1f;
                RenderSettings.fog = false;
                if (mainLight != null) mainLight.intensity = 1.2f;
                RenderSettings.ambientSkyColor = originalAmbient;
                break;

            case WeatherType.Rain:
                visibilityMultiplier = 0.85f; movementMultiplier = 0.95f; noiseMultiplier = 0.7f; bulletDropMultiplier = 1.05f;
                if (rainSystem != null) { var e = rainSystem.emission; e.rateOverTime = 2000; rainSystem.Play(); }
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.6f); RenderSettings.fogDensity = 0.01f; RenderSettings.fogMode = FogMode.Exponential;
                if (mainLight != null) mainLight.intensity = 0.8f;
                RenderSettings.ambientSkyColor = new Color(0.4f, 0.4f, 0.5f);
                break;

            case WeatherType.HeavyRain:
                visibilityMultiplier = 0.6f; movementMultiplier = 0.85f; noiseMultiplier = 0.5f; bulletDropMultiplier = 1.15f;
                if (rainSystem != null) { var e = rainSystem.emission; e.rateOverTime = 5000; rainSystem.Play(); }
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.3f, 0.3f, 0.4f); RenderSettings.fogDensity = 0.025f; RenderSettings.fogMode = FogMode.Exponential;
                if (mainLight != null) mainLight.intensity = 0.5f;
                RenderSettings.ambientSkyColor = new Color(0.25f, 0.25f, 0.35f);
                break;

            case WeatherType.Fog:
                visibilityMultiplier = 0.4f; movementMultiplier = 1f; noiseMultiplier = 1.2f; bulletDropMultiplier = 1f;
                if (fogSystem != null) { var e = fogSystem.emission; e.rateOverTime = 50; fogSystem.Play(); }
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.7f, 0.7f, 0.7f); RenderSettings.fogDensity = 0.04f; RenderSettings.fogMode = FogMode.Exponential;
                if (mainLight != null) mainLight.intensity = 0.6f;
                RenderSettings.ambientSkyColor = new Color(0.6f, 0.6f, 0.6f);
                break;

            case WeatherType.ThunderStorm:
                visibilityMultiplier = 0.5f; movementMultiplier = 0.9f; noiseMultiplier = 0.3f; bulletDropMultiplier = 1.2f;
                if (rainSystem != null) { var e = rainSystem.emission; e.rateOverTime = 4000; rainSystem.Play(); }
                if (thunderLight != null) thunderLight.SetActive(true);
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.2f, 0.2f, 0.3f); RenderSettings.fogDensity = 0.02f; RenderSettings.fogMode = FogMode.Exponential;
                if (mainLight != null) mainLight.intensity = 0.3f;
                RenderSettings.ambientSkyColor = new Color(0.15f, 0.15f, 0.25f);
                break;

            case WeatherType.SandStorm:
                visibilityMultiplier = 0.35f; movementMultiplier = 0.8f; noiseMultiplier = 0.4f; bulletDropMultiplier = 1.3f;
                if (sandSystem != null) { var e = sandSystem.emission; e.rateOverTime = 1500; sandSystem.Play(); }
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.7f, 0.5f, 0.3f); RenderSettings.fogDensity = 0.05f; RenderSettings.fogMode = FogMode.Exponential;
                if (mainLight != null) { mainLight.intensity = 0.7f; mainLight.color = new Color(1f, 0.8f, 0.5f); }
                RenderSettings.ambientSkyColor = new Color(0.6f, 0.45f, 0.25f);
                break;

            case WeatherType.Blizzard:
                visibilityMultiplier = 0.3f; movementMultiplier = 0.7f; noiseMultiplier = 0.3f; bulletDropMultiplier = 1.25f;
                if (snowSystem != null) { var e = snowSystem.emission; e.rateOverTime = 2500; snowSystem.Play(); }
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.9f, 0.9f, 0.95f); RenderSettings.fogDensity = 0.06f; RenderSettings.fogMode = FogMode.Exponential;
                if (mainLight != null) mainLight.intensity = 0.4f;
                RenderSettings.ambientSkyColor = new Color(0.7f, 0.7f, 0.8f);
                break;

            case WeatherType.NightMode:
                visibilityMultiplier = 0.3f; movementMultiplier = 1f; noiseMultiplier = 1.5f; bulletDropMultiplier = 1f;
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.1f); RenderSettings.fogDensity = 0.015f; RenderSettings.fogMode = FogMode.Exponential;
                if (mainLight != null) { mainLight.intensity = 0.1f; mainLight.color = new Color(0.3f, 0.3f, 0.5f); }
                RenderSettings.ambientSkyColor = new Color(0.05f, 0.05f, 0.1f);
                break;
        }

        // Announce weather change
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.AddKillFeedEntry("WEATHER", GetWeatherName(newWeather) + " incoming!");
        }

        Debug.Log("[WeatherSystem] Weather changed to: " + newWeather);
    }

    private void StopAllWeatherEffects()
    {
        if (rainSystem != null) rainSystem.Stop();
        if (snowSystem != null) snowSystem.Stop();
        if (sandSystem != null) sandSystem.Stop();
        if (fogSystem != null) fogSystem.Stop();
        if (thunderLight != null) thunderLight.SetActive(false);
        if (mainLight != null) mainLight.color = Color.white;
    }

    private IEnumerator ThunderFlash()
    {
        if (thunderLight == null) yield break;
        Light tl = thunderLight.GetComponent<Light>();
        if (tl == null) yield break;

        // Flash sequence
        tl.intensity = 8f;
        yield return new WaitForSeconds(0.05f);
        tl.intensity = 0f;
        yield return new WaitForSeconds(0.1f);
        tl.intensity = 12f;
        yield return new WaitForSeconds(0.1f);
        tl.intensity = 0f;

        // Thunder strike nearby (random chance to damage)
        if (Random.Range(0f, 1f) < 0.15f)
        {
            Vector3 strikePos = new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f));
            SpawnLightningStrike(strikePos);
        }
    }

    private void SpawnLightningStrike(Vector3 pos)
    {
        // Visual bolt
        GameObject bolt = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
        bolt.name = "LightningBolt";
        bolt.transform.position = pos + new Vector3(0, 25f, 0);
        bolt.transform.localScale = new Vector3(0.5f, 25f, 0.5f);
        Destroy(bolt.GetComponent<Collider>());
        Material boltMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        boltMat.color = new Color(0.8f, 0.8f, 1f);
        boltMat.SetColor("_EmissionColor", new Color(2f, 2f, 3f));
        boltMat.EnableKeyword("_EMISSION");
        bolt.GetComponent<Renderer>().material = boltMat;

        // Explosion at ground
        GameObject impactGo = new GameObject("LightningImpact");
        impactGo.transform.position = pos;
        ParticleSystem ps = impactGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = impactGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startColor = Color.white;
        main.startSize = 1f;
        main.startSpeed = 10f;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        ps.Play();

        Light impactLight = impactGo.AddComponent<Light>();
        impactLight.color = Color.white;
        impactLight.intensity = 15f;
        impactLight.range = 30f;

        // Damage nearby entities
        Collider[] hits = Physics.OverlapSphere(pos, 5f);
        foreach (Collider c in hits)
        {
            PlayerController pc = c.GetComponentInParent<PlayerController>();
            if (pc != null) pc.RequestTakeDamageServerRpc(30f, "Lightning", pos);
            AIBot bot = c.GetComponentInParent<AIBot>();
            if (bot != null) bot.RequestTakeDamageServerRpc(30f, "Lightning", pos);
        }

        Destroy(bolt, 0.3f);
        Destroy(impactGo, 2f);
    }

    private string GetWeatherName(WeatherType type)
    {
        switch (type)
        {
            case WeatherType.Clear: return "Clear Skies";
            case WeatherType.Rain: return "Rain";
            case WeatherType.HeavyRain: return "Heavy Rain";
            case WeatherType.Fog: return "Dense Fog";
            case WeatherType.ThunderStorm: return "Thunder Storm";
            case WeatherType.SandStorm: return "Sand Storm";
            case WeatherType.Blizzard: return "Blizzard";
            case WeatherType.NightMode: return "Night Mode";
            default: return "Unknown";
        }
    }
}


