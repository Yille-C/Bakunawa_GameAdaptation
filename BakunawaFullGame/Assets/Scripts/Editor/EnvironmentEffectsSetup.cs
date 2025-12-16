using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Editor tool to quickly set up rain and ripple environment effects on a canvas.
/// </summary>
public class EnvironmentEffectsSetup : EditorWindow
{
    [MenuItem("Tools/Bakunawa/Setup Environment Effects (Rain)")]
    public static void SetupEnvironmentEffects()
    {
        // Try to find the main game canvas
        Canvas gameCanvas = null;
        
        // Check common canvas names
        string[] canvasNames = { "GameCanvas", "MainCanvas", "Canvas", "GameplayCanvas", "BattleCanvas" };
        foreach (string name in canvasNames)
        {
            GameObject canvasObj = GameObject.Find(name);
            if (canvasObj != null)
            {
                gameCanvas = canvasObj.GetComponent<Canvas>();
                if (gameCanvas != null) break;
            }
        }
        
        // Fallback: find any canvas
        if (gameCanvas == null)
        {
            gameCanvas = Object.FindFirstObjectByType<Canvas>();
        }
        
        if (gameCanvas == null)
        {
            Debug.LogError("EnvironmentEffectsSetup: No Canvas found in scene! Please create a Canvas first.");
            return;
        }
        
        // Check if EnvironmentEffects already exists
        EnvironmentEffects existing = gameCanvas.GetComponentInChildren<EnvironmentEffects>();
        if (existing != null)
        {
            Debug.LogWarning("EnvironmentEffects already exists on this canvas. Select it to modify settings.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        
        // Create EnvironmentEffects GameObject
        GameObject effectsObj = new GameObject("EnvironmentEffects");
        effectsObj.transform.SetParent(gameCanvas.transform, false);
        
        // Position it first in the hierarchy so it renders behind other UI
        effectsObj.transform.SetAsFirstSibling();
        
        // Setup RectTransform to cover entire canvas
        RectTransform rt = effectsObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Add the EnvironmentEffects component
        EnvironmentEffects envEffects = effectsObj.AddComponent<EnvironmentEffects>();
        
        // Configure default rainy settings via SerializedObject
        SerializedObject so = new SerializedObject(envEffects);
        
        so.FindProperty("enableRain").boolValue = true;
        so.FindProperty("rainIntensity").intValue = 80;
        so.FindProperty("rainSpeed").floatValue = 1100f;
        so.FindProperty("windAngle").floatValue = 15f;
        
        so.FindProperty("enableRipples").boolValue = true;
        so.FindProperty("rippleInterval").floatValue = 0.5f;
        so.FindProperty("rippleAlpha").floatValue = 0.35f;
        
        so.FindProperty("enableAmbientDarkening").boolValue = true;
        so.FindProperty("ambientDarkness").floatValue = 0.12f;
        
        so.FindProperty("enableVignette").boolValue = true;
        so.FindProperty("vignetteIntensity").floatValue = 0.25f;
        
        so.FindProperty("enableLightning").boolValue = false;
        so.FindProperty("lightningInterval").floatValue = 12f;
        
        so.ApplyModifiedProperties();
        
        Undo.RegisterCreatedObjectUndo(effectsObj, "Create Environment Effects");
        
        Debug.Log($"Environment Effects created on '{gameCanvas.name}'! Configure in the Inspector.");
        Selection.activeGameObject = effectsObj;
    }
    
    [MenuItem("Tools/Bakunawa/Add Rain to Selected Object")]
    public static void AddRainToSelected()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("Please select a GameObject (Canvas or Panel) to add rain effect to.");
            return;
        }
        
        RectTransform rt = Selection.activeGameObject.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("Selected object must have a RectTransform (UI element).");
            return;
        }
        
        // Check if already has rain
        RainEffect existingRain = Selection.activeGameObject.GetComponentInChildren<RainEffect>();
        if (existingRain != null)
        {
            Debug.LogWarning("RainEffect already exists on this object.");
            Selection.activeGameObject = existingRain.gameObject;
            return;
        }
        
        // Create rain container
        GameObject rainObj = new GameObject("RainEffect");
        rainObj.transform.SetParent(Selection.activeGameObject.transform, false);
        rainObj.transform.SetAsFirstSibling();
        
        RectTransform rainRt = rainObj.AddComponent<RectTransform>();
        rainRt.anchorMin = Vector2.zero;
        rainRt.anchorMax = Vector2.one;
        rainRt.offsetMin = Vector2.zero;
        rainRt.offsetMax = Vector2.zero;
        
        RainEffect rain = rainObj.AddComponent<RainEffect>();
        
        // Add Canvas for proper sorting (Screen Space - Camera support)
        Canvas rainCanvas = rainObj.AddComponent<Canvas>();
        rainCanvas.overrideSorting = true;
        rainCanvas.sortingOrder = 7; // In front of ripples, behind UI
        rainObj.AddComponent<GraphicRaycaster>().enabled = false;
        
        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(rain);
        so.FindProperty("rainDropCount").intValue = 80;
        so.FindProperty("minFallSpeed").floatValue = 700f;
        so.FindProperty("maxFallSpeed").floatValue = 1400f;
        so.FindProperty("windAngle").floatValue = 12f;
        so.ApplyModifiedProperties();
        
        Undo.RegisterCreatedObjectUndo(rainObj, "Add Rain Effect");
        
        Debug.Log("Rain Effect added with sortingOrder 7. Configure settings in the Inspector.");
        Selection.activeGameObject = rainObj;
    }
    
    [MenuItem("Tools/Bakunawa/Add Ripples to Selected Object")]
    public static void AddRipplesToSelected()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("Please select a GameObject (Canvas or Panel) to add ripple effect to.");
            return;
        }
        
        RectTransform rt = Selection.activeGameObject.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("Selected object must have a RectTransform (UI element).");
            return;
        }
        
        // Check if already has ripples
        RippleEffect existingRipple = Selection.activeGameObject.GetComponentInChildren<RippleEffect>();
        if (existingRipple != null)
        {
            Debug.LogWarning("RippleEffect already exists on this object.");
            Selection.activeGameObject = existingRipple.gameObject;
            return;
        }
        
        // Create ripple container
        GameObject rippleObj = new GameObject("RippleEffect");
        rippleObj.transform.SetParent(Selection.activeGameObject.transform, false);
        rippleObj.transform.SetAsFirstSibling();
        
        RectTransform rippleRt = rippleObj.AddComponent<RectTransform>();
        rippleRt.anchorMin = Vector2.zero;
        rippleRt.anchorMax = Vector2.one;
        rippleRt.offsetMin = Vector2.zero;
        rippleRt.offsetMax = Vector2.zero;
        
        RippleEffect ripple = rippleObj.AddComponent<RippleEffect>();
        
        // Add Canvas for proper sorting (Screen Space - Camera support)
        Canvas rippleCanvas = rippleObj.AddComponent<Canvas>();
        rippleCanvas.overrideSorting = true;
        rippleCanvas.sortingOrder = 6; // Behind rain
        rippleObj.AddComponent<GraphicRaycaster>().enabled = false;
        
        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(ripple);
        so.FindProperty("maxRipples").intValue = 6;
        so.FindProperty("rippleInterval").floatValue = 0.6f;
        so.FindProperty("rippleAlpha").floatValue = 0.4f;
        so.ApplyModifiedProperties();
        
        Undo.RegisterCreatedObjectUndo(rippleObj, "Add Ripple Effect");
        
        Debug.Log("Ripple Effect added with sortingOrder 6. Make sure WaterRipple shader exists.");
        Selection.activeGameObject = rippleObj;
    }
}
