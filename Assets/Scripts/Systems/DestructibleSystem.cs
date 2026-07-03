using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// UNIQUE FEATURE: Destructible Environment.
/// Trees, rocks, walls, and buildings can be destroyed for materials and tactical advantage.
/// Destroyed objects drop crafting materials for the Trap/Barricade systems.
/// Unique to BloodRing — destructible environments.
/// </summary>
public class DestructibleObject : NetworkBehaviour
{
    public NetworkVariable<float> hp = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float maxHP = 100f;
    public string objectType = "Tree"; // Tree, Rock, Wall, Crate
    public int materialDrop = 2;

    private Renderer[] renderers;
    private Color originalColor;
    private bool initialized = false;

    public void Initialize(string type, float health, int drops)
    {
        objectType = type;
        maxHP = health;
        materialDrop = drops;
        if (IsServer) hp.Value = health;
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0) originalColor = renderers[0].material.color;
        initialized = true;
    }

    private void Start()
    {
        if (!initialized)
        {
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0) originalColor = renderers[0].material.color;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        if (isDestroyed.Value) return;
        hp.Value -= damage;
        DamageVisualClientRpc(hp.Value / maxHP);
        if (hp.Value <= 0)
        {
            isDestroyed.Value = true;
            DestroyObjectClientRpc();
        }
    }

    [ClientRpc]
    private void DamageVisualClientRpc(float hpPercent)
    {
        if (renderers == null) return;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            Color dmgColor = Color.Lerp(new Color(0.3f, 0.15f, 0.05f), originalColor, hpPercent);
            r.material.color = dmgColor;
        }

        // Hit particles
        GameObject hitFx = new GameObject("HitFX");
        hitFx.transform.position = transform.position + Vector3.up;
        ParticleSystem ps = hitFx.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = hitFx.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main; main.duration = 0.2f; main.loop = false;
        main.startColor = objectType == "Tree" ? new Color(0.2f, 0.4f, 0.1f) : new Color(0.5f, 0.5f, 0.5f);
        main.startSize = 0.3f; main.startSpeed = 3f; main.startLifetime = 0.5f;
        var emission = ps.emission; emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) }); ps.Play();
        Destroy(hitFx, 1f);
    }

    [ClientRpc]
    private void DestroyObjectClientRpc()
    {
        // Drop materials
        if (TrapSystem.Instance != null)
            TrapSystem.Instance.GatherMaterials(objectType, materialDrop);
        if (BarricadeSystem.Instance != null)
            BarricadeSystem.Instance.GatherMaterial(objectType, materialDrop);

        // Destruction VFX
        GameObject destroyFx = new GameObject("DestroyFX");
        destroyFx.transform.position = transform.position + Vector3.up;
        ParticleSystem ps = destroyFx.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = destroyFx.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main; main.duration = 0.4f; main.loop = false;
        main.startColor = objectType == "Tree" ? new Color(0.2f, 0.5f, 0.1f) : new Color(0.6f, 0.6f, 0.6f);
        main.startSize = 0.5f; main.startSpeed = 6f; main.startLifetime = 1f;
        var emission = ps.emission; emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) }); ps.Play();
        Destroy(destroyFx, 2f);

        // Shatter children
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
            if (rb == null) rb = child.gameObject.AddComponent<Rigidbody>();
            rb.AddExplosionForce(200f, transform.position - Vector3.up, 5f);
            rb.AddTorque(Random.insideUnitSphere * 80f);
        }

        // HUD notification
        if (GameHUD.Instance != null)
            GameHUD.Instance.AddKillFeedEntry("MATERIALS", "+" + materialDrop + " " + objectType + " materials gathered!");

        StartCoroutine(FadeAndRemove());
    }

    private IEnumerator FadeAndRemove()
    {
        yield return new WaitForSeconds(2f);
        // Disable collider so nobody interacts with remains
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(3f);
        if (IsServer) Destroy(gameObject);
    }
}

public class DestructibleSystem : MonoBehaviour
{
    public static DestructibleSystem Instance;
    private void Awake() { Instance = this; }

    /// <summary>
    /// Call after map generation to tag environment objects as destructible.
    /// </summary>
    public void MakeObjectDestructible(GameObject obj, string type, float hp, int materialDrop)
    {
        if (obj.GetComponent<DestructibleObject>() != null) return;

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj == null) netObj = obj.AddComponent<NetworkObject>();
        }

        DestructibleObject dobj = obj.AddComponent<DestructibleObject>();
        dobj.Initialize(type, hp, materialDrop);

        // Make sure it has a collider for hit detection
        if (obj.GetComponent<Collider>() == null)
            obj.AddComponent<BoxCollider>();
    }
}


