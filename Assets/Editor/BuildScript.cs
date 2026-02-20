using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// BuildScript — Headless Unity CLI build for Vault Dash Android APK.
///
/// Usage (GitHub Actions / CI):
///   Unity.exe -quit -batchmode -projectPath . \
///             -executeMethod BuildScript.BuildAndroid \
///             -logFile build.log
///
/// Environment overrides:
///   BUILD_OUTPUT_PATH  → output folder (default: Builds/Android)
///   APP_VERSION        → overrides PlayerSettings.bundleVersion
///   BUILD_NUMBER       → overrides versionCode
/// </summary>
public static class BuildScript
{
    // ─── Defaults ─────────────────────────────────────────────────────────────
    private const string DEFAULT_OUTPUT  = "Builds/Android";
    private const string APK_NAME        = "VaultDash.apk";
    private const string BUNDLE_ID       = "com.vaultdash.game";

    // ─── Android Build Entry Point ────────────────────────────────────────────
    public static void BuildAndroid()
    {
        Debug.Log("[BuildScript] ═══ Vault Dash Android Build ═══");

        string outputPath = GetEnv("BUILD_OUTPUT_PATH", DEFAULT_OUTPUT);
        string version    = GetEnv("APP_VERSION",       PlayerSettings.bundleVersion);
        string buildNum   = GetEnv("BUILD_NUMBER",      "1");

        // Ensure output directory exists
        Directory.CreateDirectory(outputPath);
        string apkPath = Path.Combine(outputPath, APK_NAME);

        // Player settings
        PlayerSettings.applicationIdentifier = BUNDLE_ID;
        PlayerSettings.bundleVersion          = version;
        PlayerSettings.Android.bundleVersionCode = int.TryParse(buildNum, out int bn) ? bn : 1;

        // Android settings
        PlayerSettings.Android.minSdkVersion     = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion  = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

        // Keystore (injected via CI env or Unity secrets)
        string keystoreBase64 = GetEnv("ANDROID_KEYSTORE_BASE64", "");
        string keystorePass   = GetEnv("ANDROID_KEYSTORE_PASS",   "");
        string keyAlias       = GetEnv("ANDROID_KEYALIAS_NAME",   "");
        string keyAliasPass   = GetEnv("ANDROID_KEYALIAS_PASS",   "");

        if (!string.IsNullOrEmpty(keystoreBase64))
        {
            string keystorePath = Path.Combine(outputPath, "vault-dash.keystore");
            File.WriteAllBytes(keystorePath, Convert.FromBase64String(keystoreBase64));

            PlayerSettings.Android.useCustomKeystore  = true;
            PlayerSettings.Android.keystoreName       = keystorePath;
            PlayerSettings.Android.keystorePass       = keystorePass;
            PlayerSettings.Android.keyaliasName       = keyAlias;
            PlayerSettings.Android.keyaliasPass       = keyAliasPass;
            Debug.Log("[BuildScript] Keystore configured.");
        }
        else
        {
            Debug.LogWarning("[BuildScript] No keystore — building debug APK.");
            PlayerSettings.Android.useCustomKeystore = false;
        }

        // Collect scenes
        string[] scenes = GetEnabledScenes();
        Debug.Log($"[BuildScript] Scenes ({scenes.Length}): {string.Join(", ", scenes)}");
        Debug.Log($"[BuildScript] Output: {apkPath}");
        Debug.Log($"[BuildScript] Version: {version} (code: {buildNum})");

        // Build options
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = apkPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        // Execute build
        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] ✅ BUILD SUCCEEDED — {summary.totalSize / 1024 / 1024} MB → {apkPath}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[BuildScript] ❌ BUILD FAILED — Result: {summary.result}");
            Debug.LogError($"[BuildScript] Errors: {summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();
    }

    static string GetEnv(string key, string fallback)
    {
        string val = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrEmpty(val) ? fallback : val;
    }
}
