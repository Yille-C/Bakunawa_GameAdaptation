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
    [SerializeField] private Button btnBack; // <--- NEW: Drag your Back Button here!

    [Header("Role Buttons")]
    [SerializeField] private Button btnAttacker;
    [SerializeField] private Button btnTank;
    [SerializeField] private Button btnSupport;
    [SerializeField] private Button btnStartGame;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private Slider volumeSlider;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Init Volume
        if (volumeSlider != null)
        {
            float savedVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVol;
            AudioListener.volume = savedVol;
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        // Init UI States
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (roleSelectionGroup != null) roleSelectionGroup.SetActive(false);
        if (btnStartGame != null) btnStartGame.gameObject.SetActive(false);

        // --- ADDED: Auto-link Back Button ---
        if (btnBack != null)
        {
            btnBack.onClick.AddListener(CloseMultiplayer);
        }
    }

    // --- BUTTON EVENTS ---

    public void OnPlayClicked()
    {
        PlayClickSound();

        // NOW OPENS MULTIPLAYER PANEL
        if (multiplayerPanel != null)
        {
            multiplayerPanel.SetActive(true);
            hostJoinButtons.SetActive(true);
            roleSelectionGroup.SetActive(false);
            statusText.text = "Enter Host IP or Click Host";
        }
        else
        {
            Debug.LogError("Multiplayer Panel is not assigned in Inspector!");
        }
    }

    public void OnHostGameClicked()
    {
        PlayClickSound();
        string myIP = SimpleNetworkManager.Instance.StartHost();
        statusText.text = "Hosting on IP: " + myIP + "\nWaiting for players...";

        EnterLobbyUI();
        btnStartGame.gameObject.SetActive(true);
    }

    public void OnJoinGameClicked()
    {
        PlayClickSound();
        string ip = ipInputField.text;
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        statusText.text = "Connecting to " + ip + "...";

        bool success = SimpleNetworkManager.Instance.JoinGame(ip);
        if (success)
        {
            statusText.text = "Connected!";
            EnterLobbyUI();
            btnStartGame.gameObject.SetActive(false);
        }
        else
        {
            statusText.text = "Connection Failed.";
        }
    }

    public void OnRoleClicked(string roleName)
    {
        PlayClickSound();
        SimpleNetworkManager.Instance.myRole = roleName;
        statusText.text = "You picked: " + roleName;
    }

    public void OnStartGameClicked()
    {
        PlayClickSound();
        SimpleNetworkManager.Instance.SendMessageToServer("START_GAME");
        LoadGameScene();
    }

    // --- NETWORK MESSAGES ---
    public void OnMessageReceived(string msg)
    {
        Debug.Log("UI Received: " + msg);
        if (msg == "START_GAME")
        {
            LoadGameScene();
        }
    }

    void EnterLobbyUI()
    {
        hostJoinButtons.SetActive(false);
        roleSelectionGroup.SetActive(true);
    }

    void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // --- OTHER MENUS ---

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

    public void CloseMultiplayer()
    {
        PlayClickSound();
        // Closes the panel
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
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