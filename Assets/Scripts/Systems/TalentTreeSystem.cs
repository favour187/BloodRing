using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UNIQUE FEATURE #4: Mid-Match Talent Tree (Unique to BloodRing this!)
/// As players earn kills and survive, they unlock talent points that can be spent
/// on a talent tree for real-time upgrades during the match.
/// This adds RPG-like progression within each match.
/// </summary>
public enum TalentBranch
{
    Combat,    // Weapon damage, fire rate, reload speed
    Survival,  // Max HP, armor, zone damage resist
    Mobility,  // Move speed, jump height, grapple range
    Tactical   // Grenade damage, trap effectiveness, ping range
}

[System.Serializable]
public class TalentNode
{
    public string id;
    public string name;
    public string description;
    public TalentBranch branch;
    public int tier;           // 1-4 (higher tiers unlock after spending X in branch)
    public int cost;           // Talent points to unlock
    public bool isUnlocked;
    public float value;        // The bonus provided

    public TalentNode(string id, string name, string desc, TalentBranch branch, int tier, int cost, float value)
    {
        this.id = id; this.name = name; this.description = desc;
        this.branch = branch; this.tier = tier; this.cost = cost;
        this.value = value; this.isUnlocked = false;
    }
}

public class TalentTreeSystem : MonoBehaviour
{
    public static TalentTreeSystem Instance;

    public int availablePoints = 0;
    public int totalPointsEarned = 0;
    private Dictionary<string, TalentNode> allTalents = new Dictionary<string, TalentNode>();
    private Dictionary<TalentBranch, int> branchInvestment = new Dictionary<TalentBranch, int>();

    // Active bonuses (computed from unlocked talents)
    public float bonusDamagePercent = 0f;
    public float bonusFireRatePercent = 0f;
    public float bonusReloadSpeedPercent = 0f;
    public float bonusMaxHP = 0f;
    public float bonusMaxArmor = 0f;
    public float bonusZoneResist = 0f;
    public float bonusMoveSpeed = 0f;
    public float bonusJumpForce = 0f;
    public float bonusGrenadeDamage = 0f;
    public float bonusTrapEffectiveness = 0f;

    private void Awake() { Instance = this; }

    public void InitializeTalentTree()
    {
        availablePoints = 0;
        totalPointsEarned = 0;
        branchInvestment[TalentBranch.Combat] = 0;
        branchInvestment[TalentBranch.Survival] = 0;
        branchInvestment[TalentBranch.Mobility] = 0;
        branchInvestment[TalentBranch.Tactical] = 0;

        // ── COMBAT BRANCH ──
        AddTalent(new TalentNode("C1_dmg", "Sharp Shooter", "+8% Weapon Damage", TalentBranch.Combat, 1, 1, 0.08f));
        AddTalent(new TalentNode("C1_rate", "Quick Trigger", "+10% Fire Rate", TalentBranch.Combat, 1, 1, 0.10f));
        AddTalent(new TalentNode("C2_dmg", "Deadly Aim", "+12% Weapon Damage", TalentBranch.Combat, 2, 2, 0.12f));
        AddTalent(new TalentNode("C2_reload", "Fast Hands", "+20% Reload Speed", TalentBranch.Combat, 2, 2, 0.20f));
        AddTalent(new TalentNode("C3_crit", "Head Hunter", "+15% Headshot Damage", TalentBranch.Combat, 3, 3, 0.15f));
        AddTalent(new TalentNode("C4_mastery", "Weapon Mastery", "+20% All Weapon Stats", TalentBranch.Combat, 4, 4, 0.20f));

        // ── SURVIVAL BRANCH ──
        AddTalent(new TalentNode("S1_hp", "Thick Skin", "+20 Max HP", TalentBranch.Survival, 1, 1, 20f));
        AddTalent(new TalentNode("S1_armor", "Iron Plates", "+15 Max Armor", TalentBranch.Survival, 1, 1, 15f));
        AddTalent(new TalentNode("S2_hp", "Vitality", "+30 Max HP", TalentBranch.Survival, 2, 2, 30f));
        AddTalent(new TalentNode("S2_zone", "Zone Walker", "-25% Zone Damage", TalentBranch.Survival, 2, 2, 0.25f));
        AddTalent(new TalentNode("S3_regen", "Regeneration", "Heal 2 HP/sec", TalentBranch.Survival, 3, 3, 2f));
        AddTalent(new TalentNode("S4_fortress", "Fortress", "+50 HP & Armor, -40% Zone", TalentBranch.Survival, 4, 4, 50f));

        // ── MOBILITY BRANCH ──
        AddTalent(new TalentNode("M1_speed", "Swift Feet", "+10% Move Speed", TalentBranch.Mobility, 1, 1, 0.10f));
        AddTalent(new TalentNode("M1_jump", "Spring Loaded", "+15% Jump Height", TalentBranch.Mobility, 1, 1, 0.15f));
        AddTalent(new TalentNode("M2_speed", "Wind Runner", "+15% Move Speed", TalentBranch.Mobility, 2, 2, 0.15f));
        AddTalent(new TalentNode("M2_slide", "Slide Master", "Unlock Combat Slide", TalentBranch.Mobility, 2, 2, 1f));
        AddTalent(new TalentNode("M3_silent", "Ghost Steps", "Silent Footsteps", TalentBranch.Mobility, 3, 3, 1f));
        AddTalent(new TalentNode("M4_flash", "Flash Step", "+30% Speed + Double Jump", TalentBranch.Mobility, 4, 4, 0.30f));

        // ── TACTICAL BRANCH ──
        AddTalent(new TalentNode("T1_nade", "Demolitions", "+20% Grenade Damage", TalentBranch.Tactical, 1, 1, 0.20f));
        AddTalent(new TalentNode("T1_trap", "Trapper", "+25% Trap Effectiveness", TalentBranch.Tactical, 1, 1, 0.25f));
        AddTalent(new TalentNode("T2_nade", "Bombardier", "+30% Grenade Radius", TalentBranch.Tactical, 2, 2, 0.30f));
        AddTalent(new TalentNode("T2_scan", "Scout Vision", "Ping reveals enemies 5s", TalentBranch.Tactical, 2, 2, 5f));
        AddTalent(new TalentNode("T3_emp", "EMP Expert", "EMPs last 2x longer", TalentBranch.Tactical, 3, 3, 2f));
        AddTalent(new TalentNode("T4_tactician", "Master Tactician", "+50% All Tactical Bonuses", TalentBranch.Tactical, 4, 4, 0.50f));

        Debug.Log("[TalentTree] Initialized with " + allTalents.Count + " talents.");
    }

    private void AddTalent(TalentNode node) { allTalents[node.id] = node; }

    /// <summary>
    /// Award talent points. Called when player gets a kill or survives a zone phase.
    /// </summary>
    public void AwardPoints(int points)
    {
        availablePoints += points;
        totalPointsEarned += points;

        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.AddKillFeedEntry("TALENT", "+" + points + " Talent Points! (" + availablePoints + " available)");
        }
    }

    /// <summary>
    /// Try to unlock a talent node. Returns true if successful.
    /// </summary>
    public bool UnlockTalent(string talentId)
    {
        if (!allTalents.ContainsKey(talentId)) return false;
        TalentNode node = allTalents[talentId];

        if (node.isUnlocked) return false;
        if (availablePoints < node.cost) return false;

        // Check tier requirement (must have spent enough in the branch)
        int invested = branchInvestment.ContainsKey(node.branch) ? branchInvestment[node.branch] : 0;
        int requiredInvestment = (node.tier - 1) * 2;
        if (invested < requiredInvestment) return false;

        // Unlock!
        availablePoints -= node.cost;
        node.isUnlocked = true;
        branchInvestment[node.branch] += node.cost;

        RecalculateBonuses();

        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.AddKillFeedEntry("TALENT", "Unlocked: " + node.name + "!");
        }

        return true;
    }

    private void RecalculateBonuses()
    {
        bonusDamagePercent = 0f; bonusFireRatePercent = 0f; bonusReloadSpeedPercent = 0f;
        bonusMaxHP = 0f; bonusMaxArmor = 0f; bonusZoneResist = 0f;
        bonusMoveSpeed = 0f; bonusJumpForce = 0f;
        bonusGrenadeDamage = 0f; bonusTrapEffectiveness = 0f;

        foreach (var kvp in allTalents)
        {
            TalentNode n = kvp.Value;
            if (!n.isUnlocked) continue;

            switch (n.id)
            {
                case "C1_dmg": bonusDamagePercent += n.value; break;
                case "C1_rate": bonusFireRatePercent += n.value; break;
                case "C2_dmg": bonusDamagePercent += n.value; break;
                case "C2_reload": bonusReloadSpeedPercent += n.value; break;
                case "C3_crit": bonusDamagePercent += n.value; break;
                case "C4_mastery":
                    bonusDamagePercent += n.value; bonusFireRatePercent += n.value; bonusReloadSpeedPercent += n.value; break;

                case "S1_hp": bonusMaxHP += n.value; break;
                case "S1_armor": bonusMaxArmor += n.value; break;
                case "S2_hp": bonusMaxHP += n.value; break;
                case "S2_zone": bonusZoneResist += n.value; break;
                case "S3_regen": break; // Handled in Update
                case "S4_fortress": bonusMaxHP += n.value; bonusMaxArmor += n.value; bonusZoneResist += 0.4f; break;

                case "M1_speed": bonusMoveSpeed += n.value; break;
                case "M1_jump": bonusJumpForce += n.value; break;
                case "M2_speed": bonusMoveSpeed += n.value; break;
                case "M3_silent": break; // Handled in audio
                case "M4_flash": bonusMoveSpeed += n.value; break;

                case "T1_nade": bonusGrenadeDamage += n.value; break;
                case "T1_trap": bonusTrapEffectiveness += n.value; break;
                case "T2_nade": bonusGrenadeDamage += n.value; break;
                case "T4_tactician":
                    bonusGrenadeDamage += bonusGrenadeDamage * n.value; bonusTrapEffectiveness += bonusTrapEffectiveness * n.value; break;
            }
        }
    }

    public bool IsTalentUnlocked(string talentId)
    {
        return allTalents.ContainsKey(talentId) && allTalents[talentId].isUnlocked;
    }

    public List<TalentNode> GetBranchTalents(TalentBranch branch)
    {
        List<TalentNode> result = new List<TalentNode>();
        foreach (var kvp in allTalents)
        {
            if (kvp.Value.branch == branch) result.Add(kvp.Value);
        }
        result.Sort((a, b) => a.tier.CompareTo(b.tier));
        return result;
    }

    public Dictionary<string, TalentNode> GetAllTalents() { return allTalents; }
}


