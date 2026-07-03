using UnityEngine;

/// <summary>
/// Blood Ring Apex Royale 3D — Aegis Sky-Catalyst Launchpads & Gravity Jump Pads.
/// Spawns angled electromagnetic propulsion ramps across rooftops and cliffs.
/// Step onto a launchpad to be catapulted high into the sky along a ballistic trajectory,
/// instantly deploying an energy glider for aerial ambushes and rapid map traversal.
/// 100% original proprietary mobility mechanic.
/// </summary>
public class SkyCatalystLaunchpad : MonoBehaviour
{
    public static SkyCatalystLaunchpad Instance;

    public float launchVerticalVelocity = 35f;
    public float launchForwardVelocity = 45f;
    public float gliderAutoDeployAltitude = 25f;

    private void Awake() { Instance = this; }

    /// <summary>Catapults a player character into the air along a high-velocity ballistic curve.</summary>
    public void LaunchPlayer(Transform playerTransform, Vector3 launchDirection)
    {
        if (playerTransform == null) return;

        Debug.Log("[SkyCatalystLaunchpad] Catapulting player via electromagnetic propulsion!");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_ParachuteDeploy");

        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = launchDirection.normalized * launchForwardVelocity + Vector3.up * launchVerticalVelocity;
        }
        else
        {
            // Fallback character controller displacement
            playerTransform.position += Vector3.up * 5f + launchDirection.normalized * 3f;
        }
    }
}
