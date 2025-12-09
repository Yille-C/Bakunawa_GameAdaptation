using UnityEngine;
using UnityEditor;

public class BackgroundAnimSetup : EditorWindow
{
    [MenuItem("Tools/Add Background Animations")]
    public static void AddAnims()
    {
        // 1. Find Logo
        GameObject logo = GameObject.Find("Logo");
        if (logo != null)
        {
            var anim = AddOrGet(logo);
            // Gentle Bobbing
            SetPrivateField(anim, "animatePosition", true);
            SetPrivateField(anim, "moveAmount", new Vector2(0f, 15f));
            SetPrivateField(anim, "moveSpeed", 1.5f);
            Debug.Log("Added floating animation to Logo.");
        }

        // 2. Find Tower/Dragon
        GameObject tower = GameObject.Find("TowerDragon");
        if (tower != null)
        {
            var anim = AddOrGet(tower);
            // Breathing effect
            SetPrivateField(anim, "animatePosition", true);
            SetPrivateField(anim, "moveAmount", new Vector2(5f, 5f)); // Diagonal drift
            SetPrivateField(anim, "moveSpeed", 0.8f);
            
            SetPrivateField(anim, "animateScale", true);
            SetPrivateField(anim, "scaleAmount", new Vector2(0.02f, 0.02f));
            SetPrivateField(anim, "scaleSpeed", 0.5f);
            Debug.Log("Added breathing animation to Tower/Dragon.");
        }
    }

    private static UIFloatingAnimation AddOrGet(GameObject go)
    {
        var anim = go.GetComponent<UIFloatingAnimation>();
        if (anim == null) anim = go.AddComponent<UIFloatingAnimation>();
        return anim;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null) field.SetValue(obj, value);
    }
}
