using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public CanvasGroup Group;

    void Awake()
    {
        if (Group == null)
            Group = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleState;
    }

    void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleState;
    }

    void HandleState(GameState state)
    {
        SetVisible(state == GameState.GameOver);
    }

    public void Retry()
    {
        GameManager.Instance?.RestartRun();
    }

    public void BackToMenu()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }

    void SetVisible(bool visible)
    {
        if (Group == null)
            return;
        Group.alpha = visible ? 1f : 0f;
        Group.interactable = visible;
        Group.blocksRaycasts = visible;
    }
}
