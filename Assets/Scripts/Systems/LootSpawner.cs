using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public enum PickupType { Weapon, Ammo, HealthPack, ArmorPack, PowerUp, Attachment, Throwable }

public class PickupItem : NetworkBehaviour
{
    public PickupType pickupType;
    public string weaponName;
    public AmmoType ammoType;
    public int amount;
    public PowerType powerType;
    public string attachmentName;
    public ThrowableType throwableType;
    public WeaponRarity weaponRarity = WeaponRarity.Common;

    private void Start()
    {
        GameObject sparkleGo = new GameObject("LootSparkle"); sparkleGo.transform.SetParent(transform, false); sparkleGo.transform.localPosition = Vector3.zero;
        ParticleSystem ps = sparkleGo.AddComponent<ParticleSystem>(); ParticleSystemRenderer pr = sparkleGo.GetComponent<ParticleSystemRenderer>(); pr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        // Rarity-colored sparkle
        Color sparkleColor = Color.white;
        if (pickupType == PickupType.Weapon) sparkleColor = WeaponSkinData.GetRarityColor(weaponRarity);
        var main = ps.main; main.duration = 1f; main.loop = false; main.startColor = sparkleColor; main.startSize = 0.15f; main.startSpeed = 2f; main.startLifetime = 0.5f;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f; ps.Play();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * 60f * Time.deltaTime);
        float newY = 1f + Mathf.Sin(Time.time * 3f + GetInstanceID()) * 0.25f;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}

/// <summary>
/// Smart loot spawner with rarity-weighted weapon generation across all 62 catalog weapons.
/// Also spawns attachments, throwables, and uses the WeaponRarity system for balanced loot distribution.
/// </summary>
public class LootSpawner : MonoBehaviour
{
    public static LootSpawner Instance;

    private void Awake() { Instance = this; }

    // Rarity drop weights (Common most frequent, Legendary rarest)
    private static readonly Dictionary<WeaponRarity, float> RarityWeights = new Dictionary<WeaponRarity, float>
    {
        { WeaponRarity.Common, 0.40f },
        { WeaponRarity.Uncommon, 0.30f },
        { WeaponRarity.Rare, 0.18f },
        { WeaponRarity.Epic, 0.09f },
        { WeaponRarity.Legendary, 0.03f }
    };

    public void SpawnInitialLoot()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;
        Debug.Log("Spawning initial polished loot across BloodRing Apex Network (62 weapons, attachments, throwables)...");
        GameObject lootContainer = new GameObject("LootContainer");

        // ── WEAPONS (rarity-weighted from full 62-weapon catalog) ──
        for (int i = 0; i < 45; i++)
        {
            string wName = RollRandomWeaponByRarity();
            SpawnWeaponPickup(wName, GetRandomPosition(), lootContainer.transform);
        }

        // ── AMMO (all 6 types including EnergyAmmo) ──
        AmmoType[] allAmmoTypes = (AmmoType[])System.Enum.GetValues(typeof(AmmoType));
        for (int i = 0; i < 60; i++)
        {
            AmmoType aType = allAmmoTypes[Random.Range(0, allAmmoTypes.Length)];
            int amt = GetAmmoAmount(aType);
            SpawnAmmoPickup(aType, amt, GetRandomPosition(), lootContainer.transform);
        }

        // ── CONSUMABLES ──
        for (int i = 0; i < 25; i++) SpawnConsumablePickup(PickupType.HealthPack, 50, GetRandomPosition(), lootContainer.transform);
        for (int i = 0; i < 18; i++) SpawnConsumablePickup(PickupType.ArmorPack, 50, GetRandomPosition(), lootContainer.transform);

        // ── POWER-UPS ──
        PowerType[] powers = (PowerType[])System.Enum.GetValues(typeof(PowerType));
        for (int i = 0; i < 15; i++) SpawnPowerUpPickup(powers[Random.Range(0, powers.Length)], GetRandomPosition(), lootContainer.transform);

        // ── ATTACHMENTS (scopes, grips, muzzles, mags, stocks, barrels) ──
        List<string> allAttachments = AttachmentData.GetAllAttachmentNames();
        for (int i = 0; i < 25; i++)
        {
            string attName = allAttachments[Random.Range(0, allAttachments.Count)];
            SpawnAttachmentPickup(attName, GetRandomPosition(), lootContainer.transform);
        }

        // ── THROWABLES (Frag, Smoke, Flash, Molotov, Sticky) ──
        ThrowableType[] throwableTypes = (ThrowableType[])System.Enum.GetValues(typeof(ThrowableType));
        for (int i = 0; i < 20; i++)
        {
            ThrowableType tType = throwableTypes[Random.Range(0, throwableTypes.Length)];
            SpawnThrowablePickup(tType, GetRandomPosition(), lootContainer.transform);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  RARITY-WEIGHTED WEAPON ROLL
    // ═══════════════════════════════════════════════════════════

    /// <summary>Roll a random weapon name from the 62-weapon catalog weighted by rarity.</summary>
    public static string RollRandomWeaponByRarity()
    {
        float roll = Random.value;
        float cumulative = 0f;
        WeaponRarity targetRarity = WeaponRarity.Common;

        foreach (var kvp in RarityWeights)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative) { targetRarity = kvp.Key; break; }
        }

        List<string> pool = WeaponData.GetWeaponsByRarity(targetRarity);
        if (pool.Count == 0) pool = WeaponData.GetAllWeaponNames();
        return pool[Random.Range(0, pool.Count)];
    }

    private int GetAmmoAmount(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.PistolAmmo: return 24;
            case AmmoType.RifleAmmo: return 60;
            case AmmoType.ShotgunAmmo: return 12;
            case AmmoType.SMGAmmo: return 60;
            case AmmoType.SniperAmmo: return 10;
            case AmmoType.EnergyAmmo: return 15;
            default: return 30;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  SPAWN METHODS
    // ═══════════════════════════════════════════════════════════

    public GameObject SpawnWeaponPickup(string wName, Vector3 pos, Transform parent = null)
    {
        GameObject go = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj"); go.name = "Pickup_Weapon_" + wName; go.transform.position = pos + new Vector3(0, 1f, 0); go.transform.localScale = new Vector3(1.2f, 0.4f, 0.4f); if (parent != null) go.transform.SetParent(parent);
        Collider col = go.GetComponent<Collider>(); col.isTrigger = true;
        NetworkObject netObj = go.AddComponent<NetworkObject>(); PickupItem p = go.AddComponent<PickupItem>(); p.pickupType = PickupType.Weapon; p.weaponName = wName;
        WeaponData wData = WeaponData.GetDefaultWeapon(wName);
        p.weaponRarity = wData.rarity;
        Color c = WeaponSkinData.GetRarityColor(wData.rarity);
        go.GetComponent<Renderer>().material.color = c;
        AddMinimapBlip(go, Color.yellow); if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) netObj.Spawn(true); return go;
    }

    public GameObject SpawnAmmoPickup(AmmoType aType, int amount, Vector3 pos, Transform parent = null)
    {
        GameObject go = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj"); go.name = "Pickup_Ammo_" + aType; go.transform.position = pos + new Vector3(0, 1f, 0); go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f); if (parent != null) go.transform.SetParent(parent);
        Collider col = go.GetComponent<Collider>(); col.isTrigger = true;
        NetworkObject netObj = go.AddComponent<NetworkObject>(); PickupItem p = go.AddComponent<PickupItem>(); p.pickupType = PickupType.Ammo; p.ammoType = aType; p.amount = amount;
        Color c = aType == AmmoType.PistolAmmo ? new Color(0.8f, 0.8f, 0f) : (aType == AmmoType.RifleAmmo ? new Color(0f, 0.8f, 0.8f) : (aType == AmmoType.SMGAmmo ? Color.cyan : (aType == AmmoType.SniperAmmo ? Color.magenta : (aType == AmmoType.EnergyAmmo ? new Color(0.4f, 0f, 1f) : new Color(0.8f, 0f, 0f)))));
        go.GetComponent<Renderer>().material.color = c;
        AddMinimapBlip(go, Color.yellow); if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) netObj.Spawn(true); return go;
    }

    public GameObject SpawnConsumablePickup(PickupType type, int amount, Vector3 pos, Transform parent = null)
    {
        GameObject go = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Capsule.obj"); go.name = "Pickup_" + type; go.transform.position = pos + new Vector3(0, 1f, 0); go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); if (parent != null) go.transform.SetParent(parent);
        Collider col = go.GetComponent<Collider>(); col.isTrigger = true;
        NetworkObject netObj = go.AddComponent<NetworkObject>(); PickupItem p = go.AddComponent<PickupItem>(); p.pickupType = type; p.amount = amount; go.GetComponent<Renderer>().material.color = (type == PickupType.HealthPack) ? Color.green : Color.blue;
        AddMinimapBlip(go, Color.yellow); if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) netObj.Spawn(true); return go;
    }

    public GameObject SpawnPowerUpPickup(PowerType pType, Vector3 pos, Transform parent = null)
    {
        GameObject go = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj"); go.name = "Pickup_Power_" + pType; go.transform.position = pos + new Vector3(0, 1f, 0); go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f); if (parent != null) go.transform.SetParent(parent);
        Collider col = go.GetComponent<Collider>(); col.isTrigger = true;
        NetworkObject netObj = go.AddComponent<NetworkObject>(); PickupItem p = go.AddComponent<PickupItem>(); p.pickupType = PickupType.PowerUp; p.powerType = pType;

        Color c = Color.magenta; switch (pType) { case PowerType.SpeedBoost: c = Color.yellow; break; case PowerType.ShieldBurst: c = Color.blue; break; case PowerType.RageMode: c = Color.red; break; case PowerType.Invisibility: c = Color.gray; break; case PowerType.HealSurge: c = Color.green; break; case PowerType.DoubleJump: c = Color.cyan; break; case PowerType.Magnet: c = Color.magenta; break; }
        Material mat = new Material(ProceduralArt.GetSafeShader("Standard")); mat.color = c; mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", c * 1.5f); go.GetComponent<Renderer>().material = mat;

        GameObject lightGo = new GameObject("OrbLight"); lightGo.transform.SetParent(go.transform, false); Light light = lightGo.AddComponent<Light>(); light.type = LightType.Point; light.color = c; light.range = 5f; light.intensity = 1.5f;
        AddMinimapBlip(go, Color.yellow); if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) netObj.Spawn(true); return go;
    }

    /// <summary>Spawn an attachment pickup on the map.</summary>
    public GameObject SpawnAttachmentPickup(string attachmentName, Vector3 pos, Transform parent = null)
    {
        GameObject go = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj"); go.name = "Pickup_Attachment_" + attachmentName; go.transform.position = pos + new Vector3(0, 1f, 0); go.transform.localScale = new Vector3(0.3f, 0.15f, 0.3f); if (parent != null) go.transform.SetParent(parent);
        Collider col = go.GetComponent<Collider>(); col.isTrigger = true;
        NetworkObject netObj = go.AddComponent<NetworkObject>(); PickupItem p = go.AddComponent<PickupItem>(); p.pickupType = PickupType.Attachment; p.attachmentName = attachmentName;
        Material mat = new Material(ProceduralArt.GetSafeShader("Standard")); mat.color = new Color(0f, 0.7f, 1f); mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0f, 0.7f, 1f) * 1.2f); go.GetComponent<Renderer>().material = mat;
        AddMinimapBlip(go, new Color(0f, 0.7f, 1f)); if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) netObj.Spawn(true); return go;
    }

    /// <summary>Spawn a throwable pickup on the map.</summary>
    public GameObject SpawnThrowablePickup(ThrowableType tType, Vector3 pos, Transform parent = null)
    {
        ThrowableData tData = ThrowableData.GetThrowable(tType);
        GameObject go = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj"); go.name = "Pickup_Throwable_" + tType; go.transform.position = pos + new Vector3(0, 1f, 0); go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f); if (parent != null) go.transform.SetParent(parent);
        Collider col = go.GetComponent<Collider>(); col.isTrigger = true;
        NetworkObject netObj = go.AddComponent<NetworkObject>(); PickupItem p = go.AddComponent<PickupItem>(); p.pickupType = PickupType.Throwable; p.throwableType = tType; p.amount = 1;
        Material mat = new Material(ProceduralArt.GetSafeShader("Standard")); mat.color = tData.color; mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", tData.color * 1.3f); go.GetComponent<Renderer>().material = mat;
        AddMinimapBlip(go, tData.color); if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) netObj.Spawn(true); return go;
    }

    private void AddMinimapBlip(GameObject target, Color col) { MinimapBlip b = target.AddComponent<MinimapBlip>(); b.blipColor = col; }
    private Vector3 GetRandomPosition() { float x = Random.Range(-220f, 220f); float z = Random.Range(-220f, 220f); return new Vector3(x, 0, z); }
}


