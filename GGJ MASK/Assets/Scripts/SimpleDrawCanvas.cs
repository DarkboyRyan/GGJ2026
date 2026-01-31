using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SimpleDrawCanvas : MonoBehaviour
{
    [Header("Canvas Texture")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    [Header("Brush")]
    public Color brushColor = Color.black;
    [Range(1, 60)] public int brushRadius = 8;
    public Image brushTargetIndicator;
    [Range(0f, 1f)] public float brushSmoothing = 0.1f; // Smoothing factor for brush movement (lower = smoother)

    [Header("Shivering Effect")]
    public bool isShivering = false;
    [Range(0f, 200f)] public float shiveringIntensity = 10f; // Amount of random offset in pixels
    [Range(0f, 100f)] public float shiveringSpeed = 30f; // How fast the shivering changes
    [Range(0.1f, 10f)] public float noiseScale = 1f; // Scale of the noise pattern (higher = finer detail)

    [Header("Temperature System")]
    [Range(0f, 100f)] public float maxTemperature = 100f; // Maximum temperature (no shivering)
    [Range(0f, 20f)] public float temperatureDropRate = 2f; // Temperature drop per second when drawing
    [Range(0f, 20f)] public float temperatureRecoveryRate = 1f; // Temperature recovery per second when not drawing
    [Range(0f, 100f)] public float shiveringThreshold = 50f; // Temperature below which shivering occurs
    [Range(1f, 10f)] public float minShiveringGap = 1f; // Minimum gap between shivers in seconds (most frequent)
    [Range(1f, 10f)] public float maxShiveringGap = 5f; // Maximum gap between shivers in seconds (least frequent)
    [Range(0.1f, 10f)] public float shiveringDuration = 3f; // How long shivering stays on in seconds

    [Header("Massive Handshake")]
    [Range(0.1f, 5f)] public float massiveHandshakeDuration = 1f; // How long massive handshake lasts
    [Range(10f, 800f)] public float massiveHandshakeIntensity = 50f; // Intensity of the massive offset

    public GameObject textureObject;

    public float temperature; // Secret temperature value
    private float lastShiverTime = 0f; // Time when last shiver period started
    private float shiveringEndTime = 0f; // Time when current shivering period ends
    private float massiveHandshakeEndTime = 0f; // Time when massive handshake ends

    private Texture2D tex;
    private Color32[] clearColors;
    private RawImage rawImage;
    private RectTransform rectTransform;
    public Canvas canvas;
    public GameObject colorLayer;
    private Camera canvasCamera;
    private Canvas mainCanvas; // Main canvas where brushTargetIndicator is
    private RectTransform indicatorRectTransform;
    private Vector2 currentDrawingPos; // Current smoothed drawing position
    private Vector2 targetDrawingPos; // Target drawing position (from mouse)

    void Awake()
    {
        InitializeComponents();
        InitializeTexture();
        InitializeTemperature();
    }

    void InitializeComponents()
    {
        rawImage = textureObject.GetComponent<RawImage>();
        rectTransform = textureObject.GetComponent<RectTransform>();
        canvasCamera = GetCanvasCamera(canvas);
        colorLayer.SetActive(false);

        if (brushTargetIndicator != null)
        {
            indicatorRectTransform = brushTargetIndicator.GetComponent<RectTransform>();
            mainCanvas = brushTargetIndicator.GetComponentInParent<Canvas>();
        }
    }

    void InitializeTexture()
    {
        tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        clearColors = new Color32[textureWidth * textureHeight];
        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = new Color32(0, 0, 0, 0);

        Clear();
        rawImage.texture = tex;
        rawImage.color = new Color(1, 1, 1, 1);

        // Initialize drawing positions
        currentDrawingPos = Vector2.zero;
        targetDrawingPos = Vector2.zero;
    }

    void InitializeTemperature()
    {
        temperature = maxTemperature;
    }

    Camera GetCanvasCamera(Canvas targetCanvas)
    {
        if (targetCanvas == null) return Camera.main;

        if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null; // Screen space overlay doesn't need a camera

        return targetCanvas.worldCamera ?? Camera.main;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Update temperature based on drawing state
        bool isDrawing = mouse.leftButton.isPressed;
        UpdateTemperature(isDrawing);

        Vector2 mousePos = mouse.position.ReadValue();
        targetDrawingPos = mousePos;

        // Check for massive handshake first (overrides everything)
        if (IsMassiveHandshakeActive())
        {
            targetDrawingPos = ApplyMassiveHandshake(mousePos);
        }
        // Apply normal shivering effect if enabled and temperature is low enough
        else if (isShivering && temperature < shiveringThreshold)
        {
            targetDrawingPos = ApplyShivering(mousePos);
        }

        // Initialize current position on first frame
        if (currentDrawingPos == Vector2.zero && targetDrawingPos != Vector2.zero)
        {
            currentDrawingPos = targetDrawingPos;
        }

        // Smoothly lerp current position towards target position (frame-rate independent)
        float lerpSpeed = 1f - Mathf.Pow(1f - brushSmoothing, Time.deltaTime * 60f); // Normalize to 60fps
        currentDrawingPos = Vector2.Lerp(currentDrawingPos, targetDrawingPos, lerpSpeed);

        // Update brush target indicator position
        UpdateBrushIndicator(currentDrawingPos);

        if (isDrawing)
        {
            if (TryGetTextureCoord(currentDrawingPos, out int x, out int y))
            {
                DrawCircle(x, y, brushRadius, brushColor);
                tex.Apply(false);
            }
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            Clear();
        }
    }

    /// <summary>
    /// Updates temperature based on drawing state.
    /// </summary>
    void UpdateTemperature(bool isDrawing)
    {
        if (isDrawing)
        {
            // Temperature drops when drawing
            temperature -= temperatureDropRate * Time.deltaTime;
            temperature = Mathf.Max(0f, temperature);
        }
        else
        {
            // Temperature recovers when not drawing
            temperature += temperatureRecoveryRate * Time.deltaTime;
            temperature = Mathf.Min(maxTemperature, temperature);
        }
    }

    public void SetBrushColor(Color c)
    {
        brushColor = c;
    }
    public void SetBrushRadius(int r)
    {
        brushRadius = r;
    }

    public void Clear()
    {
        tex.SetPixels32(clearColors);
        tex.Apply(false);
    }

    /// <summary>
    /// Gets the current drawing texture. Used by TextureComparator for comparison.
    /// </summary>
    public Texture2D GetTexture()
    {
        return tex;
    }

    bool TryGetTextureCoord(Vector2 screenPos, out int x, out int y)
    {
        x = y = 0;

        // Use the canvas camera for coordinate conversion (works for both screen space and world space)
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPos, canvasCamera, out Vector2 localPoint))
            return false;

        Rect rect = rectTransform.rect;

        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;

        if (u < 0 || u > 1 || v < 0 || v > 1) return false;

        x = Mathf.FloorToInt(u * textureWidth);
        y = Mathf.FloorToInt(v * textureHeight);
        return true;
    }

    /// <summary>
    /// Updates the brush target indicator position on the main canvas.
    /// </summary>
    void UpdateBrushIndicator(Vector2 screenPos)
    {
        if (brushTargetIndicator == null || indicatorRectTransform == null || mainCanvas == null)
            return;

        Camera mainCamera = GetCanvasCamera(mainCanvas);
        RectTransform mainCanvasRect = mainCanvas.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvasRect, screenPos, mainCamera, out Vector2 localPoint))
        {
            indicatorRectTransform.localPosition = localPoint;
        }
    }

    /// <summary>
    /// Applies shivering effect to the mouse position (discontinuous based on temperature).
    /// </summary>
    Vector2 ApplyShivering(Vector2 originalPos)
    {
        float normalizedTemp = GetNormalizedTemperature();
        float currentGap = Mathf.Lerp(minShiveringGap, maxShiveringGap, normalizedTemp);
        bool isShiveringActive = Time.time < shiveringEndTime;

        // Start new shivering period if gap has elapsed
        if (!isShiveringActive && (Time.time - lastShiverTime) >= currentGap)
        {
            lastShiverTime = Time.time;
            shiveringEndTime = Time.time + shiveringDuration;
            isShiveringActive = true;
        }

        if (!isShiveringActive) return originalPos;

        // Calculate intensity: lower temperature = higher intensity (0.5x to 2x)
        // Also scale with brush size: larger brush = more intense shivering (brush size 1-3)
        float brushSizeMultiplier = 1f + (brushRadius - 1) * 0.5f; // Size 1: 1x, Size 2: 1.5x, Size 3: 2x
        float currentIntensity = shiveringIntensity * Mathf.Lerp(2f, 0.5f, normalizedTemp) * brushSizeMultiplier;
        Vector2 offset = GetShiveringOffset(currentIntensity);

        return originalPos + offset;
    }

    float GetNormalizedTemperature()
    {
        return Mathf.Clamp01(temperature / shiveringThreshold);
    }

    Vector2 GetShiveringOffset(float intensity)
    {
        float scaledTime = Time.time * shiveringSpeed * noiseScale;
        float offsetX = (Mathf.PerlinNoise(scaledTime, 0f) - 0.5f) * 2f * intensity;
        float offsetY = (Mathf.PerlinNoise(0f, scaledTime) - 0.5f) * 2f * intensity;
        return new Vector2(offsetX, offsetY);
    }

    void DrawCircle(int cx, int cy, int r, Color col)
    {
        Color32 paintColor = CreatePaintColor(col);

        if (r == 1)
        {
            DrawSinglePixel(cx, cy, paintColor);
        }
        else if (r == 2)
        {
            DrawDiamondBrush(cx, cy, paintColor);
        }
        else
        {
            DrawCircularBrush(cx, cy, r, paintColor);
        }
    }

    Color32 CreatePaintColor(Color col)
    {
        return new Color32(
            (byte)(col.r * 255),
            (byte)(col.g * 255),
            (byte)(col.b * 255),
            255 // Alpha = 1 (fully opaque)
        );
    }

    void DrawSinglePixel(int x, int y, Color32 color)
    {
        if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
            tex.SetPixel(x, y, color);
    }

    void DrawDiamondBrush(int cx, int cy, Color32 color)
    {
        int minX = Mathf.Max(cx - 1, 0);
        int maxX = Mathf.Min(cx + 1, textureWidth - 1);
        int minY = Mathf.Max(cy - 1, 0);
        int maxY = Mathf.Min(cy + 1, textureHeight - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= 1)
                    tex.SetPixel(x, y, color);
            }
        }
    }

    void DrawCircularBrush(int cx, int cy, int r, Color32 color)
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
                    tex.SetPixel(x, y, color);
            }
        }
    }

    /// <summary>
    /// Triggers a massive handshake that overrides all other shivering effects.
    /// Call this from other scripts: simpleDrawCanvas.MassiveHandShake();
    /// </summary>
    public void MassiveHandShake()
    {
        massiveHandshakeEndTime = Time.time + massiveHandshakeDuration;
    }

    bool IsMassiveHandshakeActive()
    {
        return Time.time < massiveHandshakeEndTime;
    }

    Vector2 ApplyMassiveHandshake(Vector2 originalPos)
    {
        // Use noise for massive handshake (speed stays the same)
        float time = Time.time * shiveringSpeed;
        float scaledTime = time * noiseScale;

        // Scale intensity with brush size: larger brush = more intense massive handshake (brush size 1-3)
        float brushSizeMultiplier = 1f + (brushRadius - 1) * 0.5f; // Size 1: 1x, Size 2: 1.5x, Size 3: 2x
        float currentIntensity = massiveHandshakeIntensity * brushSizeMultiplier;

        // Generate large random offset (intensity scales, speed doesn't)
        float offsetX = (Mathf.PerlinNoise(scaledTime, 0f) - 0.5f) * 2f * currentIntensity;
        float offsetY = (Mathf.PerlinNoise(0f, scaledTime) - 0.5f) * 2f * currentIntensity;

        return originalPos + new Vector2(offsetX, offsetY);
    }

    void OnDestroy()
    {
        // Clean up texture when object is destroyed
        if (tex != null)
        {
            Destroy(tex);
            tex = null;
        }
    }
}
