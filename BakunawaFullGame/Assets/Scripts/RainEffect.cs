using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Creates a rain effect using UI sprites that fall across the screen.
/// Attach this to a UI canvas or panel to spawn rain drops.
/// </summary>
public class RainEffect : MonoBehaviour
{
    [Header("Rain Settings")]
    [SerializeField] private int rainDropCount = 150; // More drops
    [SerializeField] private float minFallSpeed = 900f;
    [SerializeField] private float maxFallSpeed = 1800f;
    [SerializeField] private float minDropLength = 25f; // Longer drops
    [SerializeField] private float maxDropLength = 70f;
    [SerializeField] private float dropWidth = 3f; // Thicker drops
    [SerializeField] private float windAngle = 12f; // Degrees from vertical
    [SerializeField] private float windVariance = 4f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color rainColor = new Color(0.85f, 0.92f, 1f, 0.85f); // High visibility
    [SerializeField] private Color rainHighlightColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private bool enableLightning = false;
    [SerializeField] private float lightningInterval = 8f;
    [SerializeField] private float lightningVariance = 5f;
    
    [Header("Spawn Area")]
    [SerializeField] private float spawnMargin = 200f; // Extra margin above screen
    
    private RectTransform canvasRect;
    private List<RainDrop> rainDrops = new List<RainDrop>();
    private float screenHeight;
    private float screenWidth;
    private float nextLightningTime;
    private Image lightningFlash;
    
    private class RainDrop
    {
        public RectTransform transform;
        public Image image;
        public float speed;
        public float windOffset;
        public float length;
    }
    
    void Start()
    {
        canvasRect = GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            canvasRect = GetComponentInParent<RectTransform>();
        }
        
        UpdateScreenSize();
        CreateRainDrops();
        
        if (enableLightning)
        {
            CreateLightningFlash();
            ScheduleNextLightning();
        }
    }
    
    void UpdateScreenSize()
    {
        if (canvasRect != null)
        {
            screenWidth = canvasRect.rect.width;
            screenHeight = canvasRect.rect.height;
        }
        else
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }
    }
    
    void CreateRainDrops()
    {
        // Create a container for rain
        GameObject rainContainer = new GameObject("RainContainer");
        rainContainer.transform.SetParent(transform, false);
        rainContainer.transform.SetAsFirstSibling(); // Behind other UI
        
        RectTransform containerRect = rainContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Create drop sprite texture (elongated ellipse)
        Texture2D dropTexture = CreateDropTexture();
        Sprite dropSprite = Sprite.Create(dropTexture, new Rect(0, 0, dropTexture.width, dropTexture.height), 
                                           new Vector2(0.5f, 0.5f), 100);
        
        for (int i = 0; i < rainDropCount; i++)
        {
            GameObject dropObj = new GameObject($"RainDrop_{i}");
            dropObj.transform.SetParent(rainContainer.transform, false);
            
            RectTransform rt = dropObj.AddComponent<RectTransform>();
            Image img = dropObj.AddComponent<Image>();
            img.sprite = dropSprite;
            
            // Randomize properties
            float length = Random.Range(minDropLength, maxDropLength);
            float speed = Random.Range(minFallSpeed, maxFallSpeed);
            float windOffset = windAngle + Random.Range(-windVariance, windVariance);
            
            // Color variation
            Color dropColor = Color.Lerp(rainColor, rainHighlightColor, Random.Range(0f, 0.3f));
            dropColor.a *= Random.Range(0.6f, 1f);
            img.color = dropColor;
            
            // Size
            rt.sizeDelta = new Vector2(dropWidth, length);
            
            // Rotation based on wind - positive angle = lean right (wind blowing right)
            rt.rotation = Quaternion.Euler(0, 0, windOffset);
            
            // Random starting position
            float x = Random.Range(-screenWidth / 2 - spawnMargin, screenWidth / 2 + spawnMargin);
            float y = Random.Range(-screenHeight / 2, screenHeight / 2 + spawnMargin);
            rt.anchoredPosition = new Vector2(x, y);
            
            RainDrop drop = new RainDrop
            {
                transform = rt,
                image = img,
                speed = speed,
                windOffset = windOffset,
                length = length
            };
            
            rainDrops.Add(drop);
        }
        
        Debug.Log($"[RainEffect] Created {rainDropCount} rain drops. Screen: {screenWidth}x{screenHeight}");
    }
    
    Texture2D CreateDropTexture()
    {
        int width = 8;
        int height = 32;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        Color[] pixels = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Normalized coordinates
                float nx = (x - width / 2f) / (width / 2f);
                float ny = (y - height / 2f) / (height / 2f);
                
                // Elongated ellipse shape with soft edges
                float ellipse = (nx * nx) / 1f + (ny * ny) / 1f;
                
                // Gradient: brighter at top, fading towards bottom
                float gradient = 1f - (ny * 0.5f + 0.5f) * 0.6f;
                
                // Soft edge
                float alpha = Mathf.Clamp01(1f - ellipse);
                alpha = Mathf.Pow(alpha, 0.5f); // Soften edges
                alpha *= gradient;
                
                pixels[y * width + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        
        return tex;
    }
    
    void CreateLightningFlash()
    {
        GameObject flashObj = new GameObject("LightningFlash");
        flashObj.transform.SetParent(transform, false);
        flashObj.transform.SetAsLastSibling();
        
        RectTransform rt = flashObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        lightningFlash = flashObj.AddComponent<Image>();
        lightningFlash.color = new Color(1, 1, 1, 0);
        lightningFlash.raycastTarget = false;
    }
    
    void ScheduleNextLightning()
    {
        nextLightningTime = Time.time + lightningInterval + Random.Range(-lightningVariance, lightningVariance);
    }
    
    void Update()
    {
        float dt = Time.deltaTime;
        
        foreach (RainDrop drop in rainDrops)
        {
            if (drop.transform == null) continue;
            
            // Calculate movement direction based on wind
            float angleRad = drop.windOffset * Mathf.Deg2Rad;
            float dx = Mathf.Sin(angleRad) * drop.speed * dt;
            float dy = -Mathf.Cos(angleRad) * drop.speed * dt; // Negative because falling down
            
            Vector2 pos = drop.transform.anchoredPosition;
            pos.x += dx;
            pos.y += dy;
            
            // Check if off screen (bottom or too far to the sides)
            if (pos.y < -screenHeight / 2 - drop.length || 
                pos.x > screenWidth / 2 + spawnMargin ||
                pos.x < -screenWidth / 2 - spawnMargin)
            {
                // Reset to top with random X
                pos.y = screenHeight / 2 + spawnMargin;
                pos.x = Random.Range(-screenWidth / 2 - spawnMargin * 0.5f, screenWidth / 2 + spawnMargin * 0.5f);
                
                // Randomize speed and appearance slightly
                drop.speed = Random.Range(minFallSpeed, maxFallSpeed);
                drop.windOffset = windAngle + Random.Range(-windVariance, windVariance);
                drop.transform.rotation = Quaternion.Euler(0, 0, drop.windOffset);
                
                // Slight color variation
                Color newColor = Color.Lerp(rainColor, rainHighlightColor, Random.Range(0f, 0.3f));
                newColor.a = rainColor.a * Random.Range(0.6f, 1f);
                drop.image.color = newColor;
            }
            
            drop.transform.anchoredPosition = pos;
        }
        
        // Lightning effect
        if (enableLightning && lightningFlash != null)
        {
            if (Time.time >= nextLightningTime)
            {
                StartCoroutine(DoLightningFlash());
                ScheduleNextLightning();
            }
        }
    }
    
    System.Collections.IEnumerator DoLightningFlash()
    {
        // Play thunder sound (with slight delay for realism)
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayThunder();
        }
        
        // Quick flash
        lightningFlash.color = new Color(1, 1, 1, 0.3f);
        yield return new WaitForSeconds(0.05f);
        lightningFlash.color = new Color(1, 1, 1, 0);
        yield return new WaitForSeconds(0.1f);
        
        // Second flash (double strike effect)
        if (Random.value > 0.5f)
        {
            lightningFlash.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(0.03f);
            lightningFlash.color = new Color(1, 1, 1, 0);
        }
    }
    
    /// <summary>
    /// Adjust rain intensity at runtime
    /// </summary>
    public void SetIntensity(float intensity)
    {
        int targetCount = Mathf.RoundToInt(rainDropCount * intensity);
        for (int i = 0; i < rainDrops.Count; i++)
        {
            if (rainDrops[i].transform != null)
            {
                rainDrops[i].transform.gameObject.SetActive(i < targetCount);
            }
        }
    }
    
    /// <summary>
    /// Change rain color at runtime
    /// </summary>
    public void SetRainColor(Color newColor)
    {
        rainColor = newColor;
        foreach (var drop in rainDrops)
        {
            if (drop.image != null)
            {
                Color dropColor = newColor;
                dropColor.a *= Random.Range(0.6f, 1f);
                drop.image.color = dropColor;
            }
        }
    }
}
