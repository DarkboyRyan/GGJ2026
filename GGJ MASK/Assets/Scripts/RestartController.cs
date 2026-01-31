using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartController : MonoBehaviour
{
    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene"); 
    }
}
