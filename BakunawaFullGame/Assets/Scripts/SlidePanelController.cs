using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SlidePanelController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The panel to slide. If null, uses this object's RectTransform.")]
    public RectTransform panelTransform;
    [Tooltip("Button to toggle the panel state.")]
    public Button toggleButton;
    [Tooltip("Optional: Arrow/Icon on the button to rotate when toggled.")]
    public RectTransform indicatorIcon;

    [Header("Positions (Anchored)")]
    public Vector2 closedPosition = new Vector2(-200, 0);
    public Vector2 openPosition = new Vector2(0, 0);

    [Header("Settings")]
    public float slideDuration = 0.4f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool startOpen = false;

    private bool isOpen = false;
    private Coroutine activeCoroutine;

    void Start()
    {
        if (panelTransform == null) panelTransform = GetComponent<RectTransform>();

        isOpen = startOpen;
        panelTransform.anchoredPosition = isOpen ? openPosition : closedPosition;
        
        // Initial icon rotation
        if (indicatorIcon != null)
        {
            indicatorIcon.localRotation = isOpen ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(SlideRoutine(isOpen ? openPosition : closedPosition));
    }

    IEnumerator SlideRoutine(Vector2 targetPos)
    {
        Vector2 startPos = panelTransform.anchoredPosition;
        Quaternion startRot = (indicatorIcon != null) ? indicatorIcon.localRotation : Quaternion.identity;
        Quaternion targetRot = isOpen ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
        
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float curvedT = animationCurve.Evaluate(t);

            panelTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, curvedT);
            
            if (indicatorIcon != null)
                indicatorIcon.localRotation = Quaternion.Lerp(startRot, targetRot, curvedT);

            yield return null;
        }
        panelTransform.anchoredPosition = targetPos;
        if (indicatorIcon != null) indicatorIcon.localRotation = targetRot;
    }
}
