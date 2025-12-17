using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Config")]
    [Tooltip("Name of the scene to load for Single Player")]
    [SerializeField] private string singlePlayerSceneName = "GameScene";
    // Note: Multiplayer scene is loaded by LobbyManager via Photon

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject modeSelectPanel; // <--- NEW: Drag your Mode Select Panel here
    [SerializeField] private GameObject connectPanel;    // <--- NEW: Drag your Photon Connect Panel here

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private Slider volumeSlider;

    [SerializeField] private GameObject lobbyPanel; 

    private void Start()
    {
        // Initialize Volume from Prefs
        if (volumeSlider != null)
        {
            float savedVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVol;
            AudioListener.volume = savedVol;
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        else
        {
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        }

        // Ensure all panels start closed
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
        if (connectPanel != null) connectPanel.SetActive(false);
    }

    // --- MAIN BUTTONS ---

    public void OnPlayClicked()
    {
        PlayClickSound();
        // Instead of loading game, OPEN MODE SELECT
        if (modeSelectPanel != null)
        {
            modeSelectPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Mode Select Panel is not assigned in MainMenuManager!");
        }
    }

    // --- MODE SELECT BUTTONS ---

    public void OnSinglePlayerClicked()
    {
        PlayClickSound();
        Debug.Log("Starting Single Player...");

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene(singlePlayerSceneName);
        }
        else
        {
            SceneManager.LoadScene(singlePlayerSceneName);
        }
    }

    public void OnMultiplayerClicked()
    {
        PlayClickSound();
        Debug.Log("Opening Multiplayer Connect...");

        // Hide Mode Select
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);

        // Show Photon Connect Panel
        if (connectPanel != null)
        {
            connectPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Connect Panel (Photon) is not assigned in MainMenuManager!");
        }
    }

    public void CloseModeSelect()
    {
        PlayClickSound();
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
    }

    // --- OTHER MENUS ---

    public void OnHowToPlayClicked()
    {
        PlayClickSound();
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void OnSettingsClicked()
    {
        PlayClickSound();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }
    }

    public void OnQuitClicked()
    {
        PlayClickSound();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CloseSettings()
    {
        PlayClickSound();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void CloseHowToPlay()
    {
        PlayClickSound();
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    // --- SETTINGS LOGIC ---

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void PlayClickSound()
    {
        if (sfxSource != null && clickClip != null)
        {
            sfxSource.PlayOneShot(clickClip);
        }
    }

    public void PlayHoverSound()
    {
        if (sfxSource != null && hoverClip != null)
        {
            sfxSource.PlayOneShot(hoverClip);
        }
    }

    public void OnBackFromConnectClicked()
    {
        PlayClickSound();
        Debug.Log("Back from Connect Panel");

        // Hide the Connect Panel
        if (connectPanel != null)
        {
            connectPanel.SetActive(false);
        }

        // Re-open the Mode Select Panel
        if (modeSelectPanel != null)
        {
            modeSelectPanel.SetActive(true);
        }
    }

    public void OnBackFromLobbyClicked()
    {
        PlayClickSound();
        Debug.Log("Leaving Lobby...");

        // 1. Tell Photon to leave the room
        PhotonNetwork.LeaveRoom();

        // 2. Update UI
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }

        // 3. Go back to the Connect Panel
        if (connectPanel != null)
        {
            connectPanel.SetActive(true);
        }
    }
}