using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the glowing edge effect on a card using the CardGlow shader.
/// Attach this to the card's main Image (card frame).
/// </summary>
[RequireComponent(typeof(Image))]
public class CardGlowEffect : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f, 1f); // Golden glow
    [SerializeField] private float glowWidth = 0.04f;
    [SerializeField] private float glowIntensity = 2.5f;
    [SerializeField] private float pulseSpeed = 3.0f;
    
    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 8f;
    
    private Image targetImage;
    private Material glowMaterial;
    private bool isGlowing = false;
    private float currentGlowAmount = 0f;
    
    // Shader property IDs for performance
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowWidthID = Shader.PropertyToID("_GlowWidth");
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
    private static readonly int GlowPulseSpeedID = Shader.PropertyToID("_GlowPulseSpeed");
    private static readonly int GlowEnabledID = Shader.PropertyToID("_GlowEnabled");

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        InitializeMaterial();
    }

    private void InitializeMaterial()
    {
        // Find the CardGlow shader
        Shader glowShader = Shader.Find("UI/CardGlow");
        
        if (glowShader == null)
        {
            Debug.LogError("CardGlowEffect: Could not find 'UI/CardGlow' shader! Make sure the shader exists in Assets/Shaders/ and has no compile errors.");
            return;
        }
        
        Debug.Log($"CardGlowEffect: Shader found on {gameObject.name}. Creating material...");
        
        // Create a unique material instance for this card
        glowMaterial = new Material(glowShader);
        glowMaterial.name = "CardGlow_Instance";
        
        // Copy the sprite texture from the original material
        if (targetImage.sprite != null)
        {
            glowMaterial.mainTexture = targetImage.sprite.texture;
            Debug.Log($"CardGlowEffect: Sprite texture assigned: {targetImage.sprite.name}");
        }
        else
        {
            Debug.LogWarning($"CardGlowEffect: No sprite on {gameObject.name} Image. Glow may not be visible.");
        }
        
        // Apply settings
        ApplyGlowSettings();
        
        // Assign material to the image
        targetImage.material = glowMaterial;
        
        // Start with glow disabled
        SetGlowEnabled(false);
        
        Debug.Log($"CardGlowEffect: Initialized on {gameObject.name}");
    }
    
    private void ApplyGlowSettings()
    {
        if (glowMaterial == null) return;
        
        glowMaterial.SetColor(GlowColorID, glowColor);
        glowMaterial.SetFloat(GlowWidthID, glowWidth);
        glowMaterial.SetFloat(GlowIntensityID, glowIntensity);
        glowMaterial.SetFloat(GlowPulseSpeedID, pulseSpeed);
    }

    private void Update()
    {
        // Smooth transition for glow effect
        float targetGlow = isGlowing ? 1f : 0f;
        currentGlowAmount = Mathf.MoveTowards(currentGlowAmount, targetGlow, Time.deltaTime * fadeSpeed);
        
        if (glowMaterial != null)
        {
            glowMaterial.SetFloat(GlowEnabledID, currentGlowAmount > 0.5f ? 1f : 0f);
            
            // Modulate intensity based on fade amount for smoother transition
            float modulatedIntensity = glowIntensity * Mathf.SmoothStep(0f, 1f, currentGlowAmount);
            glowMaterial.SetFloat(GlowIntensityID, modulatedIntensity);
        }
    }

    /// <summary>
    /// Enable or disable the glow effect with smooth animation.
    /// </summary>
    public void SetGlowEnabled(bool enabled)
    {
        isGlowing = enabled;
    }
    
    /// <summary>
    /// Immediately set glow state without animation.
    /// </summary>
    public void SetGlowEnabledImmediate(bool enabled)
    {
        isGlowing = enabled;
        currentGlowAmount = enabled ? 1f : 0f;
        
        if (glowMaterial != null)
        {
            glowMaterial.SetFloat(GlowEnabledID, enabled ? 1f : 0f);
            glowMaterial.SetFloat(GlowIntensityID, enabled ? glowIntensity : 0f);
        }
    }
    
    /// <summary>
    /// Change the glow color at runtime.
    /// </summary>
    public void SetGlowColor(Color color)
    {
        glowColor = color;
        if (glowMaterial != null)
        {
            glowMaterial.SetColor(GlowColorID, glowColor);
        }
    }
    
    /// <summary>
    /// Check if glow is currently active.
    /// </summary>
    public bool IsGlowing => isGlowing;

    private void OnDestroy()
    {
        // Clean up the material instance
        if (glowMaterial != null)
        {
            Destroy(glowMaterial);
        }
    }
}
