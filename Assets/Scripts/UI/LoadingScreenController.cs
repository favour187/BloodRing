using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles the visual transition between scenes.
/// Displays a loading bar and random game tips.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    private Image progressBarFill;
    private Text tipsText;
    private string[] gameTips = {
        "Stay inside the safe zone to avoid damage!",
        "Find high-tier loot in high-risk areas.",
        "Use cover effectively during firefights.",
        "Keep an eye on your teammates' health.",
        "The Airdrop contains the most powerful weapons!",
        "Practice your aim in the Training Ground."
    };

    private void Start()
    {
        GameObject canvasGo = new GameObject("LoadingCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background
        GameObject bgGo = new GameObject("Background"); bgGo.transform.SetParent(canvasGo.transform, false);
        Image bg = bgGo.AddComponent<Image>(); bg.color = new Color(0.05f, 0.05f, 0.1f, 1f);
        RectTransform bgR = bgGo.GetComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one; bgR.sizeDelta = Vector2.zero;

        // Loading Bar BG
        GameObject barBGGo = new GameObject("BarBG"); barBGGo.transform.SetParent(canvasGo.transform, false);
        Image barBG = barBGGo.AddComponent<Image>(); barBG.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        RectTransform barBGR = barBGGo.GetComponent<RectTransform>();
        barBGR.anchorMin = new Vector2(0.5f, 0.4f); barBGR.anchorMax = new Vector2(0.5f, 0.4f);
        barBGR.anchoredPosition = Vector2.zero; barBGR.sizeDelta = new Vector2(400, 20);

        // Loading Bar Fill
        GameObject fillGo = new GameObject("Fill"); fillGo.transform.SetParent(barBGGo.transform, false);
        progressBarFill = fillGo.AddComponent<Image>(); progressBarFill.color = UIBuilder.COL_GOLD;
        RectTransform fillR = fillGo.GetComponent<RectTransform>();
        fillR.anchorMin = Vector2.zero; fillR.anchorMax = new Vector2(0, 1);
        fillR.sizeDelta = Vector2.zero;

        // Tips Text
        GameObject tipsGo = new GameObject("TipsText"); tipsGo.transform.SetParent(canvasGo.transform, false);
        tipsText = tipsGo.AddComponent<Text>();
        tipsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tipsText.fontSize = 20; tipsText.color = Color.white; tipsText.alignment = TextAnchor.MiddleCenter;
        RectTransform tipsR = tipsGo.GetComponent<RectTransform>();
        tipsR.anchorMin = new Vector2(0.5f, 0.3f); tipsR.anchorMax = new Vector2(0.5f, 0.3f);
        tipsR.anchoredPosition = Vector2.zero; tipsR.sizeDelta = new Vector2(600, 60);

        tipsText.text = gameTips[Random.Range(0, gameTips.Length)];

        StartCoroutine(SimulateLoading());
    }

    private IEnumerator SimulateLoading()
    {
        float progress = 0;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.5f; // Load in 2 seconds
            progressBarFill.fillAmount = progress;
            yield return null;
        }

        // Move to Main Lobby or Match
        SceneManager.LoadScene("MainLobby");
    }
}
