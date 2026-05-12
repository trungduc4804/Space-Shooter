using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private bool isGameOver = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("GAME OVER");
        Time.timeScale = 0f;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver();
    }
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        SceneManager.LoadScene(gameScene);
    }
    public void QuitGame()
    {   
        Debug.Log("Quit Game");
        Application.Quit();
    }
    public bool IsGameOver => isGameOver;
}
