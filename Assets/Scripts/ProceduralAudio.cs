using UnityEngine;
using System;

/// <summary>
/// ProceduralAudio — Generates placeholder AudioClips programmatically.
///
/// When no real WAV files are assigned to AudioManager, this generates
/// simple synthesized tones for testing so every SFX slot is functional.
///
/// Attach to the same GameObject as AudioManager and call GenerateAll()
/// from AudioManager.Awake() if clips are null.
///
/// Sounds generated:
///   footstep   → short low click (60ms, 180Hz square-ish)
///   jump       → rising chirp (0.15s, 200→600Hz)
///   crouch     → short thud (0.1s, 100Hz + noise)
///   laneChange → quick swish (0.12s, 800→200Hz)
///   coin       → bright ding (0.2s, 880Hz sine)
///   gem        → sparkle chord (0.3s, 1046+1318+1568Hz)
///   powerUp    → rising sweep (0.4s, 200→1200Hz)
///   obstacleHit→ crunch (0.25s, noise burst + thud)
///   opponentNear→ low rumble (0.3s, 80Hz)
///   victory    → major fanfare (1.2s)
///   defeat     → sad descend (0.8s)
///   menuBgm    → silent (0.01s loop — assign real music separately)
/// </summary>
public static class ProceduralAudio
{
    private const int SAMPLE_RATE = 44100;

    // ─── Public Factory ───────────────────────────────────────────────────────
    public static AudioClip Footstep()   => Click(0.06f, 180f, 0.6f);
    public static AudioClip Jump()       => Chirp(0.18f, 200f, 700f, 0.5f);
    public static AudioClip Crouch()     => Thud(0.10f, 100f);
    public static AudioClip LaneChange() => Chirp(0.12f, 800f, 250f, 0.4f);  // falling
    public static AudioClip Coin()       => Ding(0.22f, 880f);
    public static AudioClip Gem()        => Sparkle(0.30f);
    public static AudioClip PowerUp()    => Sweep(0.40f, 200f, 1200f);
    public static AudioClip ObstacleHit()=> Crunch(0.28f);
    public static AudioClip OpponentNear()=> Rumble(0.30f, 80f);
    public static AudioClip Victory()    => VictoryFanfare(1.4f);
    public static AudioClip Defeat()     => DefeatSting(0.9f);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Short click/footstep sound.</summary>
    static AudioClip Click(float duration, float freq, float volume)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / SAMPLE_RATE;
            float env  = Mathf.Exp(-t * 30f);             // fast decay
            data[i]    = volume * env * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
        }

        return MakeClip("Footstep", data);
    }

    /// <summary>Chirp — frequency glide from startFreq to endFreq.</summary>
    static AudioClip Chirp(float duration, float startFreq, float endFreq, float volume)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];
        float phase  = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Clamp01(1f - t / duration);
            float f   = Mathf.Lerp(startFreq, endFreq, t / duration);
            phase    += f / SAMPLE_RATE;
            data[i]   = volume * env * Mathf.Sin(2f * Mathf.PI * phase);
        }

        return MakeClip("Chirp", data);
    }

    /// <summary>Thud — short low-frequency burst.</summary>
    static AudioClip Thud(float duration, float freq)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(42);

        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / SAMPLE_RATE;
            float env  = Mathf.Exp(-t * 40f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise= (float)(rng.NextDouble() * 2.0 - 1.0) * 0.3f;
            data[i]    = 0.6f * env * (tone + noise);
        }

        return MakeClip("Thud", data);
    }

    /// <summary>Bright sine ding for coins.</summary>
    static AudioClip Ding(float duration, float freq)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 8f);
            data[i]   = 0.5f * env * Mathf.Sin(2f * Mathf.PI * freq * t);
        }

        return MakeClip("Ding", data);
    }

    /// <summary>Multi-frequency sparkle for gems.</summary>
    static AudioClip Sparkle(float duration)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];
        float[] freqs = { 1046.5f, 1318.5f, 1568f, 2093f };

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 6f);
            float sum = 0f;
            foreach (float f in freqs)
                sum += Mathf.Sin(2f * Mathf.PI * f * t);
            data[i] = 0.15f * env * sum;
        }

        return MakeClip("Sparkle", data);
    }

    /// <summary>Frequency sweep (power-up).</summary>
    static AudioClip Sweep(float duration, float startFreq, float endFreq)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];
        float phase  = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-(t - duration) * 5f);
            float f   = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t / duration, 0.5f));
            phase    += f / SAMPLE_RATE;
            data[i]   = 0.5f * env * Mathf.Sin(2f * Mathf.PI * phase);
        }

        return MakeClip("Sweep", data);
    }

    /// <summary>Obstacle hit crunch — noise + low freq.</summary>
    static AudioClip Crunch(float duration)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(99);

        for (int i = 0; i < samples; i++)
        {
            float t     = (float)i / SAMPLE_RATE;
            float env   = Mathf.Exp(-t * 15f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float tone  = Mathf.Sin(2f * Mathf.PI * 90f * t);
            data[i]     = 0.7f * env * (noise * 0.6f + tone * 0.4f);
        }

        return MakeClip("Crunch", data);
    }

    /// <summary>Low rumble for opponent near.</summary>
    static AudioClip Rumble(float duration, float freq)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Sin(Mathf.PI * t / duration); // bell curve
            data[i]   = 0.3f * env * Mathf.Sin(2f * Mathf.PI * freq * t);
        }

        return MakeClip("Rumble", data);
    }

    /// <summary>Simple major fanfare: C-E-G-C arpeggio then chord.</summary>
    static AudioClip VictoryFanfare(float duration)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];

        // C4=261.63, E4=329.63, G4=392, C5=523.25
        float[] notes  = { 261.63f, 329.63f, 392f, 523.25f, 659.25f };
        float   noteLen = 0.18f;

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            int   ni  = Mathf.Min((int)(t / noteLen), notes.Length - 1);
            float nt  = t - ni * noteLen;  // time within note
            float env = Mathf.Exp(-nt * 4f) * Mathf.Clamp01(nt * 50f);

            // Strum chord at end
            float sum = 0f;
            if (t >= noteLen * notes.Length)
            {
                float ct  = t - noteLen * notes.Length;
                float cenv= Mathf.Exp(-ct * 2.5f);
                foreach (float f in notes)
                    sum += cenv * Mathf.Sin(2f * Mathf.PI * f * t) / notes.Length;
            }
            else
            {
                sum = env * Mathf.Sin(2f * Mathf.PI * notes[ni] * t);
            }

            data[i] = 0.55f * sum;
        }

        return MakeClip("Victory", data);
    }

    /// <summary>Descending defeat sting.</summary>
    static AudioClip DefeatSting(float duration)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] data = new float[samples];
        float phase  = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 2.5f);
            float f   = Mathf.Lerp(400f, 150f, Mathf.Pow(t / duration, 0.6f));
            phase    += f / SAMPLE_RATE;
            data[i]   = 0.4f * env * Mathf.Sin(2f * Mathf.PI * phase);
        }

        return MakeClip("Defeat", data);
    }

    // ─── Utility ──────────────────────────────────────────────────────────────
    static AudioClip MakeClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }
}

/// <summary>
/// AudioManagerBootstrap — Fills AudioManager with procedural clips on Awake
/// if the Inspector clips are null.  Drop this on the same GO as AudioManager.
/// </summary>
[DefaultExecutionOrder(-100)]  // Before AudioManager.Awake
public class AudioManagerBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Wait one frame so AudioManager.Awake() has already run
        StartCoroutine(FillClips());
    }

    System.Collections.IEnumerator FillClips()
    {
        yield return null;  // wait one frame

        AudioManager am = AudioManager.Instance;
        if (am == null) yield break;

        // Only fill slots that are null (real assets take priority)
        if (am.footstepClip   == null) am.footstepClip    = ProceduralAudio.Footstep();
        if (am.jumpClip       == null) am.jumpClip         = ProceduralAudio.Jump();
        if (am.crouchClip     == null) am.crouchClip       = ProceduralAudio.Crouch();
        if (am.laneChangeClip == null) am.laneChangeClip   = ProceduralAudio.LaneChange();
        if (am.coinCollectClip== null) am.coinCollectClip  = ProceduralAudio.Coin();
        if (am.gemCollectClip == null) am.gemCollectClip   = ProceduralAudio.Gem();
        if (am.powerUpClip    == null) am.powerUpClip      = ProceduralAudio.PowerUp();
        if (am.obstacleHitClip== null) am.obstacleHitClip  = ProceduralAudio.ObstacleHit();
        if (am.opponentNearClip==null) am.opponentNearClip = ProceduralAudio.OpponentNear();
        if (am.victoryFanfareClip==null) am.victoryFanfareClip = ProceduralAudio.Victory();
        if (am.defeatStingClip== null) am.defeatStingClip  = ProceduralAudio.Defeat();

        Debug.Log("[AudioManagerBootstrap] Procedural audio clips injected for any null slots.");
    }
}
