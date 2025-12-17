using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Global UI Audio Manager that plays sounds for all button clicks in the scene.
/// Attach this to a persistent GameObject and it will automatically hook into all buttons.
/// </summary>
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    
    [Header("Settings")]
    [SerializeField] private float clickVolume = 1f;
    [SerializeField] private float hoverVolume = 0.5f;
    [SerializeField] private bool autoHookButtons = true;
    
    [Header("Manual Button Assignment")]
    [Tooltip("Drag buttons here if auto-hook fails to find them")]
    [SerializeField] private Button[] manualButtons;
    
    private AudioSource audioSource;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }
    
    void Start()
    {
        // Hook auto-discovered buttons
        if (autoHookButtons)
        {
            HookAllButtons();
        }
        
        // Hook manually assigned buttons
        HookManualButtons();
    }
    
    /// <summary>
    /// Hooks buttons that were manually assigned in the Inspector
    /// </summary>
    public void HookManualButtons()
    {
        if (manualButtons == null || manualButtons.Length == 0) return;
        
        int hookedCount = 0;
        foreach (Button button in manualButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
                button.onClick.AddListener(PlayClickSound);
                hookedCount++;
            }
        }
        
        if (hookedCount > 0)
        {
            Debug.Log($"[UIAudioManager] Hooked {hookedCount} manual buttons for click sounds");
        }
    }
    
    /// <summary>
    /// Finds all Button components in the scene and adds click sound listeners
    /// </summary>
    public void HookAllButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        
        foreach (Button button in allButtons)
        {
            // Remove any existing listener to avoid duplicates
            button.onClick.RemoveListener(PlayClickSound);
            // Add click sound listener
            button.onClick.AddListener(PlayClickSound);
        }
        
        Debug.Log($"[UIAudioManager] Hooked {allButtons.Length} buttons for click sounds");
    }
    
    /// <summary>
    /// Hook a specific button (useful for dynamically created buttons)
    /// </summary>
    public void HookButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveListener(PlayClickSound);
        button.onClick.AddListener(PlayClickSound);
    }
    
    
    /// <summary>
    /// Plays the button click sound
    /// </summary>
    public void PlayClickSound()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound, clickVolume * volumeMultiplier);
        }
    }
    
    /// <summary>
    /// Plays the hover sound (call this from UIButtonAnimation or similar)
    /// </summary>
    public void PlayHoverSound()
    {
        if (buttonHoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonHoverSound, hoverVolume * volumeMultiplier);
        }
    }
    
    /// <summary>
    /// Plays a custom sound effect
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume * volumeMultiplier);
        }
    }
    
    private float volumeMultiplier = 1f;
    
    /// <summary>
    /// Sets the volume multiplier for all UI sounds
    /// </summary>
    public void SetVolume(float volume)
    {
        volumeMultiplier = Mathf.Clamp01(volume);
    }
    
    /// <summary>
    /// Gets the current volume multiplier
    /// </summary>
    public float GetVolume()
    {
        return volumeMultiplier;
    }
}
