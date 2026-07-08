using UnityEngine;
using UnityEngine.SceneManagement;

public class VisualSplash : MonoBehaviour
{
    public void GoToLogin()
    {
        // Safe, direct transition to Login
        SceneManager.LoadScene("LoginScene");
    }
}
