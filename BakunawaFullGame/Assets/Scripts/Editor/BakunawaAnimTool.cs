using UnityEngine;
using UnityEditor;

public class BakunawaAnimTool : EditorWindow
{
    [MenuItem("Tools/Refine Bakunawa Motion (Stretch & Bob)")]
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
            // Move up/down by 20 units
            SetPrivateField(anim, "moveAmount", new Vector2(0f, 20f)); 
            
            // 2. Scale Setup (Stretch)
            SetPrivateField(anim, "animateScale", true);
            // X goes down (thinner) when Y goes up (taller/higher) -> Classic squash/stretch
            // When Sin is 1 (UP): Y adds 0.05, X subtracts 0.03
            SetPrivateField(anim, "scaleAmount", new Vector2(-0.03f, 0.05f)); 

            // 3. Sync Speed
            float speed = 2.0f; // A bit faster for a "hovering" feel
            SetPrivateField(anim, "moveSpeed", speed);
            SetPrivateField(anim, "scaleSpeed", speed); // Speeds MUST match for sync

            Debug.Log($"Applied synchronized Stretch & Bob animation to {target.name}.");
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
