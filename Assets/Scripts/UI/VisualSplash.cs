using UnityEngine;
using UnityEngine.SceneManagement;

public class VisualSplash : MonoBehaviour
{
    public void GoToLogin()
    {
        // Safe, direct transition to Login
        if (Application.CanStreamedLevelBeLoaded("LoginScene"))
            SceneManager.LoadScene("LoginScene");
        else
            SceneManager.LoadScene(1); // fallback to second scene
    }
}
