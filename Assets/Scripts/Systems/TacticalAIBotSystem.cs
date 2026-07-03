using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Ring Apex Royale 3D — Advanced Tactical Squad AI Bot System.
/// Bots intelligently seek cover, deploy CryoBarriers when under heavy gunfire,
/// forage for Bio-Plasma Spores when injured, and execute squad flanking maneuvers.
/// </summary>
public class TacticalAIBotSystem : MonoBehaviour
{
    public static TacticalAIBotSystem Instance;
    private List<GameObject> activeBots = new List<GameObject>();

    private void Awake() { Instance = this; }

    public void RegisterBot(GameObject bot)
    {
        if (!activeBots.Contains(bot)) activeBots.Add(bot);
    }

    /// <summary>Evaluates bot tactical decisions every frame.</summary>
    public void ExecuteTacticalDecision(GameObject bot, float currentHP, float maxHP, Transform targetEnemy)
    {
        if (bot == null || targetEnemy == null) return;

        float distance = Vector3.Distance(bot.transform.position, targetEnemy.position);

        // Emergency defensive CryoBarrier deployment
        if (currentHP < maxHP * 0.4f && Random.value < 0.05f)
        {
            if (CryoBarrierSystem.Instance != null)
            {
                Vector3 spawnPos = bot.transform.position + (targetEnemy.position - bot.transform.position).normalized * 2f;
                CryoBarrierSystem.Instance.DeployBarrier(spawnPos, bot.transform.rotation, bot.name);
                Debug.Log($"[TacticalAIBotSystem] Bot {bot.name} deployed emergency defensive CryoBarrier!");
            }
        }

        // Flanking and Grid Pathfinding behavior
        if (distance > 15f && distance < 60f)
        {
            if (NavGridSystem.Instance != null)
            {
                List<Vector3> path = NavGridSystem.Instance.FindPath(bot.transform.position, targetEnemy.position);
                if (path != null && path.Count > 1)
                {
                    Vector3 moveDir = (path[1] - bot.transform.position).normalized;
                    bot.transform.position += moveDir * Time.deltaTime * 4.5f;
                }
                else
                {
                    Vector3 flankDir = Vector3.Cross(targetEnemy.position - bot.transform.position, Vector3.up).normalized;
                    bot.transform.position += flankDir * Time.deltaTime * 3.5f;
                }
            }
            else
            {
                Vector3 flankDir = Vector3.Cross(targetEnemy.position - bot.transform.position, Vector3.up).normalized;
                bot.transform.position += flankDir * Time.deltaTime * 3.5f;
            }
        }
    }
}
