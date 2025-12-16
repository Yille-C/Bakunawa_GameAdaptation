using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Creates a wandering firefly effect around a card.
/// Replacing the previous buff aura with a less distracting, organic nature-themed effect.
/// Features: Organic wandering movement, HDR colors for bloom, gentle pulsing.
/// </summary>
public class CardBuffParticleEffect : MonoBehaviour
{
    [Header("Firefly Settings")]
    [SerializeField] private float spawnInterval = 0.4f;
    [SerializeField] private float particleLifeTime = 3.0f;
    [SerializeField] private float baseSize = 18f;
    [SerializeField] private float wanderRadius = 65f;
    [SerializeField] private float moveSpeed = 40f;
    
    [Header("Colors (HDR for Bloom)")]
    // Bright Yellow-Green for Buff (Medium Intensity)
    [SerializeField] private Color buffColor = new Color(1.8f, 4.0f, 0.2f, 1f); 
    // Reddish-Orange for Debuff
    [SerializeField] private Color debuffColor = new Color(4.0f, 0.6f, 0.2f, 1f);
    
    private static Sprite dotSprite;
    private static Material particleMaterial;
    
    private bool isPlaying = false;
    private bool currentIsBuff = false; // Track which effect is playing
    private GameObject effectContainer;
    private Coroutine loopCoroutine;
    private List<Coroutine> activeParticles = new List<Coroutine>();
    
    private void Awake()
    {
        InitializeResources();
    }
    
    private void InitializeResources()
    {
        if (dotSprite == null) dotSprite = CreateDotSprite();
        
        // Use Sprites/Default which supports HDR colors nicely for UI particles
        if (particleMaterial == null) 
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                particleMaterial = new Material(shader);
        }
    }
    
    // ==================== PUBLIC API ====================
    
    public void PlayBuffEffect() => StartEffect(true);
    public void PlayDebuffEffect() => StartEffect(false);
    public void StopEffect() => StopAllEffects();
    
    public static void PlayBuffEffectAt(Transform target, bool isBuff = true)
    {
        // Debug.Log($"[CardBuffParticleEffect] Playing Firefly {(isBuff ? "BUFF" : "DEBUFF")} effect on {target.name}");
        
        CardBuffParticleEffect effect = target.GetComponent<CardBuffParticleEffect>();
        if (effect == null)
        {
            effect = target.gameObject.AddComponent<CardBuffParticleEffect>();
        }
        
        if (effect.isPlaying && effect.currentIsBuff == isBuff)
        {
             return; // Already playing correctly
        }
        
        effect.StopEffect();
        
        if (isBuff)
            effect.PlayBuffEffect();
        else
            effect.PlayDebuffEffect();
    }
    
    public static void StopEffectAt(Transform target)
    {
        CardBuffParticleEffect effect = target.GetComponent<CardBuffParticleEffect>();
        if (effect != null) effect.StopEffect();
    }
    
    // ==================== EFFECT CONTROL ====================
    
    private void StartEffect(bool isBuff)
    {
        if (isPlaying) return;
        
        InitializeResources();
        isPlaying = true;
        currentIsBuff = isBuff;
        
        CreateEffectContainer();
        
        loopCoroutine = StartCoroutine(ParticleLoop(isBuff));
    }
    
    private void StopAllEffects()
    {
        isPlaying = false;
        
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
        
        foreach (var routine in activeParticles)
        {
            if (routine != null) StopCoroutine(routine);
        }
        activeParticles.Clear();
        
        if (effectContainer != null)
        {
            Destroy(effectContainer);
            effectContainer = null;
        }
    }
    
    private void CreateEffectContainer()
    {
        effectContainer = new GameObject("FireflyEffect");
        
        // Parent directly to the card. 
        // This ensures it renders ON TOP of the card (child order) 
        // but BELOW high-level Global UI (like Banners) because the Card itself is lower.
        effectContainer.transform.SetParent(transform, false);
        
        RectTransform rt = effectContainer.AddComponent<RectTransform>();
        rt.localPosition = Vector3.zero;
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        
        // No need to set sibling index manually; as a new child it renders last (on top of card base).
    }
    
    // ==================== MAIN LOOP ====================
    
    private IEnumerator ParticleLoop(bool isBuff)
    {
        Color color = isBuff ? buffColor : debuffColor;
        
        // Initial spawn (burst a few so we don't wait for interval)
        for (int i=0; i<3; i++)
        {
            SpawnFirefly(color, true); 
        }
        
        while (isPlaying)
        {
            SpawnFirefly(color, false);
            
            // Randomize interval slightly
            yield return new WaitForSeconds(spawnInterval * Random.Range(0.8f, 1.2f));
        }
    }
    
    private void SpawnFirefly(Color color, bool startAtRandomTime)
    {
        GameObject p = CreateParticle(baseSize, color);
        if (p != null) 
        {
            float lifetime = particleLifeTime * Random.Range(0.8f, 1.2f);
            activeParticles.Add(StartCoroutine(AnimateFirefly(p, lifetime, startAtRandomTime)));
        }
    }

    // ==================== FIREFLY ANIMATION ====================
    
    private IEnumerator AnimateFirefly(GameObject particle, float lifetime, bool startRandom)
    {
        RectTransform rt = particle.GetComponent<RectTransform>();
        Image img = particle.GetComponent<Image>();
        Color baseColor = img.color;
        
        // Movement parameters
        float angle = Random.Range(0f, 360f);
        // Start from random radius or near center?
        // Fireflies usually hover around. Let's start them at random polar coord.
        float radius = Random.Range(wanderRadius * 0.4f, wanderRadius);
        
        // Each firefly has a unique noise offset
        float noiseOffset = Random.Range(0f, 1000f);
        float noiseFreq = Random.Range(0.5f, 1.5f);
        
        // Angular velocity
        float angularSpeed = (Random.value > 0.5f ? 1f : -1f) * moveSpeed * Random.Range(0.8f, 1.5f);
        
        float timer = startRandom ? Random.Range(0f, lifetime * 0.5f) : 0f;
        
        while (timer < lifetime && particle != null)
        {
            timer += Time.deltaTime;
            float t = timer / lifetime; // 0 to 1
            
            // circular orbit + noise
            angle += angularSpeed * Time.deltaTime;
            float radAngle = angle * Mathf.Deg2Rad;
            
            // Noise modifies radius (in and out)
            float rNoise = Mathf.PerlinNoise(timer * noiseFreq, noiseOffset); 
            float currentRadius = radius + (rNoise - 0.5f) * (wanderRadius * 0.6f);
            
            float x = Mathf.Cos(radAngle) * currentRadius;
            float y = Mathf.Sin(radAngle) * currentRadius;
            
            // Add a little vertical float noise (y-only offset)
            float floatY = (Mathf.PerlinNoise(noiseOffset, timer * 2f) - 0.5f) * 20f;
            
            rt.localPosition = new Vector3(x, y + floatY, 0);
            
            // FADING (Fade In / Fade Out)
            float alpha = 1f;
            if (t < 0.2f) alpha = Mathf.SmoothStep(0f, 1f, t / 0.2f);
            else if (t > 0.7f) alpha = Mathf.SmoothStep(1f, 0f, (t - 0.7f) / 0.3f);
            
            // PULSING (Glow intensity)
            // Pulse bloom intensity
            float pulse = 1f + 0.4f * Mathf.Sin(timer * 5f + noiseOffset);
            
            // Update Visuals
            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha); // Alpha on color structure
            
            // For bloom to pulse, we might need to adjust the RGB values themselves if using HDR
            // Or just scale the object slightly
            rt.localScale = Vector3.one * pulse;
            
            yield return null;
        }
        
        if (particle != null) Destroy(particle);
    }
    
    // ==================== HELPERS ====================
    
    private GameObject CreateParticle(float size, Color color)
    {
        if (effectContainer == null) return null;
        
        GameObject particle = new GameObject("Firefly");
        particle.transform.SetParent(effectContainer.transform, false);
        
        RectTransform rt = particle.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        
        Image img = particle.AddComponent<Image>();
        img.sprite = dotSprite;
        img.color = color; // HDR Color passed here
        if (particleMaterial != null) img.material = particleMaterial;
        img.raycastTarget = false;
        
        return particle;
    }
    
    private Sprite CreateDotSprite()
    {
        // Simple soft circle
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size * 0.45f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx*dx + dy*dy);
                
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = Mathf.Pow(alpha, 2f); // sharpen falloff slightly
                
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f, 0.5f));
    }
    
    private void OnDisable() => StopAllEffects();
    private void OnDestroy() => StopAllEffects();
}
