using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Required for New Input System

public class SceneLoaderTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("SceneLoaderTest active. Press 'T' to reload the current scene with the Loading Screen.");
    }

    void Update()
    {
        // Fix: Use New Input System syntax
        bool tPressed = false;
        
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            tPressed = true;
        }

        if (tPressed)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[Test] Loading scene: {currentScene}");
            
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.LoadScene(currentScene);
            }
            else
            {
                Debug.LogError("LoadingScreenManager Instance not found! Check if 'LoadingScreenManager' object exists in scene.");
            }
        }
    }
}
