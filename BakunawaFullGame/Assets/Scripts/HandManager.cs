using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Image gameOverImage;
    public Sprite victorySprite;
    public Sprite defeatSprite;

    [Header("Extra Game Over Icons")]
    public Image extraIconDisplay;
    public Sprite bakunawaIconSprite;
    public Sprite tribesmenIconSprite;

    public Text winnerText;
    public Button restartButton;
    public Button mainMenuButton;
    private bool isGameOver = false;

    [Header("Discard Notification UI")]
    public GameObject discardNotifyPanel;
    public Image discardNotifyImage;
    public Text discardNotifyText;
    public float discardNotifyDuration = 3.0f;

    [Header("Agong Retrieval UI")]
    public GameObject agongPanel;
    public Image agongCardImage;
    public Text agongCardName;
    public float agongDuration = 3.0f;

    [Header("Alay to Bathala Choice UI")]
    public GameObject alayChoicePanel;
    public Button alayBuffButton;
    public Button alayDebuffButton;
    private bool alayChoiceMade = false;

    [Header("Dice System")]
    public GameObject dicePanel;
    public Image playerDiceImg;
    public Image enemyDiceImg;
    public Button rollButton;
    public List<Sprite> diceSprites;

    [Header("Turn Choice UI")]
    public GameObject turnChoicePanel;
    public Button goFirstButton;
    public Button goSecondButton;

    [Header("Round Result UI")]
    public GameObject resultBannerObject;
    public Image bannerDisplayImage;
    public Sprite tribesmenWinSprite;
    public Sprite bakunawaWinSprite;
    public Text fallbackText;
    public float resultDuration = 2.0f;

    [Header("Planning Banner & Round Info")]
    public GameObject planningBanner;
    public Text planningBannerText;
    public float planningBannerDuration = 2.0f;
    public Text roundCounterText;

    [Header("Combat Banner")]
    public GameObject combatBanner;
    public Text combatBannerText;
    public float bannerDuration = 2.0f;

    [Header("Energy System")]
    public Slider energySlider;
    public Text energyText;
    public Text warningText;
    public int maxEnergy = 10;

    [Header("Areas")]
    public GameObject cardPrefab;
    public Transform handArea;
    [UnityEngine.Serialization.FormerlySerializedAs("lockedHandArea")]
    public Transform tribeSelectedPanel;
    public Transform tribeLockedPanel;
    public Transform deckPileArea;
    public Transform battleZone;
    public Transform discardPileArea;

    [Header("UI Controls")]
    public Button lockInButton;
    public Button playCardButton;
    public Text timerText;

    [Header("Settings")]
    public float playCardScale = 1.2f;
    public float lockedScale = 0.6f; // Scale for cards in TribeLocked panel
    public float discardScale = 0.8f;
    public float planningTime = 60f;
    public float tribePanelSpacing = -90f; // Control spacing in the inspector
    public float clashDuration = 0.5f;

    [Header("Details UI")]
    public GameObject detailsPanel;
    public Text detailName;
    public Text detailDesc;
    // --- NEW DETAILS ---
    public Image detailImage;
    public Text detailCost;
    public Text detailAttack;
    // -------------------

    [Header("Hand Pagination")]
    public int cardsPerPage = 5;
    public Button prevPageBtn;
    public Button nextPageBtn;
    public Text pageIndicatorText;
    private int currentPage = 0;

    [Header("Data")]
    public List<CardData> myDeck;

    private List<CardUI> selectedCardsUI = new List<CardUI>();

    public bool isPlanningPhase = true;
    public bool IsInputLocked => inputLocked;
    private bool inputLocked = true;
    private float currentTimer;
    private CardUI currentBattleSelection;

    // GAME STATE
    public int roundNumber = 1;
    private bool playerGoesFirst = true;
    private bool enemyHasPlayedPendingCard = false;
    private CardUI pendingEnemyCard = null;

    // FLAGS
    public bool alayBuffActive = false;
    public bool alayDebuffActive = false;
    public bool agongPlayedThisRound = false;

    private Image clashDimmer;

    // ... [Existing Awake] ...
    // Helper for animation timing
    float shakingTimeNorm(float ct, float dur)
    {
        float t = ct / dur;
        return Mathf.Clamp01(t);
    }
    
    void Awake()
    {
        Instance = this;
        EnsureDimmer();
    }

    void EnsureDimmer()
    {
        if (clashDimmer != null) return;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        
        if (rootCanvas != null)
        {
            GameObject dimObj = new GameObject("ClashDimmer");
            dimObj.transform.SetParent(rootCanvas.transform, false);
            dimObj.transform.SetAsFirstSibling(); // Put it behind most things, but we'll control draw order via card reparenting
            
            clashDimmer = dimObj.AddComponent<Image>();
            clashDimmer.color = new Color(0, 0, 0, 0f); // Start transparent
            clashDimmer.raycastTarget = false;
            
            // Stretch
            RectTransform rt = dimObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    IEnumerator FadeDimmer(bool fadeIn, float duration = 0.3f)
    {
        if (clashDimmer == null) EnsureDimmer();
        if (clashDimmer == null) yield break;

        // Ensure dimmer is just behind the front-most elements (like our clashing cards)
        // Since cards are moved to LastSibling of Root, the dimmer needs to be SecondToLast? 
        // Or just let cards pop over it. 
        // We'll set dimmer to LastSibling first, then when cards move to root they will become LastSibling (on top of dimmer).
        if (fadeIn) clashDimmer.transform.SetAsLastSibling();

        float startAlpha = clashDimmer.color.a;
        float targetAlpha = fadeIn ? 0.75f : 0f;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            float t = elapsed / duration;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            clashDimmer.color = new Color(0,0,0, a);
            elapsed += Time.deltaTime;
            yield return null;
        }
        clashDimmer.color = new Color(0,0,0, targetAlpha);
    }

    // [Enhanced] Multi-Phase Card Clash Animation - V2 (Root Canvas Detachment)
    IEnumerator AnimateCardClash(CardUI playerCard, CardUI enemyCard)
    {
        // === PHASE 0: SETUP & DETACHMENT ===
        // Dim the background
        StartCoroutine(FadeDimmer(true, 0.4f));

        // 1. Capture Original Context
        Transform pOriginalParent = playerCard.transform.parent;
        Transform eOriginalParent = (enemyCard != null) ? enemyCard.transform.parent : null;
        
        // 2. Determine "Slot" Position in BattleZone (Where they should end up)
        // We do this by temporarily parenting them to battleZone (if not already) and forcing a layout calc,
        // OR we can just simple-math it if the layout is predictable.
        // For reliability, let's assume specific "Left" and "Right" slots in the battle zone for visual clarity,
        // or just let them return to the battleZone container at the end.
        
        // For the animations, we want to work in SCREEN SPACE / ROOT CANVAS SPACE to avoid layout fighting.
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        Transform rootT = rootCanvas.transform;

        // Capture Start Positions (World Space)
        Vector3 pStartPos = playerCard.transform.position;
        Vector3 eStartPos = (enemyCard != null) ? enemyCard.transform.position : Vector3.zero;

        // 3. Move to Root - This "frees" them from the LayoutGroup
        playerCard.transform.SetParent(rootT, true); // worldPositionStays = true
        if (enemyCard != null) enemyCard.transform.SetParent(rootT, true);

        // Ensure they render on top
        playerCard.transform.SetAsLastSibling();
        if (enemyCard != null) enemyCard.transform.SetAsLastSibling();

        // Standardize Scale
        playerCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        if (enemyCard != null) enemyCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);

        // Define Key Positions relative to Screen Center
        // We use the Root Canvas as the visual center anchor for the CLASH (Screen Center)
        Vector3 clashPoint = rootT.position; // Screen Center
        Vector3 centerPoint = battleZone.position; // Return target reference

        float verticalOffset = 350f; // Increase start distance for more drama
        
        Vector3 pReadyPos = clashPoint + new Vector3(0, -verticalOffset, 0);
        Vector3 eReadyPos = clashPoint + new Vector3(0, verticalOffset, 0); // Enemy comes from top
        if (enemyCard == null) eReadyPos = clashPoint; // Dummy target center

        // === PHASE 1: WINDUP / ALIGN (0.4s) ===
        // Move from wherever they are (Hand/Deck) to the "Ready" positions
        float windupTime = 0.4f;
        float elapsed = 0f;
        
        Quaternion pStartRot = playerCard.transform.rotation;
        Quaternion eStartRot = (enemyCard != null) ? enemyCard.transform.rotation : Quaternion.identity;

        // Scale Up Logic
        Vector3 normalScale = new Vector3(playCardScale, playCardScale, playCardScale);
        Vector3 clashScaleVec = normalScale * 1.3f; // 30% larger for impact

        while (elapsed < windupTime)
        {
            float t = elapsed / windupTime;
            t = t * t * (3f - 2f * t); // SmoothStep

            playerCard.transform.position = Vector3.Lerp(pStartPos, pReadyPos, t);
            playerCard.transform.rotation = Quaternion.Lerp(pStartRot, Quaternion.identity, t);
            playerCard.transform.localScale = Vector3.Lerp(normalScale, clashScaleVec, t);

            if (enemyCard != null)
            {
                enemyCard.transform.position = Vector3.Lerp(eStartPos, eReadyPos, t);
                enemyCard.transform.rotation = Quaternion.Lerp(eStartRot, Quaternion.identity, t);
                enemyCard.transform.localScale = Vector3.Lerp(normalScale, clashScaleVec, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCard.transform.localScale = clashScaleVec;
        if (enemyCard != null) enemyCard.transform.localScale = clashScaleVec;

        // === PHASE 2: ANTICIPATION (0.15s) ===
        // Brief pause/pull back before strike
        yield return new WaitForSeconds(0.1f);
        
        // === PHASE 3: LUNGE (0.15s) ===
        // Smash together!
        float lungeTime = 0.15f;
        elapsed = 0f;
        
        // Remove Tilt as requested - keep them straight
        Quaternion pTilt = Quaternion.identity; 
        Quaternion eTilt = Quaternion.identity;

        // Dynamic Height Calculation to prevent overlap
        float actualHeight = 250f; // Default fallback
        RectTransform pRect = playerCard.GetComponent<RectTransform>();
        if (pRect != null) actualHeight = pRect.rect.height * clashScaleVec.y;

        float cardHalfHeight = actualHeight / 2f;
        
        float collisionGap = 0f; 
        float offset = cardHalfHeight + collisionGap;

        Vector3 pImpactPos = clashPoint + new Vector3(0, -offset, 0);
        Vector3 eImpactPos = clashPoint + new Vector3(0, offset, 0);

        // STRETCH VECTORS (Elongate on Y, thin on X)
        // Apply relative to the current clashScaleVec
        Vector3 stretchScale = new Vector3(clashScaleVec.x * 0.8f, clashScaleVec.y * 1.2f, clashScaleVec.z);

        while (elapsed < lungeTime)
        {
            float t = elapsed / lungeTime;
            t = t * t * t; // Cubic Ease In (Exciting!)

            playerCard.transform.position = Vector3.Lerp(pReadyPos, pImpactPos, t);
            
            // Apply STRETCH as velocity increases
            // Max stretch at t=1
            playerCard.transform.localScale = Vector3.Lerp(clashScaleVec, stretchScale, t);

            if (enemyCard != null)
            {
                enemyCard.transform.position = Vector3.Lerp(eReadyPos, eImpactPos, t);
                enemyCard.transform.localScale = Vector3.Lerp(clashScaleVec, stretchScale, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final collision position
        playerCard.transform.position = pImpactPos;
        if (enemyCard != null) enemyCard.transform.position = eImpactPos;


        // === PHASE 4: IMPACT (One Frame + Shake) ===
        // Visuals
        playerCard.SetBroken(true);
        if (enemyCard != null) enemyCard.SetBroken(true);

        // Screen Shake & IMPACT PUNCH
        Vector3 camOriginalPos = Camera.main.transform.position;
        float shakeDuration = 0.25f; // Slightly longer
        float shakeMagnitude = 15f; // Impactful jitter
        float shakeTimer = 0f;
        
        // SPAWN SPARKS
        CreateImpactSparks(clashPoint);

        // Start Recoil concurrently with shake
        float recoilTime = 0.3f;
        Vector3 pRecoilPos = pImpactPos + new Vector3(0, -50f, 0);
        Vector3 eRecoilPos = eImpactPos + new Vector3(0, 50f, 0);

        // SQUASH TARGET (Flatten on Y, widen on X)
        // This replaces the simple scale punch. We squash HARD then spring back.
        Vector3 squashScale = new Vector3(clashScaleVec.x * 1.3f, clashScaleVec.y * 0.7f, clashScaleVec.z);
        Vector3 overshootScale = new Vector3(clashScaleVec.x * 0.9f, clashScaleVec.y * 1.1f, clashScaleVec.z); // Spring effect

        while (shakeTimer < recoilTime) // Loop for length of recoil
        {
            float t = shakeTimer / recoilTime;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease Out Recoil

            // Baset Recoil Position
            Vector3 currentRecoilP = Vector3.Lerp(pImpactPos, pRecoilPos, t);
            Vector3 currentRecoilE = Vector3.Lerp(eImpactPos, eRecoilPos, t);

            // ADD VIOLENT SHAKE (Jitter)
            if (shakeTimer < shakeDuration)
            {
               float strength = 1f - (shakeTimer / shakeDuration);
               Vector3 cardJitter = (Vector3)(Random.insideUnitCircle * shakeMagnitude * strength);
               
               playerCard.transform.position = currentRecoilP + cardJitter;
               if (enemyCard != null) enemyCard.transform.position = currentRecoilE + cardJitter;

               // SQUASH AND STRETCH DECAY LOGIC
               // 0.0 -> 0.1 : Squash
               // 0.1 -> 0.3 : Overshoot (Stretch)
               // 0.3 -> 1.0 : Return to Normal
               
               float scalePhase = shakingTimeNorm(shakeTimer, 0.25f); // localized t
               Vector3 currentScale = clashScaleVec;

               if (scalePhase < 0.3f)
               {
                   currentScale = Vector3.Lerp(squashScale, overshootScale, scalePhase / 0.3f);
               }
               else
               {
                   currentScale = Vector3.Lerp(overshootScale, clashScaleVec, (scalePhase - 0.3f) / 0.7f);
               }

               playerCard.transform.localScale = currentScale;
               if (enemyCard != null) enemyCard.transform.localScale = currentScale;
            }
            else 
            {
               playerCard.transform.position = currentRecoilP;
               if (enemyCard != null) enemyCard.transform.position = currentRecoilE;
               
               playerCard.transform.localScale = clashScaleVec;
               if (enemyCard != null) enemyCard.transform.localScale = clashScaleVec;
            }

            shakeTimer += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.position = camOriginalPos; // Ensure reset

        // === PHASE 5: RETURN TO BOARD (0.4s) ===
        // Undim background
        StartCoroutine(FadeDimmer(false, 0.4f));

        // Return cards to their specific zones (Player Zone / Enemy Zone)
        
        if (pOriginalParent != null) playerCard.transform.SetParent(pOriginalParent, true);
        else playerCard.transform.SetParent(battleZone, true); // Fallback

        if (enemyCard != null)
        {
            if (eOriginalParent != null) enemyCard.transform.SetParent(eOriginalParent, true);
            else enemyCard.transform.SetParent(battleZone, true); // Fallback
        }

        // We need to know where the LayoutGroup *validly* wants them.
        
        LayoutElement pLe = playerCard.GetComponent<LayoutElement>();
        if (pLe == null) pLe = playerCard.gameObject.AddComponent<LayoutElement>();
        
        LayoutElement eLe = (enemyCard != null) ? enemyCard.GetComponent<LayoutElement>() : null;

        // Enable layout momentarily to calculate slot
        pLe.ignoreLayout = false;
        if (eLe != null) eLe.ignoreLayout = false;
        
        // Force Rebuild on correct parents
        if (playerCard.transform.parent != null) 
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerCard.transform.parent as RectTransform);
            
        if (enemyCard != null && enemyCard.transform.parent != null && enemyCard.transform.parent != playerCard.transform.parent)
             LayoutRebuilder.ForceRebuildLayoutImmediate(enemyCard.transform.parent as RectTransform);
        
        // Capture Target Positions
        Vector3 pFinalPos = playerCard.transform.position;
        Vector3 eFinalPos = (enemyCard != null) ? enemyCard.transform.position : Vector3.zero;

        // Reset to Recoil pos to start return flight
        // Requires disabling layout again so we can move them freely
        pLe.ignoreLayout = true;
        if (eLe != null) eLe.ignoreLayout = true;
        
        playerCard.transform.position = pRecoilPos;
        if (enemyCard != null) enemyCard.transform.position = eRecoilPos;
        
        // === ANIMATE TO PILE ===
        // Skip the old "return to board" logic - go directly to pile animation
        // Instead of relying on HorizontalLayoutGroup (which causes overflow),
        // we manually stack clashed cards on the LEFT/RIGHT side of each zone.
        
        // PLAYER CARD PILE - Calculate target
        pLe.ignoreLayout = true;
        int playerPileIndex = 0;
        Transform pZone = pOriginalParent != null ? pOriginalParent : battleZone;
        foreach(Transform child in pZone)
        {
            if (child == playerCard.transform) break;
            CardUI otherCard = child.GetComponent<CardUI>();
            if (otherCard != null && !otherCard.isEnemy) playerPileIndex++;
        }

        RectTransform pZoneRect = pZone as RectTransform;
        float pileOffsetX = 30f;
        float pPileBaseX = -pZoneRect.rect.width / 2f + 100f;
        Vector3 pPileTargetLocal = new Vector3(pPileBaseX + playerPileIndex * pileOffsetX, 0, 0);
        Quaternion pPileTargetRot = Quaternion.Euler(0, 0, Random.Range(-3f, 3f));
        
        // ENEMY CARD PILE - Calculate target
        Vector3 ePileTargetLocal = Vector3.zero;
        Quaternion ePileTargetRot = Quaternion.identity;
        Transform eZone = null;
        
        if (enemyCard != null)
        {
            eLe.ignoreLayout = true;
            int enemyPileIndex = 0;
            eZone = eOriginalParent != null ? eOriginalParent : battleZone;
            foreach(Transform child in eZone)
            {
                if (child == enemyCard.transform) break;
                CardUI otherCard = child.GetComponent<CardUI>();
                if (otherCard != null && otherCard.isEnemy) enemyPileIndex++;
            }

            RectTransform eZoneRect = eZone as RectTransform;
            float ePileBaseX = eZoneRect.rect.width / 2f - 100f;
            ePileTargetLocal = new Vector3(ePileBaseX - enemyPileIndex * pileOffsetX, 0, 0);
            ePileTargetRot = Quaternion.Euler(0, 0, Random.Range(-3f, 3f));
        }

        // Reparent NOW (to prepare for local position animation)
        playerCard.transform.SetParent(pZone, true); // Keep world position
        playerCard.transform.SetAsLastSibling();
        Vector3 pPileStartLocal = playerCard.transform.localPosition;
        Quaternion pPileStartRot = playerCard.transform.localRotation;
        
        Vector3 ePileStartLocal = Vector3.zero;
        Quaternion ePileStartRot = Quaternion.identity;
        if (enemyCard != null)
        {
            enemyCard.transform.SetParent(eZone, true);
            enemyCard.transform.SetAsLastSibling();
            ePileStartLocal = enemyCard.transform.localPosition;
            ePileStartRot = enemyCard.transform.localRotation;
        }

        // === ANIMATE TO PILE (0.35s) ===
        float pileTime = 0.35f;
        elapsed = 0f;
        while(elapsed < pileTime)
        {
            float t = elapsed / pileTime;
            t = t * t * (3f - 2f * t); // SmoothStep
            
            playerCard.transform.localPosition = Vector3.Lerp(pPileStartLocal, pPileTargetLocal, t);
            playerCard.transform.localRotation = Quaternion.Lerp(pPileStartRot, pPileTargetRot, t);
            playerCard.transform.localScale = Vector3.Lerp(clashScaleVec, normalScale, t);
            
            if (enemyCard != null)
            {
                enemyCard.transform.localPosition = Vector3.Lerp(ePileStartLocal, ePileTargetLocal, t);
                enemyCard.transform.localRotation = Quaternion.Lerp(ePileStartRot, ePileTargetRot, t);
                enemyCard.transform.localScale = Vector3.Lerp(clashScaleVec, normalScale, t);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Final snap
        playerCard.transform.localPosition = pPileTargetLocal;
        playerCard.transform.localRotation = pPileTargetRot;
        playerCard.transform.localScale = normalScale;
        
        if (enemyCard != null)
        {
            enemyCard.transform.localPosition = ePileTargetLocal;
            enemyCard.transform.localRotation = ePileTargetRot;
            enemyCard.transform.localScale = normalScale;
        }
    }

    void Start()
    {
        // Override Inspector value to ensure correct spacing
        tribePanelSpacing = -90f;

        // UI Cleanup
        detailsPanel.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);
        if (combatBanner != null) combatBanner.SetActive(false);
        if (resultBannerObject != null) resultBannerObject.SetActive(false);
        if (dicePanel != null) dicePanel.SetActive(false);
        if (turnChoicePanel != null) turnChoicePanel.SetActive(false);
        if (planningBanner != null) planningBanner.SetActive(false);
        if (alayChoicePanel != null) alayChoicePanel.SetActive(false);
        if (agongPanel != null) agongPanel.SetActive(false);
        if (discardNotifyPanel != null) discardNotifyPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Button Listeners
        lockInButton.onClick.AddListener(OnLockInPressed);
        playCardButton.onClick.AddListener(OnPlayButtonPressed);

        if (rollButton != null) rollButton.onClick.AddListener(OnRollDicePressed);
        if (goFirstButton != null) goFirstButton.onClick.AddListener(() => FinalizeTurnOrder(true));
        if (goSecondButton != null) goSecondButton.onClick.AddListener(() => FinalizeTurnOrder(false));

        if (alayBuffButton != null) alayBuffButton.onClick.AddListener(() => ResolveAlayChoice(true));
        if (alayDebuffButton != null) alayDebuffButton.onClick.AddListener(() => ResolveAlayChoice(false));

        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (prevPageBtn != null) 
        {
            prevPageBtn.onClick.AddListener(PrevHandPage);
            EnsureButtonAnimation(prevPageBtn);
        }
        if (nextPageBtn != null) 
        {
            nextPageBtn.onClick.AddListener(NextHandPage);
            EnsureButtonAnimation(nextPageBtn);
        }

        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);

        roundNumber = 1;
        UpdateRoundUI();

        if (tribeSelectedPanel != null)
        {
            HorizontalLayoutGroup hlg = tribeSelectedPanel.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = tribeSelectedPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            
            // Always apply settings to ensure spacing is correct
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = tribePanelSpacing; 
        }

        if (tribeLockedPanel != null)
        {
            HorizontalLayoutGroup hlg = tribeLockedPanel.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = tribeLockedPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = tribePanelSpacing; 
        }

        if (battleZone != null)
        {
            HorizontalLayoutGroup hlg = battleZone.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = battleZone.gameObject.AddComponent<HorizontalLayoutGroup>();
            
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(50, 0, 0, 0);
            hlg.spacing = 20; // Nice gap for played cards
        }

        SpawnDeck();
        StartCoroutine(StartPlanningPhaseSequence());
    }

    void Update()
    {
        if (isGameOver) return;

        if (isPlanningPhase && !inputLocked)
        {
            currentTimer -= Time.deltaTime;

            if (timerText != null)
            {
                float displayTime = currentTimer < 0 ? 0 : currentTimer;
                int minutes = Mathf.FloorToInt(displayTime / 60F);
                int seconds = Mathf.FloorToInt(displayTime % 60F);
                timerText.text = string.Format("{0}:{1:00}", minutes, seconds);

                if (currentTimer <= 10f) timerText.color = Color.red;
                else timerText.color = Color.white;
            }

            if (currentTimer <= 0)
            {
                currentTimer = 0;
                OnLockInPressed();
            }
        }
    }

    void UpdateRoundUI()
    {
        if (roundCounterText != null) roundCounterText.text = roundNumber.ToString();
    }

    // --- DETAILS PANEL LOGIC ---
    public void ShowCardDetails(CardData data)
    {
        detailsPanel.SetActive(true);
        if (detailName != null) detailName.text = data.cardName;
        if (detailDesc != null) detailDesc.text = data.description;

        if (detailImage != null && data.cardArt != null) detailImage.sprite = data.cardArt;
        if (detailCost != null) detailCost.text = data.energyCost.ToString();
        if (detailAttack != null) detailAttack.text = data.attackValue.ToString();
    }

    public void HideCardDetails()
    {
        detailsPanel.SetActive(false);
    }
    // ---------------------------

    // --- GAME OVER LOGIC ---
    public void TriggerGameOver(string winner)
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (winnerText != null)
            {
                winnerText.text = winner == "Tribesmen" ? "VICTORY" : "DEFEAT";
                winnerText.color = winner == "Tribesmen" ? new Color(0f, 1f, 1f) : Color.red;
            }
            if (gameOverImage != null)
            {
                gameOverImage.color = Color.white;
                if (winner == "Tribesmen" && victorySprite != null) gameOverImage.sprite = victorySprite;
                else if (defeatSprite != null) gameOverImage.sprite = defeatSprite;
            }
            if (extraIconDisplay != null)
            {
                extraIconDisplay.gameObject.SetActive(true);
                if (winner == "Tribesmen")
                {
                    if (tribesmenIconSprite != null) extraIconDisplay.sprite = tribesmenIconSprite;
                    else extraIconDisplay.gameObject.SetActive(false);
                }
                else
                {
                    if (bakunawaIconSprite != null) extraIconDisplay.sprite = bakunawaIconSprite;
                    else extraIconDisplay.gameObject.SetActive(false);
                }
            }
        }
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // --- ALAY CHOICE LOGIC ---
    IEnumerator ShowAlayChoice()
    {
        if (alayChoicePanel != null)
        {
            alayChoicePanel.SetActive(true);
            alayChoiceMade = false;
            while (!alayChoiceMade) { yield return null; }
            alayChoicePanel.SetActive(false);
        }
    }

    void ResolveAlayChoice(bool isBuff)
    {
        if (isBuff) { alayBuffActive = true; alayDebuffActive = false; }
        else { alayBuffActive = false; alayDebuffActive = true; }
        alayChoiceMade = true;
    }

    public void SelectCardForBattle(CardUI card)
    {
        if (inputLocked || (playCardButton != null && !playCardButton.interactable)) return;

        if (currentBattleSelection != null)
        {
            // Disable glow on previously selected card
            if (currentBattleSelection.glowOverlay != null)
                currentBattleSelection.glowOverlay.SetGlowEnabled(false);
            // Also hide legacy border just in case
            if (currentBattleSelection.selectionBorder != null)
                currentBattleSelection.selectionBorder.SetActive(false);
        }
        currentBattleSelection = card;
        // Enable glow on newly selected card
        if (currentBattleSelection.glowOverlay != null)
        {
            currentBattleSelection.glowOverlay.SetGlowEnabled(true);
        }
    }

    void OnPlayButtonPressed()
    {
        if (currentBattleSelection == null) return;
        StartCoroutine(PlayPlayerCardSequence(currentBattleSelection));
    }

    IEnumerator PlayPlayerCardSequence(CardUI cardToPlay)
    {
        playCardButton.interactable = false;
        inputLocked = true; // Lock immediately

        CardDisplay display = cardToPlay.GetComponent<CardDisplay>();
        if (display != null && display.cardData != null && display.cardData.effectID == "sup_alay")
        {
            yield return StartCoroutine(ShowAlayChoice());
        }

        if (display != null && display.cardData != null && display.cardData.effectID == "def_agong")
        {
            agongPlayedThisRound = true;
        }

        cardToPlay.transform.SetParent(battleZone);
        cardToPlay.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        cardToPlay.transform.localRotation = Quaternion.identity;
        
        // Position card at CENTER of battle zone (not piled yet)
        LayoutElement le = cardToPlay.GetComponent<LayoutElement>();
        if (le == null) le = cardToPlay.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        cardToPlay.transform.localPosition = Vector3.zero; // Center
        cardToPlay.SetLockedState(false);
        // Disable glow when card is played
        if (cardToPlay.glowOverlay != null) cardToPlay.glowOverlay.SetGlowEnabledImmediate(false);
        if (cardToPlay.selectionBorder != null) cardToPlay.selectionBorder.SetActive(false);

        RecalculateBattleEffects();

        currentBattleSelection = null;

        if (playerGoesFirst)
        {
            StartCoroutine(BakunawaResponseSequence(cardToPlay));
        }
        else
        {
            if (enemyHasPlayedPendingCard && pendingEnemyCard != null)
            {
                StartCoroutine(ResolveImmediateClash(cardToPlay, pendingEnemyCard));
            }
            else
            {
                StartCoroutine(ResolveImmediateClash(cardToPlay, null));
            }
        }
    }

    IEnumerator DiscardPlayerCardSequence(string sourceEffectName)
    {
        Transform target = null;
        if (deckPileArea.childCount > 0) target = deckPileArea.GetChild(0);
        else if (handArea.childCount > 0) target = handArea.GetChild(0);

        if (target != null)
        {
            CardUI cardUI = target.GetComponent<CardUI>();
            CardDisplay display = target.GetComponent<CardDisplay>();

            if (discardNotifyPanel != null && display != null)
            {
                discardNotifyPanel.SetActive(true);
                if (discardNotifyImage != null) discardNotifyImage.sprite = display.cardData.cardArt;
                if (discardNotifyText != null) discardNotifyText.text = sourceEffectName + " forced you to discard:\n" + display.cardData.cardName;

                CanvasGroup group = discardNotifyPanel.GetComponent<CanvasGroup>();
                if (group != null) { group.alpha = 0; while (group.alpha < 1) { group.alpha += Time.deltaTime * 3f; yield return null; } }
                yield return new WaitForSeconds(discardNotifyDuration);
                if (group != null) { while (group.alpha > 0) { group.alpha -= Time.deltaTime * 3f; yield return null; } }
                discardNotifyPanel.SetActive(false);
            }
            MoveToPile(cardUI, discardPileArea, true);
        }
    }

    IEnumerator ShowAgongRetrieval()
    {
        if (discardPileArea.childCount > 0)
        {
            Transform recoveredCardObj = discardPileArea.GetChild(discardPileArea.childCount - 1);
            CardUI recoveredCardUI = recoveredCardObj.GetComponent<CardUI>();
            CardDisplay recoveredData = recoveredCardObj.GetComponent<CardDisplay>();

            if (recoveredCardUI != null && recoveredData != null)
            {
                if (agongCardImage != null) agongCardImage.sprite = recoveredData.cardData.cardArt;
                if (agongCardName != null) agongCardName.text = recoveredData.cardData.cardName;

                if (agongPanel != null)
                {
                    agongPanel.SetActive(true);
                    CanvasGroup group = agongPanel.GetComponent<CanvasGroup>();
                    if (group != null) { group.alpha = 0; while (group.alpha < 1) { group.alpha += Time.deltaTime * 3f; yield return null; } }
                    yield return new WaitForSeconds(agongDuration);
                    if (group != null) { while (group.alpha > 0) { group.alpha -= Time.deltaTime * 3f; yield return null; } }
                    agongPanel.SetActive(false);
                }
                ReturnCardToHand(recoveredCardUI);
            }
        }
    }

    IEnumerator EndRoundSequence()
    {
        // Clear turn indicators at round end
        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.ClearTurnIndicators();
        
        int pScore = 0;
        int bScore = 0;
        bool playerLost = false;
        bool bakunawaWon = false;

        if (ScoreManager.Instance != null)
        {
            pScore = ScoreManager.Instance.playerTotal;
            bScore = ScoreManager.Instance.bakunawaTotal;
            ScoreManager.Instance.ResolveRound();
            if (bScore > pScore) { playerLost = true; bakunawaWon = true; }
        }

        yield return StartCoroutine(ShowResultBanner(pScore, bScore));

        if (playerLost && agongPlayedThisRound)
        {
            yield return StartCoroutine(ShowAgongRetrieval());
        }

        if (bakunawaWon)
        {
            bool lunarActive = false;
            bool tidalActive = false;
            int bakunawaCardCount = 0;

            foreach (Transform t in battleZone)
            {
                CardUI c = t.GetComponent<CardUI>();
                if (c != null && c.isEnemy)
                {
                    bakunawaCardCount++;
                    CardDisplay d = c.GetComponent<CardDisplay>();
                    if (d != null && d.cardData != null)
                    {
                        if (d.cardData.effectID == "atk_lunar") lunarActive = true;
                        if (d.cardData.effectID == "sup_tidal") tidalActive = true;
                    }
                }
            }

            if (lunarActive && bakunawaCardCount == 1)
            {
                if (ScoreManager.Instance != null) ScoreManager.Instance.UpdateTowerScore(1);
            }

            if (tidalActive)
            {
                yield return StartCoroutine(DiscardPlayerCardSequence("Tidal Pull"));
            }
        }

        int bakuRuntimeCount = 0;
        if (BakunawaAI.Instance != null)
        {
            bakuRuntimeCount += BakunawaAI.Instance.deckPileArea.childCount;
            bakuRuntimeCount += BakunawaAI.Instance.handArea.childCount;
        }

        if (bakuRuntimeCount <= 5)
        {
            bool draconicPlayed = false;
            foreach (Transform t in battleZone)
            {
                CardUI c = t.GetComponent<CardUI>();
                if (c != null && c.isEnemy)
                {
                    CardDisplay d = c.GetComponent<CardDisplay>();
                    if (d != null && d.cardData != null && d.cardData.effectID == "sup_draconic")
                        draconicPlayed = true;
                }
            }

            if (draconicPlayed)
            {
                yield return StartCoroutine(DiscardPlayerCardSequence("Draconic Patience"));
            }
        }

        yield return new WaitForSeconds(1.0f);

        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform child in battleZone) { CardUI card = child.GetComponent<CardUI>(); if (card != null) playedCards.Add(card); }
        foreach (CardUI card in playedCards) MoveToPile(card, discardPileArea, true);
        if (BakunawaAI.Instance != null) BakunawaAI.Instance.CleanupRound();

        yield return new WaitForSeconds(1.0f);
        StartNextRound();
    }

    IEnumerator StartPlanningPhaseSequence()
    {
        inputLocked = true;
        isPlanningPhase = true;
        currentTimer = planningTime;
        SetEnergyUIActive(false);

        alayBuffActive = false;
        alayDebuffActive = false;
        agongPlayedThisRound = false;

        // Clear turn indicators during planning phase
        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.ClearTurnIndicators();

        if (planningBanner != null)
        {
            planningBanner.SetActive(true);
            CanvasGroup group = planningBanner.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 0; while (group.alpha < 1) { group.alpha += Time.deltaTime * 3f; yield return null; } }

            if (planningBannerText != null)
            {
                planningBannerText.text = "ROUND " + roundNumber;
                yield return StartCoroutine(FadeTextInAndOut(planningBannerText, 1.5f));
            }

            if (planningBannerText != null)
            {
                planningBannerText.text = "PLANNING PHASE";
                yield return StartCoroutine(FadeTextInAndOut(planningBannerText, 1.5f));
            }

            if (group != null) { while (group.alpha > 0) { group.alpha -= Time.deltaTime * 3f; yield return null; } }
            planningBanner.SetActive(false);
        }
        else yield return new WaitForSeconds(1.0f);

        inputLocked = false;
        SetEnergyUIActive(true);
        UpdateEnergyUI();
        UpdateHandPagination();
    }

    IEnumerator FadeTextInAndOut(Text textObj, float displayDuration)
    {
        Color c = textObj.color;
        float t = 0;
        while (t < 1) { t += Time.deltaTime * 3f; textObj.color = new Color(c.r, c.g, c.b, t); yield return null; }
        yield return new WaitForSeconds(displayDuration);
        t = 1;
        while (t > 0) { t -= Time.deltaTime * 3f; textObj.color = new Color(c.r, c.g, c.b, t); yield return null; }
    }

    void OnLockInPressed()
    {
        if (inputLocked) return;

        int currentUsed = 0;
        foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card);

        if (currentUsed > maxEnergy)
        {
            Debug.Log("Cannot Lock In: Not Enough Energy!");
            StartCoroutine(ShowWarningSequence());
            return;
        }

        isPlanningPhase = false;
        inputLocked = true;
        lockInButton.gameObject.SetActive(false);
        if (timerText != null) timerText.text = "";
        SetEnergyUIActive(false);
        UpdateHandPagination();

        foreach (CardUI card in selectedCardsUI)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null && display.cardData.effectID == "def_agong")
                agongPlayedThisRound = true;

            card.transform.SetParent(tribeLockedPanel);
            // Disable glow when card is locked in
            if (card.glowOverlay != null) card.glowOverlay.SetGlowEnabledImmediate(false);
            if (card.selectionBorder != null) card.selectionBorder.SetActive(false);
            card.SetLockedState(true);
        }

        UpdateContainerSpacing(tribeLockedPanel as RectTransform);

        List<Transform> remainingCards = new List<Transform>();
        foreach (Transform child in handArea) remainingCards.Add(child);
        foreach (Transform child in remainingCards)
        {
            CardUI card = child.GetComponent<CardUI>();
            if (card != null)
            {
                card.transform.SetParent(deckPileArea);
                card.transform.localPosition = Vector3.zero;
                card.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
                card.SwitchToDeckMode(false);
            }
        }
        selectedCardsUI.Clear();

        if (BakunawaAI.Instance != null) BakunawaAI.Instance.LockInPlan();

        StartDicePhase();
    }

    void StartDicePhase()
    {
        if (dicePanel != null)
        {
            dicePanel.SetActive(true);
            rollButton.interactable = true;
        }
        else FinalizeTurnOrder(true);
    }

    void OnRollDicePressed()
    {
        rollButton.interactable = false;
        StartCoroutine(RollDiceRoutine());
    }

    IEnumerator RollDiceRoutine()
    {
        float duration = 1.0f;
        float elapsed = 0f;
        int pRoll = 1;
        int eRoll = 1;

        while (elapsed < duration)
        {
            pRoll = Random.Range(1, 7);
            eRoll = Random.Range(1, 7);

            if (diceSprites != null && diceSprites.Count >= 6)
            {
                playerDiceImg.sprite = diceSprites[pRoll - 1];
                enemyDiceImg.sprite = diceSprites[eRoll - 1];
            }
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($"Dice Roll! Player: {pRoll}, Enemy: {eRoll}");
        yield return new WaitForSeconds(0.5f);

        if (pRoll > eRoll)
        {
            if (turnChoicePanel != null)
            {
                turnChoicePanel.SetActive(true);
                dicePanel.SetActive(false);
            }
            else FinalizeTurnOrder(true);
        }
        else if (eRoll > pRoll)
        {
            dicePanel.SetActive(false);
            ProcessBakunawaTurnDecision();
        }
        else
        {
            rollButton.interactable = true;
        }
    }

    void ProcessBakunawaTurnDecision()
    {
        int playerCards = tribeLockedPanel.childCount;
        int enemyCards = 0;
        if (BakunawaAI.Instance != null && BakunawaAI.Instance.lockedArea != null)
            enemyCards = BakunawaAI.Instance.lockedArea.childCount;

        bool aiGoesFirst = true;
        if (enemyCards < playerCards) aiGoesFirst = false;
        else if (enemyCards > playerCards) aiGoesFirst = true;
        else aiGoesFirst = (Random.value > 0.5f);

        FinalizeTurnOrder(!aiGoesFirst);
    }

    void FinalizeTurnOrder(bool playerIsFirst)
    {
        playerGoesFirst = playerIsFirst;
        if (turnChoicePanel != null) turnChoicePanel.SetActive(false);
        if (dicePanel != null) dicePanel.SetActive(false);

        StartCoroutine(CombatBannerSequence());
    }

    IEnumerator CombatBannerSequence()
    {
        if (combatBanner != null)
        {
            combatBanner.SetActive(true);
            CanvasGroup bannerGroup = combatBanner.GetComponent<CanvasGroup>();

            if (bannerGroup != null) { bannerGroup.alpha = 0; while (bannerGroup.alpha < 1) { bannerGroup.alpha += Time.deltaTime * 3f; yield return null; } }

            if (combatBannerText != null)
            {
                combatBannerText.text = "CARD CLASH!";
                yield return StartCoroutine(FadeTextInAndOut(combatBannerText, 1.5f));
            }

            if (combatBannerText != null)
            {
                combatBannerText.text = playerGoesFirst ? "TRIBESMEN STRIKES FIRST!" : "BAKUNAWA STRIKES FIRST!";
                yield return StartCoroutine(FadeTextInAndOut(combatBannerText, 2.0f));
            }

            if (bannerGroup != null) { while (bannerGroup.alpha > 0) { bannerGroup.alpha -= Time.deltaTime * 3f; yield return null; } }
            combatBanner.SetActive(false);
        }
        else yield return new WaitForSeconds(1.0f);

        StartBattlePhase();
    }

    public bool TryPlayCard(CardUI card)
    {
        // Check if we are allowed to play
        if (inputLocked) return false;
        if (!playerGoesFirst && !playCardButton.interactable) return false; // Not our turn
        if (playCardButton.gameObject.activeSelf && !playCardButton.interactable) return false; // General lock

        // Check if it's actually our turn logic (simplified by reusing button interactable state)
        // If button is hidden, we use 'playCardButton.interactable' state as the logical flag
        if (!playCardButton.interactable) return false;

        StartCoroutine(PlayPlayerCardSequence(card));
        return true;
    }

    void StartBattlePhase()
    {
        inputLocked = false;

        if (tribeLockedPanel.childCount == 0)
        {
            playCardButton.gameObject.SetActive(false);
            StartCoroutine(BakunawaSoloPlaySequence());
            return;
        }

        if (playerGoesFirst)
        {
            enemyHasPlayedPendingCard = false;
            pendingEnemyCard = null;
            // HIDDEN: Using Drag instead
            playCardButton.gameObject.SetActive(false); 
            playCardButton.interactable = true;
            
            // Turn Indicator: Player's turn
            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();
        }
        else
        {
            // Lock Input as Enemy plays first
            inputLocked = true;
            
            // HIDDEN: Using Drag instead
            playCardButton.gameObject.SetActive(false);
            playCardButton.interactable = false;
            
            // Turn Indicator: Bakunawa's turn
            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetBakunawaTurn();
            
            StartCoroutine(EnemyPlaysFirstRoutine());
        }
    }

    IEnumerator EnemyPlaysFirstRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            pendingEnemyCard = BakunawaAI.Instance.PlayCard();
            
            // Animate card dragging to board
            yield return StartCoroutine(BakunawaAI.Instance.AnimateCurveToBoard(pendingEnemyCard));
            
            enemyHasPlayedPendingCard = true;
            RecalculateBattleEffects();
            
            // Allow Player Action
            inputLocked = false;
            playCardButton.interactable = true;
            
            // Turn Indicator: Switch to Player's turn
            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();
        }
        else
        {
            inputLocked = false;
            playCardButton.interactable = true;
            
            // Turn Indicator: Player's turn
            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();
        }
    }

    IEnumerator BakunawaResponseSequence(CardUI playerCard)
    {
        // Turn Indicator: Bakunawa's turn
        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetBakunawaTurn();
        
        yield return new WaitForSeconds(1.0f);

        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
            
            // Animate card moving to board
            yield return StartCoroutine(BakunawaAI.Instance.AnimateCurveToBoard(enemyCard));
            
            RecalculateBattleEffects(); // This puts card in battleZone and updates state

            // NEW: Animate Clash
            yield return StartCoroutine(AnimateCardClash(playerCard, enemyCard));

            if (ScoreManager.Instance != null)
            {
                int pAtk = GetCardAttack(playerCard);
                int eAtk = GetCardAttack(enemyCard);
                ScoreManager.Instance.ResolveClash(pAtk, eAtk);
            }
        }
        else
        {
             // Direct Attack (No enemy card)
             // Animate just Player Card hitting 'something'
             yield return StartCoroutine(AnimateCardClash(playerCard, null));

            if (ScoreManager.Instance != null)
            {
                int pAtk = GetCardAttack(playerCard);
                ScoreManager.Instance.ResolveClash(pAtk, 0);
            }
        }

        yield return new WaitForSeconds(0.5f);
        ContinueBattleLoop();
    }

    IEnumerator ResolveImmediateClash(CardUI playerCard, CardUI enemyCard)
    {
        // OLD: yield return new WaitForSeconds(0.5f);
        
        // NEW: Animate Clash
        yield return StartCoroutine(AnimateCardClash(playerCard, enemyCard));

        // Score Resolution happens AFTER clash
        int pAtk = GetCardAttack(playerCard);
        int eAtk = (enemyCard != null) ? GetCardAttack(enemyCard) : 0;

        if (ScoreManager.Instance != null) ScoreManager.Instance.ResolveClash(pAtk, eAtk);

        enemyHasPlayedPendingCard = false;
        pendingEnemyCard = null;

        yield return new WaitForSeconds(0.5f);

        if (!playerGoesFirst)
        {
            if (tribeLockedPanel.childCount > 0) StartCoroutine(EnemyPlaysFirstRoutine());
            else ContinueBattleLoop();
        }
        else
        {
            ContinueBattleLoop();
        }
    }

    void ContinueBattleLoop()
    {
        if (tribeLockedPanel.childCount > 0)
        {
            inputLocked = false;
            playCardButton.interactable = true;
            
            // Turn Indicator: Player's turn
            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();
        }
        else
        {
            // Lock logic handled by next routines or EndRound
            if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
                StartCoroutine(BakunawaSoloPlaySequence());
            else
                StartCoroutine(EndRoundSequence());
        }
    }

    IEnumerator BakunawaSoloPlaySequence()
    {
        // Turn Indicator: Bakunawa's turn (solo play)
        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetBakunawaTurn();
        
        while (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            yield return new WaitForSeconds(1.0f);
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
            
            // Animate card moving to board
            yield return StartCoroutine(BakunawaAI.Instance.AnimateCurveToBoard(enemyCard));
            
            RecalculateBattleEffects();

            if (ScoreManager.Instance != null)
            {
                int eAtk = GetCardAttack(enemyCard);
                ScoreManager.Instance.ResolveClash(0, eAtk);
            }
            yield return new WaitForSeconds(0.5f);
        }
        StartCoroutine(EndRoundSequence());
    }

    IEnumerator ShowResultBanner(int pScore, int bScore)
    {
        if (resultBannerObject != null)
        {
            if (bannerDisplayImage != null)
            {
                Color c = bannerDisplayImage.color;
                bannerDisplayImage.color = new Color(c.r, c.g, c.b, 1f);
                if (pScore > bScore && tribesmenWinSprite != null)
                {
                    bannerDisplayImage.sprite = tribesmenWinSprite;
                    if (fallbackText) fallbackText.gameObject.SetActive(false);
                }
                else if (bScore > pScore && bakunawaWinSprite != null)
                {
                    bannerDisplayImage.sprite = bakunawaWinSprite;
                    if (fallbackText) fallbackText.gameObject.SetActive(false);
                }
                else
                {
                    if (fallbackText) { fallbackText.gameObject.SetActive(true); fallbackText.text = "DRAW!"; }
                }
            }
            resultBannerObject.SetActive(true);
            CanvasGroup group = resultBannerObject.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 0; float fadeSpeed = 3f; while (group.alpha < 1) { group.alpha += Time.deltaTime * fadeSpeed; yield return null; } }
            yield return new WaitForSeconds(resultDuration);
            if (group != null) { float fadeSpeed = 3f; while (group.alpha > 0) { group.alpha -= Time.deltaTime * fadeSpeed; yield return null; } }
            resultBannerObject.SetActive(false);
        }
    }

    void RecalculateBattleEffects()
    {
        if (CardEffectManager.Instance == null) return;

        List<CardUI> playerBattleCards = new List<CardUI>();
        List<CardUI> enemyBattleCards = new List<CardUI>();

        foreach (Transform t in battleZone)
        {
            CardUI c = t.GetComponent<CardUI>();
            if (c != null)
            {
                if (c.isEnemy) enemyBattleCards.Add(c);
                else playerBattleCards.Add(c);
            }
        }
        if (BakunawaAI.Instance != null && BakunawaAI.Instance.battleZone != null && BakunawaAI.Instance.battleZone != battleZone)
        {
            foreach (Transform t in BakunawaAI.Instance.battleZone)
            {
                CardUI c = t.GetComponent<CardUI>();
                if (c != null)
                {
                    if (c.isEnemy) enemyBattleCards.Add(c);
                    else playerBattleCards.Add(c);
                }
            }
        }

        var playerResult = CardEffectManager.Instance.CalculateRoundStats(playerBattleCards, enemyBattleCards);
        var enemyResult = CardEffectManager.Instance.CalculateRoundStats(enemyBattleCards, playerBattleCards);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.enemyDebuffValue = playerResult.damageReductionToEnemy;
            ScoreManager.Instance.playerDebuffValue = enemyResult.damageReductionToEnemy;
        }
    }

    void StartNextRound()
    {
        if (roundNumber >= 10)
        {
            CheckFinalScoreWin();
            return;
        }

        if (deckPileArea.childCount > 0) { List<CardUI> unusedCards = new List<CardUI>(); foreach (Transform child in deckPileArea) { CardUI card = child.GetComponent<CardUI>(); if (card != null) unusedCards.Add(card); } foreach (CardUI card in unusedCards) ReturnCardToHand(card); }
        else { List<CardUI> discardedCards = new List<CardUI>(); foreach (Transform child in discardPileArea) { CardUI card = child.GetComponent<CardUI>(); if (card != null) discardedCards.Add(card); } ShuffleList(discardedCards); foreach (CardUI card in discardedCards) ReturnCardToHand(card); }

        isPlanningPhase = true;
        currentTimer = planningTime;
        if (timerText != null) timerText.color = Color.white;
        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);
        playCardButton.interactable = true;
        selectedCardsUI.Clear();
        SetEnergyUIActive(true);
        UpdateEnergyUI();
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.enemyDebuffValue = 0;
            ScoreManager.Instance.playerDebuffValue = 0;
        }

        roundNumber++;
        UpdateRoundUI();
        StartCoroutine(StartPlanningPhaseSequence());
    }

    void CheckFinalScoreWin()
    {
        if (ScoreManager.Instance == null) return;
        int score = ScoreManager.Instance.currentTowerScore;

        if (score > 0) TriggerGameOver("Bakunawa");
        else if (score < 0) TriggerGameOver("Tribesmen");
        else TriggerGameOver("Draw");
    }

    // moved logic to end of file to override

    void MoveToPile(CardUI card, Transform pile, bool faceDown) { card.transform.SetParent(pile); card.transform.localPosition = Vector3.zero; card.transform.localScale = new Vector3(discardScale, discardScale, discardScale); card.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f)); card.SwitchToDeckMode(faceDown); }
    void ShuffleList(List<CardUI> list) { for (int i = 0; i < list.Count; i++) { CardUI temp = list[i]; int randomIndex = Random.Range(i, list.Count); list[i] = list[randomIndex]; list[randomIndex] = temp; } }
    void UpdateEnergyUI() { int currentUsed = 0; foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card); int remaining = maxEnergy - currentUsed; if (energySlider != null) { energySlider.maxValue = maxEnergy; energySlider.value = Mathf.Max(0, remaining); } if (energyText != null) { energyText.text = remaining.ToString() + "/" + maxEnergy.ToString(); if (remaining < 0) energyText.color = Color.red; else energyText.color = Color.white; } }
    void SetEnergyUIActive(bool isActive) { if (energySlider != null) energySlider.gameObject.SetActive(isActive); if (energyText != null) energyText.gameObject.SetActive(isActive); }
    public bool ToggleCardSelection(CardUI cardUI, bool isSelected) 
    { 
        if (!isPlanningPhase) return false; 
        
        if (isSelected) 
        {
            // Optional: Check energy limit before allowing move
            int currentUsed = 0;
            foreach (CardUI c in selectedCardsUI) currentUsed += GetCardCost(c);
            if (currentUsed + GetCardCost(cardUI) > maxEnergy)
            {
                StartCoroutine(ShowWarningSequence());
                return false;
            }

            selectedCardsUI.Add(cardUI);
            cardUI.transform.SetParent(tribeSelectedPanel);
            cardUI.transform.localRotation = Quaternion.identity; // Reset rotation from curve
            cardUI.UpdateLockedLayout(); // Ensure spacing is correct for scaled card
            // Scale and position will be handled by CardUI.UpdateAnimation or LayoutGroup
        }
        else 
        {
            selectedCardsUI.Remove(cardUI);
            ReturnCardToHand(cardUI);
        }
        
        UpdateEnergyUI(); 
        
        // Fix: Update spacing for the selected panel to prevent overlapping
        UpdateContainerSpacing(tribeSelectedPanel as RectTransform);
        
        // Force layout update for the hand since a card left/entered
        if (CurvedHandLayout.Instance != null) CurvedHandLayout.Instance.ForceLayoutUpdate();
        
        return true; 
    }
    IEnumerator ShowWarningSequence() { if (warningText != null) { warningText.gameObject.SetActive(true); warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1); yield return new WaitForSeconds(0.5f); float duration = 1.0f; float currentTime = 0f; while (currentTime < duration) { float alpha = Mathf.Lerp(1f, 0f, currentTime / duration); warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, alpha); currentTime += Time.deltaTime; yield return null; } warningText.gameObject.SetActive(false); } }
    int GetCardCost(CardUI card) { if (card == null) return 0; CardDisplay display = card.GetComponent<CardDisplay>(); if (display != null && display.cardData != null) return display.cardData.energyCost; if (card.costText != null && int.TryParse(card.costText.text, out int val)) return val; return 0; }
    int GetCardAttack(CardUI card) { if (card == null) return 0; CardDisplay display = card.GetComponent<CardDisplay>(); if (display != null) return display.currentAttack; if (card.attackText != null && int.TryParse(card.attackText.text, out int val)) return val; return 0; }
    void SpawnDeck() 
    { 
        foreach (CardData card in myDeck) 
        { 
            GameObject newCard = Instantiate(cardPrefab, handArea); 
            CardUI ui = newCard.GetComponent<CardUI>(); 
            ui.Setup(card); 
            CardDisplay display = newCard.GetComponent<CardDisplay>(); 
            if (display != null) 
            { 
                display.cardData = card; 
                display.currentAttack = card.attackValue; 
            } 
        } 
        UpdateHandPagination();
    }

    // --- PAGINATION LOGIC ---
    public void UpdateHandPagination()
    {
        if (handArea == null) return;

        int totalCards = handArea.childCount;
        int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)totalCards / cardsPerPage) - 1);
        
        if (currentPage > maxPage) currentPage = maxPage;
        if (currentPage < 0) currentPage = 0;

        int startIndex = currentPage * cardsPerPage;
        int endIndex = startIndex + cardsPerPage;

        // Iterate through all cards in hand
        for (int i = 0; i < totalCards; i++)
        {
            Transform child = handArea.GetChild(i);
            bool shouldBeVisible = (i >= startIndex && i < endIndex);
            child.gameObject.SetActive(shouldBeVisible);
        }

        // Update Buttons
        bool showPagination = isPlanningPhase;

        if (prevPageBtn != null)
        {
            prevPageBtn.gameObject.SetActive(showPagination);
            if (showPagination) prevPageBtn.interactable = (currentPage > 0);
        }
        if (nextPageBtn != null)
        {
            nextPageBtn.gameObject.SetActive(showPagination);
            if (showPagination) nextPageBtn.interactable = (currentPage < maxPage);
        }

        // Update Text
        if (pageIndicatorText != null)
        {
            pageIndicatorText.gameObject.SetActive(showPagination);
            if (showPagination)
            {
                pageIndicatorText.text = $"Page {currentPage + 1}/{maxPage + 1}";
            }
        }

        // Force Layout Update for the visible cards
        if (CurvedHandLayout.Instance != null) 
        {
            CurvedHandLayout.Instance.ForceLayoutUpdate();
        }
    }

    void NextHandPage()
    {
        int totalCards = handArea.childCount;
        int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)totalCards / cardsPerPage) - 1);
        if (currentPage < maxPage)
        {
            currentPage++;
            UpdateHandPagination();
        }
    }

    void PrevHandPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateHandPagination();
        }
    }

    // Overwrite ReturnCardToHand to use pagination
    void ReturnCardToHand(CardUI card) 
    { 
        card.transform.SetParent(handArea); 
        card.transform.localScale = Vector3.one; 
        card.ResetToHandMode(); 
        
        // Ensure the card is counted in pagination
        UpdateHandPagination(); 
    }

    void EnsureButtonAnimation(Button btn)
    {
        if (btn != null)
        {
            if (btn.gameObject.GetComponent<UIButtonAnimation>() == null)
            {
                btn.gameObject.AddComponent<UIButtonAnimation>();
            }
        }
    }
    void CreateImpactSparks(Vector3 pos)
    {
        // 1. Container
        GameObject container = new GameObject("SparkContainer");
        container.transform.position = pos;
        
        Canvas root = GetComponentInParent<Canvas>();
        if (root != null && root.rootCanvas != null) root = root.rootCanvas;
        if (root != null) container.transform.SetParent(root.transform);
        else container.transform.SetParent(transform);
        
        container.transform.localScale = Vector3.one;
        container.transform.SetAsLastSibling(); // Top of everything

        // 2. Spawn Sprites
        int sparkCount = 20;
        List<RectTransform> sparks = new List<RectTransform>();
        List<Vector2> velocities = new List<Vector2>();

        for(int i=0; i<sparkCount; i++)
        {
            GameObject s = new GameObject("Spark");
            s.transform.SetParent(container.transform);
            s.transform.position = pos; // Start at center
            s.transform.localScale = Vector3.one;
            
            Image img = s.AddComponent<Image>();
            // Gold / bright orange / white mix
            float rVal = Random.value;
            if (rVal > 0.6f) img.color = new Color(1f, 0.9f, 0.4f); // Pale Gold
            else if (rVal > 0.3f) img.color = new Color(1f, 0.6f, 0.1f); // Orange
            else img.color = Color.white; // Sparkle center
            
            // Random direction
            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(300f, 800f); // High speed for screen space
            velocities.Add(dir * speed);

            RectTransform rt = s.GetComponent<RectTransform>();
            float size = Random.Range(10f, 30f);
            rt.sizeDelta = new Vector2(size, size);
            sparks.Add(rt);
        }

        StartCoroutine(AnimateUIExplosion(container, sparks, velocities));
    }

    IEnumerator AnimateUIExplosion(GameObject container, List<RectTransform> sparks, List<Vector2> velocities)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while(elapsed < duration && container != null)
        {
            float t = elapsed / duration;
            for(int i=0; i<sparks.Count; i++)
            {
                if(sparks[i] == null) continue;
                
                // Move
                sparks[i].anchoredPosition += velocities[i] * Time.deltaTime;
                
                // Slow down (Drag)
                velocities[i] = Vector2.Lerp(velocities[i], Vector2.zero, Time.deltaTime * 5f);
                
                // Find Image to Fade
                Image img = sparks[i].GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Lerp(1f, 0f, t * t); // Fade out quadratic
                    img.color = c;
                }
                
                // Shrink
                sparks[i].localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if(container != null) Destroy(container);
    }

    // --- DYNAMIC LAYOUT HELPER ---
    public void UpdateContainerSpacing(RectTransform container)
    {
        if (container == null) return;
        HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) return;

        // FORCE settings to prevent fighting
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;

        int count = container.childCount;
        if (count <= 1) 
        {
            hlg.spacing = 20; // Default
            return;
        }

        // Force canvas update to ensure rects are valid
        Canvas.ForceUpdateCanvases();

        // Get Reference Card Width
        float cardWidth = 0f;
        RectTransform child = container.GetChild(0) as RectTransform;
        
        // Fix: Use lockedScale for intended target size if in locked/selected panels to avoid animation jitter
        float contentScale = 1f;
        if (child != null) contentScale = child.localScale.x;
        
        if (container == tribeLockedPanel || container == tribeSelectedPanel)
        {
            contentScale = lockedScale;
        }

        if (child != null) cardWidth = child.rect.width * contentScale;
        if (cardWidth <= 10f) cardWidth = 150f; // Safer hardcoded fallback

        // HARD CONSTRAINT: 
        // The mat is visually about 950 pixels wide. We MUST fit inside this.
        float maxVisualWidth = 950f; 
        
        float totalCardWidth = count * cardWidth;
        
        // Desired equation: totalCardWidth + (count - 1) * spacing <= availableWidth
        // spacing <= (availableWidth - totalCardWidth) / (count - 1)

        float maxSpacing = tribePanelSpacing; // Use the inspector setting (-90)
        float dynamicSpacing = (maxVisualWidth - totalCardWidth) / (float)(count - 1);
        
        // Clamp: Never expand beyond maxSpacing, but allow infinite overlap (negative spacing)
        hlg.spacing = Mathf.Min(maxSpacing, dynamicSpacing);
    }
}