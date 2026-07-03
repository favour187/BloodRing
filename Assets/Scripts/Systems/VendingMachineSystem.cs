using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — In-Match Tactical Vending Terminals & Blood Coins Economy.
/// Players collect Blood Coins during looting and spend them at tactical vending terminals across the island.
/// 100% original proprietary economy loop.
/// </summary>
public class VendingMachineSystem : MonoBehaviour
{
    public static VendingMachineSystem Instance;

    public struct VendingItem
    {
        public string itemId;
        public string displayName;
        public int costCoins;
        public string itemType; // CryoBarrier, Medkit, AwakeningCore, ReviveBeacon
    }

    private List<VendingItem> catalog = new List<VendingItem>();

    private void Awake()
    {
        Instance = this;
        InitializeCatalog();
    }

    private void InitializeCatalog()
    {
        catalog.Add(new VendingItem { itemId = "BR_CRYO_BARRIER", displayName = "Cryo-Barrier Shield x2", costCoins = 100, itemType = "CryoBarrier" });
        catalog.Add(new VendingItem { itemId = "BR_SUPER_MED", displayName = "Super Medkit & Plasma Bonfire", costCoins = 150, itemType = "Medkit" });
        catalog.Add(new VendingItem { itemId = "BR_AWAKENING_CORE", displayName = "Weapon Awakening Core Lv+1", costCoins = 200, itemType = "AwakeningCore" });
        catalog.Add(new VendingItem { itemId = "BR_REVIVE_BEACON", displayName = "Squad Revive Beacon", costCoins = 400, itemType = "ReviveBeacon" });
    }

    public List<VendingItem> GetCatalog() => catalog;

    /// <summary>Processes a purchase at an in-match vending machine terminal.</summary>
    public bool PurchaseItem(string itemId, int playerCoins, out int remainingCoins, out string statusMessage)
    {
        VendingItem target = catalog.Find(x => x.itemId == itemId);
        if (string.IsNullOrEmpty(target.itemId))
        {
            remainingCoins = playerCoins;
            statusMessage = "Item not found in Vending Terminal catalog.";
            return false;
        }

        if (playerCoins < target.costCoins)
        {
            remainingCoins = playerCoins;
            statusMessage = $"Insufficient Blood Coins! Need {target.costCoins}, have {playerCoins}.";
            return false;
        }

        remainingCoins = playerCoins - target.costCoins;
        statusMessage = $"Successfully purchased {target.displayName}!";
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UI_Confirm");
        return true;
    }
}
