using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// BloodRing Apex Royale - Professional Android Build Script
/// </summary>
public class BuildScript
{
    [MenuItem("BloodRing/Build Android APK")]
    public static void BuildAndroid()
    {
        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("  🩸 BLOODRING APEX ROYALE — Android APK Build");
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
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        // ── Set App Icon ─────────────────────────────────────────────────────
        Texture2D appIcon = Resources.Load<Texture2D>("AppIcon");
        if (appIcon != null)
        {
            int[] iconSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
            Texture2D[] icons = new Texture2D[iconSizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = appIcon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
        }

        // ── Build all scenes ─────────────────────────────────────────────────
        string[] scenes = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories)
                                   .Where(f => !f.EndsWith(".meta"))
                                   .ToArray();

        string outputDir = "build/Android";
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        string outputPath = Path.Combine(outputDir, "BloodRingApex.apk");

        Debug.Log("Building BloodRing to: " + outputPath);
        Debug.Log("Total scenes: " + scenes.Length);

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
            Debug.Log("  ✅ BLOODRING BUILD SUCCEEDED!");
            Debug.Log("  APK: " + outputPath);
            Debug.Log("═══════════════════════════════════════════════════════════════");
        }
        else
        {
            Debug.LogError("❌ BLOODRING BUILD FAILED!");
        }
    }
}