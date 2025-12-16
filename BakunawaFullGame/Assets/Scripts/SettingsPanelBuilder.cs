using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Programmatically creates a Settings Panel UI with Resolution, Quality, Fullscreen, and Volume controls.
/// Attach this to an empty GameObject that will become the settings panel.
/// This script also handles all the settings logic (no need for separate SettingsMenu).
/// </summary>
public class SettingsPanelBuilder : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private Vector2 panelSize = new Vector2(600, 500);
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    [SerializeField] private Color headerColor = new Color(0.8f, 0.6f, 0.2f, 1f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    
    [Header("Font (Optional)")]
    [SerializeField] private TMP_FontAsset customFont;
    
    [Header("Back Button Reference")]
    [Tooltip("Will be set to the created Back button for external use")]
    public Button backButton;
    
    // Created UI Elements
    private TMP_Dropdown resolutionDropdown;
    private TMP_Dropdown qualityDropdown;
    private Toggle fullscreenToggle;
    private Slider volumeSlider;
    private TextMeshProUGUI volumeValueText;
    
    private Resolution[] availableResolutions;
    
    private void Start()
    {
        BuildSettingsPanel();
        InitializeSettings();
    }
    
    private void BuildSettingsPanel()
    {
        // Setup this object as the panel container
        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();
        
        panelRect.sizeDelta = panelSize;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        
        // Panel Background
        Image panelBg = gameObject.AddComponent<Image>();
        panelBg.color = panelColor;
        panelBg.raycastTarget = true;
        
        // Content Layout
        VerticalLayoutGroup vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 20, 20);
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        
        ContentSizeFitter csf = gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // === HEADER ===
        CreateHeader("SETTINGS");
        
        // === VOLUME ===
        CreateVolumeControl();
        
        // === RESOLUTION ===
        CreateResolutionDropdown();
        
        // === QUALITY ===
        CreateQualityDropdown();
        
        // === FULLSCREEN ===
        CreateFullscreenToggle();
        
        // === BACK BUTTON ===
        CreateBackButton();
    }
    
    private void CreateHeader(string text)
    {
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(transform, false);
        
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
    
    private void CreateVolumeControl()
    {
        GameObject row = CreateSettingRow("Volume");
        
        // Slider Container
        GameObject sliderContainer = new GameObject("SliderContainer");
        sliderContainer.transform.SetParent(row.transform, false);
        
        RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
        sliderContainerRect.sizeDelta = new Vector2(250, 30);
        
        HorizontalLayoutGroup hlg = sliderContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;
        
        // Slider
        GameObject sliderObj = CreateSlider();
        sliderObj.transform.SetParent(sliderContainer.transform, false);
        volumeSlider = sliderObj.GetComponent<Slider>();
        
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(200, 20);
        
        // Value Text
        GameObject valueTextObj = new GameObject("VolumeValue");
        valueTextObj.transform.SetParent(sliderContainer.transform, false);
        
        volumeValueText = valueTextObj.AddComponent<TextMeshProUGUI>();
        volumeValueText.text = "100%";
        volumeValueText.fontSize = 20;
        volumeValueText.color = textColor;
        volumeValueText.alignment = TextAlignmentOptions.Left;
        if (customFont != null) volumeValueText.font = customFont;
        
        LayoutElement valueLE = valueTextObj.AddComponent<LayoutElement>();
        valueLE.preferredWidth = 60;
        valueLE.preferredHeight = 30;
    }
    
    private void CreateResolutionDropdown()
    {
        GameObject row = CreateSettingRow("Resolution");
        
        resolutionDropdown = CreateDropdown(row.transform);
        
        LayoutElement le = resolutionDropdown.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 250;
        le.preferredHeight = 40;
    }
    
    private void CreateQualityDropdown()
    {
        GameObject row = CreateSettingRow("Quality");
        
        qualityDropdown = CreateDropdown(row.transform);
        
        LayoutElement le = qualityDropdown.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 250;
        le.preferredHeight = 40;
    }
    
    private void CreateFullscreenToggle()
    {
        GameObject row = CreateSettingRow("Fullscreen");
        
        fullscreenToggle = CreateToggle(row.transform);
    }
    
    private void CreateBackButton()
    {
        GameObject btnObj = new GameObject("BackButton");
        btnObj.transform.SetParent(transform, false);
        
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = accentColor;
        
        backButton = btnObj.AddComponent<Button>();
        backButton.targetGraphic = btnBg;
        
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
        
        // Add animation
        if (btnObj.GetComponent<UIButtonAnimation>() == null)
            btnObj.AddComponent<UIButtonAnimation>();
    }
    
    private GameObject CreateSettingRow(string label)
    {
        GameObject row = new GameObject(label + "Row");
        row.transform.SetParent(transform, false);
        
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
    
    private TMP_Dropdown CreateDropdown(Transform parent)
    {
        GameObject dropdownObj = new GameObject("Dropdown");
        dropdownObj.transform.SetParent(parent, false);
        
        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 40);
        
        Image bg = dropdownObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = bg;
        
        // Caption Text
        GameObject captionObj = new GameObject("Caption");
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
        
        // Template (required for TMP_Dropdown)
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
        GameObject itemBgObj = new GameObject("ItemBackground");
        itemBgObj.transform.SetParent(itemObj.transform, false);
        
        Image itemBgImage = itemBgObj.AddComponent<Image>();
        itemBgImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        
        RectTransform itemBgRect = itemBgObj.GetComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.sizeDelta = Vector2.zero;
        
        itemToggle.targetGraphic = itemBgImage;
        
        // Item Label
        GameObject itemLabelObj = new GameObject("ItemLabel");
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
        itemLabelRect.offsetMin = new Vector2(10, 0);
        itemLabelRect.offsetMax = new Vector2(0, 0);
        
        dropdown.itemText = itemLabel;
        dropdown.template = templateRect;
        
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        
        return dropdown;
    }
    
    private GameObject CreateSlider()
    {
        GameObject sliderObj = new GameObject("Slider");
        
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
        
        // Fill Area
        GameObject fillAreaObj = new GameObject("FillArea");
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
        fillRect.sizeDelta = Vector2.zero;
        
        slider.fillRect = fillRect;
        
        // Handle Area
        GameObject handleAreaObj = new GameObject("HandleSlideArea");
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
        
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        
        return sliderObj;
    }
    
    private Toggle CreateToggle(Transform parent)
    {
        GameObject toggleObj = new GameObject("Toggle");
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
        checkRect.anchorMin = new Vector2(0.1f, 0.1f);
        checkRect.anchorMax = new Vector2(0.9f, 0.9f);
        checkRect.sizeDelta = Vector2.zero;
        
        toggle.graphic = checkImage;
        
        LayoutElement le = toggleObj.AddComponent<LayoutElement>();
        le.preferredWidth = 40;
        le.preferredHeight = 40;
        
        return toggle;
    }
    
    // ==================== SETTINGS LOGIC ====================
    
    private void InitializeSettings()
    {
        // Resolution
        SetupResolutionDropdown();
        
        // Quality
        SetupQualityDropdown();
        
        // Fullscreen
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
        
        // Volume
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVolume;
            UpdateVolumeText(savedVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }
    
    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
        Resolution[] allRes = Screen.resolutions;
        List<Resolution> uniqueRes = new List<Resolution>();
        List<string> options = new List<string>();
        int currentIndex = 0;
        
        for (int i = 0; i < allRes.Length; i++)
        {
            bool isDupe = false;
            foreach (var r in uniqueRes)
            {
                if (r.width == allRes[i].width && r.height == allRes[i].height)
                {
                    isDupe = true;
                    break;
                }
            }
            if (!isDupe) uniqueRes.Add(allRes[i]);
        }
        
        // Take top 5
        if (uniqueRes.Count > 5)
            uniqueRes = uniqueRes.GetRange(uniqueRes.Count - 5, 5);
        
        for (int i = 0; i < uniqueRes.Count; i++)
        {
            options.Add($"{uniqueRes[i].width} x {uniqueRes[i].height}");
            if (uniqueRes[i].width == Screen.width && uniqueRes[i].height == Screen.height)
                currentIndex = i;
        }
        
        availableResolutions = uniqueRes.ToArray();
        
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }
    
    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;
        
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(SetQuality);
    }
    
    public void SetResolution(int index)
    {
        if (availableResolutions == null || index >= availableResolutions.Length) return;
        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        Debug.Log($"[Settings] Resolution set to {res.width}x{res.height}");
    }
    
    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
        Debug.Log($"[Settings] Quality set to {QualitySettings.names[index]}");
    }
    
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"[Settings] Fullscreen set to {isFullscreen}");
    }
    
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
        UpdateVolumeText(volume);
    }
    
    private void UpdateVolumeText(float volume)
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(volume * 100) + "%";
    }
}
