using UnityEngine;
using System;

/// <summary>
/// Central manager for audio volume that broadcasts volume changes to all tagged audio sources.
/// This is a lightweight alternative to using Audio Mixers.
/// </summary>
public class AudioVolumeManager : MonoBehaviour
{
    public static AudioVolumeManager Instance { get; private set; }
    
    /// <summary>
    /// Event fired when any volume category changes. Parameters: (category, newVolume)
    /// </summary>
    public event Action<AudioCategory, float> OnVolumeChanged;
    
    private float cachedMasterVolume;
    private float cachedMusicVolume;
    private float cachedSFXVolume;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Cache initial values
        cachedMasterVolume = SettingsMenu.GetMasterVolume();
        cachedMusicVolume = SettingsMenu.GetMusicVolume();
        cachedSFXVolume = SettingsMenu.GetSFXVolume();
    }
    
    void Update()
    {
        // Check for volume changes and broadcast them
        float newMaster = SettingsMenu.GetMasterVolume();
        float newMusic = SettingsMenu.GetMusicVolume();
        float newSFX = SettingsMenu.GetSFXVolume();
        
        if (Math.Abs(newMaster - cachedMasterVolume) > 0.001f)
        {
            cachedMasterVolume = newMaster;
            // Master affects all categories
            OnVolumeChanged?.Invoke(AudioCategory.Music, newMusic);
            OnVolumeChanged?.Invoke(AudioCategory.SFX, newSFX);
            OnVolumeChanged?.Invoke(AudioCategory.UI, newSFX);
        }
        
        if (Math.Abs(newMusic - cachedMusicVolume) > 0.001f)
        {
            cachedMusicVolume = newMusic;
            OnVolumeChanged?.Invoke(AudioCategory.Music, newMusic);
        }
        
        if (Math.Abs(newSFX - cachedSFXVolume) > 0.001f)
        {
            cachedSFXVolume = newSFX;
            OnVolumeChanged?.Invoke(AudioCategory.SFX, newSFX);
            OnVolumeChanged?.Invoke(AudioCategory.UI, newSFX);
        }
    }
    
    /// <summary>
    /// Manually trigger a volume update for all audio sources of a specific category
    /// </summary>
    public void RefreshCategory(AudioCategory category)
    {
        float volume = 1f;
        switch (category)
        {
            case AudioCategory.Music:
                volume = SettingsMenu.GetMusicVolume();
                break;
            case AudioCategory.SFX:
            case AudioCategory.UI:
                volume = SettingsMenu.GetSFXVolume();
                break;
        }
        OnVolumeChanged?.Invoke(category, volume);
    }
    
    /// <summary>
    /// Refresh all audio categories
    /// </summary>
    public void RefreshAllCategories()
    {
        RefreshCategory(AudioCategory.Music);
        RefreshCategory(AudioCategory.SFX);
        RefreshCategory(AudioCategory.UI);
    }
}
