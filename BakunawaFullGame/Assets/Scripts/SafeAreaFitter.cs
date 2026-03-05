using UnityEngine;

/// <summary>
/// Adjusts a RectTransform to stay within the device's safe area,
/// handling notches, rounded corners, and home indicators on all phones.
/// Attach this to a full-screen panel that wraps your UI content.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        // Re-apply when safe area or screen size changes (e.g. orientation change)
        if (Screen.safeArea != lastSafeArea || new Vector2Int(Screen.width, Screen.height) != lastScreenSize)
        {
            ApplySafeArea();
        }
    }

    /// <summary>
    /// Recalculates and applies the safe area insets to this RectTransform.
    /// </summary>
    public void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        Vector2 anchorMin = safeArea.position / screenSize;
        Vector2 anchorMax = (safeArea.position + safeArea.size) / screenSize;

        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}
