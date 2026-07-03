using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Store Rotation Manager — manages the Free Fire-style shop rotation.
/// Handles weekly featured items, daily deals, and lucky draw/elite pass items.
/// Connected to backend store API at /api/store
/// </summary>
public class StoreRotationManager : MonoBehaviour
{
    public static StoreRotationManager Instance;

    [Header("Store State")]
    public List<StoreItemData> featuredItems = new List<StoreItemData>();
    public List<StoreItemData> dailyItems = new List<StoreItemData>();
    public List<StoreItemData> premiumItems = new List<StoreItemData>();

    [Header("Lucky Draw")]
    public LuckyDrawConfig currentLuckyDraw;
    public int spinCostGems = 30;
    public int[] cumulativeCosts = { 30, 60, 120, 240, 480, 960, 1920, 3840 };

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeStore();
    }

    private void InitializeStore()
    {
        // Featured items (premium skins, characters, bundles)
        featuredItems = new List<StoreItemData>
        {
            new StoreItemData { itemId = "char_djneon", itemName = "DJ Neon", itemType = "CHARACTER",
                priceDiamonds = 599, priceCoins = 15000, rarity = "EPIC", 
                description = "Bass-dropping DJ with Sound Blast ability",
                iconPath = "UI/Icons/Char_DJNeon" },
            new StoreItemData { itemId = "char_pulse", itemName = "Pulse", itemType = "CHARACTER",
                priceDiamonds = 499, priceCoins = 12000, rarity = "RARE",
                description = "Tech specialist with Shield Pulse ability",
                iconPath = "UI/Icons/Char_Pulse" },
            new StoreItemData { itemId = "skin_awm_flame", itemName = "AWM Dragon Flame", itemType = "WEAPON_SKIN",
                priceDiamonds = 299, priceCoins = 8000, rarity = "LEGENDARY",
                description = "Legendary AWM skin with flame VFX",
                iconPath = "UI/Icons/Skin_AWM_Flame" },
            new StoreItemData { itemId = "skin_mp40_cobra", itemName = "MP40 Cobra", itemType = "WEAPON_SKIN",
                priceDiamonds = 399, priceCoins = 10000, rarity = "EPIC",
                description = "Predatory cobra-themed MP40 skin",
                iconPath = "UI/Icons/Skin_MP40_Cobra" },
            new StoreItemData { itemId = "bundle_heroic", itemName = "Heroic Warrior Bundle", itemType = "BUNDLE",
                priceDiamonds = 899, priceCoins = 25000, rarity = "LEGENDARY",
                description = "Complete character outfit + weapon skin + emote",
                iconPath = "UI/Icons/Bundle_Heroic" },
        };

        // Daily rotation items
        dailyItems = new List<StoreItemData>
        {
            new StoreItemData { itemId = "crate_weapon_1", itemName = "Weapon Crate", itemType = "CRATE",
                priceDiamonds = 50, priceCoins = 1500, rarity = "COMMON",
                description = "Contains random weapon skin fragments" },
            new StoreItemData { itemId = "xp_boost_1h", itemName = "XP Boost (1 Hour)", itemType = "BOOST",
                priceDiamonds = 25, priceCoins = 500, rarity = "COMMON",
                description = "Double XP for 1 hour" },
            new StoreItemData { itemId = "bp_xp_100", itemName = "100 Battle Pass XP", itemType = "CONSUMABLE",
                priceDiamonds = 15, priceCoins = 300, rarity = "COMMON",
                description = "Instantly gain 100 Battle Pass XP" },
        };

        // Lucky draw setup
        currentLuckyDraw = new LuckyDrawConfig
        {
            drawName = "Inferno Draw",
            themeColor = new Color(1f, 0.4f, 0f),
            prizes = new List<LuckyDrawPrize>
            {
                new LuckyDrawPrize { prizeId = "grand_prize", name = "Inferno AK47", rarity = "LEGENDARY", weight = 1 },
                new LuckyDrawPrize { prizeId = "epic_1", name = "Flame Jacket", rarity = "EPIC", weight = 5 },
                new LuckyDrawPrize { prizeId = "epic_2", name = "Fire Dance Emote", rarity = "EPIC", weight = 5 },
                new LuckyDrawPrize { prizeId = "rare_1", name = "200 Diamonds", rarity = "RARE", weight = 15 },
                new LuckyDrawPrize { prizeId = "rare_2", name = "5000 Coins", rarity = "RARE", weight = 20 },
                new LuckyDrawPrize { prizeId = "common_1", name = "50 Diamonds", rarity = "COMMON", weight = 25 },
                new LuckyDrawPrize { prizeId = "common_2", name = "1000 Coins", rarity = "COMMON", weight = 29 },
            }
        };

        Debug.Log($"[Store] Store initialized: {featuredItems.Count} featured, {dailyItems.Count} daily, " +
                  $"Lucky Draw: '{currentLuckyDraw.drawName}'");
    }

    // ── Lucky Draw System (Free Fire gold/diamond royale) ─────────────

    public LuckyDrawResult SpinLuckyDraw(bool useGems = true)
    {
        int cost = GetNextSpinCost();
        LuckyDrawResult result = new LuckyDrawResult();

        // Weighted random selection
        int totalWeight = 0;
        foreach (var prize in currentLuckyDraw.prizes)
            totalWeight += prize.weight;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var prize in currentLuckyDraw.prizes)
        {
            cumulative += prize.weight;
            if (roll < cumulative)
            {
                result.prize = prize;
                result.costPaid = cost;
                result.success = true;

                Debug.Log($"[Store] Lucky Draw landed on: {prize.name} ({prize.rarity}) - Cost: {cost} gems");

                // Remove from pool (no repeats until reset)
                prize.weight = 0;
                break;
            }
        }

        return result;
    }

    public int GetNextSpinCost()
    {
        int spinsDone = PlayerPrefs.GetInt("LuckyDrawSpins", 0);
        if (spinsDone < cumulativeCosts.Length)
            return cumulativeCosts[spinsDone];
        return cumulativeCosts[cumulativeCosts.Length - 1];
    }

    public void ResetLuckyDraw()
    {
        PlayerPrefs.SetInt("LuckyDrawSpins", 0);
        PlayerPrefs.Save();
        // Reset weights
        InitializeStore();
    }

    // ── Purchase Flow ─────────────────────────────────────────────────

    public bool CanAfford(string itemId, string currency)
    {
        // Check against backend profile
        if (BackendAPI.Instance == null || BackendAPI.Instance.CurrentProfile == null)
            return false;

        var profile = BackendAPI.Instance.CurrentProfile;
        var item = GetItemById(itemId);
        if (item == null) return false;

        if (currency == "DIAMONDS")
            return profile.diamonds >= item.priceDiamonds;
        else
            return profile.bloodCoins >= item.priceCoins;
    }

    public async void PurchaseItem(string itemId, string currency = "GEMS")
    {
        if (BackendAPI.Instance == null) return;

        var result = await BackendAPI.Instance.BuyStoreItemAsync(itemId, currency);
        if (result != null && string.IsNullOrEmpty(result.error))
        {
            Debug.Log($"[Store] Purchased {itemId} with {currency}: {result.message}");
        }
        else
        {
            Debug.LogWarning($"[Store] Purchase failed for {itemId}: {result?.error}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private StoreItemData GetItemById(string itemId)
    {
        foreach (var item in featuredItems)
            if (item.itemId == itemId) return item;
        foreach (var item in dailyItems)
            if (item.itemId == itemId) return item;
        foreach (var item in premiumItems)
            if (item.itemId == itemId) return item;
        return null;
    }
}

// ── Supporting Data Classes ──────────────────────────────────────────

[Serializable]
public class StoreItemData
{
    public string itemId;
    public string itemName;
    public string itemType;
    public int priceDiamonds;
    public int priceCoins;
    public string rarity;
    public string description;
    public string iconPath;
}

[Serializable]
public class LuckyDrawConfig
{
    public string drawName;
    public Color themeColor;
    public List<LuckyDrawPrize> prizes;
}

[Serializable]
public class LuckyDrawPrize
{
    public string prizeId;
    public string name;
    public string rarity;
    public int weight;
}

[Serializable]
public class LuckyDrawResult
{
    public LuckyDrawPrize prize;
    public int costPaid;
    public bool success;
}
