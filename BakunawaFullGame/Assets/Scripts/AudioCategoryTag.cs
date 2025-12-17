using UnityEngine;

/// <summary>
/// Enum to categorize audio types for volume control routing
/// </summary>
public enum AudioCategory
{
    SFX,        // Sound effects (button clicks, card sounds, impacts, etc.)
    Music,      // Background music and ambient sounds
    UI          // UI-specific sounds (can be grouped with SFX or separate)
}

/// <summary>
/// Component to tag an AudioSource with its category for volume control routing.
/// Attach this to any GameObject with an AudioSource to control which volume slider affects it.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioCategoryTag : MonoBehaviour
{
    [Header("Audio Classification")]
    [Tooltip("The category this audio source belongs to. Determines which volume slider controls it.")]
    public AudioCategory category = AudioCategory.SFX;
    
    [Header("Volume Settings")]
    [Tooltip("Base volume for this audio source (before category volume is applied)")]
    [Range(0f, 1f)]
    public float baseVolume = 1f;
    
    private AudioSource audioSource;
    private float lastAppliedCategoryVolume = 1f;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            baseVolume = audioSource.volume;
        }
    }
    
    void Start()
    {
        ApplyVolumeSettings();
    }
    
    void OnEnable()
    {
        // Subscribe to volume changes if AudioVolumeManager exists
        if (AudioVolumeManager.Instance != null)
        {
            AudioVolumeManager.Instance.OnVolumeChanged += OnVolumeChanged;
        }
        ApplyVolumeSettings();
    }
    
    void OnDisable()
    {
        if (AudioVolumeManager.Instance != null)
        {
            AudioVolumeManager.Instance.OnVolumeChanged -= OnVolumeChanged;
        }
    }
    
    private void OnVolumeChanged(AudioCategory changedCategory, float newVolume)
    {
        if (changedCategory == category)
        {
            ApplyVolumeSettings();
        }
    }
    
    /// <summary>
    /// Applies the current category volume settings to this audio source
    /// </summary>
    public void ApplyVolumeSettings()
    {
        if (audioSource == null) return;
        
        float categoryVolume = GetCategoryVolume();
        float masterVolume = SettingsMenu.GetMasterVolume();
        
        audioSource.volume = baseVolume * categoryVolume;
        lastAppliedCategoryVolume = categoryVolume;
    }
    
    /// <summary>
    /// Gets the volume for this audio source's category
    /// </summary>
    private float GetCategoryVolume()
    {
        switch (category)
        {
            case AudioCategory.Music:
                return SettingsMenu.GetMusicVolume();
            case AudioCategory.SFX:
            case AudioCategory.UI:
            default:
                return SettingsMenu.GetSFXVolume();
        }
    }
    
    /// <summary>
    /// Plays a one-shot clip with the current category volume applied
    /// </summary>
    public void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        
        float categoryVolume = GetCategoryVolume();
        audioSource.PlayOneShot(clip, baseVolume * categoryVolume);
    }
    
    /// <summary>
    /// Plays a one-shot clip with custom volume (category volume still applied)
    /// </summary>
    public void PlayOneShot(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        
        float categoryVolume = GetCategoryVolume();
        audioSource.PlayOneShot(clip, volume * categoryVolume);
    }
}
