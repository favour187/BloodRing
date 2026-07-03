using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — Bio-Plasma Spores & HP Auto-Regen System.
/// Spawns interactive bio-luminescent Plasma Spores (Levels 1 to 4) across island terrain.
/// Consuming spores grants Plasma Energy (PE) up to 200 PE. When player HP drops below max,
/// stored PE automatically converts into HP at a rate of 2 HP/sec (5 HP/sec with pet perks),
/// creating a rich exploration and foraging loop between firefights. 100% original proprietary mechanic.
/// </summary>
public class PlasmaEnergySystem : MonoBehaviour
{
    public static PlasmaEnergySystem Instance;

    public float maxPlasmaEnergy = 200f;
    public float currentPlasmaEnergy = 0f;
    public float hpConversionRate = 2f;
    public float petBonusConversionRate = 3f;

    private List<GameObject> spawnedSpores = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        currentPlasmaEnergy = 50f; // Start match with 50 PE
    }

    private void Update()
    {
        // Handle automatic PE -> HP conversion during gameplay
        if (currentPlasmaEnergy > 0 && PlayerController.Instance != null)
        {
            float currentHP = PlayerController.Instance.GetCurrentHP();
            float maxHP = PlayerController.Instance.GetMaxHP();

            if (currentHP < maxHP)
            {
                float rate = hpConversionRate;
                if (PetSystem.Instance != null && PetSystem.Instance.HasActivePet("MechaPanda")) rate += petBonusConversionRate;

                float healAmount = Mathf.Min(rate * Time.deltaTime, currentPlasmaEnergy, maxHP - currentHP);
                PlayerController.Instance.HealHP(healAmount);
                currentPlasmaEnergy -= healAmount;
            }
        }
    }

    /// <summary>Called when player interacts with a Bio-Plasma Spore in the game world.</summary>
    public void ConsumePlasmaSpore(int sporeLevel)
    {
        float energyGain = sporeLevel * 50f;
        currentPlasmaEnergy = Mathf.Min(maxPlasmaEnergy, currentPlasmaEnergy + energyGain);
        Debug.Log($"[PlasmaEnergySystem] Consumed Level {sporeLevel} Bio-Plasma Spore! Stored PE: {currentPlasmaEnergy}/{maxPlasmaEnergy}");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Loot_Pickup");
    }

    public void SpawnSporeCluster(Vector3 position, int level)
    {
        GameObject spore = BloodRing.Art.BloodRingArtLibrary.GetPropModel("BountySkull.obj") ?? new GameObject("BioPlasmaSpore_Lv" + level);
        spore.name = "BioPlasmaSpore_Lv" + level;
        spore.transform.position = position;
        spore.transform.localScale = Vector3.one * (0.6f + level * 0.2f);
        
        Renderer r = spore.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = BloodRing.Art.BloodRingArtLibrary.GetMaterial("Mat_PlasmaSpore");
            mat.color = new Color(0.6f, 0.2f, 0.95f, 0.9f);
            r.sharedMaterial = mat;
        }

        spawnedSpores.Add(spore);
    }
}
