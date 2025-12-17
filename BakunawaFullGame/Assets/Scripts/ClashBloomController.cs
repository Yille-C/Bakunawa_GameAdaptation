using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// Controls bloom intensity during card clashes for dramatic impact effects.
/// Works with URP (Universal Render Pipeline) Volume system.
/// Attach this to your main camera or a dedicated effects controller.
/// Auto-detects Volume in scene - no manual setup required.
/// </summary>
public class ClashBloomController : MonoBehaviour
{
    public static ClashBloomController Instance;
    
    [Header("Bloom Settings")]
    [SerializeField] private float normalBloomIntensity = 1f;
    [SerializeField] private float clashBloomIntensity = 8f;
    [SerializeField] private float bloomTransitionSpeed = 10f;
    
    [Header("Clash Flash Settings")]
    [SerializeField] private float flashDuration = 0.25f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Volume reference (auto-detected)
    private Volume postProcessVolume;
    
    // URP Bloom reference (using reflection for compatibility)
    private object bloomSettings;
    private System.Reflection.PropertyInfo intensityProperty;
    private float targetBloomIntensity;
    private bool hasBloom = false;
    
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
        SetupBloomReference();
    }
    
    void SetupBloomReference()
    {
        // Auto-find any Volume in scene
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        
        foreach (var vol in volumes)
        {
            if (vol.profile != null)
            {
                postProcessVolume = vol;
                if (showDebugLogs) Debug.Log($"[ClashBloomController] Found Volume: {vol.gameObject.name}");
                break;
            }
        }
        
        if (postProcessVolume == null || postProcessVolume.profile == null)
        {
            Debug.LogWarning("[ClashBloomController] No Volume found. Bloom control disabled.");
            return;
        }
        
        // Try to get URP Bloom using reflection (to avoid hard dependency on URP package)
        try
        {
            // Look for UnityEngine.Rendering.Universal.Bloom
            System.Type bloomType = System.Type.GetType("UnityEngine.Rendering.Universal.Bloom, Unity.RenderPipelines.Universal.Runtime");
            
            if (bloomType != null)
            {
                // Try to get the bloom component from the volume profile
                var components = postProcessVolume.profile.components;
                foreach (var component in components)
                {
                    if (component.GetType() == bloomType)
                    {
                        SetupBloomFromComponent(component, bloomType);
                        break;
                    }
                }
            }
            
            // Fallback: Check for any component named "Bloom" if strict type failed
            if (!hasBloom && postProcessVolume.profile != null)
            {
                var components = postProcessVolume.profile.components;
                foreach (var component in components)
                {
                    if (component.GetType().Name == "Bloom")
                    {
                         SetupBloomFromComponent(component, component.GetType());
                         break;
                    }
                }
            }
            
            if (!hasBloom)
            {
                // Fallback: Try Post Processing Stack v2
                TrySetupPostProcessingV2();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ClashBloomController] Could not setup bloom: {e.Message}");
        }
    }
    
    void TrySetupPostProcessingV2()
    {
        try
        {
            // Try Post Processing Stack v2 Bloom
            System.Type ppBloomType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.Bloom, Unity.PostProcessing.Runtime");
            
            if (ppBloomType != null && postProcessVolume != null)
            {
                // This would be Post Processing Stack v2 Volume
                var ppVolume = postProcessVolume.GetComponent("PostProcessVolume");
                if (ppVolume != null)
                {
                    Debug.Log("[ClashBloomController] Post Processing v2 detected but not fully supported. Consider using URP.");
                }
            }
        }
        catch
        {
            // Silently fail - no post processing available
        }
    }

    void SetupBloomFromComponent(object component, System.Type type)
    {
        bloomSettings = component;
        
        // Get the intensity property
        intensityProperty = type.GetProperty("intensity");
        if (intensityProperty != null)
        {
            // Get current value as default
            var intensityParam = intensityProperty.GetValue(bloomSettings);
            var valueProperty = intensityParam.GetType().GetProperty("value");
            if (valueProperty != null)
            {
                normalBloomIntensity = (float)valueProperty.GetValue(intensityParam);
                targetBloomIntensity = normalBloomIntensity;
                hasBloom = true;
                if (showDebugLogs) Debug.Log($"[ClashBloomController] Bloom found via {(type.Name == "Bloom" ? "Name Match" : "Strict Type")}. Normal intensity: {normalBloomIntensity}");
            }
        }
    }
    
    void Update()
    {
        if (!hasBloom || bloomSettings == null || intensityProperty == null) return;
        
        try
        {
            // Get current intensity
            var intensityParam = intensityProperty.GetValue(bloomSettings);
            var valueProperty = intensityParam.GetType().GetProperty("value");
            if (valueProperty == null) return;
            
            float currentIntensity = (float)valueProperty.GetValue(intensityParam);
            
            // Lerp towards target
            if (!Mathf.Approximately(currentIntensity, targetBloomIntensity))
            {
                float newIntensity = Mathf.Lerp(currentIntensity, targetBloomIntensity, Time.deltaTime * bloomTransitionSpeed);
                valueProperty.SetValue(intensityParam, newIntensity);
            }
        }
        catch
        {
            // Fail silently on frame updates
        }
    }
    
    /// <summary>
    /// Trigger a bloom spike for card clash impact
    /// </summary>
    public void TriggerClashBloom()
    {
        if (!hasBloom)
        {
            // Debug.Log("[ClashBloomController] TriggerClashBloom called but no bloom available.");
            return;
        }
        
        StartCoroutine(ClashBloomSequence());
    }
    
    IEnumerator ClashBloomSequence()
    {
        // Spike bloom immediately
        SetBloomIntensityDirect(clashBloomIntensity);
        targetBloomIntensity = clashBloomIntensity;
        
        yield return new WaitForSeconds(flashDuration * 0.3f);
        
        // Start fading back
        targetBloomIntensity = normalBloomIntensity;
        
        yield return new WaitForSeconds(flashDuration * 0.7f);
    }
    
    void SetBloomIntensityDirect(float intensity)
    {
        if (!hasBloom || bloomSettings == null || intensityProperty == null) return;
        
        try
        {
            var intensityParam = intensityProperty.GetValue(bloomSettings);
            var valueProperty = intensityParam.GetType().GetProperty("value");
            if (valueProperty != null)
            {
                valueProperty.SetValue(intensityParam, intensity);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ClashBloomController] Failed to set bloom intensity: {e.Message}");
        }
    }
    
    /// <summary>
    /// Set bloom intensity directly (0-1 normalized, multiplied by clash intensity)
    /// </summary>
    public void SetBloomIntensity(float normalizedIntensity)
    {
        targetBloomIntensity = Mathf.Lerp(normalBloomIntensity, clashBloomIntensity, normalizedIntensity);
    }
    
    /// <summary>
    /// Reset bloom to normal levels
    /// </summary>
    public void ResetBloom()
    {
        targetBloomIntensity = normalBloomIntensity;
    }
    
    /// <summary>
    /// Check if bloom control is available
    /// </summary>
    public bool IsBloomAvailable()
    {
        return hasBloom;
    }
}
