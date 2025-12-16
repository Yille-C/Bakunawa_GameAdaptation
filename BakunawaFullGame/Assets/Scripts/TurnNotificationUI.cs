using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TurnNotificationUI : MonoBehaviour
{
    public static TurnNotificationUI Instance;

    [Header("UI References")]
    public GameObject overlayCanvas;
    public Image dimmerImage;
    public Text notificationText;
    public RectTransform textRect;

    [Header("Settings")]
    public Font notificationFont; // Assign in Inspector or auto-load
    public float dimmerAlpha = 0.7f;
    public float slideInDuration = 0.5f;
    public float pauseDuration = 0.8f;
    public float slideOutDuration = 0.4f;

    private void Awake()
    {
        Instance = this;
        EnsureUI();
    }

    private void EnsureUI()
    {
        // 1. Validate Canvas
        if (overlayCanvas == null)
        {
            // Try to find if it exists in scene but reference is lost
            GameObject existing = GameObject.Find("TurnNotificationCanvas");
            if (existing != null)
            {
                overlayCanvas = existing;
            }
            else
            {
                GameObject canvasObj = new GameObject("TurnNotificationCanvas");
                Canvas c = canvasObj.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 999; 
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                overlayCanvas = canvasObj;
            }
        }

        // 2. Validate Dimmer
        if (dimmerImage == null)
        {
            Transform t = overlayCanvas.transform.Find("Dimmer");
            if (t != null) dimmerImage = t.GetComponent<Image>();
            
            if (dimmerImage == null)
            {
                GameObject dimmerObj = new GameObject("Dimmer");
                dimmerObj.transform.SetParent(overlayCanvas.transform, false);
                dimmerImage = dimmerObj.AddComponent<Image>();
                dimmerImage.color = new Color(0, 0, 0, 0);
                dimmerImage.raycastTarget = false;
                RectTransform dr = dimmerImage.rectTransform;
                dr.anchorMin = Vector2.zero;
                dr.anchorMax = Vector2.one;
                dr.sizeDelta = Vector2.zero;
            }
        }

        // 3. Validate Text
        if (notificationText == null)
        {
             Transform t = overlayCanvas.transform.Find("NotificationText");
             if (t != null) notificationText = t.GetComponent<Text>();
             
             if (notificationText == null)
             {
                GameObject textObj = new GameObject("NotificationText");
                textObj.transform.SetParent(overlayCanvas.transform, false);
                notificationText = textObj.AddComponent<Text>();
                
                // Try to load Barbara font if not assigned
                if (notificationFont == null)
                {
                    #if UNITY_EDITOR
                    notificationFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Barbara.ttf");
                    #endif
                }

                if (notificationFont != null)
                    notificationText.font = notificationFont;
                else
                    notificationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                notificationText.fontSize = 80;
                notificationText.alignment = TextAnchor.MiddleCenter;
                notificationText.horizontalOverflow = HorizontalWrapMode.Overflow;
                notificationText.verticalOverflow = VerticalWrapMode.Overflow;
                notificationText.color = Color.white;
                notificationText.raycastTarget = false;
                
                textObj.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
                Outline outline = textObj.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(3, -3);
             }
        }

        // 4. Validate Rect
        if (textRect == null && notificationText != null)
        {
            textRect = notificationText.rectTransform;
            textRect.anchorMin = new Vector2(0, 0.5f);
            textRect.anchorMax = new Vector2(1, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(0, 200);
            textRect.anchoredPosition = new Vector2(-Screen.width, 0); 
        }
        
        // 5. Ensure Correct State
        // Only disable if we are not currently running a notification (safest is to leave it unless we are initializing)
        // But since EnsureUI is called in Awake, disabling is fine.
        // If called later, we might not want to force disable?
        // Actually, PlayTurnNotification enables it immediately after.
    }

    public IEnumerator PlayTurnNotification(string text, Color textColor)
    {
        EnsureUI(); // Safety first!
        
        if (overlayCanvas != null) overlayCanvas.SetActive(true);
        
        if (notificationText != null)
        {
            notificationText.text = text;
            notificationText.color = textColor;
        }

        // Reset positions
        float screenWidth = Screen.width;
        // Start off-screen left
        if (textRect != null) textRect.anchoredPosition = new Vector2(-screenWidth * 1.5f, 0);
        if (dimmerImage != null) dimmerImage.color = new Color(0, 0, 0, 0);

        // 1. Dim screen and Slide In (Fast in)
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideInDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t); // EaseInOut

            // Dimmer
            if (dimmerImage != null) dimmerImage.color = new Color(0, 0, 0, Mathf.Lerp(0, dimmerAlpha, smoothT));

            // Slide Text: Left -> Center
            if (textRect != null)
            {
                float xPos = Mathf.Lerp(-screenWidth * 1.5f, 0, smoothT);
                textRect.anchoredPosition = new Vector2(xPos, 0);
            }

            yield return null;
        }

        // 2. Slow middle movement
        elapsed = 0f;
        Vector2 startCenter = new Vector2(0, 0);
        Vector2 endCenter = new Vector2(50, 0); 
        
        while (elapsed < pauseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pauseDuration;
            
            if (textRect != null) textRect.anchoredPosition = Vector2.Lerp(startCenter, endCenter, t);
            yield return null;
        }

        // 3. Accelerate to Right and Undim
        elapsed = 0f;
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideOutDuration;
            float easeIn = t * t; 

            if (dimmerImage != null) dimmerImage.color = new Color(0, 0, 0, Mathf.Lerp(dimmerAlpha, 0, t));

            if (textRect != null)
            {
                float xPos = Mathf.Lerp(50, screenWidth * 1.5f, easeIn);
                textRect.anchoredPosition = new Vector2(xPos, 0);
            }

            yield return null;
        }

        if (overlayCanvas != null) overlayCanvas.SetActive(false);
    }
}
