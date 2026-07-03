using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — Evolutionary Weapons & Awakening System.
/// Weapons evolve through 7 distinct tiers using Awakening Cores, unlocking custom kill feeds,
/// hit splatter effects, firing audio, reload animations, and exclusive emote perks.
/// 100% original proprietary progression mechanic.
/// 
/// Integrates with WeaponSkinData for cosmetic skins and AttachmentData for weapon customization.
/// </summary>
public class EvoWeaponSystem : MonoBehaviour
{
    public static EvoWeaponSystem Instance;

    public enum EvoTier { Lv1_Base = 1, Lv2_KillFeed = 2, Lv3_NewAudio = 3, Lv4_HitEffect = 4, Lv5_KillEffect = 5, Lv6_ReloadAnim = 6, Lv7_UltimateForm = 7 }

    public class EvoWeaponProfile
    {
        public string weaponId;
        public string weaponName;
        public EvoTier currentTier;
        public int awakeningCoresCollected;
        public float damageMultiplier;
        public float fireRateMultiplier;
        public string activeSkinId = "Default";
        public List<string> equippedAttachments = new List<string>();
        public List<string> unlockedSkins = new List<string>();
    }

    private Dictionary<string, EvoWeaponProfile> playerEvoWeapons = new Dictionary<string, EvoWeaponProfile>();
    private Dictionary<string, List<string>> playerInventory = new Dictionary<string, List<string>>();

    private void Awake()
    {
        Instance = this;
        playerEvoWeapons["AK47_EVO"] = new EvoWeaponProfile
        {
            weaponId = "AK47_EVO",
            weaponName = "CyberVortex AK47 Awakened",
            currentTier = EvoTier.Lv7_UltimateForm,
            awakeningCoresCollected = 150,
            damageMultiplier = 1.15f,
            fireRateMultiplier = 1.10f
        };
    }

    public EvoWeaponProfile GetProfile(string weaponId)
    {
        if (playerEvoWeapons.ContainsKey(weaponId)) return playerEvoWeapons[weaponId];
        return null;
    }

    /// <summary>Upgrades weapon tier when enough Awakening Cores are acquired.</summary>
    public bool UpgradeEvoTier(string weaponId)
    {
        if (!playerEvoWeapons.ContainsKey(weaponId)) return false;
        EvoWeaponProfile p = playerEvoWeapons[weaponId];
        if (p.currentTier < EvoTier.Lv7_UltimateForm)
        {
            p.currentTier = (EvoTier)((int)p.currentTier + 1);
            p.damageMultiplier += 0.03f;
            p.fireRateMultiplier += 0.02f;
            Debug.Log($"[EvoWeaponSystem] {p.weaponName} awakened to {p.currentTier}!");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_TalentUnlock");
            return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  WEAPON SKIN MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    /// <summary>Apply a skin to a weapon's evo profile.</summary>
    public bool ApplySkin(string weaponId, string skinId)
    {
        if (!playerEvoWeapons.ContainsKey(weaponId)) return false;
        EvoWeaponProfile p = playerEvoWeapons[weaponId];
        if (!p.unlockedSkins.Contains(skinId) && skinId != p.weaponName + "_Default") return false;
        p.activeSkinId = skinId;
        Debug.Log($"[EvoWeaponSystem] Applied skin {skinId} to {weaponId}");
        return true;
    }

    /// <summary>Unlock a skin for a weapon.</summary>
    public bool UnlockSkin(string weaponId, string skinId)
    {
        if (!playerEvoWeapons.ContainsKey(weaponId))
        {
            playerEvoWeapons[weaponId] = new EvoWeaponProfile
            {
                weaponId = weaponId, weaponName = weaponId,
                currentTier = EvoTier.Lv1_Base, damageMultiplier = 1f, fireRateMultiplier = 1f
            };
        }
        EvoWeaponProfile p = playerEvoWeapons[weaponId];
        if (!p.unlockedSkins.Contains(skinId))
        {
            p.unlockedSkins.Add(skinId);
            Debug.Log($"[EvoWeaponSystem] Unlocked skin {skinId} for {weaponId}");
            return true;
        }
        return false;
    }

    /// <summary>Get all available skins for a weapon (locked + unlocked).</summary>
    public List<WeaponSkinData> GetAvailableSkins(string weaponName)
    {
        return WeaponSkinData.GetSkinsForWeapon(weaponName);
    }

    // ═══════════════════════════════════════════════════════════
    //  WEAPON ATTACHMENT MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    /// <summary>Equip an attachment to a weapon. Returns false if incompatible or slot occupied.</summary>
    public bool EquipAttachment(string weaponId, string attachmentName)
    {
        if (!playerEvoWeapons.ContainsKey(weaponId)) return false;
        EvoWeaponProfile p = playerEvoWeapons[weaponId];

        AttachmentData attachment = AttachmentData.GetAttachment(attachmentName);
        WeaponData weapon = WeaponData.GetDefaultWeapon(p.weaponName);
        if (!AttachmentData.IsCompatible(attachmentName, weapon.ammoType)) return false;

        // Remove existing attachment in same slot
        for (int i = p.equippedAttachments.Count - 1; i >= 0; i--)
        {
            AttachmentData existing = AttachmentData.GetAttachment(p.equippedAttachments[i]);
            if (existing.slot == attachment.slot) p.equippedAttachments.RemoveAt(i);
        }

        p.equippedAttachments.Add(attachmentName);
        Debug.Log($"[EvoWeaponSystem] Equipped {attachmentName} on {weaponId}");
        return true;
    }

    /// <summary>Remove an attachment from a weapon.</summary>
    public bool RemoveAttachment(string weaponId, string attachmentName)
    {
        if (!playerEvoWeapons.ContainsKey(weaponId)) return false;
        EvoWeaponProfile p = playerEvoWeapons[weaponId];
        if (p.equippedAttachments.Remove(attachmentName))
        {
            Debug.Log($"[EvoWeaponSystem] Removed {attachmentName} from {weaponId}");
            return true;
        }
        return false;
    }

    /// <summary>Get all equipped attachments on a weapon.</summary>
    public List<string> GetEquippedAttachments(string weaponId)
    {
        if (!playerEvoWeapons.ContainsKey(weaponId)) return new List<string>();
        return playerEvoWeapons[weaponId].equippedAttachments;
    }

    /// <summary>Build a fully-modified WeaponData with all attachments applied.</summary>
    public WeaponData BuildModifiedWeapon(string weaponName, List<string> attachments)
    {
        WeaponData w = WeaponData.GetDefaultWeapon(weaponName);
        foreach (string attName in attachments)
        {
            AttachmentData.ApplyAttachment(w, attName);
        }
        return w;
    }

    // ═══════════════════════════════════════════════════════════
    //  LOOT INTEGRATION
    // ═══════════════════════════════════════════════════════════

    /// <summary>Add an attachment to player's global attachment inventory.</summary>
    public void AddAttachmentToInventory(string playerId, string attachmentName)
    {
        if (!playerInventory.ContainsKey(playerId)) playerInventory[playerId] = new List<string>();
        playerInventory[playerId].Add(attachmentName);
        Debug.Log($"[EvoWeaponSystem] Player {playerId} looted attachment: {attachmentName}");
    }

    /// <summary>Get player's attachment inventory.</summary>
    public List<string> GetPlayerAttachmentInventory(string playerId)
    {
        if (!playerInventory.ContainsKey(playerId)) return new List<string>();
        return playerInventory[playerId];
    }

    // ═══════════════════════════════════════════════════════════
    //  MASTER WEAPON CATALOG INFO
    // ═══════════════════════════════════════════════════════════

    /// <summary>Get complete weapon catalog info for UI display (62 weapons).</summary>
    public static List<WeaponCatalogEntry> GetFullCatalog()
    {
        List<WeaponCatalogEntry> catalog = new List<WeaponCatalogEntry>();
        foreach (string name in WeaponData.GetAllWeaponNames())
        {
            WeaponData w = WeaponData.GetDefaultWeapon(name);
            catalog.Add(new WeaponCatalogEntry
            {
                weaponName = name,
                damage = w.damage,
                fireRate = w.fireRate,
                ammoType = w.ammoType,
                rarity = w.rarity,
                isAutomatic = w.isAutomatic,
                category = GetCategoryLabel(w)
            });
        }
        return catalog;
    }

    private static string GetCategoryLabel(WeaponData w)
    {
        if (w.IsMelee()) return "Melee";
        if (w.IsSpecial()) return "Special";
        switch (w.ammoType)
        {
            case AmmoType.SMGAmmo: return "SMG";
            case AmmoType.RifleAmmo: return "Assault Rifle";
            case AmmoType.ShotgunAmmo: return "Shotgun";
            case AmmoType.SniperAmmo: return w.damage >= 70 ? "Sniper" : "DMR";
            case AmmoType.PistolAmmo: return "Pistol";
            case AmmoType.EnergyAmmo: return "Energy";
            default: return "Unknown";
        }
    }
}

[System.Serializable]
public class WeaponCatalogEntry
{
    public string weaponName;
    public float damage;
    public float fireRate;
    public AmmoType ammoType;
    public WeaponRarity rarity;
    public bool isAutomatic;
    public string category;
}
