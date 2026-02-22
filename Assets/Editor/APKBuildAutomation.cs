using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

/// <summary>
/// APKBuildAutomation: One-click workflow to build APK and download from GitHub Actions.
/// 
/// Workflow:
/// 1. Commit current changes (if any)
/// 2. Push to GitHub main
/// 3. Poll GitHub Actions until build succeeds
/// 4. Download APK from GitHub Releases
/// 5. Move to Desktop for device flashing
/// 
/// Usage: Tools → APK Build & Download
/// </summary>

public class APKBuildAutomation
{
    private const string GITHUB_OWNER = "SneakyTurtleNL";
    private const string GITHUB_REPO = "vault-dash-unity";
    private const string APK_NAME = "vault-dash-release.apk";
    
    // IMPORTANT: Set GitHub token via environment variable before running
    // export GITHUB_TOKEN=your_token_here
    // Or set in Unity Editor: EditorPrefs.SetString("GitHubToken", "...")
    private static string GetGitHubToken()
    {
        // Try environment variable first
        string token = System.Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(token))
            return token;

        // Try EditorPrefs (set manually)
        token = EditorPrefs.GetString("GitHubToken", "");
        if (!string.IsNullOrEmpty(token))
            return token;

        throw new System.Exception("GitHub token not found. Set GITHUB_TOKEN environment variable or run: EditorPrefs.SetString(\"GitHubToken\", \"your_token\")");
    }
    
    private static string _projectRoot;
    private static string _desktopPath;

    [MenuItem("Tools/APK Build & Download")]
    public static void AutomatedAPKBuild()
    {
        _projectRoot = System.IO.Path.GetFullPath(Application.dataPath + "/../..");
        _desktopPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), APK_NAME);

        EditorUtility.DisplayProgressBar("APK Build Automation", "Starting workflow...", 0f);
        
        try
        {
            Step1_CommitAndPush();
            EditorUtility.DisplayProgressBar("APK Build Automation", "Waiting for GitHub Actions build...", 0.3f);
            
            WaitForGitHubActionsBuild();
            EditorUtility.DisplayProgressBar("APK Build Automation", "Downloading APK from releases...", 0.6f);
            
            DownloadAPKFromReleases();
            EditorUtility.DisplayProgressBar("APK Build Automation", "Complete!", 0.9f);
            
            UnityEngine.Debug.Log($"✅ APK ready at: {_desktopPath}");
            EditorUtility.DisplayDialog("Success", $"APK downloaded to:\n{_desktopPath}\n\nReady to flash on device!", "OK");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"❌ Build automation failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Build failed:\n{e.Message}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// Step 1: Commit and push to GitHub.
    /// </summary>
    private static void Step1_CommitAndPush()
    {
        UnityEngine.Debug.Log("📤 Committing and pushing to GitHub...");

        // Check if there are changes
        var gitStatus = RunCommand("git", "status --porcelain", _projectRoot);
        if (string.IsNullOrWhiteSpace(gitStatus))
        {
            UnityEngine.Debug.Log("No changes to commit. Pushing latest...");
            RunCommand("git", "push origin main", _projectRoot);
            return;
        }

        // Stage all changes
        RunCommand("git", "add -A", _projectRoot);

        // Commit
        string commitMsg = $"chore: automated APK build on {System.DateTime.Now:yyyy-MM-dd HH:mm}";
        RunCommand("git", $"commit -m \"{commitMsg}\"", _projectRoot);

        // Push
        RunCommand("git", "push origin main", _projectRoot);
        
        UnityEngine.Debug.Log("✅ Pushed to GitHub. GitHub Actions building now...");
    }

    /// <summary>
    /// Step 2: Poll GitHub Actions until build succeeds.
    /// Times out after 15 minutes.
    /// </summary>
    private static void WaitForGitHubActionsBuild()
    {
        UnityEngine.Debug.Log("⏳ Polling GitHub Actions...");

        int maxAttempts = 30; // 30 × 30 sec = 15 minutes
        int attempt = 0;

        while (attempt < maxAttempts)
        {
            var latestRun = GetLatestWorkflowRun();
            
            if (latestRun == null)
            {
                UnityEngine.Debug.LogWarning("⏳ Run not yet visible. Waiting...");
            }
            else if (latestRun.status == "completed")
            {
                if (latestRun.conclusion == "success")
                {
                    UnityEngine.Debug.Log($"✅ Build succeeded! Run ID: {latestRun.id}");
                    return;
                }
                else
                {
                    throw new System.Exception($"Build failed with conclusion: {latestRun.conclusion}");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"⏳ Status: {latestRun.status} ({attempt + 1}/{maxAttempts})");
            }

            System.Threading.Thread.Sleep(30000); // Wait 30 seconds
            attempt++;
        }

        throw new System.Exception("Build timed out after 15 minutes");
    }

    /// <summary>
    /// Step 3: Download APK from GitHub Releases.
    /// </summary>
    private static void DownloadAPKFromReleases()
    {
        UnityEngine.Debug.Log("📥 Downloading APK from releases...");

        string releaseUrl = $"https://github.com/{GITHUB_OWNER}/{GITHUB_REPO}/releases";
        string downloadUrl = GetLatestReleaseAPKUrl();

        if (string.IsNullOrEmpty(downloadUrl))
        {
            throw new System.Exception("Could not find APK in latest release");
        }

        UnityEngine.Debug.Log($"Downloading from: {downloadUrl}");

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"token {GITHUB_TOKEN}");
            client.DefaultRequestHeaders.Add("User-Agent", "VaultDashAPKDownloader");

            var response = client.GetAsync(downloadUrl).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new System.Exception($"Download failed: {response.StatusCode}");
            }

            var content = response.Content.ReadAsByteArrayAsync().Result;
            File.WriteAllBytes(_desktopPath, content);

            UnityEngine.Debug.Log($"✅ APK saved to: {_desktopPath}");
        }
    }

    /// <summary>
    /// Get latest workflow run from GitHub API.
    /// </summary>
    private static dynamic GetLatestWorkflowRun()
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"token {GetGitHubToken()}");
            client.DefaultRequestHeaders.Add("User-Agent", "VaultDashAPKDownloader");

            var url = $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/actions/runs?per_page=1";
            var response = client.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = response.Content.ReadAsStringAsync().Result;
            var runs = JsonUtility.FromJson<GitHubRunsResponse>(json);

            return runs.workflow_runs.Length > 0 ? runs.workflow_runs[0] : null;
        }
    }

    /// <summary>
    /// Get APK download URL from latest release.
    /// </summary>
    private static string GetLatestReleaseAPKUrl()
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"token {GetGitHubToken()}");
            client.DefaultRequestHeaders.Add("User-Agent", "VaultDashAPKDownloader");

            var url = $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/releases/latest";
            var response = client.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = response.Content.ReadAsStringAsync().Result;
            var release = JsonUtility.FromJson<GitHubRelease>(json);

            foreach (var asset in release.assets)
            {
                if (asset.name == APK_NAME)
                {
                    return asset.browser_download_url;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Run shell command and return output.
    /// </summary>
    private static string RunCommand(string command, string args, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new System.Exception($"Command failed: {command} {args}\n{error}");
            }

            return output;
        }
    }
}

// GitHub API response structures
[System.Serializable]
public class GitHubRunsResponse
{
    public GitHubRun[] workflow_runs;
}

[System.Serializable]
public class GitHubRun
{
    public int id;
    public string status;
    public string conclusion;
}

[System.Serializable]
public class GitHubRelease
{
    public GitHubAsset[] assets;
}

[System.Serializable]
public class GitHubAsset
{
    public string name;
    public string browser_download_url;
}
