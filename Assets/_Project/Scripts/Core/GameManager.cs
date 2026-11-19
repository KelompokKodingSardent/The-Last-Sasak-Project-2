using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;
    public string CombatScene = "CombatTest";
    public string ForestScene = "ForestRun";
    public string MenuScene = "MainMenu";
    public string HubScene = "Hub";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene == "CombatTest" || scene == "ForestRun")
            SetState(GameState.InRun);
        else if (scene == "Hub" || scene == "MainMenu")
            SetState(GameState.MainMenu);
    }

    void OnEnable()
    {
        GameEvents.OnPlayerDied += GameOver;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerDied -= GameOver;
    }

    public void StartGame()
    {
        ExperienceSystem.Instance?.ResetRun();
        SetState(GameState.Loading);
        LoadNamed(CombatScene);
        SetState(GameState.InRun);
    }

    public void StartForest()
    {
        ExperienceSystem.Instance?.ResetRun();
        SetState(GameState.Loading);
        LoadNamed(ForestScene);
        SetState(GameState.InRun);
    }

    public void OpenHub()
    {
        SetState(GameState.MainMenu);
        LoadNamed(HubScene);
    }

    public void GameOver()
    {
        if (CurrentGameState == GameState.GameOver)
            return;
        SetState(GameState.GameOver);
    }

    public void ReturnToMainMenu()
    {
        SetState(GameState.MainMenu);
        LoadNamed(MenuScene);
    }

    public void RestartRun()
    {
        StartGame();
    }

    public static void LoadNamed(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        SceneManager.LoadScene($"Assets/_Project/Scenes/{sceneName}.unity");
    }

    void SetState(GameState state)
    {
        CurrentGameState = state;
        GameEvents.RaiseGameStateChanged(state);
    }
}
