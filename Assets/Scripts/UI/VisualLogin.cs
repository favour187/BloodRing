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

        // Save it to PlayerPrefs (Mimicking the AuthManager flow)
        PlayerPrefs.SetString("PlayerNickname", username);
        PlayerPrefs.Save();
        
        Invoke("OnLoginSuccess", 1.0f); // Fake a 1-second network connection
    }

    private void OnLoginSuccess()
    {
        statusText.text = "Login successful!";
        statusText.color = Color.green;
        
        if (GameManager.Instance != null) {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        } else {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
