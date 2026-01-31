using UnityEngine;
using UnityEngine.UI;

public class PaletteButton : MonoBehaviour
{
    public SimpleDrawCanvas canvas;   
    public Color color = Color.black;

    void Awake()
    {
        var img = GetComponent<Image>();
        if (img != null)
            img.color = color;

        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (canvas != null)
                canvas.SetBrushColor(color);
        });
    }
}
