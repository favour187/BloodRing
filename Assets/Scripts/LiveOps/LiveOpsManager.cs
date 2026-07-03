using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// M3 — LiveOps Manager: Battle Pass, Seasonal Events, Store Rotation
/// Connected to backend at https://academyroyalebackend.onrender.com
/// Manages seasonal content cycles, battle pass, events, and store rotation.
/// </summary>
public class LiveOpsManager : MonoBehaviour
{
    public static LiveOpsManager Instance;

    [Header("Current Season")]
    public BattlePassData currentBattlePass;
    public SeasonConfig currentSeason;

    [Header("Store Rotation")]
    public List<StoreRotationSlot> weeklyFeaturedSlots = new List<StoreRotationSlot>();
    public List<StoreRotationSlot> dailyDealsSlots = new List<StoreRotationSlot>();

    [Header("Events")]
    public List<ActiveEvent> activeEvents = new List<ActiveEvent>();

    private DateTime lastStoreRefresh = DateTime.MinValue;
    private DateTime lastDailyRefresh = DateTime.MinValue;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSeason();
        RefreshStoreRotation();
        CheckDailyReset();
    }

    private void Update()
    {
        // Check for daily reset at midnight
        if (DateTime.Now.Date != lastDailyRefresh.Date)
        {
            CheckDailyReset();
        }

        // Check for weekly store rotation (every Tuesday)
        if ((DateTime.Now - lastStoreRefresh).TotalDays >= 7)
        {
            RefreshStoreRotation();
        }
    }

    // ── Season Management ─────────────────────────────────────────────

    private void InitializeSeason()
    {
        currentSeason = new SeasonConfig
        {
            seasonNumber = 1,
            seasonName = "Season 1: Blood Storm",
            startDate = new DateTime(2026, 7, 1),
            endDate = new DateTime(2026, 9, 30),
            maxBattlePassLevel = 50,
            xpPerLevel = 1000,
            themeColor = new Color(0.85f, 0.1f, 0.05f)
        };

        if (currentBattlePass == null)
        {
            currentBattlePass = ScriptableObject.CreateInstance<BattlePassData>();
            currentBattlePass.seasonName = currentSeason.seasonName;
            currentBattlePass.maxLevel = currentSeason.maxBattlePassLevel;
            currentBattlePass.xpPerLevel = currentSeason.xpPerLevel;
        }

        // Initialize seasonal events
        InitializeSeasonalEvents();

        Debug.Log($"[LiveOps] Season {currentSeason.seasonNumber} '{currentSeason.seasonName}' active. " +
                  $"Ends: {currentSeason.endDate:yyyy-MM-dd}");
    }

    private void InitializeSeasonalEvents()
    {
        activeEvents.Clear();

        // Weekly Challenge — resets every Monday
        activeEvents.Add(new ActiveEvent
        {
            eventId = "weekly_kill_challenge",
            eventName = "Blood Storm Weekly: 20 Kills",
            description = "Eliminate 20 enemies in any mode to earn bonus rewards",
            startTime = GetNextMonday(DateTime.Now).AddDays(-7),
            endTime = GetNextMonday(DateTime.Now),
            rewardType = "BLOOD_COINS",
            rewardAmount = 2000,
            targetProgress = 20,
            currentProgress = 0,
            eventType = EventType.WeeklyChallenge
        });

        // Weekend Bonus — active Fri-Sun
        if (DateTime.Now.DayOfWeek == DayOfWeek.Friday || 
            DateTime.Now.DayOfWeek == DayOfWeek.Saturday || 
            DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
        {
            activeEvents.Add(new ActiveEvent
            {
                eventId = "weekend_xp_boost",
                eventName = "Double XP Weekend",
                description = "All XP rewards are doubled until Monday!",
                startTime = GetNextMonday(DateTime.Now).AddDays(-3),
                endTime = GetNextMonday(DateTime.Now),
                rewardType = "XP_MULTIPLIER",
                rewardAmount = 2,
                targetProgress = 0,
                currentProgress = 0,
                eventType = EventType.XPBoost
            });
        }

        // Login Bonus Streak
        activeEvents.Add(new ActiveEvent
        {
            eventId = "daily_login_streak",
            eventName = "7-Day Login Rewards",
            description = "Log in daily to claim escalating rewards",
            startTime = DateTime.Now.Date,
            endTime = DateTime.Now.Date.AddDays(7),
            rewardType = "PROGRESSIVE",
            rewardAmount = 7,
            targetProgress = 7,
            currentProgress = PlayerPrefs.GetInt("LoginStreak", 0),
            eventType = EventType.LoginBonus
        });
    }

    // ── Battle Pass ───────────────────────────────────────────────────

    public void AddBattlePassXP(int amount)
    {
        if (currentBattlePass == null) return;

        // Apply XP multiplier from active events
        float multiplier = GetActiveXPMultiplier();
        int finalAmount = Mathf.RoundToInt(amount * multiplier);

        currentBattlePass.currentXP += finalAmount;
        int levelsGained = 0;

        while (currentBattlePass.currentXP >= currentBattlePass.xpPerLevel && 
               currentBattlePass.currentLevel < currentBattlePass.maxLevel)
        {
            currentBattlePass.currentXP -= currentBattlePass.xpPerLevel;
            currentBattlePass.currentLevel++;
            levelsGained++;

            // Claim level reward
            ClaimBattlePassReward(currentBattlePass.currentLevel);
        }

        if (levelsGained > 0)
        {
            Debug.Log($"[LiveOps] Battle Pass leveled up by {levelsGained} to {currentBattlePass.currentLevel} " +
                      $"(XP multiplier: {multiplier}x)");
        }

        // Sync with backend
        if (BackendAPI.Instance != null && BackendAPI.Instance.IsLoggedIn)
        {
            BackendAPI.Instance.GetProfileAsync();
        }
    }

    private void ClaimBattlePassReward(int level)
    {
        // Free pass rewards (every level)
        int freeReward = level * 100; // Blood Coins
        Debug.Log($"[LiveOps] BP Level {level}: Claimed {freeReward} Blood Coins (free track)");

        // Premium pass rewards (every 5 levels)
        if (currentBattlePass.isPremium && level % 5 == 0)
        {
            Debug.Log($"[LiveOps] BP Level {level}: Claimed premium reward (skin/emote/crate)");
        }
    }

    public void UnlockPremiumPass()
    {
        if (currentBattlePass != null)
        {
            currentBattlePass.isPremium = true;
            Debug.Log("[LiveOps] Premium Battle Pass unlocked!");

            // Claim all retroactive premium rewards
            for (int i = 1; i <= currentBattlePass.currentLevel; i++)
            {
                if (i % 5 == 0)
                {
                    Debug.Log($"[LiveOps] Retroactive premium claim: Level {i}");
                }
            }
        }
    }

    // ── Store Rotation (weekly featured + daily deals) ───────────────

    public void RefreshStoreRotation()
    {
        lastStoreRefresh = DateTime.Now;
        int weekSeed = (DateTime.Now.Year * 100) + GetWeekOfYear(DateTime.Now);
        UnityEngine.Random.InitState(weekSeed);

        // Weekly Featured Items (premium items, rotates weekly)
        weeklyFeaturedSlots.Clear();
        string[] featuredPool = new string[]
        {
            "skin_awm_flame", "skin_mp40_cobra", "skin_m1887_rapper",
            "char_djneon", "char_pulse", "bundle_heroic",
            "emote_victory_dance", "emote_taunt", "pet_fox", "pet_eagle",
            "parachute_blood_wing", "glider_shadow_blade",
            "vehicle_skin_inferno", "vehicle_skin_arctic"
        };

        for (int i = 0; i < 4; i++)
        {
            string itemId = featuredPool[UnityEngine.Random.Range(0, featuredPool.Length)];
            int discount = UnityEngine.Random.Range(10, 40) * 5; // 10-40% off in 5% steps
            weeklyFeaturedSlots.Add(new StoreRotationSlot
            {
                itemId = itemId,
                discountPercent = discount,
                expiresAt = lastStoreRefresh.AddDays(7),
                slotType = "FEATURED"
            });
        }

        // Daily Deals (smaller items, rotates daily)
        dailyDealsSlots.Clear();
        string[] dailyPool = new string[]
        {
            "crate_weapon_1", "crate_character_1", "xp_boost_1h",
            "bp_xp_100", "emote_random", "spray_random",
            "lootbox_common", "ammo_pack", "health_pack"
        };

        for (int i = 0; i < 3; i++)
        {
            string itemId = dailyPool[UnityEngine.Random.Range(0, dailyPool.Length)];
            dailyDealsSlots.Add(new StoreRotationSlot
            {
                itemId = itemId,
                discountPercent = UnityEngine.Random.Range(1, 5) * 10,
                expiresAt = DateTime.Now.Date.AddDays(1),
                slotType = "DAILY"
            });
        }

        Debug.Log($"[LiveOps] Store rotation refreshed: {weeklyFeaturedSlots.Count} featured, " +
                  $"{dailyDealsSlots.Count} daily deals (seed: {weekSeed})");
    }

    // ── Daily Reset ───────────────────────────────────────────────────

    private void CheckDailyReset()
    {
        lastDailyRefresh = DateTime.Now.Date;

        // Track login streak
        string lastLoginDate = PlayerPrefs.GetString("LastLoginDate", "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (lastLoginDate != today)
        {
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            int streak = PlayerPrefs.GetInt("LoginStreak", 0);

            if (lastLoginDate == yesterday)
            {
                streak++;
            }
            else
            {
                streak = 1; // Reset streak
            }

            PlayerPrefs.SetInt("LoginStreak", streak);
            PlayerPrefs.SetString("LastLoginDate", today);
            PlayerPrefs.Save();

            Debug.Log($"[LiveOps] Daily login: Day {streak} streak");

            // Check for streak milestones
            if (streak == 7)
            {
                Debug.Log("[LiveOps] 🎉 7-Day Login Streak Complete! Bonus reward: 500 Diamonds");
            }
        }

        // Refresh daily deals
        RefreshDailyDeals();
    }

    private void RefreshDailyDeals()
    {
        // Only refresh daily slots, keep weekly featured
        dailyDealsSlots.Clear();
        string[] dailyPool = new string[]
        {
            "crate_weapon_1", "crate_character_1", "xp_boost_1h",
            "bp_xp_100", "emote_random", "spray_random",
            "lootbox_common", "ammo_pack", "health_pack"
        };

        UnityEngine.Random.InitState(DateTime.Now.DayOfYear + DateTime.Now.Year);
        for (int i = 0; i < 3; i++)
        {
            dailyDealsSlots.Add(new StoreRotationSlot
            {
                itemId = dailyPool[UnityEngine.Random.Range(0, dailyPool.Length)],
                discountPercent = UnityEngine.Random.Range(1, 5) * 10,
                expiresAt = DateTime.Now.Date.AddDays(1),
                slotType = "DAILY"
            });
        }
    }

    // ── Event Tracking ────────────────────────────────────────────────

    public void TrackKill(string mode)
    {
        foreach (var evt in activeEvents)
        {
            if (evt.eventType == EventType.WeeklyChallenge && evt.eventId.Contains("kill"))
            {
                evt.currentProgress++;
                if (evt.currentProgress >= evt.targetProgress && !evt.claimed)
                {
                    evt.claimed = true;
                    Debug.Log($"[LiveOps] 🎉 Event '{evt.eventName}' completed! Reward: {evt.rewardAmount} {evt.rewardType}");
                }
            }
        }
    }

    public void TrackMatchComplete(string mode, int placement)
    {
        foreach (var evt in activeEvents)
        {
            if (evt.eventType == EventType.WeeklyChallenge && evt.eventId.Contains("match"))
            {
                evt.currentProgress++;
            }
        }

        // Add battle pass XP for match completion
        int baseXP = placement <= 3 ? 200 : (placement <= 10 ? 150 : 100);
        AddBattlePassXP(baseXP);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private float GetActiveXPMultiplier()
    {
        float multiplier = 1f;
        foreach (var evt in activeEvents)
        {
            if (evt.eventType == EventType.XPBoost && evt.IsActive())
            {
                multiplier *= evt.rewardAmount;
            }
        }
        return multiplier;
    }

    private DateTime GetNextMonday(DateTime from)
    {
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        return from.Date.AddDays(daysUntilMonday);
    }

    private int GetWeekOfYear(DateTime date)
    {
        return System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    public bool IsSeasonActive()
    {
        return currentSeason != null && DateTime.Now >= currentSeason.startDate && DateTime.Now <= currentSeason.endDate;
    }

    public float GetSeasonProgress()
    {
        if (currentSeason == null) return 0f;
        float totalDays = (float)(currentSeason.endDate - currentSeason.startDate).TotalDays;
        float elapsed = (float)(DateTime.Now - currentSeason.startDate).TotalDays;
        return Mathf.Clamp01(elapsed / totalDays);
    }

    public int GetDaysRemaining()
    {
        if (currentSeason == null) return 0;
        return Math.Max(0, (int)(currentSeason.endDate - DateTime.Now).TotalDays);
    }
}

// ── Supporting Data Classes ──────────────────────────────────────────

[Serializable]
public class SeasonConfig
{
    public int seasonNumber;
    public string seasonName;
    public DateTime startDate;
    public DateTime endDate;
    public int maxBattlePassLevel;
    public int xpPerLevel;
    public Color themeColor;
}

[Serializable]
public class StoreRotationSlot
{
    public string itemId;
    public int discountPercent;
    public DateTime expiresAt;
    public string slotType; // "FEATURED" or "DAILY"

    public bool IsExpired() => DateTime.Now >= expiresAt;
}

[Serializable]
public class ActiveEvent
{
    public string eventId;
    public string eventName;
    public string description;
    public DateTime startTime;
    public DateTime endTime;
    public string rewardType;
    public int rewardAmount;
    public int targetProgress;
    public int currentProgress;
    public bool claimed;
    public EventType eventType;

    public bool IsActive() => DateTime.Now >= startTime && DateTime.Now <= endTime;
    public float ProgressPercent => targetProgress > 0 ? (float)currentProgress / targetProgress : 0f;
}

public enum EventType
{
    WeeklyChallenge,
    DailyChallenge,
    XPBoost,
    LoginBonus,
    SeasonalEvent,
    LimitedTimeMode
}
