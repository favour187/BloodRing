using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BloodRing Game Bootstrap
/// Ensures the game always starts correctly from SplashLogo.
/// This script is automatically added to the first scene.
/// </summary>
public class BloodRingGameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Make sure this object survives scene loads
        DontDestroyOnLoad(gameObject);

        // If we are the very first scene and it's SplashLogo, everything is good.
        // If someone starts from another scene, we can still help.
        Debug.Log("🩸 BloodRing Bootstrap active. Root scene: " + SceneManager.GetActiveScene().name);
    }

    // Public method that can be called from any button
    public void GoToMainMenu()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
        else if (Application.CanStreamedLevelBeLoaded("MainLobby"))
            SceneManager.LoadScene("MainLobby");
        else
            SceneManager.LoadScene(1);
    }
}