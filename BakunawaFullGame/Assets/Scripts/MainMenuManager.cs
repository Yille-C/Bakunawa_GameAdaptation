using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Config")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    public void OnPlayClicked()
    {
        Debug.Log("Play Clicked");
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnHowToPlayClicked()
    {
        Debug.Log("How to play clicked");
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void OnSettingsClicked()
    {
        Debug.Log("Settings clicked");
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }
}
