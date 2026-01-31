using UnityEngine;
using UnityEngine.UI;

public class UIStars : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Image starImage;
    void Awake()
    {
        starImage = GetComponent<Image>();
        SetStarEmpty();
    }
    public void SetStarEmpty()
    {
        var color = starImage.color;
        color.a = .2f;
        starImage.color = color;
    }
    // Update is called once per frame

    public void SetStarFull()
    {
        var color = starImage.color;
        color.a = 1f;
        starImage.color = color;
    }
}
