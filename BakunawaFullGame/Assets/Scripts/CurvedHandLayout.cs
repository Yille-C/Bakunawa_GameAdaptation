using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Arranges child cards in a curved/fan layout like a hand of cards.
/// Attach this to the handArea Transform in the scene.
/// </summary>
public class CurvedHandLayout : MonoBehaviour
{
    public static CurvedHandLayout Instance { get; private set; }

    [Header("Arc Settings")]
    [Tooltip("The maximum angle spread of the entire fan (in degrees).")]
    public float maxArcAngle = 40f;

    [Tooltip("The radius of the arc. Larger = flatter curve.")]
    public float arcRadius = 800f;

    [Tooltip("Vertical offset for the arc center (negative = below cards).")]
    public float arcCenterYOffset = -600f;

    [Header("Card Spacing")]
    [Tooltip("Maximum horizontal spread when few cards.")]
    public float maxCardSpacing = 120f;

    [Tooltip("Minimum spacing between cards when many cards.")]
    public float minCardSpacing = 60f;

    [Tooltip("Maximum number of cards before spacing shrinks.")]
    public int cardsBeforeMinSpacing = 8;

    [Header("Animation")]
    [Tooltip("How fast cards move to their target positions.")]
    public float animationSpeed = 12f;

    [Tooltip("Delay between each card starting to animate (stagger effect).")]
    public float staggerDelay = 0.05f;

    [Header("Hover/Selection Effects")]
    [Tooltip("How much a hovered card rises above others.")]
    public float hoverLift = 60f;

    [Tooltip("How much extra a selected card rises.")]
    public float selectedLift = 80f;

    [Tooltip("Scale multiplier for hovered cards.")]
    public float hoverScale = 1.1f;

    [Tooltip("Scale multiplier for selected cards.")]
    public float selectedScale = 1.15f;

    [Header("Z-Order")]
    [Tooltip("Starting Z position for cards (frontmost card).")]
    public float baseZPosition = 0f;

    [Tooltip("Z spacing between cards (for proper layering).")]
    public float zSpacing = -1f;

    [Header("Dynamic Spacing")]
    [Tooltip("Distance neighbors move away when a card is hovered.")]
    public float hoverSeparation = 0f;

    // Internal data
    private List<CardLayoutData> cardData = new List<CardLayoutData>();
    private bool layoutDirty = true;

    private class CardLayoutData
    {
        public Transform cardTransform;
        public Vector3 targetPosition;
        public Quaternion targetRotation;
        public Vector3 targetScale;
        public float animationProgress;
        public bool isAnimating;
    }

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        // Mark layout dirty on enable
        layoutDirty = true;
    }

    void Update()
    {
        // Check if children changed
        if (ChildCountChanged() || layoutDirty)
        {
            RefreshCardList();
            CalculateLayout();
            layoutDirty = false;
        }

        // Animate cards toward their targets
        AnimateCards();
    }

    private int lastChildCount = -1;

    bool ChildCountChanged()
    {
        int current = transform.childCount;
        if (current != lastChildCount)
        {
            lastChildCount = current;
            return true;
        }
        return false;
    }

    void RefreshCardList()
    {
        cardData.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                cardData.Add(new CardLayoutData
                {
                    cardTransform = child,
                    targetPosition = child.localPosition,
                    targetRotation = child.localRotation,
                    targetScale = child.localScale,
                    animationProgress = 0f,
                    isAnimating = true
                });
            }
        }
    }

    void CalculateLayout()
    {
        int cardCount = cardData.Count;
        if (cardCount == 0) return;

        // Calculate spacing based on card count
        float spacing = Mathf.Lerp(maxCardSpacing, minCardSpacing, 
            (float)(cardCount - 1) / Mathf.Max(1, cardsBeforeMinSpacing - 1));
        spacing = Mathf.Max(spacing, minCardSpacing);

        // Calculate total width and angle
        float totalWidth = spacing * (cardCount - 1);
        float anglePerCard = cardCount > 1 ? maxArcAngle / (cardCount - 1) : 0f;

        // Clamp the arc angle for many cards
        if (cardCount > 1)
        {
            float maxAnglePerCard = maxArcAngle / (cardCount - 1);
            anglePerCard = Mathf.Min(anglePerCard, maxAnglePerCard);
        }

        // Start from the leftmost card
        float startAngle = -anglePerCard * (cardCount - 1) / 2f;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < cardCount; i++)
        {
            CardLayoutData data = cardData[i];
            
            float normalizedPos = cardCount > 1 ? (float)i / (cardCount - 1) : 0.5f;
            float angle = startAngle + anglePerCard * i;

            // Calculate position on arc
            float x = startX + spacing * i;
            
            // Y position based on arc (parabolic curve for natural hand feel)
            float normalizedFromCenter = (normalizedPos - 0.5f) * 2f; // -1 to 1
            float arcY = -Mathf.Abs(normalizedFromCenter) * (arcRadius * 0.05f); // Simple parabola

            // Alternative: true circular arc
            // float radians = angle * Mathf.Deg2Rad;
            // float arcY = Mathf.Cos(radians) * arcRadius - arcRadius + arcCenterYOffset;

            // Z position for layering (middle cards in front)
            float z = baseZPosition + Mathf.Abs(normalizedFromCenter) * zSpacing * cardCount;

            data.targetPosition = new Vector3(x, arcY, z);
            data.targetRotation = Quaternion.Euler(0f, 0f, -angle);
            data.targetScale = Vector3.one;
            data.isAnimating = true;
            data.animationProgress = 0f;
        }
    }

    void AnimateCards()
    {
        // 1. Find which card is hovered (if any)
        int hoveredIndex = -1;
        for (int i = 0; i < cardData.Count; i++)
        {
             if (cardData[i].cardTransform == null) continue;
             CardUI ui = cardData[i].cardTransform.GetComponent<CardUI>();
             if (ui != null && ui.IsHovered) 
             {
                 hoveredIndex = i;
                 break;
             }
        }

        for (int i = 0; i < cardData.Count; i++)
        {
            CardLayoutData data = cardData[i];

            if (data.cardTransform == null) continue;

            // Get additional offsets from CardUI hover/selection state
            Vector3 additionalOffset = Vector3.zero;
            Vector3 additionalScale = data.targetScale;
            Quaternion finalTargetRotation = data.targetRotation;

            CardUI cardUI = data.cardTransform.GetComponent<CardUI>();
            if (cardUI != null)
            {
                if (cardUI.IsSelected)
                {
                    additionalOffset.y += selectedLift;
                    additionalScale = data.targetScale * selectedScale;
                    // Bring selected card significantly to front
                    additionalOffset.z = -50f;
                    // Straighten the card so it's easy to read
                    finalTargetRotation = Quaternion.identity;
                    
                }
                else if (IsCardHovered(cardUI))
                {
                    additionalOffset.y += hoverLift;
                    additionalScale = data.targetScale * hoverScale;
                    // Bring hovered card significantly forward to overlap neighbors
                    additionalOffset.z = -40f;
                    // Straighten the card so it's easy to read
                    finalTargetRotation = Quaternion.identity;
                }
            }

            // --- SEPARATION LOGIC ---
            // If some card is hovered, push neighbors away
            if (hoveredIndex != -1 && i != hoveredIndex)
            {
                // If this card is to the left of the hovered card -> push left
                if (i < hoveredIndex)
                    additionalOffset.x -= hoverSeparation;
                // If this card is to the right of the hovered card -> push right
                else if (i > hoveredIndex)
                    additionalOffset.x += hoverSeparation;
            }
            // -------------------------

            Vector3 finalTargetPos = data.targetPosition + additionalOffset;
            Vector3 finalTargetScale = additionalScale;

            // Smooth animation
            float speed = animationSpeed * Time.deltaTime;
            data.cardTransform.localPosition = Vector3.Lerp(
                data.cardTransform.localPosition, 
                finalTargetPos, 
                speed
            );
            data.cardTransform.localRotation = Quaternion.Slerp(
                data.cardTransform.localRotation, 
                finalTargetRotation, 
                speed
            );
            data.cardTransform.localScale = Vector3.Lerp(
                data.cardTransform.localScale, 
                finalTargetScale, 
                speed
            );
        }
    }

    bool IsCardHovered(CardUI cardUI)
    {
        if (cardUI == null) return false;
        return cardUI.IsHovered;
    }

    /// <summary>
    /// Call this to force a layout recalculation.
    /// </summary>
    public void ForceLayoutUpdate()
    {
        layoutDirty = true;
    }

    /// <summary>
    /// Called when a card is added to the hand.
    /// </summary>
    public void OnCardAdded()
    {
        layoutDirty = true;
    }

    /// <summary>
    /// Called when a card is removed from the hand.
    /// </summary>
    public void OnCardRemoved()
    {
        layoutDirty = true;
    }

    /// <summary>
    /// Immediately snap all cards to their layout positions (no animation).
    /// </summary>
    public void SnapToLayout()
    {
        RefreshCardList();
        CalculateLayout();

        for (int i = 0; i < cardData.Count; i++)
        {
            CardLayoutData data = cardData[i];
            if (data.cardTransform != null)
            {
                data.cardTransform.localPosition = data.targetPosition;
                data.cardTransform.localRotation = data.targetRotation;
                data.cardTransform.localScale = data.targetScale;
            }
        }
    }

    // Editor visualization
    void OnDrawGizmosSelected()
    {
        // Draw the arc for visualization
        Gizmos.color = Color.cyan;
        
        int segments = 20;
        Vector3 lastPoint = Vector3.zero;
        
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-maxArcAngle / 2f, maxArcAngle / 2f, t);
            float radians = angle * Mathf.Deg2Rad;
            
            float x = Mathf.Sin(radians) * arcRadius;
            float y = Mathf.Cos(radians) * arcRadius - arcRadius + arcCenterYOffset;
            
            Vector3 point = transform.TransformPoint(new Vector3(x, y, 0));
            
            if (i > 0)
            {
                Gizmos.DrawLine(lastPoint, point);
            }
            lastPoint = point;
        }
    }
}
