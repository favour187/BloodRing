using UnityEngine;
using Unity.Netcode;

/// <summary>
/// M2 small connector: allows ProductionBattleRoyaleLoop to trigger
/// the parachute drop sequence for the local player.
/// </summary>
public class BRDropTrigger : NetworkBehaviour
{
    public static BRDropTrigger Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerDropForLocalPlayer()
    {
        if (PlayerController.LocalPlayer != null)
        {
            ParachuteIntegration.StartDropForPlayer(PlayerController.LocalPlayer.transform);
        }
    }

    [ClientRpc]
    public void TriggerDropClientRpc()
    {
        if (IsOwner) TriggerDropForLocalPlayer();
    }
}
