using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    public bool IsHovered => isHovered; 
    public bool IsSelected { get; private set; } = false; 

    // Sorting Components
    private Canvas _canvas;
    private GraphicRaycaster _raycaster;

    private CardData data;
    private bool isPressed = false;
    private float pressTimer = 0f;
    private bool detailsShown = false;
    private float holdTimeNeeded = 0.5f;

    // Drag State
    private Transform originalParent;
    private int originalSiblingIndex;
    private CanvasGroup dragCanvasGroup;
    private LayoutElement layoutElement;

    // STATE FLAGS
    private bool isLocked = false;
    private bool isDeckMode = false;
    private bool isDragging = false;

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
        
        // Reset Layout Element so it doesn't interfere with Hand Layout
        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;

        // Disable Shadow
        if (cardFrameImage != null)
        {
            Shadow shadow = cardFrameImage.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = false;
        }

        SetVisualsVisible(true);
    }

    public void UpdateLockedLayout()
    {
        // When in locked area, we want the Layout Group to treat this card as smaller
        // to match its visual scale (lockedScale).
        LayoutElement le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null && HandManager.Instance != null)
        {
             float s = HandManager.Instance.lockedScale;
             le.preferredWidth = rt.rect.width * s;
             le.preferredHeight = rt.rect.height * s;
             le.ignoreLayout = false;
        }

        // Add Shadow for grounded feel
        if (cardFrameImage != null)
        {
            Shadow shadow = cardFrameImage.GetComponent<Shadow>();
            if (shadow == null) shadow = cardFrameImage.gameObject.AddComponent<Shadow>();
            
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(4, -4); // Subtle drop shadow
            shadow.enabled = true;
        }
    }

    // --- Drag Implementation ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (HandManager.Instance == null) return;
        if (transform.parent != HandManager.Instance.tribeLockedPanel) return;
        if (isEnemy || isDeckMode) return;

        // Check if we can even play right now (using HandManager check)
        // HandManager controls the flow, but we can pre-check
        // This prevents picking up cards when it's not our turn
        // We can access 'playCardButton.interactable' indirectly or just try. 
        // Better yet, just allow drag, and fail on drop if invalid.
        // But for UX, maybe don't allow drag if locked.
        // HandManager doesn't expose a simple "IsMyTurn" bool publicly other than button interactable.
        // We will allow drag for now and fail on drop for feedback, OR check button.
        // Let's assume valid drag only if technically possible.

        isDragging = true;
        isPressed = false; // CANCEL HOLD LOGIC
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null) layoutElement.ignoreLayout = true;

        // Parent to root canvas so it floats above everything
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        transform.SetParent(rootCanvas.transform);

        // Canvas Group for raycast passthrough
        dragCanvasGroup = GetComponent<CanvasGroup>();
        if (dragCanvasGroup == null) dragCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        dragCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        // Restore layout behavior immediately so it can be caught by LayoutGroups (BattleZone or Hand)
        if (layoutElement != null) layoutElement.ignoreLayout = false;

        if (dragCanvasGroup != null) dragCanvasGroup.blocksRaycasts = true;

        // Check if dropped on BattleZone
        bool success = false;
        if (HandManager.Instance != null && HandManager.Instance.battleZone != null)
        {
            RectTransform battleRect = HandManager.Instance.battleZone.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(battleRect, eventData.position, eventData.pressEventCamera))
            {
                // Dropped in zone! Try to play.
                if (HandManager.Instance.TryPlayCard(this))
                {
                    success = true;
                }
            }
        }

        if (!success)
        {
            // Return to hand
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            
            // Reset position/scale local logic will handle in Update()
        }
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
        if (pressTimer < holdTimeNeeded && !isDragging)
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
                    // No longer select on click in battle phase if locked - Drag is used
                    // HandManager.Instance.SelectCardForBattle(this);
                }
            }
        }
    }

    void Update()
    {
        // HOLD LOGIC
        if (isPressed && !detailsShown && !isDragging)
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
        
        bool isInLockedArea = false;
        if (HandManager.Instance != null && (transform.parent == HandManager.Instance.tribeSelectedPanel || transform.parent == HandManager.Instance.tribeLockedPanel || transform.parent == HandManager.Instance.battleZone))
            isInLockedArea = true;

        // If in locked area, we DO NOT want to pop on top of the hand.
        // The hand cards (when hovered) should be on top.
        if (isInLockedArea)
        {
             _canvas.overrideSorting = false;
             return; 
        }

        // If hovered or selected (and NOT in locked area), render on top of others
        bool shouldPop = (isHovered || IsSelected) && !isLocked && !isEnemy && !isDeckMode;

        if (shouldPop)
        {
            _canvas.overrideSorting = true;
            // Selected cards slightly higher than just hovered ones (if both in hand?)
            // But if it's selected, it usually goes to locked area. 
            // In case a selected card is still in hand (animating), keep it high.
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
        
        // Only animate in tribeSelectedPanel, tribeLockedPanel OR battleZone
        if (p != HandManager.Instance.tribeSelectedPanel && p != HandManager.Instance.tribeLockedPanel && p != HandManager.Instance.battleZone) return;

        // Calculate Target Scale & Position for locked hand area
        Vector3 targetScaleVec = baseScale;
        float targetY = 0f;

        if (p == HandManager.Instance.tribeSelectedPanel || p == HandManager.Instance.tribeLockedPanel)
        {
            // NEW LOGIC: Use specific scale for locked cards and sit flat in the panel
            float scale = (HandManager.Instance != null) ? HandManager.Instance.lockedScale : 1f;
            targetScaleVec = baseScale * scale;
            targetY = 0f;
            
            // Apply straight to local vars to animate logic below
        }
        else if (p == HandManager.Instance.battleZone)
        {
             // Battle Zone Logic: Scale to 'playCardScale' or 1.0
             float scale = (HandManager.Instance != null) ? HandManager.Instance.playCardScale : 1f;
             targetScaleVec = baseScale * scale;
             targetY = 0f;
        }
        else if (isLocked)
        {
            // Keep locked cards at base state (if somewhere else?)
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