using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class MainMenuBuilder : EditorWindow
{
    [MenuItem("Tools/Build Main Menu UI")]
    public static void BuildMenu()
    {
        // 1. Setup Canvas
        GameObject canvasObj = GameObject.Find("MainMenuCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("MainMenuCanvas");
            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 1.5 Ensure EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // 2. Main Menu Manager
        MainMenuManager manager = canvasObj.GetComponent<MainMenuManager>();
        if (manager == null) manager = canvasObj.AddComponent<MainMenuManager>();

        // 3. Background
        GameObject bgObj = CreateImage("Background", canvasObj.transform);
        StretchToFill(bgObj.GetComponent<RectTransform>());
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/LoadingScreen/Loading Screen.jpg"); 
        if (bgSprite == null) bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Loading Screen.jpg");
        if (bgSprite == null) bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/GameBG/main.jpg");
        if (bgSprite != null) bgObj.GetComponent<Image>().sprite = bgSprite;
        bgObj.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.8f); 

        // 4. Logo (Top Center)
        GameObject logoObj = CreateImage("Logo", canvasObj.transform);
        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 1f);
        logoRect.anchorMax = new Vector2(0.5f, 1f);
        logoRect.pivot = new Vector2(0.5f, 1f);
        logoRect.anchoredPosition = new Vector2(0, -50);
        logoRect.sizeDelta = new Vector2(250, 250);
        Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/BakunawaLogo.png");
        if (logoSprite != null) logoObj.GetComponent<Image>().sprite = logoSprite;
        
        // 5. Title (Center, below logo)
        GameObject titleObj = CreateText("TitleText", "RISE OF THE BAKUNAWA", canvasObj.transform);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -320); 
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 80;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white; 
        titleText.fontStyle = FontStyles.Bold;
        // Add basic outline/shadow if possible via shader, or just rely on asset

        // 6. Tower/Dragon (Right Side)
        GameObject towerObj = CreateImage("TowerDragon", canvasObj.transform);
        RectTransform towerRect = towerObj.GetComponent<RectTransform>();
        towerRect.anchorMin = new Vector2(1f, 0f); // Bottom Right corner
        towerRect.anchorMax = new Vector2(1f, 0f); 
        towerRect.pivot = new Vector2(1f, 0f);
        towerRect.anchoredPosition = new Vector2(0, 0);
        towerRect.sizeDelta = new Vector2(800, 1000); 
        Sprite towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/towerland.png");
        if (towerSprite == null) towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/BakunawaInBottle.png");
        if (towerSprite != null) 
        {
            towerObj.GetComponent<Image>().sprite = towerSprite;
            towerObj.GetComponent<Image>().preserveAspect = true;
        }
        
        // Anim Tower
        var towerAnim = towerObj.AddComponent<UIFloatingAnimation>();
        ReflectionExtensions.SetPrivateField(towerAnim, "moveAmount", new Vector2(5f, 5f));
        ReflectionExtensions.SetPrivateField(towerAnim, "scaleAmount", new Vector2(0.02f, 0.02f));
        ReflectionExtensions.SetPrivateField(towerAnim, "animateScale", true);

        // 7. Buttons Container (Bottom Left area)
        GameObject btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 0f);
        btnRect.anchorMax = new Vector2(0f, 0f);
        btnRect.pivot = new Vector2(0f, 0f);
        btnRect.anchoredPosition = new Vector2(100, 100);
        
        VerticalLayoutGroup vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 30;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childAlignment = TextAnchor.LowerLeft;

        // Create Buttons
        CreateButton("PlayButton", "Play", btnContainer.transform, manager, "OnPlayClicked");
        CreateButton("HowToPlayButton", "How to Play?", btnContainer.transform, manager, "OnHowToPlayClicked");
        CreateButton("SettingsButton", "Settings", btnContainer.transform, manager, "OnSettingsClicked");
        CreateButton("QuitButton", "Quit", btnContainer.transform, manager, "OnQuitClicked");

        // 8. Panels
        GameObject panelsOverlay = new GameObject("PanelsOverlay");
        panelsOverlay.transform.SetParent(canvasObj.transform, false);
        StretchToFill(panelsOverlay.AddComponent<RectTransform>());
        // Make sure overlay allows clicks potentially, usually clear generic alpha
        
        GameObject settingsPanel = CreatePanel("SettingsPanel", panelsOverlay.transform, manager, "CloseSettings");
        manager.SetPrivateField("settingsPanel", settingsPanel);

        GameObject howToPlayPanel = CreatePanel("HowToPlayPanel", panelsOverlay.transform, manager, "CloseHowToPlay");
        manager.SetPrivateField("howToPlayPanel", howToPlayPanel);

        Debug.Log("Main Menu UI Built Successfully! Please ensure TextMeshPro is imported and fonts are set.");
    }

    private static GameObject CreateImage(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>();
        return obj;
    }

    private static GameObject CreateText(string name, string content, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        return obj;
    }

    private static GameObject CreateButton(string name, string label, Transform parent, MainMenuManager manager, string methodName)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.4f, 0.7f, 1f); 
        
        Button btn = btnObj.AddComponent<Button>();
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        StretchToFill(textObj.AddComponent<RectTransform>());
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.fontSize = 28;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 70);

        if (manager != null)
        {
            var methodInfo = typeof(MainMenuManager).GetMethod(methodName);
            if (methodInfo != null)
            {
                var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), manager, methodInfo);
                UnityEventTools.AddPersistentListener(btn.onClick, action);
            }
        }

        // Add Animation
        btnObj.AddComponent<UIButtonAnimation>();

        return btnObj;
    }

    private static GameObject CreatePanel(string name, Transform parent, MainMenuManager manager, string closeMethodName)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        StretchToFill(panel.AddComponent<RectTransform>());
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.9f);
        
        GameObject text = CreateText("Title", name, panel.transform);
        text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);
        text.GetComponent<TextMeshProUGUI>().fontSize = 48;
        
        // Close Button
        GameObject closeBtn = CreateButton("CloseButton", "Close", panel.transform, manager, closeMethodName);
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    private static void StretchToFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

public static class ReflectionExtensions
{
    public static void SetPrivateField(this object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null) field.SetValue(obj, value);
    }
}
