using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class SettingsDropdownFixer : EditorWindow
{
    [MenuItem("Tools/Fix Graphics Dropdown")]
    public static void FixDropdowns()
    {
        SettingsMenu settingsMenu = FindFirstObjectByType<SettingsMenu>();
        if (settingsMenu == null)
        {
            Debug.LogError("Could not find SettingsMenu in the scene!");
            return;
        }

        // We need to access private fields via SerializedObject to be safe, 
        // or we can use reflection, but since we are in Editor, SerializedObject is great.
        SerializedObject so = new SerializedObject(settingsMenu);
        SerializedProperty qualityProp = so.FindProperty("qualityDropdown");
        
        if (qualityProp != null && qualityProp.objectReferenceValue != null)
        {
            TMP_Dropdown dropdown = (TMP_Dropdown)qualityProp.objectReferenceValue;
            FixSingleDropdown(dropdown, "Quality Dropdown");
            
            // Also fix Resolution dropdown while we are at it
            SerializedProperty resProp = so.FindProperty("resolutionDropdown");
            if (resProp != null && resProp.objectReferenceValue != null)
            {
                FixSingleDropdown((TMP_Dropdown)resProp.objectReferenceValue, "Resolution Dropdown");
            }
        }
        else
        {
            Debug.LogError("Quality Dropdown is not assigned in the SettingsMenu Inspector.");
        }
    }

    private static void FixSingleDropdown(TMP_Dropdown dropdown, string name)
    {
        Undo.RecordObject(dropdown.gameObject, $"Fix {name}");

        // 1. Reconnect Template if missing
        if (dropdown.template == null)
        {
            Transform templateTrans = dropdown.transform.Find("Template");
            if (templateTrans != null)
            {
                dropdown.template = templateTrans.GetComponent<RectTransform>();
                Debug.Log($"[Fixed] Reconnected Template for {name}");
            }
            else
            {
                Debug.LogWarning($"Could not find child named 'Template' for {name}");
            }
        }

        // 2. Fix Template RectTransform (Pivot and Position)
        if (dropdown.template != null)
        {
            RectTransform templateRect = dropdown.template;
            Undo.RecordObject(templateRect, "Fix Template Rect");

            // Set Pivot to Top (0.5, 1) to spawn below correctly
            templateRect.pivot = new Vector2(0.5f, 1f);
            
            // Reset position to be just below the button (e.g., y = 0 or -5)
            Vector2 anchoredPos = templateRect.anchoredPosition;
            anchoredPos.y = -5f; // Slight offset
            templateRect.anchoredPosition = anchoredPos;

            // 3. Set Image to Sliced
            Image templateImage = templateRect.GetComponent<Image>();
            if (templateImage != null)
            {
                templateImage.type = Image.Type.Sliced;
                // Ensure center is filled
                templateImage.fillCenter = true;
                Debug.Log($"[Fixed] Set Template Image to Sliced for {name}");
            }
        }

        // 4. Fix Item Background Slicing
        // Path: Template -> Viewport -> Content -> Item
        if (dropdown.template != null)
        {
            Transform viewport = dropdown.template.Find("Viewport");
            if (viewport != null)
            {
                Transform content = viewport.Find("Content");
                if (content != null)
                {
                    Transform item = content.Find("Item");
                    if (item != null)
                    {
                        // Check for Image component on Item directly or a child 'Item Background'
                        Image itemImage = item.GetComponent<Image>();
                        if (itemImage == null)
                        {
                            Transform itemBg = item.Find("Item Background");
                            if (itemBg != null) itemImage = itemBg.GetComponent<Image>();
                        }

                        if (itemImage != null)
                        {
                            Undo.RecordObject(itemImage, "Fix Item Image");
                            itemImage.type = Image.Type.Sliced;
                            Debug.Log($"[Fixed] Set Item Image to Sliced for {name}");
                        }
                        
                        // Fix text padding if possible
                        // Assuming standard setup: Item -> Text
                        // We might want to adjust Horizontal Layout Group padding if it exists, or RectTransform of text
                    }
                }
            }
        }
        
        // 5. Ensure Dropdown Updates
        EditorUtility.SetDirty(dropdown);
        Debug.Log($"<b>Successfully processed {name}!</b>");
    }
}
