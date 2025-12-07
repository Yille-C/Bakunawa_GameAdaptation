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
    
    // We will find these automatically from the objects above
    private CanvasGroup canvasGroup; 
    private Text legacyText;
    private TMPro.TMP_Text tmpText; // Reference to TextMeshPro if it exists

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minimumLoadTime = 2.0f;

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
        }

        float startTime = Time.time;
        AsyncOperation asyncLoad;

        if (sceneIdentifier is string)
            asyncLoad = SceneManager.LoadSceneAsync((string)sceneIdentifier);
        else
            asyncLoad = SceneManager.LoadSceneAsync((int)sceneIdentifier);

        asyncLoad.allowSceneActivation = false;

        // 3. Update Progress
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // Update Bar
            if (progressBar != null)
                progressBar.fillAmount = progress;
            
            // Update Text
            string msg = $"LOADING... {(progress * 100):0}%";
            if (legacyText != null) legacyText.text = msg;
            if (tmpText != null) tmpText.text = msg;

            // Check completion
            if (asyncLoad.progress >= 0.9f && (Time.time - startTime >= minimumLoadTime))
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
        }
    }
}
