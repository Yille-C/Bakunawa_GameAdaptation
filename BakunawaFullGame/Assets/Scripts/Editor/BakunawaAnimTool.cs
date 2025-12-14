using UnityEngine;
using UnityEditor;

public class BakunawaAnimTool : EditorWindow
{
    [MenuItem("Tools/Refine Bakunawa Motion (Chess Piece Float)")]
    public static void Refine()
    {
        // Try to find the object. It might be named "TowerDragon" (from builder) or "Bakunawa" (if renamed) or have the dragon sprite
        GameObject target = GameObject.Find("TowerDragon");
        if (target == null) target = GameObject.Find("Bakunawa");
        if (target == null) target = GameObject.Find("Dragon");

        if (target != null)
        {
            var anim = target.GetComponent<UIFloatingAnimation>();
            if (anim == null) anim = target.AddComponent<UIFloatingAnimation>();

            // Disable random offset so Position and Scale are synchronized
            SetPrivateField(anim, "randomOffset", false);

            // 1. Position Setup (Up and Down)
            SetPrivateField(anim, "animatePosition", true);
            // Move up/down by 10 units (Subtle)
            SetPrivateField(anim, "moveAmount", new Vector2(0f, 10f)); 
            
            // 2. Scale Setup (Rigid - Like a Chess Piece)
            SetPrivateField(anim, "animateScale", false);
            // Reset scale amount just in case, though bool controls it
            SetPrivateField(anim, "scaleAmount", Vector2.zero); 

            // 3. Speed
            float speed = 1.0f; // Slow and steady
            SetPrivateField(anim, "moveSpeed", speed);
            SetPrivateField(anim, "scaleSpeed", speed); 

            Debug.Log($"Applied synchronized Chess Piece Float animation to {target.name}.");
        }
        else
        {
            Debug.LogError("Could not find 'TowerDragon', 'Bakunawa', or 'Dragon' object.");
        }
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null) field.SetValue(obj, value);
    }
}
