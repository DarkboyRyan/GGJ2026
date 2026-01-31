using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TextureComparator : MonoBehaviour
{
    [Header("Reference")]
    public Image referenceImage;
    private Texture2D referenceTexture; // Fallback if Image is not used

    [Header("Mask")]
    public Image maskImage;

    [Header("Comparison Settings")]
    [Range(0f, 1f)] public float colorTolerance = 0.01f; // Tolerance for color comparison (0 = exact match)

    [Header("Canvas Reference")]
    public SimpleDrawCanvas drawCanvas;

    [Header("Score and Stars")]
    public UIStars[] stars;
    public float[] scoreTargets = new float[] { 40f, 60f, 70f };
    public float[] scoreBarPoints = new float[] { 0.4f, 0.65f, 0.9f };
    public Image scoreBar;
    [Range(0.1f, 2f)] public float scoreBarAnimationDuration = 0.5f;
    public Ease scoreBarEaseType = Ease.OutCubic;

    [SerializeField] float similarityPercentage = 0f;
    private bool[] starsFilled; // Track which stars have been filled
    private bool[] scoreBarSegmentsFilled; // Track which score bar segments have been filled
    private Tween currentScoreBarTween; // Current score bar animation tween

    // Cached resampled textures to avoid resampling every frame
    private Texture2D cachedResampledReference;
    private Texture2D cachedResampledMask;
    private int cachedDrawingWidth = -1;
    private int cachedDrawingHeight = -1;

    void Awake()
    {
        if (drawCanvas == null)
        {
            Debug.LogError("DrawCanvas is not assigned!");
            return;
        }

        // Initialize stars tracking
        if (stars != null && stars.Length > 0)
        {
            starsFilled = new bool[stars.Length];
            scoreBarSegmentsFilled = new bool[stars.Length];

            // Ensure scoreTargets array matches stars array length
            if (scoreTargets == null || scoreTargets.Length != stars.Length)
            {
                Debug.LogWarning("ScoreTargets array length doesn't match stars array. Resizing...");
                float[] newTargets = new float[stars.Length];
                for (int i = 0; i < stars.Length; i++)
                {
                    if (scoreTargets != null && i < scoreTargets.Length)
                        newTargets[i] = scoreTargets[i];
                    else
                        newTargets[i] = (i + 1) * 33.33f; // Default: 33%, 66%, 100%
                }
                scoreTargets = newTargets;
            }

            // Ensure scoreBarPoints array matches scoreTargets array length
            if (scoreBarPoints == null || scoreBarPoints.Length != scoreTargets.Length)
            {
                Debug.LogWarning("ScoreBarPoints array length doesn't match scoreTargets array. Resizing...");
                float[] newBarPoints = new float[scoreTargets.Length];
                for (int i = 0; i < scoreTargets.Length; i++)
                {
                    if (scoreBarPoints != null && i < scoreBarPoints.Length)
                        newBarPoints[i] = scoreBarPoints[i];
                    else
                        newBarPoints[i] = (i + 1) / (float)scoreTargets.Length; // Default: evenly distributed
                }
                scoreBarPoints = newBarPoints;
            }

            // Initialize all stars as empty
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    stars[i].SetStarEmpty();
                    starsFilled[i] = false;
                    scoreBarSegmentsFilled[i] = false;
                }
            }
        }

        // Initialize score bar
        if (scoreBar != null)
        {
            scoreBar.fillAmount = 0f;
        }
    }


    /// <summary>
    /// Gets the reference texture from Image (sprite) or direct texture reference.
    /// </summary>
    private Texture2D GetReferenceTexture()
    {
        // Priority: Image > direct texture reference
        if (referenceImage != null && referenceImage.sprite != null)
        {
            Sprite sprite = referenceImage.sprite;

            // Get the texture from the sprite
            if (sprite.texture != null)
            {
                return sprite.texture;
            }
        }

        // Fallback to direct texture reference
        return referenceTexture;
    }

    /// <summary>
    /// Gets the mask texture from Image (sprite).
    /// </summary>
    private Texture2D GetMaskTexture()
    {
        if (maskImage != null && maskImage.sprite != null)
        {
            Sprite sprite = maskImage.sprite;

            // Get the texture from the sprite
            if (sprite.texture != null)
            {
                return sprite.texture;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a mask pixel is white (has alpha > 0).
    /// </summary>
    private bool IsMaskWhite(Color32 maskColor)
    {
        // Mask is white if it has any alpha (not transparent)
        return maskColor.a > 0;
    }


    /// <summary>
    /// Calculates the similarity percentage between the drawn texture and reference texture.
    /// Returns a value between 0 (completely different) and 100 (identical).
    /// </summary>
    public float GetSimilarityPercentage()
    {
        if (drawCanvas == null)
        {
            Debug.LogError("DrawCanvas is not assigned!");
            return 0f;
        }

        Texture2D drawnTexture = drawCanvas.GetTexture();
        if (drawnTexture == null)
        {
            Debug.LogWarning("Drawn texture is null.");
            return 0f;
        }

        // Get reference texture from Image or direct reference
        Texture2D referenceTexture = GetReferenceTexture();

        if (referenceTexture == null)
        {
            Debug.LogWarning("No reference texture set for comparison.");
            return 0f;
        }

        // Get or create resampled textures (cached to avoid resampling every frame)
        Texture2D resampledReference = GetOrCreateResampledTexture(
            referenceTexture, drawnTexture.width, drawnTexture.height,
            ref cachedResampledReference, ref cachedDrawingWidth, ref cachedDrawingHeight);

        // Get mask texture if available
        Texture2D maskTexture = GetMaskTexture();
        Color32[] maskPixels = null;

        if (maskTexture != null)
        {
            Texture2D resampledMask = GetOrCreateResampledTexture(
                maskTexture, drawnTexture.width, drawnTexture.height,
                ref cachedResampledMask, ref cachedDrawingWidth, ref cachedDrawingHeight);
            maskPixels = resampledMask.GetPixels32();
        }

        Color32[] drawnPixels = drawnTexture.GetPixels32();
        Color32[] referencePixels = resampledReference.GetPixels32();

        if (drawnPixels.Length != referencePixels.Length)
        {
            Debug.LogError("Pixel array lengths don't match after resampling!");
            return 0f;
        }

        return ComparePixelArrays(drawnPixels, referencePixels, maskPixels);
    }

    /// <summary>
    /// Compares two textures directly without using the draw canvas.
    /// </summary>
    public float CompareTextures(Texture2D texture1, Texture2D texture2)
    {
        if (texture1 == null || texture2 == null)
        {
            Debug.LogWarning("One or both textures are null.");
            return 0f;
        }

        // Resize textures to match if needed
        if (texture1.width != texture2.width || texture1.height != texture2.height)
        {
            Debug.LogWarning("Texture dimensions don't match. Resizing texture2 to match texture1.");
            texture2 = ResizeTexture(texture2, texture1.width, texture1.height);
        }

        Color32[] pixels1 = texture1.GetPixels32();
        Color32[] pixels2 = texture2.GetPixels32();

        if (pixels1.Length != pixels2.Length)
        {
            Debug.LogError("Pixel array lengths don't match!");
            return 0f;
        }

        return ComparePixelArrays(pixels1, pixels2, null);
    }

    /// <summary>
    /// Compares two colors with a tolerance threshold.
    /// </summary>
    private bool AreColorsSimilar(Color32 c1, Color32 c2, float tolerance)
    {
        if (tolerance <= 0f)
        {
            // Exact match
            return c1.r == c2.r && c1.g == c2.g && c1.b == c2.b && c1.a == c2.a;
        }

        // Calculate color distance (normalized 0-1)
        float rDiff = Mathf.Abs(c1.r - c2.r) / 255f;
        float gDiff = Mathf.Abs(c1.g - c2.g) / 255f;
        float bDiff = Mathf.Abs(c1.b - c2.b) / 255f;
        float aDiff = Mathf.Abs(c1.a - c2.a) / 255f;

        // Average difference
        float avgDiff = (rDiff + gDiff + bDiff + aDiff) / 4f;

        return avgDiff <= tolerance;
    }

    /// <summary>
    /// Gets or creates a resampled texture, caching it to avoid resampling every frame.
    /// </summary>
    private Texture2D GetOrCreateResampledTexture(Texture2D source, int targetWidth, int targetHeight,
        ref Texture2D cached, ref int cachedWidth, ref int cachedHeight)
    {
        // Check if we need to resample (dimensions changed or cache is null)
        if (cached == null || cachedWidth != targetWidth || cachedHeight != targetHeight ||
            source.width != targetWidth || source.height != targetHeight)
        {
            // Clean up old cached texture
            if (cached != null)
            {
                Destroy(cached);
            }

            // Resample if dimensions don't match
            if (source.width != targetWidth || source.height != targetHeight)
            {
                cached = ResizeTexture(source, targetWidth, targetHeight);
            }
            else
            {
                // Same dimensions, just cache the reference
                cached = source;
            }

            cachedWidth = targetWidth;
            cachedHeight = targetHeight;
        }

        return cached;
    }

    /// <summary>
    /// Compares two pixel arrays and returns similarity percentage.
    /// </summary>
    private float ComparePixelArrays(Color32[] drawnPixels, Color32[] referencePixels, Color32[] maskPixels)
    {
        int matchingPixels = 0;
        int totalPixels = 0;

        for (int i = 0; i < drawnPixels.Length; i++)
        {
            // Check mask first - skip if mask is not white
            if (maskPixels != null && !IsMaskWhite(maskPixels[i]))
                continue;

            Color32 drawn = drawnPixels[i];
            Color32 reference = referencePixels[i];

            // Skip pixels where both are transparent (alpha = 0)
            if (drawn.a == 0 && reference.a == 0)
                continue;

            // Only count pixels where at least one has alpha = 255 (fully opaque)
            if (drawn.a == 255 || reference.a == 255)
            {
                totalPixels++;
                if (AreColorsSimilar(drawn, reference, colorTolerance))
                {
                    matchingPixels++;
                }
            }
        }

        if (totalPixels == 0)
            return 100f; // No pixels to compare, consider it a match

        return (float)matchingPixels / totalPixels * 100f;
    }

    /// <summary>
    /// Resizes a texture to the specified dimensions.
    /// </summary>
    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        Texture2D resized = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    void LateUpdate()
    {
        similarityPercentage = GetSimilarityPercentage();
        UpdateStars();
        UpdateScoreBar();
    }

    void OnDisable()
    {
        // Clean up any active tweens when the object is disabled
        if (currentScoreBarTween != null && currentScoreBarTween.IsActive())
        {
            currentScoreBarTween.Kill();
            currentScoreBarTween = null;
        }
    }

    void OnDestroy()
    {
        // Clean up any active tweens when the object is destroyed
        if (currentScoreBarTween != null && currentScoreBarTween.IsActive())
        {
            currentScoreBarTween.Kill();
            currentScoreBarTween = null;
        }

        // Clean up cached textures
        if (cachedResampledReference != null)
        {
            Destroy(cachedResampledReference);
            cachedResampledReference = null;
        }
        if (cachedResampledMask != null)
        {
            Destroy(cachedResampledMask);
            cachedResampledMask = null;
        }
    }

    /// <summary>
    /// Updates stars based on the current similarity percentage.
    /// </summary>
    private void UpdateStars()
    {
        if (stars == null || stars.Length == 0 || scoreTargets == null)
            return;

        // Check each star threshold
        for (int i = 0; i < stars.Length && i < scoreTargets.Length; i++)
        {
            if (stars[i] != null)
            {
                // If score reaches the target, fill the star
                if (similarityPercentage >= scoreTargets[i])
                {
                    if (!starsFilled[i])
                    {
                        stars[i].SetStarFull();
                        starsFilled[i] = true;
                    }
                }
                // If score drops below the target, empty the star
                else
                {
                    if (starsFilled[i])
                    {
                        stars[i].SetStarEmpty();
                        starsFilled[i] = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resets all stars to empty state.
    /// </summary>
    public void ResetStars()
    {
        if (stars == null || starsFilled == null)
            return;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].SetStarEmpty();
                starsFilled[i] = false;
                scoreBarSegmentsFilled[i] = false;
            }
        }

        // Reset score bar
        if (scoreBar != null)
        {
            if (currentScoreBarTween != null && currentScoreBarTween.IsActive())
            {
                currentScoreBarTween.Kill();
            }
            scoreBar.fillAmount = 0f;
        }
    }

    /// <summary>
    /// Calculates the score bar fill amount based on similarity percentage using curve interpolation.
    /// </summary>
    private float CalculateScoreBarFill(float similarity)
    {
        if (scoreTargets == null || scoreTargets.Length == 0 || scoreBarPoints == null || scoreBarPoints.Length == 0)
            return 0f;

        // If similarity is below the first target, interpolate from 0 to first bar point
        if (similarity < scoreTargets[0])
        {
            float t = Mathf.Clamp01(similarity / scoreTargets[0]);
            // Use smooth curve interpolation
            t = Mathf.SmoothStep(0f, 1f, t);
            return Mathf.Lerp(0f, scoreBarPoints[0], t);
        }

        // If similarity is above the last target, interpolate from last bar point to 1.0
        if (similarity >= scoreTargets[scoreTargets.Length - 1])
        {
            float lastTarget = scoreTargets[scoreTargets.Length - 1];
            float lastBarPoint = scoreBarPoints[scoreBarPoints.Length - 1];

            // Interpolate from last target to 100% (or cap at last bar point if preferred)
            float maxSimilarity = 100f;
            if (similarity >= maxSimilarity)
                return 1f;

            float t = Mathf.Clamp01((similarity - lastTarget) / (maxSimilarity - lastTarget));
            t = Mathf.SmoothStep(0f, 1f, t);
            return Mathf.Lerp(lastBarPoint, 1f, t);
        }

        // Find which segment we're in and interpolate between the two points
        for (int i = 0; i < scoreTargets.Length - 1; i++)
        {
            if (similarity >= scoreTargets[i] && similarity < scoreTargets[i + 1])
            {
                float segmentStart = scoreTargets[i];
                float segmentEnd = scoreTargets[i + 1];
                float barStart = scoreBarPoints[i];
                float barEnd = scoreBarPoints[i + 1];

                // Normalize similarity to 0-1 within this segment
                float t = (similarity - segmentStart) / (segmentEnd - segmentStart);
                // Apply smooth curve interpolation
                t = Mathf.SmoothStep(0f, 1f, t);
                return Mathf.Lerp(barStart, barEnd, t);
            }
        }

        return 0f;
    }

    /// <summary>
    /// Updates the score bar based on the current similarity percentage.
    /// </summary>
    private void UpdateScoreBar()
    {
        if (scoreBar == null || scoreTargets == null || scoreTargets.Length == 0 || scoreBarPoints == null)
            return;

        float targetFill = CalculateScoreBarFill(similarityPercentage);
        float currentFill = scoreBar.fillAmount;

        // Only animate if the fill amount has changed significantly (avoid jitter)
        if (Mathf.Abs(targetFill - currentFill) > 0.001f)
        {
            // Kill existing tween if active
            if (currentScoreBarTween != null && currentScoreBarTween.IsActive())
            {
                currentScoreBarTween.Kill();
            }

            // Animate to target fill amount
            currentScoreBarTween = DOTween.To(
                () => scoreBar.fillAmount,
                x => scoreBar.fillAmount = x,
                targetFill,
                scoreBarAnimationDuration
            ).SetEase(scoreBarEaseType);
        }
    }

}
