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
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;

    private Resolution[] resolutions;

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

        // --- Volume Setup ---
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVolume;
            if (volumeValueText != null) UpdateVolumeText(savedVolume);
            
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        Debug.Log($"Resolution set to: {resolution.width}x{resolution.height}");
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
        if (volumeValueText != null) UpdateVolumeText(volume);
    }

    private void UpdateVolumeText(float volume)
    {
        volumeValueText.text = Mathf.RoundToInt(volume * 100) + "%";
    }

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
}
