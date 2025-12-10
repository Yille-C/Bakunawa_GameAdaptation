using UnityEngine;
using UnityEditor;

public class QualitySetupTool : EditorWindow
{
    [MenuItem("Bakunawa/Setup Quality Levels")]
    public static void SetupLevels()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (assets.Length > 0)
        {
            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty qualities = so.FindProperty("m_QualitySettings");

            if (qualities != null)
            {
                // We want 3 levels: Low, Medium, High
                // Resize if needed
                qualities.arraySize = 3;

                // Configure Low (Index 0)
                ConfigureLevel(qualities.GetArrayElementAtIndex(0), "Low", 0, 0); // 0 = Fastest

                // Configure Medium (Index 1)
                ConfigureLevel(qualities.GetArrayElementAtIndex(1), "Medium", 1, 1); // 1 = Fast

                // Configure High (Index 2)
                ConfigureLevel(qualities.GetArrayElementAtIndex(2), "High", 2, 2); // 2 = Simple? No, let's just stick to naming.

                // Set default to Medium
                SerializedProperty currentLevel = so.FindProperty("m_CurrentQuality");
                if (currentLevel != null) currentLevel.intValue = 1;

                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                Debug.Log("Quality Settings Updated to: Low, Medium, High");
            }
        }
    }

    private static void ConfigureLevel(SerializedProperty level, string name, int pixelLightCount, int textureQuality)
    {
        // Set Name safely
        SerializedProperty nameProp = level.FindPropertyRelative("name");
        if (nameProp != null) nameProp.stringValue = name;
        
        // Ensure it is not excluded for any platform
        SerializedProperty excluded = level.FindPropertyRelative("excludedTargetPlatforms");
        if (excluded != null)
        {
            excluded.ClearArray();
        }
        
        // Set Pixel Light count
        SerializedProperty pixelLights = level.FindPropertyRelative("pixelLightCount");
        if (pixelLights != null) pixelLights.intValue = pixelLightCount == 0 ? 0 : (pixelLightCount == 1 ? 2 : 4);
        
        // Save changes
        level.serializedObject.ApplyModifiedProperties();
    }
}
