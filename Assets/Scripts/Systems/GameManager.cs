using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Splash,
    MainMenu,
    CharacterSelect,
    Lobby,
    Game,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[GameManager]");
                instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public GameState currentState = GameState.Splash;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            DG.Tweening.DOTween.Init();
            // Trigger audio manager initialization
            AudioManager.Instance.SetZoneDamageActive(false);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        string sceneName = "";
        switch (newState)
        {
            case GameState.Splash: sceneName = "SplashScreen"; break;
            case GameState.MainMenu: sceneName = "MainMenu"; break;
            case GameState.CharacterSelect: sceneName = "CharacterSelect"; break;
            case GameState.Lobby: sceneName = "LobbyScene"; break;
            case GameState.Game: sceneName = "GameScene"; break;
            case GameState.GameOver: sceneName = "GameOver"; break;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneLoader.Instance.LoadScene(sceneName);
        }
    }

    public void StartGame()
    {
        ChangeState(GameState.CharacterSelect);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game Requested");
        Application.Quit();
    }
}


