using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("  BLOODRING APEX ROYALE — Android APK Build (v5.0)");
        Debug.Log("═══════════════════════════════════════════════════════════════");

        // ── Force Player Settings ────────────────────────────────────────────
        PlayerSettings.companyName = "BloodRingStudio";
        PlayerSettings.productName = "BloodRing Apex Royale";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.BloodRingStudio.BloodRing");
        PlayerSettings.bundleVersion = "5.0.0";
        PlayerSettings.Android.bundleVersionCode = 5;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

        // Force IL2CPP for production builds
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        // Landscape orientation
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        // ── Set App Icon ─────────────────────────────────────────────────────
        Texture2D appIcon = Resources.Load<Texture2D>("AppIcon");
        if (appIcon != null)
        {
            // Set all icon sizes
            int[] iconSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
            Texture2D[] icons = new Texture2D[iconSizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = appIcon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
            Debug.Log("AppIcon assigned for all Android icon sizes (" + iconSizes.Length + " sizes).");
        }
        else
        {
            Debug.LogWarning("AppIcon not found in Resources — using default Unity icon.");
        }

        // ── Ensure Scenes Are Configured ─────────────────────────────────────
        string[] scenes = new string[]
        {
            "Assets/Scenes/SplashLogo.unity",
            "Assets/Scenes/StartupInitialization.unity",
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/LoadingScene.unity",
            "Assets/Scenes/MainLobby.unity",
            "Assets/Scenes/EventsPage.unity",
            "Assets/Scenes/StoreScene.unity",
            "Assets/Scenes/CharacterPage.unity",
            "Assets/Scenes/InventoryScene.unity",
            "Assets/Scenes/SettingsScene.unity",
            "Assets/Scenes/MatchmakingScene.unity",
            "Assets/Scenes/WaitingIsland.unity",
            "Assets/Scenes/MainBattleRoyaleMap.unity",
            "Assets/Scenes/TrainingGround.unity",
            "Assets/Scenes/ResultVictoryScreen.unity",
            "Assets/Scenes/ClanSocial.unity",
            "Assets/Scenes/ProfileScene.unity",
            "Assets/Scenes/ReconnectErrorScene.unity",
            "Assets/Scenes/SplashScreen.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/CharacterSelect.unity",
            "Assets/Scenes/LobbyScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/GameOver.unity"
        };

        // Verify all scene files exist
        foreach (string scene in scenes)
        {
            if (!File.Exists(scene))
            {
                Debug.LogWarning("Scene file missing (will still attempt build): " + scene);
            }
        }

        // ── Build ────────────────────────────────────────────────────────────
        string outputDir = "build/Android";
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            Debug.Log("Created output directory: " + outputDir);
        }

        string outputPath = Path.Combine(outputDir, "BloodRingApex.apk");

        Debug.Log("Building to: " + outputPath);
        Debug.Log("Scenes: " + string.Join(", ", scenes));

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4HC
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log("  BUILD SUCCEEDED!");
            Debug.Log("  APK: " + outputPath);
            Debug.Log("  Size: " + (new FileInfo(outputPath).Length / (1024f * 1024f)).ToString("F1") + " MB");
            Debug.Log("  Total build time: " + report.summary.totalTime);
            Debug.Log("═══════════════════════════════════════════════════════════════");
        }
        else
        {
            Debug.LogError("═══════════════════════════════════════════════════════════════");
            Debug.LogError("  BUILD FAILED!");
            Debug.LogError("  Result: " + report.summary.result);
            Debug.LogError("  Errors: " + report.summary.totalErrors);
            Debug.LogError("═══════════════════════════════════════════════════════════════");

            // Log individual errors
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == UnityEngine.LogType.Error)
                        Debug.LogError("[Build Error] " + msg.content);
                }
            }
        }
    }
}


