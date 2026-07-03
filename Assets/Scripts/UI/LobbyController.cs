using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class LobbyController : MonoBehaviour
{
    private Text playerCountText; private Text timerText; private Text playerListText; private Text joinCodeText;
    private int currentPlayers = 1; private float countdown = 10f; private bool countdownStarted = false; private float beepTimer = 1f;

    public static string[] botNames = new string[] { "Chinedu", "Ngozi", "Emeka", "Fatima", "Ade", "Binta", "Obi", "Amaka", "Kelechi", "Yusuf", "Chinwe", "Babajide", "Chika", "Dayo", "Funke", "Gbenga", "Ibrahim", "Nkechi", "Oluwaseun", "Simi", "Tunde", "Uzoma", "Chijioke", "Ezinne", "Idris", "Kemi", "Okonkwo", "Titilayo", "Abubakar", "Adaora" };

    private List<string> joinedNames = new List<string>();

    private void Start()
    {
        Camera cam = Camera.main; if (cam == null) { GameObject camGo = new GameObject("Main Camera"); cam = camGo.AddComponent<Camera>(); cam.tag = "MainCamera"; } cam.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 1f); cam.clearFlags = CameraClearFlags.SolidColor;

        GameObject canvasGo = new GameObject("LobbyCanvas"); Canvas canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); canvasGo.AddComponent<GraphicRaycaster>();

        GameObject mapGo = new GameObject("MapNameText"); mapGo.transform.SetParent(canvasGo.transform, false); Text mapText = mapGo.AddComponent<Text>(); mapText.text = "MAP: BLOODRING ISLAND"; mapText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); mapText.fontSize = 32; mapText.fontStyle = FontStyle.Bold; mapText.color = Color.yellow; mapText.alignment = TextAnchor.UpperLeft; RectTransform mapRect = mapGo.GetComponent<RectTransform>(); mapRect.anchorMin = new Vector2(0, 1); mapRect.anchorMax = new Vector2(0, 1); mapRect.anchoredPosition = new Vector2(220, -40); mapRect.sizeDelta = new Vector2(400, 40);

        GameObject codeGo = new GameObject("JoinCodeText"); codeGo.transform.SetParent(canvasGo.transform, false); joinCodeText = codeGo.AddComponent<Text>(); joinCodeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); joinCodeText.fontSize = 32; joinCodeText.color = Color.cyan; joinCodeText.alignment = TextAnchor.UpperCenter; RectTransform codeRect = codeGo.GetComponent<RectTransform>(); codeRect.anchorMin = new Vector2(0.5f, 1); codeRect.anchorMax = new Vector2(0.5f, 1); codeRect.anchoredPosition = new Vector2(0, -40); codeRect.sizeDelta = new Vector2(400, 40);
        if (NetworkController.Instance != null && NetworkController.Instance.isOnlineMode) { joinCodeText.text = "JOIN CODE: " + NetworkController.Instance.joinCode; } else { joinCodeText.text = "SOLO MATCH (BOTS)"; }

        GameObject countGo = new GameObject("PlayerCountText"); countGo.transform.SetParent(canvasGo.transform, false); playerCountText = countGo.AddComponent<Text>(); playerCountText.text = "PLAYERS: 1/20"; playerCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); playerCountText.fontSize = 32; playerCountText.fontStyle = FontStyle.Bold; playerCountText.color = Color.white; playerCountText.alignment = TextAnchor.UpperRight; RectTransform countRect = countGo.GetComponent<RectTransform>(); countRect.anchorMin = new Vector2(1, 1); countRect.anchorMax = new Vector2(1, 1); countRect.anchoredPosition = new Vector2(-200, -40); countRect.sizeDelta = new Vector2(350, 40);

        GameObject timerGo = new GameObject("TimerText"); timerGo.transform.SetParent(canvasGo.transform, false); timerText = timerGo.AddComponent<Text>(); timerText.text = "WAITING FOR PLAYERS..."; timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); timerText.fontSize = 48; timerText.fontStyle = FontStyle.Bold; timerText.color = Color.red; timerText.alignment = TextAnchor.MiddleCenter; RectTransform timerRect = timerGo.GetComponent<RectTransform>(); timerRect.anchoredPosition = new Vector2(0, 250); timerRect.sizeDelta = new Vector2(600, 80);

        GameObject listBgGo = new GameObject("PlayerListBG"); listBgGo.transform.SetParent(canvasGo.transform, false); Image listBg = listBgGo.AddComponent<Image>(); listBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f); RectTransform listBgRect = listBgGo.GetComponent<RectTransform>(); listBgRect.anchoredPosition = new Vector2(0, -30); listBgRect.sizeDelta = new Vector2(800, 450);
        GameObject listTextGo = new GameObject("PlayerListText"); listTextGo.transform.SetParent(listBgGo.transform, false); playerListText = listTextGo.AddComponent<Text>(); playerListText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); playerListText.fontSize = 22; playerListText.color = Color.white; playerListText.alignment = TextAnchor.UpperLeft; playerListText.horizontalOverflow = HorizontalWrapMode.Wrap; RectTransform listTextRect = listTextGo.GetComponent<RectTransform>(); listTextRect.anchorMin = Vector2.zero; listTextRect.anchorMax = Vector2.one; listTextRect.offsetMin = new Vector2(20, 20); listTextRect.offsetMax = new Vector2(-20, -20);

        RefreshPlayerList();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) { StartCoroutine(SpawnBotsRoutine()); }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient) { timerText.text = "WAITING FOR HOST TO START..."; }
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && countdownStarted)
        {
            countdown -= Time.deltaTime; beepTimer -= Time.deltaTime;
            timerText.text = "MATCH STARTS IN: " + Mathf.CeilToInt(countdown) + "s";
            
            if (beepTimer <= 0)
            {
                beepTimer = 1f; bool isFinal = countdown <= 3f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayBeep(isFinal);
            }

            if (countdown <= 0) { countdownStarted = false; GameManager.Instance.ChangeState(GameState.Game); }
        }
    }

    private void RefreshPlayerList() { joinedNames.Clear(); if (NetworkController.Instance != null) { foreach (var kvp in NetworkController.Instance.GetAllPlayerNicknames()) { string suffix = (NetworkManager.Singleton != null && kvp.Key == NetworkManager.Singleton.LocalClientId) ? " (You)" : ""; joinedNames.Add(kvp.Value + suffix); } } if (joinedNames.Count == 0) joinedNames.Add(PlayerPrefs.GetString("PlayerNickname", "GuestPlayer") + " (You)"); currentPlayers = joinedNames.Count; playerCountText.text = "PLAYERS: " + currentPlayers + "/20"; UpdatePlayerListUI(); }

    private IEnumerator SpawnBotsRoutine()
    {
        List<string> availableBots = new List<string>(botNames); for (int i = 0; i < availableBots.Count; i++) { string temp = availableBots[i]; int r = Random.Range(i, availableBots.Count); availableBots[i] = availableBots[r]; availableBots[r] = temp; }
        int botIndex = 0;
        while (currentPlayers < 20 && botIndex < availableBots.Count)
        {
            yield return new WaitForSeconds(0.5f); string newBot = availableBots[botIndex] + " (Bot)"; joinedNames.Add(newBot); botIndex++; currentPlayers++; playerCountText.text = "PLAYERS: " + currentPlayers + "/20";
            
            // Typewriter effect coroutine for new names
            StartCoroutine(TypewriterUpdateUI());

            if (currentPlayers >= 20 && !countdownStarted) { countdownStarted = true; countdown = 10f; beepTimer = 1f; }
        }
    }

    private IEnumerator TypewriterUpdateUI()
    {
        string fullText = ""; for (int i = 0; i < joinedNames.Count; i++) { fullText += (i + 1) + ". " + joinedNames[i] + "\t\t"; if ((i + 1) % 3 == 0) fullText += "\n"; }
        string curText = playerListText.text; int startLen = curText.Length; int targetLen = fullText.Length;
        for (int i = startLen; i <= targetLen; i++) { playerListText.text = fullText.Substring(0, i); yield return new WaitForSeconds(0.01f); }
    }

    private void UpdatePlayerListUI() { string text = ""; for (int i = 0; i < joinedNames.Count; i++) { text += (i + 1) + ". " + joinedNames[i] + "\t\t"; if ((i + 1) % 3 == 0) text += "\n"; } playerListText.text = text; }
}


