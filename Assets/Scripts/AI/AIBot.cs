using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public enum AIState { Patrol, Chase, Attack, Looting }

public class AIBot : NetworkBehaviour
{
    private NavMeshAgent agent; private AIState currentState = AIState.Looting;
    private Animator animator; private Animator weaponAnimator;

    public NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> netHP = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> netIsDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public string botName; private WeaponData currentWeapon; private float shotCooldown = 0f;
    private Transform playerTarget; private PlayerController playerController; private PickupItem targetLoot = null; private float stateTimer = 0f;

    private GameObject humanoidModel; private GameObject equippedGunModel; private Transform rightArmTransform;
    private bool ragdollTriggered = false; private Vector3 remotePosTarget; private Quaternion remoteRotTarget;

    public void InitializeBot(string name, Transform player)
    {
        botName = name; playerTarget = player; if (player != null) playerController = player.GetComponent<PlayerController>();

        GameObject realModelPrefab = Resources.Load<GameObject>("Models/SoldierRigged") ?? Resources.Load<GameObject>("Models/Soldier");
        if (realModelPrefab != null) { humanoidModel = Object.Instantiate(realModelPrefab, transform); humanoidModel.name = "RealSoldierMesh"; humanoidModel.transform.localPosition = Vector3.zero; animator = humanoidModel.GetComponent<Animator>(); if (animator == null) animator = humanoidModel.AddComponent<Animator>(); if (animator.runtimeAnimatorController == null) { var _ac = Resources.Load<RuntimeAnimatorController>("Animation/AIAnimator"); if (_ac != null) animator.runtimeAnimatorController = _ac; } rightArmTransform = humanoidModel.transform.Find("RightArm") ?? humanoidModel.transform; }
        else { humanoidModel = ProceduralArt.CreateHumanoidMesh("Tank"); humanoidModel.transform.SetParent(transform); humanoidModel.transform.localPosition = Vector3.zero; rightArmTransform = humanoidModel.transform.Find("RightArm") ?? humanoidModel.transform; }

        MinimapBlip blip = gameObject.AddComponent<MinimapBlip>(); blip.blipColor = Color.red;
        CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>(); col.height = 2f; col.radius = 0.5f; col.center = new Vector3(0, 1f, 0);

        if (IsServer) { agent = gameObject.AddComponent<NavMeshAgent>(); agent.speed = 4.5f; agent.stoppingDistance = 2f; agent.angularSpeed = 200f; netPosition.Value = transform.position; netRotation.Value = transform.rotation; FindNearestWeaponLoot(); }
    }

    private void EquipGunModel(string wName)
    {
        if (equippedGunModel != null) Destroy(equippedGunModel);
        GameObject realGunPrefab = Resources.Load<GameObject>("Models/" + wName) ?? Resources.Load<GameObject>("Models/AK47");
        if (realGunPrefab != null) { equippedGunModel = Object.Instantiate(realGunPrefab, rightArmTransform); equippedGunModel.name = "RealGunMesh"; equippedGunModel.transform.localPosition = new Vector3(0, -0.1f, 0.3f); equippedGunModel.transform.localRotation = Quaternion.identity; weaponAnimator = equippedGunModel.GetComponent<Animator>(); }
        else { equippedGunModel = ProceduralArt.CreateGunMesh(wName); equippedGunModel.transform.SetParent(rightArmTransform); equippedGunModel.transform.localPosition = new Vector3(0, -0.1f, 0.3f); equippedGunModel.transform.localRotation = Quaternion.identity; }
    }

    private void Update()
    {
        if (netIsDead.Value) { if (!ragdollTriggered) TriggerRagdollDeath(); return; }
        if (!IsServer)
        {
            remotePosTarget = netPosition.Value; remoteRotTarget = netRotation.Value;
            transform.position = Vector3.Lerp(transform.position, remotePosTarget, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Slerp(transform.rotation, remoteRotTarget, Time.deltaTime * 15f);
            if (animator != null) { float speed = Vector3.Distance(transform.position, remotePosTarget) / Time.deltaTime; animator.SetFloat("Speed", Mathf.Clamp01(speed / 4.5f)); }
            return;
        }

        if (shotCooldown > 0) shotCooldown -= Time.deltaTime; stateTimer += Time.deltaTime;
        if (ZoneController.Instance != null && ZoneController.Instance.IsOutsideZone(transform.position)) { if (stateTimer >= 1f) { RequestTakeDamageServerRpc(1f, "The Zone", transform.position); stateTimer = 0f; } }

        switch (currentState) { case AIState.Looting: HandleLootingState(); break; case AIState.Patrol: HandlePatrolState(); break; case AIState.Chase: HandleChaseState(); break; case AIState.Attack: HandleAttackState(); break; }
        CheckPlayerDistance();
        if (animator != null && agent != null) animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);
        netPosition.Value = transform.position; netRotation.Value = transform.rotation;
    }

    private void FindNearestWeaponLoot() { PickupItem[] pickups = FindObjectsOfType<PickupItem>(); float minDist = Mathf.Infinity; foreach (PickupItem p in pickups) { if (p.pickupType == PickupType.Weapon) { float d = Vector3.Distance(transform.position, p.transform.position); if (d < minDist) { minDist = d; targetLoot = p; } } } if (targetLoot != null) { currentState = AIState.Looting; agent.SetDestination(targetLoot.transform.position); agent.stoppingDistance = 0.5f; } else { currentWeapon = WeaponData.GetDefaultWeapon("Rifle"); EquipGunModel("Rifle"); currentState = AIState.Patrol; agent.stoppingDistance = 2f; } }
    private void HandleLootingState() { if (targetLoot == null) { FindNearestWeaponLoot(); return; } if (Vector3.Distance(transform.position, targetLoot.transform.position) < 2f) { currentWeapon = Resources.Load<WeaponData>("Weapons/" + targetLoot.weaponName); if (currentWeapon == null) currentWeapon = WeaponData.GetDefaultWeapon(targetLoot.weaponName); EquipGunModel(currentWeapon.weaponName); Destroy(targetLoot.gameObject); targetLoot = null; currentState = AIState.Patrol; agent.stoppingDistance = 2f; } }
    private void HandlePatrolState() { if (!agent.hasPath || agent.remainingDistance < 2f) { Vector3 randomDest = transform.position + new Vector3(Random.Range(-30f, 30f), 0, Random.Range(-30f, 30f)); NavMeshHit hit; if (NavMesh.SamplePosition(randomDest, out hit, 10f, NavMesh.AllAreas)) { agent.SetDestination(hit.position); } } }
    private void HandleChaseState() { if (playerTarget != null) { agent.SetDestination(playerTarget.position); } }
    private void HandleAttackState() { if (playerTarget != null) { agent.SetDestination(transform.position); Vector3 lookDir = playerTarget.position - transform.position; lookDir.y = 0; transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f); if (shotCooldown <= 0 && currentWeapon != null) { ShootAtPlayer(); } } }

    private void ShootAtPlayer()
    {
        shotCooldown = currentWeapon.fireRate * 2f;
        if (animator != null) animator.SetTrigger("Fire"); if (weaponAnimator != null) weaponAnimator.SetTrigger("Fire");
        if (AudioManager.Instance != null && Vector3.Distance(transform.position, playerTarget.position) < 30f) { AudioManager.Instance.PlayWeaponSound(currentWeapon.weaponName); }
        Vector3 shootDir = (playerTarget.position + new Vector3(0, 1f, 0)) - (transform.position + new Vector3(0, 1.5f, 0)); shootDir += new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)); shootDir.Normalize();
        RaycastHit hit; if (Physics.Raycast(transform.position + new Vector3(0, 1.5f, 0), shootDir, out hit, 100f)) { PlayerController p = hit.collider.GetComponentInParent<PlayerController>(); if (p != null) { p.RequestTakeDamageServerRpc(currentWeapon.damage * 0.5f, botName, transform.position); } }
    }

    private void CheckPlayerDistance() { if (currentState == AIState.Looting || playerTarget == null) return; if (playerController != null && playerController.IsInvisible) { if (currentState != AIState.Patrol) currentState = AIState.Patrol; return; } float dist = Vector3.Distance(transform.position, playerTarget.position); if (dist <= 20f) { currentState = AIState.Attack; } else if (dist <= 40f) { currentState = AIState.Chase; } else if (currentState != AIState.Patrol) { currentState = AIState.Patrol; } }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTakeDamageServerRpc(float dmg, string killerName, Vector3 shooterPos)
    {
        if (netIsDead.Value) return;
        float d = Vector3.Distance(transform.position, shooterPos);
        if (d > 220f) { Debug.LogWarning("Server Anti-Cheat: Shooter exceeded max ballistic range (" + d + "m). Discarding hit."); return; }

        netHP.Value -= dmg;
        if (netHP.Value <= 0) { netIsDead.Value = true; DieClientRpc(killerName); } else if (currentState == AIState.Patrol && playerTarget != null) { currentState = AIState.Chase; }
    }

    [ClientRpc]
    private void DieClientRpc(string killerName)
    {
        if (agent != null) agent.enabled = false; GetComponent<Collider>().enabled = false;
        bool isPlayerKiller = killerName == PlayerPrefs.GetString("PlayerNickname", "Player");
        if (KillSystem.Instance != null) KillSystem.Instance.OnEntityKilled(botName, killerName, false, isPlayerKiller);
        if (animator != null) animator.SetBool("IsDead", true); TriggerRagdollDeath();
    }

    private void TriggerRagdollDeath()
    {
        ragdollTriggered = true; if (agent != null) agent.enabled = false;
        if (humanoidModel != null) { foreach (Transform t in humanoidModel.transform) { Rigidbody rb = t.gameObject.GetComponent<Rigidbody>(); if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>(); rb.AddTorque(Random.insideUnitSphere * 50f); rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse); } }
        StartCoroutine(FadeAndDestroyRagdoll());
    }

    private IEnumerator FadeAndDestroyRagdoll()
    {
        yield return new WaitForSeconds(3.5f); float startTime = Time.time; float duration = 1.5f;
        while (Time.time - startTime < duration) { float t = (Time.time - startTime) / duration; if (humanoidModel != null) { foreach (Transform child in humanoidModel.transform) { Renderer r = child.GetComponent<Renderer>(); if (r != null) { Color c = r.material.color; c.a = 1f - t; r.material.color = c; } } } yield return null; }
        if (IsServer) Destroy(gameObject);
    }
}


