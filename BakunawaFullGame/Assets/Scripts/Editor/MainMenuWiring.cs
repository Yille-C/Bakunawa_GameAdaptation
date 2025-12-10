using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.Events;

public class MainMenuWiring : EditorWindow
{
    [MenuItem("Bakunawa/Refine Main Menu Connections")]
    public static void ShowWindow()
    {
        GetWindow<MainMenuWiring>("Main Menu Wiring");
    }

    private void OnGUI()
    {
        GUILayout.Label("Main Menu Connector", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Wire Settings & Buttons"))
        {
            WireMainMenu();
        }
    }

    private void WireMainMenu()
    {
        MainMenuManager manager = Object.FindFirstObjectByType<MainMenuManager>();
        if (manager == null)
        {
            Debug.LogError("No MainMenuManager found in the scene set Active!");
            return;
        }

        Undo.RecordObject(manager, "Wire Main Menu");

        // 1. Find or Create Settings Panel
        GameObject panel = FindSettingsPanel();
        if (panel == null)
        {
            Debug.Log("Creating new Settings Panel...");
            panel = CreateSettingsPanel();
        }
        else
        {
            Debug.Log($"Found existing Settings Panel: {panel.name}");
        }

        // Force reset the RectTransform to ensure it's visible
        EnforcePanelRect(panel);

        // 2. Assign Panel to Manager
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty panelProp = so.FindProperty("settingsPanel");
        panelProp.objectReferenceValue = panel;
        so.ApplyModifiedProperties();
        Debug.Log("Assigned Settings Panel to MainMenuManager.");

        // 3. Find Settings Button and cleanup/link
        Button settingsBtn = FindButtonByName("Settings", "Option");
        if (settingsBtn != null)
        {
            Debug.Log($"Found Settings Button: {settingsBtn.name}. Wiring...");
            UnityEventTools.RemovePersistentListener(settingsBtn.onClick, manager.OnSettingsClicked);
            UnityEventTools.AddPersistentListener(settingsBtn.onClick, manager.OnSettingsClicked);
        }
        else
        {
            Debug.LogWarning("Could not find a button named 'Settings' or 'Option'. Please name your settings button 'SettingsButton'.");
        }

        // 4. Find Close Button inside Panel and link
        Button closeBtn = FindButtonInHierarchy(panel.transform, "Close", "Back", "Exit");
        if (closeBtn != null)
        {
            Debug.Log($"Found Close Button inside panel: {closeBtn.name}. Wiring...");
            UnityEventTools.RemovePersistentListener(closeBtn.onClick, manager.CloseSettings);
            UnityEventTools.AddPersistentListener(closeBtn.onClick, manager.CloseSettings);
        }
        else
        {
            Debug.Log("No Close button found inside Settings Panel. Consider adding one.");
        }

        // 5. Wire Play and Quit if found
        Button playBtn = FindButtonByName("Play", "Start");
        if (playBtn != null)
        {
            UnityEventTools.RemovePersistentListener(playBtn.onClick, manager.OnPlayClicked);
            UnityEventTools.AddPersistentListener(playBtn.onClick, manager.OnPlayClicked);
            Debug.Log("Wired Play Button.");
        }

        Button quitBtn = FindButtonByName("Quit", "Exit");
        if (quitBtn != null)
        {
            UnityEventTools.RemovePersistentListener(quitBtn.onClick, manager.OnQuitClicked);
            UnityEventTools.AddPersistentListener(quitBtn.onClick, manager.OnQuitClicked);
            Debug.Log("Wired Quit Button.");
        }
        
        Debug.Log("Main Menu Wiring Complete!");
    }

    private GameObject FindSettingsPanel()
    {
        // Try finding by name first
        GameObject p = GameObject.Find("SettingsPanel");
        if (p == null) p = GameObject.Find("Settings Menu");
        if (p == null) p = GameObject.Find("OptionsPanel");
        
        // Try finding by component
        if (p == null)
        {
            SettingsMenu sm = Object.FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include); // include inactive
            if (sm != null) return sm.gameObject;
        }

        return p;
    }

    private GameObject CreateSettingsPanel()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        // Add Image
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.18f, 0.95f); // Deep blue theme
        
        // Add SettingsMenu script
        panel.AddComponent<SettingsMenu>();

        // Fill basic structure
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(50, 50);
            rt.offsetMax = new Vector2(-50, -50);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = Vector2.zero;
        }

        // Add a Close Button
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(panel.transform, false);
        Image btnImg = closeBtnObj.AddComponent<Image>();
        btnImg.color = Color.red;
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        
        RectTransform btnRt = closeBtnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1, 1);
        btnRt.anchorMax = new Vector2(1, 1);
        btnRt.anchoredPosition = new Vector2(-20, -20);
        btnRt.sizeDelta = new Vector2(50, 50);
        
        // Add Close Text
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        Text txt = closeTextObj.AddComponent<Text>();
        txt.text = "X";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.white;
        RectTransform txtRt = closeTextObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        panel.SetActive(false); // Default to closed so we can open it
        return panel;
    }

    private Button FindButtonByName(params string[] searchTerms)
    {
        // Find all buttons, including inactive ones? No, usually buttons on main menu are active.
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            foreach (var term in searchTerms)
            {
                if (btn.name.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return btn;
                }
                // Also check text component if it exists
                var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null && text.text.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return btn;
                }
                var legacyText = btn.GetComponentInChildren<Text>();
                if (legacyText != null && legacyText.text.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return btn;
                }
            }
        }
        return null;
    }

    private Button FindButtonInHierarchy(Transform parent, params string[] searchTerms)
    {
        Button[] buttons = parent.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            foreach (var term in searchTerms)
            {
                if (btn.name.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return btn;
            }
        }
        return null;
    }

    private void EnforcePanelRect(GameObject panel)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Fix Settings Panel Rect");
            // Stretch to fill
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // Small margins (50px)
            rt.offsetMin = new Vector2(50, 50); // Left, Bottom
            rt.offsetMax = new Vector2(-50, -50); // Right, Top (-x, -y)
            
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
