using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;
    public static SceneLoader Instance { get { if (instance == null) { GameObject go = new GameObject("[SceneLoader]"); instance = go.AddComponent<SceneLoader>(); DontDestroyOnLoad(go); instance.InitializeUI(); } return instance; } }

    private GameObject loadingCanvasGo; private Canvas loadingCanvas; private Image progressBarFill; private Text loadingText; private Text tipText;

    private string[] apexTips = new string[]
    {
        "TIP: Keep moving while looting to avoid becoming an easy sniper target!",
        "TIP: Aim slightly above your target's chest to secure critical headshots.",
        "TIP: Stay near the shrinking zone edge during Phase 3 to avoid being flanked.",
        "TIP: Use the DJ Neon or Pulse active skills during intense squad firefights.",
        "TIP: Check the Lucky Spin daily for a chance to win legendary weapon skins!",
        "TIP: Equip a silencer attachment to keep your position hidden on the minimap."
    };

    private void Awake() { if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); InitializeUI(); } else if (instance != this) { Destroy(gameObject); } }

    private void InitializeUI()
    {
        if (loadingCanvasGo != null) return;
        loadingCanvasGo = new GameObject("LoadingCanvas"); DontDestroyOnLoad(loadingCanvasGo);
        loadingCanvas = loadingCanvasGo.AddComponent<Canvas>(); loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay; loadingCanvas.sortingOrder = 9999;
        CanvasScaler scaler = loadingCanvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); loadingCanvasGo.AddComponent<GraphicRaycaster>();

        GameObject bgGo = new GameObject("Background"); bgGo.transform.SetParent(loadingCanvasGo.transform, false); Image bg = bgGo.AddComponent<Image>(); bg.color = new Color(0.05f, 0.05f, 0.05f, 1f); RectTransform bgRect = bgGo.GetComponent<RectTransform>(); bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;

        GameObject textGo = new GameObject("LoadingText"); textGo.transform.SetParent(loadingCanvasGo.transform, false); loadingText = textGo.AddComponent<Text>(); loadingText.text = "LOADING..."; loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); loadingText.fontSize = 48; loadingText.fontStyle = FontStyle.Bold; loadingText.color = Color.red; loadingText.alignment = TextAnchor.MiddleCenter; RectTransform textRect = textGo.GetComponent<RectTransform>(); textRect.anchoredPosition = new Vector2(0, 80); textRect.sizeDelta = new Vector2(400, 100);

        GameObject tipGo = new GameObject("TipText"); tipGo.transform.SetParent(loadingCanvasGo.transform, false); tipText = tipGo.AddComponent<Text>(); tipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tipText.fontSize = 26; tipText.color = Color.yellow; tipText.alignment = TextAnchor.MiddleCenter; RectTransform tipRect = tipGo.GetComponent<RectTransform>(); tipRect.anchoredPosition = new Vector2(0, -120); tipRect.sizeDelta = new Vector2(1000, 60);

        GameObject progBgGo = new GameObject("ProgressBarBG"); progBgGo.transform.SetParent(loadingCanvasGo.transform, false); Image progBg = progBgGo.AddComponent<Image>(); progBg.color = Color.gray; RectTransform progBgRect = progBgGo.GetComponent<RectTransform>(); progBgRect.anchoredPosition = new Vector2(0, -30); progBgRect.sizeDelta = new Vector2(600, 40);
        GameObject progFillGo = new GameObject("ProgressBarFill"); progFillGo.transform.SetParent(progBgGo.transform, false); progressBarFill = progFillGo.AddComponent<Image>(); progressBarFill.color = Color.red; RectTransform progFillRect = progFillGo.GetComponent<RectTransform>(); progFillRect.anchorMin = new Vector2(0, 0); progFillRect.anchorMax = new Vector2(0, 1); progFillRect.sizeDelta = new Vector2(0, 0); progFillRect.pivot = new Vector2(0, 0.5f);

        loadingCanvasGo.SetActive(false);
    }

    public void LoadScene(string sceneName) { StartCoroutine(LoadSceneCoroutine(sceneName)); }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        loadingCanvasGo.SetActive(true); progressBarFill.rectTransform.anchorMax = new Vector2(0, 1);
        loadingText.text = "LOADING " + sceneName.ToUpper() + "...";
        tipText.text = apexTips[Random.Range(0, apexTips.Length)];

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName); op.allowSceneActivation = false;
        while (op.progress < 0.9f) { float progress = Mathf.Clamp01(op.progress / 0.9f); progressBarFill.rectTransform.anchorMax = new Vector2(progress, 1); yield return null; }
        progressBarFill.rectTransform.anchorMax = new Vector2(1, 1); yield return new WaitForSeconds(0.3f); op.allowSceneActivation = true;
        
        while (!op.isDone) yield return null;
        
        loadingCanvasGo.SetActive(false);
    }
}


