using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("Scene Config")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Main Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Multiplayer UI")]
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private InputField ipInputField;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject hostJoinButtons;
    [SerializeField] private GameObject roleSelectionGroup;
    [SerializeField] private Button btnBack;

    [Header("Role Buttons")]
    [SerializeField] private Button btnAttacker;
    [SerializeField] private Button btnTank;
    [SerializeField] private Button btnSupport;
    [SerializeField] private Button btnStartGame; // Only visible to host

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private Slider volumeSlider;

    private void Awake()
    {
        Instance = this;
    }

    // Inside MainMenuManager.cs



    // --- BUTTON EVENTS ---

    public void OnPlayClicked()
    {
        PlayClickSound();
        // Instantly load the single player game
        SceneManager.LoadScene(gameSceneName);
    }





    public void OnHowToPlayClicked()
    {
        PlayClickSound();
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    public void OnSettingsClicked()
    {
        PlayClickSound();
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        PlayClickSound();
        Application.Quit();
    }

    public void CloseSettings()
    {
        PlayClickSound();
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void CloseHowToPlay()
    {
        PlayClickSound();
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void PlayClickSound()
    {
        if (sfxSource != null && clickClip != null) sfxSource.PlayOneShot(clickClip);
    }
}