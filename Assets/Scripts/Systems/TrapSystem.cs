using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UNIQUE FEATURE #2: Trap Crafting System (Unique to BloodRing this!)
/// Players can craft and deploy traps from collected materials.
/// Traps include: Spike Trap, Land Mine, Tripwire Alarm, Bear Trap, EMP Trap, Decoy.
/// Materials can be gathered from the environment and combined.
/// </summary>
public enum TrapType
{
    SpikeTrap,      // Damage + slow when stepped on
    LandMine,       // Explosive on proximity
    TripwireAlarm,  // Alerts deployer of enemy location
    BearTrap,       // Immobilizes target for 3 seconds
    EMPTrap,        // Disables nearby electronics/vehicles
    DecoyTrap,      // Creates a fake player hologram
    PoisonTrap,     // DOT poison cloud on trigger
    NetTrap         // Slows and partially blinds
}

[System.Serializable]
public class TrapRecipe
{
    public TrapType type;
    public string displayName;
    public int metalCost;
    public int woodCost;
    public int techCost;
    public float damage;
    public float radius;
    public float duration;
    public Color color;

    public static TrapRecipe GetRecipe(TrapType type)
    {
        TrapRecipe r = new TrapRecipe();
        r.type = type;
        switch (type)
        {
            case TrapType.SpikeTrap:
                r.displayName = "Spike Trap"; r.metalCost = 2; r.woodCost = 1; r.techCost = 0;
                r.damage = 40f; r.radius = 2f; r.duration = 120f; r.color = new Color(0.5f, 0.5f, 0.5f); break;
            case TrapType.LandMine:
                r.displayName = "Land Mine"; r.metalCost = 3; r.woodCost = 0; r.techCost = 1;
                r.damage = 80f; r.radius = 5f; r.duration = 300f; r.color = new Color(0.3f, 0.3f, 0.2f); break;
            case TrapType.TripwireAlarm:
                r.displayName = "Tripwire Alarm"; r.metalCost = 1; r.woodCost = 0; r.techCost = 2;
                r.damage = 0f; r.radius = 3f; r.duration = 180f; r.color = Color.yellow; break;
            case TrapType.BearTrap:
                r.displayName = "Bear Trap"; r.metalCost = 3; r.woodCost = 1; r.techCost = 0;
                r.damage = 25f; r.radius = 1f; r.duration = 200f; r.color = new Color(0.4f, 0.3f, 0.2f); break;
            case TrapType.EMPTrap:
                r.displayName = "EMP Trap"; r.metalCost = 1; r.woodCost = 0; r.techCost = 4;
                r.damage = 10f; r.radius = 8f; r.duration = 60f; r.color = new Color(0.2f, 0.4f, 0.9f); break;
            case TrapType.DecoyTrap:
                r.displayName = "Decoy Hologram"; r.metalCost = 0; r.woodCost = 0; r.techCost = 3;
                r.damage = 0f; r.radius = 0f; r.duration = 45f; r.color = new Color(0.5f, 0.9f, 1f); break;
            case TrapType.PoisonTrap:
                r.displayName = "Poison Trap"; r.metalCost = 1; r.woodCost = 2; r.techCost = 1;
                r.damage = 8f; r.radius = 4f; r.duration = 150f; r.color = new Color(0.3f, 0.8f, 0.2f); break;
            case TrapType.NetTrap:
                r.displayName = "Net Trap"; r.metalCost = 1; r.woodCost = 3; r.techCost = 0;
                r.damage = 5f; r.radius = 3f; r.duration = 120f; r.color = new Color(0.6f, 0.6f, 0.5f); break;
        }
        return r;
    }
}

public class DeployedTrap : NetworkBehaviour
{
    public TrapType trapType;
    public NetworkVariable<bool> isTriggered = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private TrapRecipe recipe;
    private float spawnTime;
    private ulong ownerClientId;
    private bool isArmed = false;
    private float armDelay = 2f;
    private GameObject trapMesh;
    private GameObject triggerIndicator;

    public void Initialize(TrapType type, ulong ownerId)
    {
        trapType = type;
        ownerClientId = ownerId;
        recipe = TrapRecipe.GetRecipe(type);
        spawnTime = Time.time;

        // Collider for trigger detection
        SphereCollider trigCol = gameObject.AddComponent<SphereCollider>();
        trigCol.isTrigger = true;
        trigCol.radius = recipe.radius;

        BuildTrapVisual();
    }

    private void BuildTrapVisual()
    {
        trapMesh = new GameObject("TrapMesh_" + trapType);
        trapMesh.transform.SetParent(transform);
        trapMesh.transform.localPosition = Vector3.zero;

        Material trapMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        trapMat.color = recipe.color;

        switch (trapType)
        {
            case TrapType.SpikeTrap:
                // Flat plate with spikes
                GameObject plate = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
                plate.transform.SetParent(trapMesh.transform);
                plate.transform.localPosition = new Vector3(0, 0.05f, 0);
                plate.transform.localScale = new Vector3(2f, 0.1f, 2f);
                Destroy(plate.GetComponent<Collider>());
                plate.GetComponent<Renderer>().material = trapMat;
                for (int i = 0; i < 9; i++)
                {
                    GameObject spike = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
                    spike.transform.SetParent(trapMesh.transform);
                    float x = (i % 3 - 1) * 0.5f;
                    float z = (i / 3 - 1) * 0.5f;
                    spike.transform.localPosition = new Vector3(x, 0.3f, z);
                    spike.transform.localScale = new Vector3(0.1f, 0.25f, 0.1f);
                    Destroy(spike.GetComponent<Collider>());
                    Material spikeMat = new Material(ProceduralArt.GetSafeShader("Standard"));
                    spikeMat.color = new Color(0.7f, 0.7f, 0.7f);
                    spike.GetComponent<Renderer>().material = spikeMat;
                }
                break;

            case TrapType.LandMine:
                GameObject mine = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
                mine.transform.SetParent(trapMesh.transform);
                mine.transform.localPosition = new Vector3(0, 0.05f, 0);
                mine.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
                Destroy(mine.GetComponent<Collider>());
                mine.GetComponent<Renderer>().material = trapMat;
                // Red blink light
                GameObject blinkGo = new GameObject("BlinkLight");
                blinkGo.transform.SetParent(trapMesh.transform);
                blinkGo.transform.localPosition = new Vector3(0, 0.1f, 0);
                Light blink = blinkGo.AddComponent<Light>();
                blink.color = Color.red;
                blink.intensity = 1f;
                blink.range = 2f;
                break;

            case TrapType.DecoyTrap:
                // Holographic player
                GameObject hologram = ProceduralArt.CreateHumanoidMesh("Striker");
                hologram.transform.SetParent(trapMesh.transform);
                hologram.transform.localPosition = Vector3.zero;
                // Make it transparent/holographic
                foreach (Renderer r in hologram.GetComponentsInChildren<Renderer>())
                {
                    Material holoMat = new Material(ProceduralArt.GetSafeShader("Standard"));
                    holoMat.color = new Color(0.3f, 0.7f, 1f, 0.4f);
                    holoMat.SetFloat("_Mode", 3);
                    holoMat.SetInt("_ZWrite", 0);
                    holoMat.renderQueue = 3000;
                    r.material = holoMat;
                }
                break;

            default:
                // Generic trap mesh
                GameObject generic = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
                generic.transform.SetParent(trapMesh.transform);
                generic.transform.localPosition = new Vector3(0, 0.15f, 0);
                generic.transform.localScale = new Vector3(1f, 0.3f, 1f);
                Destroy(generic.GetComponent<Collider>());
                generic.GetComponent<Renderer>().material = trapMat;
                break;
        }

        // Initially semi-visible, fades after arming
        StartCoroutine(ArmTrap());
    }

    private IEnumerator ArmTrap()
    {
        yield return new WaitForSeconds(armDelay);
        isArmed = true;

        // Make trap less visible after arming
        foreach (Renderer r in trapMesh.GetComponentsInChildren<Renderer>())
        {
            Color c = r.material.color;
            c.a = 0.3f;
            r.material.color = c;
            r.material.SetFloat("_Mode", 3);
            r.material.SetInt("_ZWrite", 0);
            r.material.renderQueue = 3000;
        }
    }

    private void Update()
    {
        // Lifetime expiry
        if (IsServer && Time.time - spawnTime > recipe.duration)
        {
            Destroy(gameObject);
        }

        // Blink effect for land mine
        if (trapType == TrapType.LandMine)
        {
            Light l = GetComponentInChildren<Light>();
            if (l != null) l.intensity = Mathf.Abs(Mathf.Sin(Time.time * 2f)) * 2f;
        }

        // Decoy idle animation
        if (trapType == TrapType.DecoyTrap && trapMesh != null)
        {
            // Slight swaying
            trapMesh.transform.localPosition = new Vector3(0, Mathf.Sin(Time.time) * 0.05f, 0);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !isArmed || isTriggered.Value) return;

        // Don't trigger on owner
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc.OwnerClientId == ownerClientId) return;

        AIBot bot = other.GetComponentInParent<AIBot>();
        if (pc == null && bot == null) return;

        isTriggered.Value = true;
        TriggerTrapClientRpc();

        switch (trapType)
        {
            case TrapType.SpikeTrap:
            case TrapType.BearTrap:
                if (pc != null) pc.RequestTakeDamageServerRpc(recipe.damage, "Trap", transform.position);
                if (bot != null) bot.RequestTakeDamageServerRpc(recipe.damage, "Trap", transform.position);
                StartCoroutine(DestroyAfter(5f));
                break;

            case TrapType.LandMine:
                // Explosion
                Collider[] hits = Physics.OverlapSphere(transform.position, recipe.radius);
                foreach (Collider c in hits)
                {
                    PlayerController p = c.GetComponentInParent<PlayerController>();
                    if (p != null) p.RequestTakeDamageServerRpc(recipe.damage, "Land Mine", transform.position);
                    AIBot b = c.GetComponentInParent<AIBot>();
                    if (b != null) b.RequestTakeDamageServerRpc(recipe.damage, "Land Mine", transform.position);
                }
                StartCoroutine(DestroyAfter(0.5f));
                break;

            case TrapType.TripwireAlarm:
                // Alert the deployer
                if (GameHUD.Instance != null)
                    GameHUD.Instance.AddKillFeedEntry("TRAP ALERT", "Enemy triggered your Tripwire!");
                StartCoroutine(DestroyAfter(1f));
                break;

            case TrapType.EMPTrap:
                // Damage nearby entities and stun
                Collider[] empHits = Physics.OverlapSphere(transform.position, recipe.radius);
                foreach (Collider ec in empHits)
                {
                    PlayerController empPc = ec.GetComponentInParent<PlayerController>();
                    if (empPc != null && empPc.OwnerClientId != ownerClientId) empPc.RequestTakeDamageServerRpc(recipe.damage, "EMP Trap", transform.position);
                    AIBot empBot = ec.GetComponentInParent<AIBot>();
                    if (empBot != null) empBot.RequestTakeDamageServerRpc(recipe.damage, "EMP Trap", transform.position);
                }
                StartCoroutine(DestroyAfter(2f));
                break;

            case TrapType.PoisonTrap:
                StartCoroutine(PoisonCloud());
                break;

            case TrapType.DecoyTrap:
                // Just gets destroyed when shot at
                StartCoroutine(DestroyAfter(0.5f));
                break;

            case TrapType.NetTrap:
                if (pc != null) pc.RequestTakeDamageServerRpc(recipe.damage, "Net Trap", transform.position);
                if (bot != null) bot.RequestTakeDamageServerRpc(recipe.damage, "Net Trap", transform.position);
                StartCoroutine(DestroyAfter(3f));
                break;
        }
    }

    [ClientRpc]
    private void TriggerTrapClientRpc()
    {
        // Visual/audio feedback
        GameObject fxGo = new GameObject("TrapFX");
        fxGo.transform.position = transform.position + new Vector3(0, 0.5f, 0);
        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = fxGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startColor = recipe != null ? recipe.color : Color.red;
        main.startSize = 0.5f;
        main.startSpeed = 5f;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });
        ps.Play();
        Destroy(fxGo, 2f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeathSound();
    }

    private IEnumerator PoisonCloud()
    {
        // Spawn poison cloud particles
        GameObject cloudGo = new GameObject("PoisonCloud");
        cloudGo.transform.position = transform.position;
        ParticleSystem ps = cloudGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = cloudGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main;
        main.loop = true;
        main.startColor = new Color(0.3f, 0.8f, 0.2f, 0.5f);
        main.startSize = 2f;
        main.startSpeed = 0.5f;
        main.startLifetime = 3f;
        var emission = ps.emission;
        emission.rateOverTime = 15;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = recipe.radius * 0.5f;
        ps.Play();

        float elapsed = 0f;
        while (elapsed < 8f)
        {
            elapsed += 1f;
            Collider[] inCloud = Physics.OverlapSphere(transform.position, recipe.radius);
            foreach (Collider c in inCloud)
            {
                PlayerController p = c.GetComponentInParent<PlayerController>();
                if (p != null && p.OwnerClientId != ownerClientId) p.RequestTakeDamageServerRpc(recipe.damage, "Poison", transform.position);
                AIBot b = c.GetComponentInParent<AIBot>();
                if (b != null) b.RequestTakeDamageServerRpc(recipe.damage, "Poison", transform.position);
            }
            yield return new WaitForSeconds(1f);
        }

        Destroy(cloudGo);
        Destroy(gameObject);
    }

    private IEnumerator DestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsServer) Destroy(gameObject);
    }
}

public class TrapSystem : MonoBehaviour
{
    public static TrapSystem Instance;

    // Player crafting materials inventory
    public int metalScrap = 0;
    public int woodPlanks = 0;
    public int techParts = 0;

    private void Awake() { Instance = this; }

    public bool CanCraft(TrapType type)
    {
        TrapRecipe recipe = TrapRecipe.GetRecipe(type);
        return metalScrap >= recipe.metalCost && woodPlanks >= recipe.woodCost && techParts >= recipe.techCost;
    }

    public bool CraftAndDeploy(TrapType type, Vector3 position, Quaternion rotation, ulong ownerId)
    {
        TrapRecipe recipe = TrapRecipe.GetRecipe(type);
        if (!CanCraft(type)) return false;

        metalScrap -= recipe.metalCost;
        woodPlanks -= recipe.woodCost;
        techParts -= recipe.techCost;

        GameObject trapGo = new GameObject("Trap_" + type + "_" + Time.time);
        trapGo.transform.position = position;
        trapGo.transform.rotation = rotation;

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = trapGo.AddComponent<NetworkObject>();
            netObj.Spawn(true);
        }

        DeployedTrap trap = trapGo.AddComponent<DeployedTrap>();
        trap.Initialize(type, ownerId);

        return true;
    }

    /// <summary>
    /// Gather materials from environment objects (trees = wood, rocks = metal, crates = tech).
    /// </summary>
    public void GatherMaterials(string sourceType, int amount)
    {
        switch (sourceType)
        {
            case "Tree": woodPlanks += amount; break;
            case "Rock": metalScrap += amount; break;
            case "Crate": techParts += amount; break;
        }
    }
}


