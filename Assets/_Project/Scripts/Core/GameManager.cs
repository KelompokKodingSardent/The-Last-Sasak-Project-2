using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
    public string MainMenuScene = "MainMenu";
    public string HubScene = "Hub";
    public string CombatScene = "CombatTest";
    public string ForestRunScene = "ForestRun";

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public RunId CurrentRunId { get; private set; }
    public string ActiveRunSceneName { get; private set; }

    private int _runCounter;
    private string _pendingSceneTarget;
    private GameState _pendingStateAfterLoad;
    private bool _hasPendingTransition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Scene activeScene = SceneManager.GetActiveScene();
        SyncInitialStateFromScene(activeScene.name);
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        GameEvents.RaiseGameStateChanged(CurrentState);
    }

    public void StartGame()
    {
        BeginRunInScene(CombatScene);
    }

    public void StartForestRun()
    {
        BeginRunInScene(ForestRunScene);
    }

public void StartForest() => StartForestRun();

    public void BeginRunInScene(string runSceneName)
    {
        if (string.IsNullOrWhiteSpace(runSceneName))
            runSceneName = CombatScene;

        EndCurrentRunIfActive(RunEndReason.Restarted);

        ActiveRunSceneName = runSceneName;
        _runCounter++;
        CurrentRunId = new RunId(_runCounter);

        if (ExperienceSystem.Instance != null)
            ExperienceSystem.Instance.ResetRun();

        Time.timeScale = 1f;
        TransitionToScene(runSceneName, GameState.InRun);
    }

    public void RestartRun()
    {
        string targetRunScene = !string.IsNullOrWhiteSpace(ActiveRunSceneName)
            ? ActiveRunSceneName
            : SceneManager.GetActiveScene().name;

        if (targetRunScene == MainMenuScene || targetRunScene == HubScene)
            targetRunScene = CombatScene;

        BeginRunInScene(targetRunScene);
    }

    public void LoadHub()
    {
        EndCurrentRunIfActive(RunEndReason.AbandonedToHub);
        Time.timeScale = 1f;
        TransitionToScene(HubScene, GameState.Hub);
    }

    public void OpenHub() => LoadHub();

    public void ReturnToMainMenu()
    {
        EndCurrentRunIfActive(RunEndReason.AbandonedToMenu);
        Time.timeScale = 1f;
        TransitionToScene(MainMenuScene, GameState.MainMenu);
    }

    public void CompleteRunVictory()
    {
        if (CurrentState != GameState.InRun)
            return;

        EndCurrentRunIfActive(RunEndReason.Completed);
        SetState(GameState.Victory);
    }

    private void HandlePlayerDied()
    {
        if (CurrentState == GameState.GameOver)
            return;

        EndCurrentRunIfActive(RunEndReason.PlayerDied);
        SetState(GameState.GameOver);
    }

    private void EndCurrentRunIfActive(RunEndReason reason)
    {
        if (!CurrentRunId.IsValid)
            return;

        RunId endedRun = CurrentRunId;
        CurrentRunId = default;
        GameEvents.RaiseRunEnded(endedRun, reason);
    }

    private void TransitionToScene(string sceneName, GameState stateAfterLoad)
    {
        _pendingSceneTarget = sceneName;
        _pendingStateAfterLoad = stateAfterLoad;
        _hasPendingTransition = true;
        SceneManager.LoadScene(sceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_hasPendingTransition && scene.name == _pendingSceneTarget)
        {
            _hasPendingTransition = false;
            SetState(_pendingStateAfterLoad);

            if (_pendingStateAfterLoad == GameState.InRun && CurrentRunId.IsValid)
            {
                GameEvents.RaiseRunStarted(CurrentRunId, scene.name);
            }
            return;
        }

        // Mendukung pengujian langsung dari Editor (Play langsung di CombatTest / ForestRun / Hub)
        SyncInitialStateFromScene(scene.name);
    }

    private void SyncInitialStateFromScene(string sceneName)
    {
        if (sceneName == MainMenuScene)
        {
            SetState(GameState.MainMenu);
        }
        else if (sceneName == HubScene)
        {
            SetState(GameState.Hub);
        }
        else
        {
            ActiveRunSceneName = sceneName;
            if (!CurrentRunId.IsValid)
            {
                _runCounter++;
                CurrentRunId = new RunId(_runCounter);
            }
            SetState(GameState.InRun);
            GameEvents.RaiseRunStarted(CurrentRunId, sceneName);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}