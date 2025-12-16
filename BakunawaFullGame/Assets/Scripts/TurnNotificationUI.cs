using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TurnNotificationUI : MonoBehaviour
{
    public static TurnNotificationUI Instance;

    [Header("UI References")]
    public GameObject overlayCanvas;
    public Image dimmerImage;
    public TextMeshProUGUI notificationText;
    public RectTransform textRect;

    [Header("Settings")]
    public TMP_FontAsset notificationFont;
    public float dimmerAlpha = 0.7f;
    public float slideInDuration = 0.5f;
    public float pauseDuration = 0.8f;
    public float slideOutDuration = 0.4f;
    
    [Header("Bloom Settings")]
    public float bloomIntensity = 1.5f;
    public float bloomThreshold = 0.5f;
    
    private Volume bloomVolume;
    private Bloom bloomEffect;
    private float originalBloomIntensity = 0f;
    private float originalBloomThreshold = 0.9f;
    
    // Prevent overlapping notifications
    private bool isPlaying = false;
    private Coroutine currentNotificationCoroutine;

    private void Awake()
    {
        // Singleton - destroy duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureUI();
    }

    private void EnsureUI()
    {
        // 1. Validate Canvas - Use Screen Space Camera for post-processing bloom support
        if (overlayCanvas == null)
        {
            GameObject existing = GameObject.Find("TurnNotificationCanvas");
            if (existing != null)
            {
                overlayCanvas = existing;
            }
            else
            {
                GameObject canvasObj = new GameObject("TurnNotificationCanvas");
                Canvas c = canvasObj.AddComponent<Canvas>();
                
                // Use Screen Space Camera so post-processing (bloom) applies to UI
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = Camera.main;
                c.planeDistance = 50f; // Further from camera to avoid clipping issues
                c.sortingOrder = 999; 
                
                CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                
                canvasObj.AddComponent<GraphicRaycaster>();
                overlayCanvas = canvasObj;
            }
        }

        // 2. Validate Dimmer
        if (dimmerImage == null)
        {
            Transform t = overlayCanvas.transform.Find("Dimmer");
            if (t != null) dimmerImage = t.GetComponent<Image>();
            
            if (dimmerImage == null)
            {
                GameObject dimmerObj = new GameObject("Dimmer");
                dimmerObj.transform.SetParent(overlayCanvas.transform, false);
                dimmerImage = dimmerObj.AddComponent<Image>();
                dimmerImage.color = new Color(0, 0, 0, 0);
                dimmerImage.raycastTarget = false;
                RectTransform dr = dimmerImage.rectTransform;
                dr.anchorMin = Vector2.zero;
                dr.anchorMax = Vector2.one;
                dr.sizeDelta = Vector2.zero;
            }
        }

        // 3. Validate Text
        if (notificationText == null)
        {
             Transform t = overlayCanvas.transform.Find("NotificationText");
             if (t != null) notificationText = t.GetComponent<TextMeshProUGUI>();
             
             if (notificationText == null)
             {
                Transform oldContainer = overlayCanvas.transform.Find("TextContainer");
                if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

                GameObject textObj = new GameObject("NotificationText");
                textObj.transform.SetParent(overlayCanvas.transform, false);
                
                notificationText = textObj.AddComponent<TextMeshProUGUI>();
                
                // Load Barbara SDF font
                if (notificationFont == null)
                {
                    // Try Resources folder first
                    notificationFont = Resources.Load<TMP_FontAsset>("Fonts/Barbara SDF 1");
                }
                
                if (notificationFont == null)
                {
                    // Fallback: Search for any Barbara font
                    TMP_FontAsset[] allFonts = Resources.LoadAll<TMP_FontAsset>("");
                    foreach (var f in allFonts)
                    {
                        if (f.name.Contains("Barbara"))
                        {
                            notificationFont = f;
                            break;
                        }
                    }
                }

                if (notificationFont != null) 
                {
                     notificationText.font = notificationFont;
                     Debug.Log($"TurnNotificationUI: Using font '{notificationFont.name}'");
                }
                else
                {
                     Debug.LogWarning("TurnNotificationUI: Barbara SDF font not found! Using default.");
                     notificationText.font = TMP_Settings.defaultFontAsset;
                }
                
                notificationText.fontSize = 100;
                notificationText.alignment = TextAlignmentOptions.Center;
                notificationText.textWrappingMode = TextWrappingModes.NoWrap;
                notificationText.raycastTarget = false;
             }
        }

        // 4. Validate Rect
        if (textRect == null && notificationText != null)
        {
            textRect = notificationText.rectTransform;
            textRect.anchorMin = new Vector2(0, 0.5f);
            textRect.anchorMax = new Vector2(1, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(0, 200);
            textRect.anchoredPosition = new Vector2(-Screen.width, 0); 
        }
        
        // 5. Find or Create Bloom Volume
        SetupBloom();
        
        // 6. CRITICAL: Disable canvas initially to prevent blocking other UI
        if (overlayCanvas != null)
        {
            overlayCanvas.SetActive(false);
            
            // Also disable GraphicRaycaster to prevent any input blocking
            GraphicRaycaster gr = overlayCanvas.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = false;
        }
    }
    
    private void SetupBloom()
    {
        // Try to find existing global volume
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (var v in volumes)
        {
            if (v.profile != null && v.profile.TryGet(out Bloom b))
            {
                bloomVolume = v;
                bloomEffect = b;
                originalBloomIntensity = b.intensity.value;
                originalBloomThreshold = b.threshold.value;
                return;
            }
        }
        
        // No bloom volume found - create one
        GameObject volObj = new GameObject("TurnNotificationBloomVolume");
        bloomVolume = volObj.AddComponent<Volume>();
        bloomVolume.isGlobal = true;
        bloomVolume.priority = 100; // High priority to override
        
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        bloomEffect = profile.Add<Bloom>();
        bloomEffect.intensity.overrideState = true;
        bloomEffect.intensity.value = 0f; // Start disabled
        bloomEffect.threshold.overrideState = true;
        bloomEffect.threshold.value = 0.5f;
        bloomEffect.scatter.overrideState = true;
        bloomEffect.scatter.value = 0.7f;
        
        bloomVolume.profile = profile;
    }

    public IEnumerator PlayTurnNotification(string text, Color textColor)
    {
        // If already playing, stop the current one first
        if (isPlaying && currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
            // Clean up - hide canvas immediately
            if (overlayCanvas != null) overlayCanvas.SetActive(false);
            // Restore bloom
            if (bloomEffect != null)
            {
                bloomEffect.intensity.value = originalBloomIntensity;
                bloomEffect.threshold.value = originalBloomThreshold;
            }
        }
        
        isPlaying = true;
        
        EnsureUI();
        
        if (overlayCanvas == null)
        {
            Debug.LogWarning("[TurnNotificationUI] overlayCanvas is null after EnsureUI!");
            isPlaying = false;
            yield break;
        }
        
        overlayCanvas.SetActive(true);
        
        // Enable Bloom for notification
        if (bloomEffect != null)
        {
            bloomEffect.intensity.value = bloomIntensity;
            bloomEffect.threshold.value = bloomThreshold;
        }
        
        if (notificationText != null)
        {
            notificationText.text = text;
            
            // Use HDR color (intensity > 1) to trigger bloom
            float hdrIntensity = 3f; // Multiplier for bloom pickup
            Color hdrColor = new Color(textColor.r * hdrIntensity, textColor.g * hdrIntensity, textColor.b * hdrIntensity, 1f);
            notificationText.color = hdrColor;

            Material mat = notificationText.fontMaterial; 
            
            // INTENSE GLOW SETTINGS with HDR
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", hdrColor); // HDR underlay
            mat.SetFloat("_UnderlayOffsetX", 0);
            mat.SetFloat("_UnderlayOffsetY", 0);
            mat.SetFloat("_UnderlayDilate", 1.0f);
            mat.SetFloat("_UnderlaySoftness", 0.3f);
            
            // Outline
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor("_OutlineColor", Color.black);
            mat.SetFloat("_OutlineWidth", 0.2f);
            
            // Force Update
            notificationText.SetAllDirty();
        }

        // Reset positions
        float screenWidth = Screen.width;
        if (textRect != null) textRect.anchoredPosition = new Vector2(-screenWidth * 1.5f, 0);
        if (dimmerImage != null) dimmerImage.color = new Color(0, 0, 0, 0);


        // 1. Dim screen and Slide In (Fast in)
        float elapsed = 0f;
        float spawnTimer = 0f;
        
        while (elapsed < slideInDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            spawnTimer += dt;
            
            float t = elapsed / slideInDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t); // EaseInOut

            // Dimmer
            if (dimmerImage != null) dimmerImage.color = new Color(0, 0, 0, Mathf.Lerp(0, dimmerAlpha, smoothT));

            // Slide Text: Left -> Center
            if (textRect != null)
            {
                float xPos = Mathf.Lerp(-screenWidth * 1.5f, 0, smoothT);
                textRect.anchoredPosition = new Vector2(xPos, 0);
                
                // Trail Particles
                if (spawnTimer > 0.05f)
                {
                    CreateParticleBurst(textRect.position, textColor, 5, 20f); // Small trail
                    spawnTimer = 0f;
                }
            }

            yield return null;
        }

        // IMPACT BURST at center
        if (textRect != null) CreateParticleBurst(textRect.position, textColor, 80, 150f);

        // 2. Slow middle movement
        elapsed = 0f;
        Vector2 startCenter = new Vector2(0, 0);
        Vector2 endCenter = new Vector2(50, 0); 
        
        while (elapsed < pauseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pauseDuration;
            
            if (textRect != null) textRect.anchoredPosition = Vector2.Lerp(startCenter, endCenter, t);
            yield return null;
        }

        // 3. Accelerate to Right and Undim
        elapsed = 0f;
        spawnTimer = 0f;
        
        while (elapsed < slideOutDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            spawnTimer += dt;
            
            float t = elapsed / slideOutDuration;
            float easeIn = t * t; 

            if (dimmerImage != null) dimmerImage.color = new Color(0, 0, 0, Mathf.Lerp(dimmerAlpha, 0, t));

            if (textRect != null)
            {
                float xPos = Mathf.Lerp(50, screenWidth * 1.5f, easeIn);
                textRect.anchoredPosition = new Vector2(xPos, 0);
                
                // Trail Particles
                if (spawnTimer > 0.05f)
                {
                    CreateParticleBurst(textRect.position, textColor, 8, 30f);
                    spawnTimer = 0f;
                }
            }

            yield return null;
        }

        // Restore bloom settings
        if (bloomEffect != null)
        {
            bloomEffect.intensity.value = originalBloomIntensity;
            bloomEffect.threshold.value = originalBloomThreshold;
        }

        if (overlayCanvas != null) overlayCanvas.SetActive(false);
        
        isPlaying = false;
    }

    // --- PARTICLE SYSTEM (Adapted from HandManager) ---
    void CreateParticleBurst(Vector3 pos, Color baseColor, int count, float spread)
    {
        if (overlayCanvas == null) return;

        // 1. Container
        GameObject container = new GameObject("ParticleBurst");
        container.transform.SetParent(overlayCanvas.transform, false); // Parent to our canvas
        container.transform.position = pos;
        
        // To be safe, let's put it behind text but in front of dimmer. 
        if (dimmerImage != null)
            container.transform.SetSiblingIndex(dimmerImage.transform.GetSiblingIndex() + 1);
        
        // 2. Spawn Sprites
        List<RectTransform> sparks = new List<RectTransform>();
        List<Vector2> velocities = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            GameObject s = new GameObject("Spark");
            s.transform.SetParent(container.transform, false);
            s.transform.localPosition = Random.insideUnitCircle * spread; // Slight random offset
            s.transform.localScale = Vector3.one;
            
            Image img = s.AddComponent<Image>();
            img.raycastTarget = false;
            
            // Color Variation
            float rVal = Random.value;
            if (rVal > 0.7f) img.color = Color.white;
            else if (rVal > 0.3f) img.color = baseColor;
            else img.color = Color.Lerp(baseColor, Color.white, 0.5f);
            
            // Random direction/Velocity
            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(400f, 1000f); 
            velocities.Add(dir * speed);

            RectTransform rt = s.GetComponent<RectTransform>();
            float size = Random.Range(10f, 40f);
            rt.sizeDelta = new Vector2(size, size);
            sparks.Add(rt);
        }

        StartCoroutine(AnimateParticles(container, sparks, velocities));
    }

    IEnumerator AnimateParticles(GameObject container, List<RectTransform> sparks, List<Vector2> velocities)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration && container != null)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float t = elapsed / duration;

            for (int i = 0; i < sparks.Count; i++)
            {
                if (sparks[i] == null) continue;

                // Move
                sparks[i].anchoredPosition += velocities[i] * dt;

                // Drag/Slow down
                velocities[i] = Vector2.Lerp(velocities[i], Vector2.zero, dt * 5f);

                // Fade & Shrink
                Image img = sparks[i].GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Lerp(1f, 0f, t * t);
                    img.color = c;
                }
                sparks[i].localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            }
            yield return null;
        }

        if (container != null) Destroy(container);
    }
}
