using System;
using UnityEngine;

/// <summary>
/// GameMode — Defines rules per game mode with P2W protection.
///
/// P2W SPLIT (key design principle):
///   • Ranked / PvP : NO revive available.  Fair competition.
///   • Solo / Casual: Revive is allowed (rewarded-ad or gem purchase).
///   • Leaderboards  : NO revive flag stored per run; server rejects revived scores.
///
/// USAGE:
///   GameMode.Current = GameMode.Create(GameModeType.Ranked);
///   if (GameMode.Current.AllowsRevive) { ... }
///
/// INTEGRATION:
///   - Player.cs    : check AllowsRevive before showing revive prompt
///   - VictoryScreen: check IsReviveContaminated before uploading to leaderboard
///   - MatchManager : set GameMode before starting a match
/// </summary>
public class GameMode
{
    // ─── Mode Types ───────────────────────────────────────────────────────────
    public enum GameModeType
    {
        Solo,       // Practice / offline — revive allowed
        Casual,     // Casual 1v1 — revive allowed, no trophy stakes
        Ranked,     // Competitive — NO revive, trophy-ranked
        PvP,        // Tournament PvP — NO revive, prize pool
        Spectate    // Watch-only — no interaction
    }

    // ─── Properties ───────────────────────────────────────────────────────────
    public GameModeType Type            { get; private set; }
    public string       DisplayName     { get; private set; }
    public string       IconKey         { get; private set; }

    /// <summary>True if the player may use a revive in this mode.</summary>
    public bool AllowsRevive            { get; private set; }

    /// <summary>True if results from this mode may appear on leaderboards.</summary>
    public bool CountsForLeaderboard    { get; private set; }

    /// <summary>True if trophies are awarded/deducted for this mode.</summary>
    public bool AwardsTrophies          { get; private set; }

    /// <summary>True if THIS run used a revive (set at runtime; disqualifies from LB).</summary>
    public bool IsReviveContaminated    { get; private set; } = false;

    // ─── Global Current Mode ─────────────────────────────────────────────────
    private static GameMode _current;

    /// <summary>The active game mode for the current session.</summary>
    public static GameMode Current
    {
        get => _current ?? (_current = Create(GameModeType.Solo));
        set => _current = value;
    }

    // ─── Factory ─────────────────────────────────────────────────────────────

    /// <summary>Create a game mode with correct P2W rules applied.</summary>
    public static GameMode Create(GameModeType type)
    {
        var gm = new GameMode { Type = type };

        switch (type)
        {
            case GameModeType.Solo:
                gm.DisplayName          = "Solo";
                gm.IconKey              = "icon_solo";
                gm.AllowsRevive         = true;   // ✅ revive OK in practice
                gm.CountsForLeaderboard = false;
                gm.AwardsTrophies       = false;
                break;

            case GameModeType.Casual:
                gm.DisplayName          = "Casual";
                gm.IconKey              = "icon_casual";
                gm.AllowsRevive         = true;   // ✅ revive OK in casual
                gm.CountsForLeaderboard = false;
                gm.AwardsTrophies       = false;
                break;

            case GameModeType.Ranked:
                gm.DisplayName          = "Ranked";
                gm.IconKey              = "icon_ranked";
                gm.AllowsRevive         = false;  // ❌ NO revive in ranked
                gm.CountsForLeaderboard = true;
                gm.AwardsTrophies       = true;
                break;

            case GameModeType.PvP:
                gm.DisplayName          = "PvP";
                gm.IconKey              = "icon_pvp";
                gm.AllowsRevive         = false;  // ❌ NO revive in PvP
                gm.CountsForLeaderboard = true;
                gm.AwardsTrophies       = true;
                break;

            case GameModeType.Spectate:
                gm.DisplayName          = "Spectate";
                gm.IconKey              = "icon_spectate";
                gm.AllowsRevive         = false;
                gm.CountsForLeaderboard = false;
                gm.AwardsTrophies       = false;
                break;

            default:
                goto case GameModeType.Solo;
        }

        return gm;
    }

    // ─── Runtime Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Called when the player triggers a revive.
    /// Returns false (and logs a warning) if the mode does not allow revives.
    /// </summary>
    public bool TryRevive()
    {
        if (!AllowsRevive)
        {
            Debug.LogWarning($"[GameMode] Revive attempted in {Type} — not allowed.");
            return false;
        }

        IsReviveContaminated = true;
        Debug.Log($"[GameMode] Revive used in {Type}. Run is leaderboard-ineligible.");
        return true;
    }

    /// <summary>
    /// Checks whether the score from this run is eligible for leaderboard submission.
    /// Criteria: mode must count for LB AND no revive was used.
    /// </summary>
    public bool IsLeaderboardEligible()
    {
        if (!CountsForLeaderboard)  return false;
        if (IsReviveContaminated)   return false;
        return true;
    }

    // ─── Convenience Checks ──────────────────────────────────────────────────

    public bool IsCompetitive  => Type == GameModeType.Ranked || Type == GameModeType.PvP;
    public bool IsRelaxed      => Type == GameModeType.Solo   || Type == GameModeType.Casual;

    public override string ToString() =>
        $"GameMode({Type}, Revive={AllowsRevive}, LB={CountsForLeaderboard}, Contaminated={IsReviveContaminated})";
}
