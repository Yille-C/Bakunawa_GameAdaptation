using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adds glowing HDR effect to score text displays.
/// Uses HDR colors for post-processing bloom pickup.
/// Attach to TribeScore or BakunawaScore text GameObjects.
/// </summary>
public class ScoreGlowEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private float glowIntensity = 2f; // HDR multiplier
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f; // 0-1
    [SerializeField] private bool enablePulse = true;
    
    // References
    private Text uiText;
    private TextMeshProUGUI tmpText;
    
    // Cache
    private Color baseHDRColor;
    private float lastValue = -999;
    private Coroutine flashCoroutine;
    
    void Start()
    {
        // Find text component
        uiText = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();
        
        // Apply initial glow settings
        ApplyGlow();
    }
    
    void ApplyGlow()
    {
        // Calculate HDR color (values > 1 trigger bloom)
        baseHDRColor = new Color(
            glowColor.r * glowIntensity,
            glowColor.g * glowIntensity,
            glowColor.b * glowIntensity,
            1f
        );
        
        if (uiText != null)
        {
            uiText.color = baseHDRColor;
            
            // Add outline for extra visibility
            Outline outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f); // Dark outline
            outline.effectDistance = new Vector2(2, -2);
            
            // Add shadow
            Shadow shadow = GetComponent<Shadow>();
            if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = baseHDRColor * 0.5f;
            shadow.effectDistance = new Vector2(3, -3);
        }
        else if (tmpText != null)
        {
            tmpText.color = baseHDRColor;
            
            // TMP has better glow options via material
            Material mat = tmpText.fontMaterial;
            if (mat != null)
            {
                // Underlay for glow effect
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor("_UnderlayColor", new Color(glowColor.r * glowIntensity * 0.5f, 
                                                          glowColor.g * glowIntensity * 0.5f, 
                                                          glowColor.b * glowIntensity * 0.5f, 0.5f));
                mat.SetFloat("_UnderlayOffsetX", 0);
                mat.SetFloat("_UnderlayOffsetY", 0);
                mat.SetFloat("_UnderlayDilate", 0.8f);
                mat.SetFloat("_UnderlaySoftness", 0.3f);
                
                // Outline
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetColor("_OutlineColor", Color.black);
                mat.SetFloat("_OutlineWidth", 0.15f);
                
                tmpText.SetAllDirty();
            }
        }
    }
    
    void Update()
    {
        if (!enablePulse) return;
        
        // Gentle pulse effect on text color
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        
        Color pulsedColor = new Color(
            glowColor.r * glowIntensity * pulse,
            glowColor.g * glowIntensity * pulse,
            glowColor.b * glowIntensity * pulse,
            1f
        );
        
        if (uiText != null)
        {
            uiText.color = pulsedColor;
        }
        else if (tmpText != null)
        {
            tmpText.color = pulsedColor;
        }
        
        // Check for value change and flash
        CheckForValueChange();
    }
    
    void CheckForValueChange()
    {
        float currentValue = 0;
        
        if (uiText != null && float.TryParse(uiText.text, out float val1))
            currentValue = val1;
        else if (tmpText != null && float.TryParse(tmpText.text, out float val2))
            currentValue = val2;
        
        if (lastValue != -999 && currentValue != lastValue)
        {
            // Value changed - flash!
            Flash();
        }
        lastValue = currentValue;
    }
    
    /// <summary>
    /// Flash the score when it changes
    /// </summary>
    public void Flash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }
    
    System.Collections.IEnumerator FlashRoutine()
    {
        float flashDuration = 0.3f;
        float elapsed = 0f;
        
        // Spike intensity
        float peakIntensity = glowIntensity * 3f;
        
        while (elapsed < flashDuration)
        {
            float t = elapsed / flashDuration;
            float currentIntensity = Mathf.Lerp(peakIntensity, glowIntensity, t);
            
            Color flashColor = new Color(
                glowColor.r * currentIntensity,
                glowColor.g * currentIntensity,
                glowColor.b * currentIntensity,
                1f
            );
            
            if (uiText != null) uiText.color = flashColor;
            else if (tmpText != null) tmpText.color = flashColor;
            
            // Scale punch
            float scale = Mathf.Lerp(1.3f, 1f, t);
            transform.localScale = Vector3.one * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset
        transform.localScale = Vector3.one;
    }
    
    /// <summary>
    /// Set custom glow color
    /// </summary>
    public void SetGlowColor(Color color)
    {
        glowColor = color;
        ApplyGlow();
    }
}
