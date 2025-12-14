using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image cardFrameImage;
    public Image artworkImage;
    public Text nameText;
    public Text costText;
    public Text attackText;
    [Tooltip("Legacy selection border - will be hidden. Glow effect is now used instead.")]
    public GameObject selectionBorder;
    
    [Header("Glow Effect")]
    [Tooltip("Optional: Glow overlay component. Will be auto-created if not assigned.")]
    public CardGlowOverlay glowOverlay;
    
    // Legacy reference kept for compatibility
    [HideInInspector] public CardGlowEffect glowEffect;

    [Header("Card States")]
    public GameObject cardBackObject;
    public GameObject lockedArtObject;

    [Header("Locked Info References")]
    public Text lockedCostObject;
    public Text lockedAttackObject;

    [Header("Settings")]
    public bool isEnemy = false;

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float selectedYOffset = 20f;
    [SerializeField] private float animSpeed = 15f;
    
    // Internal Animation State
    private Vector3 baseScale;
    private bool isHovered = false;
    public bool IsHovered => isHovered; // Public property for CurvedHandLayout
    public bool IsSelected { get; private set; } = false; // Public property for read access logic

    // Sorting Components
    private Canvas _canvas;
    private GraphicRaycaster _raycaster;

    private CardData data;
    private bool isPressed = false;
    private float pressTimer = 0f;
    private bool detailsShown = false;
    private float holdTimeNeeded = 0.5f;

    // STATE FLAGS
    private bool isLocked = false;
    private bool isDeckMode = false; // Used to prevent clicking without disabling script

    public void Setup(CardData cardData)
    {
        data = cardData;
        baseScale = transform.localScale;
        // Keep initial scale from the prefab or set to one
        if (baseScale == Vector3.zero) baseScale = Vector3.one;

        // Ensure we have a Canvas for sorting override (to pop hovered cards to front)
        _canvas = GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();

        _raycaster = GetComponent<GraphicRaycaster>();
        if (_raycaster == null) _raycaster = gameObject.AddComponent<GraphicRaycaster>();

        // Default to not overriding
        if (_canvas != null) _canvas.overrideSorting = false;
        
        // Initialize glow overlay - auto-create if not assigned
        if (glowOverlay == null)
        {
            glowOverlay = GetComponent<CardGlowOverlay>();
            if (glowOverlay == null)
            {
                glowOverlay = gameObject.AddComponent<CardGlowOverlay>();
            }
        }

        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        if (attackText != null) attackText.text = data.attackValue.ToString();

        if (data.cardArt != null && artworkImage != null) artworkImage.sprite = data.cardArt;

        if (lockedCostObject != null)
        {
            Text txt = lockedCostObject.GetComponent<Text>();
            if (txt != null) txt.text = data.energyCost.ToString();
        }
        if (lockedAttackObject != null)
        {
            Text txt = lockedAttackObject.GetComponent<Text>();
            if (txt != null) txt.text = data.attackValue.ToString();
        }

        // Hide legacy selection border (glow effect is used now)
        if (selectionBorder != null) selectionBorder.SetActive(false);
        // Ensure glow starts disabled
        if (glowOverlay != null) glowOverlay.SetGlowEnabledImmediate(false);
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        SetVisualsVisible(true);
        if (cardFrameImage != null) cardFrameImage.color = Color.white;

        // Ensure script is ON so we can detect holds
        this.enabled = true;
    }

    public void SetLockedState(bool locked)
    {
        isLocked = locked;
        if (lockedArtObject != null) lockedArtObject.SetActive(isLocked);
        SetVisualsVisible(!isLocked);
    }

    void SetVisualsVisible(bool isVisible)
    {
        if (cardFrameImage != null) cardFrameImage.enabled = isVisible;
        if (artworkImage != null) artworkImage.enabled = isVisible;
        if (nameText != null) nameText.enabled = isVisible;
        if (costText != null) costText.enabled = isVisible;
        if (attackText != null) attackText.enabled = isVisible;
    }

    public void SwitchToDeckMode(bool showBack)
    {
        isDeckMode = true;

        if (cardBackObject != null) cardBackObject.SetActive(showBack);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        SetVisualsVisible(true);

        // CHANGED: We KEEP the script enabled so logic works, 
        // but we block clicks using the 'isDeckMode' flag instead.
        this.enabled = true;
    }

    public void ResetToHandMode()
    {
        isDeckMode = false;
        this.enabled = true;

        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);
        if (selectionBorder != null) selectionBorder.SetActive(false);
        if (glowOverlay != null) glowOverlay.SetGlowEnabledImmediate(false);
        IsSelected = false; // Reset internal state
        
        SetVisualsVisible(true);
    }

    // --- Hover Handling ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enabled || isEnemy || isDeckMode || isLocked) return;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // --- Input Handling ---
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        pressTimer = 0f;
        detailsShown = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (detailsShown)
        {
            // Close details if they were open
            if (HandManager.Instance != null) HandManager.Instance.HideCardDetails();
            detailsShown = false;
            return; // Don't process click logic
        }

        // Only process click if it wasn't a long hold
        if (pressTimer < holdTimeNeeded)
        {
            if (HandManager.Instance == null) return;

            // 1. Prevent clicking Enemy Cards
            if (isEnemy) return;

            // 2. Prevent clicking Deck/Pile Cards
            if (isDeckMode) return;

            // 3. Normal Logic
            if (HandManager.Instance.isPlanningPhase)
            {
                if (!isLocked)
                {
                    // Toggle Selection Logic
                    bool targetState = !IsSelected;
                    // Ask Manager if we can toggle (updates energy etc)
                    bool success = HandManager.Instance.ToggleCardSelection(this, targetState);
                    
                    if (success)
                    {
                        IsSelected = targetState;
                        // Use glow effect for visual feedback
                        if (glowOverlay != null) glowOverlay.SetGlowEnabled(IsSelected);
                        // Keep legacy border hidden
                        if (selectionBorder != null) selectionBorder.SetActive(false);
                    }
                }
            }
            else
            {
                if (isLocked)
                {
                    HandManager.Instance.SelectCardForBattle(this);
                }
            }
        }
    }

    void Update()
    {
        // HOLD LOGIC
        if (isPressed && !detailsShown)
        {
            pressTimer += Time.deltaTime;

            if (pressTimer >= holdTimeNeeded)
            {
                // SAFETY: Don't show details if card is Face Down (Cheating protection)
                if (cardBackObject != null && cardBackObject.activeSelf) return;

                detailsShown = true;
                if (HandManager.Instance != null && data != null)
                {
                    HandManager.Instance.ShowCardDetails(data);
                }
            }
        }
        
        UpdateAnimation();
        UpdateSorting();
    }

    void UpdateSorting()
    {
        if (_canvas == null) return;

        // If hovered or selected, render on top of others
        bool shouldPop = (isHovered || IsSelected) && !isLocked && !isEnemy && !isDeckMode;

        if (shouldPop)
        {
            _canvas.overrideSorting = true;
            // Selected cards slightly higher than just hovered ones
            _canvas.sortingOrder = IsSelected ? 21 : 20; 
        }
        else
        {
            _canvas.overrideSorting = false;
        }
    }

    void UpdateAnimation()
    {
        // Safety: Only animate if in valid areas
        if (HandManager.Instance == null) return;
        Transform p = transform.parent;
        
        // Skip animation for handArea - CurvedHandLayout handles that
        if (p == HandManager.Instance.handArea) return;
        
        // Only animate in lockedHandArea
        if (p != HandManager.Instance.lockedHandArea) return;

        // Calculate Target Scale & Position for locked hand area
        Vector3 targetScaleVec = baseScale;
        float targetY = 0f;

        if (isLocked)
        {
            // Keep locked cards at base state
        }
        else if (isEnemy || isDeckMode)
        {
            // No interaction animations
        }
        else
        {
            if (IsSelected)
            {
                targetScaleVec = baseScale * selectedScale;
                targetY = selectedYOffset;
            }
            else if (isHovered && !isPressed)
            {
                targetScaleVec = baseScale * hoverScale;
                targetY = selectedYOffset * 0.5f; // Slight lift on hover
            }
        }

        // Apply Smoothing
        transform.localScale = Vector3.Lerp(transform.localScale, targetScaleVec, Time.deltaTime * animSpeed);
        
        // Apply Y-Offset for locked area
        Vector3 currentLocalPos = transform.localPosition;
        float newY = Mathf.Lerp(currentLocalPos.y, targetY, Time.deltaTime * animSpeed);
        transform.localPosition = new Vector3(currentLocalPos.x, newY, currentLocalPos.z);
    }
}