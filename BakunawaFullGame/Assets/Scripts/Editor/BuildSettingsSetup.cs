using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BuildSettingsSetup : EditorWindow
{
    [MenuItem("Tools/Fix Build Settings")]
    public static void FixBuildSettings()
    {
        // Define the scenes we want in the build (in order preferably)
        string[] requiredScenes = new string[]
        {
            "Assets/Scenes/SplashScene.unity",
            "Assets/Scenes/Main Menu.unity",
            "Assets/Scenes/LoadingScreen.unity",
            "Assets/Scenes/GameScene.unity"
        };

        List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool changed = false;

        foreach (var scenePath in requiredScenes)
        {
            // Check if scene exists asset database
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogWarning($"Scene not found at path: {scenePath}. Attempting to search...");
                // Fallback search
                string[] guids = AssetDatabase.FindAssets("t:Scene " + System.IO.Path.GetFileNameWithoutExtension(scenePath));
                if (guids.Length > 0)
                {
                    string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    if (!IsSceneInBuildSettings(foundPath, editorBuildSettingsScenes))
                    {
                        editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(foundPath, true));
                        Debug.Log($"Added {foundPath} to Build Settings.");
                        changed = true;
                    }
                }
                else
                {
                    Debug.LogError($"Could not find scene: {scenePath}");
                }
            }
            else
            {
                if (!IsSceneInBuildSettings(scenePath, editorBuildSettingsScenes))
                {
                    editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    Debug.Log($"Added {scenePath} to Build Settings.");
                    changed = true;
                }
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
            Debug.Log("Build Settings Updated successfully!");
        }
        else
        {
            Debug.Log("Build Settings were already correct.");
        }
    }

    private static bool IsSceneInBuildSettings(string path, List<EditorBuildSettingsScene> scenes)
    {
        return scenes.Any(s => s.path == path);
    }
}
