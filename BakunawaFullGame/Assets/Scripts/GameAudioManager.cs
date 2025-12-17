using UnityEngine;

/// <summary>
/// Manages all audio for the game scene including BGM, ambient sounds (rain), and SFX (thunder).
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }
    
    [Header("Background Music")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField] private bool playBGMOnStart = true;
    
    [Header("Ambient Sounds")]
    [SerializeField] private AudioClip rainAmbientClip;
    [SerializeField] [Range(0f, 1f)] private float rainVolume = 0.4f;
    [SerializeField] private bool playRainOnStart = true;
    
    [Header("Thunder Sound Effects")]
    [Tooltip("Add multiple thunder clips for variety - one will be randomly selected each strike")]
    [SerializeField] private AudioClip[] thunderClips;
    [SerializeField] [Range(0f, 1f)] private float thunderVolume = 0.8f;
    [SerializeField] private float thunderDelayMin = 0.1f; // Delay after lightning flash
    [SerializeField] private float thunderDelayMax = 0.5f;
    [SerializeField] [Range(0f, 0.3f)] private float thunderPitchVariation = 0.1f; // Pitch randomization
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxSource;
    
    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Create audio sources if not assigned
        SetupAudioSources();
    }
    
    void Start()
    {
        if (playBGMOnStart && bgmClip != null)
        {
            PlayBGM();
        }
        
        if (playRainOnStart && rainAmbientClip != null)
        {
            PlayRainAmbient();
        }
    }
    
    void SetupAudioSources()
    {
        // BGM Source
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
        }
        
        // Ambient Source
        if (ambientSource == null)
        {
            GameObject ambientObj = new GameObject("Ambient_Source");
            ambientObj.transform.SetParent(transform);
            ambientSource = ambientObj.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
        }
        
        // SFX Source
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }
    }
    
    #region BGM Controls
    
    public void PlayBGM()
    {
        if (bgmClip == null || bgmSource == null) return;
        
        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }
    
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }
    
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }
    
    public void FadeBGM(float targetVolume, float duration)
    {
        StartCoroutine(FadeAudioSource(bgmSource, targetVolume, duration));
    }
    
    #endregion
    
    #region Ambient Controls
    
    public void PlayRainAmbient()
    {
        if (rainAmbientClip == null || ambientSource == null) return;
        
        ambientSource.clip = rainAmbientClip;
        ambientSource.volume = rainVolume;
        ambientSource.Play();
    }
    
    public void StopRainAmbient()
    {
        if (ambientSource != null) ambientSource.Stop();
    }
    
    public void SetRainVolume(float volume)
    {
        rainVolume = Mathf.Clamp01(volume);
        if (ambientSource != null) ambientSource.volume = rainVolume;
    }
    
    #endregion
    
    #region Thunder SFX
    
    /// <summary>
    /// Plays a random thunder sound effect synced with lightning flash.
    /// Call this from RainEffect when lightning occurs.
    /// </summary>
    public void PlayThunder()
    {
        if (thunderClips == null || thunderClips.Length == 0 || sfxSource == null) return;
        
        // Add slight delay to simulate thunder arriving after lightning
        float delay = Random.Range(thunderDelayMin, thunderDelayMax);
        StartCoroutine(PlayThunderDelayed(delay));
    }
    
    /// <summary>
    /// Plays thunder immediately without delay
    /// </summary>
    public void PlayThunderImmediate()
    {
        if (thunderClips == null || thunderClips.Length == 0 || sfxSource == null) return;
        
        PlayRandomThunderClip();
    }
    
    /// <summary>
    /// Selects and plays a random thunder clip with volume and pitch variation
    /// </summary>
    private void PlayRandomThunderClip()
    {
        // Select random thunder clip
        AudioClip selectedClip = thunderClips[Random.Range(0, thunderClips.Length)];
        if (selectedClip == null) return;
        
        // Volume variation for realism
        float volumeVariation = Random.Range(0.85f, 1f);
        
        // Pitch variation for even more variety
        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = 1f + Random.Range(-thunderPitchVariation, thunderPitchVariation);
        
        sfxSource.PlayOneShot(selectedClip, thunderVolume * volumeVariation);
        
        // Reset pitch after a frame (PlayOneShot uses current pitch)
        StartCoroutine(ResetPitchAfterFrame(originalPitch));
    }
    
    System.Collections.IEnumerator PlayThunderDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayRandomThunderClip();
    }
    
    System.Collections.IEnumerator ResetPitchAfterFrame(float originalPitch)
    {
        yield return null;
        if (sfxSource != null) sfxSource.pitch = originalPitch;
    }
    
    #endregion
    
    #region General SFX
    
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume * sfxVolumeMultiplier);
    }
    
    private float sfxVolumeMultiplier = 1f;
    private float musicVolumeMultiplier = 1f;
    
    /// <summary>
    /// Sets the music volume multiplier (affects BGM and ambient)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolumeMultiplier = Mathf.Clamp01(volume);
        
        // Apply to BGM
        if (bgmSource != null) 
            bgmSource.volume = bgmVolume * musicVolumeMultiplier;
        
        // Apply to Ambient
        if (ambientSource != null)
            ambientSource.volume = rainVolume * musicVolumeMultiplier;
    }
    
    /// <summary>
    /// Sets the SFX volume multiplier
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolumeMultiplier = Mathf.Clamp01(volume);
        // SFX volume is applied when sounds are played via PlaySFX
    }
    
    #endregion
    
    #region Utility
    
    System.Collections.IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration)
    {
        if (source == null) yield break;
        
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }
        
        source.volume = targetVolume;
        
        if (targetVolume <= 0f)
        {
            source.Stop();
        }
    }
    
    #endregion
}
