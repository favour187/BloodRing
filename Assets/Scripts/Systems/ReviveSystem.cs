using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Battle-royale-grade Down-But-Not-Out (DBNO) and Revive system.
/// In squad modes, downed players can crawl and be revived by teammates.
/// Includes bleed-out timer, revive progress bar, and instant-finish mechanic.
/// </summary>
public enum PlayerLifeState
{
    Alive,
    Downed,    // Can be revived
    Dead,      // Cannot be revived
    Spectating
}

public class ReviveSystem : MonoBehaviour
{
    public static ReviveSystem Instance;

    private void Awake() { Instance = this; }

    // DBNO settings
    public float bleedOutTime = 60f;       // 60 seconds to bleed out
    public float reviveTime = 5f;          // 5 seconds to revive
    public float bleedDamagePerSecond = 2f;// HP drain while downed
    public float crawlSpeed = 1.5f;
    public int maxKnocks = 3;              // Max times you can be downed before instant death

    /// <summary>
    /// Called when player takes lethal damage. Returns true if player should enter DBNO state,
    /// false if they should die immediately.
    /// </summary>
    public bool ShouldEnterDBNO(string gameMode, int knockCount)
    {
        // Only in squad/duo modes
        if (gameMode == "CLASSIC" || gameMode == "CLASH_SQUAD")
        {
            return knockCount < maxKnocks;
        }
        return false; // Solo modes = instant death
    }
}

/// <summary>
/// Attached to players to handle their down/revive state.
/// Communicates with PlayerController for movement restriction.
/// </summary>
public class PlayerReviveHandler : NetworkBehaviour
{
    public NetworkVariable<int> lifeState = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> bleedTimer = new NetworkVariable<float>(60f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> reviveProgress = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> knockCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool isBeingRevived = false;
    private ulong reviverClientId = 0;
    private GameObject downedIndicator;
    private GameObject reviveProgressBar;
    private Image reviveProgressFill;
    private Text reviveText;

    public PlayerLifeState GetLifeState() { return (PlayerLifeState)lifeState.Value; }
    public bool IsDowned() { return lifeState.Value == (int)PlayerLifeState.Downed; }
    public bool IsAlive() { return lifeState.Value == (int)PlayerLifeState.Alive; }
    public bool IsDead() { return lifeState.Value == (int)PlayerLifeState.Dead; }

    public override void OnNetworkSpawn()
    {
        lifeState.OnValueChanged += OnLifeStateChanged;
    }

    private void OnLifeStateChanged(int prev, int current)
    {
        if (current == (int)PlayerLifeState.Downed)
        {
            ShowDownedVisuals();
        }
        else if (current == (int)PlayerLifeState.Alive)
        {
            HideDownedVisuals();
        }
        else if (current == (int)PlayerLifeState.Dead)
        {
            HideDownedVisuals();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnterDownedStateServerRpc()
    {
        knockCount.Value++;
        lifeState.Value = (int)PlayerLifeState.Downed;
        bleedTimer.Value = ReviveSystem.Instance != null ? ReviveSystem.Instance.bleedOutTime : 60f;
        reviveProgress.Value = 0f;
        isBeingRevived = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartReviveServerRpc(ulong reviverId)
    {
        if (lifeState.Value != (int)PlayerLifeState.Downed) return;
        isBeingRevived = true;
        reviverClientId = reviverId;
        reviveProgress.Value = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopReviveServerRpc()
    {
        isBeingRevived = false;
        reviveProgress.Value = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void FinishKillServerRpc(string killerName)
    {
        if (lifeState.Value != (int)PlayerLifeState.Downed) return;
        lifeState.Value = (int)PlayerLifeState.Dead;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (lifeState.Value == (int)PlayerLifeState.Downed)
        {
            // Bleed out timer
            bleedTimer.Value -= Time.deltaTime;
            if (bleedTimer.Value <= 0)
            {
                lifeState.Value = (int)PlayerLifeState.Dead;
                return;
            }

            // Revive progress
            if (isBeingRevived)
            {
                float reviveTime = ReviveSystem.Instance != null ? ReviveSystem.Instance.reviveTime : 5f;
                reviveProgress.Value += Time.deltaTime / reviveTime;
                if (reviveProgress.Value >= 1f)
                {
                    // Revived!
                    lifeState.Value = (int)PlayerLifeState.Alive;
                    reviveProgress.Value = 0f;
                    isBeingRevived = false;
                    OnRevivedClientRpc();
                }
            }
        }
    }

    [ClientRpc]
    private void OnRevivedClientRpc()
    {
        // Revive particles
        GameObject reviveFx = new GameObject("ReviveFX");
        reviveFx.transform.position = transform.position + new Vector3(0, 1f, 0);
        ParticleSystem ps = reviveFx.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = reviveFx.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startColor = Color.green;
        main.startSize = 0.3f;
        main.startSpeed = 5f;
        main.startLifetime = 1f;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });
        ps.Play();
        Destroy(reviveFx, 2f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPowerPickupSound();
    }

    private void ShowDownedVisuals()
    {
        // Red pulsing indicator above player
        if (downedIndicator == null)
        {
            downedIndicator = new GameObject("DownedIndicator");
            downedIndicator.transform.SetParent(transform);
            downedIndicator.transform.localPosition = new Vector3(0, 3f, 0);

            TextMesh tm = downedIndicator.AddComponent<TextMesh>();
            tm.text = "▼ DOWNED ▼";
            tm.fontSize = 40;
            tm.characterSize = 0.06f;
            tm.color = Color.red;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            // Blinking light
            Light light = downedIndicator.AddComponent<Light>();
            light.color = Color.red;
            light.intensity = 3f;
            light.range = 8f;
        }
        downedIndicator.SetActive(true);
    }

    private void HideDownedVisuals()
    {
        if (downedIndicator != null) downedIndicator.SetActive(false);
    }

    private void LateUpdate()
    {
        // Billboard downed indicator
        if (downedIndicator != null && downedIndicator.activeSelf && Camera.main != null)
        {
            downedIndicator.transform.LookAt(Camera.main.transform);
            downedIndicator.transform.Rotate(0, 180, 0);

            // Pulse effect
            float pulse = Mathf.Abs(Mathf.Sin(Time.time * 3f));
            TextMesh tm = downedIndicator.GetComponent<TextMesh>();
            if (tm != null) tm.color = new Color(1f, pulse * 0.3f, 0f);

            Light l = downedIndicator.GetComponent<Light>();
            if (l != null) l.intensity = 2f + pulse * 3f;
        }
    }
}


