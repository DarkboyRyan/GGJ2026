using UnityEngine;

public class PauseController : MonoBehaviour
{
    [Header("Optional UI")]
    [SerializeField] private GameObject pauseOverlay; 

    public bool IsPaused { get; private set; }

    void Awake()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        if (pauseOverlay) pauseOverlay.SetActive(false);
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        Time.timeScale = 0f;
        if (pauseOverlay) pauseOverlay.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = 1f;
        if (pauseOverlay) pauseOverlay.SetActive(false);
    }
}
