using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Battle-royale HUD controls — exact layout matching the HTML5 preview.
/// Left side:  large joystick (bottom-left)
/// Right side: large FIRE circle, SCOPE above, JUMP top-right, CROUCH/PRONE mid-right, SWAP bottom
/// Upper-right cluster: NADE, BUILD
/// Contextual center: LOOT, POWER
/// The right 60 % of the screen is a transparent look-drag area.
/// </summary>
public class TouchControls : MonoBehaviour
{
    public static TouchControls Instance;

    // ── Public input state ───────────────────────────────────────────────────
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public bool IsFiring { get; private set; }
    public bool IsAiming { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool JumpRequested { get; set; }
    public bool SwapWeaponRequested { get; set; }
    public bool LootRequested { get; set; }
    public bool PowerUpRequested { get; set; }
    public bool BarricadeRequested { get; set; }
    public bool GrenadeRequested { get; set; }
    public bool EmoteRequested { get; set; }
    public bool PingRequested { get; set; }
    public bool ProneRequested { get; set; }
    public bool TrapDeployRequested { get; set; }
    public bool TalentTreeRequested { get; set; }
    public bool ReloadRequested { get; set; }
    public bool SprintRequested { get; set; }
    public bool InteractRequested { get; set; }
    public int SelectedEmoteSlot { get; set; }
    public ThrowableType SelectedThrowable { get; set; }
    public TrapType SelectedTrap { get; set; }

    // ── Private refs ─────────────────────────────────────────────────────────
    private GameObject lootBtnGo;  private Text lootBtnText;
    private GameObject powerBtnGo; private Text powerBtnText;

    // Joystick
    private RectTransform joystickBgRect;
    private RectTransform joystickKnobRect;
    private Image joystickKnobImg;
    private bool isDraggingJoystick;
    private float joystickRadius = 80f;

    // Look
    private Vector2 lastLookTouchPos;
    private int lookTouchId = -1;
    private float aimTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        SelectedThrowable = ThrowableType.FragGrenade;
        SelectedTrap = TrapType.SpikeTrap;
    }

    public void InitializeControls(Transform ct)
    {
        // ==================================================================
        //  JOYSTICK  —  bottom-left, 180×180, dark translucent ring + bright knob
        // ==================================================================
        GameObject joyBg = new GameObject("JoystickBG"); joyBg.transform.SetParent(ct, false);
        {
            Image img = joyBg.AddComponent<Image>();
            Texture2D ringTex = ProceduralArt.GenerateCircleButtonTexture(new Color(0.22f, 0.22f, 0.28f, 0.35f), 256);
            img.sprite = Sprite.Create(ringTex, new Rect(0, 0, 256, 256), Vector2.one * 0.5f);
            joystickBgRect = joyBg.GetComponent<RectTransform>();
            joystickBgRect.anchorMin = Vector2.zero; joystickBgRect.anchorMax = Vector2.zero;
            joystickBgRect.anchoredPosition = new Vector2(120, 120);
            joystickBgRect.sizeDelta = new Vector2(180, 180);
        }
        GameObject joyKnob = new GameObject("JoystickKnob"); joyKnob.transform.SetParent(joyBg.transform, false);
        {
            joystickKnobImg = joyKnob.AddComponent<Image>();
            Texture2D knobTex = ProceduralArt.GenerateCircleButtonTexture(new Color(0.7f, 0.7f, 0.75f, 0.55f), 128);
            joystickKnobImg.sprite = Sprite.Create(knobTex, new Rect(0, 0, 128, 128), Vector2.one * 0.5f);
            joystickKnobRect = joyKnob.GetComponent<RectTransform>();
            joystickKnobRect.anchoredPosition = Vector2.zero;
            joystickKnobRect.sizeDelta = new Vector2(75, 75);
        }
        EventTrigger jt = joyBg.AddComponent<EventTrigger>();
        AddET(jt, EventTriggerType.PointerDown, d => { isDraggingJoystick = true; JoyDrag((PointerEventData)d); joystickKnobImg.color = new Color(1f, 0.65f, 0.2f, 0.7f); });
        AddET(jt, EventTriggerType.Drag, d => JoyDrag((PointerEventData)d));
        AddET(jt, EventTriggerType.PointerUp, d => { isDraggingJoystick = false; MoveInput = Vector2.zero; joystickKnobRect.anchoredPosition = Vector2.zero; joystickKnobImg.color = new Color(0.7f, 0.7f, 0.75f, 0.55f); });

        // ==================================================================
        //  LOOK AREA  —  right 60 % of screen, fully transparent
        // ==================================================================
        GameObject lookGo = new GameObject("LookArea"); lookGo.transform.SetParent(ct, false);
        lookGo.AddComponent<Image>().color = Color.clear;
        RectTransform lookR = lookGo.GetComponent<RectTransform>();
        lookR.anchorMin = new Vector2(0.35f, 0); lookR.anchorMax = Vector2.one; lookR.sizeDelta = Vector2.zero;
        EventTrigger lt = lookGo.AddComponent<EventTrigger>();
        AddET(lt, EventTriggerType.PointerDown, d => { var pd = (PointerEventData)d; lookTouchId = pd.pointerId; lastLookTouchPos = pd.position; aimTimer = 0; });
        AddET(lt, EventTriggerType.Drag, d => { var pd = (PointerEventData)d; if (pd.pointerId == lookTouchId) { LookDelta = (pd.position - lastLookTouchPos) * 0.2f; lastLookTouchPos = pd.position; } });
        AddET(lt, EventTriggerType.PointerUp, d => { if (((PointerEventData)d).pointerId == lookTouchId) { lookTouchId = -1; LookDelta = Vector2.zero; IsAiming = false; } });

        // ==================================================================
        //  FIRE  —  large red circle, bottom-right
        //  Position: right 100px from edge, bottom 80px — 120×120
        // ==================================================================
        GameObject fireGo = MakeCircleBtn(ct, "FireBtn", "FIRE", new Color(0.85f, 0.12f, 0.1f, 0.9f), new Vector2(-100, 80), new Vector2(120, 120));
        EventTrigger ft = fireGo.AddComponent<EventTrigger>();
        AddET(ft, EventTriggerType.PointerDown, d => { IsFiring = true; });
        AddET(ft, EventTriggerType.PointerUp, d => { IsFiring = false; });

        // ==================================================================
        //  RIGHT-SIDE ACTION CLUSTER  (matching demo layout exactly)
        // ==================================================================
        // SCOPE — above fire
        MakeBtnWithTrigger(ct, "ScopeBtn", "SCOPE", new Color(0.3f, 0.3f, 0.36f, 0.8f), new Vector2(-130, 220), new Vector2(60, 60), d => { IsAiming = !IsAiming; });

        // JUMP — top-right of fire
        MakeBtnWithTrigger(ct, "JumpBtn", "JUMP", new Color(0.35f, 0.35f, 0.4f, 0.8f), new Vector2(-40, 220), new Vector2(60, 60), d => { JumpRequested = true; });

        // CROUCH — mid-left of fire
        MakeBtnWithTrigger(ct, "CrouchBtn", "CROUCH", new Color(0.3f, 0.3f, 0.35f, 0.8f), new Vector2(-240, 150), new Vector2(58, 58), d => { IsCrouching = !IsCrouching; });

        // PRONE — below crouch
        MakeBtnWithTrigger(ct, "ProneBtn", "PRONE", new Color(0.25f, 0.25f, 0.3f, 0.75f), new Vector2(-240, 80), new Vector2(56, 56), d => { ProneRequested = true; });

        // SWAP — bottom between fire and edge
        MakeBtnWithTrigger(ct, "SwapBtn", "SWAP", new Color(0.12f, 0.45f, 0.72f, 0.8f), new Vector2(-150, 30), new Vector2(58, 58), d => { SwapWeaponRequested = true; });

        // NADE — upper cluster
        MakeBtnWithTrigger(ct, "GrenadeBtn", "NADE", new Color(0.4f, 0.5f, 0.22f, 0.8f), new Vector2(-60, 290), new Vector2(58, 58), d => { GrenadeRequested = true; });

        // BUILD — upper cluster left of nade
        MakeBtnWithTrigger(ct, "BuildBtn", "BUILD", new Color(0.18f, 0.6f, 0.7f, 0.8f), new Vector2(-150, 290), new Vector2(58, 58), d => { BarricadeRequested = true; });

        // RELOAD & INTERACT — right side utility
        MakeBtnWithTrigger(ct, "ReloadBtn", "RELOAD", new Color(0.7f, 0.5f, 0.1f, 0.8f), new Vector2(-150, 100), new Vector2(56, 56), d => { ReloadRequested = true; });
        MakeBtnWithTrigger(ct, "InteractBtn", "INTERACT", new Color(0.2f, 0.6f, 0.8f, 0.8f), new Vector2(-150, 170), new Vector2(56, 56), d => { InteractRequested = true; LootRequested = true; });

        // ==================================================================
        //  SMALL UTILITY BUTTONS  (left side, above joystick)
        // ==================================================================
        MakeBtnWithTriggerLeft(ct, "SprintBtn", "SPRINT", new Color(0.2f, 0.7f, 0.3f, 0.75f), new Vector2(50, 210), new Vector2(56, 56), d => { SprintRequested = !SprintRequested; });
        MakeBtnWithTriggerLeft(ct, "EmoteBtn", "EMOTE", new Color(0.6f, 0.35f, 0.75f, 0.65f), new Vector2(50, 280), new Vector2(52, 52), d => { EmoteRequested = true; SelectedEmoteSlot = 0; });
        MakeBtnWithTriggerLeft(ct, "PingBtn", "PING", new Color(0.2f, 0.5f, 0.9f, 0.65f), new Vector2(120, 280), new Vector2(52, 52), d => { PingRequested = true; });
        MakeBtnWithTriggerLeft(ct, "TrapBtn", "TRAP", new Color(0.55f, 0.38f, 0.2f, 0.65f), new Vector2(50, 340), new Vector2(52, 52), d => { TrapDeployRequested = true; });
        MakeBtnWithTriggerLeft(ct, "TalentBtn", "LVL", new Color(0.8f, 0.6f, 0.1f, 0.65f), new Vector2(120, 340), new Vector2(52, 52), d => { TalentTreeRequested = true; });

        // ==================================================================
        //  CONTEXTUAL BUTTONS (centre, hidden by default)
        // ==================================================================
        lootBtnGo = MakeRoundedPill(ct, "LootBtn", "LOOT", new Color(0.8f, 0.75f, 0.15f, 0.9f), new Vector2(0, 190), new Vector2(180, 55), true);
        lootBtnText = lootBtnGo.GetComponentInChildren<Text>();
        EventTrigger lootT = lootBtnGo.AddComponent<EventTrigger>();
        AddET(lootT, EventTriggerType.PointerDown, d => { LootRequested = true; });
        lootBtnGo.SetActive(false);

        powerBtnGo = MakeRoundedPill(ct, "PowerBtn", "USE POWER", new Color(0.7f, 0.15f, 0.7f, 0.9f), new Vector2(0, 260), new Vector2(200, 55), true);
        powerBtnText = powerBtnGo.GetComponentInChildren<Text>();
        EventTrigger powerT = powerBtnGo.AddComponent<EventTrigger>();
        AddET(powerT, EventTriggerType.PointerDown, d => { PowerUpRequested = true; });
        powerBtnGo.SetActive(false);
    }

    // ── Update (keyboard fallback for editor) ────────────────────────────────
    private void Update()
    {
        if (Input.GetKey(KeyCode.W)) MoveInput = new Vector2(MoveInput.x, 1);
        if (Input.GetKey(KeyCode.S)) MoveInput = new Vector2(MoveInput.x, -1);
        if (Input.GetKey(KeyCode.A)) MoveInput = new Vector2(-1, MoveInput.y);
        if (Input.GetKey(KeyCode.D)) MoveInput = new Vector2(1, MoveInput.y);
        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S)) MoveInput = new Vector2(MoveInput.x, 0);
        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D)) MoveInput = new Vector2(0, MoveInput.y);

        if (Input.GetKeyDown(KeyCode.Space)) JumpRequested = true;
        if (Input.GetKeyDown(KeyCode.C)) IsCrouching = !IsCrouching;
        if (Input.GetKeyDown(KeyCode.Z)) ProneRequested = true;
        if (Input.GetKeyDown(KeyCode.Q)) SwapWeaponRequested = true;
        if (Input.GetKeyDown(KeyCode.E)) LootRequested = true;
        if (Input.GetKeyDown(KeyCode.F)) PowerUpRequested = true;
        if (Input.GetKeyDown(KeyCode.G)) BarricadeRequested = true;
        if (Input.GetKeyDown(KeyCode.H)) GrenadeRequested = true;
        if (Input.GetKeyDown(KeyCode.T)) EmoteRequested = true;
        if (Input.GetKeyDown(KeyCode.V)) PingRequested = true;
        if (Input.GetKeyDown(KeyCode.X)) TrapDeployRequested = true;
        if (Input.GetKeyDown(KeyCode.Tab)) TalentTreeRequested = true;
        if (Input.GetKeyDown(KeyCode.R)) ReloadRequested = true;
        if (Input.GetKeyDown(KeyCode.LeftShift)) SprintRequested = !SprintRequested;
        if (Input.GetKeyDown(KeyCode.I)) { InteractRequested = true; LootRequested = true; }

        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > Screen.width * 0.4f) IsFiring = true;
        if (Input.GetMouseButtonUp(0)) IsFiring = false;

        if (Input.GetMouseButton(1)) { LookDelta = new Vector2(Input.GetAxis("Mouse X") * 5f, Input.GetAxis("Mouse Y") * 5f); IsAiming = true; }
        else if (lookTouchId == -1) { LookDelta = Vector2.zero; IsAiming = false; }
        if (lookTouchId != -1) { aimTimer += Time.deltaTime; if (aimTimer > 0.3f) IsAiming = true; }
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void ResetFiring() { IsFiring = false; }
    public void SetLootButtonActive(bool a, string l = "LOOT") { if (lootBtnGo != null) lootBtnGo.SetActive(a); if (a && lootBtnText != null) lootBtnText.text = "LOOT " + l; }
    public void SetPowerButtonActive(bool a, string p = "") { if (powerBtnGo != null) powerBtnGo.SetActive(a); if (a && powerBtnText != null) powerBtnText.text = "USE " + p.ToUpper(); }

    // ── Throwable Inventory ─────────────────────────────────────────────────
    private Dictionary<ThrowableType, int> throwableInventory = new Dictionary<ThrowableType, int>();

    /// <summary>Add throwables to the player's grenade inventory.</summary>
    public void AddThrowableToInventory(ThrowableType type, int count)
    {
        if (!throwableInventory.ContainsKey(type)) throwableInventory[type] = 0;
        throwableInventory[type] += count;
    }

    /// <summary>Get count of a specific throwable type.</summary>
    public int GetThrowableCount(ThrowableType type)
    {
        return throwableInventory.ContainsKey(type) ? throwableInventory[type] : 0;
    }

    /// <summary>Consume one throwable of the selected type. Returns false if none left.</summary>
    public bool ConsumeThrowable(ThrowableType type)
    {
        if (!throwableInventory.ContainsKey(type) || throwableInventory[type] <= 0) return false;
        throwableInventory[type]--;
        return true;
    }

    /// <summary>Cycle to the next throwable type that the player has in inventory.</summary>
    public void CycleThrowable()
    {
        ThrowableType[] types = (ThrowableType[])System.Enum.GetValues(typeof(ThrowableType));
        int idx = System.Array.IndexOf(types, SelectedThrowable);
        for (int i = 1; i <= types.Length; i++)
        {
            int nextIdx = (idx + i) % types.Length;
            if (GetThrowableCount(types[nextIdx]) > 0)
            {
                SelectedThrowable = types[nextIdx];
                return;
            }
        }
    }

    // ── Joystick math ────────────────────────────────────────────────────────
    private void JoyDrag(PointerEventData d)
    {
        if (!isDraggingJoystick) return;
        Vector2 lp; RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBgRect, d.position, d.pressEventCamera, out lp);
        if (lp.magnitude > joystickRadius) lp = lp.normalized * joystickRadius;
        joystickKnobRect.anchoredPosition = lp;
        MoveInput = lp / joystickRadius;
    }

    // ── Factory: circular gradient button (right-anchored) ───────────────────
    private GameObject MakeCircleBtn(Transform p, string n, string label, Color col, Vector2 pos, Vector2 sz)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        Image img = go.AddComponent<Image>();
        Texture2D tex = ProceduralArt.GenerateCircleButtonTexture(col, 128);
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), Vector2.one * 0.5f);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 0);
        r.anchoredPosition = pos; r.sizeDelta = sz;
        // Text shadow + text
        MkLabel(go.transform, label, sz.x < 80 ? 11 : 14);
        return go;
    }

    private void MakeBtnWithTrigger(Transform p, string n, string label, Color col, Vector2 pos, Vector2 sz, UnityEngine.Events.UnityAction<BaseEventData> onDown)
    {
        GameObject go = MakeCircleBtn(p, n, label, col, pos, sz);
        EventTrigger t = go.AddComponent<EventTrigger>();
        AddET(t, EventTriggerType.PointerDown, onDown);
    }

    // Factory: left-anchored small button
    private void MakeBtnWithTriggerLeft(Transform p, string n, string label, Color col, Vector2 pos, Vector2 sz, UnityEngine.Events.UnityAction<BaseEventData> onDown)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        Image img = go.AddComponent<Image>();
        Texture2D tex = ProceduralArt.GenerateCircleButtonTexture(col, 64);
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.zero;
        r.anchoredPosition = pos; r.sizeDelta = sz;
        MkLabel(go.transform, label, 10);
        EventTrigger t = go.AddComponent<EventTrigger>();
        AddET(t, EventTriggerType.PointerDown, onDown);
    }

    // Factory: rounded pill button (for LOOT / POWER)
    private GameObject MakeRoundedPill(Transform p, string n, string label, Color col, Vector2 pos, Vector2 sz, bool center)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        Image img = go.AddComponent<Image>();
        Texture2D tex = ProceduralArt.GenerateButtonTexture(col, new Color(col.r + 0.2f, col.g + 0.2f, col.b + 0.2f, col.a));
        img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
        img.type = Image.Type.Sliced;
        RectTransform r = go.GetComponent<RectTransform>();
        if (center) { r.anchorMin = new Vector2(0.5f, 0); r.anchorMax = new Vector2(0.5f, 0); }
        else { r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 0); }
        r.anchoredPosition = pos; r.sizeDelta = sz;
        MkLabel(go.transform, label, 16);
        return go;
    }

    // Small helper: label with shadow
    private void MkLabel(Transform p, string label, int size)
    {
        GameObject sGo = new GameObject("Shd"); sGo.transform.SetParent(p, false);
        Text st = sGo.AddComponent<Text>(); st.text = label;
        st.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        st.fontSize = size; st.fontStyle = FontStyle.Bold; st.color = new Color(0, 0, 0, 0.5f);
        st.alignment = TextAnchor.MiddleCenter; st.raycastTarget = false;
        RectTransform sr = sGo.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.sizeDelta = Vector2.zero;
        sr.anchoredPosition = new Vector2(1, -1);

        GameObject tGo = new GameObject("Text"); tGo.transform.SetParent(p, false);
        Text tt = tGo.AddComponent<Text>(); tt.text = label;
        tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tt.fontSize = size; tt.fontStyle = FontStyle.Bold; tt.color = Color.white;
        tt.alignment = TextAnchor.MiddleCenter;
        RectTransform tr = tGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
    }

    private void AddET(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry(); entry.eventID = type;
        entry.callback.AddListener(action); trigger.triggers.Add(entry);
    }
}


