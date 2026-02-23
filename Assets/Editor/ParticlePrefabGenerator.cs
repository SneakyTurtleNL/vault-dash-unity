using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// ParticlePrefabGenerator — Unity Editor Script
/// 
/// Genereert alle 8 particle prefabs voor het Vault Dash cosmetics systeem.
/// Run via: Tools > Vault Dash > Generate Particle Prefabs
/// 
/// Gegenereerde prefabs:
///   Resources/Particles/Auras/aura_blue_energy
///   Resources/Particles/Auras/aura_golden_light
///   Resources/Particles/Auras/aura_purple_mystical
///   Resources/Particles/Trails/trail_fire
///   Resources/Particles/Trails/trail_ice
///   Resources/Particles/Footsteps/footstep_stars
///   Resources/Particles/Spawn/spawn_burst_cosmic
///   Resources/Particles/LevelUp/levelup_burst_legendary
/// </summary>
public class ParticlePrefabGenerator : Editor
{
    [MenuItem("Tools/Vault Dash/Generate Particle Prefabs")]
    public static void GenerateAll()
    {
        int created = 0;

        created += CreateAuraBlueEnergy()    ? 1 : 0;
        created += CreateAuraGoldenLight()   ? 1 : 0;
        created += CreateAuraPurpleMystical()? 1 : 0;
        created += CreateTrailFire()         ? 1 : 0;
        created += CreateTrailIce()          ? 1 : 0;
        created += CreateFootstepStars()     ? 1 : 0;
        created += CreateSpawnBurstCosmic()  ? 1 : 0;
        created += CreateLevelUpLegendary()  ? 1 : 0;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Particle Prefabs Gegenereerd",
            $"✅ {created}/8 particle prefabs aangemaakt in Assets/Resources/Particles/\n\n" +
            "Prefabs zijn direct bruikbaar via Resources.Load() in CharacterParticleCosmetics.",
            "OK"
        );

        Debug.Log($"[ParticlePrefabGenerator] {created}/8 prefabs gegenereerd.");
    }

    // ─────────────────────────────────────────────
    // AURAS
    // ─────────────────────────────────────────────

    static bool CreateAuraBlueEnergy()
    {
        const string path = "Assets/Resources/Particles/Auras/aura_blue_energy.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("aura_blue_energy");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        // Main
        var main = ps.main;
        main.loop              = true;
        main.duration          = 2f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(0.3f, 0.6f, 1f, 0.8f),
            new Color(0.5f, 0.8f, 1f, 0.4f)
        );
        main.maxParticles      = 80;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;

        // Emission
        var emission = ps.emission;
        emission.rateOverTime  = 30f;

        // Shape — ring around character
        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Circle;
        shape.radius           = 0.4f;
        shape.radiusThickness  = 0.1f;

        // Color over lifetime — fade out
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(new Color(0.3f, 0.7f, 1f), 0f),   new GradientColorKey(new Color(0.6f, 0.9f, 1f), 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        // Size over lifetime — shrink
        var size               = ps.sizeOverLifetime;
        size.enabled           = true;
        AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        size.size              = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer
        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 10;

        SavePrefab(go, path);
        return true;
    }

    static bool CreateAuraGoldenLight()
    {
        const string path = "Assets/Resources/Particles/Auras/aura_golden_light.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("aura_golden_light");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop              = true;
        main.duration          = 3f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.84f, 0f, 0.9f),
            new Color(1f, 0.95f, 0.4f, 0.5f)
        );
        main.maxParticles      = 60;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = -0.05f; // float upward slightly

        var emission = ps.emission;
        emission.rateOverTime  = 20f;

        // Shape — sphere around character
        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Sphere;
        shape.radius           = 0.45f;

        // Color over lifetime
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),   new GradientColorKey(new Color(1f, 0.6f, 0f), 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 10;

        SavePrefab(go, path);
        return true;
    }

    static bool CreateAuraPurpleMystical()
    {
        const string path = "Assets/Resources/Particles/Auras/aura_purple_mystical.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("aura_purple_mystical");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop              = true;
        main.duration          = 2f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(0.8f, 0.2f, 1f, 0.7f),
            new Color(0.5f, 0.1f, 0.8f, 0.4f)
        );
        main.maxParticles      = 70;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime  = 25f;

        // Shape — cone rising up
        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Circle;
        shape.radius           = 0.35f;

        // Color over lifetime
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(new Color(0.9f, 0.4f, 1f), 0f),   new GradientColorKey(new Color(0.4f, 0.1f, 0.8f), 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 10;

        SavePrefab(go, path);
        return true;
    }

    // ─────────────────────────────────────────────
    // TRAILS
    // ─────────────────────────────────────────────

    static bool CreateTrailFire()
    {
        const string path = "Assets/Resources/Particles/Trails/trail_fire.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("trail_fire");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop              = true;
        main.duration          = 1f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.5f, 0f, 0.9f),
            new Color(1f, 0.9f, 0f, 0.7f)
        );
        main.maxParticles      = 50;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = -0.3f; // fire rises

        var emission = ps.emission;
        emission.rateOverTime  = 40f;

        // Shape — cone behind character
        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Cone;
        shape.angle            = 15f;
        shape.radius           = 0.1f;

        // Color over lifetime — orange → red → black
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  {
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.1f, 0f), 1f)
            },
            new GradientAlphaKey[]  { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 10;

        SavePrefab(go, path);
        return true;
    }

    static bool CreateTrailIce()
    {
        const string path = "Assets/Resources/Particles/Trails/trail_ice.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("trail_ice");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop              = true;
        main.duration          = 1f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(0.5f, 0.9f, 1f, 0.9f),
            new Color(0.8f, 1f, 1f, 0.6f)
        );
        main.maxParticles      = 60;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = 0.05f; // slight fall

        var emission = ps.emission;
        emission.rateOverTime  = 35f;

        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Cone;
        shape.angle            = 10f;
        shape.radius           = 0.08f;

        // Color — light blue → white → transparent
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  {
                new GradientColorKey(new Color(0.6f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.95f, 1f), 1f)
            },
            new GradientAlphaKey[]  { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 10;

        SavePrefab(go, path);
        return true;
    }

    // ─────────────────────────────────────────────
    // FOOTSTEPS
    // ─────────────────────────────────────────────

    static bool CreateFootstepStars()
    {
        const string path = "Assets/Resources/Particles/Footsteps/footstep_stars.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("footstep_stars");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop              = false; // One-shot per footstep
        main.duration          = 0.3f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.3f, 1f),
            new Color(1f, 0.8f, 0.1f, 0.8f)
        );
        main.maxParticles      = 12;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = 0.3f;

        var emission = ps.emission;
        emission.rateOverTime  = 0f;
        // Burst: 5-8 stars per footstep
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 5, 8)
        });

        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Circle;
        shape.radius           = 0.05f;

        // Color — gold → fade
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(new Color(1f, 0.95f, 0.3f), 0f),  new GradientColorKey(new Color(1f, 0.7f, 0.1f), 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 0.8f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 10;

        SavePrefab(go, path);
        return true;
    }

    // ─────────────────────────────────────────────
    // SPAWN BURST
    // ─────────────────────────────────────────────

    static bool CreateSpawnBurstCosmic()
    {
        const string path = "Assets/Resources/Particles/Spawn/spawn_burst_cosmic.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("spawn_burst_cosmic");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop              = false; // one-shot burst
        main.duration          = 0.5f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(0.6f, 0.2f, 1f, 0.9f),
            new Color(0.9f, 0.5f, 1f, 0.8f)
        );
        main.maxParticles      = 60;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime  = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 40, 60)
        });

        // Shape — sphere explosion outward
        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Sphere;
        shape.radius           = 0.1f;

        // Color — purple → pink → fade
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.8f, 0.4f, 1f), 0.3f),
                new GradientColorKey(new Color(0.4f, 0.1f, 0.8f), 1f)
            },
            new GradientAlphaKey[]  { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 15;

        SavePrefab(go, path);
        return true;
    }

    // ─────────────────────────────────────────────
    // LEVEL-UP BURST
    // ─────────────────────────────────────────────

    static bool CreateLevelUpLegendary()
    {
        const string path = "Assets/Resources/Particles/LevelUp/levelup_burst_legendary.prefab";
        if (File.Exists(path)) { Debug.Log($"[Skip] {path} bestaat al"); return false; }

        var go = new GameObject("levelup_burst_legendary");
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop              = false;
        main.duration          = 1f;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize         = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor        = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.84f, 0f, 1f),
            new Color(1f, 1f, 0.5f, 0.8f)
        );
        main.maxParticles      = 100;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = -0.2f;

        var emission = ps.emission;
        emission.rateOverTime  = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f,  60, 80),
            new ParticleSystem.Burst(0.3f, 20, 30)  // second wave
        });

        // Shape — upward cone explosion
        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Cone;
        shape.angle            = 60f;
        shape.radius           = 0.3f;

        // Color — white flash → gold → fade
        var col = ps.colorOverLifetime;
        col.enabled            = true;
        var gradient           = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.9f, 0.1f), 0.2f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.7f),
                new GradientColorKey(new Color(0.8f, 0.2f, 0f), 1f)
            },
            new GradientAlphaKey[]  { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color              = new ParticleSystem.MinMaxGradient(gradient);

        renderer.renderMode    = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder  = 15;

        SavePrefab(go, path);
        return true;
    }

    // ─────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────

    static void SavePrefab(GameObject go, string path)
    {
        // Ensure directory exists
        string dir = System.IO.Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        Debug.Log($"[ParticlePrefabGenerator] ✅ Aangemaakt: {path}");
    }
}
