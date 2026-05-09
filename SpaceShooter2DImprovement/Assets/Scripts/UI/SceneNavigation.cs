using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
    public string gameSceneName = "GameLevel";
    public string menuSceneName = "MainMenu";

    public void StartNewGame()
    {
        GameManager.ResetScore();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void RestartGame()
    {
        GameManager.ResetScore();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
