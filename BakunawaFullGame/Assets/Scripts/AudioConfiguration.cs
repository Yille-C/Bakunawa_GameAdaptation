using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that stores audio clip categorization.
/// Create via Assets > Create > Bakunawa > Audio Configuration
/// </summary>
[CreateAssetMenu(fileName = "AudioConfiguration", menuName = "Bakunawa/Audio Configuration")]
public class AudioConfiguration : ScriptableObject
{
    [System.Serializable]
    public class AudioClipEntry
    {
        public AudioClip clip;
        public AudioCategory category;
        public string path; // For reference
    }
    
    [Header("Audio Categorization")]
    [Tooltip("List of all categorized audio clips")]
    public List<AudioClipEntry> audioClips = new List<AudioClipEntry>();
    
    /// <summary>
    /// Gets the category for a given audio clip
    /// </summary>
    public AudioCategory GetCategory(AudioClip clip)
    {
        if (clip == null) return AudioCategory.SFX;
        
        foreach (var entry in audioClips)
        {
            if (entry.clip == clip)
                return entry.category;
        }
        
        // Default to SFX if not found
        return AudioCategory.SFX;
    }
    
    /// <summary>
    /// Sets or updates the category for an audio clip
    /// </summary>
    public void SetCategory(AudioClip clip, AudioCategory category, string path = "")
    {
        if (clip == null) return;
        
        // Check if already exists
        for (int i = 0; i < audioClips.Count; i++)
        {
            if (audioClips[i].clip == clip)
            {
                audioClips[i].category = category;
                return;
            }
        }
        
        // Add new entry
        audioClips.Add(new AudioClipEntry
        {
            clip = clip,
            category = category,
            path = path
        });
    }
    
    /// <summary>
    /// Checks if a clip is categorized
    /// </summary>
    public bool HasClip(AudioClip clip)
    {
        foreach (var entry in audioClips)
        {
            if (entry.clip == clip)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Removes a clip from the configuration
    /// </summary>
    public void RemoveClip(AudioClip clip)
    {
        audioClips.RemoveAll(e => e.clip == clip);
    }
    
    /// <summary>
    /// Cleans up null references
    /// </summary>
    public void CleanupNullReferences()
    {
        audioClips.RemoveAll(e => e.clip == null);
    }
    
    /// <summary>
    /// Gets all clips of a specific category
    /// </summary>
    public List<AudioClip> GetClipsByCategory(AudioCategory category)
    {
        List<AudioClip> result = new List<AudioClip>();
        foreach (var entry in audioClips)
        {
            if (entry.category == category && entry.clip != null)
                result.Add(entry.clip);
        }
        return result;
    }
    
    // Singleton instance for easy access
    private static AudioConfiguration _instance;
    public static AudioConfiguration Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<AudioConfiguration>("AudioConfiguration");
            }
            return _instance;
        }
    }
}
