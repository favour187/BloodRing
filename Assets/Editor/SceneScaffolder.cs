using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SceneScaffolder : EditorWindow
{
    // ===================================================================================
    // 0. FIX BUILD SETTINGS
    // ===================================================================================
    [MenuItem("BloodRing/0. FIX: Add Scenes To Build Settings")]
    public static void FixBuildSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach(string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("Build Settings fixed! All buttons will now transition perfectly.");
    }

    // ===================================================================================
    // 1. AUTO-BUILD SPLASH UI (FREE FIRE STYLE)
    // ===================================================================================
    [MenuItem("BloodRing/1. Auto-Build Splash UI")]
    public static void BuildSplashUI()
    {
        GameObject canvasObj = CreateCanvas();
        VisualSplash script = CreateManager<VisualSplash>(canvasObj, "[SplashManager]");
        
        CreateBackground(canvasObj, "Assets/Resources/Art/Scenes/Splash_KeyArt.png", new Color(0,0,0));
        
        // Massive glowing title
        TextMeshProUGUI title = CreateText(canvasObj, "Title", "BLOODRING", 120, new Vector2(0, 150), new Color(0.9f, 0.1f, 0.1f));
        title.fontStyle = FontStyles.Bold | FontStyles.Italic;
        AddShadow(title.gameObject, new Color(0,0,0,0.8f), new Vector2(4, -4));

        TextMeshProUGUI subTitle = CreateText(canvasObj, "SubTitle", "APEX ROYALE", 45, new Vector2(0, 50), new Color(1f, 0.8f, 0f));
        subTitle.fontStyle = FontStyles.Bold;

        // Glowing "TAP TO START"
        Button btn = CreateFFButton(canvasObj, "TapToStartBtn", "TAP TO BEGIN", new Vector2(0, -250), new Color(1f, 1f, 1f, 0f), Color.white, 40);
        
        UnityAction action = new UnityAction(script.GoToLogin);
        UnityEventTools.AddPersistentListener(btn.onClick, action);
        
        Selection.activeGameObject = canvasObj;
        Debug.Log("Splash Scene Auto-Generated!");
    }

    // ===================================================================================
    // 2. AUTO-BUILD LOGIN UI (FREE FIRE STYLE)
    // ===================================================================================
    [MenuItem("BloodRing/2. Auto-Build Login UI")]
    public static void BuildLoginUI()
    {
        GameObject canvasObj = CreateCanvas();
        VisualLogin script = CreateManager<VisualLogin>(canvasObj, "[LoginManager]");
        
        CreateBackground(canvasObj, "Assets/Resources/Art/Scenes/Splash_KeyArt.png", new Color(0.1f, 0.1f, 0.15f));
        
        // Dark transparent login panel
        GameObject panel = CreatePanel(canvasObj, "LoginPanel", new Vector2(0, -50), new Vector2(500, 450), new Color(0, 0, 0, 0.75f));

        TextMeshProUGUI title = CreateText(panel, "Title", "GUEST LOGIN", 40, new Vector2(0, 150), Color.white);
        title.fontStyle = FontStyles.Bold;

        TMP_InputField inputField = CreateInputField(panel, new Vector2(0, 30));
        
        // Iconic Yellow Action Button
        Button btn = CreateFFButton(panel, "PlayBtn", "LOGIN", new Vector2(0, -80), new Color(0.96f, 0.76f, 0.11f), Color.black, 32, new Vector2(350, 70));
        
        TextMeshProUGUI statusTmp = CreateText(panel, "StatusText", "", 24, new Vector2(0, -170), Color.yellow);

        script.usernameInput = inputField;
        script.statusText = statusTmp;

        UnityAction action = new UnityAction(script.OnLoginButtonPressed);
        UnityEventTools.AddPersistentListener(btn.onClick, action);
        
        Selection.activeGameObject = canvasObj;
        Debug.Log("Login Scene Auto-Generated!");
    }

    // ===================================================================================
    // 3. AUTO-BUILD MAIN MENU UI (FREE FIRE STYLE)
    // ===================================================================================
    [MenuItem("BloodRing/3. Auto-Build Main Menu UI")]
    public static void BuildMainMenuUI()
    {
        GameObject canvasObj = CreateCanvas();
        VisualMainMenu script = CreateManager<VisualMainMenu>(canvasObj, "[MainMenuManager]");
        
        CreateBackground(canvasObj, "Assets/Resources/Art/Scenes/MainLobby.png", new Color(0.2f, 0.2f, 0.2f));
        
        // Top Info Bar (Dark strip)
        GameObject topBar = CreatePanel(canvasObj, "TopBar", new Vector2(0, 490), new Vector2(1920, 100), new Color(0, 0, 0, 0.5f));
        
        // Player Profile info (Top Left)
        TextMeshProUGUI nameTmp = CreateText(topBar, "PlayerNameText", "PLAYER NAME", 32, new Vector2(-700, 0), Color.white);
        nameTmp.alignment = TextAlignmentOptions.Left;
        
        // Currencies (Top Right)
        CreateText(topBar, "CoinsText", "🪙 5000", 30, new Vector2(600, 0), new Color(1f, 0.8f, 0f));
        CreateText(topBar, "DiamondsText", "💎 250", 30, new Vector2(800, 0), new Color(0f, 0.8f, 1f));

        // Bottom Right: MASSIVE YELLOW START BUTTON
        Button playBtn = CreateFFButton(canvasObj, "StartMatchBtn", "START", new Vector2(700, -380), new Color(0.96f, 0.76f, 0.11f), Color.black, 50, new Vector2(350, 120));
        
        // Bottom Left Icons (Store, Weapons)
        Button storeBtn = CreateFFButton(canvasObj, "StoreBtn", "STORE", new Vector2(-800, -380), new Color(0, 0, 0, 0.6f), Color.white, 24, new Vector2(180, 80));
        Button weaponsBtn = CreateFFButton(canvasObj, "WeaponsBtn", "WEAPONS", new Vector2(-600, -380), new Color(0, 0, 0, 0.6f), Color.white, 24, new Vector2(180, 80));

        // Settings / Logout (Top Right corner below bar)
        Button logoutBtn = CreateFFButton(canvasObj, "LogoutBtn", "LOGOUT", new Vector2(850, 380), new Color(0, 0, 0, 0.6f), Color.white, 20, new Vector2(120, 50));

        script.playerNameText = nameTmp;

        UnityAction playAction = new UnityAction(script.PlayGame);
        UnityEventTools.AddPersistentListener(playBtn.onClick, playAction);
        
        UnityAction logoutAction = new UnityAction(script.Logout);
        UnityEventTools.AddPersistentListener(logoutBtn.onClick, logoutAction);

        Selection.activeGameObject = canvasObj;
        Debug.Log("FreeFire-Style Main Menu Auto-Generated!");
    }

    // ===================================================================================
    // HELPERS FOR FREE FIRE AESTHETIC
    // ===================================================================================
    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
        return canvasObj;
    }

    private static T CreateManager<T>(GameObject canvas, string name) where T : MonoBehaviour
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(canvas.transform, false);
        return obj.AddComponent<T>();
    }

    private static void CreateBackground(GameObject canvas, string path, Color fallback)
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);
        RawImage bgImg = bgObj.AddComponent<RawImage>();
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) bgImg.texture = tex; else bgImg.color = fallback;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
    }

    private static GameObject CreatePanel(GameObject parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return obj;
    }

    private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, int size, Vector2 pos, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(800, 150);
        return tmp;
    }

    private static Button CreateFFButton(GameObject parent, string name, string text, Vector2 pos, Color bgColor, Color txtColor, int fontSize, Vector2 size = default)
    {
        if (size == default) size = new Vector2(300, 80);

        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        // Add subtle shadow to button frame
        Shadow shadow = btnObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0,0,0,0.5f);
        shadow.effectDistance = new Vector2(3, -3);

        Button btn = btnObj.AddComponent<Button>();
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = txtColor;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

        return btn;
    }

    private static TMP_InputField CreateInputField(GameObject parent, Vector2 pos)
    {
        GameObject bgObj = new GameObject("InputFieldBase");
        bgObj.transform.SetParent(parent.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        
        // Yellow underline accent
        GameObject underline = new GameObject("Underline");
        underline.transform.SetParent(bgObj.transform, false);
        Image lineImg = underline.AddComponent<Image>();
        lineImg.color = new Color(0.96f, 0.76f, 0.11f);
        RectTransform lineRect = underline.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 0); lineRect.anchorMax = new Vector2(1, 0);
        lineRect.offsetMin = new Vector2(0, 0); lineRect.offsetMax = new Vector2(0, 4); // 4px thick line

        RectTransform rect = bgObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 70);

        TMP_InputField input = bgObj.AddComponent<TMP_InputField>();
        input.characterLimit = 20;

        GameObject textObj = new GameObject("Text Area");
        textObj.transform.SetParent(bgObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.color = Color.white;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(10, 0); txtRect.offsetMax = new Vector2(-10, 0);
        input.textComponent = tmp;

        return input;
    }

    private static void AddShadow(GameObject obj, Color color, Vector2 dist)
    {
        Shadow s = obj.AddComponent<Shadow>();
        s.effectColor = color;
        s.effectDistance = dist;
    }
}
