using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
// Alias for TMP to avoid errors if not installed, though usually it is. 
// We will use GetComponent(s) dynamic checks to be safe.

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance;

    [Header("UI References")]
    [Tooltip("Drag the Panel or Canvas object here.")]
    [SerializeField] private GameObject loadingScreenCanvas; 
    
    [Tooltip("Drag the Text object here (supports Legacy Text or TextMeshPro).")]
    [SerializeField] private GameObject loadingTextObject;   
    
    [Tooltip("Drag the Progress Bar Image object here.")]
    [SerializeField] private Image progressBar;              

    [Tooltip("Drag the Slider object here (alternative to Image fill).")]
    [SerializeField] private Slider progressSlider;
    
    // We will find these automatically from the objects above
    private CanvasGroup canvasGroup; 
    private Text legacyText;
    private TMPro.TMP_Text tmpText; // Reference to TextMeshPro if it exists

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minimumLoadTime = 2.0f;

    [Header("Progress Bar Settings")]
    [SerializeField] private bool useSmoothProgress = true;
    [SerializeField] private float smoothFillSpeed = 5.0f;

    [Header("Animation Details")]
    [Tooltip("The Cloud object to animate (Horizontal movement).")]
    [SerializeField] private RectTransform cloudRect;
    [SerializeField] private float cloudSwaySpeed = 0.5f;
    [SerializeField] private float cloudSwayDistance = 20f;

    [Tooltip("The Moon object (Subtle glow).")]
    [SerializeField] private RectTransform moonRect;
    [SerializeField] private float moonGlowSpeed = 0.5f;
    [SerializeField] private float moonMinAlpha = 0.7f;
    [SerializeField] private float moonMaxAlpha = 1.0f;

    [Tooltip("The Stars object or group (Twinkling/Alpha fade).")]
    [SerializeField] private GameObject starsObject;
    [SerializeField] private float starsTwinkleSpeed = 1.0f;
    [SerializeField] private float starsMinAlpha = 0.4f;
    [SerializeField] private float starsMaxAlpha = 1.0f;

    [Tooltip("Optional Particle System for extra effects.")]
    [SerializeField] private ParticleSystem loadingParticles;

    private Vector2 cloudStartPos;
    private CanvasGroup moonGroup;
    private CanvasGroup starsGroup;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 1. Setup Canvas/Panel Group
        if (loadingScreenCanvas != null)
        {
            // Try get CanvasGroup, add if missing
            canvasGroup = loadingScreenCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = loadingScreenCanvas.AddComponent<CanvasGroup>();
            }
            loadingScreenCanvas.SetActive(false);
            
            // Auto-find Animation Elements if not assigned
            if (cloudRect == null)
            {
                // Try "Cloud" or "Clouds"
                Transform t = loadingScreenCanvas.transform.Find("Cloud");
                if (t == null) t = loadingScreenCanvas.transform.Find("Clouds");
                if (t != null) cloudRect = t as RectTransform;
            }
            if (moonRect == null)
            {
                Transform t = loadingScreenCanvas.transform.Find("Moon");
                if (t != null) moonRect = t as RectTransform;
            }
            if (starsObject == null)
            {
                // Try "Stars" or "Star"
                Transform t = loadingScreenCanvas.transform.Find("Stars");
                if (t == null) t = loadingScreenCanvas.transform.Find("Star");
                if (t != null) starsObject = t.gameObject;
            }
            if (loadingParticles == null)
            {
                Transform t = loadingScreenCanvas.transform.Find("Particles");
                if (t != null) loadingParticles = t.GetComponent<ParticleSystem>();
            }
        }

        // Initialize Animation States
        if (cloudRect != null) cloudStartPos = cloudRect.anchoredPosition;
        if (moonRect != null)
        {
            moonGroup = moonRect.GetComponent<CanvasGroup>();
            if (moonGroup == null) moonGroup = moonRect.gameObject.AddComponent<CanvasGroup>();
        }
        if (starsObject != null)
        {
            starsGroup = starsObject.GetComponent<CanvasGroup>();
            if (starsGroup == null) starsGroup = starsObject.AddComponent<CanvasGroup>();
        }

        // 2. Setup Text (Legacy vs TMP)
        if (loadingTextObject != null)
        {
            legacyText = loadingTextObject.GetComponent<Text>();
            // Try get TMP via reflection or direct type if we added user "using TMPro;" 
            // but to be safe without enforcing TMP dependencies, we stick to standard checks or simple assumed component.
            // For now, let's assume if legacy is null, check for generic component.
             tmpText = loadingTextObject.GetComponent<TMPro.TMP_Text>();
        }
    }

    private void Update()
    {
        // Only animate if loading screen is visible
        if (loadingScreenCanvas != null && loadingScreenCanvas.activeInHierarchy)
        {
            float time = Time.time;

            // 1. Cloud: Blowing wind (Horizontal Sway)
            if (cloudRect != null)
            {
                float offset = Mathf.Sin(time * cloudSwaySpeed) * cloudSwayDistance;
                cloudRect.anchoredPosition = new Vector2(cloudStartPos.x + offset, cloudStartPos.y);
            }

            // 2. Stars: Twinkle (Alpha Glow)
            if (starsGroup != null)
            {
                // PingPong between min and max alpha
                // Use PerlinNoise or Sin for smoother random-like feel? 
                // Request says "on and off glow", so smooth Sin/PingPong is good.
                float t = Mathf.PingPong(time * starsTwinkleSpeed, 1f); // 0 to 1
                starsGroup.alpha = Mathf.Lerp(starsMinAlpha, starsMaxAlpha, t);
            }

            // 3. Moon: Subtle Glow (Alpha)
            if (moonGroup != null)
            {
                float t = Mathf.PingPong(time * moonGlowSpeed, 1f);
                moonGroup.alpha = Mathf.Lerp(moonMinAlpha, moonMaxAlpha, t);
            }
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadSceneRoutine(sceneIndex));
    }

    private IEnumerator LoadSceneRoutine(object sceneIdentifier)
    {
        // 1. Show Loading Screen
        if (loadingScreenCanvas != null)
        {
            loadingScreenCanvas.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                while (canvasGroup.alpha < 1)
                {
                    canvasGroup.alpha += Time.deltaTime / fadeDuration;
                    yield return null;
                }
                canvasGroup.alpha = 1;
            }
            
            if (loadingParticles != null) loadingParticles.Play();
        }

        float startTime = Time.time;
        AsyncOperation asyncLoad;

        if (sceneIdentifier is string)
            asyncLoad = SceneManager.LoadSceneAsync((string)sceneIdentifier);
        else
            asyncLoad = SceneManager.LoadSceneAsync((int)sceneIdentifier);

        asyncLoad.allowSceneActivation = false;

        // Reset animations if needed? Update handles continuous time anyway.
        
        float currentFill = 0f;

        // 3. Update Progress
        while (!asyncLoad.isDone)
        {
            float targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (useSmoothProgress)
            {
                currentFill = Mathf.Lerp(currentFill, targetProgress, Time.deltaTime * smoothFillSpeed);
                if (targetProgress >= 1f && Mathf.Abs(currentFill - 1f) < 0.01f) currentFill = 1f;
            }
            else
            {
                currentFill = targetProgress;
            }

            // Update Bar
            if (progressBar != null)
                progressBar.fillAmount = currentFill;
            if (progressSlider != null)
                progressSlider.value = currentFill;
            
            // Update Text
            string msg = $"LOADING... {(currentFill * 100):0}%";
            if (legacyText != null) legacyText.text = msg;
            if (tmpText != null) tmpText.text = msg;

            // Check completion
            bool visualDone = (!useSmoothProgress || currentFill >= 0.99f); 

            if (asyncLoad.progress >= 0.9f && (Time.time - startTime >= minimumLoadTime) && visualDone)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // 4. Fade Out
        if (loadingScreenCanvas != null && canvasGroup != null)
        {
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime / fadeDuration;
                yield return null;
            }
            loadingScreenCanvas.SetActive(false);
            if (loadingParticles != null) loadingParticles.Stop();
        }
    }
}
