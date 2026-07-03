using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Weapon skin / cosmetic system.
/// Each skin is a visual variant of a weapon with unique colors, patterns, and effects.
/// Skins are purely cosmetic — they do not modify weapon stats.
/// Unlocked via battle pass, store, events, or crafting.
/// </summary>
public enum SkinTier { Standard, Deluxe, Premium, Legendary, Mythic }

[System.Serializable]
public class WeaponSkinData
{
    public string skinId;
    public string weaponName;       // Which weapon this skin is for
    public string displayName;
    public string description;
    public SkinTier tier;
    public Color primaryColor;
    public Color secondaryColor;
    public Color accentColor;
    public bool hasGlowEffect;
    public bool hasKillEffect;
    public bool hasTrailEffect;
    public string unlockMethod;     // "BattlePass", "Store", "Event", "Craft", "Airdrop"

    /// <summary>
    /// Built-in skin catalog. Each weapon has 3-5 skins across tiers.
    /// In production, skin data would come from server LiveOps config.
    /// </summary>
    public static List<WeaponSkinData> GetSkinsForWeapon(string weaponName)
    {
        List<WeaponSkinData> skins = new List<WeaponSkinData>();
        WeaponRarity baseRarity = WeaponData.GetDefaultWeapon(weaponName).rarity;

        // Universal skins — available for all weapons
        skins.Add(new WeaponSkinData {
            skinId = weaponName + "_Default", weaponName = weaponName,
            displayName = "Default", description = "Standard issue finish.",
            tier = SkinTier.Standard, primaryColor = new Color(0.3f, 0.3f, 0.3f),
            secondaryColor = new Color(0.2f, 0.2f, 0.2f), accentColor = Color.gray,
            unlockMethod = "Owned"
        });

        skins.Add(new WeaponSkinData {
            skinId = weaponName + "_Midnight", weaponName = weaponName,
            displayName = "Midnight Shadow", description = "Matte black stealth finish with subtle dark blue accents.",
            tier = SkinTier.Deluxe, primaryColor = new Color(0.05f, 0.05f, 0.08f),
            secondaryColor = new Color(0.1f, 0.1f, 0.2f), accentColor = new Color(0f, 0.3f, 0.8f),
            unlockMethod = "BattlePass"
        });

        skins.Add(new WeaponSkinData {
            skinId = weaponName + "_CrimsonBlaze", weaponName = weaponName,
            displayName = "Crimson Blaze", description = "Blood red carbon fiber with glowing orange heat lines.",
            tier = SkinTier.Premium, primaryColor = new Color(0.8f, 0.05f, 0.05f),
            secondaryColor = new Color(0.1f, 0.05f, 0.05f), accentColor = new Color(1f, 0.4f, 0f),
            hasGlowEffect = true, unlockMethod = "Store"
        });

        skins.Add(new WeaponSkinData {
            skinId = weaponName + "_NeonCircuit", weaponName = weaponName,
            displayName = "Neon Circuit", description = "Cyberpunk chrome with glowing cyan circuit board patterns.",
            tier = SkinTier.Legendary, primaryColor = new Color(0.7f, 0.7f, 0.8f),
            secondaryColor = new Color(0.1f, 0.1f, 0.15f), accentColor = new Color(0f, 1f, 1f),
            hasGlowEffect = true, hasTrailEffect = true, unlockMethod = "Event"
        });

        // Weapon-specific legendary skins
        if (weaponName == "AK47" || weaponName == "M4A1" || weaponName == "Groza")
        {
            skins.Add(new WeaponSkinData {
                skinId = weaponName + "_DragonScale", weaponName = weaponName,
                displayName = "Dragon Scale", description = "Ancient dragon forged steel with ember glow particles.",
                tier = SkinTier.Mythic, primaryColor = new Color(0.2f, 0.15f, 0.1f),
                secondaryColor = new Color(0.6f, 0.3f, 0f), accentColor = new Color(1f, 0.5f, 0f),
                hasGlowEffect = true, hasKillEffect = true, hasTrailEffect = true,
                unlockMethod = "Craft"
            });
        }

        if (weaponName == "AWM" || weaponName == "Valkyrie")
        {
            skins.Add(new WeaponSkinData {
                skinId = weaponName + "_ArcticWolf", weaponName = weaponName,
                displayName = "Arctic Wolf", description = "Frozen steel with ice crystal formations and frost trail.",
                tier = SkinTier.Mythic, primaryColor = new Color(0.85f, 0.9f, 1f),
                secondaryColor = new Color(0.6f, 0.75f, 0.9f), accentColor = new Color(0.4f, 0.8f, 1f),
                hasGlowEffect = true, hasKillEffect = true, hasTrailEffect = true,
                unlockMethod = "Airdrop"
            });
        }

        if (weaponName == "Katana")
        {
            skins.Add(new WeaponSkinData {
                skinId = weaponName + "_SakuraBlade", weaponName = weaponName,
                displayName = "Sakura Blade", description = "Cherry blossom etched steel with petal particle trail.",
                tier = SkinTier.Mythic, primaryColor = new Color(1f, 0.7f, 0.8f),
                secondaryColor = new Color(0.9f, 0.3f, 0.5f), accentColor = new Color(1f, 0.9f, 0.9f),
                hasGlowEffect = true, hasKillEffect = true, hasTrailEffect = true,
                unlockMethod = "Event"
            });
        }

        return skins;
    }

    /// <summary>Get skin tier color for UI display.</summary>
    public static Color GetTierColor(SkinTier tier)
    {
        switch (tier)
        {
            case SkinTier.Standard: return new Color(0.6f, 0.6f, 0.6f);
            case SkinTier.Deluxe: return new Color(0.2f, 0.6f, 0.2f);
            case SkinTier.Premium: return new Color(0.2f, 0.4f, 0.9f);
            case SkinTier.Legendary: return new Color(0.7f, 0.3f, 0.9f);
            case SkinTier.Mythic: return new Color(1f, 0.7f, 0f);
            default: return Color.white;
        }
    }

    /// <summary>Get rarity border color for weapon cards.</summary>
    public static Color GetRarityColor(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Common: return new Color(0.6f, 0.6f, 0.6f);
            case WeaponRarity.Uncommon: return new Color(0.2f, 0.8f, 0.2f);
            case WeaponRarity.Rare: return new Color(0.2f, 0.5f, 1f);
            case WeaponRarity.Epic: return new Color(0.7f, 0.2f, 0.9f);
            case WeaponRarity.Legendary: return new Color(1f, 0.7f, 0f);
            default: return Color.white;
        }
    }
}
