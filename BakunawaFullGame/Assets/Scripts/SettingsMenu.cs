using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    
    [Header("Volume Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    
    // Legacy support - single volume slider
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;

    private Resolution[] resolutions;
    
    // PlayerPrefs keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    // Cached volume values
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private void Start()
    {
        InitializeSettings();
    }

    private void InitializeSettings()
    {
        // --- Resolution Setup ---
        resolutions = Screen.resolutions;
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            List<Resolution> uniqueResolutions = new List<Resolution>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                // key check to avoid duplicates
                bool isDuplicate = false;
                foreach(var res in uniqueResolutions) {
                    if (res.width == resolutions[i].width && res.height == resolutions[i].height) {
                        isDuplicate = true;
                        break;
                    }
                }
                
                if (!isDuplicate)
                {
                    uniqueResolutions.Add(resolutions[i]);
                }
            }

            // Limit to top 5 highest resolutions (assuming Screen.resolutions is Low->High)
            // We take the last 5 elements
            if (uniqueResolutions.Count > 5)
            {
                uniqueResolutions = uniqueResolutions.GetRange(uniqueResolutions.Count - 5, 5);
            }

            // Create options list from the filtered set
            for (int i = 0; i < uniqueResolutions.Count; i++)
            {
                string option = uniqueResolutions[i].width + " x " + uniqueResolutions[i].height;
                options.Add(option);

                // Check if this matches current screen resolution
                if (uniqueResolutions[i].width == Screen.width &&
                    uniqueResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                }
            }
            
            // update our local array to match the unique list indices
            resolutions = uniqueResolutions.ToArray();

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
            
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        // --- Quality Setup ---
        if (qualityDropdown != null)
        {
            // Populate quality levels based on project settings
            qualityDropdown.ClearOptions();
            List<string> qualityOptions = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(qualityOptions);
            
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
            
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        // --- Fullscreen Setup ---
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // --- Volume Setup (New separated controls) ---
        
        // Master Volume
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVolume;
            UpdateVolumeText(masterVolumeText, masterVolume);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        
        // Music Volume
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = musicVolume;
            UpdateVolumeText(musicVolumeText, musicVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        
        // SFX Volume
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVolume;
            UpdateVolumeText(sfxVolumeText, sfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        
        // Apply initial volumes
        ApplyAllVolumes();

        // --- Legacy Volume Setup (single slider fallback) ---
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            volumeSlider.value = savedVolume;
            UpdateVolumeText(volumeValueText, savedVolume);
            
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        Debug.Log($"Resolution set to: {resolution.width}x{resolution.height}");
    }

    #region Volume Controls

    /// <summary>
    /// Sets Master Volume - affects all audio
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        UpdateVolumeText(masterVolumeText, volume);
        ApplyAllVolumes();
    }
    
    /// <summary>
    /// Sets Music Volume - affects BGM and ambient sounds
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        UpdateVolumeText(musicVolumeText, volume);
        ApplyMusicVolume();
    }
    
    /// <summary>
    /// Sets SFX Volume - affects sound effects
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        UpdateVolumeText(sfxVolumeText, volume);
        ApplySFXVolume();
    }
    
    /// <summary>
    /// Legacy single volume control
    /// </summary>
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        UpdateVolumeText(volumeValueText, volume);
    }
    
    /// <summary>
    /// Applies all volume settings to audio managers
    /// </summary>
    private void ApplyAllVolumes()
    {
        // Set AudioListener for master volume
        AudioListener.volume = masterVolume;
        
        ApplyMusicVolume();
        ApplySFXVolume();
    }
    
    /// <summary>
    /// Applies music volume to relevant audio sources
    /// </summary>
    private void ApplyMusicVolume()
    {
        float effectiveVolume = musicVolume; // Master is applied via AudioListener
        
        // Apply to GameAudioManager if available
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetMusicVolume(effectiveVolume);
        }
    }
    
    /// <summary>
    /// Applies SFX volume to relevant audio sources
    /// </summary>
    private void ApplySFXVolume()
    {
        float effectiveVolume = sfxVolume; // Master is applied via AudioListener
        
        // Apply to GameAudioManager if available
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetSFXVolume(effectiveVolume);
        }
        
        // Apply to UIAudioManager if available
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.SetVolume(effectiveVolume);
        }
    }

    private void UpdateVolumeText(TextMeshProUGUI textComponent, float volume)
    {
        if (textComponent != null)
        {
            textComponent.text = Mathf.RoundToInt(volume * 100) + "%";
        }
    }

    #endregion

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        Debug.Log($"Quality level set to: {QualitySettings.names[qualityIndex]}");
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"Fullscreen set to: {isFullscreen}");
    }
    
    #region Static Volume Accessors
    
    /// <summary>
    /// Gets the saved master volume (0-1)
    /// </summary>
    public static float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
    }
    
    /// <summary>
    /// Gets the saved music volume (0-1)
    /// </summary>
    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
    }
    
    /// <summary>
    /// Gets the saved SFX volume (0-1)
    /// </summary>
    public static float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
    }
    
    #endregion
}
