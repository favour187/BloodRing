using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public class EditorBuildSettingsConfigurator
{
    static EditorBuildSettingsConfigurator()
    {
        EditorApplication.delayCall += ConfigureBuildSettings;
    }

    private static void ConfigureBuildSettings()
    {
        string[] requiredScenes = new string[]
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

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        foreach (string scenePath in requiredScenes)
        {
            if (File.Exists(scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}


