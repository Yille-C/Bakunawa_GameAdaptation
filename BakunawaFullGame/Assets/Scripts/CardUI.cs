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
    public GameObject lockedArtObject; // The parent object for the locked state
    public Image lockedArtImage;       // The specific Image component for the locked art
    public GameObject brokenVisuals;

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

        // Ensure we have a Canvas for sorting override
        _canvas = GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();

        _raycaster = GetComponent<GraphicRaycaster>();
        if (_raycaster == null) _raycaster = gameObject.AddComponent<GraphicRaycaster>();

        if (_canvas != null) _canvas.overrideSorting = false;

        // Initialize glow overlay
        if (glowOverlay == null)
        {
            glowOverlay = GetComponent<CardGlowOverlay>();
            if (glowOverlay == null) glowOverlay = gameObject.AddComponent<CardGlowOverlay>();
        }

        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        if (attackText != null) attackText.text = data.attackValue.ToString();

        if (data.cardArt != null && artworkImage != null) artworkImage.sprite = data.cardArt;

        // --- ASSIGN LOCKED ART (Works for Enemy too) ---
        if (lockedArtImage != null && data.lockedArt != null)
        {
            lockedArtImage.sprite = data.lockedArt;
        }

        // Assign locked stats text (we will hide them later if it's an enemy)
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

        if (selectionBorder != null) selectionBorder.SetActive(false);
        if (glowOverlay != null) glowOverlay.SetGlowEnabledImmediate(false);
        if (cardBackObject != null) cardBackObject.SetActive(false);

        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        if (brokenVisuals != null) brokenVisuals.SetActive(false);

        SetVisualsVisible(true);
        if (cardFrameImage != null) cardFrameImage.color = Color.white;

        this.enabled = true;
    }

    public void SetLockedState(bool locked)
    {
        isLocked = locked;

        // --- NEW FIX: Force Card Back OFF when locked ---
        if (isLocked && cardBackObject != null)
        {
            cardBackObject.SetActive(false);
        }
        // -----------------------------------------------

        if (lockedArtObject != null)
        {
            lockedArtObject.SetActive(isLocked);

            // --- ENEMY VISIBILITY ---
            if (isLocked)
            {
                if (isEnemy)
                {
                    // If it is Bakunawa (Enemy), HIDE the cost and attack numbers.
                    // This ensures ONLY the locked image is visible.
                    if (lockedCostObject != null) lockedCostObject.gameObject.SetActive(false);
                    if (lockedAttackObject != null) lockedAttackObject.gameObject.SetActive(false);
                }
                else
                {
                    // If it is the Player, we usually show the numbers on the locked card.
                    if (lockedCostObject != null) lockedCostObject.gameObject.SetActive(true);
                    if (lockedAttackObject != null) lockedAttackObject.gameObject.SetActive(true);
                }
            }
        }

        // This hides the normal card frame, artwork, and main text
        SetVisualsVisible(!isLocked);
    }

    public void SetBroken(bool broken)
    {
        if (brokenVisuals != null) brokenVisuals.SetActive(broken);
        if (cardFrameImage != null)
        {
            cardFrameImage.color = broken ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        }
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
        IsSelected = false;

        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;

        if (cardFrameImage != null)
        {
            Shadow shadow = cardFrameImage.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = false;
        }

        SetVisualsVisible(true);
    }

    public void UpdateLockedLayout()
    {
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

        if (cardFrameImage != null)
        {
            Shadow shadow = cardFrameImage.GetComponent<Shadow>();
            if (shadow == null) shadow = cardFrameImage.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(4, -4);
            shadow.enabled = true;
        }
    }

    // --- Drag Implementation ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (HandManager.Instance == null) return;
        if (transform.parent != HandManager.Instance.tribeLockedPanel) return;
        if (isEnemy || isDeckMode) return;

        if (HandManager.Instance.IsInputLocked) return;

        isDragging = true;
        isPressed = false;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null) layoutElement.ignoreLayout = true;

        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        transform.SetParent(rootCanvas.transform);

        dragCanvasGroup = GetComponent<CanvasGroup>();
        if (dragCanvasGroup == null) dragCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        dragCanvasGroup.blocksRaycasts = false;

        EnableDragShadow(true);
    }

    private GameObject dragShadowObject;
    private Image dragShadowImage;

    void EnableDragShadow(bool enable)
    {
        if (enable)
        {
            if (dragShadowObject == null)
            {
                dragShadowObject = new GameObject("DragShadow");
                dragShadowObject.transform.SetParent(transform, false);
                dragShadowObject.transform.SetAsFirstSibling();

                dragShadowImage = dragShadowObject.AddComponent<Image>();
                dragShadowImage.color = new Color(0, 0, 0, 0.5f);
                dragShadowImage.raycastTarget = false;

                RectTransform shadowRect = dragShadowObject.GetComponent<RectTransform>();

                shadowRect.anchorMin = Vector2.zero;
                shadowRect.anchorMax = Vector2.one;
                shadowRect.offsetMin = new Vector2(-5, -5);
                shadowRect.offsetMax = new Vector2(5, 5);

                shadowRect.anchoredPosition = new Vector2(15, -15);
            }
            dragShadowObject.SetActive(true);
        }
        else
        {
            if (dragShadowObject != null) dragShadowObject.SetActive(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Canvas root = GetComponentInParent<Canvas>();
        if (root != null && root.rootCanvas != null) root = root.rootCanvas;

        if (root != null)
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, eventData.position, root.worldCamera, out localPos))
            {
                transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            }
        }
        else
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (layoutElement != null) layoutElement.ignoreLayout = false;

        if (dragCanvasGroup != null) dragCanvasGroup.blocksRaycasts = true;

        EnableDragShadow(false);

        bool success = false;
        if (HandManager.Instance != null && HandManager.Instance.battleZone != null)
        {
            RectTransform battleRect = HandManager.Instance.battleZone.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(battleRect, eventData.position, eventData.pressEventCamera))
            {
                if (HandManager.Instance.TryPlayCard(this))
                {
                    success = true;
                }
            }
        }

        if (!success)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
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

        // 1. Handle hiding details (Safe for both modes if checked properly)
        if (detailsShown)
        {
            if (HandManager.Instance != null)
                HandManager.Instance.HideCardDetails();

            // If you have a details viewer in Multiplayer, add that check here too

            detailsShown = false;
            return;
        }

        // 2. Handle Click (Selection)
        if (pressTimer < holdTimeNeeded && !isDragging)
        {
            // Check for general restrictions first
            if (isEnemy) return;
            if (isDeckMode) return;
            if (isLocked) return; // Prevent clicking locked cards in both modes

            // --- MODE DETECTION ---

            // A. SINGLE PLAYER
            if (HandManager.Instance != null)
            {
                if (HandManager.Instance.isPlanningPhase)
                {
                    bool targetState = !IsSelected;
                    bool success = HandManager.Instance.ToggleCardSelection(this, targetState);

                    if (success)
                    {
                        IsSelected = targetState;
                        if (glowOverlay != null) glowOverlay.SetGlowEnabled(IsSelected);
                        if (selectionBorder != null) selectionBorder.SetActive(false);
                    }
                }
            }
            // B. MULTIPLAYER
            else if (MultiplayerGameManager.Instance != null)
            {
                // Pass the click to the multiplayer manager
                // The Manager will decide if it's the planning phase or if the card can be selected
                MultiplayerGameManager.Instance.OnCardClicked(this);
            }
        }
    }

    void Update()
    {
        if (isPressed && !detailsShown && !isDragging)
        {
            pressTimer += Time.deltaTime;

            if (pressTimer >= holdTimeNeeded)
            {
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

        if (isInLockedArea)
        {
            _canvas.overrideSorting = false;
            return;
        }

        bool shouldPop = (isHovered || IsSelected) && !isLocked && !isEnemy && !isDeckMode;

        if (shouldPop)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = IsSelected ? 21 : 20;
        }
        else
        {
            _canvas.overrideSorting = false;
        }
    }

    void UpdateAnimation()
    {
        if (HandManager.Instance == null) return;
        Transform p = transform.parent;

        if (p == HandManager.Instance.handArea) return;

        bool inBakunawaArea = false;
        if (BakunawaAI.Instance != null)
        {
            inBakunawaArea = (p == BakunawaAI.Instance.lockedArea ||
                              p == BakunawaAI.Instance.handArea ||
                              p == BakunawaAI.Instance.battleZone);
        }

        bool inTribeArea = (p == HandManager.Instance.tribeSelectedPanel ||
                           p == HandManager.Instance.tribeLockedPanel ||
                           p == HandManager.Instance.battleZone);

        if (!inTribeArea && !inBakunawaArea) return;

        Vector3 targetScaleVec = baseScale;
        float targetY = 0f;

        if (p == HandManager.Instance.tribeSelectedPanel || p == HandManager.Instance.tribeLockedPanel)
        {
            float scale = (HandManager.Instance != null) ? HandManager.Instance.lockedScale : 1f;
            targetScaleVec = baseScale * scale;
            targetY = 0f;
        }
        else if (BakunawaAI.Instance != null && p == BakunawaAI.Instance.lockedArea)
        {
            float scale = BakunawaAI.Instance.lockedScale;
            targetScaleVec = baseScale * scale;
            targetY = 0f;
        }
        else if (p == HandManager.Instance.battleZone ||
                 (BakunawaAI.Instance != null && p == BakunawaAI.Instance.battleZone))
        {
            float scale = (HandManager.Instance != null) ? HandManager.Instance.playCardScale : 1f;
            targetScaleVec = baseScale * scale;
            targetY = 0f;
        }
        else if (isLocked)
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
                targetY = selectedYOffset * 0.5f;
            }
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScaleVec, Time.deltaTime * animSpeed);

        Vector3 currentLocalPos = transform.localPosition;
        float newY = Mathf.Lerp(currentLocalPos.y, targetY, Time.deltaTime * animSpeed);
        transform.localPosition = new Vector3(currentLocalPos.x, newY, currentLocalPos.z);
    }
}