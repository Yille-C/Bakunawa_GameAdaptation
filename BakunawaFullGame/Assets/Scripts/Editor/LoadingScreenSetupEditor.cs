using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class LoadingScreenSetupEditor : EditorWindow
{
    [MenuItem("Tools/Setup Loading Screen")]
    public static void SetupLoadingScreen()
    {
        // 1. Create or Find Canvas
        GameObject canvasObj = GameObject.Find("LoadingScreenCanvas");
        Canvas canvas;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("LoadingScreenCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }
        
        // Ensure it's high sort order to be on top
        canvas.sortingOrder = 999;

        // 2. Create Panel (Background)
        GameObject panelObj = new GameObject("LoadingPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 1); // Black
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();

        // 3. Create Logo
        GameObject logoObj = new GameObject("Logo");
        logoObj.transform.SetParent(panelObj.transform, false);
        Image logoImage = logoObj.AddComponent<Image>();
        Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/BakunawaLogo.png");
        if (logoSprite != null) 
        {
            logoImage.sprite = logoSprite;
            logoImage.preserveAspect = true;
        }
        logoImage.SetNativeSize();
        // Position Left
        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0f, 0.5f);
        logoRect.anchorMax = new Vector2(0f, 0.5f);
        logoRect.pivot = new Vector2(0f, 0.5f);
        logoRect.anchoredPosition = new Vector2(100, 0); // Padding from left
        logoRect.localScale = Vector3.one * 0.8f; // Scale down a bit

        // 4. Create Bottle Art
        GameObject bottleObj = new GameObject("BottleArt");
        bottleObj.transform.SetParent(panelObj.transform, false);
        Image bottleImage = bottleObj.AddComponent<Image>();
        Sprite bottleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/BakunawaInBottle.png");
        if (bottleSprite != null) 
        {
            bottleImage.sprite = bottleSprite;
            bottleImage.preserveAspect = true;
        }
        bottleImage.SetNativeSize();
        // Position Right
        RectTransform bottleRect = bottleObj.GetComponent<RectTransform>();
        bottleRect.anchorMin = new Vector2(1f, 0.5f);
        bottleRect.anchorMax = new Vector2(1f, 0.5f);
        bottleRect.pivot = new Vector2(1f, 0.5f);
        bottleRect.anchoredPosition = new Vector2(-100, 0); // Padding from right
        bottleRect.localRotation = Quaternion.Euler(0, 0, 15); // Tilt

        // 5. Create Loading Text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(panelObj.transform, false);
        Text textComp = textObj.AddComponent<Text>();
        textComp.text = "LOADING...";
        textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // Fallback
        textComp.color = Color.white;
        textComp.fontSize = 36;
        textComp.alignment = TextAnchor.MiddleLeft;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(0f, 0f);
        textRect.pivot = new Vector2(0f, 0f);
        textRect.anchoredPosition = new Vector2(50, 50);
        textRect.sizeDelta = new Vector2(400, 100);

        // 6. Setup Manager
        GameObject managerObj = GameObject.Find("LoadingScreenManager");
        if (managerObj == null) managerObj = new GameObject("LoadingScreenManager");
        
        LoadingScreenManager manager = managerObj.GetComponent<LoadingScreenManager>();
        if (manager == null) manager = managerObj.AddComponent<LoadingScreenManager>();

        // 7. Link References
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("loadingScreenCanvas").objectReferenceValue = panelObj; // Using Panel as the toggle object
        so.FindProperty("loadingText").objectReferenceValue = textComp;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        // Optional: Assign progress bar if we made one, but we skipped it for simplicity
        so.ApplyModifiedProperties();

        // 8. Make Persistent (optional setup, script handles DontDestroyOnLoad)
        // Ensure the manager is not a child of anything
        managerObj.transform.SetParent(null);
        
        Debug.Log("Loading Screen Setup Complete!");
        Selection.activeGameObject = managerObj;
    }
}
