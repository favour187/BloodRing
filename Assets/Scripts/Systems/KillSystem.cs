using UnityEngine;
using System.Collections;

public class KillSystem : MonoBehaviour
{
    public static KillSystem Instance;

    private int aliveCount = 20;
    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;
    }

    public void InitializeKillSystem()
    {
        aliveCount = 20;
        isGameOver = false;
        MatchData data = MatchData.Load();
        data.ResetData();
        if (GameHUD.Instance != null) GameHUD.Instance.UpdateAliveCount(aliveCount);
    }

    public void OnEntityKilled(string victimName, string killerName, bool isPlayerVictim, bool isPlayerKiller)
    {
        if (isGameOver) return;

        aliveCount--;
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.UpdateAliveCount(aliveCount);
            GameHUD.Instance.AddKillFeedEntry(killerName, victimName);
        }

        MatchData data = MatchData.Load();

        if (isPlayerKiller)
        {
            data.kills++;

            // ── Bounty System Integration ────────────────────────────────────
            if (BountySystem.Instance != null)
            {
                ulong killerClientId = 0; // Local player
                BountySystem.Instance.RegisterKill(killerClientId, killerName);
            }

            // ── Talent System: Award talent point per kill ───────────────────
            if (TalentTreeSystem.Instance != null)
            {
                TalentTreeSystem.Instance.AwardPoints(1);
            }

            // ── Pet on-kill bonus ────────────────────────────────────────────
            PetCompanion[] pets = FindObjectsOfType<PetCompanion>();
            foreach (PetCompanion pet in pets)
            {
                float hpBonus = pet.GetHPOnKill();
                if (hpBonus > 0)
                {
                    // Apply through player controller
                    PlayerController[] players = FindObjectsOfType<PlayerController>();
                    foreach (PlayerController p in players)
                    {
                        if (p.IsOwner && p.netHP != null)
                        {
                            // Request heal
                            break;
                        }
                    }
                }
            }
        }

        if (isPlayerVictim)
        {
            data.placement = aliveCount + 1;
            data.matchDuration = Time.timeSinceLevelLoad;
            StartCoroutine(EndMatch());
        }
        else if (aliveCount <= 1) // Only player left alive!
        {
            data.placement = 1;
            data.matchDuration = Time.timeSinceLevelLoad;
            StartCoroutine(EndMatch());
        }

        // ── Award talent points on zone phase change ─────────────────────
        if (aliveCount == 15 || aliveCount == 10 || aliveCount == 5)
        {
            if (TalentTreeSystem.Instance != null)
            {
                TalentTreeSystem.Instance.AwardPoints(2);
            }
        }
    }

    public void AddPlayerDamageDealt(float dmg)
    {
        MatchData data = MatchData.Load();
        data.damageDealt += dmg;
    }

    private IEnumerator EndMatch()
    {
        isGameOver = true;
        yield return new WaitForSeconds(2.5f);
        GameManager.Instance.ChangeState(GameState.GameOver);
    }
}


