using UnityEngine;

/// <summary>
/// M3 — Simple client-side ranking system (leaderboard ready).
/// Full authoritative version lives on backend.
/// </summary>
public class RankingSystem : MonoBehaviour
{
    public static RankingSystem Instance;

    public int currentRank = 1247;
    public int currentMMR = 1840;
    public string division = "Gold III";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddMMR(int amount)
    {
        currentMMR += amount;
        if (currentMMR >= 2000) division = "Platinum";
        else if (currentMMR >= 1600) division = "Gold III";
        Debug.Log($"[LiveOps] MMR updated: {currentMMR} ({division})");
    }

    public void UpdateRank(int newRank)
    {
        currentRank = newRank;
    }
}
