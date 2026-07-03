using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Battle-royale-grade Ping/Quick Communication system for squad play.
/// Players can ping locations, enemies, loot, and send quick voice messages.
/// </summary>
public enum PingType
{
    GenericPing,
    EnemySpotted,
    LootHere,
    Danger,
    GoHere,
    NeedHelp,
    Attacking,
    Defending,
    WatchOut,
    GatherHere
}

[System.Serializable]
public class PingData
{
    public PingType type;
    public string message;
    public Color color;
    public float duration;

    public static PingData GetPing(PingType type)
    {
        PingData p = new PingData();
        p.type = type;
        p.duration = 8f;
        switch (type)
        {
            case PingType.GenericPing:    p.message = "Ping!";           p.color = Color.white; break;
            case PingType.EnemySpotted:   p.message = "Enemy Spotted!";  p.color = Color.red; break;
            case PingType.LootHere:       p.message = "Loot Here!";      p.color = Color.yellow; break;
            case PingType.Danger:         p.message = "⚠ DANGER!";       p.color = new Color(1f, 0.5f, 0f); break;
            case PingType.GoHere:         p.message = "Go Here →";       p.color = Color.blue; break;
            case PingType.NeedHelp:       p.message = "Need Help!";      p.color = Color.green; break;
            case PingType.Attacking:      p.message = "Attacking!";      p.color = Color.red; break;
            case PingType.Defending:      p.message = "Defending!";      p.color = Color.cyan; break;
            case PingType.WatchOut:       p.message = "Watch Out!";      p.color = new Color(1f, 0.3f, 0f); break;
            case PingType.GatherHere:     p.message = "Gather Here!";    p.color = Color.green; break;
        }
        return p;
    }
}

public class WorldPing : MonoBehaviour
{
    private TextMesh pingText;
    private Light pingLight;
    private ParticleSystem pingParticles;
    private float lifetime;
    private float spawnTime;
    private string senderName;

    public void Initialize(PingData data, string sender, Vector3 worldPos)
    {
        senderName = sender;
        lifetime = data.duration;
        spawnTime = Time.time;
        transform.position = worldPos;

        // Ping marker (vertical beam)
        GameObject beam = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
        beam.name = "PingBeam";
        beam.transform.SetParent(transform);
        beam.transform.localPosition = new Vector3(0, 10f, 0);
        beam.transform.localScale = new Vector3(0.3f, 10f, 0.3f);
        Destroy(beam.GetComponent<Collider>());
        Material beamMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        beamMat.color = new Color(data.color.r, data.color.g, data.color.b, 0.5f);
        beamMat.SetFloat("_Mode", 3);
        beamMat.SetInt("_ZWrite", 0);
        beamMat.renderQueue = 3000;
        beam.GetComponent<Renderer>().material = beamMat;

        // Diamond marker at base
        GameObject diamond = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        diamond.name = "PingDiamond";
        diamond.transform.SetParent(transform);
        diamond.transform.localPosition = new Vector3(0, 1.5f, 0);
        diamond.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        diamond.transform.rotation = Quaternion.Euler(45, 0, 45);
        Destroy(diamond.GetComponent<Collider>());
        Material diamondMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        diamondMat.color = data.color;
        diamondMat.SetFloat("_Glossiness", 0.9f);
        diamond.GetComponent<Renderer>().material = diamondMat;

        // Text label
        GameObject textGo = new GameObject("PingText");
        textGo.transform.SetParent(transform);
        textGo.transform.localPosition = new Vector3(0, 3.5f, 0);
        pingText = textGo.AddComponent<TextMesh>();
        pingText.text = sender + ": " + data.message;
        pingText.fontSize = 36;
        pingText.characterSize = 0.08f;
        pingText.color = data.color;
        pingText.anchor = TextAnchor.MiddleCenter;
        pingText.alignment = TextAlignment.Center;

        // Light
        GameObject lightGo = new GameObject("PingLight");
        lightGo.transform.SetParent(transform);
        lightGo.transform.localPosition = new Vector3(0, 2f, 0);
        pingLight = lightGo.AddComponent<Light>();
        pingLight.color = data.color;
        pingLight.intensity = 4f;
        pingLight.range = 15f;

        // Ping particles
        GameObject psGo = new GameObject("PingParticles");
        psGo.transform.SetParent(transform);
        psGo.transform.localPosition = new Vector3(0, 1f, 0);
        pingParticles = psGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = psGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = pingParticles.main;
        main.loop = true;
        main.startColor = new Color(data.color.r, data.color.g, data.color.b, 0.6f);
        main.startSize = 0.3f;
        main.startSpeed = 2f;
        main.startLifetime = 1f;
        var emission = pingParticles.emission;
        emission.rateOverTime = 10;

        // Minimap blip
        MinimapBlip blip = gameObject.AddComponent<MinimapBlip>();
        blip.blipColor = data.color;

        // Audio notification
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }

    private void Update()
    {
        // Fade out and destroy
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Billboard text
        if (pingText != null && Camera.main != null)
        {
            pingText.transform.LookAt(Camera.main.transform);
            pingText.transform.Rotate(0, 180, 0);
        }

        // Pulse light
        if (pingLight != null)
        {
            float pulse = Mathf.Abs(Mathf.Sin(Time.time * 3f));
            pingLight.intensity = 2f + pulse * 3f;
        }

        // Rotate diamond
        Transform diamond = transform.Find("PingDiamond");
        if (diamond != null) diamond.Rotate(Vector3.up * 90f * Time.deltaTime);

        // Fade alpha near end
        float remaining = lifetime - (Time.time - spawnTime);
        if (remaining < 2f && pingText != null)
        {
            Color c = pingText.color;
            c.a = remaining / 2f;
            pingText.color = c;
        }
    }
}

public class PingSystem : MonoBehaviour
{
    public static PingSystem Instance;
    private List<WorldPing> activePings = new List<WorldPing>();

    private void Awake() { Instance = this; }

    public void CreatePing(PingType type, Vector3 worldPosition, string senderName)
    {
        PingData data = PingData.GetPing(type);
        GameObject pingGo = new GameObject("Ping_" + type + "_" + Time.time);
        WorldPing ping = pingGo.AddComponent<WorldPing>();
        ping.Initialize(data, senderName, worldPosition);
        activePings.Add(ping);

        // Show in HUD kill feed
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.AddKillFeedEntry(senderName, data.message);
        }
    }

    /// <summary>
    /// Raycast from camera to world and create ping at hit point.
    /// </summary>
    public void PingFromCamera(PingType type, string senderName)
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 500f))
        {
            CreatePing(type, hit.point, senderName);
        }
        else
        {
            // Ping at max distance in look direction
            CreatePing(type, ray.origin + ray.direction * 100f, senderName);
        }
    }
}


