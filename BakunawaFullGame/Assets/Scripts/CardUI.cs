using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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

    [Header("Audio")]
    [SerializeField] private AudioClip flipCardSound;
    [SerializeField] private AudioClip hoverCardSound;
    private AudioSource audioSource;

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float selectedYOffset = 20f;
    [SerializeField] private float animSpeed = 15f;

    [Header("Drag Flip Animation")]
    [SerializeField] private float dragLiftScale = 1.15f;
    [SerializeField] private float dragMaxTiltX = 15f;  // Forward/back tilt based on vertical drag
    [SerializeField] private float dragMaxTiltY = 20f;  // Side tilt based on horizontal drag
    [SerializeField] private float dragTiltSpeed = 8f;
    [SerializeField] private float dragFlipDuration = 0.15f; // Quick lift animation duration

    [Header("Drag Shadow Settings")]
    [SerializeField] private float shadowBaseOffset = 20f;      // Base shadow offset when lifted
    [SerializeField] private float shadowTiltMultiplier = 1.5f; // How much tilt affects shadow position
    [SerializeField] private float shadowBaseAlpha = 0.35f;     // Base shadow opacity
    [SerializeField] private float shadowScaleMultiplier = 1.08f; // Shadow is slightly larger than card

    // Drag animation state
    private Vector2 lastDragPosition;
    private Vector2 dragVelocity;
    private float currentTiltX = 0f;
    private float currentTiltY = 0f;
    private Coroutine liftAnimationCoroutine;
    private float currentShadowAlpha = 0f;

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
    private bool isExhausted = false; // Exhausted cards cannot be selected for 1 round
    
    /// <summary>
    /// Returns true if the card is exhausted (cannot be used this round)
    /// </summary>
    public bool IsExhausted => isExhausted;
    
    /// <summary>
    /// Sets the exhausted state. Exhausted cards appear face-down and cannot be selected.
    /// </summary>
    public void SetExhausted(bool exhausted)
    {
        isExhausted = exhausted;
        
        if (isExhausted)
        {
            // Show card as face-down/dimmed to indicate exhaustion
            if (cardBackObject != null) cardBackObject.SetActive(true);
            
            // Dim the card
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;
        }
        else
        {
            // Restore normal appearance
            if (cardBackObject != null) cardBackObject.SetActive(false);
            
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }
    
    /// <summary>
    /// Clears exhaustion without visual changes (for cleanup)
    /// </summary>
    public void ClearExhaustion()
    {
        isExhausted = false;
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
    }

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

        // Pre-initialize AudioSource for instant sound playback
        InitializeAudioSource();

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

        // Switch to normal/full card state (from minimized/locked state)
        SetLockedState(false);

        // Play flip card sound
        PlayFlipSound();

        EnableDragShadow(true);

        // Initialize drag animation state
        lastDragPosition = eventData.position;
        dragVelocity = Vector2.zero;
        currentTiltX = 0f;
        currentTiltY = 0f;

        // Start lift animation (quick flip effect)
        if (liftAnimationCoroutine != null) StopCoroutine(liftAnimationCoroutine);
        liftAnimationCoroutine = StartCoroutine(DragLiftAnimation());
    }

    /// <summary>
    /// Pre-initializes the AudioSource for instant sound playback
    /// </summary>
    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }
    }

    /// <summary>
    /// Plays the flip card sound effect
    /// </summary>
    private void PlayFlipSound()
    {
        if (flipCardSound == null || audioSource == null) return;
        audioSource.PlayOneShot(flipCardSound);
    }

    /// <summary>
    /// Plays the hover card sound effect when hovering over a card in hand
    /// </summary>
    private void PlayHoverSound()
    {
        if (hoverCardSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverCardSound);
        }
    }

    private GameObject dragShadowObject;
    private Image[] shadowLayers; // Multiple layers for soft shadow effect
    private RectTransform shadowRectTransform;
    private const int SHADOW_LAYER_COUNT = 3; // Number of shadow layers for softness

    void EnableDragShadow(bool enable)
    {
        if (enable)
        {
            if (dragShadowObject == null)
            {
                CreateMultiLayerShadow();
            }
            dragShadowObject.SetActive(true);
            currentShadowAlpha = 0f; // Start faded out, will animate in
        }
        else
        {
            if (dragShadowObject != null) dragShadowObject.SetActive(false);
        }
    }

    /// <summary>
    /// Creates a multi-layer shadow for a softer, more realistic appearance
    /// </summary>
    void CreateMultiLayerShadow()
    {
        dragShadowObject = new GameObject("DragShadow");
        dragShadowObject.transform.SetParent(transform, false);
        dragShadowObject.transform.SetAsFirstSibling();

        shadowRectTransform = dragShadowObject.AddComponent<RectTransform>();
        shadowRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Get card size
        RectTransform cardRect = GetComponent<RectTransform>();
        float cardWidth = cardRect.rect.width;
        float cardHeight = cardRect.rect.height;

        shadowLayers = new Image[SHADOW_LAYER_COUNT];

        // Create multiple shadow layers - each progressively larger and more transparent
        for (int i = 0; i < SHADOW_LAYER_COUNT; i++)
        {
            GameObject layerObj = new GameObject($"ShadowLayer_{i}");
            layerObj.transform.SetParent(dragShadowObject.transform, false);

            Image layerImage = layerObj.AddComponent<Image>();
            layerImage.raycastTarget = false;

            // Each layer is progressively larger and more transparent
            float layerScale = shadowScaleMultiplier + (i * 0.04f);
            float layerAlpha = shadowBaseAlpha / (i + 1); // Decreasing alpha for outer layers

            layerImage.color = new Color(0, 0, 0, layerAlpha);

            RectTransform layerRect = layerObj.GetComponent<RectTransform>();
            layerRect.anchorMin = new Vector2(0.5f, 0.5f);
            layerRect.anchorMax = new Vector2(0.5f, 0.5f);
            layerRect.pivot = new Vector2(0.5f, 0.5f);
            layerRect.sizeDelta = new Vector2(cardWidth * layerScale, cardHeight * layerScale);
            layerRect.anchoredPosition = Vector2.zero;

            shadowLayers[i] = layerImage;
        }

        // Set initial shadow position
        shadowRectTransform.sizeDelta = new Vector2(cardWidth, cardHeight);
        shadowRectTransform.anchoredPosition = new Vector2(shadowBaseOffset, -shadowBaseOffset);
    }

    /// <summary>
    /// Updates shadow position and appearance based on card tilt and lift state
    /// </summary>
    void UpdateDragShadow(float liftProgress = 1f)
    {
        if (dragShadowObject == null || !dragShadowObject.activeSelf) return;
        if (shadowRectTransform == null) return;

        // Calculate shadow offset based on tilt
        // When card tilts right (positive Y), shadow moves left
        // When card tilts down (positive X), shadow moves up
        float tiltOffsetX = -currentTiltY * shadowTiltMultiplier;
        float tiltOffsetY = currentTiltX * shadowTiltMultiplier;

        // Base offset (light source from top-left)
        float baseX = shadowBaseOffset * liftProgress;
        float baseY = -shadowBaseOffset * liftProgress;

        // Apply combined offset
        shadowRectTransform.anchoredPosition = new Vector2(baseX + tiltOffsetX, baseY + tiltOffsetY);

        // Animate shadow alpha (fade in during lift)
        float targetAlpha = liftProgress;
        currentShadowAlpha = Mathf.Lerp(currentShadowAlpha, targetAlpha, Time.deltaTime * 10f);

        // Update shadow layer alphas
        if (shadowLayers != null)
        {
            for (int i = 0; i < shadowLayers.Length; i++)
            {
                if (shadowLayers[i] != null)
                {
                    float layerAlpha = (shadowBaseAlpha / (i + 1)) * currentShadowAlpha;
                    shadowLayers[i].color = new Color(0, 0, 0, layerAlpha);
                }
            }
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

        // Calculate drag velocity for tilt effect
        dragVelocity = (eventData.position - lastDragPosition) / Time.deltaTime;
        lastDragPosition = eventData.position;

        // Apply dynamic tilt based on velocity
        UpdateDragTilt();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (layoutElement != null) layoutElement.ignoreLayout = false;

        if (dragCanvasGroup != null) dragCanvasGroup.blocksRaycasts = true;

        EnableDragShadow(false);

        // Stop any ongoing lift animation
        if (liftAnimationCoroutine != null)
        {
            StopCoroutine(liftAnimationCoroutine);
            liftAnimationCoroutine = null;
        }

        // Reset rotation smoothly
        StartCoroutine(ResetDragRotation());

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
            // Drag cancelled - return to original parent and restore locked/minimized state
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            SetLockedState(true); // Return to minimized state
            UpdateLockedLayout(); // Restore proper layout settings
        }
    }

    /// <summary>
    /// Quick lift animation when starting to drag - gives a satisfying "pickup" feel
    /// </summary>
    private IEnumerator DragLiftAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = baseScale * dragLiftScale;

        // First half: Scale up and rotate slightly (like lifting the card)
        while (elapsed < dragFlipDuration * 0.5f)
        {
            float t = elapsed / (dragFlipDuration * 0.5f);
            t = t * t * (3f - 2f * t); // Smooth step

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            // Slight Y rotation for flip effect (0 -> 15 degrees)
            float yRot = Mathf.Lerp(0f, 15f, t);
            transform.localRotation = Quaternion.Euler(currentTiltX, yRot, 0f);

            // Update shadow with lift progress
            UpdateDragShadow(t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Second half: Rotate back and settle at drag scale
        elapsed = 0f;
        while (elapsed < dragFlipDuration * 0.5f)
        {
            float t = elapsed / (dragFlipDuration * 0.5f);
            t = t * t * (3f - 2f * t); // Smooth step

            // Y rotation back (15 -> 0 degrees)
            float yRot = Mathf.Lerp(15f, 0f, t);
            transform.localRotation = Quaternion.Euler(currentTiltX, yRot, 0f);

            // Shadow fully visible in second half
            UpdateDragShadow(1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        liftAnimationCoroutine = null;
    }

    /// <summary>
    /// Updates card tilt based on drag velocity for a dynamic 3D feel
    /// </summary>
    private void UpdateDragTilt()
    {
        // Calculate target tilt based on velocity
        // Horizontal velocity -> Y-axis tilt (card tilts in direction of movement)
        // Vertical velocity -> X-axis tilt (card tilts forward/back)
        float targetTiltY = Mathf.Clamp(-dragVelocity.x * 0.02f, -dragMaxTiltY, dragMaxTiltY);
        float targetTiltX = Mathf.Clamp(dragVelocity.y * 0.015f, -dragMaxTiltX, dragMaxTiltX);

        // Smooth the tilt
        currentTiltY = Mathf.Lerp(currentTiltY, targetTiltY, Time.deltaTime * dragTiltSpeed);
        currentTiltX = Mathf.Lerp(currentTiltX, targetTiltX, Time.deltaTime * dragTiltSpeed);

        // Apply rotation (only if lift animation is done)
        if (liftAnimationCoroutine == null)
        {
            transform.localRotation = Quaternion.Euler(currentTiltX, currentTiltY, 0f);
        }

        // Update shadow to follow tilt
        UpdateDragShadow(1f);
    }

    /// <summary>
    /// Smoothly resets the card rotation when drag ends
    /// </summary>
    private IEnumerator ResetDragRotation()
    {
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = Quaternion.identity;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = baseScale * (HandManager.Instance != null ? HandManager.Instance.lockedScale : 1f);

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smooth step

            transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = targetRot;
        transform.localScale = targetScale;
        currentTiltX = 0f;
        currentTiltY = 0f;
    }

    // --- Hover Handling ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enabled || isEnemy || isDeckMode || isLocked) return;
        isHovered = true;
        PlayHoverSound();
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
            if (HandManager.Instance != null) HandManager.Instance.HideCardDetails();
            detailsShown = false;
            return;
        }

        if (pressTimer < holdTimeNeeded && !isDragging)
        {
            if (HandManager.Instance == null) return;
            if (isEnemy) return;
            if (isDeckMode) return;
            if (isExhausted) return; // Cannot select exhausted cards

            if (HandManager.Instance.isPlanningPhase)
            {
                if (!isLocked)
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

                // Prevent showing details for enemy's locked cards
                if (isEnemy && isLocked) return;

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