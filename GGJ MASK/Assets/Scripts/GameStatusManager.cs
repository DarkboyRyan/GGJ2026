using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

public class GameStatusManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timerDuration = 120f; // 2 minutes in seconds
    public Image timerBar;
    public TextMeshProUGUI timerText;

    [Header("Timer State")]
    private float timeRemaining = 120f;

    public GameObject drawingGuideTop;


    void Start()
    {
        timeRemaining = timerDuration;
        drawingGuideTop.SetActive(false);
    }

    void Update()
    {

        timeRemaining -= Time.deltaTime;

        // Update UI elements
        UpdateTimerUI();

        // Notify listeners of timer updat

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerUI(); // Final update to show 0:00
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            drawingGuideTop.SetActive(true);

        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            drawingGuideTop.SetActive(false);
        }

    }

    void UpdateTimerUI()
    {
        // Update timer bar fill amount (1.0 = full, 0.0 = empty)
        if (timerBar != null)
        {
            timerBar.fillAmount = timeRemaining / timerDuration;
        }

        // Update timer text with formatted time
        if (timerText != null)
        {
            timerText.text = GetTimeRemainingFormatted();
        }
    }


    public string GetTimeRemainingFormatted()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}
