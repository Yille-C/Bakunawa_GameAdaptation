using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class AnimationRefiner : EditorWindow
{
    [MenuItem("Tools/Refine Background Animations")]
    public static void Refine()
    {
        // 1. Remove Animation from Logo
        GameObject logo = GameObject.Find("Logo");
        if (logo != null)
        {
            var anim = logo.GetComponent<UIFloatingAnimation>();
            if (anim != null)
            {
                DestroyImmediate(anim);
                Debug.Log("Removed animation from Logo.");
            }
        }

        // 2. Add Cloud Spawner if not present
        GameObject bgObj = GameObject.Find("Background");
        GameObject canvas = GameObject.Find("MainMenuCanvas");
        
        // We often want clouds to be children of the canvas, just above the background
        if (canvas != null)
        {
             // Check if we already have a Cloud Container
            GameObject cloudContainer = GameObject.Find("CloudContainer");
            if (cloudContainer == null)
            {
                cloudContainer = new GameObject("CloudContainer");
                cloudContainer.transform.SetParent(canvas.transform, false);
                cloudContainer.AddComponent<RectTransform>().anchorMin = Vector2.zero;
                cloudContainer.GetComponent<RectTransform>().anchorMax = Vector2.one;
                cloudContainer.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                cloudContainer.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                
                // Ensure it is behind other things but in front of BG
                // Assuming BG is index 0.
                if (bgObj != null)
                {
                    cloudContainer.transform.SetSiblingIndex(bgObj.transform.GetSiblingIndex() + 1);
                }
                else
                {
                    cloudContainer.transform.SetSiblingIndex(0);
                }
                
                CloudSpawner spawner = cloudContainer.AddComponent<CloudSpawner>();
                
                // Try to find a sprite for the clouds
                Sprite cloudSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/EnergyBall.png"); // Fallback check
                // If we don't really have a cloud sprite, we might use a knob
                if (cloudSprite == null) cloudSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                
                SetPrivateField(spawner, "cloudSprite", cloudSprite);
                Debug.Log("Created CloudContainer with CloudSpawner.");
            }
        }
        
        Debug.Log("Refinement Complete: Logo static, Clouds added.");
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null) field.SetValue(obj, value);
    }
}
