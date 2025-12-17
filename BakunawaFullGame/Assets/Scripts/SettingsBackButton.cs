using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple helper script for the Settings Panel Back button.
/// Attach this to the BackButton GameObject or let SettingsPanelGenerator add it.
/// </summary>
[RequireComponent(typeof(Button))]
public class SettingsBackButton : MonoBehaviour
{
    [Tooltip("Reference to the Settings Panel to close")]
    public GameObject settingsPanel;
    
    [Tooltip("Reference to the Menu Content Panel to show")]
    public GameObject menuContentPanel;
    
    private Button button;
    
    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnBackClicked);
        }
    }
    
    public void OnBackClicked()
    {
        // Method 1: Try to use GameMenuController if available
        GameMenuController menuController = FindFirstObjectByType<GameMenuController>();
        if (menuController != null)
        {
            menuController.CloseSettings();
            return;
        }
        
        // Method 2: Direct panel manipulation (fallback)
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            // Try to find parent settings panel
            Transform current = transform.parent;
            while (current != null)
            {
                if (current.name.Contains("Settings"))
                {
                    current.gameObject.SetActive(false);
                    break;
                }
                current = current.parent;
            }
        }
        
        if (menuContentPanel != null)
        {
            menuContentPanel.SetActive(true);
        }
        
        Debug.Log("[SettingsBackButton] Back button clicked");
    }
    
    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnBackClicked);
        }
    }
}
