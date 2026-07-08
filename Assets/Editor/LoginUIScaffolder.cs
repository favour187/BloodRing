using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEngine.EventSystems;

public class LoginUIScaffolder : EditorWindow
{
    [MenuItem("BloodRing/1. Auto-Build Login UI")]
    public static void BuildLoginScreen()
    {
        // 1. Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Create Event System if not exists
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // 3. Create Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RawImage bgImg = bgObj.AddComponent<RawImage>();
        Texture2D splashTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/Scenes/Splash_KeyArt.png");
        if (splashTex != null) bgImg.texture = splashTex;
        else bgImg.color = new Color(0.1f, 0.1f, 0.15f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        // 4. Create Login Manager
        GameObject managerObj = new GameObject("[VisualLoginManager]");
        managerObj.transform.SetParent(canvasObj.transform, false);
        VisualLogin loginScript = managerObj.AddComponent<VisualLogin>();

        // 5. Create Username Input Field (TMP)
        GameObject inputBgObj = new GameObject("UsernameInput");
        inputBgObj.transform.SetParent(canvasObj.transform, false);
        Image inputBg = inputBgObj.AddComponent<Image>();
        inputBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        RectTransform inputRect = inputBgObj.GetComponent<RectTransform>();
        inputRect.anchoredPosition = new Vector2(0, 50);
        inputRect.sizeDelta = new Vector2(400, 70);

        TMP_InputField inputField = inputBgObj.AddComponent<TMP_InputField>();
        inputField.characterLimit = 20;

        // 5a. Text Component for Input
        GameObject textObj = new GameObject("Text Area");
        textObj.transform.SetParent(inputBgObj.transform, false);
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.color = Color.white;
        tmpText.fontSize = 28;
        tmpText.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0); textRect.offsetMax = new Vector2(-10, 0);
        inputField.textComponent = tmpText;

        // 5b. Placeholder Component
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputBgObj.transform, false);
        TextMeshProUGUI placeholderTxt = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTxt.text = "Enter username";
        placeholderTxt.color = new Color(1, 1, 1, 0.4f);
        placeholderTxt.fontSize = 28;
        placeholderTxt.alignment = TextAlignmentOptions.Center;
        RectTransform placeRect = placeholderObj.GetComponent<RectTransform>();
        placeRect.anchorMin = Vector2.zero; placeRect.anchorMax = Vector2.one;
        placeRect.offsetMin = new Vector2(10, 0); placeRect.offsetMax = new Vector2(-10, 0);
        inputField.placeholder = placeholderTxt;

        // 6. Create Login Button
        GameObject btnObj = new GameObject("PlayButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.15f, 0.1f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -60);
        btnRect.sizeDelta = new Vector2(300, 100);

        GameObject btnTextObj = new GameObject("Text (TMP)");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "PLAY AS GUEST";
        btnTmp.color = Color.white;
        btnTmp.fontSize = 32;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero; btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero; btnTextRect.offsetMax = Vector2.zero;

        // 7. Create Status Text
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.text = "";
        statusTmp.color = Color.yellow;
        statusTmp.fontSize = 26;
        statusTmp.alignment = TextAlignmentOptions.Center;
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 150);
        statusRect.sizeDelta = new Vector2(600, 50);

        // 8. Wire Everything Up Automatically!
        loginScript.usernameInput = inputField;
        loginScript.statusText = statusTmp;

        UnityAction action = new UnityAction(loginScript.OnLoginButtonPressed);
        UnityEventTools.AddPersistentListener(btn.onClick, action);

        Selection.activeGameObject = managerObj;
        Debug.Log("Login Scene UI successfully Auto-Generated!");
    }
}
