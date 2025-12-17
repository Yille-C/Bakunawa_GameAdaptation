using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Creates a convincing pseudo-3D dice rolling animation using UI transforms.
/// Simulates 3D tumbling with rotation, scaling, and bounce effects.
/// </summary>
public class Dice3DAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image diceImage;
    [SerializeField] private RectTransform diceTransform;
    [SerializeField] private Image shadowImage;
    
    [Header("Dice Face Sprites")]
    [SerializeField] private List<Sprite> diceFaces; // 6 faces (index 0 = 1, index 5 = 6)
    
    [Header("Animation Settings")]
    [SerializeField] private float rollDuration = 1.5f;
    [SerializeField] private float maxRotationSpeed = 720f; // Degrees per second
    [SerializeField] private float bounceHeight = 50f;
    [SerializeField] private float bounceCount = 3f;
    [SerializeField] private float maxTiltAngle = 25f;
    [SerializeField] private float maxScaleVariation = 0.3f;
    
    [Header("Shadow Settings")]
    [SerializeField] private float shadowMaxOffset = 30f;
    [SerializeField] private float shadowMinAlpha = 0.2f;
    [SerializeField] private float shadowMaxAlpha = 0.5f;
    [SerializeField] private float shadowMinScale = 0.8f;
    [SerializeField] private float shadowMaxScale = 1.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip rollSound;
    [SerializeField] private AudioClip bounceSound;
    private AudioSource audioSource;
    
    // State
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private int currentFaceIndex = 0;
    private bool isRolling = false;
    private Coroutine rollCoroutine;
    
    void Awake()
    {
        if (diceTransform == null) diceTransform = GetComponent<RectTransform>();
        if (diceImage == null) diceImage = GetComponent<Image>();
        
        originalPosition = diceTransform.anchoredPosition;
        originalScale = diceTransform.localScale;
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        
        // Create shadow if not assigned
        if (shadowImage == null)
        {
            CreateShadow();
        }
    }
    
    void CreateShadow()
    {
        GameObject shadowObj = new GameObject("DiceShadow");
        shadowObj.transform.SetParent(transform.parent, false);
        shadowObj.transform.SetSiblingIndex(transform.GetSiblingIndex()); // Behind dice
        
        shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.color = new Color(0, 0, 0, shadowMaxAlpha);
        shadowImage.raycastTarget = false;
        
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = diceTransform.anchorMin;
        shadowRect.anchorMax = diceTransform.anchorMax;
        shadowRect.pivot = diceTransform.pivot;
        shadowRect.sizeDelta = diceTransform.sizeDelta * shadowMaxScale;
        shadowRect.anchoredPosition = diceTransform.anchoredPosition + new Vector2(shadowMaxOffset, -shadowMaxOffset);
    }
    
    /// <summary>
    /// Starts the 3D dice roll animation and returns the final result (1-6)
    /// </summary>
    public void Roll(System.Action<int> onComplete)
    {
        if (isRolling) return;
        
        if (rollCoroutine != null) StopCoroutine(rollCoroutine);
        rollCoroutine = StartCoroutine(RollRoutine(onComplete));
    }
    
    /// <summary>
    /// Sets the dice to show a specific face without animation
    /// </summary>
    public void SetFace(int faceValue)
    {
        if (faceValue < 1 || faceValue > 6) return;
        if (diceFaces == null || diceFaces.Count < 6) return;
        
        currentFaceIndex = faceValue - 1;
        diceImage.sprite = diceFaces[currentFaceIndex];
    }
    
    IEnumerator RollRoutine(System.Action<int> onComplete)
    {
        isRolling = true;
        
        // Play roll sound
        if (rollSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(rollSound);
        }
        
        // Determine final result
        int finalResult = Random.Range(1, 7);
        
        float elapsed = 0f;
        float faceChangeInterval = 0.05f; // Start fast
        float lastFaceChange = 0f;
        
        // Random rotation direction
        float rotationDirectionX = Random.value > 0.5f ? 1f : -1f;
        float rotationDirectionY = Random.value > 0.5f ? 1f : -1f;
        float rotationDirectionZ = Random.value > 0.5f ? 1f : -1f;
        
        // Cumulative rotation for smooth spinning
        float totalRotationX = 0f;
        float totalRotationY = 0f;
        float totalRotationZ = 0f;
        
        while (elapsed < rollDuration)
        {
            float t = elapsed / rollDuration;
            
            // Ease out - starts fast, slows down
            float speedMultiplier = 1f - EaseOutQuart(t);
            
            // Rotation speed decreases over time
            float currentRotSpeed = maxRotationSpeed * speedMultiplier;
            
            // Accumulate rotation
            float deltaRot = currentRotSpeed * Time.deltaTime;
            totalRotationX += deltaRot * rotationDirectionX;
            totalRotationY += deltaRot * rotationDirectionY * 0.7f;
            totalRotationZ += deltaRot * rotationDirectionZ * 0.3f;
            
            // Apply 3D-like tilt based on rotation phase
            float tiltX = Mathf.Sin(totalRotationX * Mathf.Deg2Rad * 0.5f) * maxTiltAngle * speedMultiplier;
            float tiltY = Mathf.Sin(totalRotationY * Mathf.Deg2Rad * 0.5f) * maxTiltAngle * speedMultiplier;
            float tiltZ = Mathf.Sin(totalRotationZ * Mathf.Deg2Rad * 0.3f) * maxTiltAngle * 0.5f * speedMultiplier;
            
            diceTransform.localRotation = Quaternion.Euler(tiltX, tiltY, tiltZ);
            
            // Bounce effect (simulates dice hitting table)
            float bounceProgress = t * bounceCount * Mathf.PI * 2f;
            float bounceValue = Mathf.Abs(Mathf.Sin(bounceProgress)) * bounceHeight * (1f - t);
            Vector3 newPos = originalPosition + new Vector2(0, bounceValue);
            diceTransform.anchoredPosition = newPos;
            
            // Scale variation (perspective simulation)
            float scaleVariation = 1f + Mathf.Sin(totalRotationX * Mathf.Deg2Rad) * maxScaleVariation * speedMultiplier;
            diceTransform.localScale = originalScale * scaleVariation;
            
            // Update shadow
            UpdateShadow(bounceValue, speedMultiplier);
            
            // Change face sprite based on "rotation"
            if (elapsed - lastFaceChange > faceChangeInterval)
            {
                // As we slow down, make face changes slower
                faceChangeInterval = Mathf.Lerp(0.05f, 0.15f, t);
                
                // In the last 20% of animation, start showing the final face more often
                if (t > 0.8f && Random.value > 0.5f)
                {
                    currentFaceIndex = finalResult - 1;
                }
                else
                {
                    currentFaceIndex = Random.Range(0, 6);
                }
                
                if (diceFaces != null && diceFaces.Count >= 6)
                {
                    diceImage.sprite = diceFaces[currentFaceIndex];
                }
                
                lastFaceChange = elapsed;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Final settle animation
        yield return StartCoroutine(SettleAnimation(finalResult));
        
        isRolling = false;
        onComplete?.Invoke(finalResult);
    }
    
    IEnumerator SettleAnimation(int finalResult)
    {
        // Play bounce sound
        if (bounceSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bounceSound);
        }
        
        // Set final face
        currentFaceIndex = finalResult - 1;
        if (diceFaces != null && diceFaces.Count >= 6)
        {
            diceImage.sprite = diceFaces[currentFaceIndex];
        }
        
        // Small bounce settle
        float settleDuration = 0.3f;
        float elapsed = 0f;
        
        Quaternion startRot = diceTransform.localRotation;
        Vector2 startPos = diceTransform.anchoredPosition;
        Vector3 startScale = diceTransform.localScale;
        
        while (elapsed < settleDuration)
        {
            float t = elapsed / settleDuration;
            float easeT = EaseOutBounce(t);
            
            // Settle rotation to identity
            diceTransform.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, easeT);
            
            // Settle position
            diceTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, easeT);
            
            // Settle scale
            diceTransform.localScale = Vector3.Lerp(startScale, originalScale, easeT);
            
            // Settle shadow
            UpdateShadow(Mathf.Lerp(10f, 0f, easeT), 1f - t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state
        diceTransform.localRotation = Quaternion.identity;
        diceTransform.anchoredPosition = originalPosition;
        diceTransform.localScale = originalScale;
        UpdateShadow(0f, 0f);
    }
    
    void UpdateShadow(float height, float intensity)
    {
        if (shadowImage == null) return;
        
        RectTransform shadowRect = shadowImage.GetComponent<RectTransform>();
        
        // Shadow offset increases with height
        float normalizedHeight = height / bounceHeight;
        float offsetX = shadowMaxOffset * (0.3f + normalizedHeight * 0.7f);
        float offsetY = -shadowMaxOffset * (0.3f + normalizedHeight * 0.7f);
        
        shadowRect.anchoredPosition = diceTransform.anchoredPosition + new Vector2(offsetX, offsetY);
        
        // Shadow fades and grows with height
        float alpha = Mathf.Lerp(shadowMaxAlpha, shadowMinAlpha, normalizedHeight);
        shadowImage.color = new Color(0, 0, 0, alpha * (0.5f + intensity * 0.5f));
        
        float scale = Mathf.Lerp(shadowMinScale, shadowMaxScale, normalizedHeight);
        shadowRect.localScale = Vector3.one * scale;
    }
    
    // Easing functions
    float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }
    
    float EaseOutBounce(float t)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;
        
        if (t < 1f / d1)
            return n1 * t * t;
        else if (t < 2f / d1)
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        else if (t < 2.5f / d1)
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        else
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }
    
    /// <summary>
    /// Resets the dice to its original state
    /// </summary>
    public void ResetDice()
    {
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
        }
        
        isRolling = false;
        diceTransform.localRotation = Quaternion.identity;
        diceTransform.anchoredPosition = originalPosition;
        diceTransform.localScale = originalScale;
        
        if (shadowImage != null)
        {
            shadowImage.color = new Color(0, 0, 0, 0);
        }
    }
    
    public bool IsRolling => isRolling;
}
