using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    private CharacterController controller; private Transform cameraPivot; private CinemachineVirtualCamera vcam;
    private Animator animator; private Animator weaponAnimator;

    public NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> netHP = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> netArmor = new NetworkVariable<float>(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> netIsDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> netIsInvisible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float walkSpeed = 5f; private float runSpeed = 9f; private float crouchSpeed = 2.5f; private float proneSpeed = 1.5f; private float swimSpeed = 3.5f; private float jumpForce = 6f; private float gravity = -15f; private float velocityY = 0f;
    private bool isCrouching = false; private bool isProne = false; private bool isSwimming = false; private bool isZiplining = false; private bool canDoubleJump = false; private bool hasDoubleJumped = false;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideSpeed = 12f;
    private PlayerReviveHandler reviveHandler;

    private float maxHP = 100f; private float maxArmor = 100f; public string playerName = "Player";

    private WeaponData[] weaponSlots = new WeaponData[2]; private int[] currentMagAmmo = new int[2]; private int activeWeaponSlot = 0; private bool isReloading = false; private float shotCooldown = 0f;
    private Dictionary<AmmoType, int> ammoInventory = new Dictionary<AmmoType, int>(); private int consumableCount = 1;
    private Dictionary<PowerType, float> activePowers = new Dictionary<PowerType, float>(); private PickupItem nearbyPickup = null; private PickupItem nearbyPowerUp = null;
    private Vehicle nearbyVehicle = null; private Vehicle currentVehicle = null; private Zipline nearbyZipline = null; private LedgeClimb nearbyLedge = null;

    private GameObject humanoidModel; private GameObject equippedGunModel; private Transform rightArmTransform;
    private ParticleSystem muzzleFlash; private GameObject yellowTrail; private GameObject cyanFootDust; private GameObject purpleGlow; private Light greenPulseLight; private GameObject redVignetteGo; private GameObject zoneVignetteGo;

    private float zoneDamageTimer = 0f; private float walkBobTimer = 0f; private Vector3 origGunPos; private Quaternion origGunRot; private Vector3 recoilOffset = Vector3.zero; private Quaternion recoilRot = Quaternion.identity;
    private bool ragdollTriggered = false; private Vector3 lastValidPos; private float antiCheatTimer = 0f;
    private Transform spectatorTarget = null; private List<Transform> spectatePool = new List<Transform>(); private int spectateIndex = 0;
    private Vector3 remotePosTarget; private Quaternion remoteRotTarget;

    public static PlayerController Instance { get; private set; }

    public bool IsInvisible { get { return netIsInvisible.Value; } }

    public float GetCurrentHP() { return netHP.Value; }
    public float GetMaxHP() { return maxHP; }
    public void HealHP(float amount) { if (IsServer) netHP.Value = Mathf.Min(maxHP, netHP.Value + amount); else HealHPServerRpc(amount); }

    [ServerRpc] private void HealHPServerRpc(float amount) { netHP.Value = Mathf.Min(maxHP, netHP.Value + amount); }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) Instance = this;
        Debug.Log("OnNetworkSpawn for Polished BloodRing Apex Player ID: " + OwnerClientId);
        playerName = NetworkController.Instance != null ? NetworkController.Instance.GetPlayerNickname(OwnerClientId) : ("Player_" + OwnerClientId);
        string charChoice = NetworkController.Instance != null ? NetworkController.Instance.GetPlayerCharacter(OwnerClientId) : "DJNeon";

        reviveHandler = gameObject.GetComponent<PlayerReviveHandler>();
        if (reviveHandler == null) reviveHandler = gameObject.AddComponent<PlayerReviveHandler>();

        CharacterData cData = CharacterData.GetCharacter(charChoice);

        if (charChoice == "DJNeon" || charChoice == "Bolt") { walkSpeed = 5.5f; runSpeed = 9.8f; } else if (charChoice == "Ronin") { maxArmor = 120f; if (IsServer) netArmor.Value = 100f; } else if (charChoice == "Mirage") { if (IsServer) netIsInvisible.Value = true; }

        controller = gameObject.GetComponent<CharacterController>(); if (controller == null) controller = gameObject.AddComponent<CharacterController>(); controller.height = 2f; controller.radius = 0.5f; controller.center = new Vector3(0, 1f, 0); controller.enabled = IsOwner;

        // Attempt to load Rigged Character Model with Animator
        GameObject realModelPrefab = Resources.Load<GameObject>("Models/SoldierRigged") ?? Resources.Load<GameObject>("Models/Soldier");
        if (realModelPrefab != null)
        {
            humanoidModel = Object.Instantiate(realModelPrefab, transform); humanoidModel.name = "RealSoldierMesh"; humanoidModel.transform.localPosition = Vector3.zero;
            animator = humanoidModel.GetComponent<Animator>(); if (animator == null) animator = humanoidModel.AddComponent<Animator>();
            if (animator.runtimeAnimatorController == null) { var _pc = Resources.Load<RuntimeAnimatorController>("Animation/PlayerAnimator"); if (_pc != null) animator.runtimeAnimatorController = _pc; }
            rightArmTransform = humanoidModel.transform.Find("RightArm") ?? humanoidModel.transform;
        }
        else
        {
            humanoidModel = ProceduralArt.CreateHumanoidMesh(charChoice == "DJNeon" ? "Striker" : (charChoice == "Pulse" ? "Tank" : "Stealth")); humanoidModel.transform.SetParent(transform); humanoidModel.transform.localPosition = Vector3.zero; rightArmTransform = humanoidModel.transform.Find("RightArm") ?? humanoidModel.transform; animator = humanoidModel.GetComponent<Animator>(); if (animator == null) animator = humanoidModel.AddComponent<Animator>(); if (animator.runtimeAnimatorController == null) { var _pc = Resources.Load<RuntimeAnimatorController>("Animation/PlayerAnimator"); if (_pc != null) animator.runtimeAnimatorController = _pc; }
        }

        MinimapBlip blip = gameObject.AddComponent<MinimapBlip>(); blip.blipColor = Color.white;

        GameObject mfGo = new GameObject("MuzzleFlash"); mfGo.transform.SetParent(rightArmTransform); mfGo.transform.localPosition = new Vector3(0, 0, 0.8f); muzzleFlash = mfGo.AddComponent<ParticleSystem>(); var main = muzzleFlash.main; main.duration = 0.05f; main.loop = false; main.startSize = 0.5f; main.startSpeed = 5f; main.startColor = Color.yellow; var emission = muzzleFlash.emission; emission.rateOverTime = 0; emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

        SetupPowerUpVisuals();

        if (IsOwner)
        {
            cameraPivot = new GameObject("CameraPivot").transform; cameraPivot.SetParent(transform); cameraPivot.localPosition = new Vector3(0, 1.8f, 0);
            GameObject vcamGo = new GameObject("PlayerVCam"); vcam = vcamGo.AddComponent<CinemachineVirtualCamera>(); vcam.Follow = cameraPivot; vcam.LookAt = cameraPivot; Cinemachine3rdPersonFollow body = vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>(); body.CameraDistance = 4f; body.ShoulderOffset = new Vector3(0.8f, 0.5f, 0); body.Damping = new Vector3(0.1f, 0.2f, 0.1f);
            ammoInventory[AmmoType.PistolAmmo] = 36; ammoInventory[AmmoType.RifleAmmo] = 60; ammoInventory[AmmoType.ShotgunAmmo] = 18; ammoInventory[AmmoType.SMGAmmo] = 90; ammoInventory[AmmoType.SniperAmmo] = 15;
            WeaponData starterW = WeaponData.GetDefaultWeapon(PlayerPrefs.GetString("EquippedWeapon", "MP40")); weaponSlots[0] = starterW; currentMagAmmo[0] = starterW.maxAmmo; EquipGunModel(starterW.weaponName);
            netPosition.Value = transform.position; netRotation.Value = transform.rotation; lastValidPos = transform.position; remotePosTarget = transform.position; remoteRotTarget = transform.rotation; UpdateHUD();
        }
    }

    private void EquipGunModel(string wName)
    {
        if (equippedGunModel != null) Destroy(equippedGunModel);
        GameObject realGunPrefab = Resources.Load<GameObject>("Models/" + wName) ?? Resources.Load<GameObject>("Models/AK47");
        if (realGunPrefab != null) { equippedGunModel = Object.Instantiate(realGunPrefab, rightArmTransform); equippedGunModel.name = "RealGunMesh"; equippedGunModel.transform.localPosition = new Vector3(0, -0.1f, 0.3f); equippedGunModel.transform.localRotation = Quaternion.identity; weaponAnimator = equippedGunModel.GetComponent<Animator>(); if (weaponAnimator == null) weaponAnimator = equippedGunModel.AddComponent<Animator>(); if (weaponAnimator.runtimeAnimatorController == null) { var _wc = Resources.Load<RuntimeAnimatorController>("Animation/WeaponAnimator"); if (_wc != null) weaponAnimator.runtimeAnimatorController = _wc; } }
        else { equippedGunModel = ProceduralArt.CreateGunMesh(wName); equippedGunModel.transform.SetParent(rightArmTransform); equippedGunModel.transform.localPosition = new Vector3(0, -0.1f, 0.3f); equippedGunModel.transform.localRotation = Quaternion.identity; }
        origGunPos = equippedGunModel.transform.localPosition; origGunRot = equippedGunModel.transform.localRotation;
    }

    private void SetupPowerUpVisuals() { yellowTrail = new GameObject("YellowTrail"); yellowTrail.transform.SetParent(transform); yellowTrail.transform.localPosition = new Vector3(0, 0.5f, 0); ParticleSystem ts = yellowTrail.AddComponent<ParticleSystem>(); var tMain = ts.main; tMain.startColor = Color.yellow; tMain.loop = true; tMain.startSize = 0.3f; tMain.startLifetime = 0.5f; var tEm = ts.emission; tEm.rateOverTime = 20; yellowTrail.SetActive(false); cyanFootDust = new GameObject("CyanDust"); cyanFootDust.transform.SetParent(transform); cyanFootDust.transform.localPosition = new Vector3(0, 0.1f, 0); ParticleSystem cs = cyanFootDust.AddComponent<ParticleSystem>(); var cMain = cs.main; cMain.startColor = Color.cyan; cMain.loop = true; cMain.startSize = 0.2f; var cEm = cs.emission; cEm.rateOverTime = 15; cyanFootDust.SetActive(false); purpleGlow = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj"); purpleGlow.transform.SetParent(transform); purpleGlow.transform.localPosition = new Vector3(0, 1f, 0); purpleGlow.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f); Destroy(purpleGlow.GetComponent<Collider>()); Material magMat = new Material(ProceduralArt.GetSafeShader("Standard")); magMat.color = new Color(0.8f, 0.1f, 0.8f, 0.3f); magMat.SetFloat("_Mode", 3); magMat.SetInt("_ZWrite", 0); magMat.renderQueue = 3000; purpleGlow.GetComponent<Renderer>().material = magMat; purpleGlow.SetActive(false); GameObject lightGo = new GameObject("HealSurgeLight"); lightGo.transform.SetParent(transform); lightGo.transform.localPosition = new Vector3(0, 1.5f, 0); greenPulseLight = lightGo.AddComponent<Light>(); greenPulseLight.type = LightType.Point; greenPulseLight.color = Color.green; greenPulseLight.range = 5f; lightGo.SetActive(false); }

    private void Update()
    {
        if (reviveHandler != null && reviveHandler.IsDowned())
        {
            if (IsOwner)
            {
                controller.enabled = false;
                if (animator != null) animator.SetBool("IsDowned", true);
                // Basic crawling
                Vector2 moveInput = TouchControls.Instance != null ? TouchControls.Instance.MoveInput : Vector2.zero;
                Vector3 crawlDir = cameraPivot.forward * moveInput.y + cameraPivot.right * moveInput.x;
                crawlDir.y = 0; crawlDir.Normalize();
                transform.position += crawlDir * (ReviveSystem.Instance != null ? ReviveSystem.Instance.crawlSpeed : 1.5f) * Time.deltaTime;
                if (crawlDir.magnitude > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(crawlDir), Time.deltaTime * 5f);
            }
            netPosition.Value = transform.position; netRotation.Value = transform.rotation;
            return;
        }

        if (netIsDead.Value) { if (!ragdollTriggered) TriggerRagdollDeath(); HandleSpectatorMode(); return; }

        if (!IsOwner)
        {
            // Smooth Remote Player Interpolation
            remotePosTarget = netPosition.Value; remoteRotTarget = netRotation.Value;
            transform.position = Vector3.Lerp(transform.position, remotePosTarget, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Slerp(transform.rotation, remoteRotTarget, Time.deltaTime * 15f);
            if (animator != null) { float speed = Vector3.Distance(transform.position, remotePosTarget) / Time.deltaTime; animator.SetFloat("Speed", Mathf.Clamp01(speed / runSpeed)); }
            return;
        }

        // Client Prediction + Server Reconciliation Anti-Cheat Verification
        antiCheatTimer += Time.deltaTime; if (antiCheatTimer >= 1f) { antiCheatTimer = 0f; float d = Vector3.Distance(transform.position, lastValidPos); if (d > 35f && !isZiplining && currentVehicle == null) { RequestServerValidationServerRpc(transform.position, d); } lastValidPos = transform.position; }

        if (currentVehicle != null) { transform.position = currentVehicle.GetSeat().position; transform.rotation = currentVehicle.GetSeat().rotation; netPosition.Value = transform.position; netRotation.Value = transform.rotation; if (Input.GetKeyDown(KeyCode.F) || (TouchControls.Instance != null && TouchControls.Instance.PowerUpRequested)) { TouchControls.Instance.PowerUpRequested = false; currentVehicle.RequestExitServerRpc(); currentVehicle = null; controller.enabled = true; } UpdateHUD(); return; }
        if (isZiplining && nearbyZipline != null) { transform.position = Vector3.MoveTowards(transform.position, nearbyZipline.endPos, Time.deltaTime * 30f); netPosition.Value = transform.position; if (Vector3.Distance(transform.position, nearbyZipline.endPos) < 1f) { isZiplining = false; controller.enabled = true; } UpdateHUD(); return; }

        HandleMovement(); HandleCameraAndAim(); HandleShooting(); HandlePowers(); HandleZone(); HandleLootAndInteractions(); HandleNewSystems(); HandleProceduralAnimations(); UpdateHUD();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestServerValidationServerRpc(Vector3 clientPos, float velocity)
    {
        if (velocity > 35f) { Debug.LogWarning("Server Anti-Cheat: Player speed hack detected! Snapping back."); ForceSnapClientRpc(netPosition.Value); if (BackendAPI.Instance != null && BackendAPI.Instance.IsLoggedIn) { var _ = BackendAPI.Instance.LogAntiCheatViolationAsync("SpeedHack", "Velocity > 35u/s"); } }
        else { netPosition.Value = clientPos; }
    }

    [ClientRpc] private void ForceSnapClientRpc(Vector3 validPos) { if (IsOwner) { transform.position = validPos; lastValidPos = validPos; } }

    private void HandleMovement()
    {
        Vector2 moveInput = TouchControls.Instance != null ? TouchControls.Instance.MoveInput : Vector2.zero; bool isMoving = moveInput.magnitude > 0.1f;
        bool isGrass = true; RaycastHit hit; if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1f)) { if (hit.collider.gameObject.name.Contains("Building")) isGrass = false; } if (AudioManager.Instance != null) AudioManager.Instance.SetMoving(isMoving && controller.isGrounded, isGrass);

        if (Input.GetKeyDown(KeyCode.Z)) isProne = !isProne;
        isCrouching = TouchControls.Instance != null && TouchControls.Instance.IsCrouching;
        
        // Sliding Logic
        if (IsOwner && isMoving && !isCrouching && !isProne && controller.isGrounded && (moveInput.magnitude > 0.7f || (TouchControls.Instance != null && TouchControls.Instance.SprintRequested)) && isCrouching)
        {
            if (!isSliding) { isSliding = true; slideTimer = 1.2f; if (animator != null) animator.SetTrigger("Slide"); if (AudioManager.Instance != null) AudioManager.Instance.PlaySlideSound(); }
        }
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0 || !isMoving) isSliding = false;
            if (Time.frameCount % 10 == 0 && GameFeel.Instance != null) GameFeel.Instance.SpawnDustCloud(transform.position);
        }

        float targetSpeed = walkSpeed; if (isProne) targetSpeed = proneSpeed; else if (isCrouching) targetSpeed = crouchSpeed; else if (isSwimming) targetSpeed = swimSpeed; else if (isSliding) targetSpeed = slideSpeed; else if (moveInput.magnitude > 0.7f || (TouchControls.Instance != null && TouchControls.Instance.SprintRequested)) targetSpeed = runSpeed * ((TouchControls.Instance != null && TouchControls.Instance.SprintRequested) ? 1.35f : 1.0f); if (activePowers.ContainsKey(PowerType.SpeedBoost)) targetSpeed *= 1.5f;

        Vector3 moveDir = cameraPivot.forward * moveInput.y + cameraPivot.right * moveInput.x; moveDir.y = 0; moveDir.Normalize();
        if (controller.isGrounded || isSwimming) { velocityY = isSwimming ? 0f : -2f; hasDoubleJumped = false; if (TouchControls.Instance != null && TouchControls.Instance.JumpRequested) { TouchControls.Instance.JumpRequested = false; if (animator != null) animator.SetTrigger("Jump"); 
            // Improved Vaulting/Mantling
            RaycastHit vaultHit; if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out vaultHit, 1.2f)) { 
                if (Physics.Raycast(vaultHit.point + Vector3.up * 1.5f, Vector3.down, out RaycastHit topHit, 1.0f)) { 
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayVaultSound();
                    StartCoroutine(VaultRoutine(topHit.point)); 
                } 
            } else if (nearbyLedge != null) { transform.position += new Vector3(0, 2.5f, 0) + transform.forward * 1f; } else { velocityY = jumpForce; } } } else { if (canDoubleJump && !hasDoubleJumped && TouchControls.Instance != null && TouchControls.Instance.JumpRequested) { velocityY = jumpForce; hasDoubleJumped = true; TouchControls.Instance.JumpRequested = false; if (animator != null) animator.SetTrigger("Jump"); } velocityY += gravity * Time.deltaTime; }
        Vector3 finalMove = moveDir * targetSpeed + Vector3.up * velocityY; controller.Move(finalMove * Time.deltaTime);
        if (moveDir.magnitude > 0.1f && (TouchControls.Instance == null || !TouchControls.Instance.IsAiming)) { transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f); }
        
        // Drive Animator Parameters
        if (animator != null) { animator.SetFloat("Speed", isMoving ? (targetSpeed == runSpeed ? 1.0f : 0.5f) : 0f); animator.SetBool("IsCrouching", isCrouching); animator.SetBool("IsProne", isProne); animator.SetBool("IsSwimming", isSwimming); animator.SetBool("IsSliding", isSliding); }

        netPosition.Value = transform.position; netRotation.Value = transform.rotation;
    }


    private IEnumerator VaultRoutine(Vector3 targetPos)
    {
        controller.enabled = false;
        Vector3 startPos = transform.position;
        float t = 0;
        while (t < 1) { t += Time.deltaTime * 5f; transform.position = Vector3.Lerp(startPos, targetPos + Vector3.up * 0.1f, t); yield return null; }
        controller.enabled = true;
    }


    private void HandleProceduralAnimations()
    {
        if (humanoidModel == null) return; Vector2 moveInput = TouchControls.Instance != null ? TouchControls.Instance.MoveInput : Vector2.zero; bool isMoving = moveInput.magnitude > 0.1f;
        if (isMoving && controller.isGrounded && !isCrouching && !isProne) { walkBobTimer += Time.deltaTime * 12f; float bob = Mathf.Sin(walkBobTimer) * 0.05f; humanoidModel.transform.localPosition = new Vector3(0, bob, 0); } else { humanoidModel.transform.localPosition = Vector3.Lerp(humanoidModel.transform.localPosition, Vector3.zero, Time.deltaTime * 10f); }
        float targetPitch = (isMoving && moveInput.y > 0.7f && !isCrouching && !isProne) ? 15f : (isProne ? 90f : 0f); humanoidModel.transform.localRotation = Quaternion.Slerp(humanoidModel.transform.localRotation, Quaternion.Euler(targetPitch, 0, 0), Time.deltaTime * 8f);
        float targetYScale = isProne ? 0.3f : (isCrouching ? 0.6f : 1.0f); humanoidModel.transform.localScale = Vector3.Lerp(humanoidModel.transform.localScale, new Vector3(1f, targetYScale, 1f), Time.deltaTime * 10f);

        if (equippedGunModel != null) { float swayX = -TouchControls.Instance.LookDelta.x * 0.5f; float swayY = TouchControls.Instance.LookDelta.y * 0.5f; Quaternion swayRot = Quaternion.Euler(swayY, swayX, 0); recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * 15f); recoilRot = Quaternion.Slerp(recoilRot, Quaternion.identity, Time.deltaTime * 15f); equippedGunModel.transform.localPosition = origGunPos + recoilOffset; equippedGunModel.transform.localRotation = origGunRot * swayRot * recoilRot; }
    }

    private void HandleCameraAndAim() { if (TouchControls.Instance == null) return; Vector2 lookDelta = TouchControls.Instance.LookDelta; cameraPivot.Rotate(Vector3.up * lookDelta.x, Space.World); float newPitch = cameraPivot.localEulerAngles.x - lookDelta.y; if (newPitch > 180f) newPitch -= 360f; newPitch = Mathf.Clamp(newPitch, -45f, 60f); cameraPivot.localEulerAngles = new Vector3(newPitch, cameraPivot.localEulerAngles.y, 0); if (TouchControls.Instance.IsAiming || TouchControls.Instance.IsFiring) { Vector3 aimDir = cameraPivot.forward; aimDir.y = 0; transform.rotation = Quaternion.LookRotation(aimDir); Cinemachine3rdPersonFollow body = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>(); body.CameraDistance = Mathf.Lerp(body.CameraDistance, weaponSlots[activeWeaponSlot]?.hasScope ?? false ? 1.5f : 2.5f, Time.deltaTime * 10f); } else { Cinemachine3rdPersonFollow body = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>(); body.CameraDistance = Mathf.Lerp(body.CameraDistance, 4f, Time.deltaTime * 10f); } netRotation.Value = transform.rotation; }

    private void HandleShooting() { if (shotCooldown > 0) shotCooldown -= Time.deltaTime; if (TouchControls.Instance == null) return; WeaponData curWeapon = weaponSlots[activeWeaponSlot]; if (curWeapon == null || isReloading || isSwimming || isZiplining) return; if (TouchControls.Instance.ReloadRequested) { TouchControls.Instance.ReloadRequested = false; if (currentMagAmmo[activeWeaponSlot] < (curWeapon.maxAmmo + (curWeapon.hasExtMag ? 10 : 0))) { StartCoroutine(ReloadCoroutine(curWeapon)); return; } } if (TouchControls.Instance.IsFiring && shotCooldown <= 0) { if (currentMagAmmo[activeWeaponSlot] > 0) { ShootWeapon(curWeapon); if (!curWeapon.isAutomatic) TouchControls.Instance.ResetFiring(); } else { StartCoroutine(ReloadCoroutine(curWeapon)); } } }

    private void ShootWeapon(WeaponData weapon)
    {
        currentMagAmmo[activeWeaponSlot]--; float fr = weapon.fireRate; if (activePowers.ContainsKey(PowerType.RageMode)) fr *= 0.5f; shotCooldown = fr;
        recoilOffset = new Vector3(0, 0, -0.05f); recoilRot = Quaternion.Euler(-15f, Random.Range(-2f, 2f), 0);
        if (animator != null) animator.SetTrigger("Fire"); if (weaponAnimator != null) weaponAnimator.SetTrigger("Fire");
        muzzleFlash.Play(); if (AudioManager.Instance != null) AudioManager.Instance.PlayWeaponSound(weapon.weaponName); PlayMuzzleFlashServerRpc(weapon.weaponName);

        if (weapon.IsMelee())
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMeleeSwingSound();
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, 2f);
            foreach (var col in hits)
            {
                AIBot bot = col.GetComponentInParent<AIBot>(); PlayerController hitPlayer = col.GetComponentInParent<PlayerController>();
                if (bot != null) {
                    bot.RequestTakeDamageServerRpc(weapon.damage, playerName, transform.position);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayMeleeHitSound();
                    if (GameFeel.Instance != null) GameFeel.Instance.TriggerMeleeImpact(col.ClosestPoint(transform.position), Vector3.zero);
                }
                else if (hitPlayer != null && hitPlayer != this) {
                    hitPlayer.RequestTakeDamageServerRpc(weapon.damage, playerName, transform.position);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayMeleeHitSound();
                    if (GameFeel.Instance != null) GameFeel.Instance.TriggerMeleeImpact(col.ClosestPoint(transform.position), Vector3.zero);
                }
                if (GameFeel.Instance != null && (bot != null || hitPlayer != null)) GameFeel.Instance.SpawnImpact(col.ClosestPoint(transform.position), Vector3.zero, "Blood");
            }
            return;
        }

        int pelletCount = weapon.pellets; float dmg = weapon.damage; if (activePowers.ContainsKey(PowerType.RageMode)) dmg *= 2f; bool isBoosted = activePowers.ContainsKey(PowerType.RageMode); float spr = weapon.spread; if (weapon.hasGrip) spr *= 0.5f;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 shootDir = cameraPivot.forward; if (spr > 0) { shootDir += new Vector3(Random.Range(-spr, spr), Random.Range(-spr, spr), Random.Range(-spr, spr)); shootDir.Normalize(); }
            RaycastHit hit;
            if (Physics.Raycast(cameraPivot.position, shootDir, out hit, 200f))
            {
                if (GameFeel.Instance != null) { GameFeel.Instance.SpawnTracer(muzzleFlash.transform.position, hit.point); string sType = hit.collider.gameObject.name.Contains("Terrain") ? "Dirt" : (hit.collider.gameObject.name.Contains("Building") ? "Wood" : "Metal"); GameFeel.Instance.SpawnImpact(hit.point, hit.normal, sType); }
                AIBot bot = hit.collider.GetComponentInParent<AIBot>(); PlayerController hitPlayer = hit.collider.GetComponentInParent<PlayerController>();
                if (bot != null)
                {
                    bool isHead = (hit.point.y - bot.transform.position.y) > 1.6f; float fDmg = isHead ? dmg * 1.5f : dmg;
                    bot.RequestTakeDamageServerRpc(fDmg, playerName, cameraPivot.position); if (KillSystem.Instance != null) KillSystem.Instance.AddPlayerDamageDealt(fDmg);
                    if (GameFeel.Instance != null) { GameFeel.Instance.SpawnDamageNumber(hit.point, fDmg, isHead, isBoosted); GameFeel.Instance.SpawnImpact(hit.point, hit.normal, "Blood"); GameFeel.Instance.TriggerHitStop(); }
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayHitMarker(isHead);
                }
                else if (hitPlayer != null && hitPlayer != this)
                {
                    bool isHead = (hit.point.y - hitPlayer.transform.position.y) > 1.6f; float fDmg = isHead ? dmg * 1.5f : dmg;
                    hitPlayer.RequestTakeDamageServerRpc(fDmg, playerName, cameraPivot.position); if (KillSystem.Instance != null) KillSystem.Instance.AddPlayerDamageDealt(fDmg);
                    if (GameFeel.Instance != null) { GameFeel.Instance.SpawnDamageNumber(hit.point, fDmg, isHead, isBoosted); GameFeel.Instance.SpawnImpact(hit.point, hit.normal, "Blood"); GameFeel.Instance.TriggerHitStop(); }
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayHitMarker(isHead);
                }
                else { if (GameFeel.Instance != null) GameFeel.Instance.SpawnDecal(hit.point, hit.normal); }
            }
        }
    }



    [ServerRpc(RequireOwnership = false)] private void PlayMuzzleFlashServerRpc(string wName) { PlayMuzzleFlashClientRpc(wName); } [ClientRpc] private void PlayMuzzleFlashClientRpc(string wName) { if (IsOwner) return; muzzleFlash.Play(); if (AudioManager.Instance != null) AudioManager.Instance.PlayWeaponSound(wName); if (animator != null) animator.SetTrigger("Fire"); }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTakeDamageServerRpc(float dmg, string killerName, Vector3 shooterPos)
    {
        if (netIsDead.Value) return;
        // Server-Authoritative Anti-Cheat Range Verification
        float d = Vector3.Distance(transform.position, shooterPos);
        if (d > 220f) { Debug.LogWarning("Server Anti-Cheat: Shooter exceeded max ballistic range (" + d + "m). Discarding hit."); if (BackendAPI.Instance != null && BackendAPI.Instance.IsLoggedIn) { var _ = BackendAPI.Instance.LogAntiCheatViolationAsync("RangeExploit", "Shot from " + d + "m"); } return; }

        float curArmor = netArmor.Value; if (curArmor > 0) { float armorDmg = Mathf.Min(curArmor, dmg * 0.7f); netArmor.Value -= armorDmg; dmg -= armorDmg; } netHP.Value -= dmg; TakeDamageClientRpc();
        if (netHP.Value <= 0) { netHP.Value = 0; 
            if (ReviveSystem.Instance != null && ReviveSystem.Instance.ShouldEnterDBNO("CLASSIC", reviveHandler != null ? reviveHandler.knockCount.Value : 0)) {
                if (reviveHandler != null) reviveHandler.EnterDownedStateServerRpc();
            } else {
                netIsDead.Value = true; DieClientRpc(killerName); if (KillSystem.Instance != null) KillSystem.Instance.OnEntityKilled(playerName, killerName, true, false);
            }
        }
    }

    [ClientRpc] private void TakeDamageClientRpc() { if (IsOwner && GameFeel.Instance != null) GameFeel.Instance.TriggerScreenShake(); }
    [ClientRpc] private void DieClientRpc(string killerName) { Debug.Log("Player eliminated by " + killerName); if (IsOwner && AudioManager.Instance != null) { AudioManager.Instance.SetZoneDamageActive(false); AudioManager.Instance.PlayDeathSound(); } if (animator != null) animator.SetBool("IsDead", true); TriggerRagdollDeath(); }

    private void TriggerRagdollDeath() { ragdollTriggered = true; if (controller != null) controller.enabled = false; if (humanoidModel != null) { foreach (Transform t in humanoidModel.transform) { Rigidbody rb = t.gameObject.GetComponent<Rigidbody>(); if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>(); rb.AddTorque(Random.insideUnitSphere * 50f); rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse); } } }

    private void HandleSpectatorMode() { if (!IsOwner || vcam == null) return; if (spectatePool.Count == 0) { PlayerController[] ps = FindObjectsOfType<PlayerController>(); foreach (var p in ps) { if (!p.netIsDead.Value && p != this) spectatePool.Add(p.transform); } AIBot[] bs = FindObjectsOfType<AIBot>(); foreach (var b in bs) { if (!b.netIsDead.Value) spectatePool.Add(b.transform); } } if (spectatePool.Count > 0) { if (spectatorTarget == null || Input.GetMouseButtonDown(0)) { spectatorTarget = spectatePool[spectateIndex % spectatePool.Count]; spectateIndex++; vcam.Follow = spectatorTarget; vcam.LookAt = spectatorTarget; } } }

    private IEnumerator ReloadCoroutine(WeaponData weapon) { isReloading = true; if (animator != null) animator.SetTrigger("Reload"); if (weaponAnimator != null) weaponAnimator.SetTrigger("Reload"); yield return new WaitForSeconds(weapon.reloadTime); int needed = (weapon.maxAmmo + (weapon.hasExtMag ? 10 : 0)) - currentMagAmmo[activeWeaponSlot]; int available = ammoInventory.ContainsKey(weapon.ammoType) ? ammoInventory[weapon.ammoType] : 0; int toAdd = Mathf.Min(needed, available); currentMagAmmo[activeWeaponSlot] += toAdd; if (ammoInventory.ContainsKey(weapon.ammoType)) ammoInventory[weapon.ammoType] -= toAdd; isReloading = false; UpdateHUD(); }

    private void HandlePowers() { List<PowerType> keys = new List<PowerType>(activePowers.Keys); foreach (PowerType key in keys) { activePowers[key] -= Time.deltaTime; if (activePowers[key] <= 0) { DeactivatePower(key); } else if (key == PowerType.HealSurge && IsServer) { netHP.Value = Mathf.Min(maxHP, netHP.Value + 10f * Time.deltaTime); } else if (key == PowerType.Magnet) { Collider[] cols = Physics.OverlapSphere(transform.position, 15f); foreach (Collider c in cols) { PickupItem p = c.GetComponent<PickupItem>(); if (p != null) { p.transform.position = Vector3.MoveTowards(p.transform.position, transform.position, Time.deltaTime * 12f); } } } } if (TouchControls.Instance != null && TouchControls.Instance.PowerUpRequested) { TouchControls.Instance.PowerUpRequested = false; if (nearbyPowerUp != null) { ActivatePower(nearbyPowerUp.powerType); if (IsServer) Destroy(nearbyPowerUp.gameObject); else RequestDestroyPickupServerRpc(nearbyPowerUp.GetComponent<NetworkObject>()?.NetworkObjectId ?? 0); nearbyPowerUp = null; TouchControls.Instance.SetPowerButtonActive(false); if (AudioManager.Instance != null) AudioManager.Instance.PlayPowerPickupSound(); } else if (consumableCount > 0) { consumableCount--; if (IsServer) netHP.Value = Mathf.Min(maxHP, netHP.Value + 50f); else RequestHealServerRpc(50f); UpdateHUD(); } } }
    [ServerRpc(RequireOwnership = false)] private void RequestHealServerRpc(float amt) { netHP.Value = Mathf.Min(maxHP, netHP.Value + amt); } [ServerRpc(RequireOwnership = false)] private void RequestDestroyPickupServerRpc(ulong netObjId) { if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject netObj)) { Destroy(netObj.gameObject); } }

    public void ActivatePower(PowerType type) { PowerData pData = Resources.Load<PowerData>("Powers/" + type); if (pData == null) pData = PowerData.GetDefaultPower(type); activePowers[type] = pData.duration; switch (type) { case PowerType.SpeedBoost: yellowTrail.SetActive(true); break; case PowerType.ShieldBurst: if (IsServer) netArmor.Value = Mathf.Min(maxArmor, netArmor.Value + 50f); else RequestArmorServerRpc(50f); break; case PowerType.RageMode: if (redVignetteGo != null) redVignetteGo.SetActive(true); break; case PowerType.Invisibility: if (IsServer) netIsInvisible.Value = true; else RequestInvisibilityServerRpc(true); break; case PowerType.HealSurge: if (greenPulseLight != null) greenPulseLight.gameObject.SetActive(true); break; case PowerType.DoubleJump: canDoubleJump = true; cyanFootDust.SetActive(true); break; case PowerType.Magnet: purpleGlow.SetActive(true); break; } if (GameHUD.Instance != null) GameHUD.Instance.UpdatePowerUpBar(pData.powerName, pData.duration, pData.duration, pData.iconColor); }
    [ServerRpc(RequireOwnership = false)] private void RequestArmorServerRpc(float amt) { netArmor.Value = Mathf.Min(maxArmor, netArmor.Value + amt); } [ServerRpc(RequireOwnership = false)] private void RequestInvisibilityServerRpc(bool inv) { netIsInvisible.Value = inv; }
    private void DeactivatePower(PowerType type) { activePowers.Remove(type); switch (type) { case PowerType.SpeedBoost: yellowTrail.SetActive(false); break; case PowerType.RageMode: if (redVignetteGo != null) redVignetteGo.SetActive(false); break; case PowerType.Invisibility: if (IsServer) netIsInvisible.Value = false; else RequestInvisibilityServerRpc(false); break; case PowerType.HealSurge: if (greenPulseLight != null) greenPulseLight.gameObject.SetActive(false); break; case PowerType.DoubleJump: canDoubleJump = false; cyanFootDust.SetActive(false); break; case PowerType.Magnet: purpleGlow.SetActive(false); break; } if (GameHUD.Instance != null) GameHUD.Instance.UpdatePowerUpBar("", 0, 1, Color.white); }

    private void HandleZone() { if (ZoneController.Instance == null) return; bool outside = ZoneController.Instance.IsOutsideZone(transform.position); if (zoneVignetteGo != null) zoneVignetteGo.SetActive(outside); if (AudioManager.Instance != null && IsOwner) AudioManager.Instance.SetZoneDamageActive(outside, ZoneController.Instance.GetShrinkProgress()); if (GameFeel.Instance != null && IsOwner) GameFeel.Instance.SetZoneVignetteActive(outside); if (outside && IsServer) { zoneDamageTimer += Time.deltaTime; if (zoneDamageTimer >= 1f) { zoneDamageTimer = 0f; RequestTakeDamageServerRpc(1f, "The Zone", transform.position); } } else { zoneDamageTimer = 0f; } }

    private void HandleLootAndInteractions() { if (TouchControls.Instance == null) return; if (TouchControls.Instance.SwapWeaponRequested) { TouchControls.Instance.SwapWeaponRequested = false; activeWeaponSlot = (activeWeaponSlot + 1) % 2; isReloading = false; EquipGunModel(weaponSlots[activeWeaponSlot]?.weaponName ?? "Pistol"); UpdateHUD(); } if (TouchControls.Instance.LootRequested) { TouchControls.Instance.LootRequested = false; if (nearbyVehicle != null) { currentVehicle = nearbyVehicle; controller.enabled = false; currentVehicle.RequestEnterServerRpc(OwnerClientId); } else if (nearbyZipline != null) { isZiplining = true; controller.enabled = false; } else if (nearbyPickup != null) { Pickup(nearbyPickup); nearbyPickup = null; TouchControls.Instance.SetLootButtonActive(false); } } }
    private void Pickup(PickupItem p)
    {
        if (p.pickupType == PickupType.Weapon)
        {
            WeaponData newW = Resources.Load<WeaponData>("Weapons/" + p.weaponName);
            if (newW == null) newW = WeaponData.GetDefaultWeapon(p.weaponName);
            if (weaponSlots[1] == null && weaponSlots[0] != null)
            {
                weaponSlots[1] = newW; currentMagAmmo[1] = newW.maxAmmo; activeWeaponSlot = 1;
            }
            else
            {
                if (weaponSlots[activeWeaponSlot] != null && LootSpawner.Instance != null && IsServer)
                    LootSpawner.Instance.SpawnWeaponPickup(weaponSlots[activeWeaponSlot].weaponName, transform.position);
                weaponSlots[activeWeaponSlot] = newW; currentMagAmmo[activeWeaponSlot] = newW.maxAmmo;
            }
            EquipGunModel(newW.weaponName);
        }
        else if (p.pickupType == PickupType.Ammo)
        {
            if (!ammoInventory.ContainsKey(p.ammoType)) ammoInventory[p.ammoType] = 0;
            ammoInventory[p.ammoType] += p.amount;
            // EnergyAmmo support
            if (p.ammoType == AmmoType.EnergyAmmo && !ammoInventory.ContainsKey(AmmoType.EnergyAmmo))
                ammoInventory[AmmoType.EnergyAmmo] = 0;
        }
        else if (p.pickupType == PickupType.HealthPack) { consumableCount++; }
        else if (p.pickupType == PickupType.ArmorPack)
        {
            if (IsServer) netArmor.Value = Mathf.Min(maxArmor, netArmor.Value + p.amount);
            else RequestArmorServerRpc(p.amount);
        }
        else if (p.pickupType == PickupType.Attachment)
        {
            // Auto-equip attachment to current weapon if compatible
            if (weaponSlots[activeWeaponSlot] != null && EvoWeaponSystem.Instance != null)
            {
                string wId = weaponSlots[activeWeaponSlot].weaponName;
                bool equipped = EvoWeaponSystem.Instance.EquipAttachment(wId, p.attachmentName);
                if (!equipped)
                {
                    // Store in inventory for later
                    EvoWeaponSystem.Instance.AddAttachmentToInventory(playerName, p.attachmentName);
                }
            }
            else
            {
                if (EvoWeaponSystem.Instance != null)
                    EvoWeaponSystem.Instance.AddAttachmentToInventory(playerName, p.attachmentName);
            }
        }
        else if (p.pickupType == PickupType.Throwable)
        {
            // Throwable pickups go to grenade inventory (handled by TouchControls)
            if (TouchControls.Instance != null)
                TouchControls.Instance.AddThrowableToInventory(p.throwableType, 1);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPowerPickupSound();
        if (IsServer) Destroy(p.gameObject);
        else RequestDestroyPickupServerRpc(p.GetComponent<NetworkObject>()?.NetworkObjectId ?? 0);
        UpdateHUD();
    }

    private void OnTriggerEnter(Collider other) { if (!IsOwner) return; if (other.gameObject.name.Contains("WaterPlane")) isSwimming = true; Vehicle v = other.GetComponentInParent<Vehicle>(); if (v != null) { nearbyVehicle = v; if (TouchControls.Instance != null) TouchControls.Instance.SetLootButtonActive(true, "DRIVE " + v.vType); return; } Zipline z = other.GetComponentInParent<Zipline>(); if (z != null) { nearbyZipline = z; if (TouchControls.Instance != null) TouchControls.Instance.SetLootButtonActive(true, "ZIPLINE"); return; } LedgeClimb l = other.GetComponentInParent<LedgeClimb>(); if (l != null) nearbyLedge = l; PickupItem p = other.GetComponent<PickupItem>(); if (p != null) { if (p.pickupType == PickupType.PowerUp) { nearbyPowerUp = p; if (TouchControls.Instance != null) TouchControls.Instance.SetPowerButtonActive(true, p.powerType.ToString()); } else { nearbyPickup = p; string label = p.pickupType == PickupType.Weapon ? p.weaponName : p.pickupType.ToString(); if (TouchControls.Instance != null) TouchControls.Instance.SetLootButtonActive(true, label); } } }
    private void OnTriggerExit(Collider other) { if (!IsOwner) return; if (other.gameObject.name.Contains("WaterPlane")) isSwimming = false; Vehicle v = other.GetComponentInParent<Vehicle>(); if (v != null && v == nearbyVehicle) { nearbyVehicle = null; if (TouchControls.Instance != null) TouchControls.Instance.SetLootButtonActive(false); return; } Zipline z = other.GetComponentInParent<Zipline>(); if (z != null && z == nearbyZipline) { nearbyZipline = null; if (TouchControls.Instance != null) TouchControls.Instance.SetLootButtonActive(false); return; } LedgeClimb l = other.GetComponentInParent<LedgeClimb>(); if (l != null && l == nearbyLedge) nearbyLedge = null; PickupItem p = other.GetComponent<PickupItem>(); if (p != null) { if (p == nearbyPowerUp) { nearbyPowerUp = null; if (TouchControls.Instance != null) TouchControls.Instance.SetPowerButtonActive(false); } else if (p == nearbyPickup) { nearbyPickup = null; if (TouchControls.Instance != null) TouchControls.Instance.SetLootButtonActive(false); } } }

    /// <summary>
    /// Handles all new system inputs: grenades, emotes, pings, barricades, traps, talents, prone.
    /// Each reads its flag from TouchControls, consumes it, and calls the appropriate system.
    /// </summary>
    private void HandleNewSystems()
    {
        if (TouchControls.Instance == null) return;

        // Grenade throw
        if (TouchControls.Instance.GrenadeRequested)
        {
            TouchControls.Instance.GrenadeRequested = false;
            if (ThrowableSystem.Instance != null)
            {
                Vector3 dir = cameraPivot != null ? cameraPivot.forward : transform.forward;
                if (IsServer) ThrowableSystem.Instance.ThrowProjectile(TouchControls.Instance.SelectedThrowable, transform.position, dir, 18f, playerName);
                else RequestThrowGrenadeServerRpc((int)TouchControls.Instance.SelectedThrowable, transform.position, dir);
            }
        }

        // Emote
        if (TouchControls.Instance.EmoteRequested)
        {
            TouchControls.Instance.EmoteRequested = false;
            EmoteSystem emote = GetComponent<EmoteSystem>();
            if (emote != null) emote.TriggerEmote(TouchControls.Instance.SelectedEmoteSlot);
        }

        // Ping
        if (TouchControls.Instance.PingRequested)
        {
            TouchControls.Instance.PingRequested = false;
            if (PingSystem.Instance != null)
                PingSystem.Instance.PingFromCamera(PingType.GenericPing, playerName);
        }

        // Barricade build
        if (TouchControls.Instance.BarricadeRequested)
        {
            TouchControls.Instance.BarricadeRequested = false;
            if (BarricadeSystem.Instance != null)
            {
                Vector3 buildPos = transform.position + transform.forward * 3f;
                BarricadeSystem.Instance.Build(BarricadeType.WoodWall, buildPos, transform.rotation);
            }
        }

        // Trap deploy
        if (TouchControls.Instance.TrapDeployRequested)
        {
            TouchControls.Instance.TrapDeployRequested = false;
            if (TrapSystem.Instance != null)
            {
                Vector3 trapPos = transform.position + transform.forward * 2f;
                trapPos.y = 0;
                TrapSystem.Instance.CraftAndDeploy(TouchControls.Instance.SelectedTrap, trapPos, transform.rotation, OwnerClientId);
            }
        }

        // Talent tree (awards are automatic via KillSystem — this just opens UI notification)
        if (TouchControls.Instance.TalentTreeRequested)
        {
            TouchControls.Instance.TalentTreeRequested = false;
            if (TalentTreeSystem.Instance != null && TalentTreeSystem.Instance.availablePoints > 0)
            {
                // Auto-spend on best available talent
                var talents = TalentTreeSystem.Instance.GetAllTalents();
                foreach (var kvp in talents)
                {
                    if (!kvp.Value.isUnlocked && TalentTreeSystem.Instance.UnlockTalent(kvp.Key))
                        break;
                }
            }
        }

        // Prone toggle
        if (TouchControls.Instance.ProneRequested)
        {
            TouchControls.Instance.ProneRequested = false;
            isProne = !isProne;
            if (isProne) isCrouching = false;
            if (controller != null)
            {
                controller.height = isProne ? 0.5f : (isCrouching ? 1.2f : 2f);
                controller.center = new Vector3(0, isProne ? 0.25f : (isCrouching ? 0.6f : 1f), 0);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestThrowGrenadeServerRpc(int throwableTypeIndex, Vector3 origin, Vector3 direction)
    {
        if (ThrowableSystem.Instance != null)
            ThrowableSystem.Instance.ThrowProjectile((ThrowableType)throwableTypeIndex, origin, direction, 18f, playerName);
    }

    private void UpdateHUD() { if (GameHUD.Instance == null || !IsOwner) return; GameHUD.Instance.UpdateHealthArmor(netHP.Value, maxHP, netArmor.Value, maxArmor); WeaponData curW = weaponSlots[activeWeaponSlot]; if (curW != null) { int reserve = ammoInventory.ContainsKey(curW.ammoType) ? ammoInventory[curW.ammoType] : 0; GameHUD.Instance.UpdateWeaponAmmo(curW.weaponName, currentMagAmmo[activeWeaponSlot], reserve, curW.iconColor); } else { GameHUD.Instance.UpdateWeaponAmmo("UNARMED", 0, 0, Color.gray); } string w1 = weaponSlots[0] != null ? weaponSlots[0].weaponName : "EMPTY"; string w2 = weaponSlots[1] != null ? weaponSlots[1].weaponName : "EMPTY"; GameHUD.Instance.UpdateInventory(w1, w2, activeWeaponSlot, "Health Pack", consumableCount); if (activePowers.Count > 0) { foreach (var kvp in activePowers) { GameHUD.Instance.UpdatePowerUpBar(kvp.Key.ToString(), kvp.Value, 10f, Color.magenta); break; } } }
}


