using System.Collections;
using UnityEngine;

/// <summary>
/// AudioManager — Central audio hub for Vault Dash.
///
/// Manages:
///  • SFX: footstep (looped), jump, crouch, obstacle collision, loot collect, power-up
///  • Music: menu BGM, match BGM, victory fanfare
///  • Volume control (master, sfx, music)
///  • Distance-based tension music (pitch up as opponent closes)
///
/// Singleton — persists across scenes.
/// All AudioClips are assigned in the Inspector (Assets/Audio/).
/// Falls back gracefully when clips are null.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ─── SFX Clips ────────────────────────────────────────────────────────────
    [Header("SFX — Footsteps")]
    [Tooltip("Short footstep clip — looped while player is running")]
    public AudioClip footstepClip;
    [Tooltip("Seconds between each footstep tick")]
    public float footstepInterval = 0.32f;

    [Header("SFX — Actions")]
    public AudioClip jumpClip;
    public AudioClip crouchClip;
    public AudioClip laneChangeClip;

    [Header("SFX — Events")]
    public AudioClip obstacleHitClip;    // game-over collision
    public AudioClip coinCollectClip;
    public AudioClip gemCollectClip;
    public AudioClip powerUpClip;
    public AudioClip opponentNearClip;   // tension sound when opponent enters tunnel view

    [Header("SFX — Match End")]
    public AudioClip victoryFanfareClip;
    public AudioClip defeatStingClip;
    public AudioClip countdownClip;      // 3-2-1 before match

    // ─── Music Clips ──────────────────────────────────────────────────────────
    [Header("Music")]
    public AudioClip menuBgmClip;
    public AudioClip matchBgmClip;
    public float     matchBgmFadeInTime  = 1.0f;
    public float     musicVolume         = 0.45f;

    // ─── Volume ───────────────────────────────────────────────────────────────
    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float sfxVolume    = 0.8f;

    // ─── Private Refs ─────────────────────────────────────────────────────────
    private AudioSource _sfxSource;          // one-shot SFX
    private AudioSource _footstepSource;     // looping footstep channel
    private AudioSource _musicSource;        // background music
    private AudioSource _tensionSource;      // tension/dramatic layer

    private Coroutine _footstepRoutine;
    private Coroutine _musicFadeRoutine;

    private bool _footstepsActive = false;
    private bool _matchBgmPlaying = false;

    // Tension: pitch ramps 1.0→1.4 as distance closes 100m→0m
    private const float TENSION_PITCH_MIN = 1.0f;
    private const float TENSION_PITCH_MAX = 1.4f;

    // PlayerPrefs keys
    private const string KEY_MASTER = "Audio_Master";
    private const string KEY_SFX    = "Audio_SFX";
    private const string KEY_MUSIC  = "Audio_Music";

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAudioSources();
        LoadVolumePrefs();
    }

    void BuildAudioSources()
    {
        // SFX — one-shot channel
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.loop        = false;

        // Footstep — dedicated channel for tighter control
        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.playOnAwake = false;
        _footstepSource.loop        = false;

        // Music — looping BGM
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop        = true;
        _musicSource.volume      = musicVolume;

        // Tension — extra drama layer
        _tensionSource = gameObject.AddComponent<AudioSource>();
        _tensionSource.playOnAwake = false;
        _tensionSource.loop        = true;
        _tensionSource.volume      = 0f;
    }

    void LoadVolumePrefs()
    {
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1.0f);
        sfxVolume    = PlayerPrefs.GetFloat(KEY_SFX,    0.8f);
        musicVolume  = PlayerPrefs.GetFloat(KEY_MUSIC,  0.45f);
        ApplyVolumes();
    }

    void ApplyVolumes()
    {
        if (_sfxSource    != null) _sfxSource.volume    = sfxVolume    * masterVolume;
        if (_footstepSource!= null)_footstepSource.volume = sfxVolume * masterVolume;
        if (_musicSource  != null) _musicSource.volume  = musicVolume  * masterVolume;
    }

    // ─── SFX API ──────────────────────────────────────────────────────────────
    public void PlayJump()       => PlaySFX(jumpClip);
    public void PlayCrouch()     => PlaySFX(crouchClip);
    public void PlayLaneChange() => PlaySFX(laneChangeClip);
    public void PlayObstacleHit()=> PlaySFX(obstacleHitClip);
    public void PlayCoinCollect()=> PlaySFX(coinCollectClip);
    public void PlayGemCollect() => PlaySFX(gemCollectClip,  1.2f);  // slightly higher pitch
    public void PlayPowerUp()    => PlaySFX(powerUpClip);
    public void PlayOpponentNear()=> PlaySFX(opponentNearClip);
    public void PlayCountdown()  => PlaySFX(countdownClip);

    public void PlayVictory()
    {
        StopMusic();
        PlaySFX(victoryFanfareClip, 1.0f, 1.0f);   // full volume
    }

    public void PlayDefeat()
    {
        StopMusic();
        PlaySFX(defeatStingClip, 1.0f, 0.9f);
    }

    void PlaySFX(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.pitch = pitch;
        _sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }

    // ─── Footstep Loop ────────────────────────────────────────────────────────
    public void StartFootsteps()
    {
        if (_footstepsActive) return;
        _footstepsActive = true;
        if (_footstepRoutine != null) StopCoroutine(_footstepRoutine);
        _footstepRoutine = StartCoroutine(FootstepLoop());
    }

    public void StopFootsteps()
    {
        _footstepsActive = false;
        if (_footstepRoutine != null)
        {
            StopCoroutine(_footstepRoutine);
            _footstepRoutine = null;
        }
        if (_footstepSource != null) _footstepSource.Stop();
    }

    IEnumerator FootstepLoop()
    {
        while (_footstepsActive)
        {
            if (footstepClip != null && _footstepSource != null)
                _footstepSource.PlayOneShot(footstepClip, sfxVolume * masterVolume);

            yield return new WaitForSeconds(footstepInterval);
        }
    }

    public void SetFootstepSpeed(float runSpeedMultiplier)
    {
        // Faster character = quicker footstep cadence
        footstepInterval = 0.32f / Mathf.Max(0.5f, runSpeedMultiplier);
    }

    // ─── Music ────────────────────────────────────────────────────────────────
    public void PlayMenuMusic()
    {
        if (menuBgmClip == null) return;
        if (_musicSource.isPlaying && _musicSource.clip == menuBgmClip) return;

        StopMusic(false);
        _musicSource.clip   = menuBgmClip;
        _musicSource.volume = musicVolume * masterVolume;
        _musicSource.pitch  = 1f;
        _musicSource.Play();
        _matchBgmPlaying    = false;
    }

    public void PlayMatchMusic()
    {
        if (matchBgmClip == null) return;
        if (_matchBgmPlaying) return;

        _matchBgmPlaying = true;
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeInMusic(matchBgmClip, matchBgmFadeInTime));
    }

    public void StopMusic(bool fade = true)
    {
        if (fade && _musicSource != null && _musicSource.isPlaying)
        {
            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(FadeOutMusic(0.5f));
        }
        else if (_musicSource != null)
        {
            _musicSource.Stop();
        }
        _matchBgmPlaying = false;
    }

    IEnumerator FadeInMusic(AudioClip clip, float duration)
    {
        // Fade out current
        if (_musicSource.isPlaying)
        {
            float startVol = _musicSource.volume;
            float elapsed  = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / 0.4f);
                yield return null;
            }
        }

        _musicSource.clip   = clip;
        _musicSource.volume = 0f;
        _musicSource.pitch  = 1f;
        _musicSource.Play();

        float e = 0f;
        float targetVol = musicVolume * masterVolume;
        while (e < duration)
        {
            e += Time.deltaTime;
            _musicSource.volume = Mathf.Lerp(0f, targetVol, e / duration);
            yield return null;
        }
        _musicSource.volume = targetVol;
    }

    IEnumerator FadeOutMusic(float duration)
    {
        float startVol = _musicSource.volume;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        _musicSource.Stop();
    }

    // ─── Tension System ───────────────────────────────────────────────────────
    /// <summary>
    /// Call every frame from MatchManager.
    /// distanceRemaining: how many meters until collision (0–100m range for effect).
    /// </summary>
    public void UpdateTension(float distanceRemaining)
    {
        if (!_matchBgmPlaying) return;

        // Pitch ramp: 100m→0m maps to TENSION_PITCH_MIN→TENSION_PITCH_MAX
        float t     = Mathf.Clamp01(1f - (distanceRemaining / 100f));
        float pitch = Mathf.Lerp(TENSION_PITCH_MIN, TENSION_PITCH_MAX, t);

        if (_musicSource != null)
            _musicSource.pitch = pitch;
    }

    public void ResetTension()
    {
        if (_musicSource != null) _musicSource.pitch = 1f;
    }

    // ─── Volume Control (called from Settings UI) ─────────────────────────────
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
}
