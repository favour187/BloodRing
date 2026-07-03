using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Battle-royale-grade Emote system with wheel selection, 3D animations, and networked sync.
/// Players can equip up to 6 emotes in their emote wheel and trigger them in-game.
/// </summary>
public enum EmoteType
{
    Victory,
    ThumbsUp,
    Dab,
    FlosseDance,
    Pushup,
    LookAround,
    Salute,
    Clap,
    FistPump,
    ChickenDance,
    HeadBang,
    Flex
}

[System.Serializable]
public class EmoteData
{
    public EmoteType type;
    public string displayName;
    public float duration;
    public Color particleColor;

    public static EmoteData GetEmote(EmoteType type)
    {
        EmoteData e = new EmoteData();
        e.type = type;
        switch (type)
        {
            case EmoteType.Victory:       e.displayName = "VICTORY!";       e.duration = 2.5f; e.particleColor = Color.yellow; break;
            case EmoteType.ThumbsUp:      e.displayName = "Thumbs Up";      e.duration = 2.0f; e.particleColor = Color.green; break;
            case EmoteType.Dab:           e.displayName = "Dab";            e.duration = 1.5f; e.particleColor = Color.cyan; break;
            case EmoteType.FlosseDance:   e.displayName = "Flosse Dance";   e.duration = 3.0f; e.particleColor = Color.magenta; break;
            case EmoteType.Pushup:        e.displayName = "Push-Up";        e.duration = 3.5f; e.particleColor = Color.red; break;
            case EmoteType.LookAround:    e.displayName = "Look Around";    e.duration = 2.0f; e.particleColor = Color.white; break;
            case EmoteType.Salute:        e.displayName = "Salute";         e.duration = 2.0f; e.particleColor = new Color(0.2f, 0.6f, 0.2f); break;
            case EmoteType.Clap:          e.displayName = "Clap";           e.duration = 2.5f; e.particleColor = Color.yellow; break;
            case EmoteType.FistPump:      e.displayName = "Fist Pump";      e.duration = 2.0f; e.particleColor = new Color(1f, 0.5f, 0f); break;
            case EmoteType.ChickenDance:  e.displayName = "Chicken Dance";  e.duration = 3.0f; e.particleColor = Color.yellow; break;
            case EmoteType.HeadBang:      e.displayName = "Head Bang";      e.duration = 3.0f; e.particleColor = Color.red; break;
            case EmoteType.Flex:          e.displayName = "Flex";           e.duration = 2.5f; e.particleColor = new Color(1f, 0.8f, 0f); break;
        }
        return e;
    }
}

public class EmoteSystem : NetworkBehaviour
{
    public static EmoteSystem Instance;

    public NetworkVariable<int> currentEmote = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private EmoteType[] equippedEmotes = new EmoteType[6];
    private bool isEmoting = false;
    private float emoteEndTime = 0f;
    private GameObject emoteParticles;
    private GameObject emoteTextObj;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        // Default emote loadout
        equippedEmotes[0] = EmoteType.Victory;
        equippedEmotes[1] = EmoteType.ThumbsUp;
        equippedEmotes[2] = EmoteType.Dab;
        equippedEmotes[3] = EmoteType.FlosseDance;
        equippedEmotes[4] = EmoteType.Salute;
        equippedEmotes[5] = EmoteType.Flex;
    }

    public void TriggerEmote(int slotIndex)
    {
        if (isEmoting || slotIndex < 0 || slotIndex >= equippedEmotes.Length) return;
        EmoteData emote = EmoteData.GetEmote(equippedEmotes[slotIndex]);
        isEmoting = true;
        emoteEndTime = Time.time + emote.duration;
        currentEmote.Value = (int)equippedEmotes[slotIndex];
        PlayEmoteVisuals(emote);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }

    private void PlayEmoteVisuals(EmoteData emote)
    {
        // Cleanup old
        if (emoteParticles != null) Destroy(emoteParticles);
        if (emoteTextObj != null) Destroy(emoteTextObj);

        // Particle burst
        emoteParticles = new GameObject("EmoteFX");
        emoteParticles.transform.SetParent(transform);
        emoteParticles.transform.localPosition = new Vector3(0, 2.5f, 0);
        ParticleSystem ps = emoteParticles.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = emoteParticles.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(ProceduralArt.GetSafeShader("Sprites/Default"));
        var main = ps.main;
        main.duration = emote.duration;
        main.loop = true;
        main.startColor = emote.particleColor;
        main.startSize = 0.2f;
        main.startSpeed = 2f;
        main.startLifetime = 1f;
        var emission = ps.emission;
        emission.rateOverTime = 15;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        ps.Play();

        // 3D floating text
        emoteTextObj = new GameObject("EmoteText3D");
        emoteTextObj.transform.SetParent(transform);
        emoteTextObj.transform.localPosition = new Vector3(0, 3.2f, 0);
        TextMesh tm = emoteTextObj.AddComponent<TextMesh>();
        tm.text = emote.displayName;
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.color = emote.particleColor;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
    }

    private void Update()
    {
        if (isEmoting && Time.time >= emoteEndTime)
        {
            StopEmote();
        }

        // Billboard emote text
        if (emoteTextObj != null && Camera.main != null)
        {
            emoteTextObj.transform.LookAt(Camera.main.transform);
            emoteTextObj.transform.Rotate(0, 180, 0);
        }
    }

    public void StopEmote()
    {
        isEmoting = false;
        currentEmote.Value = -1;
        if (emoteParticles != null) { Destroy(emoteParticles); emoteParticles = null; }
        if (emoteTextObj != null) { Destroy(emoteTextObj); emoteTextObj = null; }
    }

    public bool IsEmoting() { return isEmoting; }
    public EmoteType[] GetEquippedEmotes() { return equippedEmotes; }
    public void SetEquippedEmote(int slot, EmoteType type) { if (slot >= 0 && slot < 6) equippedEmotes[slot] = type; }
}


