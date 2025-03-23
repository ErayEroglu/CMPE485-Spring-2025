using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStatus : MonoBehaviour
{
    public GameObject gameOverUI;
    public GameObject winUI;
    
    // Singleton pattern for easy access
    public static GameStatus instance;
    void Awake()
    {
        // Ensure only one GameManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keeps manager between scenes
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Hide end-game UIs at start
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (winUI != null) winUI.SetActive(false);
    }
    
    // Call this when player dies
    public void GameOver()
    {
        
        gameOverUI.SetActive(true); 
        Time.timeScale = 0; 
        
        
    }
    
    public void WinGame()
    {
        
        if (winUI != null)
        {
            winUI.SetActive(true);
            Time.timeScale = 0;
        }
    }
    
    // Replace your existing RestartGame method in GameManager
    public void RestartGame()
    {
        Debug.Log("RestartGame called");
    
        // Hide UI elements first
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (winUI != null) winUI.SetActive(false);
    
        // Reset time scale
        Time.timeScale = 1f;
    
        // Get current scene index
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
    
        // Reload scene using index
        SceneManager.LoadScene(currentSceneIndex, LoadSceneMode.Single);
    }
    
   
}