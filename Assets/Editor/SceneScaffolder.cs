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
    // 0. FIX BUILD SETTINGS (Crucial for button transitions!)
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
    // 1. AUTO-BUILD SPLASH UI
    // ===================================================================================
    [MenuItem("BloodRing/1. Auto-Build Splash UI")]
    public static void BuildSplashUI()
    {
        GameObject canvasObj = CreateCanvas();
        VisualSplash script = CreateManager<VisualSplash>(canvasObj, "[SplashManager]");
        
        CreateBackground(canvasObj, "Assets/Resources/Art/Scenes/Splash_KeyArt.png", new Color(0,0,0));
        CreateText(canvasObj, "Title", "BLOODRING STUDIO", 60, new Vector2(0, 100), Color.red);
        
        Button btn = CreateButton(canvasObj, "TapToStartBtn", "TAP TO START", new Vector2(0, -150), new Color(0.8f, 0.2f, 0.1f));
        UnityAction action = new UnityAction(script.GoToLogin);
        UnityEventTools.AddPersistentListener(btn.onClick, action);
        
        Selection.activeGameObject = canvasObj;
        Debug.Log("Splash Scene Auto-Generated!");
    }

    // ===================================================================================
    // 2. AUTO-BUILD LOGIN UI
    // ===================================================================================
    [MenuItem("BloodRing/2. Auto-Build Login UI")]
    public static void BuildLoginUI()
    {
        GameObject canvasObj = CreateCanvas();
        VisualLogin script = CreateManager<VisualLogin>(canvasObj, "[LoginManager]");
        
        CreateBackground(canvasObj, "Assets/Resources/Art/Scenes/Splash_KeyArt.png", new Color(0.1f, 0.1f, 0.15f));
        CreateText(canvasObj, "Title", "LOGIN", 50, new Vector2(0, 200), Color.white);

        TMP_InputField inputField = CreateInputField(canvasObj, new Vector2(0, 50));
        Button btn = CreateButton(canvasObj, "PlayBtn", "PLAY AS GUEST", new Vector2(0, -60), new Color(0.85f, 0.15f, 0.1f));
        TextMeshProUGUI statusTmp = CreateText(canvasObj, "StatusText", "", 26, new Vector2(0, 150), Color.yellow);

        script.usernameInput = inputField;
        script.statusText = statusTmp;

        UnityAction action = new UnityAction(script.OnLoginButtonPressed);
        UnityEventTools.AddPersistentListener(btn.onClick, action);
        
        Selection.activeGameObject = canvasObj;
        Debug.Log("Login Scene Auto-Generated!");
    }

    // ===================================================================================
    // 3. AUTO-BUILD MAIN MENU UI
    // ===================================================================================
    [MenuItem("BloodRing/3. Auto-Build Main Menu UI")]
    public static void BuildMainMenuUI()
    {
        GameObject canvasObj = CreateCanvas();
        VisualMainMenu script = CreateManager<VisualMainMenu>(canvasObj, "[MainMenuManager]");
        
        // Use MainLobby background
        CreateBackground(canvasObj, "Assets/Resources/Art/Scenes/MainLobby.png", new Color(0.2f, 0.2f, 0.2f));
        
        // Welcome Text
        TextMeshProUGUI nameTmp = CreateText(canvasObj, "PlayerNameText", "Welcome, Player!", 36, new Vector2(-400, 450), Color.yellow);
        
        // Buttons
        Button playBtn = CreateButton(canvasObj, "FindMatchBtn", "FIND MATCH", new Vector2(400, -300), new Color(0.9f, 0.4f, 0.1f));
        Button logoutBtn = CreateButton(canvasObj, "LogoutBtn", "LOGOUT", new Vector2(-400, -300), new Color(0.4f, 0.4f, 0.4f));

        script.playerNameText = nameTmp;

        UnityAction playAction = new UnityAction(script.PlayGame);
        UnityEventTools.AddPersistentListener(playBtn.onClick, playAction);
        
        UnityAction logoutAction = new UnityAction(script.Logout);
        UnityEventTools.AddPersistentListener(logoutBtn.onClick, logoutAction);

        Selection.activeGameObject = canvasObj;
        Debug.Log("Main Menu Auto-Generated! DJ Neon will spawn when you press Play.");
    }

    // ===================================================================================
    // HELPERS
    // ===================================================================================
    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
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

    private static TextMeshProUGUI CreateText(GameObject canvas, string name, string text, int size, Vector2 pos, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(600, 100);
        return tmp;
    }

    private static Button CreateButton(GameObject canvas, string name, string text, Vector2 pos, Color color)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(canvas.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        Button btn = btnObj.AddComponent<Button>();
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300, 80);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = Color.white;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

        return btn;
    }

    private static TMP_InputField CreateInputField(GameObject canvas, Vector2 pos)
    {
        GameObject bgObj = new GameObject("InputField");
        bgObj.transform.SetParent(canvas.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
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
}
