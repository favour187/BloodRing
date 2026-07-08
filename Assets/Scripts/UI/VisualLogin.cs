using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class VisualLogin : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TextMeshProUGUI statusText;

    public void OnLoginButtonPressed()
    {
        if (usernameInput == null || statusText == null) return;
        
        string username = usernameInput.text;
        
        if (string.IsNullOrEmpty(username))
        {
            statusText.text = "Please enter a username!";
            statusText.color = Color.red;
            return;
        }

        statusText.text = "Connecting...";
        statusText.color = Color.yellow;

        // Save it to PlayerPrefs
        PlayerPrefs.SetString("PlayerNickname", username);
        PlayerPrefs.Save();
        
        Invoke("OnLoginSuccess", 1.0f);
    }

    private void OnLoginSuccess()
    {
        statusText.text = "Login successful!";
        statusText.color = Color.green;
        
        // Use standard Unity scene loading so it never gets stuck!
        SceneManager.LoadScene("MainMenu");
    }
}
