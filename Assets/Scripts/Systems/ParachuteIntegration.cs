using UnityEngine;
using Unity.Netcode;

/// <summary>
/// M2 Integration helper: wires ParachuteDrop into ProductionBattleRoyaleLoop
/// and PlayerController for seamless aircraft → landing flow.
/// </summary>
public static class ParachuteIntegration
{
    public static void StartDropForPlayer(Transform player)
    {
        if (ParachuteDrop.Instance == null)
        {
            GameObject go = new GameObject("ParachuteDrop");
            go.AddComponent<ParachuteDrop>();
        }
        
        ParachuteDrop.Instance.InitializePlaneDropSequence(player);
    }

    [RuntimeInitializeOnLoadMethod]
    private static void HookIntoLoop()
    {
        // This runs at startup — ProductionBattleRoyaleLoop will call this when needed
        Debug.Log("[M2] ParachuteIntegration ready for ProductionBattleRoyaleLoop");
    }
}
