using UnityEngine;
using UnityEngine.UI;

public class SimpleDrawCanvas : MonoBehaviour
{
    [Header("Canvas Texture")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    [Header("Brush")]
    public Color brushColor = Color.black;
    [Range(1, 60)] public int brushRadius = 8;

    private Texture2D tex;
    private Color32[] clearColors;
    private RawImage rawImage;
    private RectTransform rectTransform;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();

        tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        clearColors = new Color32[textureWidth * textureHeight];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = new Color32(255, 255, 255, 255); // 白底

        Clear();

        rawImage.texture = tex;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (TryGetTextureCoord(Input.mousePosition, out int x, out int y))
            {
                DrawCircle(x, y, brushRadius, brushColor);
                tex.Apply(false); 
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Clear();
        }
    }

    public void Clear()
    {
        tex.SetPixels32(clearColors);
        tex.Apply(false);
    }

    bool TryGetTextureCoord(Vector2 screenPos, out int x, out int y)
    {
        x = y = 0;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPos, null, out Vector2 localPoint))
            return false;

        Rect rect = rectTransform.rect;

        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;

        if (u < 0 || u > 1 || v < 0 || v > 1) return false;

        x = Mathf.FloorToInt(u * textureWidth);
        y = Mathf.FloorToInt(v * textureHeight);
        return true;
    }

    void DrawCircle(int cx, int cy, int r, Color col)
    {
        int r2 = r * r;

        int minX = Mathf.Max(cx - r, 0);
        int maxX = Mathf.Min(cx + r, textureWidth - 1);
        int minY = Mathf.Max(cy - r, 0);
        int maxY = Mathf.Min(cy + r, textureHeight - 1);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy * dy <= r2)
                {
                    tex.SetPixel(x, y, col);
                }
            }
        }
    }
}
