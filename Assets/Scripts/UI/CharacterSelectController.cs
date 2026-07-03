using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CharacterSelectController : MonoBehaviour
{
    private int selectedIndex = 0;
    private string[] charNames = new string[] { "DJNeon", "Pulse", "Bolt" };
    private string[] charDescs = new string[] { "Cyber Beatmaster. Skill: Surge Beat (Speed + Heal aura).", "Quantum Vanguard. Skill: Aegis Dome (Forcefield blocks 800 dmg).", "Track Elite. Skill: Overdrive Sprint (Passive sprint speed boost)." };
    private float[] speedStats = new float[] { 0.85f, 0.75f, 0.95f };
    private float[] armorStats = new float[] { 0.65f, 0.95f, 0.55f };
    private float[] powerStats = new float[] { 0.90f, 0.85f, 0.70f };

    private List<GameObject> characterModels = new List<GameObject>();
    private Text nameText; private Text descText; private Image speedBarFill; private Image armorBarFill; private Image powerBarFill;
    private RawImage charPreviewCard;

    private void Start()
    {
        if (EventSystem.current == null) { GameObject esGo = new GameObject("EventSystem"); esGo.AddComponent<EventSystem>(); esGo.AddComponent<StandaloneInputModule>(); }
        Camera cam = Camera.main; if (cam == null) { GameObject camGo = new GameObject("Main Camera"); cam = camGo.AddComponent<Camera>(); cam.tag = "MainCamera"; } cam.transform.position = new Vector3(0, 2f, -6f); cam.transform.LookAt(new Vector3(0, 1f, 0)); cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f); cam.clearFlags = CameraClearFlags.SolidColor;
        GameObject lightGo = new GameObject("Directional Light"); Light light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, -30, 0);

        Vector3[] pos = new Vector3[] { new Vector3(-2.5f, 0, 0), new Vector3(0, 0, 0), new Vector3(2.5f, 0, 0) };

        for (int i = 0; i < 3; i++)
        {
            GameObject pedestal = BloodRing.Art.BloodRingArtLibrary.GetPrimitive3D("Cylinder.obj"); pedestal.transform.position = pos[i] + new Vector3(0, -0.2f, 0); pedestal.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f); pedestal.GetComponent<Renderer>().material.color = Color.black;
            GameObject model = ProceduralArt.CreateHumanoidMesh(charNames[i] == "DJNeon" ? "Striker" : (charNames[i] == "Pulse" ? "Tank" : "Stealth")); model.transform.position = pos[i]; model.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            characterModels.Add(model);
        }

        GameObject canvasGo = new GameObject("CharSelectCanvas"); Canvas canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); canvasGo.AddComponent<GraphicRaycaster>();

        GameObject titleGo = new GameObject("TitleText"); titleGo.transform.SetParent(canvasGo.transform, false); Text titleText = titleGo.AddComponent<Text>(); titleText.text = "SELECT YOUR AGENT"; titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); titleText.fontSize = 50; titleText.fontStyle = FontStyle.Bold; titleText.color = Color.white; titleText.alignment = TextAnchor.UpperCenter; RectTransform titleRect = titleGo.GetComponent<RectTransform>(); titleRect.anchorMin = new Vector2(0.5f, 1); titleRect.anchorMax = new Vector2(0.5f, 1); titleRect.anchoredPosition = new Vector2(0, -50); titleRect.sizeDelta = new Vector2(600, 60);

        GameObject infoGo = new GameObject("InfoPanel"); infoGo.transform.SetParent(canvasGo.transform, false); Image infoBg = infoGo.AddComponent<Image>(); infoBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); RectTransform infoRect = infoGo.GetComponent<RectTransform>(); infoRect.anchorMin = new Vector2(1, 0); infoRect.anchorMax = new Vector2(1, 1); infoRect.anchoredPosition = new Vector2(-200, 0); infoRect.sizeDelta = new Vector2(400, 0);

        GameObject cardGo = new GameObject("CharPreviewCard"); cardGo.transform.SetParent(infoGo.transform, false); charPreviewCard = cardGo.AddComponent<RawImage>(); RectTransform cRect = cardGo.GetComponent<RectTransform>(); cRect.anchoredPosition = new Vector2(0, 180); cRect.sizeDelta = new Vector2(350, 220);

        GameObject nameGo = new GameObject("CharName"); nameGo.transform.SetParent(infoGo.transform, false); nameText = nameGo.AddComponent<Text>(); nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); nameText.fontSize = 40; nameText.fontStyle = FontStyle.Bold; nameText.color = Color.red; nameText.alignment = TextAnchor.MiddleCenter; RectTransform nameRect = nameGo.GetComponent<RectTransform>(); nameRect.anchoredPosition = new Vector2(0, 300); nameRect.sizeDelta = new Vector2(350, 50);
        GameObject descGo = new GameObject("CharDesc"); descGo.transform.SetParent(infoGo.transform, false); descText = descGo.AddComponent<Text>(); descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); descText.fontSize = 20; descText.color = Color.white; descText.alignment = TextAnchor.MiddleCenter; descText.horizontalOverflow = HorizontalWrapMode.Wrap; RectTransform descRect = descGo.GetComponent<RectTransform>(); descRect.anchoredPosition = new Vector2(0, 20); descRect.sizeDelta = new Vector2(360, 100);

        speedBarFill = CreateStatBar(infoGo.transform, "SPEED", new Vector2(20, -80), Color.yellow); armorBarFill = CreateStatBar(infoGo.transform, "ARMOR", new Vector2(20, -140), Color.blue); powerBarFill = CreateStatBar(infoGo.transform, "POWER", new Vector2(20, -200), Color.magenta);

        UIBuilder.CreateButton(canvasGo.transform, "Select_DJNEON", "DJ NEON", new Vector2(-400, -250), new Color(0.6f, 0.2f, 0.2f, 0.9f), Color.red, () => { SelectCharacter(0); });
        UIBuilder.CreateButton(canvasGo.transform, "Select_PULSE", "PULSE", new Vector2(0, -250), new Color(0.2f, 0.4f, 0.6f, 0.9f), Color.cyan, () => { SelectCharacter(1); });
        UIBuilder.CreateButton(canvasGo.transform, "Select_BOLT", "BOLT", new Vector2(400, -250), new Color(0.4f, 0.2f, 0.6f, 0.9f), Color.magenta, () => { SelectCharacter(2); });

        UIBuilder.CreateButton(infoGo.transform, "ConfirmBtn", "CONFIRM", new Vector2(0, -300), new Color(0.8f, 0.1f, 0.1f, 1f), Color.yellow, async () =>
        {
            PlayerPrefs.SetString("SelectedCharacter", charNames[selectedIndex]); PlayerPrefs.Save();
            if (BackendAPI.Instance != null && BackendAPI.Instance.IsLoggedIn) await BackendAPI.Instance.UpdateCharacterAsync(charNames[selectedIndex]);
            GameManager.Instance.ChangeState(GameState.Lobby);
        });

        SelectCharacter(0);
    }

    private void Update() { for (int i = 0; i < characterModels.Count; i++) { float speed = (i == selectedIndex) ? 90f : 20f; characterModels[i].transform.Rotate(Vector3.up * speed * Time.deltaTime); float scale = (i == selectedIndex) ? 1.1f : 0.8f; characterModels[i].transform.localScale = Vector3.Lerp(characterModels[i].transform.localScale, new Vector3(scale, scale, scale), Time.deltaTime * 5f); } }

    private void SelectCharacter(int index) { selectedIndex = index; nameText.text = charNames[index].ToUpper(); descText.text = charDescs[index]; speedBarFill.rectTransform.anchorMax = new Vector2(speedStats[index], 1); armorBarFill.rectTransform.anchorMax = new Vector2(armorStats[index], 1); powerBarFill.rectTransform.anchorMax = new Vector2(powerStats[index], 1); Texture2D cTex = Resources.Load<Texture2D>("Char_" + charNames[index]); if (cTex != null && charPreviewCard != null) charPreviewCard.texture = cTex; }

    private Image CreateStatBar(Transform parent, string label, Vector2 pos, Color color) { GameObject labelGo = new GameObject(label + "_Text"); labelGo.transform.SetParent(parent, false); Text t = labelGo.AddComponent<Text>(); t.text = label; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = 20; t.color = Color.white; RectTransform labelRect = labelGo.GetComponent<RectTransform>(); labelRect.anchoredPosition = pos + new Vector2(-120, 25); labelRect.sizeDelta = new Vector2(100, 30); GameObject bgGo = new GameObject(label + "_BG"); bgGo.transform.SetParent(parent, false); Image bg = bgGo.AddComponent<Image>(); bg.color = Color.gray; RectTransform bgRect = bgGo.GetComponent<RectTransform>(); bgRect.anchoredPosition = pos; bgRect.sizeDelta = new Vector2(240, 20); GameObject fillGo = new GameObject(label + "_Fill"); fillGo.transform.SetParent(bgGo.transform, false); Image fill = fillGo.AddComponent<Image>(); fill.color = color; RectTransform fillRect = fillGo.GetComponent<RectTransform>(); fillRect.anchorMin = new Vector2(0, 0); fillRect.anchorMax = new Vector2(0.5f, 1); fillRect.sizeDelta = Vector2.zero; fillRect.pivot = new Vector2(0, 0.5f); return fill; }
}


