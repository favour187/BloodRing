using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// UNIQUE FEATURE: Barricade / Fortification Crafting System.
/// Players gather materials from the destructible environment and build fortifications:
/// wooden walls, metal barriers, watchtowers, sandbag covers, spike fences.
/// This replaces legacy barriers with a richer, multi-tier build system.
/// Unique to BloodRing — this.
/// </summary>
public enum BarricadeType
{
    WoodWall,       // Low HP, cheap, fast to build
    MetalBarrier,   // Medium HP, blocks bullets
    Sandbags,       // Low profile cover, crouch behind
    WatchTower,     // Elevated platform for sniping
    SpikeFence,     // Damages enemies who touch it
    ReinforceWall   // High HP, expensive, slow to build
}

[System.Serializable]
public class BarricadeRecipe
{
    public BarricadeType type;
    public string name;
    public int woodCost;
    public int metalCost;
    public float hp;
    public float buildTime;
    public Color color;

    public static BarricadeRecipe Get(BarricadeType type)
    {
        BarricadeRecipe r = new BarricadeRecipe();
        r.type = type;
        switch (type)
        {
            case BarricadeType.WoodWall:
                r.name = "Wood Wall"; r.woodCost = 3; r.metalCost = 0; r.hp = 200f; r.buildTime = 1.5f;
                r.color = new Color(0.5f, 0.35f, 0.2f); break;
            case BarricadeType.MetalBarrier:
                r.name = "Metal Barrier"; r.woodCost = 0; r.metalCost = 4; r.hp = 500f; r.buildTime = 2.5f;
                r.color = new Color(0.5f, 0.5f, 0.55f); break;
            case BarricadeType.Sandbags:
                r.name = "Sandbags"; r.woodCost = 1; r.metalCost = 0; r.hp = 150f; r.buildTime = 1f;
                r.color = new Color(0.6f, 0.55f, 0.4f); break;
            case BarricadeType.WatchTower:
                r.name = "Watch Tower"; r.woodCost = 6; r.metalCost = 3; r.hp = 400f; r.buildTime = 4f;
                r.color = new Color(0.4f, 0.3f, 0.2f); break;
            case BarricadeType.SpikeFence:
                r.name = "Spike Fence"; r.woodCost = 2; r.metalCost = 2; r.hp = 250f; r.buildTime = 2f;
                r.color = new Color(0.45f, 0.4f, 0.35f); break;
            case BarricadeType.ReinforceWall:
                r.name = "Reinforced Wall"; r.woodCost = 3; r.metalCost = 5; r.hp = 800f; r.buildTime = 3.5f;
                r.color = new Color(0.4f, 0.45f, 0.5f); break;
        }
        return r;
    }
}

public class PlacedBarricade : NetworkBehaviour
{
    public NetworkVariable<float> hp = new NetworkVariable<float>(200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> destroyed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public BarricadeType barricadeType;
    private BarricadeRecipe recipe;
    private float lifetime = 180f; // 3 minutes max
    private float spawnTime;

    public void Initialize(BarricadeType type, Vector3 pos, Quaternion rot)
    {
        barricadeType = type;
        recipe = BarricadeRecipe.Get(type);
        if (IsServer) hp.Value = recipe.hp;
        spawnTime = Time.time;

        transform.position = pos;
        transform.rotation = rot;

        Material mat = new Material(ProceduralArt.GetSafeShader("Standard"));
        mat.color = recipe.color;

        switch (type)
        {
            case BarricadeType.WoodWall:
                CreateWallMesh(new Vector3(4f, 3f, 0.4f), mat);
                // Wood grain lines
                for (int i = 0; i < 4; i++)
                {
                    GameObject plank = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
                    plank.transform.SetParent(transform);
                    plank.transform.localPosition = new Vector3(0, 0.4f + i * 0.7f, 0.21f);
                    plank.transform.localScale = new Vector3(3.8f, 0.08f, 0.02f);
                    Destroy(plank.GetComponent<Collider>());
                    Material lineMat = new Material(ProceduralArt.GetSafeShader("Unlit/Color"));
                    lineMat.color = new Color(0.35f, 0.25f, 0.15f);
                    plank.GetComponent<Renderer>().material = lineMat;
                }
                break;
            case BarricadeType.MetalBarrier:
                CreateWallMesh(new Vector3(3.5f, 2.5f, 0.5f), mat);
                break;
            case BarricadeType.Sandbags:
                for (int r2 = 0; r2 < 3; r2++)
                    for (int c = 0; c < 4; c++)
                    {
                        GameObject bag = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
                        bag.transform.SetParent(transform);
                        bag.transform.localPosition = new Vector3(-1.2f + c * 0.8f, 0.2f + r2 * 0.35f, 0);
                        bag.transform.localScale = new Vector3(0.7f, 0.3f, 0.5f);
                        bag.GetComponent<Renderer>().material = mat;
                        Destroy(bag.GetComponent<Collider>());
                    }
                BoxCollider sandCol = gameObject.AddComponent<BoxCollider>();
                sandCol.center = new Vector3(0, 0.5f, 0);
                sandCol.size = new Vector3(3.5f, 1.2f, 0.6f);
                break;
            case BarricadeType.WatchTower:
                // 4 posts + platform
                for (int i = 0; i < 4; i++)
                {
                    GameObject post = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
                    post.transform.SetParent(transform);
                    float px = (i % 2 == 0) ? -1.5f : 1.5f;
                    float pz = (i < 2) ? -1.5f : 1.5f;
                    post.transform.localPosition = new Vector3(px, 3f, pz);
                    post.transform.localScale = new Vector3(0.2f, 3f, 0.2f);
                    post.GetComponent<Renderer>().material = mat;
                    Destroy(post.GetComponent<Collider>());
                }
                GameObject platform = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
                platform.transform.SetParent(transform);
                platform.transform.localPosition = new Vector3(0, 6f, 0);
                platform.transform.localScale = new Vector3(4f, 0.3f, 4f);
                platform.GetComponent<Renderer>().material = mat;
                BoxCollider towerCol = gameObject.AddComponent<BoxCollider>();
                towerCol.center = new Vector3(0, 3f, 0);
                towerCol.size = new Vector3(4f, 6.5f, 4f);
                break;
            case BarricadeType.SpikeFence:
                CreateWallMesh(new Vector3(4f, 1.5f, 0.3f), mat);
                for (int i = 0; i < 8; i++)
                {
                    GameObject spike = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
                    spike.transform.SetParent(transform);
                    spike.transform.localPosition = new Vector3(-1.5f + i * 0.45f, 2f, 0);
                    spike.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
                    spike.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
                    Destroy(spike.GetComponent<Collider>());
                    Material spikeMat = new Material(ProceduralArt.GetSafeShader("Standard"));
                    spikeMat.color = new Color(0.6f, 0.6f, 0.6f);
                    spike.GetComponent<Renderer>().material = spikeMat;
                }
                break;
            case BarricadeType.ReinforceWall:
                CreateWallMesh(new Vector3(5f, 3.5f, 0.6f), mat);
                // Reinforcement X-brace
                for (int i = 0; i < 2; i++)
                {
                    GameObject brace = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
                    brace.transform.SetParent(transform);
                    brace.transform.localPosition = new Vector3(0, 1.75f, 0.31f);
                    brace.transform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 45 : -45);
                    brace.transform.localScale = new Vector3(0.15f, 4f, 0.05f);
                    Destroy(brace.GetComponent<Collider>());
                    Material braceMat = new Material(ProceduralArt.GetSafeShader("Standard"));
                    braceMat.color = new Color(0.5f, 0.5f, 0.55f);
                    brace.GetComponent<Renderer>().material = braceMat;
                }
                break;
        }

        // Build particles
        GameObject buildFx = new GameObject("BuildFX");
        buildFx.transform.position = transform.position + Vector3.up;
        ParticleSystem ps = buildFx.AddComponent<ParticleSystem>();
        buildFx.GetComponent<ParticleSystemRenderer>().material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main; main.duration = 0.5f; main.loop = false;
        main.startColor = recipe.color; main.startSize = 0.3f; main.startSpeed = 3f;
        var emission = ps.emission; emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) }); ps.Play();
        Destroy(buildFx, 2f);
    }

    private void CreateWallMesh(Vector3 scale, Material mat)
    {
        GameObject wall = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        wall.name = "WallMesh";
        wall.transform.SetParent(transform);
        wall.transform.localPosition = new Vector3(0, scale.y / 2f, 0);
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = mat;
        // Use the wall's collider as the barricade collider
    }

    private void Update()
    {
        if (destroyed.Value) return;
        if (IsServer && Time.time - spawnTime > lifetime)
        {
            destroyed.Value = true;
            DestroyBarricadeClientRpc();
        }

        // Spike fence damage on contact
        if (barricadeType == BarricadeType.SpikeFence && IsServer)
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, 2f);
            foreach (Collider c in nearby)
            {
                AIBot bot = c.GetComponentInParent<AIBot>();
                if (bot != null) bot.RequestTakeDamageServerRpc(5f * Time.deltaTime, "Spike Fence", transform.position);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        if (destroyed.Value) return;
        hp.Value -= damage;
        if (hp.Value <= 0) { destroyed.Value = true; DestroyBarricadeClientRpc(); }
    }

    [ClientRpc]
    private void DestroyBarricadeClientRpc()
    {
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
            rb.AddExplosionForce(250f, transform.position, 5f);
        }
        StartCoroutine(RemoveAfter());
    }

    private IEnumerator RemoveAfter() { yield return new WaitForSeconds(2.5f); if (IsServer) Destroy(gameObject); }
}

public class BarricadeSystem : MonoBehaviour
{
    public static BarricadeSystem Instance;

    public int woodCount = 0;
    public int metalCount = 0;

    private void Awake() { Instance = this; }

    public void GatherMaterial(string sourceType, int amount)
    {
        if (sourceType == "Tree" || sourceType == "Wood") woodCount += amount;
        else if (sourceType == "Rock" || sourceType == "Metal" || sourceType == "Crate") metalCount += amount;
    }

    public bool CanBuild(BarricadeType type)
    {
        BarricadeRecipe r = BarricadeRecipe.Get(type);
        return woodCount >= r.woodCost && metalCount >= r.metalCost;
    }

    public bool Build(BarricadeType type, Vector3 position, Quaternion rotation)
    {
        BarricadeRecipe r = BarricadeRecipe.Get(type);
        if (!CanBuild(type)) return false;

        woodCount -= r.woodCost;
        metalCount -= r.metalCost;

        GameObject go = new GameObject("Barricade_" + type);
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = go.AddComponent<NetworkObject>();
            netObj.Spawn(true);
        }
        PlacedBarricade barricade = go.AddComponent<PlacedBarricade>();
        barricade.Initialize(type, position, rotation);

        if (GameHUD.Instance != null)
            GameHUD.Instance.AddKillFeedEntry("BUILD", r.name + " placed! (Wood:" + woodCount + " Metal:" + metalCount + ")");

        return true;
    }
}


