using UnityEngine;
using UnityEngine.UI;

public class VisualLoginManager : MonoBehaviour
{
    [Header("Drag your visual UI buttons here!")]
    public Button playButton;

    void Start()
    {
        // Tell the button what to do when clicked
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
        else
        {
            Debug.LogWarning("VisualLoginManager: You forgot to drag the Play Button into the Inspector!");
        }
    }

    public void OnPlayClicked()
    {
        Debug.Log("Play Button Clicked visually! Transitioning to Main Menu...");
        
        // Use the backend GameManager to change scenes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }
}
