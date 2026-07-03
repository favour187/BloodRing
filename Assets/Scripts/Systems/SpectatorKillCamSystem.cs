using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Blood Ring Apex Royale 3D — Death Recap & Spectator Kill-Cam System.
/// Displays automated spectator death recap showing killer identity, weapon loadout,
/// distance, headshot verification, and remaining HP when eliminated.
/// </summary>
public class SpectatorKillCamSystem : MonoBehaviour
{
    public static SpectatorKillCamSystem Instance;
    private bool isSpectating = false;

    private void Awake() { Instance = this; }

    public void TriggerDeathRecap(string killerName, string weaponUsed, float distanceMeters, float killerRemainingHP, bool isHeadshot)
    {
        Debug.Log($"[SpectatorKillCamSystem] DEATH RECAP: Eliminated by {killerName} wielding {weaponUsed} from {distanceMeters:F1}m! Killer HP: {killerRemainingHP:F0} | Headshot: {isHeadshot}");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_DownedThud");
        StartCoroutine(ShowRecapCoroutine(killerName, weaponUsed, distanceMeters, killerRemainingHP, isHeadshot));
    }

    private IEnumerator ShowRecapCoroutine(string killerName, string weaponUsed, float distanceMeters, float killerRemainingHP, bool isHeadshot)
    {
        isSpectating = true;
        GameObject recapCanvasGo = new GameObject("DeathRecapCanvas");
        Canvas c = recapCanvasGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 99999;
        recapCanvasGo.AddComponent<CanvasScaler>();
        recapCanvasGo.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("RecapBG");
        bg.transform.SetParent(recapCanvasGo.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.2f, 0.3f);
        bgRect.anchorMax = new Vector2(0.8f, 0.7f);

        GameObject textGo = new GameObject("RecapText");
        textGo.transform.SetParent(bg.transform, false);
        Text t = textGo.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.text = $"<color=#FF2222>ELIMINATED IN COMBAT</color>\n\nKiller: <b>{killerName}</b>\nWeapon: <b>{weaponUsed}</b> {(isHeadshot ? "(CRITICAL HEADSHOT)" : "")}\nDistance: <b>{distanceMeters:F1}m</b> | Killer HP: <b>{killerRemainingHP:F0}</b>";
        RectTransform tRect = textGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one; tRect.sizeDelta = Vector2.zero;

        yield return new WaitForSeconds(6f);
        Destroy(recapCanvasGo);
        isSpectating = false;
    }
}
