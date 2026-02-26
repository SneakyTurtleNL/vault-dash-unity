using System;
using UnityEngine;

/// <summary>
/// PowerUpConfig — Authoritative configuration for all power-ups.
///
/// BALANCE CHANGES (post-alpha review):
///   Reverse      : duration 2.0s → 1.2s  (was overpowered)
///   SlowMo       : renamed "TimeWarp"; now slows OPPONENT (not self)
///   Steal        : now steals opponent's active power-up (was 100 coin reward)
///   Pulse        : full revive → 50% HP restoration (half shield)
///   ObstacleSpawn: adds 0.5s warning visual before obstacle appears
///
/// USAGE:
///   PowerUpConfig.Get(PowerUpType.Reverse).Duration
///   PowerUpConfig.Get(PowerUpType.TimeWarp).Effect
///
/// INTEGRATION:
///   - PowerUpManager.cs  : reads config on activation
///   - ObstacleManager.cs : reads ObstacleSpawn.WarningDuration
///   - Player.cs          : reads Pulse.HpRestorePercent
/// </summary>
[CreateAssetMenu(fileName = "PowerUpConfig", menuName = "VaultDash/PowerUpConfig")]
public class PowerUpConfig : ScriptableObject
{
    // ─── Singleton / Resources ────────────────────────────────────────────────
    private static PowerUpConfig _instance;
    public  static PowerUpConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<PowerUpConfig>("PowerUpConfig")
                            ?? CreateDefault();
            return _instance;
        }
    }

    // ─── Power-Up Types ───────────────────────────────────────────────────────
    public enum PowerUpType
    {
        Magnet,         // Collect nearby coins automatically
        Shield,         // Block one obstacle hit
        SpeedBoost,     // Temporarily increase run speed
        Reverse,        // Reverse opponent's controls
        TimeWarp,       // Slow OPPONENT's world (was "SlowMo")
        Steal,          // Steal opponent's active power-up
        Pulse,          // Restore 50% HP (was full revive)
        ObstacleSpawn,  // Spawn extra obstacles in opponent's lane
        Coin2x,         // Double coins for duration
        Ghost           // Phase through obstacles for duration
    }

    // ─── Data Struct ──────────────────────────────────────────────────────────
    [Serializable]
    public class PowerUpData
    {
        public PowerUpType Type;
        public string      DisplayName;
        public string      Description;
        public string      IconKey;

        [Tooltip("Active duration in seconds (0 = instant).")]
        public float Duration;

        [Tooltip("Numeric strength value (speed multiplier, hp%, coin multiplier, etc.)")]
        public float EffectValue;

        [Tooltip("Warning pre-visual duration before effect applies (e.g. ObstacleSpawn).")]
        public float WarningDuration;

        [Tooltip("If true, effect targets the OPPONENT; if false, targets self.")]
        public bool TargetsOpponent;

        [Tooltip("Rarity weight for drop pool (1=common, 5=rare).")]
        public int DropWeight;
    }

    // ─── Inspector Array ──────────────────────────────────────────────────────
    [Header("Power-Up Definitions")]
    public PowerUpData[] PowerUps = new PowerUpData[]
    {
        new PowerUpData
        {
            Type            = PowerUpType.Magnet,
            DisplayName     = "Magnet",
            Description     = "Attracts all nearby coins for the duration.",
            IconKey         = "powerup_magnet",
            Duration        = 5f,
            EffectValue     = 8f,     // attract radius (units)
            WarningDuration = 0f,
            TargetsOpponent = false,
            DropWeight      = 5
        },
        new PowerUpData
        {
            Type            = PowerUpType.Shield,
            DisplayName     = "Shield",
            Description     = "Absorbs the next obstacle hit.",
            IconKey         = "powerup_shield",
            Duration        = 0f,     // instant, persists until hit
            EffectValue     = 1f,     // charges
            WarningDuration = 0f,
            TargetsOpponent = false,
            DropWeight      = 4
        },
        new PowerUpData
        {
            Type            = PowerUpType.SpeedBoost,
            DisplayName     = "Speed Boost",
            Description     = "Increases run speed by 40% for 4s.",
            IconKey         = "powerup_speedboost",
            Duration        = 4f,
            EffectValue     = 1.4f,   // speed multiplier
            WarningDuration = 0f,
            TargetsOpponent = false,
            DropWeight      = 4
        },
        new PowerUpData
        {
            Type            = PowerUpType.Reverse,
            DisplayName     = "Reverse",
            Description     = "Reverses opponent's left/right controls for 1.2s.",
            IconKey         = "powerup_reverse",
            Duration        = 1.2f,   // ✅ CHANGED: was 2.0s
            EffectValue     = 1f,
            WarningDuration = 0f,
            TargetsOpponent = true,
            DropWeight      = 2
        },
        new PowerUpData
        {
            Type            = PowerUpType.TimeWarp,
            DisplayName     = "TimeWarp",             // ✅ RENAMED from SlowMo
            Description     = "Slows the OPPONENT's world for 3s.",  // ✅ targets opponent
            IconKey         = "powerup_timewarp",
            Duration        = 3f,
            EffectValue     = 0.5f,   // time scale for opponent (0.5 = half speed)
            WarningDuration = 0f,
            TargetsOpponent = true,   // ✅ CHANGED: was self-buff
            DropWeight      = 3
        },
        new PowerUpData
        {
            Type            = PowerUpType.Steal,
            DisplayName     = "Steal",
            Description     = "Steals opponent's currently active power-up.",  // ✅ CHANGED
            IconKey         = "powerup_steal",
            Duration        = 0f,     // instant
            EffectValue     = 0f,     // ✅ CHANGED: no longer 100 coin bonus
            WarningDuration = 0f,
            TargetsOpponent = true,
            DropWeight      = 2
        },
        new PowerUpData
        {
            Type            = PowerUpType.Pulse,
            DisplayName     = "Pulse",
            Description     = "Restores 50% HP (half shield charge).",  // ✅ CHANGED: was full revive
            IconKey         = "powerup_pulse",
            Duration        = 0f,     // instant
            EffectValue     = 50f,    // ✅ 50% HP restore (was 100%)
            WarningDuration = 0f,
            TargetsOpponent = false,
            DropWeight      = 2
        },
        new PowerUpData
        {
            Type            = PowerUpType.ObstacleSpawn,
            DisplayName     = "Obstacle Spawn",
            Description     = "Spawns 2 obstacles in opponent's lane (with 0.5s warning).",
            IconKey         = "powerup_obstacle",
            Duration        = 0f,
            EffectValue     = 2f,     // number of obstacles
            WarningDuration = 0.5f,   // ✅ NEW: 0.5s warning flash before obstacle appears
            TargetsOpponent = true,
            DropWeight      = 2
        },
        new PowerUpData
        {
            Type            = PowerUpType.Coin2x,
            DisplayName     = "Coin ×2",
            Description     = "Doubles coin collection for 6s.",
            IconKey         = "powerup_coin2x",
            Duration        = 6f,
            EffectValue     = 2f,
            WarningDuration = 0f,
            TargetsOpponent = false,
            DropWeight      = 4
        },
        new PowerUpData
        {
            Type            = PowerUpType.Ghost,
            DisplayName     = "Ghost",
            Description     = "Phase through obstacles for 2s.",
            IconKey         = "powerup_ghost",
            Duration        = 2f,
            EffectValue     = 1f,
            WarningDuration = 0f,
            TargetsOpponent = false,
            DropWeight      = 2
        }
    };

    // ─── Lookup ───────────────────────────────────────────────────────────────

    private System.Collections.Generic.Dictionary<PowerUpType, PowerUpData> _lookup;

    /// <summary>Returns config for the given power-up type.</summary>
    public static PowerUpData Get(PowerUpType type)
    {
        var inst = Instance;
        if (inst._lookup == null) inst.BuildLookup();
        inst._lookup.TryGetValue(type, out var data);
        return data;
    }

    private void BuildLookup()
    {
        _lookup = new System.Collections.Generic.Dictionary<PowerUpType, PowerUpData>();
        foreach (var pu in PowerUps)
            _lookup[pu.Type] = pu;
    }

    // ─── Default Factory ──────────────────────────────────────────────────────

    private static PowerUpConfig CreateDefault()
    {
        var cfg = CreateInstance<PowerUpConfig>();
        Debug.LogWarning("[PowerUpConfig] No asset found in Resources — using default values.");
        return cfg;
    }

    private void OnEnable() => BuildLookup();
}
