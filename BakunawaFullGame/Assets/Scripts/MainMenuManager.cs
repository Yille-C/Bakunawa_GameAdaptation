using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Config")]
    [Tooltip("Name of the scene to load when Play is clicked")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        // Initialize Volume from Prefs
        if (volumeSlider != null)
        {
            float savedVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVol;
            AudioListener.volume = savedVol;
            
            // Add listener dynamically
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        else
        {
            // Just load the volume if slider missing
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        }
    }

    // --- Button Events ---

    public void OnPlayClicked()
    {
        PlayClickSound();
        Debug.Log("Play Clicked");
        
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnHowToPlayClicked()
    {
        PlayClickSound();
        Debug.Log("How to play clicked");
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void OnSettingsClicked()
    {
        PlayClickSound();
        Debug.Log("Settings clicked");
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        PlayClickSound();
        Debug.Log("Quit clicked");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- Panel Control ---

    public void CloseSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void CloseHowToPlay()
    {
        PlayClickSound();
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    // --- Settings Logic ---

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    // --- Audio Helpers ---

    private void PlayClickSound()
    {
        if (sfxSource != null && clickClip != null)
        {
            sfxSource.PlayOneShot(clickClip);
        }
    }

    public void PlayHoverSound() // To be called via EventTrigger if desired
    {
        if (sfxSource != null && hoverClip != null)
        {
            sfxSource.PlayOneShot(hoverClip);
        }
    }
}
