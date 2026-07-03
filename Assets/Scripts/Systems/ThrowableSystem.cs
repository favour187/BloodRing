using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Battle-royale-grade Grenade/Throwable system.
/// Supports Frag Grenades, Smoke Grenades, Flashbangs, Molotov, and Sticky Bombs.
/// </summary>
public enum ThrowableType
{
    FragGrenade,
    SmokeGrenade,
    Flashbang,
    Molotov,
    StickyBomb
}

[System.Serializable]
public class ThrowableData
{
    public ThrowableType type;
    public string displayName;
    public float damage;
    public float radius;
    public float fuseTime;
    public float effectDuration;
    public Color color;

    public static ThrowableData GetThrowable(ThrowableType type)
    {
        ThrowableData t = new ThrowableData();
        t.type = type;
        switch (type)
        {
            case ThrowableType.FragGrenade:
                t.displayName = "Frag Grenade"; t.damage = 100f; t.radius = 8f; t.fuseTime = 3f; t.effectDuration = 0f;
                t.color = new Color(0.3f, 0.4f, 0.2f); break;
            case ThrowableType.SmokeGrenade:
                t.displayName = "Smoke Grenade"; t.damage = 0f; t.radius = 10f; t.fuseTime = 1.5f; t.effectDuration = 15f;
                t.color = new Color(0.7f, 0.7f, 0.7f); break;
            case ThrowableType.Flashbang:
                t.displayName = "Flashbang"; t.damage = 0f; t.radius = 12f; t.fuseTime = 2f; t.effectDuration = 3f;
                t.color = Color.white; break;
            case ThrowableType.Molotov:
                t.displayName = "Molotov"; t.damage = 15f; t.radius = 6f; t.fuseTime = 0f; t.effectDuration = 8f;
                t.color = new Color(1f, 0.4f, 0f); break;
            case ThrowableType.StickyBomb:
                t.displayName = "Sticky Bomb"; t.damage = 120f; t.radius = 5f; t.fuseTime = 2.5f; t.effectDuration = 0f;
                t.color = new Color(0.8f, 0.1f, 0.1f); break;
        }
        return t;
    }
}

public class ThrownProjectile : NetworkBehaviour
{
    public ThrowableType throwableType;
    private ThrowableData data;
    private Rigidbody rb;
    private float spawnTime;
    private bool hasDetonated = false;
    private GameObject projectileMesh;
    private TrailRenderer trail;

    public void Initialize(ThrowableType type, Vector3 throwForce, string throwerName)
    {
        throwableType = type;
        data = ThrowableData.GetThrowable(type);
        spawnTime = Time.time;

        projectileMesh = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj");
        projectileMesh.name = "ProjectileMesh";
        projectileMesh.transform.SetParent(transform);
        projectileMesh.transform.localPosition = Vector3.zero;
        projectileMesh.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        Destroy(projectileMesh.GetComponent<Collider>());
        Material mat = new Material(ProceduralArt.GetSafeShader("Standard"));
        mat.color = data.color;
        projectileMesh.GetComponent<Renderer>().material = mat;

        trail = gameObject.AddComponent<TrailRenderer>();
        trail.startWidth = 0.15f;
        trail.endWidth = 0.02f;
        trail.time = 0.5f;
        trail.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        trail.startColor = data.color;
        trail.endColor = new Color(data.color.r, data.color.g, data.color.b, 0f);

        rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        rb.drag = 0.1f;
        rb.AddForce(throwForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 10f);

        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.radius = 0.15f;
        gameObject.tag = "Throwable";
    }

    private void Update()
    {
        if (hasDetonated) return;
        if (data != null && data.fuseTime > 0 && Time.time - spawnTime >= data.fuseTime)
            Detonate();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasDetonated) return;
        if (throwableType == ThrowableType.Molotov) { Detonate(); }
        else if (throwableType == ThrowableType.StickyBomb)
        { if (rb != null) { rb.isKinematic = true; rb.velocity = Vector3.zero; } transform.SetParent(collision.transform); }
    }

    private void Detonate()
    {
        if (hasDetonated) return;
        hasDetonated = true;
        if (!IsServer) return;
        DetonateClientRpc();

        switch (throwableType)
        {
            case ThrowableType.FragGrenade:
            case ThrowableType.StickyBomb:
                Collider[] hits = Physics.OverlapSphere(transform.position, data.radius);
                foreach (Collider c in hits)
                {
                    float dist = Vector3.Distance(transform.position, c.transform.position);
                    float falloff = 1f - (dist / data.radius);
                    float dmg = data.damage * Mathf.Max(0, falloff);

                    PlayerController pc = c.GetComponentInParent<PlayerController>();
                    if (pc != null) pc.RequestTakeDamageServerRpc(dmg, "Grenade", transform.position);
                    AIBot bot = c.GetComponentInParent<AIBot>();
                    if (bot != null) bot.RequestTakeDamageServerRpc(dmg, "Grenade", transform.position);

                    // Damage destructibles
                    DestructibleObject dobj = c.GetComponent<DestructibleObject>();
                    if (dobj != null) dobj.TakeDamageServerRpc(dmg);
                }
                StartCoroutine(DestroyAfter(0.5f));
                break;
            case ThrowableType.SmokeGrenade:
                StartCoroutine(SmokeEffect());
                break;
            case ThrowableType.Flashbang:
                StartCoroutine(FlashEffect());
                break;
            case ThrowableType.Molotov:
                StartCoroutine(FireEffect());
                break;
        }
    }

    [ClientRpc] private void DetonateClientRpc()
    {
        if (throwableType == ThrowableType.FragGrenade || throwableType == ThrowableType.StickyBomb)
            SpawnExplosionVFX(transform.position, data.radius, Color.yellow);
        else if (throwableType == ThrowableType.Flashbang)
            SpawnExplosionVFX(transform.position, 2f, Color.white);
        if (projectileMesh != null) projectileMesh.SetActive(false);
        if (trail != null) trail.enabled = false;
    }

    private void SpawnExplosionVFX(Vector3 pos, float radius, Color color)
    {
        GameObject fxGo = new GameObject("ExplosionFX"); fxGo.transform.position = pos;
        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = fxGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main; main.duration = 0.5f; main.loop = false; main.startColor = color;
        main.startSize = radius * 0.3f; main.startSpeed = radius; main.startLifetime = 0.8f;
        var emission = ps.emission; emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) }); ps.Play();
        Light flash = fxGo.AddComponent<Light>(); flash.color = color; flash.intensity = 10f; flash.range = radius * 2f;
        Destroy(fxGo, 2f);
    }

    private IEnumerator SmokeEffect()
    {
        GameObject smokeGo = new GameObject("SmokeCloud"); smokeGo.transform.position = transform.position + Vector3.up;
        ParticleSystem ps = smokeGo.AddComponent<ParticleSystem>();
        smokeGo.GetComponent<ParticleSystemRenderer>().material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var m = ps.main; m.duration = data.effectDuration; m.loop = true;
        m.startColor = new Color(0.8f, 0.8f, 0.8f, 0.6f); m.startSize = 3f; m.startSpeed = 0.5f; m.startLifetime = 3f;
        var e = ps.emission; e.rateOverTime = 20;
        var s = ps.shape; s.shapeType = ParticleSystemShapeType.Sphere; s.radius = data.radius * 0.5f; ps.Play();
        yield return new WaitForSeconds(data.effectDuration);
        Destroy(smokeGo); if (IsServer) Destroy(gameObject);
    }

    private IEnumerator FlashEffect() { yield return new WaitForSeconds(0.5f); if (IsServer) Destroy(gameObject); }

    private IEnumerator FireEffect()
    {
        GameObject fireGo = new GameObject("FirePool"); fireGo.transform.position = transform.position;
        ParticleSystem ps = fireGo.AddComponent<ParticleSystem>();
        fireGo.GetComponent<ParticleSystemRenderer>().material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var m = ps.main; m.duration = data.effectDuration; m.loop = true;
        m.startColor = new Color(1f, 0.4f, 0f, 0.8f); m.startSize = 1f; m.startSpeed = 2f; m.startLifetime = 0.5f;
        var e = ps.emission; e.rateOverTime = 30;
        var s = ps.shape; s.shapeType = ParticleSystemShapeType.Circle; s.radius = data.radius * 0.5f; ps.Play();
        Light fl = fireGo.AddComponent<Light>(); fl.color = new Color(1f, 0.5f, 0f); fl.intensity = 5f; fl.range = data.radius;
        float elapsed = 0f;
        while (elapsed < data.effectDuration)
        {
            elapsed += 1f;
            Collider[] inFire = Physics.OverlapSphere(fireGo.transform.position, data.radius);
            foreach (Collider c in inFire)
            {
                PlayerController pc = c.GetComponentInParent<PlayerController>(); if (pc != null) pc.RequestTakeDamageServerRpc(data.damage, "Molotov", fireGo.transform.position);
                AIBot bot = c.GetComponentInParent<AIBot>(); if (bot != null) bot.RequestTakeDamageServerRpc(data.damage, "Molotov", fireGo.transform.position);
            }
            yield return new WaitForSeconds(1f);
        }
        Destroy(fireGo); if (IsServer) Destroy(gameObject);
    }

    private IEnumerator DestroyAfter(float delay) { yield return new WaitForSeconds(delay); if (IsServer) Destroy(gameObject); }
}

public class ThrowableSystem : MonoBehaviour
{
    public static ThrowableSystem Instance;
    private void Awake() { Instance = this; }

    public void ThrowProjectile(ThrowableType type, Vector3 origin, Vector3 direction, float force, string throwerName)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;
        GameObject projGo = new GameObject("Thrown_" + type);
        projGo.transform.position = origin + new Vector3(0, 1.5f, 0);
        NetworkObject netObj = projGo.AddComponent<NetworkObject>();
        ThrownProjectile proj = projGo.AddComponent<ThrownProjectile>();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) netObj.Spawn(true);
        Vector3 throwForce = direction.normalized * force + new Vector3(0, force * 0.5f, 0);
        proj.Initialize(type, throwForce, throwerName);
    }
}


