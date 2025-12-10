using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Toggle))]
public class ToggleSwitch : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The moving handle/knob. If empty, tries to use Toggle.graphic.")]
    [SerializeField] private RectTransform uiHandleRect;
    [Tooltip("The background track. If empty, tries to use Toggle.targetGraphic.")]
    [SerializeField] private Image backgroundImage;

    [Header("Visual Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Color backgroundColorOn = new Color(0.18f, 0.76f, 0.81f); // Cyan
    [SerializeField] private Color backgroundColorOff = new Color(0.00f, 0.00f, 0.00f); // Dark Gray / Unsaturated

    [Header("Layout Settings")]
    [Tooltip("Padding between handle and background edge.")]
    [SerializeField] private float padding = 5f;

    private Toggle toggle;
    private Coroutine animateCoroutine;
    private Vector2 offPosition;
    private Vector2 onPosition;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        SetupReferences();
        CalculatePositions();
        
        // Ensure default transition is off so Unity doesn't interfere
        toggle.transition = Toggle.Transition.None;
        
        // Listen for changes
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        
        // Initial state (instant)
        UpdateVisuals(toggle.isOn, true);
    }

    private void SetupReferences()
    {
        // Auto-detect Handle if missing
        if (uiHandleRect == null && toggle.graphic != null)
        {
            uiHandleRect = toggle.graphic.rectTransform;
            
            // IMPORTANT: Unlink from Toggle so Unity doesn't hide it (fade out) when Off.
            // We want it visible and moving.
            toggle.graphic = null;
            
            // Ensure it's active
            uiHandleRect.gameObject.SetActive(true);
        }

        // Auto-detect Background if missing
        if (backgroundImage == null && toggle.targetGraphic is Image)
        {
            backgroundImage = (Image)toggle.targetGraphic;
        }
    }

    private void CalculatePositions()
    {
        if (uiHandleRect == null || backgroundImage == null) return;

        float bgWidth = backgroundImage.rectTransform.rect.width;
        float handleWidth = uiHandleRect.rect.width;

        // Assume the handle is anchored to the center or behaves relative to the background center
        // Move distance is generally half the background width minus half handle width (and padding)
        
        float moveDistance = (bgWidth / 2f) - (handleWidth / 2f) - padding;

        onPosition = new Vector2(moveDistance, 0);
        offPosition = new Vector2(-moveDistance, 0);
    }

    // Called when the toggle value changes
    private void OnToggleValueChanged(bool isOn)
    {
        UpdateVisuals(isOn, false);
    }

    private void UpdateVisuals(bool isOn, bool instant)
    {
        if (animateCoroutine != null) StopCoroutine(animateCoroutine);

        if (instant)
        {
            if (uiHandleRect != null) uiHandleRect.anchoredPosition = isOn ? onPosition : offPosition;
            if (backgroundImage != null) backgroundImage.color = isOn ? backgroundColorOn : backgroundColorOff;
        }
        else
        {
            animateCoroutine = StartCoroutine(AnimateSwitch(isOn));
        }
    }

    private IEnumerator AnimateSwitch(bool isOn)
    {
        float timer = 0f;
        
        Vector2 startPos = uiHandleRect != null ? uiHandleRect.anchoredPosition : Vector2.zero;
        Vector2 targetPos = isOn ? onPosition : offPosition;
        
        Color startColor = backgroundImage != null ? backgroundImage.color : Color.white;
        Color targetColor = isOn ? backgroundColorOn : backgroundColorOff;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / animationDuration);
            
            // Ease out cubic
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            if (uiHandleRect != null)
                uiHandleRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeT);
            
            if (backgroundImage != null)
                backgroundImage.color = Color.Lerp(startColor, targetColor, easeT);

            yield return null;
        }

        // Finalize
        if (uiHandleRect != null) uiHandleRect.anchoredPosition = targetPos;
        if (backgroundImage != null) backgroundImage.color = targetColor;
    }
    
    // Manual re-calc wrapper for Editor if needed
    public void RefreshLayout() => CalculatePositions();
}
