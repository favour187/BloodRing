using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class VisualMainMenu : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;

    void Start()
    {
        // 1. Display the name we saved in the Login Screen
        if (playerNameText != null) {
            playerNameText.text = "Welcome, " + PlayerPrefs.GetString("PlayerNickname", "Guest") + "!";
        }

        // 2. Automatically spawn DJ Neon (Striker) visually in the menu!
        GameObject playerModel = BloodRing.Art.BloodRingArtLibrary.GetCharacterModel("Striker");
        if (playerModel != null)
        {
            // Position him in front of the camera
            playerModel.transform.position = new Vector3(0, -2f, 5f);
            playerModel.transform.rotation = Quaternion.Euler(0, 180, 0); // Face the camera
            
            // Give him a gun!
            Transform rightArm = playerModel.transform.Find("RightArm") ?? playerModel.transform;
            GameObject gun = BloodRing.Art.BloodRingArtLibrary.GetWeaponModel("AK47");
            if (gun != null) {
                gun.transform.SetParent(rightArm);
                gun.transform.localPosition = new Vector3(0, -0.1f, 0.3f);
            }
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("MatchmakingScene");
    }

    public void Logout()
    {
        SceneManager.LoadScene("LoginScene");
    }
}
