using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a glowing border overlay effect for cards.
/// This is a simpler approach that creates its own Image for the glow,
/// rather than modifying the card's existing material.
/// </summary>
public class CardGlowOverlay : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f, 1f); // Golden glow
    [SerializeField] private float glowSize = 15f;
    [SerializeField] private float glowIntensity = 2.0f;
    [SerializeField] private float pulseSpeed = 3.0f;
    [SerializeField] private float padding = 10f; // Extra size around the card
    
    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 10f;
    
    private GameObject glowObject;
    private Image glowImage;
    private Material glowMaterial;
    private RectTransform glowRect;
    private RectTransform cardRect;
    
    private bool isGlowing = false;
    private float currentAlpha = 0f;
    
    // Shader property IDs
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowSizeID = Shader.PropertyToID("_GlowSize");
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
    private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
    
    private void Awake()
    {
        cardRect = GetComponent<RectTransform>();
        CreateGlowOverlay();
    }
    
    private void CreateGlowOverlay()
    {
        // Find shader
        Shader glowShader = Shader.Find("UI/CardGlowOutline");
        if (glowShader == null)
        {
            Debug.LogError("CardGlowOverlay: Could not find 'UI/CardGlowOutline' shader!");
            return;
        }
        
        // Create glow GameObject as the FIRST child so it renders behind card content
        glowObject = new GameObject("GlowOverlay");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.SetAsFirstSibling(); // Render behind other elements
        
        // Add RectTransform
        glowRect = glowObject.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-padding, -padding);
        glowRect.offsetMax = new Vector2(padding, padding);
        
        // Add CanvasRenderer
        glowObject.AddComponent<CanvasRenderer>();
        
        // Add Image with glow material
        glowImage = glowObject.AddComponent<Image>();
        glowImage.raycastTarget = false; // Don't block clicks
        
        // Create material
        glowMaterial = new Material(glowShader);
        glowMaterial.name = "CardGlowOverlay_Instance";
        ApplyGlowSettings();
        
        glowImage.material = glowMaterial;
        glowImage.color = new Color(1, 1, 1, 0); // Start invisible
        
        // Start disabled
        glowObject.SetActive(false);
        
        Debug.Log($"CardGlowOverlay: Created on {gameObject.name}");
    }
    
    private void ApplyGlowSettings()
    {
        if (glowMaterial == null) return;
        
        glowMaterial.SetColor(GlowColorID, glowColor);
        glowMaterial.SetFloat(GlowSizeID, glowSize);
        glowMaterial.SetFloat(GlowIntensityID, glowIntensity);
        glowMaterial.SetFloat(PulseSpeedID, pulseSpeed);
    }
    
    private void Update()
    {
        if (glowImage == null) return;
        
        float targetAlpha = isGlowing ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        
        glowImage.color = new Color(1, 1, 1, currentAlpha);
        
        // Enable/disable object for performance
        if (currentAlpha > 0.01f && !glowObject.activeSelf)
        {
            glowObject.SetActive(true);
        }
        else if (currentAlpha <= 0.01f && glowObject.activeSelf)
        {
            glowObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Enable or disable the glow effect with smooth animation.
    /// </summary>
    public void SetGlowEnabled(bool enabled)
    {
        isGlowing = enabled;
        
        // Make sure object is active so Update runs
        if (enabled && glowObject != null && !glowObject.activeSelf)
        {
            glowObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Immediately set glow state without animation.
    /// </summary>
    public void SetGlowEnabledImmediate(bool enabled)
    {
        isGlowing = enabled;
        currentAlpha = enabled ? 1f : 0f;
        
        if (glowImage != null)
        {
            glowImage.color = new Color(1, 1, 1, currentAlpha);
        }
        
        if (glowObject != null)
        {
            glowObject.SetActive(enabled);
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
    
    public bool IsGlowing => isGlowing;
    
    private void OnDestroy()
    {
        if (glowMaterial != null)
        {
            Destroy(glowMaterial);
        }
        if (glowObject != null)
        {
            Destroy(glowObject);
        }
    }
}
