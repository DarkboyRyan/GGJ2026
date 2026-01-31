using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "SampleScene"; // 你的游戏场景名

    [Header("Intro UI")]
    [SerializeField] private GameObject introTextObject;

    [Header("Lore UI")]
    [SerializeField] private GameObject loreTextObject;

    private bool introOpen = false;
    private bool loreOpen = false;

    void Start()
    {
        Time.timeScale = 1f;

        if (introTextObject) introTextObject.SetActive(false);
        if (loreTextObject) loreTextObject.SetActive(false);

        introOpen = false;
        loreOpen = false;
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ToggleIntro()
    {
        introOpen = !introOpen;
        if (introTextObject) introTextObject.SetActive(introOpen);
    }

    public void ToggleLore()
    {
        loreOpen = !loreOpen;
        if (loreTextObject) loreTextObject.SetActive(loreOpen);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
