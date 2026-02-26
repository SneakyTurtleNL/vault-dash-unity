using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// LatencyValidator — Nakama RTT check before competitive matches.
///
/// FLOW:
///   1. Call ValidateLatency(onResult) before starting matchmaking.
///   2. Sends up to PING_ATTEMPTS pings via Nakama RPC "ping_pong_echo".
///   3. Measures average RTT; calls back with LatencyResult.
///   4. If RTT > FallbackThresholdMs  → recommend ghost/bot match.
///   5. If RTT > WarnThresholdMs      → show "high latency" warning but allow.
///   6. If RTT ≤ WarnThresholdMs      → all clear.
///
/// NAKAMA RPC:
///   Name     : "ping_pong_echo"
///   Payload  : {"ts": <millisecond timestamp>}
///   Response : {"ts": <same timestamp>}   (echo back so we can diff)
///   The RPC is already deployed on the Nakama server.
///
/// REQUIREMENTS:
///   • NAKAMA_AVAILABLE scripting define + Nakama Unity SDK.
///   • MatchManager (or caller) must provide an authenticated ISession.
///
/// INTEGRATION:
///   MatchManager.cs calls LatencyValidator.Instance.ValidateLatency(...)
///   before kicking off FindMatch().
/// </summary>
public class LatencyValidator : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static LatencyValidator Instance { get; private set; }

    // ─── Config ───────────────────────────────────────────────────────────────
    [Header("Thresholds (ms)")]
    [Tooltip("RTT above this → force ghost/bot match.")]
    public int FallbackThresholdMs = 150;

    [Tooltip("RTT above this (but below Fallback) → show warning UI.")]
    public int WarnThresholdMs = 100;

    [Header("Ping Config")]
    [Tooltip("Number of pings to average.")]
    public int PingAttempts = 3;

    [Tooltip("Timeout per ping (seconds).")]
    public float PingTimeoutSec = 3f;

    // ─── Result ───────────────────────────────────────────────────────────────
    public enum LatencyStatus { Good, Acceptable, High, Failed }

    public struct LatencyResult
    {
        public LatencyStatus Status;
        public float         AverageRttMs;
        public bool          ShouldUseBotFallback;

        public override string ToString() =>
            $"Latency({Status}, RTT={AverageRttMs:F0}ms, BotFallback={ShouldUseBotFallback})";
    }

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Validates network latency to the Nakama server.
    /// <param name="onResult">Callback with latency result.</param>
    /// </summary>
    public void ValidateLatency(Action<LatencyResult> onResult)
    {
        StartCoroutine(PingRoutine(onResult));
    }

    // ─── Ping Coroutine ───────────────────────────────────────────────────────

    private IEnumerator PingRoutine(Action<LatencyResult> onResult)
    {
#if NAKAMA_AVAILABLE
        yield return PingViaNakama(onResult);
#else
        yield return PingViaUnityPing(onResult);
#endif
    }

#if NAKAMA_AVAILABLE
    private IEnumerator PingViaNakama(Action<LatencyResult> onResult)
    {
        // Retrieve session from MatchManager
        if (MatchManager.Instance == null)
        {
            Debug.LogWarning("[LatencyValidator] MatchManager not found — using Unity ping fallback.");
            yield return PingViaUnityPing(onResult);
            yield break;
        }

        var session = MatchManager.Instance.GetSession();
        var client  = MatchManager.Instance.GetClient();

        if (session == null || client == null)
        {
            Debug.LogWarning("[LatencyValidator] No Nakama session yet — using Unity ping fallback.");
            yield return PingViaUnityPing(onResult);
            yield break;
        }

        float totalRtt = 0f;
        int   success  = 0;

        for (int i = 0; i < PingAttempts; i++)
        {
            long   sentMs  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string payload = $"{{\"ts\":{sentMs}}}";

            bool   done    = false;
            float  rtt     = -1f;

            client.RpcAsync(session, "ping_pong_echo", payload)
                  .ContinueWith(t =>
                  {
                      if (!t.IsFaulted)
                      {
                          long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                          rtt  = nowMs - sentMs;
                          success++;
                      }
                      done = true;
                  });

            float waited = 0f;
            while (!done && waited < PingTimeoutSec)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!done)
            {
                Debug.LogWarning($"[LatencyValidator] Ping {i+1} timed out.");
            }
            else if (rtt >= 0)
            {
                totalRtt += rtt;
                Debug.Log($"[LatencyValidator] Ping {i+1}: {rtt}ms");
            }

            yield return new WaitForSeconds(0.2f);  // small gap between pings
        }

        float avgRtt = success > 0 ? totalRtt / success : float.MaxValue;
        onResult?.Invoke(BuildResult(avgRtt, success == 0));
    }
#endif

    /// <summary>
    /// Fallback: Unity built-in Ping to the Nakama host for RTT estimation.
    /// Less precise than RPC echo but works without an authenticated session.
    /// </summary>
    private IEnumerator PingViaUnityPing(Action<LatencyResult> onResult)
    {
        string host = "127.0.0.1";
        if (MatchManager.Instance != null) host = MatchManager.Instance.nakamaHost;

        float totalRtt = 0f;
        int   success  = 0;

        for (int i = 0; i < PingAttempts; i++)
        {
            var ping   = new Ping(host);
            float waited = 0f;

            while (!ping.isDone && waited < PingTimeoutSec)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (ping.isDone && ping.time >= 0)
            {
                totalRtt += ping.time;
                success++;
                Debug.Log($"[LatencyValidator] UnityPing {i+1}: {ping.time}ms");
            }
            else
            {
                Debug.LogWarning($"[LatencyValidator] UnityPing {i+1} failed.");
            }

            ping.DestroyPing();
            yield return new WaitForSeconds(0.2f);
        }

        float avgRtt = success > 0 ? totalRtt / success : float.MaxValue;
        onResult?.Invoke(BuildResult(avgRtt, success == 0));
    }

    // ─── Result Builder ───────────────────────────────────────────────────────

    private LatencyResult BuildResult(float avgRttMs, bool failed)
    {
        if (failed)
        {
            return new LatencyResult
            {
                Status               = LatencyStatus.Failed,
                AverageRttMs         = float.MaxValue,
                ShouldUseBotFallback = true
            };
        }

        LatencyStatus status;
        bool          botFallback;

        if (avgRttMs > FallbackThresholdMs)
        {
            status      = LatencyStatus.High;
            botFallback = true;
        }
        else if (avgRttMs > WarnThresholdMs)
        {
            status      = LatencyStatus.Acceptable;
            botFallback = false;
        }
        else
        {
            status      = LatencyStatus.Good;
            botFallback = false;
        }

        var result = new LatencyResult
        {
            Status               = status,
            AverageRttMs         = avgRttMs,
            ShouldUseBotFallback = botFallback
        };

        Debug.Log($"[LatencyValidator] {result}");
        return result;
    }
}
