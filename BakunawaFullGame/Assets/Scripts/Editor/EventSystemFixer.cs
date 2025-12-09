using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class EventSystemFixer : EditorWindow
{
    [MenuItem("Tools/Fix EventSystem (Input System)")]
    public static void FixEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            es = eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            Debug.Log("Created new EventSystem with InputSystemUIInputModule.");
        }
        else
        {
            // Check if it has the wrong module
            StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Undo.RecordObject(es.gameObject, "Fix Input Module");
                DestroyImmediate(oldModule);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("Replaced StandaloneInputModule with InputSystemUIInputModule on existing EventSystem.");
            }
            else if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("Added InputSystemUIInputModule to existing EventSystem.");
            }
            else
            {
                Debug.Log("EventSystem is already correctly configured.");
            }
        }
    }
}
