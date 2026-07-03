using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — Cryo-Barrier Tactical Forcefield System.
/// Instantly deploys a crystalline energy barrier that absorbs up to 1,200 damage.
/// Can be reinforced by companion pets (e.g. RoboSphere pet adding +400 shield HP).
/// 100% original proprietary mechanic for Blood Ring.
/// </summary>
public class CryoBarrierSystem : MonoBehaviour
{
    public static CryoBarrierSystem Instance;
    private List<GameObject> activeBarriers = new List<GameObject>();
    public float baseBarrierHP = 1200f;
    public float petBonusShieldHP = 400f;

    private void Awake() { Instance = this; }

    /// <summary>Instantly deploys a Cryo-Barrier forcefield in front of the player.</summary>
    public GameObject DeployBarrier(Vector3 spawnPos, Quaternion rotation, string ownerId, bool hasRoboPet = false)
    {
        GameObject barrierModel = BloodRing.Art.BloodRingArtLibrary.GetEnvironmentModel("Wall.obj") ?? new GameObject("CryoBarrier_Shield");
        barrierModel.name = "CryoBarrier_" + ownerId + "_" + Time.time;
        barrierModel.transform.position = spawnPos + Vector3.up * 1.5f;
        barrierModel.transform.rotation = rotation;
        barrierModel.transform.localScale = new Vector3(4.5f, 3.2f, 0.6f);

        BoxCollider col = barrierModel.GetComponent<BoxCollider>();
        if (col == null) col = barrierModel.AddComponent<BoxCollider>();

        CryoBarrierHealth health = barrierModel.AddComponent<CryoBarrierHealth>();
        health.maxHP = baseBarrierHP + (hasRoboPet ? petBonusShieldHP : 0f);
        health.currentHP = health.maxHP;
        health.ownerId = ownerId;

        Renderer renderer = barrierModel.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material barrierMat = BloodRing.Art.BloodRingArtLibrary.GetMaterial("Mat_CryoBarrier");
            barrierMat.color = new Color(0.1f, 0.85f, 0.95f, 0.85f);
            renderer.sharedMaterial = barrierMat;
        }

        activeBarriers.Add(barrierModel);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_BuildHammer");
        return barrierModel;
    }

    public void DestroyBarrier(GameObject barrier)
    {
        if (activeBarriers.Contains(barrier)) activeBarriers.Remove(barrier);
        if (barrier != null) Destroy(barrier);
    }
}

public class CryoBarrierHealth : MonoBehaviour
{
    public float maxHP = 1200f;
    public float currentHP = 1200f;
    public string ownerId;

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP <= 0)
        {
            if (CryoBarrierSystem.Instance != null) CryoBarrierSystem.Instance.DestroyBarrier(gameObject);
            else Destroy(gameObject);
        }
    }
}
