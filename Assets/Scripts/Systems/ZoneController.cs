using UnityEngine;

/// <summary>
/// Dynamic shrinking safe zone with random final circles.
/// Uses MapData for map-specific zone parameters.
/// 6 phases: wait → shrink → wait → shrink → wait → final.
/// Zone center shifts randomly each phase for dynamic gameplay.
/// </summary>
public class ZoneController : MonoBehaviour
{
    public static ZoneController Instance;

    public float currentRadius = 250f;
    public float elapsedTime = 0f;
    public Vector3 zoneCenter = Vector3.zero;

    private GameObject zoneWall;
    private Material zoneMat;
    private MapData mapData;

    // Zone phases with configurable timing and radii
    private readonly float[] phaseWaitTimes = { 120f, 0f, 30f, 0f, 30f, 0f };
    private readonly float[] phaseShrinkTimes = { 0f, 90f, 0f, 60f, 0f, 30f };
    private readonly float[] phaseTargetRadii = { 250f, 150f, 150f, 75f, 75f, 25f };

    private int currentPhase = 0;
    private string phaseText = "ZONE STABLE";
    private float uvOffset = 0f;
    private float phaseTime = 0f;
    private float startRadius;
    private Vector3 startCenter;
    private Vector3 targetCenter;

    private void Awake() { Instance = this; }

    public void InitializeZone()
    {
        Debug.Log("Initializing Dynamic Shrinking Zone...");
        mapData = MapData.GetDefaultMap("IslaVerde");
        currentRadius = mapData.mapRadius * 0.5f;
        zoneCenter = mapData.mapCenter;

        // Update phase radii based on map size
        float scale = mapData.mapRadius / 500f;
        phaseTargetRadii[0] = currentRadius;
        phaseTargetRadii[1] = currentRadius * 0.6f;
        phaseTargetRadii[2] = currentRadius * 0.6f;
        phaseTargetRadii[3] = currentRadius * 0.3f;
        phaseTargetRadii[4] = currentRadius * 0.3f;
        phaseTargetRadii[5] = 25f * scale;

        zoneWall = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj");
        zoneWall.name = "ZoneCylinderWall";
        zoneWall.transform.position = new Vector3(zoneCenter.x, 50f, zoneCenter.z);
        zoneWall.transform.localScale = new Vector3(currentRadius * 2f, 50f, currentRadius * 2f);
        Destroy(zoneWall.GetComponent<Collider>());

        zoneMat = new Material(ProceduralArt.GetSafeShader("Standard"));
        zoneMat.color = new Color(0.1f, 0.2f, 1f, 0.35f);
        zoneMat.SetFloat("_Mode", 3);
        zoneMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        zoneMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        zoneMat.SetInt("_ZWrite", 0);
        zoneMat.EnableKeyword("_ALPHABLEND_ON");
        zoneMat.renderQueue = 3000;
        zoneMat.mainTextureScale = new Vector2(20, 5);
        zoneWall.GetComponent<Renderer>().material = zoneMat;
        zoneWall.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        zoneWall.GetComponent<Renderer>().receiveShadows = false;

        currentPhase = 0;
        phaseTime = 0f;
        startRadius = currentRadius;
        startCenter = zoneCenter;
        targetCenter = zoneCenter;
        phaseText = "SHRINK IN: " + Mathf.CeilToInt(phaseWaitTimes[0]) + "s";
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        phaseTime += Time.deltaTime;
        uvOffset += Time.deltaTime * 0.5f;
        if (zoneMat != null) zoneMat.mainTextureOffset = new Vector2(0, uvOffset);

        if (currentPhase >= 6) return;

        float waitTime = phaseWaitTimes[currentPhase];
        float shrinkTime = phaseShrinkTimes[currentPhase];
        float targetRadius = phaseTargetRadii[currentPhase];

        if (phaseTime < waitTime)
        {
            // Waiting phase
            float remaining = waitTime - phaseTime;
            string phaseLabel = currentPhase == 0 ? "SHRINK IN" : "PHASE " + (currentPhase / 2 + 1) + " IN";
            phaseText = phaseLabel + ": " + Mathf.CeilToInt(remaining) + "s";
        }
        else if (phaseTime < waitTime + shrinkTime)
        {
            // Shrinking phase
            float t = (phaseTime - waitTime) / shrinkTime;
            t = Mathf.SmoothStep(0, 1, t);
            currentRadius = Mathf.Lerp(startRadius, targetRadius, t);
            zoneCenter = Vector3.Lerp(startCenter, targetCenter, t);
            float remaining = waitTime + shrinkTime - phaseTime;
            phaseText = "SHRINKING: " + Mathf.CeilToInt(remaining) + "s";
        }
        else
        {
            // Phase complete — advance to next
            currentRadius = targetRadius;
            zoneCenter = targetCenter;
            currentPhase++;
            phaseTime = 0f;
            startRadius = currentRadius;
            startCenter = zoneCenter;

            if (currentPhase < 6)
            {
                // Randomize next zone center — biased toward map center
                float bias = 1f - (currentPhase * 0.15f);
                float maxOffset = mapData != null ? mapData.mapRadius * 0.3f * bias : 80f * bias;
                targetCenter = mapData.mapCenter + new Vector3(
                    Random.Range(-maxOffset, maxOffset), 0,
                    Random.Range(-maxOffset, maxOffset)
                );

                if (phaseWaitTimes[currentPhase] > 0)
                    phaseText = "PHASE " + (currentPhase / 2 + 2) + " IN: " + Mathf.CeilToInt(phaseWaitTimes[currentPhase]) + "s";
                else
                    phaseText = "SHRINKING...";
            }
            else
            {
                phaseText = "FINAL ZONE";
            }
        }

        // Update zone wall visual
        if (zoneWall != null)
        {
            zoneWall.transform.position = new Vector3(zoneCenter.x, 50f, zoneCenter.z);
            zoneWall.transform.localScale = new Vector3(currentRadius * 2f, 50f, currentRadius * 2f);
        }
    }

    /// <summary>Check if a position is outside the safe zone.</summary>
    public bool IsOutsideZone(Vector3 pos)
    {
        Vector2 p = new Vector2(pos.x - zoneCenter.x, pos.z - zoneCenter.z);
        return p.magnitude > currentRadius;
    }

    /// <summary>Get zone shrink progress (0 = full size, 1 = fully shrunk).</summary>
    public float GetShrinkProgress()
    {
        float maxRadius = mapData != null ? mapData.mapRadius * 0.5f : 250f;
        return 1f - (currentRadius / maxRadius);
    }

    /// <summary>Get zone status text for HUD.</summary>
    public string GetZoneStatusText() => phaseText;

    /// <summary>Get current zone center for minimap display.</summary>
    public Vector3 GetZoneCenter() => zoneCenter;

    /// <summary>Get current zone radius for minimap display.</summary>
    public float GetZoneRadius() => currentRadius;
}


