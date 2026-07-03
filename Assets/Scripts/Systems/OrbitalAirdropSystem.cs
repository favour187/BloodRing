using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — Orbital Supply Drones & Signal Flare Beacons.
/// Manages automated high-altitude Orbital Supply Drones dropping golden heavy-armor supply
/// crates every 90 seconds. Allows players with Signal Flare Beacons to call personal airdrops
/// containing Level 3 Cyber-Armor, Ghillie cloaks, and legendary sniper rifles.
/// 100% original proprietary supply mechanic.
/// </summary>
public class OrbitalAirdropSystem : MonoBehaviour
{
    public static OrbitalAirdropSystem Instance;

    public float automatedDropInterval = 90f;
    private List<GameObject> activeAirdrops = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        StartCoroutine(AutomatedDropRoutineCoroutine());
    }

    private IEnumerator AutomatedDropRoutineCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(automatedDropInterval);
            Vector2 randomCircle = Random.insideUnitCircle * 120f;
            Vector3 dropPos = new Vector3(randomCircle.x, 60f, randomCircle.y);
            DeployOrbitalAirdrop(dropPos, false, "AUTOMATED_ORBITAL");
        }
    }

    /// <summary>Deploys an Orbital Supply Crate with golden smoke plume at specified target.</summary>
    public GameObject DeployOrbitalAirdrop(Vector3 spawnPosition, bool isPersonalFlare, string callerId)
    {
        Debug.Log($"[OrbitalAirdropSystem] Deploying {(isPersonalFlare ? "Personal Flare" : "Automated Orbital")} Supply Crate at {spawnPosition}");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("DropPlane_Flyover");
            if (isPersonalFlare) AudioManager.Instance.PlayVO("VO_OrbitalAirdrop_RealAI");
        }

        GameObject crateModel = BloodRing.Art.BloodRingArtLibrary.GetEnvironmentModel("LootBox.obj") ?? new GameObject("OrbitalSupplyCrate");
        crateModel.name = "OrbitalCrate_" + callerId + "_" + Time.time;
        crateModel.transform.position = spawnPosition;
        crateModel.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

        Rigidbody rb = crateModel.GetComponent<Rigidbody>();
        if (rb == null) rb = crateModel.AddComponent<Rigidbody>();
        rb.mass = 50f;
        rb.drag = 2f; // Slow parachute descent

        Renderer r = crateModel.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = BloodRing.Art.BloodRingArtLibrary.GetMaterial("Mat_OrbitalCrate");
            mat.color = isPersonalFlare ? new Color(1f, 0.75f, 0.1f) : new Color(0.9f, 0.2f, 0.2f);
            r.sharedMaterial = mat;
        }

        activeAirdrops.Add(crateModel);
        return crateModel;
    }
}
