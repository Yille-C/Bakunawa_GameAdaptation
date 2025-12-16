using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Creates a glowing edge particle effect to indicate whose turn it is.
/// Attach this to a manager object and assign the TribeSide and BakunawaSide GameObjects.
/// </summary>
public class TurnIndicatorEffect : MonoBehaviour
{
    public static TurnIndicatorEffect Instance;

    [Header("Side References")]
    [Tooltip("The GameObject representing the Tribe's side of the board")]
    public GameObject tribeSide;
    [Tooltip("The GameObject representing the Bakunawa's side of the board")]
    public GameObject bakunawaSide;

    [Header("Bakunawa Colors (Blue Theme)")]
    [SerializeField] private Color bakunawaGlowColor = new Color(0.1f, 0.5f, 1f, 1f);
    [SerializeField] private Color bakunawaSecondaryColor = new Color(0.2f, 0.7f, 1f, 1f);

    [Header("Tribe Colors (Brown/Red Theme)")]
    [SerializeField] private Color tribeGlowColor = new Color(0.8f, 0.3f, 0.1f, 1f);
    [SerializeField] private Color tribeSecondaryColor = new Color(1f, 0.5f, 0.2f, 1f);

    [Header("Effect Settings")]
    [SerializeField] private float edgeWidth = 0.02f;
    [SerializeField] private float glowFalloff = 0.05f;
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float flowSpeed = 0.8f;
    [SerializeField] private float particleCount = 8f;
    [SerializeField] private float padding = 8f;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 4f;

    // Internal references
    private GameObject tribeGlowObject;
    private GameObject bakunawaGlowObject;
    private Image tribeGlowImage;
    private Image bakunawaGlowImage;
    private Material tribeMaterial;
    private Material bakunawaMaterial;

    // State
    private bool isTribeTurn = false;
    private bool isBakunawaTurn = false;
    private float tribeAlpha = 0f;
    private float bakunawaAlpha = 0f;

    // Shader property IDs
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowColor2ID = Shader.PropertyToID("_GlowColor2");
    private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");
    private static readonly int GlowFalloffID = Shader.PropertyToID("_GlowFalloff");
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
    private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
    private static readonly int FlowSpeedID = Shader.PropertyToID("_FlowSpeed");
    private static readonly int ParticleCountID = Shader.PropertyToID("_ParticleCount");
    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CreateGlowOverlays();
    }

    private void CreateGlowOverlays()
    {
        Shader glowShader = Shader.Find("UI/TurnIndicatorGlow");
        if (glowShader == null)
        {
            Debug.LogError("TurnIndicatorEffect: Could not find 'UI/TurnIndicatorGlow' shader!");
            return;
        }

        // Create Tribe glow
        if (tribeSide != null)
        {
            tribeGlowObject = CreateGlowForSide(tribeSide, "TribeGlow", out tribeGlowImage, out tribeMaterial, glowShader);
            ApplyMaterialSettings(tribeMaterial, tribeGlowColor, tribeSecondaryColor);
            if (tribeGlowObject != null) tribeGlowObject.SetActive(false);
        }

        // Create Bakunawa glow
        if (bakunawaSide != null)
        {
            bakunawaGlowObject = CreateGlowForSide(bakunawaSide, "BakunawaGlow", out bakunawaGlowImage, out bakunawaMaterial, glowShader);
            ApplyMaterialSettings(bakunawaMaterial, bakunawaGlowColor, bakunawaSecondaryColor);
            if (bakunawaGlowObject != null) bakunawaGlowObject.SetActive(false);
        }

        Debug.Log("TurnIndicatorEffect: Glow overlays created successfully.");
    }

    private GameObject CreateGlowForSide(GameObject side, string name, out Image image, out Material mat, Shader shader)
    {
        image = null;
        mat = null;

        RectTransform sideRect = side.GetComponent<RectTransform>();
        if (sideRect == null)
        {
            Debug.LogWarning($"TurnIndicatorEffect: {side.name} does not have a RectTransform!");
            return null;
        }

        // Create glow object as a child
        GameObject glowObj = new GameObject(name);
        glowObj.transform.SetParent(side.transform, false);
        glowObj.transform.SetAsFirstSibling(); // Behind other content

        // Setup RectTransform to cover the entire side panel plus padding
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-padding, -padding);
        glowRect.offsetMax = new Vector2(padding, padding);

        // Add CanvasRenderer for UI rendering
        glowObj.AddComponent<CanvasRenderer>();

        // Add Image component
        image = glowObj.AddComponent<Image>();
        image.raycastTarget = false;

        // Create material instance
        mat = new Material(shader);
        mat.name = $"{name}_Material";
        image.material = mat;
        image.color = new Color(1, 1, 1, 0); // Start invisible

        return glowObj;
    }

    private void ApplyMaterialSettings(Material mat, Color primary, Color secondary)
    {
        if (mat == null) return;

        mat.SetColor(GlowColorID, primary);
        mat.SetColor(GlowColor2ID, secondary);
        mat.SetFloat(EdgeWidthID, edgeWidth);
        mat.SetFloat(GlowFalloffID, glowFalloff);
        mat.SetFloat(GlowIntensityID, glowIntensity);
        mat.SetFloat(PulseSpeedID, pulseSpeed);
        mat.SetFloat(FlowSpeedID, flowSpeed);
        mat.SetFloat(ParticleCountID, particleCount);
        mat.SetFloat(AlphaID, 1f);
    }

    private void Update()
    {
        // Animate tribe glow
        if (tribeGlowImage != null)
        {
            float targetAlpha = isTribeTurn ? 1f : 0f;
            tribeAlpha = Mathf.MoveTowards(tribeAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            tribeGlowImage.color = new Color(1, 1, 1, tribeAlpha);

            if (tribeAlpha > 0.01f && !tribeGlowObject.activeSelf)
                tribeGlowObject.SetActive(true);
            else if (tribeAlpha <= 0.01f && tribeGlowObject.activeSelf)
                tribeGlowObject.SetActive(false);
        }

        // Animate bakunawa glow
        if (bakunawaGlowImage != null)
        {
            float targetAlpha = isBakunawaTurn ? 1f : 0f;
            bakunawaAlpha = Mathf.MoveTowards(bakunawaAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            bakunawaGlowImage.color = new Color(1, 1, 1, bakunawaAlpha);

            if (bakunawaAlpha > 0.01f && !bakunawaGlowObject.activeSelf)
                bakunawaGlowObject.SetActive(true);
            else if (bakunawaAlpha <= 0.01f && bakunawaGlowObject.activeSelf)
                bakunawaGlowObject.SetActive(false);
        }
    }

    /// <summary>
    /// Activate the Tribe's turn indicator.
    /// </summary>
    public void SetTribeTurn()
    {
        isTribeTurn = true;
        isBakunawaTurn = false;
        
        if (tribeGlowObject != null) tribeGlowObject.SetActive(true);
        Debug.Log("TurnIndicatorEffect: Tribe's turn activated");
    }

    /// <summary>
    /// Activate the Bakunawa's turn indicator.
    /// </summary>
    public void SetBakunawaTurn()
    {
        isTribeTurn = false;
        isBakunawaTurn = true;
        
        if (bakunawaGlowObject != null) bakunawaGlowObject.SetActive(true);
        Debug.Log("TurnIndicatorEffect: Bakunawa's turn activated");
    }

    /// <summary>
    /// Deactivate both turn indicators (e.g., during planning phase or transitions).
    /// </summary>
    public void ClearTurnIndicators()
    {
        isTribeTurn = false;
        isBakunawaTurn = false;
        Debug.Log("TurnIndicatorEffect: Turn indicators cleared");
    }

    /// <summary>
    /// Set which side is currently active based on player turn order.
    /// </summary>
    /// <param name="isPlayerTurn">True if it's the player's (Tribe's) turn</param>
    public void SetActiveTurn(bool isPlayerTurn)
    {
        if (isPlayerTurn)
            SetTribeTurn();
        else
            SetBakunawaTurn();
    }

    /// <summary>
    /// Immediately show turn indicator without fade animation.
    /// </summary>
    public void SetTribeTurnImmediate()
    {
        isTribeTurn = true;
        isBakunawaTurn = false;
        tribeAlpha = 1f;
        bakunawaAlpha = 0f;

        if (tribeGlowImage != null) tribeGlowImage.color = new Color(1, 1, 1, 1);
        if (bakunawaGlowImage != null) bakunawaGlowImage.color = new Color(1, 1, 1, 0);
        if (tribeGlowObject != null) tribeGlowObject.SetActive(true);
        if (bakunawaGlowObject != null) bakunawaGlowObject.SetActive(false);
    }

    /// <summary>
    /// Immediately show bakunawa turn indicator without fade animation.
    /// </summary>
    public void SetBakunawaTurnImmediate()
    {
        isTribeTurn = false;
        isBakunawaTurn = true;
        tribeAlpha = 0f;
        bakunawaAlpha = 1f;

        if (tribeGlowImage != null) tribeGlowImage.color = new Color(1, 1, 1, 0);
        if (bakunawaGlowImage != null) bakunawaGlowImage.color = new Color(1, 1, 1, 1);
        if (tribeGlowObject != null) tribeGlowObject.SetActive(false);
        if (bakunawaGlowObject != null) bakunawaGlowObject.SetActive(true);
    }

    private void OnDestroy()
    {
        // Clean up materials
        if (tribeMaterial != null) Destroy(tribeMaterial);
        if (bakunawaMaterial != null) Destroy(bakunawaMaterial);
        
        // Clean up glow objects
        if (tribeGlowObject != null) Destroy(tribeGlowObject);
        if (bakunawaGlowObject != null) Destroy(bakunawaGlowObject);
    }
}
