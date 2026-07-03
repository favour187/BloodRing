using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Battle-royale-grade Parachute/Plane drop spawn system.
/// Players start in a plane flying over the map and can jump out,
/// then glide with a parachute before landing.
/// </summary>
public class ParachuteDrop : NetworkBehaviour
{
    public static ParachuteDrop Instance;

    private GameObject planeMesh;
    private Vector3 planeStart;
    private Vector3 planeEnd;
    private float planeSpeed = 60f;
    private float flightProgress = 0f;
    private bool isInPlane = true;
    private bool isParachuting = false;
    private bool hasLanded = false;

    private GameObject playerParachute;
    private float glideSpeed = 12f;
    private float fallSpeed = 5f;
    private float freefallSpeed = 25f;
    private float landingHeight = 2f;
    private Vector2 glideInput = Vector2.zero;

    private Transform playerTransform;
    private Camera dropCamera;
    private Canvas dropCanvas;
    private Text altitudeText;
    private Text instructionText;
    private GameObject jumpButton;

    private void Awake() { Instance = this; }

    public void InitializePlaneDropSequence(Transform player)
    {
        playerTransform = player;
        isInPlane = true;
        isParachuting = false;
        hasLanded = false;

        // Randomize plane path across the map
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float mapRadius = 220f;
        planeStart = new Vector3(Mathf.Sin(angle) * mapRadius, 120f, Mathf.Cos(angle) * mapRadius);
        planeEnd = new Vector3(-Mathf.Sin(angle) * mapRadius, 120f, -Mathf.Cos(angle) * mapRadius);

        // Create plane visual
        planeMesh = new GameObject("TransportPlane");
        planeMesh.transform.position = planeStart;
        planeMesh.transform.LookAt(planeEnd);

        // Fuselage
        GameObject fuselage = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Capsule.obj");
        fuselage.name = "Fuselage";
        fuselage.transform.SetParent(planeMesh.transform);
        fuselage.transform.localPosition = Vector3.zero;
        fuselage.transform.localRotation = Quaternion.Euler(0, 0, 90);
        fuselage.transform.localScale = new Vector3(3f, 12f, 3f);
        Destroy(fuselage.GetComponent<Collider>());
        Material planeMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        planeMat.color = new Color(0.6f, 0.65f, 0.7f);
        fuselage.GetComponent<Renderer>().material = planeMat;

        // Wings
        GameObject leftWing = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        leftWing.name = "LeftWing";
        leftWing.transform.SetParent(planeMesh.transform);
        leftWing.transform.localPosition = new Vector3(-8f, 0, 0);
        leftWing.transform.localScale = new Vector3(12f, 0.3f, 4f);
        Destroy(leftWing.GetComponent<Collider>());
        leftWing.GetComponent<Renderer>().material = planeMat;

        GameObject rightWing = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        rightWing.name = "RightWing";
        rightWing.transform.SetParent(planeMesh.transform);
        rightWing.transform.localPosition = new Vector3(8f, 0, 0);
        rightWing.transform.localScale = new Vector3(12f, 0.3f, 4f);
        Destroy(rightWing.GetComponent<Collider>());
        rightWing.GetComponent<Renderer>().material = planeMat;

        // Tail
        GameObject tail = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cube.obj");
        tail.name = "Tail";
        tail.transform.SetParent(planeMesh.transform);
        tail.transform.localPosition = new Vector3(0, 3f, -10f);
        tail.transform.localScale = new Vector3(6f, 4f, 0.3f);
        Destroy(tail.GetComponent<Collider>());
        tail.GetComponent<Renderer>().material = planeMat;

        // Engine trail
        GameObject trailGo = new GameObject("EngineTrail");
        trailGo.transform.SetParent(planeMesh.transform);
        trailGo.transform.localPosition = new Vector3(0, 0, -12f);
        ParticleSystem ps = trailGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = trailGo.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main;
        main.loop = true;
        main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        main.startSize = 2f;
        main.startSpeed = 1f;
        main.startLifetime = 3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 30;
        ps.Play();

        // Place player in plane
        if (playerTransform != null)
        {
            playerTransform.position = planeStart;
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Create drop UI
        CreateDropUI();

        StartCoroutine(PlaneFlightSequence());
    }

    private void CreateDropUI()
    {
        GameObject canvasGo = new GameObject("DropCanvas");
        dropCanvas = canvasGo.AddComponent<Canvas>();
        dropCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dropCanvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Altitude display
        GameObject altGo = new GameObject("AltitudeText");
        altGo.transform.SetParent(canvasGo.transform, false);
        altitudeText = altGo.AddComponent<Text>();
        altitudeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        altitudeText.fontSize = 36;
        altitudeText.fontStyle = FontStyle.Bold;
        altitudeText.color = Color.white;
        altitudeText.alignment = TextAnchor.UpperRight;
        RectTransform altRect = altGo.GetComponent<RectTransform>();
        altRect.anchorMin = new Vector2(1, 1);
        altRect.anchorMax = new Vector2(1, 1);
        altRect.anchoredPosition = new Vector2(-20, -60);
        altRect.sizeDelta = new Vector2(300, 60);

        // Instruction text
        GameObject instGo = new GameObject("InstructionText");
        instGo.transform.SetParent(canvasGo.transform, false);
        instructionText = instGo.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 32;
        instructionText.fontStyle = FontStyle.Bold;
        instructionText.color = Color.yellow;
        instructionText.alignment = TextAnchor.MiddleCenter;
        RectTransform instRect = instGo.GetComponent<RectTransform>();
        instRect.anchoredPosition = new Vector2(0, -200);
        instRect.sizeDelta = new Vector2(600, 60);
        instructionText.text = "TAP JUMP TO DROP!";

        // Jump button (large)
        jumpButton = UIBuilder.CreateButton(canvasGo.transform, "JumpDropBtn", "🪂 DROP!", new Vector2(0, -300),
            new Color(0.9f, 0.2f, 0.1f, 0.9f), Color.yellow, () => { OnJumpFromPlane(); });
        RectTransform jumpRect = jumpButton.GetComponent<RectTransform>();
        jumpRect.sizeDelta = new Vector2(350, 90);
    }

    private IEnumerator PlaneFlightSequence()
    {
        // Pre-flight countdown
        if (AudioManager.Instance != null) AudioManager.Instance.PlayVOCountdown();

        for (int i = 5; i > 0; i--)
        {
            if (instructionText != null)
                instructionText.text = "DEPLOYING IN: " + i;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBeep(i == 1);
            yield return new WaitForSeconds(1f);
        }

        if (instructionText != null) instructionText.text = "TAP TO DROP!";

        // Start flight
        while (isInPlane && flightProgress < 1f)
        {
            flightProgress += Time.deltaTime * (planeSpeed / Vector3.Distance(planeStart, planeEnd));
            if (planeMesh != null)
            {
                planeMesh.transform.position = Vector3.Lerp(planeStart, planeEnd, flightProgress);
                if (playerTransform != null && isInPlane)
                    playerTransform.position = planeMesh.transform.position;
            }

            if (altitudeText != null && playerTransform != null)
                altitudeText.text = "ALT: " + Mathf.RoundToInt(playerTransform.position.y) + "m";

            yield return null;
        }

        // Auto-jump at end of path
        if (isInPlane) OnJumpFromPlane();
    }

    public void OnJumpFromPlane()
    {
        if (!isInPlane) return;
        isInPlane = false;
        isParachuting = true;

        if (jumpButton != null) jumpButton.SetActive(false);
        if (instructionText != null) instructionText.text = "GLIDING... MOVE TO STEER";

        // Create player parachute
        CreatePlayerParachute();

        StartCoroutine(ParachuteDescentSequence());
    }

    private void CreatePlayerParachute()
    {
        playerParachute = new GameObject("PlayerParachute");
        playerParachute.transform.SetParent(playerTransform);
        playerParachute.transform.localPosition = new Vector3(0, 4f, 0);

        // Canopy
        GameObject canopy = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Sphere.obj");
        canopy.name = "Canopy";
        canopy.transform.SetParent(playerParachute.transform);
        canopy.transform.localPosition = Vector3.zero;
        canopy.transform.localScale = new Vector3(4f, 1.5f, 4f);
        Destroy(canopy.GetComponent<Collider>());
        Material chuteMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        chuteMat.color = new Color(0.9f, 0.3f, 0.1f, 0.85f);
        chuteMat.SetFloat("_Mode", 3);
        chuteMat.SetInt("_ZWrite", 0);
        chuteMat.renderQueue = 3000;
        canopy.GetComponent<Renderer>().material = chuteMat;

        // Lines
        for (int i = 0; i < 4; i++)
        {
            GameObject line = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
            line.name = "Line_" + i;
            line.transform.SetParent(playerParachute.transform);
            float angle = i * 90f * Mathf.Deg2Rad;
            line.transform.localPosition = new Vector3(Mathf.Sin(angle) * 0.8f, -2f, Mathf.Cos(angle) * 0.8f);
            line.transform.localScale = new Vector3(0.03f, 2f, 0.03f);
            Destroy(line.GetComponent<Collider>());
            Material lineMat = new Material(ProceduralArt.GetSafeShader("Unlit/Color"));
            lineMat.color = Color.white;
            line.GetComponent<Renderer>().material = lineMat;
        }
    }

    private IEnumerator ParachuteDescentSequence()
    {
        while (isParachuting && playerTransform != null)
        {
            // Get input for steering
            Vector2 moveInput = Vector2.zero;
            if (TouchControls.Instance != null)
                moveInput = TouchControls.Instance.MoveInput;

            // Movement
            Vector3 horizontalMove = new Vector3(moveInput.x * glideSpeed, 0, moveInput.y * glideSpeed);
            Vector3 descent = new Vector3(0, -fallSpeed, 0);
            playerTransform.position += (horizontalMove + descent) * Time.deltaTime;

            // Update altitude display
            if (altitudeText != null)
                altitudeText.text = "ALT: " + Mathf.RoundToInt(playerTransform.position.y) + "m";

            // Landing check
            if (playerTransform.position.y <= landingHeight)
            {
                OnLanded();
                yield break;
            }

            yield return null;
        }
    }

    private void OnLanded()
    {
        isParachuting = false;
        hasLanded = true;

        // Remove parachute
        if (playerParachute != null) Destroy(playerParachute);
        if (planeMesh != null) Destroy(planeMesh);

        // Re-enable character controller
        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(playerTransform.position.x, 1f, playerTransform.position.z);
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }

        // Remove drop UI
        if (dropCanvas != null) Destroy(dropCanvas.gameObject);

        // Landing particles
        if (playerTransform != null)
        {
            GameObject dustGo = new GameObject("LandingDust");
            dustGo.transform.position = playerTransform.position;
            ParticleSystem ps = dustGo.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = dustGo.GetComponent<ParticleSystemRenderer>();
            psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startColor = new Color(0.6f, 0.5f, 0.3f, 0.6f);
            main.startSize = 1f;
            main.startSpeed = 3f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });
            ps.Play();
            Destroy(dustGo, 2f);
        }

        if (instructionText != null) instructionText.text = "";
        Debug.Log("[ParachuteDrop] Player landed successfully!");
    }

    public bool HasLanded() { return hasLanded; }
    public bool IsInPlane() { return isInPlane; }
    public bool IsParachuting() { return isParachuting; }
}


