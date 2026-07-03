using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[AudioManager]");
                instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
                instance.InitializeAudio();
            }
            return instance;
        }
    }

    private AudioSource sfxSource;
    private AudioSource echoSource;
    private AudioSource musicSource;
    private AudioSource zoneDamageSource;
    private AudioSource footstepSource;
    private AudioSource voSource;

    // SFX Clips
    private AudioClip pistolClip; private AudioClip rifleClip; private AudioClip shotgunClip;
    private AudioClip footstepGrassClip; private AudioClip footstepWoodClip;
    private AudioClip zoneDamageClip; private AudioClip powerPickupClip;
    private AudioClip winClip; private AudioClip loseClip;
    private AudioClip uiClickClip; private AudioClip hitMarkerClip; private AudioClip headshotClip;
    private AudioClip deathClip; private AudioClip beepNormalClip; private AudioClip beepFinalClip;

    // High-Fidelity Voiceover Clips
    private AudioClip voWelcomeClip; private AudioClip voVictoryClip; private AudioClip voZoneWarningClip; private AudioClip voCountdownClip;

    // Polished Lively Music Clips
    private AudioClip livelyLobbyBeat;
    private AudioClip battleActionBeat;
    private AudioClip proceduralLobbyBeat;

    private bool isFootstepGrass = true;
    private Coroutine footstepCoroutine;
    private bool playedZoneWarning = false;

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); InitializeAudio(); }
        else if (instance != this) { Destroy(gameObject); }
    }

    private void InitializeAudio()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        echoSource = gameObject.AddComponent<AudioSource>(); echoSource.volume = 0.3f;
        musicSource = gameObject.AddComponent<AudioSource>(); musicSource.loop = true; musicSource.volume = 0.5f; musicSource.priority = 20;
        zoneDamageSource = gameObject.AddComponent<AudioSource>(); zoneDamageSource.loop = true; zoneDamageSource.volume = 0.4f;
        footstepSource = gameObject.AddComponent<AudioSource>(); footstepSource.volume = 0.5f;
        voSource = gameObject.AddComponent<AudioSource>(); voSource.volume = 1.0f; voSource.priority = 10;

        LoadExternalOrGenerateClips();
        PlayLobbyMusic();
    }

    private void LoadExternalOrGenerateClips()
    {
        // 1. Load Polished Lively Music from Resources
        livelyLobbyBeat = Resources.Load<AudioClip>("Audio/Music/LivelyLobbyBeat");
        battleActionBeat = Resources.Load<AudioClip>("Audio/Music/BattleActionBeat");

        // 2. Load high-end AI Voiceovers
        voWelcomeClip = Resources.Load<AudioClip>("Audio/VO_Welcome") ?? LoadAssetAtPath("Assets/Audio/VO_Welcome.wav");
        voVictoryClip = Resources.Load<AudioClip>("Audio/VO_Victory") ?? LoadAssetAtPath("Assets/Audio/VO_Victory.wav");
        voZoneWarningClip = Resources.Load<AudioClip>("Audio/VO_ZoneWarning") ?? LoadAssetAtPath("Assets/Audio/VO_ZoneWarning.wav");
        voCountdownClip = Resources.Load<AudioClip>("Audio/VO_Countdown") ?? LoadAssetAtPath("Assets/Audio/VO_Countdown.wav");

        // 3. Load real external SFX assets
        pistolClip = Resources.Load<AudioClip>("Audio/Weapons/Pistol");
        rifleClip = Resources.Load<AudioClip>("Audio/Weapons/Rifle");
        shotgunClip = Resources.Load<AudioClip>("Audio/Weapons/Shotgun");
        footstepGrassClip = Resources.Load<AudioClip>("Audio/SFX/FootGrass");
        footstepWoodClip = Resources.Load<AudioClip>("Audio/SFX/FootWood");
        zoneDamageClip = Resources.Load<AudioClip>("Audio/Environment/ZoneDamage");
        uiClickClip = Resources.Load<AudioClip>("Audio/SFX/UIClick");
        hitMarkerClip = Resources.Load<AudioClip>("Audio/SFX/HitMarker");
        headshotClip = Resources.Load<AudioClip>("Audio/SFX/Headshot");
        deathClip = Resources.Load<AudioClip>("Audio/SFX/DeathThud");

        int sampleRate = 44100;
        // 4. Generate Lively Procedural EDM Beat (128 BPM = 0.46875s per beat)
        if (livelyLobbyBeat == null || battleActionBeat == null)
        {
            int beatSamples = (int)(sampleRate * 3.75f); // 8 beats loop
            float[] beatData = new float[beatSamples];
            float beatLen = 0.46875f;
            for (int i = 0; i < beatSamples; i++)
            {
                float t = (float)i / sampleRate; float beatTime = t % beatLen;
                
                // Punchy Kick Drum (every beat)
                float kickFreq = Mathf.Lerp(150f, 40f, beatTime / 0.15f);
                float kick = (beatTime < 0.15f) ? Mathf.Sin(2 * Mathf.PI * kickFreq * beatTime) * (1f - (beatTime / 0.15f)) : 0f;

                // Offbeat Hi-Hat
                float hatTime = (t + beatLen * 0.5f) % beatLen;
                float hat = (hatTime < 0.05f) ? Random.Range(-1f, 1f) * (1f - (hatTime / 0.05f)) * 0.3f : 0f;

                // Catchy Cyberpunk Synth Bass/Lead
                int step = Mathf.FloorToInt(t / (beatLen * 0.5f));
                float[] notes = new float[] { 110f, 110f, 130.81f, 146.83f, 164.81f, 146.83f, 130.81f, 123.47f };
                float noteFreq = notes[step % notes.Length];
                float synth = Mathf.Sin(2 * Mathf.PI * noteFreq * t) * 0.3f;

                beatData[i] = Mathf.Clamp(kick + hat + synth, -1f, 1f);
            }
            proceduralLobbyBeat = AudioClip.Create("ProceduralLivelyBeat", beatSamples, 1, sampleRate, false);
            proceduralLobbyBeat.SetData(beatData, 0);
        }

        if (pistolClip == null) { int samples = (int)(sampleRate * 0.15f); float[] data = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; float freq = Mathf.Lerp(800f, 200f, t / 0.15f); data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * (1f - (t / 0.15f)); } pistolClip = AudioClip.Create("PistolSound", samples, 1, sampleRate, false); pistolClip.SetData(data, 0); }
        if (rifleClip == null) { int samples = (int)(sampleRate * 0.1f); float[] data = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; data[i] = Random.Range(-1f, 1f) * (1f - (t / 0.1f)) * 0.8f; } rifleClip = AudioClip.Create("RifleSound", samples, 1, sampleRate, false); rifleClip.SetData(data, 0); }
        if (shotgunClip == null) { int samples = (int)(sampleRate * 0.35f); float[] data = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; float freq = Mathf.Lerp(300f, 80f, t / 0.35f); float sine = Mathf.Sin(2 * Mathf.PI * freq * t); float noise = Random.Range(-1f, 1f); data[i] = (sine * 0.6f + noise * 0.4f) * (1f - (t / 0.35f)); } shotgunClip = AudioClip.Create("ShotgunSound", samples, 1, sampleRate, false); shotgunClip.SetData(data, 0); }
        
        if (footstepGrassClip == null || footstepWoodClip == null) { int samples = (int)(sampleRate * 0.08f); float[] grassData = new float[samples]; float[] woodData = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; grassData[i] = Mathf.Sin(2 * Mathf.PI * 90f * t) * (1f - (t / 0.08f)) * 0.3f; woodData[i] = Mathf.Sin(2 * Mathf.PI * 400f * t) * (1f - (t / 0.08f)) * 0.4f; } footstepGrassClip = AudioClip.Create("FootGrass", samples, 1, sampleRate, false); footstepGrassClip.SetData(grassData, 0); footstepWoodClip = AudioClip.Create("FootWood", samples, 1, sampleRate, false); footstepWoodClip.SetData(woodData, 0); }
        if (zoneDamageClip == null) { int samples = (int)(sampleRate * 0.5f); float[] data = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; data[i] = Mathf.Sin(2 * Mathf.PI * 65f * t) * 0.5f; } zoneDamageClip = AudioClip.Create("ZoneDamage", samples, 1, sampleRate, false); zoneDamageClip.SetData(data, 0); }
        if (uiClickClip == null) { int samples = (int)(sampleRate * 0.03f); float[] data = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; data[i] = Mathf.Sin(2 * Mathf.PI * 1000f * t) * (1f - (t / 0.03f)) * 0.3f; } uiClickClip = AudioClip.Create("UIClick", samples, 1, sampleRate, false); uiClickClip.SetData(data, 0); }
        if (hitMarkerClip == null || headshotClip == null) { int samples = (int)(sampleRate * 0.05f); float[] hitData = new float[samples]; float[] headData = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; hitData[i] = Mathf.Sin(2 * Mathf.PI * 1200f * t) * (1f - (t / 0.05f)) * 0.4f; headData[i] = Mathf.Sin(2 * Mathf.PI * 2500f * t) * (1f - (t / 0.05f)) * 0.7f; } hitMarkerClip = AudioClip.Create("HitMarker", samples, 1, sampleRate, false); hitMarkerClip.SetData(hitData, 0); headshotClip = AudioClip.Create("Headshot", samples, 1, sampleRate, false); headshotClip.SetData(headData, 0); }
        if (deathClip == null) { int samples = (int)(sampleRate * 0.6f); float[] data = new float[samples]; for (int i = 0; i < samples; i++) { float t = (float)i / sampleRate; float freq = Mathf.Lerp(150f, 40f, t / 0.3f); data[i] = (t < 0.3f ? Mathf.Sin(2 * Mathf.PI * freq * t) : Random.Range(-0.2f, 0.2f)) * (1f - (t / 0.6f)); } deathClip = AudioClip.Create("DeathThud", samples, 1, sampleRate, false); deathClip.SetData(data, 0); }

        // Countdown Beeps
        int beepSamples = (int)(sampleRate * 0.1f); float[] beepNormData = new float[beepSamples]; float[] beepFinalData = new float[beepSamples]; for (int i = 0; i < beepSamples; i++) { float t = (float)i / sampleRate; beepNormData[i] = Mathf.Sin(2 * Mathf.PI * 600f * t) * (1f - (t / 0.1f)) * 0.5f; beepFinalData[i] = Mathf.Sin(2 * Mathf.PI * 1200f * t) * (1f - (t / 0.1f)) * 0.8f; } beepNormalClip = AudioClip.Create("BeepNorm", beepSamples, 1, sampleRate, false); beepNormalClip.SetData(beepNormData, 0); beepFinalClip = AudioClip.Create("BeepFinal", beepSamples, 1, sampleRate, false); beepFinalClip.SetData(beepFinalData, 0);

        int powerSamples = (int)(sampleRate * 0.45f); float[] powerData = new float[powerSamples]; for (int i = 0; i < powerSamples; i++) { float t = (float)i / sampleRate; float freq = t < 0.15f ? 440f : (t < 0.3f ? 554.37f : 659.25f); powerData[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.5f; } powerPickupClip = AudioClip.Create("PowerPickup", powerSamples, 1, sampleRate, false); powerPickupClip.SetData(powerData, 0);
        int winSamples = (int)(sampleRate * 1.0f); float[] winData = new float[winSamples]; for (int i = 0; i < winSamples; i++) { float t = (float)i / sampleRate; float freq = t < 0.2f ? 261.63f : (t < 0.4f ? 329.63f : (t < 0.6f ? 392.00f : (t < 0.8f ? 493.88f : 523.25f))); winData[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.6f; } winClip = AudioClip.Create("WinSound", winSamples, 1, sampleRate, false); winClip.SetData(winData, 0);
        int loseSamples = (int)(sampleRate * 0.75f); float[] loseData = new float[loseSamples]; for (int i = 0; i < loseSamples; i++) { float t = (float)i / sampleRate; float freq = t < 0.25f ? 392.00f : (t < 0.5f ? 311.13f : 261.63f); loseData[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.6f; } loseClip = AudioClip.Create("LoseSound", loseSamples, 1, sampleRate, false); loseClip.SetData(loseData, 0);
    }

    private AudioClip LoadAssetAtPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
#else
        return null;
#endif
    }

    // Lively Music Controllers
    public void PlayLobbyMusic()
    {
        if (musicSource == null) return;
        AudioClip target = livelyLobbyBeat != null ? livelyLobbyBeat : proceduralLobbyBeat;
        if (musicSource.clip != target || !musicSource.isPlaying)
        {
            musicSource.clip = target;
            musicSource.Play();
        }
    }

    public void PlayBattleMusic()
    {
        if (musicSource == null) return;
        AudioClip target = battleActionBeat != null ? battleActionBeat : proceduralLobbyBeat;
        if (musicSource.clip != target || !musicSource.isPlaying)
        {
            musicSource.clip = target;
            musicSource.Play();
        }
    }

    // Voiceover Triggers
    public void PlayVOWelcome() { if (voSource != null && voWelcomeClip != null) voSource.PlayOneShot(voWelcomeClip); }
    public void PlayVOVictory() { if (voSource != null && voVictoryClip != null) voSource.PlayOneShot(voVictoryClip); }
    public void PlayVOCountdown() { if (voSource != null && voCountdownClip != null) voSource.PlayOneShot(voCountdownClip); }
    public void PlayVOZoneWarning() { if (voSource != null && voZoneWarningClip != null && !playedZoneWarning) { voSource.PlayOneShot(voZoneWarningClip); playedZoneWarning = true; } }

    public void PlayWeaponSound(string weaponName)
    {
        if (sfxSource == null) return;
        AudioClip clip = weaponName.Contains("Pistol") || weaponName.Contains("Eagle") ? pistolClip : (weaponName.Contains("Shotgun") || weaponName.Contains("M1887") ? shotgunClip : rifleClip);
        if (clip != null) { sfxSource.PlayOneShot(clip); StartCoroutine(PlayEchoTail(clip)); }
    }

    private IEnumerator PlayEchoTail(AudioClip clip) { yield return new WaitForSeconds(0.08f); if (echoSource != null) echoSource.PlayOneShot(clip); }
    public void PlayUIClick() { if (sfxSource != null && uiClickClip != null) sfxSource.PlayOneShot(uiClickClip); }
    public void PlayHitMarker(bool isHeadshot) { if (sfxSource != null) sfxSource.PlayOneShot(isHeadshot ? headshotClip : hitMarkerClip); }
    public void PlayDeathSound() { if (sfxSource != null && deathClip != null) sfxSource.PlayOneShot(deathClip); }
    public void PlayBeep(bool isFinal) { if (sfxSource != null) sfxSource.PlayOneShot(isFinal ? beepFinalClip : beepNormalClip); }
    public void PlayPowerPickupSound() { if (sfxSource != null && powerPickupClip != null) sfxSource.PlayOneShot(powerPickupClip); }
    public void PlayWinSound() { if (sfxSource != null && winClip != null) { sfxSource.PlayOneShot(winClip); PlayVOVictory(); } }
    public void PlayLoseSound() { if (sfxSource != null && loseClip != null) sfxSource.PlayOneShot(loseClip); }

    private readonly Dictionary<string, AudioClip> _dynamicSfxCache = new Dictionary<string, AudioClip>();

    /// <summary>Play a one-shot SFX clip looked up by name from Resources/Audio/SFX (cached after first load).</summary>
    public void PlaySFX(string clipName)
    {
        if (sfxSource == null || string.IsNullOrEmpty(clipName)) return;
        if (!_dynamicSfxCache.TryGetValue(clipName, out AudioClip clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("Audio/SFX/" + clipName);
            if (clip != null) _dynamicSfxCache[clipName] = clip;
        }
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    /// <summary>Play a one-shot voiceover clip looked up by name from Resources/Audio/VO (cached after first load).</summary>
    public void PlayVO(string clipName)
    {
        if (voSource == null || string.IsNullOrEmpty(clipName)) return;
        if (!_dynamicSfxCache.TryGetValue(clipName, out AudioClip clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("Audio/VO/" + clipName);
            if (clip != null) _dynamicSfxCache[clipName] = clip;
        }
        if (clip != null) voSource.PlayOneShot(clip);
    }

    public void PlaySlideSound() { PlaySFX("SFX_WindGust"); }
    public void PlayVaultSound() { PlaySFX("SFX_ZiplineSlide"); }
    public void PlayMeleeSwingSound() { PlaySFX("SFX_GlassBreak"); }
    public void PlayMeleeHitSound() { PlaySFX("SFX_MetalImpact"); }

    public void SetZoneDamageActive(bool active, float shrinkProgress = 0f)
    {
        if (zoneDamageSource == null) return;
        if (active && !zoneDamageSource.isPlaying) { zoneDamageSource.clip = zoneDamageClip; zoneDamageSource.Play(); PlayVOZoneWarning(); }
        else if (!active && zoneDamageSource.isPlaying) { zoneDamageSource.Stop(); playedZoneWarning = false; }
        if (active) zoneDamageSource.pitch = 1f + shrinkProgress * 1.5f;
    }

    public void SetMoving(bool moving, bool isGrass = true)
    {
        isFootstepGrass = isGrass;
        if (moving && footstepCoroutine == null) { footstepCoroutine = StartCoroutine(FootstepLoop()); }
        else if (!moving && footstepCoroutine != null) { StopCoroutine(footstepCoroutine); footstepCoroutine = null; }
    }

    private IEnumerator FootstepLoop() { while (true) { if (footstepSource != null) { footstepSource.PlayOneShot(isFootstepGrass ? footstepGrassClip : footstepWoodClip); } yield return new WaitForSeconds(0.4f); } }
}


