using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — Squad Clash Arena Mode Controller (4v4 Round-Based BR Mode).
/// 4v4 round-based tactical combat in restricted island zones, with pre-round store buy phases
/// and best-of-7 round victory conditions. 100% original proprietary mode.
/// </summary>
public class ClashSquadController : MonoBehaviour
{
    public static ClashSquadController Instance;

    public int currentRound = 1;
    public int teamA_Score = 0;
    public int teamB_Score = 0;
    public int winningScore = 4; // Best of 7
    public bool isBuyPhase = true;
    public float buyPhaseTimer = 15f;

    private void Awake() { Instance = this; }

    public void StartClashSquadMatch()
    {
        currentRound = 1;
        teamA_Score = 0;
        teamB_Score = 0;
        StartCoroutine(RoundLoopCoroutine());
    }

    private IEnumerator RoundLoopCoroutine()
    {
        while (teamA_Score < winningScore && teamB_Score < winningScore)
        {
            Debug.Log($"[ClashSquadController] Round {currentRound} Start! Buy Phase Active.");
            isBuyPhase = true;
            buyPhaseTimer = 15f;

            while (buyPhaseTimer > 0)
            {
                buyPhaseTimer -= Time.deltaTime;
                yield return null;
            }

            Debug.Log("[ClashSquadController] Buy Phase Ended! Combat Phase Initiated.");
            isBuyPhase = false;

            yield return new WaitForSeconds(5f);

            if (Random.value > 0.5f) teamA_Score++; else teamB_Score++;
            Debug.Log($"[ClashSquadController] Round End! Score -> Team A: {teamA_Score} | Team B: {teamB_Score}");
            currentRound++;
            yield return new WaitForSeconds(3f);
        }

        string winner = teamA_Score >= winningScore ? "Team Alpha (Blood Ring Champions)" : "Team Bravo (Apex Challengers)";
        Debug.Log($"[ClashSquadController] Match Concluded! Winner: {winner}");
    }
}
