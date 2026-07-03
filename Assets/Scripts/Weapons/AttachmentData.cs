using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Weapon attachment definitions.
/// Each attachment modifies weapon stats when equipped.
/// Attachments are found as loot and can be equipped via the inventory UI.
/// </summary>
public enum AttachmentSlot { Scope, Muzzle, Magazine, Grip, Stock, Laser, Barrel }

[System.Serializable]
public class AttachmentData
{
    public string attachmentName;
    public string displayName;
    public AttachmentSlot slot;
    public string description;

    // Stat modifiers (additive to base weapon stats)
    public float damageBonus;      // Added to base damage
    public float spreadModifier;   // Multiplied onto spread (lower = more accurate)
    public float fireRateModifier; // Multiplied onto fire rate (lower = faster)
    public float reloadModifier;   // Multiplied onto reload time (lower = faster)
    public int   extraAmmo;        // Added to magazine capacity
    public float rangeModifier;    // Multiplied onto max range (higher = farther)
    public float adsModifier;      // Multiplied onto ADS speed (lower = faster)

    public static AttachmentData GetAttachment(string name)
    {
        AttachmentData a = new AttachmentData();
        a.attachmentName = name;
        a.spreadModifier = 1f; a.fireRateModifier = 1f; a.reloadModifier = 1f;
        a.rangeModifier = 1f; a.adsModifier = 1f;

        switch (name)
        {
            // ── SCOPES ──────────────────────────────────────────────
            case "RedDot":
                a.displayName = "Red Dot Sight"; a.slot = AttachmentSlot.Scope;
                a.description = "Close-range reflex sight. Improves ADS speed.";
                a.adsModifier = 0.85f; break;
            case "Holo":
                a.displayName = "Holographic Sight"; a.slot = AttachmentSlot.Scope;
                a.description = "Medium-range holographic sight. Better target acquisition.";
                a.adsModifier = 0.9f; a.spreadModifier = 0.95f; break;
            case "Scope2x":
                a.displayName = "2x Scope"; a.slot = AttachmentSlot.Scope;
                a.description = "2x magnification for medium range.";
                a.adsModifier = 0.95f; a.spreadModifier = 0.9f; break;
            case "Scope4x":
                a.displayName = "4x ACOG"; a.slot = AttachmentSlot.Scope;
                a.description = "4x magnification for long range.";
                a.adsModifier = 1.0f; a.spreadModifier = 0.85f; break;
            case "Scope8x":
                a.displayName = "8x Sniper Scope"; a.slot = AttachmentSlot.Scope;
                a.description = "8x magnification for extreme range. Sniper rifles only.";
                a.spreadModifier = 0.7f; break;
            case "Thermal":
                a.displayName = "Thermal Scope"; a.slot = AttachmentSlot.Scope;
                a.description = "Highlights enemy heat signatures through smoke and foliage.";
                a.spreadModifier = 0.8f; break;

            // ── MUZZLE ──────────────────────────────────────────────
            case "Suppressor":
                a.displayName = "Suppressor"; a.slot = AttachmentSlot.Muzzle;
                a.description = "Reduces gunshot sound and muzzle flash. Slightly reduces damage.";
                a.damageBonus = -2f; break;
            case "Compensator":
                a.displayName = "Compensator"; a.slot = AttachmentSlot.Muzzle;
                a.description = "Reduces horizontal recoil significantly.";
                a.spreadModifier = 0.8f; break;
            case "FlashHider":
                a.displayName = "Flash Hider"; a.slot = AttachmentSlot.Muzzle;
                a.description = "Eliminates muzzle flash. Slight recoil reduction.";
                a.spreadModifier = 0.9f; break;
            case "MuzzleBrake":
                a.displayName = "Muzzle Brake"; a.slot = AttachmentSlot.Muzzle;
                a.description = "Reduces vertical recoil. Best for automatic weapons.";
                a.spreadModifier = 0.85f; break;

            // ── MAGAZINES ───────────────────────────────────────────
            case "ExtMag_S":
                a.displayName = "Extended Mag (S)"; a.slot = AttachmentSlot.Magazine;
                a.description = "+10 ammo capacity.";
                a.extraAmmo = 10; break;
            case "ExtMag_L":
                a.displayName = "Extended Mag (L)"; a.slot = AttachmentSlot.Magazine;
                a.description = "+20 ammo capacity. Slower reload.";
                a.extraAmmo = 20; a.reloadModifier = 1.15f; break;
            case "QuickDraw":
                a.displayName = "Quickdraw Mag"; a.slot = AttachmentSlot.Magazine;
                a.description = "Faster reload speed.";
                a.reloadModifier = 0.75f; break;
            case "ExtQuickDraw":
                a.displayName = "Ext Quickdraw Mag"; a.slot = AttachmentSlot.Magazine;
                a.description = "+10 ammo and faster reload. Best of both worlds.";
                a.extraAmmo = 10; a.reloadModifier = 0.8f; break;

            // ── GRIPS ───────────────────────────────────────────────
            case "VerticalGrip":
                a.displayName = "Vertical Grip"; a.slot = AttachmentSlot.Grip;
                a.description = "Reduces vertical recoil. Stable for sprays.";
                a.spreadModifier = 0.85f; break;
            case "AngledGrip":
                a.displayName = "Angled Grip"; a.slot = AttachmentSlot.Grip;
                a.description = "Faster ADS speed. Better for peek-shooting.";
                a.adsModifier = 0.8f; a.spreadModifier = 0.95f; break;
            case "LaserGrip":
                a.displayName = "Laser Sight Grip"; a.slot = AttachmentSlot.Grip;
                a.description = "Improves hipfire accuracy significantly.";
                a.spreadModifier = 0.7f; break;
            case "ThumbGrip":
                a.displayName = "Thumb Grip"; a.slot = AttachmentSlot.Grip;
                a.description = "Faster ADS and slight recoil control.";
                a.adsModifier = 0.85f; a.spreadModifier = 0.92f; break;

            // ── STOCKS ──────────────────────────────────────────────
            case "TacticalStock":
                a.displayName = "Tactical Stock"; a.slot = AttachmentSlot.Stock;
                a.description = "Reduces recoil and improves stability while moving.";
                a.spreadModifier = 0.88f; break;
            case "LightStock":
                a.displayName = "Light Stock"; a.slot = AttachmentSlot.Stock;
                a.description = "Faster movement speed while aiming down sights.";
                a.adsModifier = 0.85f; break;
            case "HeavyStock":
                a.displayName = "Heavy Stock"; a.slot = AttachmentSlot.Stock;
                a.description = "Maximum recoil control. Slower ADS.";
                a.spreadModifier = 0.75f; a.adsModifier = 1.15f; break;

            // ── LASERS ──────────────────────────────────────────────
            case "LaserSight":
                a.displayName = "Laser Sight"; a.slot = AttachmentSlot.Laser;
                a.description = "Visible laser improves hipfire accuracy.";
                a.spreadModifier = 0.75f; break;

            // ── BARRELS ─────────────────────────────────────────────
            case "LongBarrel":
                a.displayName = "Long Barrel"; a.slot = AttachmentSlot.Barrel;
                a.description = "Increases effective range and bullet velocity.";
                a.damageBonus = 3f; a.rangeModifier = 1.2f; break;
            case "ShortBarrel":
                a.displayName = "Short Barrel"; a.slot = AttachmentSlot.Barrel;
                a.description = "Faster ADS and movement. Reduced range.";
                a.adsModifier = 0.8f; a.rangeModifier = 0.85f; break;
        }
        return a;
    }

    /// <summary>All available attachment names.</summary>
    public static List<string> GetAllAttachmentNames()
    {
        return new List<string>
        {
            "RedDot", "Holo", "Scope2x", "Scope4x", "Scope8x", "Thermal",
            "Suppressor", "Compensator", "FlashHider", "MuzzleBrake",
            "ExtMag_S", "ExtMag_L", "QuickDraw", "ExtQuickDraw",
            "VerticalGrip", "AngledGrip", "LaserGrip", "ThumbGrip",
            "TacticalStock", "LightStock", "HeavyStock",
            "LaserSight",
            "LongBarrel", "ShortBarrel"
        };
    }

    /// <summary>Get attachments filtered by slot type.</summary>
    public static List<string> GetAttachmentsBySlot(AttachmentSlot slot)
    {
        List<string> all = GetAllAttachmentNames();
        List<string> filtered = new List<string>();
        foreach (string n in all) { if (GetAttachment(n).slot == slot) filtered.Add(n); }
        return filtered;
    }

    /// <summary>Check if attachment is compatible with a weapon category.</summary>
    public static bool IsCompatible(string attachmentName, AmmoType weaponAmmoType)
    {
        AttachmentData a = GetAttachment(attachmentName);
        // Scope8x and Thermal only for snipers
        if (a.attachmentName == "Scope8x" && weaponAmmoType != AmmoType.SniperAmmo) return false;
        if (a.attachmentName == "Thermal" && weaponAmmoType != AmmoType.SniperAmmo) return false;
        // LongBarrel only for rifles and snipers
        if (a.attachmentName == "LongBarrel" && weaponAmmoType != AmmoType.RifleAmmo && weaponAmmoType != AmmoType.SniperAmmo) return false;
        return true;
    }

    /// <summary>Apply this attachment's modifiers to weapon stats.</summary>
    public static void ApplyAttachment(WeaponData weapon, string attachmentName)
    {
        AttachmentData a = GetAttachment(attachmentName);
        weapon.damage += a.damageBonus;
        weapon.spread *= a.spreadModifier;
        weapon.fireRate *= a.fireRateModifier;
        weapon.reloadTime *= a.reloadModifier;
        weapon.maxAmmo += a.extraAmmo;
        if (a.slot == AttachmentSlot.Scope) weapon.hasScope = true;
        if (a.slot == AttachmentSlot.Muzzle && a.attachmentName == "Suppressor") weapon.hasSilencer = true;
        if (a.slot == AttachmentSlot.Magazine && a.extraAmmo > 0) weapon.hasExtMag = true;
        if (a.slot == AttachmentSlot.Grip) weapon.hasGrip = true;
        if (a.slot == AttachmentSlot.Stock) weapon.hasStock = true;
    }
}
