using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 
using System.Collections;
using UnityEngine.UI;

public class SplashScreenManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The name of the scene to load after the splash screen finishes.")]
    [SerializeField] private string nextSceneName = "GameScene";

    [Header("Timing Settings")]
    [Tooltip("How long the splash screen stays fully visible (excluding fade times).")]
    [SerializeField] private float displayDuration = 2.0f;
    [Tooltip("How long it takes to fade in.")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [Tooltip("How long it takes to fade out.")]
    [SerializeField] private float fadeOutDuration = 1.0f;

    [Header("UI References")]
    [Tooltip("The CanvasGroup component on the UI element (e.g., Panel or Image) you want to fade.")]
    [SerializeField] private CanvasGroup splashCanvasGroup;

    private bool _hasStartedLoading = false;

    private void Start()
    {
        // Initialize alpha
        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 0f;
        }

        StartCoroutine(SplashSequence());
    }

    private void Update()
    {
        // Allow user to skip if they press any key (optional)
        // Using new Input System check
        if (!_hasStartedLoading && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            StopAllCoroutines();
            StartCoroutine(SkipSplashSequence());
        }
    }

    private IEnumerator SplashSequence()
    {
        // 1. Fade In
        if (splashCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                splashCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            splashCanvasGroup.alpha = 1f;
        }

        // 2. Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // 3. Fade Out
        if (splashCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }
            splashCanvasGroup.alpha = 0f;
        }

        // 4. Load Next Scene
        LoadNextScene();
    }

    private IEnumerator SkipSplashSequence()
    {
        // Fast fade out if skipping
        if (splashCanvasGroup != null)
        {
            float startAlpha = splashCanvasGroup.alpha;
            float elapsed = 0f;
            float skipFadeTime = 0.5f;

            while (elapsed < skipFadeTime)
            {
                elapsed += Time.deltaTime;
                splashCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / skipFadeTime);
                yield return null;
            }
            splashCanvasGroup.alpha = 0f;
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (_hasStartedLoading) return;
        _hasStartedLoading = true;

        Debug.Log($"Splash Screen Complete. Loading {nextSceneName}...");

        // Use LoadingScreenManager if available for a smooth transition with loading bar
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene(nextSceneName);
        }
        else
        {
            // Fallback: Direct load
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
