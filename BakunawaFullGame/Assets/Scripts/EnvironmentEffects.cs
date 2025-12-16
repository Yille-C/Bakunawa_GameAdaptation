using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master controller for environmental effects: rain, ripples, ambient lighting.
/// Attach this to your main game canvas or a dedicated environment effects container.
/// Uses Canvas overrideSorting for proper visibility in Screen Space - Camera mode.
/// </summary>
public class EnvironmentEffects : MonoBehaviour
{
    public static EnvironmentEffects Instance;
    
    [Header("Effects Components")]
    [SerializeField] private RainEffect rainEffect;
    [SerializeField] private RippleEffect rippleEffect;
    
    [Header("Rain Settings")]
    [SerializeField] private bool enableRain = true;
    [SerializeField] private int rainIntensity = 100;
    [SerializeField] private float rainSpeed = 1200f;
    [SerializeField] private float windAngle = 12f;
    [SerializeField] private Color rainColor = new Color(0.7f, 0.85f, 1f, 0.35f);
    [SerializeField] [Range(0.5f, 3f)] private float rainScale = 1f; // Size multiplier for rain drops
    
    [Header("Ripple Settings")]
    [SerializeField] private bool enableRipples = true;
    [SerializeField] private float rippleInterval = 0.6f;
    [SerializeField] private Color rippleColor = new Color(0.5f, 0.7f, 0.9f, 0.3f);
    [SerializeField] private float rippleAlpha = 0.35f;
    [SerializeField] [Range(0.05f, 0.5f)] private float rippleMaxSize = 0.15f; // Max radius of ripples (0-1 UV space)
    [SerializeField] [Range(0.002f, 0.02f)] private float rippleRingThickness = 0.006f; // Thickness of ripple rings
    
    [Header("Ambient Effect Settings")]
    [SerializeField] private bool enableAmbientDarkening = true;
    [SerializeField] private float ambientDarkness = 0.15f;
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private float vignetteIntensity = 0.3f;
    [Header("Thunder/Ambiance")]
    [SerializeField] private bool enableLightning = false;
    [SerializeField] private float lightningInterval = 10f;
    
    [Header("Hierarchy Position")]
    [Tooltip("Position in canvas hierarchy. 0=first (behind everything), 1=after background, higher=further forward. Set to 1 to render after background but before UI.")]
    [SerializeField] private int hierarchySiblingIndex = 1;
    
    private Image ambientOverlay;
    private Image vignetteOverlay;
    private GameObject effectsContainer;
    private bool isActive = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        SetupEffects();
    }
    
    void SetupEffects()
    {
        // Find root canvas for proper parenting
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;
        
        Transform effectsParent = (rootCanvas != null) ? rootCanvas.transform : transform;
        
        // Create effects container
        effectsContainer = new GameObject("EnvironmentEffectsContainer");
        effectsContainer.transform.SetParent(effectsParent, false);
        
        // Position in hierarchy: 0 = behind everything, 1 = after background, etc.
        int maxIndex = effectsParent.childCount - 1;
        int targetIndex = Mathf.Clamp(hierarchySiblingIndex, 0, maxIndex);
        effectsContainer.transform.SetSiblingIndex(targetIndex);
        
        Debug.Log($"[EnvironmentEffects] Placed at sibling index {targetIndex} of {effectsParent.childCount} children");
        
        RectTransform containerRect = effectsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // NO Canvas component - just use hierarchy order within parent canvas
        // First child = behind, Last child = in front
        
        // 1. Ambient Darkening Overlay (first child = furthest back)
        if (enableAmbientDarkening)
        {
            CreateAmbientOverlay(effectsContainer.transform);
        }
        
        // 2. Ripple Effect
        if (enableRipples)
        {
            CreateRippleEffect(effectsContainer.transform);
        }
        
        // 3. Rain Effect
        if (enableRain)
        {
            CreateRainEffect(effectsContainer.transform);
        }
        
        // 4. Vignette (last child of effects = closest to other UI, but container is first so still behind)
        if (enableVignette)
        {
            CreateVignetteOverlay(effectsContainer.transform);
        }
        
        Debug.Log($"[EnvironmentEffects] Setup complete. Using hierarchy order (first sibling = behind).");
    }
    
    void CreateAmbientOverlay(Transform parent)
    {
        GameObject ambientObj = new GameObject("AmbientDarkness");
        ambientObj.transform.SetParent(parent, false);
        
        RectTransform rt = ambientObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // NO separate Canvas - just use hierarchy order
        ambientOverlay = ambientObj.AddComponent<Image>();
        ambientOverlay.color = new Color(0.1f, 0.15f, 0.25f, ambientDarkness);
        ambientOverlay.raycastTarget = false;
    }
    
    void CreateRippleEffect(Transform parent)
    {
        GameObject rippleObj = new GameObject("RippleEffect");
        rippleObj.transform.SetParent(parent, false);
        
        RectTransform rt = rippleObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // NO separate Canvas - just use hierarchy order
        rippleEffect = rippleObj.AddComponent<RippleEffect>();
        
        // Configure via reflection
        SetPrivateField(rippleEffect, "rippleInterval", rippleInterval);
        SetPrivateField(rippleEffect, "rippleColor", rippleColor);
        SetPrivateField(rippleEffect, "rippleAlpha", rippleAlpha);
        SetPrivateField(rippleEffect, "rippleMaxSize", rippleMaxSize);
        SetPrivateField(rippleEffect, "rippleRingThickness", rippleRingThickness);
    }
    
    void CreateRainEffect(Transform parent)
    {
        GameObject rainObj = new GameObject("RainEffect");
        rainObj.transform.SetParent(parent, false);
        
        RectTransform rt = rainObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // NO separate Canvas - just use hierarchy order
        rainEffect = rainObj.AddComponent<RainEffect>();
        
        // Configure via reflection
        SetPrivateField(rainEffect, "rainDropCount", rainIntensity);
        SetPrivateField(rainEffect, "minFallSpeed", rainSpeed * 0.6f);
        SetPrivateField(rainEffect, "maxFallSpeed", rainSpeed * 1.3f);
        SetPrivateField(rainEffect, "windAngle", windAngle);
        SetPrivateField(rainEffect, "rainColor", rainColor);
        SetPrivateField(rainEffect, "enableLightning", enableLightning);
        SetPrivateField(rainEffect, "lightningInterval", lightningInterval);
        SetPrivateField(rainEffect, "dropWidth", 3f * rainScale);
        SetPrivateField(rainEffect, "minDropLength", 25f * rainScale);
        SetPrivateField(rainEffect, "maxDropLength", 70f * rainScale);
    }
    
    void CreateVignetteOverlay(Transform parent)
    {
        GameObject vignetteObj = new GameObject("VignetteOverlay");
        vignetteObj.transform.SetParent(parent, false);
        
        RectTransform rt = vignetteObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // NO separate Canvas - just use hierarchy order
        vignetteOverlay = vignetteObj.AddComponent<Image>();
        vignetteOverlay.raycastTarget = false;
        
        // Create vignette texture
        Texture2D vignetteTex = CreateVignetteTexture(256, vignetteIntensity);
        vignetteOverlay.sprite = Sprite.Create(vignetteTex, 
            new Rect(0, 0, vignetteTex.width, vignetteTex.height), 
            new Vector2(0.5f, 0.5f), 100);
        vignetteOverlay.color = Color.white;
    }
    
    Texture2D CreateVignetteTexture(int size, float intensity)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        float center = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Smooth vignette falloff
                float vignette = Mathf.SmoothStep(0.3f, 1.2f, dist) * intensity;
                
                // Darker blue-grey in corners for moody atmosphere
                pixels[y * size + x] = new Color(0.05f, 0.08f, 0.15f, vignette);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        return tex;
    }
    
    void SetPrivateField(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
    
    /// <summary>
    /// Enable or disable all environment effects
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (effectsContainer != null)
            effectsContainer.SetActive(active);
    }
    
    /// <summary>
    /// Set rain intensity (0-1 scale)
    /// </summary>
    public void SetRainIntensity(float intensity)
    {
        if (rainEffect != null)
        {
            rainEffect.SetIntensity(intensity);
        }
    }
    
    /// <summary>
    /// Set ripple intensity (0-1 scale)
    /// </summary>
    public void SetRippleIntensity(float intensity)
    {
        if (rippleEffect != null)
        {
            rippleEffect.SetIntensity(intensity);
        }
    }
    
    /// <summary>
    /// Spawn a ripple at a screen position (e.g., when a card lands)
    /// </summary>
    public void SpawnRippleAt(Vector2 screenPosition)
    {
        if (rippleEffect != null && enableRipples)
        {
            rippleEffect.SpawnRippleAtScreenPos(screenPosition);
        }
    }
    
    /// <summary>
    /// Set the overall mood/atmosphere intensity
    /// </summary>
    public void SetAtmosphereIntensity(float intensity)
    {
        if (ambientOverlay != null)
        {
            Color c = ambientOverlay.color;
            c.a = ambientDarkness * intensity;
            ambientOverlay.color = c;
        }
        
        if (vignetteOverlay != null)
        {
            Color c = vignetteOverlay.color;
            c.a = intensity;
            vignetteOverlay.color = c;
        }
    }
}
