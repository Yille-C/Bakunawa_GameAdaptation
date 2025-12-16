using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Creates animated water ripple effects on a UI surface.
/// Uses the WaterRipple shader for GPU-accelerated ripple animation.
/// </summary>
public class RippleEffect : MonoBehaviour
{
    [Header("Ripple Settings")]
    [SerializeField] private int maxRipples = 8;
    [SerializeField] private float rippleInterval = 0.3f;
    [SerializeField] private float rippleIntervalVariance = 0.15f;
    [SerializeField] private float rippleLifetime = 2f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color rippleColor = new Color(0.7f, 0.85f, 1f, 0.5f);
    [SerializeField] private float rippleSpeed = 2.5f;
    [SerializeField] private float rippleFrequency = 8f;
    [SerializeField] private float rippleAlpha = 0.6f;
    
    [Header("Size Controls")]
    [SerializeField] [Range(0.05f, 0.5f)] private float rippleMaxSize = 0.15f; // Max radius in UV space
    [SerializeField] [Range(0.002f, 0.02f)] private float rippleRingThickness = 0.006f; // Ring line thickness
    
    [Header("Spawn Area")]
    [SerializeField] private float spawnPaddingX = 0.15f;
    [SerializeField] private float spawnPaddingY = 0.15f;
    
    private Material rippleMaterial;
    private Image rippleImage;
    private float nextRippleTime;
    private int currentRippleIndex = 0;
    
    // Ripple tracking
    private Vector2[] rippleCenters;
    private float[] rippleBirthTimes;
    
    // Shader property IDs for faster access
    private int[] rippleCenterIds;
    private int rippleTimesId;
    private int rippleTimes2Id;
    
    void Start()
    {
        SetupRippleOverlay();
        InitializeRipples();
    }
    
    void SetupRippleOverlay()
    {
        // Create overlay image for ripple effect
        GameObject overlayObj = new GameObject("RippleOverlay");
        overlayObj.transform.SetParent(transform, false);
        overlayObj.transform.SetAsFirstSibling(); // Behind other UI
        
        RectTransform rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        rippleImage = overlayObj.AddComponent<Image>();
        rippleImage.raycastTarget = false;
        
        // Create material from shader
        Shader rippleShader = Shader.Find("UI/WaterRipple");
        if (rippleShader != null)
        {
            rippleMaterial = new Material(rippleShader);
            rippleImage.material = rippleMaterial;
            
            // Initial settings
            rippleMaterial.SetColor("_RippleColor", rippleColor);
            rippleMaterial.SetFloat("_RippleSpeed", rippleSpeed);
            rippleMaterial.SetFloat("_RippleFrequency", rippleFrequency);
            rippleMaterial.SetFloat("_Alpha", rippleAlpha);
            rippleMaterial.SetFloat("_RippleCount", maxRipples);
            rippleMaterial.SetFloat("_RippleMaxSize", rippleMaxSize);
            rippleMaterial.SetFloat("_RingThickness", rippleRingThickness);
            
            // Get aspect ratio for perfect circles
            RectTransform myRect = GetComponent<RectTransform>();
            if (myRect != null && myRect.rect.height > 0)
            {
                float aspectRatio = myRect.rect.width / myRect.rect.height;
                rippleMaterial.SetFloat("_AspectRatio", aspectRatio);
            }
            else
            {
                rippleMaterial.SetFloat("_AspectRatio", (float)Screen.width / Screen.height);
            }
            
            Debug.Log("[RippleEffect] WaterRipple shader found and material created.");
        }
        else
        {
            Debug.LogWarning("RippleEffect: WaterRipple shader not found! Using fallback colored overlay.");
            // Fallback: simple colored overlay with higher visibility
            rippleImage.color = new Color(rippleColor.r, rippleColor.g, rippleColor.b, 0.2f);
        }
        
        // Setup shader property IDs
        rippleCenterIds = new int[6];
        rippleCenterIds[0] = Shader.PropertyToID("_Ripple1Center");
        rippleCenterIds[1] = Shader.PropertyToID("_Ripple2Center");
        rippleCenterIds[2] = Shader.PropertyToID("_Ripple3Center");
        rippleCenterIds[3] = Shader.PropertyToID("_Ripple4Center");
        rippleCenterIds[4] = Shader.PropertyToID("_Ripple5Center");
        rippleCenterIds[5] = Shader.PropertyToID("_Ripple6Center");
        rippleTimesId = Shader.PropertyToID("_RippleTimes");
        rippleTimes2Id = Shader.PropertyToID("_RippleTimes2");
    }
    
    void InitializeRipples()
    {
        rippleCenters = new Vector2[maxRipples];
        rippleBirthTimes = new float[maxRipples];
        
        // Initialize with staggered times
        for (int i = 0; i < maxRipples; i++)
        {
            rippleCenters[i] = GetRandomSpawnPosition();
            rippleBirthTimes[i] = -rippleLifetime + (i * rippleLifetime / maxRipples);
        }
        
        UpdateShaderProperties();
        ScheduleNextRipple();
    }
    
    Vector2 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnPaddingX, 1f - spawnPaddingX);
        float y = Random.Range(spawnPaddingY, 1f - spawnPaddingY);
        return new Vector2(x, y);
    }
    
    void ScheduleNextRipple()
    {
        nextRippleTime = Time.time + rippleInterval + Random.Range(-rippleIntervalVariance, rippleIntervalVariance);
    }
    
    void Update()
    {
        if (rippleMaterial == null) return;
        
        // Spawn new ripples periodically
        if (Time.time >= nextRippleTime)
        {
            SpawnNewRipple();
            ScheduleNextRipple();
        }
        
        // Update shader time offset for smooth animation
        rippleMaterial.SetFloat("_TimeOffset", 0); // Shader uses _Time.y internally
    }
    
    void SpawnNewRipple()
    {
        // Cycle through ripple slots
        currentRippleIndex = (currentRippleIndex + 1) % maxRipples;
        
        // Set new position and reset birth time
        rippleCenters[currentRippleIndex] = GetRandomSpawnPosition();
        rippleBirthTimes[currentRippleIndex] = Time.time;
        
        UpdateShaderProperties();
    }
    
    void UpdateShaderProperties()
    {
        if (rippleMaterial == null) return;
        
        // Update ripple centers
        for (int i = 0; i < Mathf.Min(maxRipples, 6); i++)
        {
            rippleMaterial.SetVector(rippleCenterIds[i], 
                new Vector4(rippleCenters[i].x, rippleCenters[i].y, 0, 0));
        }
        
        // Update ripple birth times
        rippleMaterial.SetVector(rippleTimesId, new Vector4(
            rippleBirthTimes.Length > 0 ? rippleBirthTimes[0] : 0,
            rippleBirthTimes.Length > 1 ? rippleBirthTimes[1] : 0,
            rippleBirthTimes.Length > 2 ? rippleBirthTimes[2] : 0,
            rippleBirthTimes.Length > 3 ? rippleBirthTimes[3] : 0
        ));
        
        rippleMaterial.SetVector(rippleTimes2Id, new Vector4(
            rippleBirthTimes.Length > 4 ? rippleBirthTimes[4] : 0,
            rippleBirthTimes.Length > 5 ? rippleBirthTimes[5] : 0,
            0, 0
        ));
    }
    
    /// <summary>
    /// Spawn a ripple at a specific UV position (0-1 range)
    /// </summary>
    public void SpawnRippleAt(Vector2 uvPosition)
    {
        currentRippleIndex = (currentRippleIndex + 1) % maxRipples;
        rippleCenters[currentRippleIndex] = uvPosition;
        rippleBirthTimes[currentRippleIndex] = Time.time;
        UpdateShaderProperties();
    }
    
    /// <summary>
    /// Spawn a ripple at world/screen position
    /// </summary>
    public void SpawnRippleAtScreenPos(Vector2 screenPos)
    {
        RectTransform rt = rippleImage?.GetComponent<RectTransform>();
        if (rt == null) return;
        
        // Convert screen position to UV (0-1)
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out localPoint))
        {
            Vector2 uv = new Vector2(
                (localPoint.x / rt.rect.width) + 0.5f,
                (localPoint.y / rt.rect.height) + 0.5f
            );
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
            SpawnRippleAt(uv);
        }
    }
    
    /// <summary>
    /// Change ripple intensity at runtime
    /// </summary>
    public void SetIntensity(float intensity)
    {
        if (rippleMaterial != null)
        {
            rippleMaterial.SetFloat("_Alpha", rippleAlpha * intensity);
        }
    }
    
    /// <summary>
    /// Change ripple color at runtime
    /// </summary>
    public void SetRippleColor(Color newColor)
    {
        rippleColor = newColor;
        if (rippleMaterial != null)
        {
            rippleMaterial.SetColor("_RippleColor", rippleColor);
        }
    }
    
    void OnDestroy()
    {
        if (rippleMaterial != null)
        {
            Destroy(rippleMaterial);
        }
    }
}
