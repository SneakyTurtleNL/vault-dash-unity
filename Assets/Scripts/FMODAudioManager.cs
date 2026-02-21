using System.Collections;
using UnityEngine;

/// <summary>
/// FMODAudioManager — Professional audio engine integration for Vault Dash.
///
/// Architecture:
///   • Wraps FMOD Studio events via EventReference (when FMOD_AVAILABLE is defined)
///   • Falls back gracefully to ProceduralAudio / Unity AudioSource if FMOD SDK absent
///   • Replaces AudioManager.cs as the primary audio hub (AudioManager remains for
///     legacy fallback if this component is missing)
///
/// FMOD Setup (Week 3):
///   1. Download FMOD Studio + Unity integration from fmod.com (free for indie)
///   2. Import FMODUnity.unitypackage into project
///   3. Add FMOD_AVAILABLE to Project Settings → Player → Scripting Define Symbols
///   4. Assign EventReferences in Inspector (drag from FMOD project browser)
///
/// Event naming convention (matches FMOD Studio project):
///   event:/SFX/Footstep
///   event:/SFX/Jump
///   event:/SFX/Crouch
///   event:/SFX/LaneChange
///   event:/SFX/ObstacleHit
///   event:/SFX/CoinCollect
///   event:/SFX/GemCollect
///   event:/SFX/PowerUp
///   event:/SFX/OpponentNear
///   event:/SFX/Countdown
///   event:/Music/MenuBGM
///   event:/Music/MatchBGM
///   event:/Music/VictoryFanfare
///   event:/Music/DefeatSting
/// </summary>
public class FMODAudioManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static FMODAudioManager Instance { get; private set; }

#if FMOD_AVAILABLE
    // ─── FMOD Event References ────────────────────────────────────────────────
    [Header("FMOD — SFX")]
    [FMODUnity.EventRef] public string footstepEvent    = "event:/SFX/Footstep";
    [FMODUnity.EventRef] public string jumpEvent        = "event:/SFX/Jump";
    [FMODUnity.EventRef] public string crouchEvent      = "event:/SFX/Crouch";
    [FMODUnity.EventRef] public string laneChangeEvent  = "event:/SFX/LaneChange";
    [FMODUnity.EventRef] public string obstacleHitEvent = "event:/SFX/ObstacleHit";
    [FMODUnity.EventRef] public string coinCollectEvent = "event:/SFX/CoinCollect";
    [FMODUnity.EventRef] public string gemCollectEvent  = "event:/SFX/GemCollect";
    [FMODUnity.EventRef] public string powerUpEvent     = "event:/SFX/PowerUp";
    [FMODUnity.EventRef] public string opponentNearEvent= "event:/SFX/OpponentNear";
    [FMODUnity.EventRef] public string countdownEvent   = "event:/SFX/Countdown";

    [Header("FMOD — Music")]
    [FMODUnity.EventRef] public string menuBgmEvent      = "event:/Music/MenuBGM";
    [FMODUnity.EventRef] public string matchBgmEvent     = "event:/Music/MatchBGM";
    [FMODUnity.EventRef] public string victoryFanfareEvent = "event:/Music/VictoryFanfare";
    [FMODUnity.EventRef] public string defeatStingEvent  = "event:/Music/DefeatSting";

    // ─── FMOD Instances (for music + tension control) ─────────────────────────
    private FMOD.Studio.EventInstance _menuBgmInstance;
    private FMOD.Studio.EventInstance _matchBgmInstance;
    private FMOD.Studio.EventInstance _footstepInstance;

    private bool _matchBgmPlaying = false;
    private bool _footstepsActive = false;

    // FMOD parameter names
    private const string PARAM_TENSION  = "Tension";   // 0.0 → 1.0 (drives pitch + filter)
    private const string PARAM_CHARACTER= "CharacterPitch"; // per-character footstep pitch
#endif

    // ─── Volume (shared with/without FMOD) ───────────────────────────────────
    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float sfxVolume    = 0.8f;
    [Range(0f, 1f)] public float musicVolume  = 0.45f;

    private const string KEY_MASTER = "Audio_Master";
    private const string KEY_SFX    = "Audio_SFX";
    private const string KEY_MUSIC  = "Audio_Music";

    // ─── Tension state ────────────────────────────────────────────────────────
    private float _currentTension = 0f;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumePrefs();

#if FMOD_AVAILABLE
        Debug.Log("[FMODAudioManager] FMOD Studio active ✓");
        InitFMOD();
#else
        Debug.Log("[FMODAudioManager] FMOD not available — falling back to AudioManager.");
#endif
    }

#if FMOD_AVAILABLE
    void InitFMOD()
    {
        // Pre-create persistent event instances (music + footsteps loop)
        _menuBgmInstance   = FMODUnity.RuntimeManager.CreateInstance(menuBgmEvent);
        _matchBgmInstance  = FMODUnity.RuntimeManager.CreateInstance(matchBgmEvent);
        _footstepInstance  = FMODUnity.RuntimeManager.CreateInstance(footstepEvent);

        // Set initial bus volumes
        ApplyVolumes();
    }

    void ApplyVolumes()
    {
        // FMOD bus volume control
        FMOD.Studio.Bus sfxBus, musicBus;
        FMODUnity.RuntimeManager.StudioSystem.getBus("bus:/SFX",   out sfxBus);
        FMODUnity.RuntimeManager.StudioSystem.getBus("bus:/Music", out musicBus);
        sfxBus.setVolume(sfxVolume * masterVolume);
        musicBus.setVolume(musicVolume * masterVolume);
    }
#else
    void ApplyVolumes()
    {
        // Delegate to legacy AudioManager
        AudioManager.Instance?.SetMasterVolume(masterVolume);
        AudioManager.Instance?.SetSFXVolume(sfxVolume);
        AudioManager.Instance?.SetMusicVolume(musicVolume);
    }
#endif

    void LoadVolumePrefs()
    {
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1.0f);
        sfxVolume    = PlayerPrefs.GetFloat(KEY_SFX,    0.8f);
        musicVolume  = PlayerPrefs.GetFloat(KEY_MUSIC,  0.45f);
    }

    // ─── SFX API ──────────────────────────────────────────────────────────────
    public void PlayJump()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(jumpEvent);
#else
        AudioManager.Instance?.PlayJump();
#endif
    }

    public void PlayCrouch()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(crouchEvent);
#else
        AudioManager.Instance?.PlayCrouch();
#endif
    }

    public void PlayLaneChange()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(laneChangeEvent);
#else
        AudioManager.Instance?.PlayLaneChange();
#endif
    }

    public void PlayObstacleHit()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(obstacleHitEvent);
#else
        AudioManager.Instance?.PlayObstacleHit();
#endif
    }

    public void PlayCoinCollect()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(coinCollectEvent);
#else
        AudioManager.Instance?.PlayCoinCollect();
#endif
    }

    public void PlayGemCollect()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(gemCollectEvent);
#else
        AudioManager.Instance?.PlayGemCollect();
#endif
    }

    public void PlayPowerUp()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(powerUpEvent);
#else
        AudioManager.Instance?.PlayPowerUp();
#endif
    }

    public void PlayOpponentNear()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(opponentNearEvent);
#else
        AudioManager.Instance?.PlayOpponentNear();
#endif
    }

    public void PlayCountdown()
    {
#if FMOD_AVAILABLE
        FMODUnity.RuntimeManager.PlayOneShot(countdownEvent);
#else
        AudioManager.Instance?.PlayCountdown();
#endif
    }

    // ─── Footsteps (looped) ───────────────────────────────────────────────────
    /// <param name="characterPitch">0.8–1.2 per-character pitch variation</param>
    public void StartFootsteps(float characterPitch = 1.0f)
    {
#if FMOD_AVAILABLE
        if (_footstepsActive) return;
        _footstepsActive = true;
        _footstepInstance.setParameterByName(PARAM_CHARACTER, characterPitch);
        _footstepInstance.start();
        Debug.Log($"[FMODAudioManager] Footsteps start (pitch={characterPitch})");
#else
        AudioManager.Instance?.StartFootsteps();
        AudioManager.Instance?.SetFootstepSpeed(characterPitch);
#endif
    }

    public void StopFootsteps()
    {
#if FMOD_AVAILABLE
        if (!_footstepsActive) return;
        _footstepsActive = false;
        _footstepInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        Debug.Log("[FMODAudioManager] Footsteps stop");
#else
        AudioManager.Instance?.StopFootsteps();
#endif
    }

    // ─── Music ────────────────────────────────────────────────────────────────
    public void PlayMenuMusic()
    {
#if FMOD_AVAILABLE
        StopMatchMusic();
        _menuBgmInstance.start();
        Debug.Log("[FMODAudioManager] Menu music ▶");
#else
        AudioManager.Instance?.PlayMenuMusic();
#endif
    }

    public void StopMenuMusic()
    {
#if FMOD_AVAILABLE
        _menuBgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
#else
        AudioManager.Instance?.StopMusic();
#endif
    }

    public void PlayMatchMusic()
    {
#if FMOD_AVAILABLE
        if (_matchBgmPlaying) return;
        _matchBgmPlaying = true;
        StopMenuMusic();
        _matchBgmInstance.start();
        Debug.Log("[FMODAudioManager] Match music ▶");
#else
        AudioManager.Instance?.PlayMatchMusic();
#endif
    }

    public void StopMatchMusic()
    {
#if FMOD_AVAILABLE
        if (!_matchBgmPlaying) return;
        _matchBgmPlaying = false;
        _matchBgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
#else
        AudioManager.Instance?.StopMusic();
#endif
    }

    public void PlayVictory()
    {
#if FMOD_AVAILABLE
        StopMatchMusic();
        StopMenuMusic();
        FMODUnity.RuntimeManager.PlayOneShot(victoryFanfareEvent);
        Debug.Log("[FMODAudioManager] Victory fanfare 🎉");
#else
        AudioManager.Instance?.PlayVictory();
#endif
    }

    public void PlayDefeat()
    {
#if FMOD_AVAILABLE
        StopMatchMusic();
        StopMenuMusic();
        FMODUnity.RuntimeManager.PlayOneShot(defeatStingEvent);
        Debug.Log("[FMODAudioManager] Defeat sting 😞");
#else
        AudioManager.Instance?.PlayDefeat();
#endif
    }

    // ─── Tension System ───────────────────────────────────────────────────────
    /// <summary>
    /// Drive dynamic tension music. Call from MatchManager every frame.
    /// distanceRemaining: 0–100m range (below 100m = full effect range).
    /// </summary>
    public void UpdateTension(float distanceRemaining)
    {
        float t = Mathf.Clamp01(1f - (distanceRemaining / 100f));
        _currentTension = Mathf.Lerp(_currentTension, t, Time.deltaTime * 3f); // smooth

#if FMOD_AVAILABLE
        if (_matchBgmPlaying)
        {
            _matchBgmInstance.setParameterByName(PARAM_TENSION, _currentTension);
        }
#else
        AudioManager.Instance?.UpdateTension(distanceRemaining);
#endif
    }

    public void ResetTension()
    {
        _currentTension = 0f;
#if FMOD_AVAILABLE
        if (_matchBgmInstance.isValid())
            _matchBgmInstance.setParameterByName(PARAM_TENSION, 0f);
#else
        AudioManager.Instance?.ResetTension();
#endif
    }

    // ─── Volume Control ───────────────────────────────────────────────────────
    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_MUSIC, musicVolume);
        ApplyVolumes();
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────
    void OnDestroy()
    {
#if FMOD_AVAILABLE
        _menuBgmInstance.release();
        _matchBgmInstance.release();
        _footstepInstance.release();
#endif
    }
}
