using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// SceneBootstrapper — Modified for VISUAL WORKFLOW!
/// This script now ONLY creates the invisible background managers (GameManager, Audio, etc.).
/// It no longer creates the UI. You are now free to build your Canvas and UI visually in the Editor!
/// </summary>
public class SceneBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[Bootstrapper] Visual Workflow Bootstrapper initialized (Backend Only)");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureFirstScene()
    {
        BootstrapBackend(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootstrapBackend(scene.name);
    }

    private static void BootstrapBackend(string sceneName)
    {
        Debug.Log("[Bootstrapper] Loading Backend Systems for: " + sceneName);

        // Ensure persistent singletons exist so the game logic still works!
        EnsureSingleton<GameManager>("[GameManager]");
        EnsureSingleton<AudioManager>("[AudioManager]");
        EnsureSingleton<BackendAPI>("[BackendAPI]");
        EnsureSingleton<LiveOpsManager>("[LiveOpsManager]");
        EnsureSingleton<StoreRotationManager>("[StoreRotationManager]");

        // UI Generation has been completely removed.
        // Build your UI in the Unity Hierarchy!
    }

    private static void EnsureSingleton<T>(string name) where T : MonoBehaviour
    {
        if (Object.FindObjectOfType<T>() == null)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<T>();
            Object.DontDestroyOnLoad(go);
        }
    }
}
