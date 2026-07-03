using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Interactive map objects: destructible doors, windows, breakable cover,
/// loot crates, zip-lines, launch pads, and environmental hazards.
/// Each object has health, can be damaged, and triggers effects on destruction.
/// </summary>
public enum InteractiveType
{
    Door,
    Window,
    BreakableCover,
    LootCrate,
    ExplosiveBarrel,
    VendingMachine,
    LaunchPad,
    ZiplineAnchor,
    HealthStation,
    AmmoCrate
}

[System.Serializable]
public class InteractiveObjectData
{
    public InteractiveType type;
    public string displayName;
    public float maxHealth;
    public bool isDestructible;
    public bool isInteractable;
    public Color baseColor;
    public float interactRadius;

    public static InteractiveObjectData GetDefault(InteractiveType type)
    {
        InteractiveObjectData d = new InteractiveObjectData();
        d.type = type;

        switch (type)
        {
            case InteractiveType.Door:
                d.displayName = "Door"; d.maxHealth = 80f; d.isDestructible = true; d.isInteractable = true;
                d.baseColor = new Color(0.35f, 0.25f, 0.15f); d.interactRadius = 2.5f; break;

            case InteractiveType.Window:
                d.displayName = "Window"; d.maxHealth = 30f; d.isDestructible = true; d.isInteractable = false;
                d.baseColor = new Color(0.6f, 0.8f, 0.95f, 0.5f); d.interactRadius = 1.5f; break;

            case InteractiveType.BreakableCover:
                d.displayName = "Cover"; d.maxHealth = 120f; d.isDestructible = true; d.isInteractable = false;
                d.baseColor = new Color(0.5f, 0.5f, 0.45f); d.interactRadius = 3f; break;

            case InteractiveType.LootCrate:
                d.displayName = "Loot Crate"; d.maxHealth = 60f; d.isDestructible = true; d.isInteractable = true;
                d.baseColor = new Color(0.8f, 0.65f, 0.1f); d.interactRadius = 2f; break;

            case InteractiveType.ExplosiveBarrel:
                d.displayName = "Explosive Barrel"; d.maxHealth = 40f; d.isDestructible = true; d.isInteractable = false;
                d.baseColor = new Color(0.7f, 0.15f, 0.1f); d.interactRadius = 1.5f; break;

            case InteractiveType.VendingMachine:
                d.displayName = "Vending Machine"; d.maxHealth = 150f; d.isDestructible = true; d.isInteractable = true;
                d.baseColor = new Color(0.2f, 0.5f, 0.9f); d.interactRadius = 2.5f; break;

            case InteractiveType.LaunchPad:
                d.displayName = "Launch Pad"; d.maxHealth = 9999f; d.isDestructible = false; d.isInteractable = true;
                d.baseColor = new Color(0.9f, 0.7f, 0f); d.interactRadius = 3f; break;

            case InteractiveType.ZiplineAnchor:
                d.displayName = "Zipline"; d.maxHealth = 9999f; d.isDestructible = false; d.isInteractable = true;
                d.baseColor = new Color(0.6f, 0.6f, 0.6f); d.interactRadius = 2f; break;

            case InteractiveType.HealthStation:
                d.displayName = "Health Station"; d.maxHealth = 100f; d.isDestructible = true; d.isInteractable = true;
                d.baseColor = new Color(0.1f, 0.8f, 0.2f); d.interactRadius = 2.5f; break;

            case InteractiveType.AmmoCrate:
                d.displayName = "Ammo Crate"; d.maxHealth = 50f; d.isDestructible = true; d.isInteractable = true;
                d.baseColor = new Color(0.6f, 0.5f, 0.2f); d.interactRadius = 2f; break;
        }
        return d;
    }
}

/// <summary>
/// Runtime interactive object placed on the map.
/// Handles damage, destruction, interaction, and visual feedback.
/// </summary>
public class InteractiveObject : NetworkBehaviour
{
    public InteractiveType objectType = InteractiveType.Door;
    public InteractiveObjectData data;

    public NetworkVariable<float> netHealth = new NetworkVariable<float>(100f);
    public NetworkVariable<bool> netIsDestroyed = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> netIsOpen = new NetworkVariable<bool>(false);

    private GameObject visualMesh;
    private Collider interactCollider;

    private void Awake()
    {
        if (data == null) data = InteractiveObjectData.GetDefault(objectType);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) netHealth.Value = data.maxHealth;
        CreateVisual();
    }

    /// <summary>Take damage from weapons, explosions, vehicles.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage, string attackerName)
    {
        if (!data.isDestructible || netIsDestroyed.Value) return;

        netHealth.Value -= damage;
        TakeDamageClientRpc(damage);

        if (netHealth.Value <= 0)
        {
            netHealth.Value = 0;
            netIsDestroyed.Value = true;
            OnDestroyedClientRpc();
        }
    }

    [ClientRpc]
    private void TakeDamageClientRpc(float damage)
    {
        // Visual feedback — flash, crack effect
        if (visualMesh != null)
        {
            Renderer r = visualMesh.GetComponent<Renderer>();
            if (r != null)
            {
                float healthRatio = netHealth.Value / data.maxHealth;
                // Darken as health decreases
                Color damaged = data.baseColor * Mathf.Lerp(0.5f, 1f, healthRatio);
                r.material.color = damaged;
            }
        }
    }

    [ClientRpc]
    private void OnDestroyedClientRpc()
    {
        // Destruction VFX
        SpawnDestructionEffect();

        // Disable visual and collider
        if (visualMesh != null) visualMesh.SetActive(false);
        if (interactCollider != null) interactCollider.enabled = false;

        // Explosive barrels deal area damage
        if (objectType == InteractiveType.ExplosiveBarrel)
        {
            SpawnExplosion();
        }
    }

    /// <summary>Interact with the object (open door, use vending machine, etc.)</summary>
    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc(ulong interactClientId)
    {
        if (!data.isInteractable || netIsDestroyed.Value) return;

        switch (objectType)
        {
            case InteractiveType.Door:
                netIsOpen.Value = !netIsOpen.Value;
                InteractDoorClientRpc(netIsOpen.Value);
                break;

            case InteractiveType.LootCrate:
                SpawnLootClientRpc();
                netIsDestroyed.Value = true;
                break;

            case InteractiveType.VendingMachine:
                VendingPurchaseClientRpc(interactClientId);
                break;

            case InteractiveType.HealthStation:
                HealPlayerServerRpc(interactClientId);
                break;

            case InteractiveType.AmmoCrate:
                GiveAmmoClientRpc(interactClientId);
                break;

            case InteractiveType.LaunchPad:
                LaunchPlayerClientRpc(interactClientId);
                break;
        }
    }

    [ClientRpc]
    private void InteractDoorClientRpc(bool isOpen)
    {
        if (visualMesh != null)
        {
            float angle = isOpen ? 90f : 0f;
            visualMesh.transform.localRotation = Quaternion.Euler(0, angle, 0);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_WoodBreak");
    }

    [ClientRpc]
    private void SpawnLootClientRpc()
    {
        // Spawn 2-4 random loot items at crate position
        if (LootSpawner.Instance != null)
        {
            for (int i = 0; i < Random.Range(2, 5); i++)
            {
                Vector3 offset = Random.insideUnitSphere * 1.5f;
                offset.y = 1f;
                string weaponName = LootSpawner.RollRandomWeaponByRarity();
                LootSpawner.Instance.SpawnWeaponPickup(weaponName, transform.position + offset);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void VendingPurchaseClientRpc(ulong clientId)
    {
        // Vending machines give random consumable
        if (GameHUD.Instance != null) GameHUD.Instance.ShowToast("Vending Machine: Health Pack dispensed");
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealPlayerServerRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            PlayerController pc = client.PlayerObject?.GetComponent<PlayerController>();
            if (pc != null) pc.HealHP(25f);
        }
    }

    [ClientRpc]
    private void GiveAmmoClientRpc(ulong clientId)
    {
        if (GameHUD.Instance != null) GameHUD.Instance.ShowToast("Ammo Crate: Ammo replenished");
    }

    [ClientRpc]
    private void LaunchPlayerClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            PlayerController pc = PlayerController.Instance;
            if (pc != null)
            {
                // Launch pad boost — upward + forward force
                Rigidbody rb = pc.GetComponent<Rigidbody>();
                if (rb != null) rb.AddForce(Vector3.up * 30f + pc.transform.forward * 15f, ForceMode.VelocityChange);
            }
        }
    }

    private void SpawnDestructionEffect()
    {
        GameObject fx = new GameObject("DestructFX_" + objectType);
        fx.transform.position = transform.position;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.startColor = data.baseColor;
        main.startSize = 0.3f;
        main.startSpeed = 5f;
        main.startLifetime = 0.8f;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        ps.Play();
        Destroy(fx, 2f);

        if (AudioManager.Instance != null)
        {
            if (objectType == InteractiveType.Window) AudioManager.Instance.PlaySFX("SFX_GlassBreak");
            else AudioManager.Instance.PlaySFX("SFX_WoodBreak");
        }
    }

    private void SpawnExplosion()
    {
        // Explosive barrel explosion — area damage
        Collider[] hits = Physics.OverlapSphere(transform.position, 8f);
        foreach (Collider c in hits)
        {
            PlayerController pc = c.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                float dist = Vector3.Distance(transform.position, c.transform.position);
                float damage = 100f * Mathf.Max(0, 1f - dist / 8f);
                pc.RequestTakeDamageServerRpc(damage, "Explosive Barrel", transform.position);
            }
            AIBot bot = c.GetComponentInParent<AIBot>();
            if (bot != null)
            {
                float dist = Vector3.Distance(transform.position, c.transform.position);
                float damage = 100f * Mathf.Max(0, 1f - dist / 8f);
                bot.RequestTakeDamageServerRpc(damage, "Explosive Barrel", transform.position);
            }
            // Chain-explosion other barrels
            InteractiveObject other = c.GetComponent<InteractiveObject>();
            if (other != null && other.objectType == InteractiveType.ExplosiveBarrel && other != this)
            {
                other.TakeDamageServerRpc(200f, "Chain Explosion");
            }
        }

        // Big explosion VFX
        GameObject fx = new GameObject("BarrelExplosion");
        fx.transform.position = transform.position;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.8f;
        main.startColor = new Color(1f, 0.6f, 0f);
        main.startSize = 4f;
        main.startSpeed = 15f;
        main.startLifetime = 1f;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 80) });
        ps.Play();

        Light flash = fx.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.7f, 0f);
        flash.intensity = 15f;
        flash.range = 20f;
        Destroy(fx, 2f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Explosion");
    }

    private void CreateVisual()
    {
        string primitive = "Cube.obj";
        float scaleX = 1f, scaleY = 2f, scaleZ = 0.2f;

        switch (objectType)
        {
            case InteractiveType.Door: scaleX = 1.2f; scaleY = 2.5f; scaleZ = 0.15f; break;
            case InteractiveType.Window: scaleX = 1.5f; scaleY = 1f; scaleZ = 0.1f; break;
            case InteractiveType.BreakableCover: scaleX = 2f; scaleY = 1.2f; scaleZ = 0.8f; break;
            case InteractiveType.LootCrate: primitive = "Cube.obj"; scaleX = 0.8f; scaleY = 0.6f; scaleZ = 0.8f; break;
            case InteractiveType.ExplosiveBarrel: primitive = "Cylinder.obj"; scaleX = 0.6f; scaleY = 1f; scaleZ = 0.6f; break;
            case InteractiveType.VendingMachine: scaleX = 1.2f; scaleY = 2f; scaleZ = 0.8f; break;
            case InteractiveType.LaunchPad: primitive = "Cylinder.obj"; scaleX = 2f; scaleY = 0.15f; scaleZ = 2f; break;
            case InteractiveType.HealthStation: primitive = "Capsule.obj"; scaleX = 0.6f; scaleY = 1f; scaleZ = 0.6f; break;
            case InteractiveType.AmmoCrate: scaleX = 0.7f; scaleY = 0.5f; scaleZ = 0.7f; break;
        }

        visualMesh = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D(primitive);
        visualMesh.name = objectType.ToString();
        visualMesh.transform.SetParent(transform);
        visualMesh.transform.localPosition = Vector3.zero;
        visualMesh.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

        Material mat = new Material(ProceduralArt.GetSafeShader("Standard"));
        mat.color = data.baseColor;

        // Emissive for special objects
        if (objectType == InteractiveType.LaunchPad || objectType == InteractiveType.HealthStation)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", data.baseColor * 0.8f);
        }

        // Transparent for windows
        if (objectType == InteractiveType.Window)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }

        visualMesh.GetComponent<Renderer>().material = mat;

        // Add collider for interaction
        interactCollider = visualMesh.GetComponent<Collider>();
        if (interactCollider == null)
        {
            BoxCollider box = visualMesh.AddComponent<BoxCollider>();
            box.size = new Vector3(scaleX, scaleY, scaleZ);
            interactCollider = box;
        }
    }

    /// <summary>Get interaction prompt text for HUD.</summary>
    public string GetInteractPrompt()
    {
        if (netIsDestroyed.Value) return "";
        if (!data.isInteractable) return "";

        switch (objectType)
        {
            case InteractiveType.Door: return netIsOpen.Value ? "CLOSE DOOR" : "OPEN DOOR";
            case InteractiveType.LootCrate: return "OPEN CRATE";
            case InteractiveType.VendingMachine: return "USE VENDING MACHINE";
            case InteractiveType.HealthStation: return "USE HEALTH STATION";
            case InteractiveType.AmmoCrate: return "LOOT AMMO";
            case InteractiveType.LaunchPad: return "LAUNCH";
            case InteractiveType.ZiplineAnchor: return "USE ZIPLINE";
            default: return "INTERACT";
        }
    }
}
