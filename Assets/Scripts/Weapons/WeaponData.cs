using UnityEngine;
using System.Collections.Generic;

public enum AmmoType { PistolAmmo, RifleAmmo, ShotgunAmmo, SniperAmmo, SMGAmmo, EnergyAmmo }

/// <summary>
/// WeaponData — Blood Ring weapon catalog.
/// 62 weapons total across 8 categories: SMGs (12), ARs (14), Shotguns (9),
/// Snipers/DMRs (10), Pistols/Sidearms (8), Melee (4), Specials (5), Energy (2).
/// All stats are tuned for mobile TPS battle-royale feel (Free Fire / COD Mobile quality).
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "BloodRing/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public float damage;
    public int maxAmmo;
    public bool isAutomatic;
    public float fireRate;
    public float spread;
    public float reloadTime;
    public int pellets = 1;
    public AmmoType ammoType;
    public Color iconColor = Color.white;

    // Attachments & Mastery
    public bool hasScope = false;
    public bool hasSilencer = false;
    public bool hasExtMag = false;
    public bool hasGrip = false;
    public bool hasStock = false;
    public int masteryLevel = 1;
    public string activeSkin = "Default";

    // Weapon classification for UI / loot rarity
    public WeaponRarity rarity = WeaponRarity.Common;

    public static WeaponData GetDefaultWeapon(string name)
    {
        WeaponData w = ScriptableObject.CreateInstance<WeaponData>();
        w.weaponName = name; w.masteryLevel = 1; w.activeSkin = "Default";

        switch (name)
        {
            // ═══════════════════════════════════════════════════════════
            //  SMGs (12 total)
            // ═══════════════════════════════════════════════════════════
            case "MP40": w.damage = 22f; w.maxAmmo = 20; w.isAutomatic = true; w.fireRate = 0.07f; w.spread = 0.04f; w.reloadTime = 1.5f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Common; break;
            case "Vector": w.damage = 19f; w.maxAmmo = 25; w.isAutomatic = true; w.fireRate = 0.06f; w.spread = 0.05f; w.reloadTime = 1.4f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Rare; break;
            case "P90": w.damage = 21f; w.maxAmmo = 50; w.isAutomatic = true; w.fireRate = 0.08f; w.spread = 0.04f; w.reloadTime = 2.2f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Rare; break;
            case "UMP": w.damage = 24f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.1f; w.spread = 0.035f; w.reloadTime = 1.8f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Common; break;
            case "Mac10": w.damage = 20f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.075f; w.spread = 0.045f; w.reloadTime = 1.5f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Common; break;
            case "Thompson": w.damage = 25f; w.maxAmmo = 42; w.isAutomatic = true; w.fireRate = 0.09f; w.spread = 0.04f; w.reloadTime = 2.0f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Uncommon; break;
            case "Bizon": w.damage = 23f; w.maxAmmo = 60; w.isAutomatic = true; w.fireRate = 0.095f; w.spread = 0.042f; w.reloadTime = 2.5f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Uncommon; break;
            case "MP5": w.damage = 23f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.085f; w.spread = 0.038f; w.reloadTime = 1.7f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Common; break;
            case "CG15": w.damage = 26f; w.maxAmmo = 20; w.isAutomatic = true; w.fireRate = 0.12f; w.spread = 0.025f; w.reloadTime = 1.9f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Epic; break;
            case "MP9": w.damage = 21f; w.maxAmmo = 25; w.isAutomatic = true; w.fireRate = 0.072f; w.spread = 0.041f; w.reloadTime = 1.45f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = Color.cyan; w.rarity = WeaponRarity.Common; break;
            // NEW SMGs
            case "Havoc": w.damage = 24f; w.maxAmmo = 35; w.isAutomatic = true; w.fireRate = 0.055f; w.spread = 0.048f; w.reloadTime = 1.6f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = new Color(1f, 0.5f, 0f); w.rarity = WeaponRarity.Epic; break;
            case "Razorback": w.damage = 22f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.075f; w.spread = 0.03f; w.reloadTime = 1.5f; w.ammoType = AmmoType.SMGAmmo; w.iconColor = new Color(0f, 0.9f, 1f); w.rarity = WeaponRarity.Rare; break;

            // ═══════════════════════════════════════════════════════════
            //  ASSAULT RIFLES (14 total)
            // ═══════════════════════════════════════════════════════════
            case "AK47": w.damage = 32f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.12f; w.spread = 0.055f; w.reloadTime = 2.3f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Uncommon; break;
            case "SCAR": w.damage = 28f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.11f; w.spread = 0.04f; w.reloadTime = 2.0f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Uncommon; break;
            case "Groza": w.damage = 34f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.115f; w.spread = 0.038f; w.reloadTime = 2.1f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Legendary; break;
            case "FAMAS": w.damage = 26f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.09f; w.spread = 0.035f; w.reloadTime = 1.9f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Uncommon; break;
            case "M4A1": w.damage = 27f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.105f; w.spread = 0.038f; w.reloadTime = 1.9f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Common; break;
            case "XM8": w.damage = 29f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.11f; w.spread = 0.036f; w.reloadTime = 2.0f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Rare; break;
            case "AN94": w.damage = 31f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.125f; w.spread = 0.048f; w.reloadTime = 2.2f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Rare; break;
            case "AUG": w.damage = 28f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.11f; w.spread = 0.037f; w.reloadTime = 2.1f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Rare; break;
            case "Parafal": w.damage = 33f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.13f; w.spread = 0.05f; w.reloadTime = 2.4f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Epic; break;
            case "Kingfisher": w.damage = 27f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.095f; w.spread = 0.035f; w.reloadTime = 1.8f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Rare; break;
            case "G36": w.damage = 28f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.105f; w.spread = 0.039f; w.reloadTime = 2.0f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Uncommon; break;
            case "FAL": w.damage = 35f; w.maxAmmo = 20; w.isAutomatic = false; w.fireRate = 0.2f; w.spread = 0.025f; w.reloadTime = 2.5f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Epic; break;
            // NEW ARs
            case "Tempest": w.damage = 30f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.098f; w.spread = 0.032f; w.reloadTime = 2.0f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = new Color(0f, 0.6f, 1f); w.rarity = WeaponRarity.Epic; w.hasGrip = true; break;
            case "Spectre_AR": w.damage = 26f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.1f; w.spread = 0.03f; w.reloadTime = 1.8f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = new Color(0.6f, 0f, 0.8f); w.rarity = WeaponRarity.Epic; w.hasSilencer = true; break;

            // ═══════════════════════════════════════════════════════════
            //  SHOTGUNS (9 total)
            // ═══════════════════════════════════════════════════════════
            case "M1887": w.damage = 18f; w.maxAmmo = 2; w.isAutomatic = false; w.fireRate = 0.6f; w.spread = 0.12f; w.reloadTime = 1.8f; w.pellets = 10; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Uncommon; break;
            case "M1014": w.damage = 14f; w.maxAmmo = 6; w.isAutomatic = false; w.fireRate = 0.4f; w.spread = 0.15f; w.reloadTime = 2.6f; w.pellets = 8; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Common; break;
            case "SPAS12": w.damage = 16f; w.maxAmmo = 5; w.isAutomatic = false; w.fireRate = 0.5f; w.spread = 0.13f; w.reloadTime = 2.4f; w.pellets = 8; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Rare; break;
            case "MAG7": w.damage = 13f; w.maxAmmo = 8; w.isAutomatic = false; w.fireRate = 0.3f; w.spread = 0.14f; w.reloadTime = 2.2f; w.pellets = 7; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Common; break;
            case "ChargeBuster": w.damage = 20f; w.maxAmmo = 3; w.isAutomatic = false; w.fireRate = 0.7f; w.spread = 0.1f; w.reloadTime = 2.0f; w.pellets = 8; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Epic; break;
            case "Trogon": w.damage = 15f; w.maxAmmo = 12; w.isAutomatic = false; w.fireRate = 0.35f; w.spread = 0.15f; w.reloadTime = 2.8f; w.pellets = 8; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Uncommon; break;
            case "Striker12": w.damage = 12f; w.maxAmmo = 12; w.isAutomatic = false; w.fireRate = 0.3f; w.spread = 0.16f; w.reloadTime = 3.0f; w.pellets = 8; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Uncommon; break;
            // NEW Shotguns
            case "BreachersSG": w.damage = 17f; w.maxAmmo = 4; w.isAutomatic = false; w.fireRate = 0.55f; w.spread = 0.11f; w.reloadTime = 2.0f; w.pellets = 12; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = new Color(1f, 0.6f, 0f); w.rarity = WeaponRarity.Rare; break;
            case "Thunderbolt": w.damage = 22f; w.maxAmmo = 2; w.isAutomatic = false; w.fireRate = 0.8f; w.spread = 0.09f; w.reloadTime = 2.5f; w.pellets = 6; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = new Color(1f, 0.8f, 0f); w.rarity = WeaponRarity.Epic; break;

            // ═══════════════════════════════════════════════════════════
            //  SNIPER / DMR (10 total)
            // ═══════════════════════════════════════════════════════════
            case "AWM": w.damage = 90f; w.maxAmmo = 5; w.isAutomatic = false; w.fireRate = 1.5f; w.spread = 0.005f; w.reloadTime = 3.5f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.hasScope = true; w.rarity = WeaponRarity.Legendary; break;
            case "Kar98k": w.damage = 75f; w.maxAmmo = 5; w.isAutomatic = false; w.fireRate = 1.2f; w.spread = 0.008f; w.reloadTime = 3.0f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.hasScope = true; w.rarity = WeaponRarity.Uncommon; break;
            case "M82B": w.damage = 85f; w.maxAmmo = 5; w.isAutomatic = false; w.fireRate = 1.4f; w.spread = 0.006f; w.reloadTime = 3.2f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.hasScope = true; w.rarity = WeaponRarity.Epic; break;
            case "M24": w.damage = 80f; w.maxAmmo = 5; w.isAutomatic = false; w.fireRate = 1.3f; w.spread = 0.007f; w.reloadTime = 3.1f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.hasScope = true; w.rarity = WeaponRarity.Rare; break;
            case "SVD": w.damage = 55f; w.maxAmmo = 10; w.isAutomatic = false; w.fireRate = 0.5f; w.spread = 0.015f; w.reloadTime = 2.5f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.hasScope = true; w.rarity = WeaponRarity.Uncommon; break;
            case "Woodpecker": w.damage = 60f; w.maxAmmo = 12; w.isAutomatic = false; w.fireRate = 0.45f; w.spread = 0.012f; w.reloadTime = 2.3f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.hasScope = true; w.rarity = WeaponRarity.Rare; break;
            case "AC80": w.damage = 65f; w.maxAmmo = 10; w.isAutomatic = false; w.fireRate = 0.5f; w.spread = 0.014f; w.reloadTime = 2.4f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.hasScope = true; w.rarity = WeaponRarity.Rare; break;
            case "M14": w.damage = 48f; w.maxAmmo = 15; w.isAutomatic = false; w.fireRate = 0.35f; w.spread = 0.018f; w.reloadTime = 2.2f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.rarity = WeaponRarity.Uncommon; break;
            // NEW Snipers
            case "Valkyrie": w.damage = 95f; w.maxAmmo = 3; w.isAutomatic = false; w.fireRate = 2.0f; w.spread = 0.003f; w.reloadTime = 4.0f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = new Color(0f, 1f, 1f); w.hasScope = true; w.rarity = WeaponRarity.Legendary; break;
            case "Phantom": w.damage = 58f; w.maxAmmo = 8; w.isAutomatic = false; w.fireRate = 0.4f; w.spread = 0.013f; w.reloadTime = 2.4f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = new Color(0.5f, 0f, 0.5f); w.hasScope = true; w.rarity = WeaponRarity.Epic; w.hasSilencer = true; break;

            // ═══════════════════════════════════════════════════════════
            //  PISTOLS / SIDEARMS (8 total)
            // ═══════════════════════════════════════════════════════════
            case "DesertEagle": w.damage = 45f; w.maxAmmo = 7; w.isAutomatic = false; w.fireRate = 0.5f; w.spread = 0.015f; w.reloadTime = 1.6f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.white; w.rarity = WeaponRarity.Uncommon; break;
            case "G18": w.damage = 18f; w.maxAmmo = 15; w.isAutomatic = true; w.fireRate = 0.1f; w.spread = 0.035f; w.reloadTime = 1.3f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.white; w.rarity = WeaponRarity.Common; break;
            case "M500": w.damage = 50f; w.maxAmmo = 5; w.isAutomatic = false; w.fireRate = 0.6f; w.spread = 0.012f; w.reloadTime = 1.8f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.white; w.hasScope = true; w.rarity = WeaponRarity.Rare; break;
            case "USP": w.damage = 22f; w.maxAmmo = 12; w.isAutomatic = false; w.fireRate = 0.25f; w.spread = 0.025f; w.reloadTime = 1.2f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.white; w.rarity = WeaponRarity.Common; break;
            case "MiniUzi": w.damage = 16f; w.maxAmmo = 20; w.isAutomatic = true; w.fireRate = 0.065f; w.spread = 0.045f; w.reloadTime = 1.4f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.white; w.rarity = WeaponRarity.Common; break;
            case "TreatmentGun": w.damage = 15f; w.maxAmmo = 12; w.isAutomatic = false; w.fireRate = 0.3f; w.spread = 0.02f; w.reloadTime = 2.0f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.green; w.rarity = WeaponRarity.Rare; break;
            // NEW Pistols
            case "Python": w.damage = 55f; w.maxAmmo = 6; w.isAutomatic = false; w.fireRate = 0.55f; w.spread = 0.014f; w.reloadTime = 2.0f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = new Color(0.8f, 0.6f, 0f); w.rarity = WeaponRarity.Rare; break;
            case "Stinger": w.damage = 14f; w.maxAmmo = 20; w.isAutomatic = true; w.fireRate = 0.06f; w.spread = 0.04f; w.reloadTime = 1.2f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = new Color(1f, 0.8f, 0f); w.rarity = WeaponRarity.Common; break;

            // ═══════════════════════════════════════════════════════════
            //  MELEE (4 total)
            // ═══════════════════════════════════════════════════════════
            case "Katana": w.damage = 60f; w.maxAmmo = 1; w.isAutomatic = false; w.fireRate = 0.7f; w.spread = 0f; w.reloadTime = 0f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.gray; w.rarity = WeaponRarity.Rare; break;
            case "Pan": w.damage = 55f; w.maxAmmo = 1; w.isAutomatic = false; w.fireRate = 0.8f; w.spread = 0f; w.reloadTime = 0f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.gray; w.rarity = WeaponRarity.Common; break;
            case "Machete": w.damage = 58f; w.maxAmmo = 1; w.isAutomatic = false; w.fireRate = 0.75f; w.spread = 0f; w.reloadTime = 0f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.gray; w.rarity = WeaponRarity.Common; break;
            case "Bat": w.damage = 52f; w.maxAmmo = 1; w.isAutomatic = false; w.fireRate = 0.7f; w.spread = 0f; w.reloadTime = 0f; w.ammoType = AmmoType.PistolAmmo; w.iconColor = Color.gray; w.rarity = WeaponRarity.Common; break;

            // ═══════════════════════════════════════════════════════════
            //  SPECIALS (5 total)
            // ═══════════════════════════════════════════════════════════
            case "Crossbow": w.damage = 70f; w.maxAmmo = 1; w.isAutomatic = false; w.fireRate = 1.5f; w.spread = 0.01f; w.reloadTime = 2.5f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = Color.magenta; w.rarity = WeaponRarity.Rare; break;
            case "M79": w.damage = 150f; w.maxAmmo = 1; w.isAutomatic = false; w.fireRate = 2.0f; w.spread = 0.05f; w.reloadTime = 3.5f; w.ammoType = AmmoType.ShotgunAmmo; w.iconColor = Color.red; w.rarity = WeaponRarity.Legendary; break;
            case "Gatling": w.damage = 25f; w.maxAmmo = 120; w.isAutomatic = true; w.fireRate = 0.05f; w.spread = 0.05f; w.reloadTime = 4.5f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Legendary; break;
            // NEW Specials
            case "PlasmaCannon": w.damage = 120f; w.maxAmmo = 3; w.isAutomatic = false; w.fireRate = 1.8f; w.spread = 0.02f; w.reloadTime = 3.0f; w.ammoType = AmmoType.EnergyAmmo; w.iconColor = new Color(0f, 0.8f, 1f); w.rarity = WeaponRarity.Legendary; break;
            case "Harpoon": w.damage = 75f; w.maxAmmo = 3; w.isAutomatic = false; w.fireRate = 1.6f; w.spread = 0.008f; w.reloadTime = 2.8f; w.ammoType = AmmoType.SniperAmmo; w.iconColor = new Color(0f, 0.6f, 0.8f); w.rarity = WeaponRarity.Epic; break;

            // ═══════════════════════════════════════════════════════════
            //  ENERGY (2 total — new EnergyAmmo category)
            // ═══════════════════════════════════════════════════════════
            case "ArcBlade": w.damage = 65f; w.maxAmmo = 1; w.isAutomatic = false; w.fireRate = 0.6f; w.spread = 0f; w.reloadTime = 0f; w.ammoType = AmmoType.EnergyAmmo; w.iconColor = new Color(0f, 1f, 0.8f); w.rarity = WeaponRarity.Legendary; break;
            case "PulseRifle": w.damage = 35f; w.maxAmmo = 25; w.isAutomatic = true; w.fireRate = 0.13f; w.spread = 0.035f; w.reloadTime = 2.5f; w.ammoType = AmmoType.EnergyAmmo; w.iconColor = new Color(0.4f, 0f, 1f); w.rarity = WeaponRarity.Legendary; break;

            default: w.damage = 25f; w.maxAmmo = 30; w.isAutomatic = true; w.fireRate = 0.1f; w.spread = 0.05f; w.reloadTime = 2.0f; w.ammoType = AmmoType.RifleAmmo; w.iconColor = Color.yellow; w.rarity = WeaponRarity.Common; break;
        }
        return w;
    }

    /// <summary>Complete weapon catalog — 62 weapons across all categories.</summary>
    public static List<string> GetAllWeaponNames()
    {
        return new List<string>
        {
            // SMGs (12)
            "MP40", "Vector", "P90", "UMP", "Mac10", "Thompson", "Bizon", "MP5", "CG15", "MP9", "Havoc", "Razorback",
            // Assault Rifles (14)
            "AK47", "SCAR", "Groza", "FAMAS", "M4A1", "XM8", "AN94", "AUG", "Parafal", "Kingfisher", "G36", "FAL", "Tempest", "Spectre_AR",
            // Shotguns (9)
            "M1887", "M1014", "SPAS12", "MAG7", "ChargeBuster", "Trogon", "Striker12", "BreachersSG", "Thunderbolt",
            // Snipers / DMRs (10)
            "AWM", "Kar98k", "M82B", "M24", "SVD", "Woodpecker", "AC80", "M14", "Valkyrie", "Phantom",
            // Pistols / Sidearms (8)
            "DesertEagle", "G18", "M500", "USP", "MiniUzi", "TreatmentGun", "Python", "Stinger",
            // Melee (4)
            "Katana", "Pan", "Machete", "Bat",
            // Specials (5)
            "Crossbow", "M79", "Gatling", "PlasmaCannon", "Harpoon"
        };
    }

    /// <summary>Get weapons filtered by category for UI / loot tables.</summary>
    public static List<string> GetWeaponsByCategory(AmmoType category)
    {
        List<string> all = GetAllWeaponNames();
        List<string> filtered = new List<string>();
        foreach (string n in all) { WeaponData w = GetDefaultWeapon(n); if (w.ammoType == category) filtered.Add(n); }
        return filtered;
    }

    /// <summary>Get weapons filtered by rarity for loot tables / airdrops.</summary>
    public static List<string> GetWeaponsByRarity(WeaponRarity rarity)
    {
        List<string> all = GetAllWeaponNames();
        List<string> filtered = new List<string>();
        foreach (string n in all) { WeaponData w = GetDefaultWeapon(n); if (w.rarity == rarity) filtered.Add(n); }
        return filtered;
    }

    /// <summary>Check if weapon is a melee weapon (no ammo, no range).</summary>
    public bool IsMelee()
    {
        return spread == 0f && ammoType == AmmoType.PistolAmmo && maxAmmo == 1;
    }

    /// <summary>Check if weapon is a special/heavy weapon.</summary>
    public bool IsSpecial()
    {
        return damage >= 100f || weaponName == "Gatling" || weaponName == "PlasmaCannon" || weaponName == "Harpoon";
    }
}

/// <summary>Weapon rarity tiers for loot generation and UI color-coding.</summary>
public enum WeaponRarity
{
    Common,     // Gray — basic floor loot
    Uncommon,   // Green — slightly better stats
    Rare,       // Blue — noticeable advantage
    Epic,       // Purple — airdrop / high-tier loot
    Legendary   // Gold — rarest, best-in-class
}


