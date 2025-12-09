using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SplashScreenSetupEditor : EditorWindow
{
    [MenuItem("Tools/Bakunawa/Setup Splash Screen")]
    public static void SetupSplashScreen()
    {
        // 0. Fix Skybox Flash: Setup Camera to Solid Black
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
             GameObject camObj = new GameObject("Main Camera");
             camObj.tag = "MainCamera";
             mainCam = camObj.AddComponent<Camera>();
             camObj.AddComponent<AudioListener>();
        }
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = Color.black;

        // 1. Create Canvas
        GameObject canvasObj = GameObject.Find("SplashCanvas");
        Canvas canvas;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("SplashCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // 2. Create Background Panel
        // This solves the 'cant add color' issue: A Panel MUST have an Image component to have color.
        GameObject panelObj = new GameObject("BackgroundPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        // Add Image component so we can set color
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = Color.black; // Default to black
        
        // Add CanvasGroup for fading
        CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();
        
        // Strain to fill screen
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // 3. Create Logo Image
        GameObject logoObj = new GameObject("Logo");
        logoObj.transform.SetParent(panelObj.transform, false);
        Image logoImage = logoObj.AddComponent<Image>();
        
        // Try to find the logo asset
        Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/BakunawaLogo.png");
        if (logoSprite != null)
        {
            logoImage.sprite = logoSprite;
            logoImage.preserveAspect = true;
            logoImage.SetNativeSize();
        }
        else
        {
            Debug.LogWarning("BakunawaLogo.png not found at Assets/UI/BakunawaLogo.png. Please assign manually.");
        }

        // Center the logo
        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.anchoredPosition = Vector2.zero;

        // 4. Create Manager and Link
        GameObject managerObj = GameObject.Find("SplashScreenManager");
        if (managerObj == null) managerObj = new GameObject("SplashScreenManager");
        
        SplashScreenManager manager = managerObj.GetComponent<SplashScreenManager>();
        if (manager == null) manager = managerObj.AddComponent<SplashScreenManager>();

        // 5. Link References via SerializedObject to support Undo/Persistence
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty canvasGroupProp = so.FindProperty("splashCanvasGroup");
        if (canvasGroupProp != null)
        {
            canvasGroupProp.objectReferenceValue = canvasGroup;
        }
        
        // Update other settings defaults if needed
        so.FindProperty("nextSceneName").stringValue = "GameScene"; 
        
        so.ApplyModifiedProperties();

        Debug.Log("Splash Screen Setup Complete! Check the 'SplashCanvas' and 'SplashScreenManager' objects.");
        Selection.activeGameObject = managerObj;
    }
}
