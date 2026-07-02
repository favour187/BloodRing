using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// One-click Android configuration and build entry point for the upgraded mobile 3D client.
/// Keeps existing files intact and only writes Unity build settings / generated config.
/// </summary>
public static class MobileAndroidBuildConfigurator
{
    private static readonly string[] Scenes = new string[]
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
    [MenuItem("Build/Configure Mobile 3D Android")]
    public static void ConfigureAndroidProject()
    {
        EditorBuildSettings.scenes = Scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();

        PlayerSettings.companyName = "Blood Ring Studio";
        PlayerSettings.productName = "Blood Ring 3D";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.bloodring.mobile3d");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.MTRendering = true;
        PlayerSettings.SplashScreen.show = false;

        QualitySettings.vSyncCount = 0;
        QualitySettings.lodBias = 0.75f;
        QualitySettings.shadowDistance = 45f;
        QualitySettings.shadowCascades = 1;
        QualitySettings.pixelLightCount = 1;
        QualitySettings.realtimeReflectionProbes = false;

        Directory.CreateDirectory("Assets/StreamingAssets");
        string configPath = "Assets/StreamingAssets/mobile_client_config.json";
        File.WriteAllText(configPath, "{\n" +
            "  \"target\": \"Android\",\n" +
            "  \"additionalTargets\": [\"iOS\", \"Standalone\"],\n" +
            "  \"graphics\": {\n" +
            "    \"fps\": 60,\n" +
            "    \"lod\": true,\n" +
            "    \"occlusionCulling\": true,\n" +
            "    \"objectPooling\": true,\n" +
            "    \"mobileShaders\": true,\n" +
            "    \"textureCompression\": \"ASTC/ETC2 via Android build settings\"\n" +
            "  }\n" +
            "}\n");

        AssetDatabase.Refresh();
        Debug.Log("Blood Ring mobile 3D Android configuration applied.");
    }

    [MenuItem("Build/Build Mobile 3D Android APK")]
    public static void BuildMobile3DAndroidApk()
    {
        ConfigureAndroidProject();
        Directory.CreateDirectory("build/Android");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "build/Android/BloodRing3D.apk",
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4HC
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Mobile 3D Android build result: " + report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception("Android build failed: " + report.summary.result);
        }
    }
}


