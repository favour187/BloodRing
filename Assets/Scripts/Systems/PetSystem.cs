using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Battle-royale-grade Pet companion system.
/// Pets follow the player, provide passive bonuses, and have unique abilities.
/// </summary>
public enum PetType
{
    Falco,      // Gliding speed boost
    Shiba,      // Mushroom/health item detection
    Robo,       // Creates a small deployable shield
    Panda,      // HP recovery on kill
    Kitty,      // Zone damage reduction
    DragonPup,  // Deals small AoE damage nearby
    Phoenix,    // Revive assist speed boost
    Wolf        // Extra damage when low HP
}

[System.Serializable]
public class PetData
{
    public PetType type;
    public string petName;
    public string abilityName;
    public string abilityDesc;
    public Color petColor;
    public float abilityValue;
    public int level;

    public static PetData GetPet(PetType type)
    {
        PetData p = new PetData();
        p.type = type;
        p.level = 1;
        switch (type)
        {
            case PetType.Falco:
                p.petName = "Falco"; p.abilityName = "Skyline Spree"; p.abilityDesc = "Increases gliding speed by 30%";
                p.petColor = new Color(0.2f, 0.5f, 0.9f); p.abilityValue = 1.3f; break;
            case PetType.Shiba:
                p.petName = "Shiba"; p.abilityName = "Mushroom Sense"; p.abilityDesc = "Detects nearby health items within 30m";
                p.petColor = new Color(0.9f, 0.7f, 0.3f); p.abilityValue = 30f; break;
            case PetType.Robo:
                p.petName = "Robo"; p.abilityName = "Wall Enforce"; p.abilityDesc = "Repairs CryoBarriers and shields by 50 HP";
                p.petColor = new Color(0.6f, 0.6f, 0.8f); p.abilityValue = 50f; break;
            case PetType.Panda:
                p.petName = "Panda"; p.abilityName = "Panda's Blessings"; p.abilityDesc = "Restore 10 HP per kill";
                p.petColor = new Color(0.9f, 0.9f, 0.9f); p.abilityValue = 10f; break;
            case PetType.Kitty:
                p.petName = "Kitty"; p.abilityName = "Zone Guard"; p.abilityDesc = "Reduces zone damage by 30%";
                p.petColor = new Color(1f, 0.6f, 0.8f); p.abilityValue = 0.7f; break;
            case PetType.DragonPup:
                p.petName = "Dragon Pup"; p.abilityName = "Fire Breath"; p.abilityDesc = "Burns nearby enemies for 5 DPS";
                p.petColor = new Color(1f, 0.3f, 0.1f); p.abilityValue = 5f; break;
            case PetType.Phoenix:
                p.petName = "Phoenix"; p.abilityName = "Revival Aid"; p.abilityDesc = "Speeds up ally revive by 40%";
                p.petColor = new Color(1f, 0.6f, 0.2f); p.abilityValue = 0.6f; break;
            case PetType.Wolf:
                p.petName = "Wolf"; p.abilityName = "Last Stand"; p.abilityDesc = "+15% damage when below 30% HP";
                p.petColor = new Color(0.4f, 0.4f, 0.5f); p.abilityValue = 1.15f; break;
        }
        return p;
    }
}

public class PetCompanion : MonoBehaviour
{
    public PetData petData;
    private Transform ownerTransform;
    private Vector3 targetOffset;
    private float bobTimer = 0f;
    private float orbitAngle = 0f;
    private GameObject petModel;
    private ParticleSystem petAura;
    private GameObject petNameTag;
    private float abilityCooldown = 0f;

    public void Initialize(PetData data, Transform owner)
    {
        petData = data;
        ownerTransform = owner;
        targetOffset = new Vector3(1.5f, 0.5f, -1f);

        // Create pet visual model (small sphere-based creature)
        petModel = new GameObject("PetModel_" + data.petName);
        petModel.transform.SetParent(transform);
        petModel.transform.localPosition = Vector3.zero;

        // Body
        GameObject body = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj");
        body.name = "Body";
        body.transform.SetParent(petModel.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        Destroy(body.GetComponent<Collider>());
        Material bodyMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        bodyMat.color = data.petColor;
        bodyMat.SetFloat("_Glossiness", 0.8f);
        body.GetComponent<Renderer>().material = bodyMat;

        // Head
        GameObject head = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj");
        head.name = "Head";
        head.transform.SetParent(petModel.transform);
        head.transform.localPosition = new Vector3(0, 0.15f, 0.25f);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().material = bodyMat;

        // Eyes
        for (int i = 0; i < 2; i++)
        {
            GameObject eye = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj");
            eye.name = "Eye_" + i;
            eye.transform.SetParent(head.transform);
            eye.transform.localPosition = new Vector3(i == 0 ? -0.3f : 0.3f, 0.1f, 0.35f);
            eye.transform.localScale = new Vector3(0.25f, 0.25f, 0.15f);
            Destroy(eye.GetComponent<Collider>());
            Material eyeMat = new Material(ProceduralArt.GetSafeShader("Standard"));
            eyeMat.color = Color.white;
            eye.GetComponent<Renderer>().material = eyeMat;

            GameObject pupil = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj");
            pupil.name = "Pupil";
            pupil.transform.SetParent(eye.transform);
            pupil.transform.localPosition = new Vector3(0, 0, 0.3f);
            pupil.transform.localScale = new Vector3(0.5f, 0.5f, 0.3f);
            Destroy(pupil.GetComponent<Collider>());
            Material pupilMat = new Material(ProceduralArt.GetSafeShader("Standard"));
            pupilMat.color = Color.black;
            pupil.GetComponent<Renderer>().material = pupilMat;
        }

        // Pet aura particles
        GameObject auraGo = new GameObject("PetAura");
        auraGo.transform.SetParent(transform);
        auraGo.transform.localPosition = Vector3.zero;
        petAura = auraGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = auraGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = petAura.main;
        main.loop = true;
        main.startColor = new Color(data.petColor.r, data.petColor.g, data.petColor.b, 0.4f);
        main.startSize = 0.1f;
        main.startSpeed = 0.5f;
        main.startLifetime = 1f;
        var emission = petAura.emission;
        emission.rateOverTime = 5;

        // Pet name tag
        petNameTag = new GameObject("PetNameTag");
        petNameTag.transform.SetParent(transform);
        petNameTag.transform.localPosition = new Vector3(0, 0.7f, 0);
        TextMesh tm = petNameTag.AddComponent<TextMesh>();
        tm.text = data.petName;
        tm.fontSize = 32;
        tm.characterSize = 0.05f;
        tm.color = data.petColor;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
    }

    private void Update()
    {
        if (ownerTransform == null) return;

        // Orbit around owner
        orbitAngle += Time.deltaTime * 45f;
        bobTimer += Time.deltaTime * 3f;
        float bobY = Mathf.Sin(bobTimer) * 0.15f;

        Vector3 orbitOffset = new Vector3(
            Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * 1.8f,
            0.8f + bobY,
            Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * 1.8f
        );

        Vector3 targetPos = ownerTransform.position + orbitOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

        // Look at owner
        Vector3 lookDir = ownerTransform.position - transform.position;
        lookDir.y = 0;
        if (lookDir.magnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);

        // Billboard name tag
        if (petNameTag != null && Camera.main != null)
        {
            petNameTag.transform.LookAt(Camera.main.transform);
            petNameTag.transform.Rotate(0, 180, 0);
        }

        // Ability cooldown
        if (abilityCooldown > 0) abilityCooldown -= Time.deltaTime;
    }

    /// <summary>
    /// Called by PlayerController to apply passive pet effects each frame.
    /// </summary>
    public float GetZoneDamageMultiplier()
    {
        if (petData.type == PetType.Kitty) return petData.abilityValue;
        return 1f;
    }

    public float GetDamageBonusMultiplier(float currentHPPercent)
    {
        if (petData.type == PetType.Wolf && currentHPPercent < 0.3f) return petData.abilityValue;
        return 1f;
    }

    public float GetReviveSpeedMultiplier()
    {
        if (petData.type == PetType.Phoenix) return petData.abilityValue;
        return 1f;
    }

    public float GetHPOnKill()
    {
        if (petData.type == PetType.Panda) return petData.abilityValue;
        return 0f;
    }

    public void TriggerActiveAbility(Vector3 position)
    {
        if (abilityCooldown > 0) return;
        abilityCooldown = 30f; // 30s cooldown

        if (petData.type == PetType.DragonPup)
        {
            // Fire breath AoE
            Collider[] cols = Physics.OverlapSphere(position, 8f);
            foreach (Collider c in cols)
            {
                AIBot bot = c.GetComponentInParent<AIBot>();
                if (bot != null)
                {
                    bot.RequestTakeDamageServerRpc(petData.abilityValue, "DragonPup", position);
                }
            }
        }
        else if (petData.type == PetType.Shiba)
        {
            // Highlight nearby health items
            PickupItem[] pickups = Object.FindObjectsOfType<PickupItem>();
            foreach (PickupItem p in pickups)
            {
                if (p.pickupType == PickupType.HealthPack && Vector3.Distance(position, p.transform.position) <= petData.abilityValue)
                {
                    // Add temporary highlight
                    Light highlight = p.gameObject.AddComponent<Light>();
                    highlight.color = Color.green;
                    highlight.intensity = 3f;
                    highlight.range = 5f;
                    Object.Destroy(highlight, 10f);
                }
            }
        }
    }
}

public class PetSystem : MonoBehaviour
{
    public static PetSystem Instance;
    private void Awake() { Instance = this; }

    private PetCompanion activePet;

    public PetCompanion SpawnPet(PetType type, Transform owner)
    {
        GameObject petGo = new GameObject("Pet_" + type);
        PetCompanion companion = petGo.AddComponent<PetCompanion>();
        PetData data = PetData.GetPet(type);
        companion.Initialize(data, owner);
        activePet = companion;
        return companion;
    }

    /// <summary>True if a pet is currently active whose name/type matches or is contained in the given identifier.</summary>
    public bool HasActivePet(string identifier)
    {
        if (activePet == null || activePet.petData == null || string.IsNullOrEmpty(identifier)) return false;
        string petName = activePet.petData.petName ?? "";
        string typeName = activePet.petData.type.ToString();
        return identifier.Contains(petName) || identifier.Contains(typeName) || petName.Contains(identifier);
    }
}


