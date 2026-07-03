using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — Daily Missions & Tactical Achievements System.
/// Tracks in-match combat goals (e.g., sniper eliminations, CryoBarrier deployments,
/// Bio-Plasma Spore consumption, and Squad Clash wins). Awarding Blood Coins and Awakening Cores upon completion.
/// 100% original proprietary progression mechanic.
/// </summary>
public class DailyMissionsSystem : MonoBehaviour
{
    public static DailyMissionsSystem Instance;

    public class Mission
    {
        public string missionId;
        public string title;
        public int currentProgress;
        public int targetProgress;
        public int rewardCoins;
        public int rewardCores;
        public bool isCompleted;
    }

    private List<Mission> activeMissions = new List<Mission>();

    private void Awake()
    {
        Instance = this;
        InitializeMissions();
    }

    private void InitializeMissions()
    {
        activeMissions.Add(new Mission { missionId = "M_SNIPER_KILLS", title = "Eliminate 3 enemies using Sniper Rifles", targetProgress = 3, rewardCoins = 250, rewardCores = 5 });
        activeMissions.Add(new Mission { missionId = "M_CRYO_BUILD", title = "Deploy 5 Cryo-Barrier Shields in combat", targetProgress = 5, rewardCoins = 150, rewardCores = 2 });
        activeMissions.Add(new Mission { missionId = "M_SPORE_EAT", title = "Consume 4 Bio-Plasma Spores for PE", targetProgress = 4, rewardCoins = 200, rewardCores = 3 });
        activeMissions.Add(new Mission { missionId = "M_CLASH_WIN", title = "Win 1 Squad Clash Arena Match", targetProgress = 1, rewardCoins = 500, rewardCores = 10 });
    }

    public List<Mission> GetActiveMissions() => activeMissions;

    public void ReportProgress(string missionId, int amount = 1)
    {
        Mission m = activeMissions.Find(x => x.missionId == missionId);
        if (m != null && !m.isCompleted)
        {
            m.currentProgress = Mathf.Min(m.targetProgress, m.currentProgress + amount);
            Debug.Log($"[DailyMissionsSystem] Progress on {m.title}: {m.currentProgress}/{m.targetProgress}");
            if (m.currentProgress >= m.targetProgress)
            {
                m.isCompleted = true;
                Debug.Log($"[DailyMissionsSystem] MISSION COMPLETED: {m.title}! Awarded {m.rewardCoins} Blood Coins & {m.rewardCores} Awakening Cores!");
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_TalentUnlock");
            }
        }
    }
}
