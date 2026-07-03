using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// UNIQUE FEATURE: Kill-Cam / Replay System.
/// Records the last N seconds of gameplay transforms and replays them in slow-motion
/// when the player dies, showing who killed them and from where.
/// Unique to BloodRing — a proper kill-cam replay.
/// </summary>
public class ReplaySystem : MonoBehaviour
{
    public static ReplaySystem Instance;

    [System.Serializable]
    public class ReplayFrame
    {
        public float timestamp;
        public Vector3 playerPos;
        public Quaternion playerRot;
        public Vector3 killerPos;
        public Quaternion killerRot;
        public string killerName;
        public string weaponUsed;
    }

    private List<ReplayFrame> replayBuffer = new List<ReplayFrame>();
    private int maxFrames = 300; // ~5 seconds at 60 fps
    private float recordInterval = 1f / 60f;
    private float lastRecordTime = 0f;
    private bool isRecording = true;
    private bool isReplaying = false;
    private int replayIndex = 0;
    private float replaySpeed = 0.3f;
    private float replayTimer = 0f;

    private Transform trackedPlayer;
    private Camera replayCam;
    private GameObject replayUI;
    private UnityEngine.UI.Text replayText;
    private UnityEngine.UI.Text killInfoText;

    private string lastKillerName = "";
    private string lastWeaponName = "";
    private Vector3 lastKillerPos = Vector3.zero;

    private void Awake() { Instance = this; }

    public void SetTrackedPlayer(Transform player) { trackedPlayer = player; }

    public void RecordKillerInfo(string killerName, string weapon, Vector3 killerPos)
    {
        lastKillerName = killerName;
        lastWeaponName = weapon;
        lastKillerPos = killerPos;
    }

    private void Update()
    {
        if (isRecording && trackedPlayer != null && Time.time - lastRecordTime >= recordInterval)
        {
            lastRecordTime = Time.time;
            ReplayFrame frame = new ReplayFrame
            {
                timestamp = Time.time,
                playerPos = trackedPlayer.position,
                playerRot = trackedPlayer.rotation,
                killerPos = lastKillerPos,
                killerRot = Quaternion.identity,
                killerName = lastKillerName,
                weaponUsed = lastWeaponName
            };
            replayBuffer.Add(frame);
            if (replayBuffer.Count > maxFrames) replayBuffer.RemoveAt(0);
        }

        if (isReplaying) UpdateReplay();
    }

    public void TriggerKillCam(string killerName, string weapon, Vector3 killerPos)
    {
        if (replayBuffer.Count < 30) return; // Need at least 0.5s of data
        isRecording = false;
        lastKillerName = killerName;
        lastWeaponName = weapon;
        lastKillerPos = killerPos;
        StartCoroutine(PlayKillCam());
    }

    private IEnumerator PlayKillCam()
    {
        yield return new WaitForSeconds(0.5f);

        // Create replay camera
        GameObject camGo = new GameObject("ReplayCam");
        replayCam = camGo.AddComponent<Camera>();
        replayCam.depth = 100;
        replayCam.clearFlags = CameraClearFlags.SolidColor;
        replayCam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);

        // Create replay UI overlay
        replayUI = new GameObject("ReplayUI");
        Canvas canvas = replayUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = replayUI.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        // "KILL CAM" header
        GameObject headerGo = new GameObject("Header"); headerGo.transform.SetParent(replayUI.transform, false);
        replayText = headerGo.AddComponent<UnityEngine.UI.Text>();
        replayText.text = "◄◄ KILL CAM ►►";
        replayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        replayText.fontSize = 42; replayText.fontStyle = FontStyle.Bold;
        replayText.color = Color.red; replayText.alignment = TextAnchor.UpperCenter;
        RectTransform hrect = headerGo.GetComponent<RectTransform>();
        hrect.anchoredPosition = new Vector2(0, -40); hrect.sizeDelta = new Vector2(600, 80);

        // Kill info
        GameObject infoGo = new GameObject("KillInfo"); infoGo.transform.SetParent(replayUI.transform, false);
        killInfoText = infoGo.AddComponent<UnityEngine.UI.Text>();
        killInfoText.text = lastKillerName + " eliminated you with " + lastWeaponName;
        killInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        killInfoText.fontSize = 28; killInfoText.color = Color.yellow;
        killInfoText.alignment = TextAnchor.UpperCenter;
        RectTransform irect = infoGo.GetComponent<RectTransform>();
        irect.anchoredPosition = new Vector2(0, -100); irect.sizeDelta = new Vector2(800, 50);

        // Slow-motion vignette border
        GameObject border = new GameObject("Border"); border.transform.SetParent(replayUI.transform, false);
        UnityEngine.UI.Image borderImg = border.AddComponent<UnityEngine.UI.Image>();
        borderImg.color = new Color(1f, 0f, 0f, 0.15f);
        RectTransform brect = border.GetComponent<RectTransform>();
        brect.anchorMin = Vector2.zero; brect.anchorMax = Vector2.one; brect.sizeDelta = Vector2.zero;

        // Play the replay
        isReplaying = true;
        replayIndex = Mathf.Max(0, replayBuffer.Count - 180); // Last 3 seconds
        Time.timeScale = replaySpeed;

        // Position camera behind killer looking at player
        if (replayBuffer.Count > 0)
        {
            ReplayFrame lastFrame = replayBuffer[replayBuffer.Count - 1];
            Vector3 camPos = lastKillerPos + new Vector3(0, 3f, -5f);
            replayCam.transform.position = camPos;
            replayCam.transform.LookAt(lastFrame.playerPos + Vector3.up);
        }

        yield return new WaitForSecondsRealtime(5f); // 5 real seconds of replay

        // Clean up
        Time.timeScale = 1f;
        isReplaying = false;
        if (replayCam != null) Destroy(replayCam.gameObject);
        if (replayUI != null) Destroy(replayUI);
    }

    private void UpdateReplay()
    {
        if (replayIndex >= replayBuffer.Count) return;
        replayTimer += Time.unscaledDeltaTime;
        if (replayTimer >= recordInterval / replaySpeed)
        {
            replayTimer = 0f;
            ReplayFrame frame = replayBuffer[replayIndex];

            if (replayCam != null)
            {
                // Orbit camera around the death event
                float angle = (replayIndex - (replayBuffer.Count - 180)) * 0.5f;
                Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 8f, 4f, Mathf.Cos(angle * Mathf.Deg2Rad) * 8f);
                replayCam.transform.position = Vector3.Lerp(replayCam.transform.position, frame.playerPos + offset, 0.1f);
                replayCam.transform.LookAt(frame.playerPos + Vector3.up);
            }

            replayIndex++;
        }
    }

    public bool IsReplaying() { return isReplaying; }
}


