using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the in-game Pause/Menu system.
/// Handles pausing time, toggling the menu UI, and navigation to Settings or Main Menu.
/// </summary>
public class GameMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main container for the pause menu UI (stays active when settings shown)")]
    public GameObject menuPanel;
    
    [Tooltip("The container for menu buttons (Resume, Settings, etc.) - hidden when settings shown")]
    public GameObject menuContentPanel;
    
    [Tooltip("The button in the HUD that opens this menu")]
    public Button openMenuButton;
    
    [Tooltip("The settings panel (can be a child of menuPanel)")]
    public GameObject settingsPanel;

    [Header("Menu Buttons")]
    public Button resumeButton;
    public Button settingsButton;
    public Button mainMenuButton;
    public Button quitButton;
    
    [Header("Settings Panel")]
    [Tooltip("The Back button inside the Settings Panel")]
    public Button settingsBackButton;
    
    [Header("Scene Management")]
    [Tooltip("Name of the Main Menu scene to load on Quit")]
    public string mainMenuSceneName = "Main Menu";

    private bool isPaused = false;

    private void Start()
    {
        // Ensure menu is closed at start
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Setup Buttons
        if (openMenuButton != null)
            openMenuButton.onClick.AddListener(ToggleMenu);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        // Settings Panel Back Button
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettings);
            
        // Optional: Ensure buttons have animations
        EnsureButtonAnimations();
    }

    private void Update()
    {
        // Toggle with Escape key - using new Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                // If settings are open, back to pause menu
                settingsPanel.SetActive(false);
                menuPanel.SetActive(true);
            }
            else
            {
                ToggleMenu();
            }
        }
    }

    /// <summary>
    /// Toggles the pause state and menu visibility.
    /// </summary>
    public void ToggleMenu()
    {
        isPaused = !isPaused;
        
        if (menuPanel != null) 
            menuPanel.SetActive(isPaused);
        
        // Show menu content, hide settings when toggling
        if (menuContentPanel != null)
            menuContentPanel.SetActive(isPaused);
            
        // Close settings if we are just opening/closing the main pause layer
        if (settingsPanel != null) 
            settingsPanel.SetActive(false);

        // Pause/Resume Time
        Time.timeScale = isPaused ? 0f : 1f;
        
        Debug.Log($"[GameMenuController] Game {(isPaused ? "Paused" : "Resumed")}");
    }

    public void OnResumeClicked()
    {
        if (isPaused) ToggleMenu();
    }

    public void OnSettingsClicked()
    {
        // Hide menu content but keep the container (menuPanel) active
        // so that settingsPanel (a child) can be shown
        if (menuContentPanel != null) menuContentPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }
    
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuContentPanel != null) menuContentPanel.SetActive(true);
    }

    public void OnMainMenuClicked()
    {
        // Ensure time is running before switching scenes
        Time.timeScale = 1f;
        Debug.Log("[GameMenuController] Loading Main Menu...");
        LoadingScreenManager.SceneToLoadOnStart = mainMenuSceneName;
        SceneManager.LoadScene("LoadingScreen");
    }

    public void OnQuitClicked()
    {
        Debug.Log("[GameMenuController] Exiting Game...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void EnsureButtonAnimations()
    {
        // Helper to auto-add hover effects if missing
        Button[] eventButtons = { openMenuButton, resumeButton, settingsButton, mainMenuButton, quitButton, settingsBackButton };
        foreach (var btn in eventButtons)
        {
            if (btn != null && btn.GetComponent<UIButtonAnimation>() == null)
            {
                btn.gameObject.AddComponent<UIButtonAnimation>();
            }
        }
    }

    private void OnDestroy()
    {
        // Safety check: Reset time scale if this object is destroyed (e.g. restart)
        Time.timeScale = 1f;
    }
}
