using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Mobile polish layer for production combat: aim assist, server-side hit request,
/// recoil smoothing, touch sensitivity scaling, and basic anti-cheat telemetry.
/// Add to Player prefab alongside PlayerController.
/// </summary>
public class MobileCombatPolish : NetworkBehaviour
{
    [Header("Mobile Aim")]
    public float aimAssistRadius = 5.5f;
    public float aimAssistStrength = 0.38f;
    public float maxAssistDistance = 85f;
    public LayerMask targetMask = ~0;

    [Header("Server Combat")]
    public float maxFireDistance = 120f;
    public float maxAllowedMoveSpeed = 18f;
    public float lowLatencyInterpolation = 0.12f;

    private Vector3 lastServerCheckedPosition;
    private float antiCheatTimer;
    private float recoil;

    private void Start()
    {
        lastServerCheckedPosition = transform.position;
        Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
        QualitySettings.vSyncCount = 0;
    }

    private void Update()
    {
        if (!IsOwner) return;
        ApplyTouchAimAssist();
        SmoothRecoil();
        antiCheatTimer += Time.deltaTime;
        if (antiCheatTimer >= 1f)
        {
            antiCheatTimer = 0;
            ReportMovementForAntiCheatServerRpc(transform.position);
        }
    }

    private void ApplyTouchAimAssist()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Collider[] hits = Physics.OverlapSphere(cam.transform.position + cam.transform.forward * 18f, aimAssistRadius, targetMask);
        Transform best = null;
        float bestScore = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.transform == transform || h.GetComponent<PlayerController>() == null) continue;
            Vector3 to = h.bounds.center - cam.transform.position;
            if (to.magnitude > maxAssistDistance) continue;
            float angle = Vector3.Angle(cam.transform.forward, to.normalized);
            if (angle < bestScore) { bestScore = angle; best = h.transform; }
        }
        if (best != null && bestScore < 12f)
        {
            Vector3 dir = (best.position + Vector3.up * 1.2f - cam.transform.position).normalized;
            Quaternion assisted = Quaternion.LookRotation(Vector3.Slerp(cam.transform.forward, dir, aimAssistStrength * Time.deltaTime * 8f), Vector3.up);
            cam.transform.rotation = assisted;
        }
    }

    private void SmoothRecoil()
    {
        recoil = Mathf.Lerp(recoil, 0, Time.deltaTime * 8f);
    }

    public void FireMobileWeapon(string weaponId, Vector3 origin, Vector3 direction, float clientTime)
    {
        if (!IsOwner) return;
        recoil += 1f;
        FireServerRpc(weaponId, origin, direction.normalized, clientTime);
    }

    [ServerRpc]
    private void FireServerRpc(string weaponId, Vector3 origin, Vector3 direction, float clientTime)
    {
        if (Vector3.Distance(origin, transform.position + Vector3.up * 1.2f) > 4f)
        {
            ProductionBattleRoyaleLoop.Instance?.ReportAntiCheat(OwnerClientId, "fire_origin_mismatch", Vector3.Distance(origin, transform.position));
            return;
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxFireDistance, targetMask, QueryTriggerInteraction.Ignore))
        {
            PlayerController victim = hit.collider.GetComponentInParent<PlayerController>();
            if (victim != null && victim.OwnerClientId != OwnerClientId)
            {
                float damage = ResolveWeaponDamage(weaponId, hit.collider.name.ToLower().Contains("head") ? 1.75f : 1f);
                victim.RequestTakeDamageServerRpc(damage, "Player_" + OwnerClientId, hit.point);
                if (ProductionBattleRoyaleLoop.Instance != null)
                    ProductionBattleRoyaleLoop.Instance.RegisterKill(OwnerClientId, victim.OwnerClientId, damage);
            }
        }
    }

    private float ResolveWeaponDamage(string weaponId, float multiplier)
    {
        if (weaponId.Contains("Sniper")) return 82f * multiplier;
        if (weaponId.Contains("Shotgun")) return 54f * multiplier;
        if (weaponId.Contains("SMG")) return 22f * multiplier;
        if (weaponId.Contains("Pistol")) return 28f * multiplier;
        return 34f * multiplier;
    }

    [ServerRpc]
    private void ReportMovementForAntiCheatServerRpc(Vector3 clientPosition)
    {
        float distance = Vector3.Distance(clientPosition, lastServerCheckedPosition);
        if (distance > maxAllowedMoveSpeed * 1.35f)
            ProductionBattleRoyaleLoop.Instance?.ReportAntiCheat(OwnerClientId, "speed_or_teleport", distance);
        lastServerCheckedPosition = clientPosition;
    }
}


