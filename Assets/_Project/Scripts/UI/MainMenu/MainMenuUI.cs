using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void StartCombatTest()
    {
        GameManager.Instance?.StartGame();
    }

    public void StartForestRun()
    {
        GameManager.Instance?.StartForest();
    }

    public void OpenHub()
    {
        GameManager.Instance?.OpenHub();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
