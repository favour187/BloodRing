using UnityEngine;
using System;

/// <summary>
/// M3 — Seasonal Events & Limited-Time content manager.
/// Ready for remote config from backend.
/// </summary>
public class SeasonalEventManager : MonoBehaviour
{
    public static SeasonalEventManager Instance;

    public string currentEventName = "Blood Storm Launch";
    public DateTime eventEndTime = new DateTime(2026, 8, 15, 23, 59, 59);

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsEventActive()
    {
        return DateTime.UtcNow < eventEndTime;
    }

    public void ClaimEventReward(string rewardId)
    {
        Debug.Log($"[LiveOps] Claimed event reward: {rewardId}");
    }
}
