using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class ButtonSetupTools : EditorWindow
{
    [MenuItem("Tools/Add Animations to All Buttons")]
    public static void AddAnimations()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach(var btn in buttons)
        {
            if(btn.GetComponent<UIButtonAnimation>() == null)
            {
                btn.gameObject.AddComponent<UIButtonAnimation>();
                count++;
            }
        }
        Debug.Log($"Added UIButtonAnimation component to {count} buttons in the scene.");
    }
}
