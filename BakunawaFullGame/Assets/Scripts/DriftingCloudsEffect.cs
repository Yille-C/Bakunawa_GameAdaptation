using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Creates atmospheric drifting clouds that pass across the screen,
/// simulating clouds moving beneath the moon for a moody night scene effect.
/// Attach to a GameObject inside a Canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DriftingCloudsEffect : MonoBehaviour
{
    [Header("Cloud Settings")]
    [SerializeField] private int cloudCount = 8;
    [SerializeField] private float minSpeed = 15f;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 3f;
    
    [Header("Cloud Appearance")]
    [SerializeField] private Color cloudColor = new Color(0.85f, 0.88f, 0.92f, 0.4f);
    [SerializeField] private float minAlpha = 0.15f;
    [SerializeField] private float maxAlpha = 0.5f;
    [SerializeField] private Sprite cloudSprite; // Optional custom cloud sprite
    
    [Header("Debug")]
    [Tooltip("Make clouds very visible for testing")]
    [SerializeField] private bool debugHighVisibility = false;
    
    [Header("Spawn Area")]
    [SerializeField] private float verticalSpread = 0.8f; // 0-1, how much of screen height to use
    [SerializeField] private float verticalOffset = 0f; // Offset from center (positive = higher)
    
    [Header("Movement")]
    [SerializeField] private bool moveLeftToRight = true;
    [SerializeField] private float verticalDrift = 15f; // Slight up/down wobble
    [SerializeField] private float driftSpeed = 0.3f;
    
    [Header("Parallax (Optional)")]
    [SerializeField] private bool enableParallax = true;
    [SerializeField] private int parallaxLayers = 3;
    
    private RectTransform canvasRect;
    private List<CloudData> clouds = new List<CloudData>();
    private float screenWidth;
    private float screenHeight;
    
    private class CloudData
    {
        public RectTransform transform;
        public Image image;
        public float speed;
        public float baseY;
        public float driftPhase;
        public float driftAmount;
        public int layer;
    }
    
    void Start()
    {
        // Ensure we have a RectTransform (required for UI)
        canvasRect = GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            // Add RectTransform if missing (this replaces Transform)
            canvasRect = gameObject.AddComponent<RectTransform>();
            Debug.Log("[DriftingCloudsEffect] Added RectTransform to GameObject");
        }
        
        // Auto-configure to stretch and fill parent
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localScale = Vector3.one;
        
        // Wait a frame to ensure layout is calculated
        StartCoroutine(DelayedInit());
    }
    
    System.Collections.IEnumerator DelayedInit()
    {
        yield return null; // Wait one frame for layout to be calculated
        
        UpdateScreenSize();
        CreateClouds();
    }
    
    void UpdateScreenSize()
    {
        if (canvasRect != null)
        {
            screenWidth = canvasRect.rect.width;
            screenHeight = canvasRect.rect.height;
            
            // Safety check - if dimensions are 0, use fallback
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                Debug.LogWarning($"[DriftingCloudsEffect] RectTransform has zero size ({screenWidth}x{screenHeight}), using Screen size");
                screenWidth = Screen.width;
                screenHeight = Screen.height;
            }
        }
        else
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }
        
        Debug.Log($"[DriftingCloudsEffect] Using dimensions: {screenWidth}x{screenHeight}");
    }
    
    void CreateClouds()
    {
        // Create container as a child, matching parent size
        GameObject container = new GameObject("CloudContainer");
        container.transform.SetParent(transform, false);
        container.transform.SetAsLastSibling(); // In FRONT for visibility (change to SetAsFirstSibling for behind)
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Get or create sprites (multiple variants for variety)
        Sprite[] sprites = null;
        
        if (cloudSprite != null)
        {
            // Use provided sprite (single variant)
            sprites = new Sprite[] { cloudSprite };
        }
        else
        {
            // Create procedural variants
            sprites = CreateCloudTextureVariants(4);
        }
        
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[DriftingCloudsEffect] No sprites available - clouds will render as solid color rectangles");
        }
        
        int cloudsPerLayer = enableParallax ? cloudCount / parallaxLayers : cloudCount;
        
        for (int i = 0; i < cloudCount; i++)
        {
            int layer = enableParallax ? (i % parallaxLayers) : 0;
            float layerMultiplier = enableParallax ? (1f + layer * 0.5f) : 1f;
            
            GameObject cloudObj = new GameObject($"Cloud_{i}");
            cloudObj.transform.SetParent(container.transform, false);
            
            RectTransform rt = cloudObj.AddComponent<RectTransform>();
            Image img = cloudObj.AddComponent<Image>();
            
            // Assign random sprite variant for variety
            if (sprites != null && sprites.Length > 0)
            {
                img.sprite = sprites[Random.Range(0, sprites.Length)];
            }
            // If no sprite, Image will render as solid color rectangle
            
            img.raycastTarget = false;
            
            // Randomize properties based on layer (parallax)
            float speed = Random.Range(minSpeed, maxSpeed) / layerMultiplier;
            float scale = Random.Range(minScale, maxScale) * layerMultiplier;
            float alpha = Random.Range(minAlpha, maxAlpha) / layerMultiplier;
            
            // Color with alpha (or debug color)
            Color color;
            if (debugHighVisibility)
            {
                // Bright visible color for debugging
                color = new Color(1f, 0.2f, 0.2f, 0.8f); // Bright red
            }
            else
            {
                color = cloudColor;
                color.a = alpha;
            }
            img.color = color;
            
            // Size - relative to screen size for consistent appearance
            // Base cloud is ~20% of screen width, scaled by random factor
            float baseWidth = screenWidth * 0.2f;
            float baseHeight = screenHeight * 0.08f;
            float width = baseWidth * scale;
            float height = baseHeight * scale;
            rt.sizeDelta = new Vector2(width, height);
            
            // Random starting position (using anchored position, centered at 0,0)
            float spawnRangeY = screenHeight * verticalSpread;
            float centerY = screenHeight * verticalOffset;
            float y = centerY + Random.Range(-spawnRangeY / 2, spawnRangeY / 2);
            
            float x = Random.Range(-screenWidth / 2 - width, screenWidth / 2 + width);
            
            rt.anchoredPosition = new Vector2(x, y);
            rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0); // Force Z to 0
            
            // Slight random rotation for variety
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f));
            
            CloudData cloud = new CloudData
            {
                transform = rt,
                image = img,
                speed = speed,
                baseY = y,
                driftPhase = Random.Range(0f, Mathf.PI * 2f),
                driftAmount = Random.Range(verticalDrift * 0.5f, verticalDrift),
                layer = layer
            };
            
            clouds.Add(cloud);
            
            // Log first cloud details for debugging
            if (i == 0)
            {
                Debug.Log($"[DriftingCloudsEffect] First cloud - Pos: {rt.anchoredPosition}, Size: {rt.sizeDelta}, Color: {img.color}, HasSprite: {img.sprite != null}");
            }
        }
        
        Debug.Log($"[DriftingCloudsEffect] Created {cloudCount} clouds. Screen: {screenWidth}x{screenHeight}. Check Hierarchy for 'CloudContainer'");
    }
    
    /// <summary>
    /// Creates multiple cloud texture variants with different shapes
    /// </summary>
    Sprite[] CreateCloudTextureVariants(int variantCount = 4)
    {
        Sprite[] sprites = new Sprite[variantCount];
        
        for (int v = 0; v < variantCount; v++)
        {
            Texture2D tex = CreateCloudTextureWithSeed(v);
            sprites[v] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                        new Vector2(0.5f, 0.5f), 100);
        }
        
        Debug.Log($"[DriftingCloudsEffect] Created {variantCount} cloud shape variants");
        return sprites;
    }
    
    Texture2D CreateCloudTextureWithSeed(int seed)
    {
        // Vary dimensions based on seed for different aspect ratios
        int width, height;
        switch (seed % 4)
        {
            case 0: // Wide and flat (wispy)
                width = 320;
                height = 64;
                break;
            case 1: // Tall and fluffy
                width = 192;
                height = 128;
                break;
            case 2: // Elongated
                width = 384;
                height = 80;
                break;
            default: // Standard
                width = 256;
                height = 96;
                break;
        }
        
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        
        System.Random rand = new System.Random(seed * 31 + 17); // Different seed for each variant
        
        // Generate random puff configuration based on seed
        int puffCount = 4 + (seed % 4); // 4-7 puffs
        Vector2[] puffs = new Vector2[puffCount];
        float[] puffSizes = new float[puffCount];
        
        for (int p = 0; p < puffCount; p++)
        {
            // Different distribution patterns based on seed
            float px, py;
            switch (seed % 4)
            {
                case 0: // Spread horizontally (wispy)
                    px = 0.15f + (float)p / puffCount * 0.7f;
                    py = 0.4f + (float)rand.NextDouble() * 0.2f;
                    puffSizes[p] = 0.15f + (float)rand.NextDouble() * 0.15f;
                    break;
                case 1: // Clustered (fluffy cumulus)
                    px = 0.35f + (float)rand.NextDouble() * 0.3f;
                    py = 0.3f + (float)rand.NextDouble() * 0.4f;
                    puffSizes[p] = 0.2f + (float)rand.NextDouble() * 0.2f;
                    break;
                case 2: // Elongated streak
                    px = 0.1f + (float)p / puffCount * 0.8f;
                    py = 0.45f + (float)rand.NextDouble() * 0.1f;
                    puffSizes[p] = 0.12f + (float)rand.NextDouble() * 0.1f;
                    break;
                default: // Scattered
                    px = 0.2f + (float)rand.NextDouble() * 0.6f;
                    py = 0.3f + (float)rand.NextDouble() * 0.4f;
                    puffSizes[p] = 0.15f + (float)rand.NextDouble() * 0.2f;
                    break;
            }
            puffs[p] = new Vector2(px, py);
        }
        
        // Generate the texture
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                float ny = (float)y / height;
                
                float alpha = 0f;
                
                // Combine puffs
                for (int p = 0; p < puffs.Length; p++)
                {
                    float dx = (nx - puffs[p].x) / puffSizes[p];
                    float dy = (ny - puffs[p].y) / (puffSizes[p] * 0.5f); // Flatten vertically
                    
                    float dist = dx * dx + dy * dy;
                    float puffAlpha = Mathf.Clamp01(1f - dist);
                    puffAlpha = Mathf.Pow(puffAlpha, 1.2f + (seed % 3) * 0.3f); // Vary softness
                    
                    alpha = Mathf.Max(alpha, puffAlpha);
                }
                
                // Edge fade
                float edgeFadeX = 1f - Mathf.Pow(Mathf.Abs(nx - 0.5f) * 2f, 2.5f);
                float edgeFadeY = 1f - Mathf.Pow(Mathf.Abs(ny - 0.5f) * 2f, 2f);
                alpha *= edgeFadeX * edgeFadeY;
                
                // Add noise for texture variation
                float noise = (float)rand.NextDouble() * 0.15f;
                alpha = Mathf.Clamp01(alpha + noise * alpha - noise * 0.3f);
                
                pixels[y * width + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        return tex;
    }
    
    void Update()
    {
        float dt = Time.deltaTime;
        float time = Time.time;
        
        foreach (CloudData cloud in clouds)
        {
            if (cloud.transform == null) continue;
            
            Vector2 pos = cloud.transform.anchoredPosition;
            
            // Horizontal movement
            float direction = moveLeftToRight ? 1f : -1f;
            pos.x += cloud.speed * direction * dt;
            
            // Vertical drift (gentle wobble)
            float driftOffset = Mathf.Sin(time * driftSpeed + cloud.driftPhase) * cloud.driftAmount;
            pos.y = cloud.baseY + driftOffset;
            
            // Wrap around screen
            float cloudWidth = cloud.transform.sizeDelta.x;
            
            if (moveLeftToRight && pos.x > screenWidth / 2 + cloudWidth)
            {
                pos.x = -screenWidth / 2 - cloudWidth;
                pos.y = RandomizeCloudY(cloud);
                cloud.baseY = pos.y;
            }
            else if (!moveLeftToRight && pos.x < -screenWidth / 2 - cloudWidth)
            {
                pos.x = screenWidth / 2 + cloudWidth;
                pos.y = RandomizeCloudY(cloud);
                cloud.baseY = pos.y;
            }
            
            cloud.transform.anchoredPosition = pos;
        }
    }
    
    float RandomizeCloudY(CloudData cloud)
    {
        float spawnRangeY = screenHeight * verticalSpread;
        float centerY = screenHeight * verticalOffset;
        return centerY + Random.Range(-spawnRangeY / 2, spawnRangeY / 2);
    }
    
    /// <summary>
    /// Adjust cloud density at runtime
    /// </summary>
    public void SetDensity(float density)
    {
        int targetCount = Mathf.RoundToInt(cloudCount * Mathf.Clamp01(density));
        for (int i = 0; i < clouds.Count; i++)
        {
            if (clouds[i].transform != null)
            {
                clouds[i].transform.gameObject.SetActive(i < targetCount);
            }
        }
    }
    
    /// <summary>
    /// Change cloud speed multiplier at runtime
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        foreach (var cloud in clouds)
        {
            cloud.speed = Random.Range(minSpeed, maxSpeed) * multiplier;
        }
    }
    
    /// <summary>
    /// Change cloud opacity at runtime
    /// </summary>
    public void SetOpacity(float opacity)
    {
        foreach (var cloud in clouds)
        {
            if (cloud.image != null)
            {
                Color c = cloud.image.color;
                c.a = Mathf.Clamp(opacity, minAlpha, maxAlpha);
                cloud.image.color = c;
            }
        }
    }
}
