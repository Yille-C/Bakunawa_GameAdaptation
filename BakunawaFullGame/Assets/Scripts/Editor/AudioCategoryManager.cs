using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window to view and manage audio categorization in the scene.
/// Access via Tools > Bakunawa > Audio Category Manager
/// </summary>
public class AudioCategoryManager : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showTaggedSources = true;
    private bool showUntaggedSources = true;
    private AudioCategory filterCategory = AudioCategory.SFX;
    private bool useFilter = false;
    private string searchFilter = "";
    
    // Style caching
    private GUIStyle headerStyle;
    private GUIStyle categoryButtonStyle;
    private bool stylesInitialized = false;
    
    [MenuItem("Tools/Bakunawa/Audio Category Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<AudioCategoryManager>("Audio Manager");
        window.minSize = new Vector2(500, 400);
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            margin = new RectOffset(0, 0, 10, 5)
        };
        
        stylesInitialized = true;
    }
    
    private void OnGUI()
    {
        InitStyles();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Audio Category Manager", headerStyle);
        EditorGUILayout.HelpBox(
            "This tool helps you categorize AudioSources as SFX or Music.\n" +
            "• SFX: Sound effects, UI sounds, impacts\n" +
            "• Music: Background music, ambient sounds\n" +
            "Add AudioCategoryTag component to control which volume slider affects each source.",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Filters
        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField("Search", searchFilter, GUILayout.Width(300));
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        showTaggedSources = EditorGUILayout.ToggleLeft("Show Tagged", showTaggedSources, GUILayout.Width(120));
        showUntaggedSources = EditorGUILayout.ToggleLeft("Show Untagged", showUntaggedSources, GUILayout.Width(120));
        useFilter = EditorGUILayout.ToggleLeft("Filter by Category", useFilter, GUILayout.Width(130));
        if (useFilter)
        {
            filterCategory = (AudioCategory)EditorGUILayout.EnumPopup(filterCategory, GUILayout.Width(80));
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Quick Actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Height(25)))
        {
            Repaint();
        }
        if (GUILayout.Button("Tag All Untagged as SFX", GUILayout.Height(25)))
        {
            TagAllUntagged(AudioCategory.SFX);
        }
        if (GUILayout.Button("Tag All Untagged as Music", GUILayout.Height(25)))
        {
            TagAllUntagged(AudioCategory.Music);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Scrollable list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Find all AudioSources in the scene
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        
        // Separate into tagged and untagged
        List<AudioSource> taggedSources = new List<AudioSource>();
        List<AudioSource> untaggedSources = new List<AudioSource>();
        
        foreach (var source in allSources)
        {
            AudioCategoryTag tag = source.GetComponent<AudioCategoryTag>();
            if (tag != null)
            {
                if (useFilter && tag.category != filterCategory) continue;
                if (!MatchesSearch(source.gameObject.name)) continue;
                taggedSources.Add(source);
            }
            else
            {
                if (!MatchesSearch(source.gameObject.name)) continue;
                untaggedSources.Add(source);
            }
        }
        
        // Display Tagged Sources
        if (showTaggedSources && taggedSources.Count > 0)
        {
            EditorGUILayout.LabelField($"Tagged Audio Sources ({taggedSources.Count})", headerStyle);
            DrawSeparator();
            
            foreach (var source in taggedSources.OrderBy(s => s.GetComponent<AudioCategoryTag>().category))
            {
                DrawTaggedSourceRow(source);
            }
            
            EditorGUILayout.Space(15);
        }
        
        // Display Untagged Sources
        if (showUntaggedSources && untaggedSources.Count > 0)
        {
            EditorGUILayout.LabelField($"Untagged Audio Sources ({untaggedSources.Count})", headerStyle);
            EditorGUILayout.HelpBox("These AudioSources don't have an AudioCategoryTag. They will use default volume only.", MessageType.Warning);
            DrawSeparator();
            
            foreach (var source in untaggedSources)
            {
                DrawUntaggedSourceRow(source);
            }
        }
        
        if (taggedSources.Count == 0 && untaggedSources.Count == 0)
        {
            EditorGUILayout.LabelField("No AudioSources found in scene.", EditorStyles.centeredGreyMiniLabel);
        }
        
        EditorGUILayout.EndScrollView();
        
        // Summary
        DrawSeparator();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Total: {allSources.Length} sources | Tagged: {taggedSources.Count} | Untagged: {untaggedSources.Count}");
        EditorGUILayout.EndHorizontal();
    }
    
    private bool MatchesSearch(string name)
    {
        if (string.IsNullOrEmpty(searchFilter)) return true;
        return name.ToLower().Contains(searchFilter.ToLower());
    }
    
    private void DrawTaggedSourceRow(AudioSource source)
    {
        AudioCategoryTag tag = source.GetComponent<AudioCategoryTag>();
        
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        // Category color indicator
        Color categoryColor = GetCategoryColor(tag.category);
        Rect colorRect = GUILayoutUtility.GetRect(8, 20, GUILayout.Width(8));
        EditorGUI.DrawRect(colorRect, categoryColor);
        
        // GameObject name (clickable)
        if (GUILayout.Button(source.gameObject.name, EditorStyles.linkLabel, GUILayout.Width(200)))
        {
            Selection.activeGameObject = source.gameObject;
            EditorGUIUtility.PingObject(source.gameObject);
        }
        
        // Current category
        EditorGUILayout.LabelField(tag.category.ToString(), GUILayout.Width(60));
        
        // Quick category change buttons
        GUI.backgroundColor = tag.category == AudioCategory.SFX ? Color.green : Color.white;
        if (GUILayout.Button("SFX", GUILayout.Width(50)))
        {
            Undo.RecordObject(tag, "Change Audio Category");
            tag.category = AudioCategory.SFX;
            EditorUtility.SetDirty(tag);
        }
        
        GUI.backgroundColor = tag.category == AudioCategory.Music ? Color.cyan : Color.white;
        if (GUILayout.Button("Music", GUILayout.Width(50)))
        {
            Undo.RecordObject(tag, "Change Audio Category");
            tag.category = AudioCategory.Music;
            EditorUtility.SetDirty(tag);
        }
        
        GUI.backgroundColor = tag.category == AudioCategory.UI ? Color.yellow : Color.white;
        if (GUILayout.Button("UI", GUILayout.Width(35)))
        {
            Undo.RecordObject(tag, "Change Audio Category");
            tag.category = AudioCategory.UI;
            EditorUtility.SetDirty(tag);
        }
        GUI.backgroundColor = Color.white;
        
        // Base volume
        EditorGUILayout.LabelField("Vol:", GUILayout.Width(30));
        float newVolume = EditorGUILayout.Slider(tag.baseVolume, 0f, 1f, GUILayout.Width(100));
        if (Mathf.Abs(newVolume - tag.baseVolume) > 0.001f)
        {
            Undo.RecordObject(tag, "Change Base Volume");
            tag.baseVolume = newVolume;
            EditorUtility.SetDirty(tag);
        }
        
        // Remove tag button
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            Undo.DestroyObjectImmediate(tag);
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawUntaggedSourceRow(AudioSource source)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        // Grey indicator for untagged
        Rect colorRect = GUILayoutUtility.GetRect(8, 20, GUILayout.Width(8));
        EditorGUI.DrawRect(colorRect, Color.gray);
        
        // GameObject name (clickable)
        if (GUILayout.Button(source.gameObject.name, EditorStyles.linkLabel, GUILayout.Width(200)))
        {
            Selection.activeGameObject = source.gameObject;
            EditorGUIUtility.PingObject(source.gameObject);
        }
        
        // Clip name if assigned
        string clipName = source.clip != null ? source.clip.name : "(No clip)";
        EditorGUILayout.LabelField(clipName, EditorStyles.miniLabel, GUILayout.Width(150));
        
        // Quick tag buttons
        if (GUILayout.Button("+ SFX", GUILayout.Width(60)))
        {
            AddCategoryTag(source.gameObject, AudioCategory.SFX);
        }
        if (GUILayout.Button("+ Music", GUILayout.Width(60)))
        {
            AddCategoryTag(source.gameObject, AudioCategory.Music);
        }
        if (GUILayout.Button("+ UI", GUILayout.Width(45)))
        {
            AddCategoryTag(source.gameObject, AudioCategory.UI);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void AddCategoryTag(GameObject obj, AudioCategory category)
    {
        Undo.AddComponent<AudioCategoryTag>(obj);
        AudioCategoryTag tag = obj.GetComponent<AudioCategoryTag>();
        tag.category = category;
        
        AudioSource source = obj.GetComponent<AudioSource>();
        if (source != null)
        {
            tag.baseVolume = source.volume;
        }
        
        EditorUtility.SetDirty(obj);
    }
    
    private void TagAllUntagged(AudioCategory category)
    {
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        int count = 0;
        
        foreach (var source in allSources)
        {
            if (source.GetComponent<AudioCategoryTag>() == null)
            {
                AddCategoryTag(source.gameObject, category);
                count++;
            }
        }
        
        Debug.Log($"[Audio Manager] Tagged {count} AudioSources as {category}");
    }
    
    private Color GetCategoryColor(AudioCategory category)
    {
        switch (category)
        {
            case AudioCategory.SFX:
                return new Color(0.3f, 0.8f, 0.3f); // Green
            case AudioCategory.Music:
                return new Color(0.3f, 0.7f, 1f);   // Blue
            case AudioCategory.UI:
                return new Color(1f, 0.9f, 0.3f);   // Yellow
            default:
                return Color.gray;
        }
    }
    
    private void DrawSeparator()
    {
        EditorGUILayout.Space(2);
        Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(2);
    }
}
