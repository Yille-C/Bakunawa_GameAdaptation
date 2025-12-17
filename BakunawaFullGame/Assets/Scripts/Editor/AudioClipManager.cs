using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Editor window to view and manage audio clip categorization in the project.
/// Access via Tools > Bakunawa > Audio Clip Manager
/// </summary>
public class AudioClipManager : EditorWindow
{
    private Vector2 scrollPosition;
    private AudioConfiguration config;
    private string searchFilter = "";
    private bool showCategorized = true;
    private bool showUncategorized = true;
    private AudioCategory filterCategory = AudioCategory.SFX;
    private bool useFilter = false;
    
    // Cached audio clips
    private List<AudioClipInfo> allClips = new List<AudioClipInfo>();
    private bool needsRefresh = true;
    
    private class AudioClipInfo
    {
        public AudioClip clip;
        public string path;
        public string name;
        public AudioCategory category;
        public bool isCategorized;
    }
    
    [MenuItem("Tools/Bakunawa/Audio Clip Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<AudioClipManager>("Audio Clips");
        window.minSize = new Vector2(600, 500);
    }
    
    private void OnEnable()
    {
        needsRefresh = true;
        LoadOrCreateConfig();
    }
    
    private void OnFocus()
    {
        needsRefresh = true;
    }
    
    private void LoadOrCreateConfig()
    {
        // Try to find existing config
        string[] guids = AssetDatabase.FindAssets("t:AudioConfiguration");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            config = AssetDatabase.LoadAssetAtPath<AudioConfiguration>(path);
        }
        
        if (config == null)
        {
            // Check Resources folder
            config = Resources.Load<AudioConfiguration>("AudioConfiguration");
        }
    }
    
    private void CreateNewConfig()
    {
        // Ensure Resources folder exists
        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        config = ScriptableObject.CreateInstance<AudioConfiguration>();
        AssetDatabase.CreateAsset(config, "Assets/Resources/AudioConfiguration.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;
        
        Debug.Log("[Audio Clip Manager] Created AudioConfiguration at Assets/Resources/AudioConfiguration.asset");
    }
    
    private void RefreshClipList()
    {
        allClips.Clear();
        
        // Find all AudioClips in the project
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            
            if (clip != null)
            {
                AudioClipInfo info = new AudioClipInfo
                {
                    clip = clip,
                    path = path,
                    name = clip.name,
                    isCategorized = config != null && config.HasClip(clip),
                    category = config != null ? config.GetCategory(clip) : AudioCategory.SFX
                };
                allClips.Add(info);
            }
        }
        
        // Sort by name
        allClips = allClips.OrderBy(c => c.name).ToList();
        needsRefresh = false;
    }
    
    private void OnGUI()
    {
        if (needsRefresh)
        {
            RefreshClipList();
        }
        
        EditorGUILayout.Space(10);
        
        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Audio Clip Manager", EditorStyles.boldLabel);
        
        // Config field
        EditorGUI.BeginChangeCheck();
        config = (AudioConfiguration)EditorGUILayout.ObjectField(config, typeof(AudioConfiguration), false, GUILayout.Width(200));
        if (EditorGUI.EndChangeCheck())
        {
            needsRefresh = true;
        }
        
        if (config == null)
        {
            if (GUILayout.Button("Create Config", GUILayout.Width(100)))
            {
                CreateNewConfig();
                needsRefresh = true;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (config == null)
        {
            EditorGUILayout.HelpBox("No AudioConfiguration found. Click 'Create Config' to create one.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.HelpBox(
            "Categorize your audio clips as SFX or Music.\n" +
            "• SFX (Green): Sound effects, impacts, UI sounds\n" +
            "• Music (Blue): Background music, ambient sounds\n" +
            "• UI (Yellow): Button clicks, hover sounds",
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        // Filters
        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField("Search", searchFilter, GUILayout.Width(300));
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            needsRefresh = true;
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        showCategorized = EditorGUILayout.ToggleLeft("Show Categorized", showCategorized, GUILayout.Width(130));
        showUncategorized = EditorGUILayout.ToggleLeft("Show Uncategorized", showUncategorized, GUILayout.Width(140));
        useFilter = EditorGUILayout.ToggleLeft("Filter", useFilter, GUILayout.Width(60));
        if (useFilter)
        {
            filterCategory = (AudioCategory)EditorGUILayout.EnumPopup(filterCategory, GUILayout.Width(80));
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Quick Actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set All Uncategorized → SFX", GUILayout.Height(25)))
        {
            SetAllUncategorized(AudioCategory.SFX);
        }
        if (GUILayout.Button("Set All Uncategorized → Music", GUILayout.Height(25)))
        {
            SetAllUncategorized(AudioCategory.Music);
        }
        if (GUILayout.Button("Cleanup Nulls", GUILayout.Height(25)))
        {
            config.CleanupNullReferences();
            EditorUtility.SetDirty(config);
            needsRefresh = true;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        DrawSeparator();
        
        // Scrollable list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Filter clips
        var filteredClips = allClips.Where(c => MatchesFilters(c)).ToList();
        
        // Group by folder
        var groupedByFolder = filteredClips
            .GroupBy(c => Path.GetDirectoryName(c.path))
            .OrderBy(g => g.Key);
        
        foreach (var group in groupedByFolder)
        {
            // Folder header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📁 {group.Key}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"({group.Count()} clips)", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            foreach (var clipInfo in group)
            {
                DrawClipRow(clipInfo);
            }
            
            EditorGUILayout.Space(5);
        }
        
        if (filteredClips.Count == 0)
        {
            EditorGUILayout.LabelField("No audio clips match your filters.", EditorStyles.centeredGreyMiniLabel);
        }
        
        EditorGUILayout.EndScrollView();
        
        // Summary
        DrawSeparator();
        int categorizedCount = allClips.Count(c => c.isCategorized);
        int sfxCount = allClips.Count(c => c.isCategorized && c.category == AudioCategory.SFX);
        int musicCount = allClips.Count(c => c.isCategorized && c.category == AudioCategory.Music);
        int uiCount = allClips.Count(c => c.isCategorized && c.category == AudioCategory.UI);
        
        EditorGUILayout.LabelField(
            $"Total: {allClips.Count} clips | Categorized: {categorizedCount} | " +
            $"SFX: {sfxCount} | Music: {musicCount} | UI: {uiCount}");
    }
    
    private bool MatchesFilters(AudioClipInfo info)
    {
        // Search filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            if (!info.name.ToLower().Contains(searchFilter.ToLower()) &&
                !info.path.ToLower().Contains(searchFilter.ToLower()))
            {
                return false;
            }
        }
        
        // Categorization filter
        if (info.isCategorized && !showCategorized) return false;
        if (!info.isCategorized && !showUncategorized) return false;
        
        // Category filter
        if (useFilter && info.isCategorized && info.category != filterCategory) return false;
        
        return true;
    }
    
    private void DrawClipRow(AudioClipInfo info)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        // Category color indicator
        Color categoryColor = info.isCategorized ? GetCategoryColor(info.category) : Color.gray;
        Rect colorRect = GUILayoutUtility.GetRect(8, 20, GUILayout.Width(8));
        EditorGUI.DrawRect(colorRect, categoryColor);
        
        // Clip icon and name (clickable)
        if (GUILayout.Button(info.name, EditorStyles.linkLabel, GUILayout.Width(200)))
        {
            Selection.activeObject = info.clip;
            EditorGUIUtility.PingObject(info.clip);
        }
        
        // Duration
        float duration = info.clip.length;
        EditorGUILayout.LabelField($"{duration:F1}s", EditorStyles.miniLabel, GUILayout.Width(40));
        
        // Current category label
        if (info.isCategorized)
        {
            EditorGUILayout.LabelField(info.category.ToString(), GUILayout.Width(50));
        }
        else
        {
            EditorGUILayout.LabelField("--", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(50));
        }
        
        // Category buttons
        bool isSFX = info.isCategorized && info.category == AudioCategory.SFX;
        bool isMusic = info.isCategorized && info.category == AudioCategory.Music;
        bool isUI = info.isCategorized && info.category == AudioCategory.UI;
        
        GUI.backgroundColor = isSFX ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
        if (GUILayout.Button("SFX", GUILayout.Width(45)))
        {
            SetCategory(info, AudioCategory.SFX);
        }
        
        GUI.backgroundColor = isMusic ? new Color(0.4f, 0.8f, 1f) : Color.white;
        if (GUILayout.Button("Music", GUILayout.Width(50)))
        {
            SetCategory(info, AudioCategory.Music);
        }
        
        GUI.backgroundColor = isUI ? new Color(1f, 0.95f, 0.4f) : Color.white;
        if (GUILayout.Button("UI", GUILayout.Width(35)))
        {
            SetCategory(info, AudioCategory.UI);
        }
        GUI.backgroundColor = Color.white;
        
        // Preview button
        if (GUILayout.Button("▶", GUILayout.Width(25)))
        {
            PlayClipPreview(info.clip);
        }
        
        // Remove button (only if categorized)
        if (info.isCategorized)
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                config.RemoveClip(info.clip);
                EditorUtility.SetDirty(config);
                info.isCategorized = false;
            }
            GUI.backgroundColor = Color.white;
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void SetCategory(AudioClipInfo info, AudioCategory category)
    {
        Undo.RecordObject(config, "Set Audio Category");
        config.SetCategory(info.clip, category, info.path);
        EditorUtility.SetDirty(config);
        
        info.category = category;
        info.isCategorized = true;
    }
    
    private void SetAllUncategorized(AudioCategory category)
    {
        Undo.RecordObject(config, "Set All Uncategorized");
        
        int count = 0;
        foreach (var info in allClips)
        {
            if (!info.isCategorized)
            {
                config.SetCategory(info.clip, category, info.path);
                info.category = category;
                info.isCategorized = true;
                count++;
            }
        }
        
        EditorUtility.SetDirty(config);
        Debug.Log($"[Audio Clip Manager] Set {count} clips to {category}");
    }
    
    private void PlayClipPreview(AudioClip clip)
    {
        // Use reflection to access the internal AudioUtil preview functionality
        var unityEditorAssembly = typeof(AudioImporter).Assembly;
        var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        
        var method = audioUtilClass.GetMethod(
            "PlayPreviewClip",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null,
            new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );
        
        if (method != null)
        {
            method.Invoke(null, new object[] { clip, 0, false });
        }
        else
        {
            // Fallback for newer Unity versions
            var playMethod = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );
            if (playMethod != null)
            {
                playMethod.Invoke(null, new object[] { clip });
            }
        }
    }
    
    private Color GetCategoryColor(AudioCategory category)
    {
        switch (category)
        {
            case AudioCategory.SFX:
                return new Color(0.3f, 0.8f, 0.3f);
            case AudioCategory.Music:
                return new Color(0.3f, 0.7f, 1f);
            case AudioCategory.UI:
                return new Color(1f, 0.9f, 0.3f);
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
