using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Editor tool to generate a Settings Panel UI in the scene.
/// The generated UI is permanent and can be modified in the Editor.
/// </summary>
public class SettingsPanelGenerator : EditorWindow
{
    private Transform parentTransform;
    private TMP_FontAsset customFont;
    private Vector2 panelSize = new Vector2(600, 500);
    private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    private Color headerColor = new Color(0.8f, 0.6f, 0.2f, 1f);
    private Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    private Color accentColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    
    [MenuItem("Tools/Bakunawa/Generate Settings Panel")]
    public static void ShowWindow()
    {
        GetWindow<SettingsPanelGenerator>("Settings Panel Generator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Settings Panel Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        parentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform", parentTransform, typeof(Transform), true);
        customFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Custom Font", customFont, typeof(TMP_FontAsset), false);
        
        EditorGUILayout.Space(10);
        GUILayout.Label("Panel Settings", EditorStyles.boldLabel);
        panelSize = EditorGUILayout.Vector2Field("Panel Size", panelSize);
        panelColor = EditorGUILayout.ColorField("Panel Color", panelColor);
        headerColor = EditorGUILayout.ColorField("Header Color", headerColor);
        textColor = EditorGUILayout.ColorField("Text Color", textColor);
        accentColor = EditorGUILayout.ColorField("Accent Color", accentColor);
        
        EditorGUILayout.Space(20);
        
        if (GUILayout.Button("Generate Settings Panel", GUILayout.Height(40)))
        {
            if (parentTransform == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Parent Transform (e.g., your Canvas or a panel).", "OK");
                return;
            }
            
            GeneratePanel();
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("1. Select a Canvas or Panel as the Parent Transform.\n2. Customize colors and font.\n3. Click 'Generate Settings Panel'.\n4. The UI will be created and you can modify it freely.", MessageType.Info);
    }
    
    private void GeneratePanel()
    {
        // Create Panel Container
        GameObject panelObj = new GameObject("SettingsPanel");
        panelObj.transform.SetParent(parentTransform, false);
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Settings Panel");
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = panelColor;
        panelBg.raycastTarget = true;
        
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 20, 20);
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        
        // Header
        CreateHeader(panelObj.transform, "SETTINGS");
        
        // Volume Row
        CreateVolumeRow(panelObj.transform);
        
        // Resolution Row
        CreateDropdownRow(panelObj.transform, "Resolution", "ResolutionDropdown");
        
        // Quality Row
        CreateDropdownRow(panelObj.transform, "Quality", "QualityDropdown");
        
        // Fullscreen Row
        CreateToggleRow(panelObj.transform, "Fullscreen", "FullscreenToggle");
        
        // Back Button
        CreateBackButton(panelObj.transform);
        
        // Now attach the SettingsMenu script and wire references
        AttachSettingsMenuScript(panelObj);
        
        Selection.activeGameObject = panelObj;
        EditorUtility.DisplayDialog("Success", "Settings Panel created! You can now modify it in the Hierarchy.", "OK");
    }
    
    private void CreateHeader(Transform parent, string text)
    {
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(parent, false);
        
        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = text;
        headerText.fontSize = 42;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = headerColor;
        headerText.alignment = TextAlignmentOptions.Center;
        if (customFont != null) headerText.font = customFont;
        
        LayoutElement le = headerObj.AddComponent<LayoutElement>();
        le.preferredHeight = 60;
    }
    
    private void CreateVolumeRow(Transform parent)
    {
        GameObject row = CreateSettingRow(parent, "Volume");
        
        // Slider Container
        GameObject sliderContainer = new GameObject("SliderContainer");
        sliderContainer.transform.SetParent(row.transform, false);
        
        HorizontalLayoutGroup hlg = sliderContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;
        
        LayoutElement containerLE = sliderContainer.AddComponent<LayoutElement>();
        containerLE.preferredWidth = 280;
        containerLE.preferredHeight = 30;
        
        // Create Slider
        GameObject sliderObj = CreateSlider(sliderContainer.transform, "VolumeSlider");
        
        // Value Text
        GameObject valueTextObj = new GameObject("VolumeValueText");
        valueTextObj.transform.SetParent(sliderContainer.transform, false);
        
        TextMeshProUGUI valueText = valueTextObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "100%";
        valueText.fontSize = 20;
        valueText.color = textColor;
        valueText.alignment = TextAlignmentOptions.Left;
        if (customFont != null) valueText.font = customFont;
        
        LayoutElement valueLE = valueTextObj.AddComponent<LayoutElement>();
        valueLE.preferredWidth = 60;
        valueLE.preferredHeight = 30;
    }
    
    private void CreateDropdownRow(Transform parent, string label, string dropdownName)
    {
        GameObject row = CreateSettingRow(parent, label);
        CreateDropdown(row.transform, dropdownName);
    }
    
    private void CreateToggleRow(Transform parent, string label, string toggleName)
    {
        GameObject row = CreateSettingRow(parent, label);
        CreateToggle(row.transform, toggleName);
    }
    
    private GameObject CreateSettingRow(Transform parent, string label)
    {
        GameObject row = new GameObject(label + "Row");
        row.transform.SetParent(parent, false);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;
        
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 45;
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 24;
        labelText.color = textColor;
        labelText.alignment = TextAlignmentOptions.Right;
        if (customFont != null) labelText.font = customFont;
        
        LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 150;
        labelLE.preferredHeight = 40;
        
        return row;
    }
    
    private void CreateDropdown(Transform parent, string name)
    {
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent, false);
        
        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 40);
        
        Image bg = dropdownObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = bg;
        
        // Caption Text
        GameObject captionObj = new GameObject("Label");
        captionObj.transform.SetParent(dropdownObj.transform, false);
        
        TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
        captionText.text = "Select...";
        captionText.fontSize = 18;
        captionText.color = textColor;
        captionText.alignment = TextAlignmentOptions.Left;
        if (customFont != null) captionText.font = customFont;
        
        RectTransform captionRect = captionObj.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(10, 0);
        captionRect.offsetMax = new Vector2(-30, 0);
        
        dropdown.captionText = captionText;
        
        // Arrow
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownObj.transform, false);
        
        TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.fontSize = 18;
        arrowText.color = textColor;
        arrowText.alignment = TextAlignmentOptions.Center;
        if (customFont != null) arrowText.font = customFont;
        
        RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(30, 0);
        arrowRect.anchoredPosition = Vector2.zero;
        
        // Template
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        templateObj.SetActive(false);
        
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.sizeDelta = new Vector2(0, 150);
        
        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        
        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
        
        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;
        viewportObj.AddComponent<Image>();
        
        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        
        VerticalLayoutGroup contentVLG = contentObj.AddComponent<VerticalLayoutGroup>();
        contentVLG.childControlHeight = false;
        contentVLG.childForceExpandHeight = false;
        
        ContentSizeFitter contentCSF = contentObj.AddComponent<ContentSizeFitter>();
        contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Item
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 30);
        
        Toggle itemToggle = itemObj.AddComponent<Toggle>();
        
        // Item Background
        GameObject itemBgObj = new GameObject("Item Background");
        itemBgObj.transform.SetParent(itemObj.transform, false);
        
        Image itemBgImage = itemBgObj.AddComponent<Image>();
        itemBgImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        
        RectTransform itemBgRect = itemBgObj.GetComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.sizeDelta = Vector2.zero;
        
        itemToggle.targetGraphic = itemBgImage;
        
        // Item Checkmark (hidden by default for dropdown)
        GameObject checkmarkObj = new GameObject("Item Checkmark");
        checkmarkObj.transform.SetParent(itemBgObj.transform, false);
        
        Image checkmarkImage = checkmarkObj.AddComponent<Image>();
        checkmarkImage.color = accentColor;
        
        RectTransform checkmarkRect = checkmarkObj.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0, 0.1f);
        checkmarkRect.anchorMax = new Vector2(0, 0.9f);
        checkmarkRect.pivot = new Vector2(0, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(20, 0);
        checkmarkRect.anchoredPosition = new Vector2(5, 0);
        
        itemToggle.graphic = checkmarkImage;
        
        // Item Label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        
        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.text = "Option";
        itemLabel.fontSize = 16;
        itemLabel.color = textColor;
        itemLabel.alignment = TextAlignmentOptions.Left;
        if (customFont != null) itemLabel.font = customFont;
        
        RectTransform itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(30, 0);
        itemLabelRect.offsetMax = new Vector2(0, 0);
        
        dropdown.itemText = itemLabel;
        dropdown.template = templateRect;
        
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        
        LayoutElement le = dropdownObj.AddComponent<LayoutElement>();
        le.preferredWidth = 250;
        le.preferredHeight = 40;
    }
    
    private GameObject CreateSlider(Transform parent, string name)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(200, 20);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        
        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);
        
        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = accentColor;
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.sizeDelta = Vector2.zero;
        
        slider.fillRect = fillRect;
        
        // Handle Slide Area
        GameObject handleAreaObj = new GameObject("Handle Slide Area");
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
        
        RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);
        
        // Handle
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        
        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = Color.white;
        
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(0, 1);
        
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        
        LayoutElement le = sliderObj.AddComponent<LayoutElement>();
        le.preferredWidth = 200;
        le.preferredHeight = 20;
        
        return sliderObj;
    }
    
    private void CreateToggle(Transform parent, string name)
    {
        GameObject toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(parent, false);
        
        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(40, 40);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        toggle.targetGraphic = bgImage;
        
        // Checkmark
        GameObject checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(bgObj.transform, false);
        
        Image checkImage = checkObj.AddComponent<Image>();
        checkImage.color = accentColor;
        
        RectTransform checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkRect.sizeDelta = Vector2.zero;
        
        toggle.graphic = checkImage;
        
        LayoutElement le = toggleObj.AddComponent<LayoutElement>();
        le.preferredWidth = 40;
        le.preferredHeight = 40;
    }
    
    private void CreateBackButton(Transform parent)
    {
        GameObject btnObj = new GameObject("BackButton");
        btnObj.transform.SetParent(parent, false);
        
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = accentColor;
        
        Button button = btnObj.AddComponent<Button>();
        button.targetGraphic = btnBg;
        
        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "BACK";
        btnText.fontSize = 28;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        if (customFont != null) btnText.font = customFont;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        le.preferredWidth = 200;
    }
    
    private void AttachSettingsMenuScript(GameObject panelObj)
    {
        // Add SettingsMenu component
        SettingsMenu settingsMenu = panelObj.AddComponent<SettingsMenu>();
        
        // Find and assign references using SerializedObject
        SerializedObject so = new SerializedObject(settingsMenu);
        
        // Find Resolution Dropdown
        Transform resRow = panelObj.transform.Find("ResolutionRow");
        if (resRow != null)
        {
            TMP_Dropdown resDropdown = resRow.GetComponentInChildren<TMP_Dropdown>();
            so.FindProperty("resolutionDropdown").objectReferenceValue = resDropdown;
        }
        
        // Find Quality Dropdown
        Transform qualityRow = panelObj.transform.Find("QualityRow");
        if (qualityRow != null)
        {
            TMP_Dropdown qualityDropdown = qualityRow.GetComponentInChildren<TMP_Dropdown>();
            so.FindProperty("qualityDropdown").objectReferenceValue = qualityDropdown;
        }
        
        // Find Fullscreen Toggle
        Transform fullscreenRow = panelObj.transform.Find("FullscreenRow");
        if (fullscreenRow != null)
        {
            Toggle fullscreenToggle = fullscreenRow.GetComponentInChildren<Toggle>();
            so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
        }
        
        // Find Volume Slider
        Transform volumeRow = panelObj.transform.Find("VolumeRow");
        if (volumeRow != null)
        {
            Slider volumeSlider = volumeRow.GetComponentInChildren<Slider>();
            so.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
            
            // Find Volume Text
            Transform sliderContainer = volumeRow.Find("SliderContainer");
            if (sliderContainer != null)
            {
                Transform valueTextTransform = sliderContainer.Find("VolumeValueText");
                if (valueTextTransform != null)
                {
                    TextMeshProUGUI volumeText = valueTextTransform.GetComponent<TextMeshProUGUI>();
                    so.FindProperty("volumeValueText").objectReferenceValue = volumeText;
                }
            }
        }
        
        so.ApplyModifiedProperties();
    }
}
