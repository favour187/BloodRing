using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Battle Pass Data — Dual-track battle pass with free and premium reward tiers.
/// Free track: rewards every level. Premium track: exclusive rewards every 5 levels.
/// 50 levels per season, 1000 XP per level.
/// </summary>
[CreateAssetMenu(fileName = "BattlePassData", menuName = "BloodRing/LiveOps/BattlePassData")]
public class BattlePassData : ScriptableObject
{
    [Header("Season Info")]
    public string seasonName = "Season 1: Blood Storm";
    public int seasonNumber = 1;
    
    [Header("Progress")]
    public int currentLevel = 1;
    public int maxLevel = 50;
    public int currentXP = 0;
    public int xpPerLevel = 1000;
    public bool isPremium = false;

    [Header("Rewards")]
    public BattlePassReward[] freeRewards;
    public BattlePassReward[] premiumRewards;

    // ── Default Reward Setup ────────────────────────────────────────

    public void InitializeDefaultRewards()
    {
        var freeList = new List<BattlePassReward>();
        var premiumList = new List<BattlePassReward>();

        // Free track — reward every level
        for (int i = 1; i <= maxLevel; i++)
        {
            string rewardName;
            string rewardType;
            int quantity;

            if (i % 10 == 0)
            {
                // Major milestone: crate
                rewardName = "Legendary Crate";
                rewardType = "CRATE";
                quantity = 1;
            }
            else if (i % 5 == 0)
            {
                // Minor milestone: diamonds
                rewardName = "Diamonds";
                rewardType = "DIAMONDS";
                quantity = 25 + (i / 5) * 5;
            }
            else if (i % 3 == 0)
            {
                rewardName = "XP Boost";
                rewardType = "BOOST";
                quantity = 1;
            }
            else
            {
                // Standard: blood coins
                rewardName = "Blood Coins";
                rewardType = "BLOOD_COINS";
                quantity = 200 + i * 20;
            }

            freeList.Add(new BattlePassReward
            {
                level = i,
                rewardName = rewardName,
                quantity = quantity,
                rewardType = rewardType,
                claimed = false
            });
        }

        // Premium track — exclusive rewards every 5 levels
        string[] premiumRewards = new string[]
        {
            "Storm Rider Set",        // Level 5
            "Blood Wings Parachute",  // Level 10
            "AWM Crimson Storm",      // Level 15
            "Victory Dance Emote",    // Level 20
            "Shadow Assassin Set",    // Level 25
            "Helicopter Inferno",     // Level 30
            "Pet: Blood Wolf",        // Level 35
            "M416 Dragon Scale",      // Level 40
            "Inferno Crown",          // Level 45
            "Legendary Hero Card",    // Level 50
        };

        for (int i = 0; i < premiumRewards.Length; i++)
        {
            int level = (i + 1) * 5;
            premiumList.Add(new BattlePassReward
            {
                level = level,
                rewardName = premiumRewards[i],
                quantity = 1,
                rewardType = level == 50 ? "LEGENDARY" : (level >= 25 ? "EPIC" : "RARE"),
                claimed = false
            });
        }

        this.freeRewards = freeList.ToArray();
        this.premiumRewards = premiumList.ToArray();

        Debug.Log($"[BattlePass] Initialized {freeList.Count} free + {premiumList.Count} premium rewards");
    }

    public int XPForNextLevel()
    {
        return xpPerLevel - currentXP;
    }

    public float LevelProgress()
    {
        return (float)currentXP / xpPerLevel;
    }
}

[System.Serializable]
public class BattlePassReward
{
    public int level;
    public string rewardName;
    public int quantity = 1;
    public string rewardType; // Skin, Currency, Weapon, Crate, etc.
    public bool claimed;
}
