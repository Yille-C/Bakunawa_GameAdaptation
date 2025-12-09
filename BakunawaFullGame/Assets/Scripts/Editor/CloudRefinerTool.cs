using UnityEngine;
using UnityEditor;

public class CloudRefinerTool : EditorWindow
{
    [MenuItem("Tools/Set Cloud 1 to Breathe")]
    public static void RefineCloud1()
    {
        // Find specifically "Cloud 1"
        GameObject cloud1 = GameObject.Find("Cloud 1");
        
        if (cloud1 != null)
        {
            // 1. Remove Movement
            var movement = cloud1.GetComponent<CloudMovement>();
            if (movement != null)
            {
                DestroyImmediate(movement);
                Debug.Log("Removed CloudMovement from Cloud 1.");
            }

            // 2. Add Breathing (Floating Anim)
            var anim = cloud1.GetComponent<UIFloatingAnimation>();
            if (anim == null)
            {
                anim = cloud1.AddComponent<UIFloatingAnimation>();
            }

            // Configure for breathing only
            SetPrivateField(anim, "animatePosition", false);
            SetPrivateField(anim, "animateRotation", false);
            
            SetPrivateField(anim, "animateScale", true);
            SetPrivateField(anim, "scaleAmount", new Vector2(0.05f, 0.05f)); // 5% breathing
            SetPrivateField(anim, "scaleSpeed", 0.8f);
            
            Debug.Log("Added Breathing Animation to Cloud 1.");
        }
        else
        {
            Debug.LogError("Could not find object named 'Cloud 1' in the scene.");
        }
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null) field.SetValue(obj, value);
    }
}
