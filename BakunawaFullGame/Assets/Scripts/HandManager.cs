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
    public float tribePanelSpacing = -40f; // Control spacing in the inspector
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

    void Awake()
    {
        Instance = this;
    }

    // [Enhanced] Multi-Phase Card Clash Animation
    IEnumerator AnimateCardClash(CardUI playerCard, CardUI enemyCard)
    {
        // === PHASE 0: SETUP ===
        Vector3 pStartPos = playerCard.transform.position; // Current Drop Pos
        Vector3 eStartPos = (enemyCard != null) ? enemyCard.transform.position : Vector3.zero;
        Quaternion pOriginalRot = Quaternion.identity;
        Quaternion eOriginalRot = Quaternion.identity;

        // Capture Target Slot Positions (Where they return to)
        // Ensure standard scaling
        playerCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        if (enemyCard != null) enemyCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);

        // Rebuild Layouts to get Slot Positions
        LayoutElement pLe = playerCard.GetComponent<LayoutElement>();
        LayoutElement eLe = (enemyCard != null) ? enemyCard.GetComponent<LayoutElement>() : null;
        if (pLe != null) pLe.ignoreLayout = false;
        if (eLe != null) eLe.ignoreLayout = false;

        if (playerCard.transform.parent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(playerCard.transform.parent as RectTransform);
        if (enemyCard != null && enemyCard.transform.parent != null && enemyCard.transform.parent != playerCard.transform.parent)
             LayoutRebuilder.ForceRebuildLayoutImmediate(enemyCard.transform.parent as RectTransform);
        
        Vector3 pSlotPos = playerCard.transform.position;
        Vector3 eSlotPos = (enemyCard != null) ? enemyCard.transform.position : Vector3.zero;

        // Restore visual start for animation
        playerCard.transform.position = pStartPos;
        if (enemyCard != null) enemyCard.transform.position = eStartPos;

        // Disable Layout
        if (pLe != null) pLe.ignoreLayout = true;
        if (eLe != null) eLe.ignoreLayout = true;
        Canvas.ForceUpdateCanvases();

        // Standardize Alignment: Launch Positions (Center Line)
        Vector3 clashCenter = battleZone.position;
        float launchDist = 250f;
        // Use local offset relative to clashCenter to ensure vertical alignment
        Vector3 pLaunchPos = clashCenter + new Vector3(0, -launchDist, 0);
        Vector3 eLaunchPos = clashCenter + new Vector3(0, launchDist, 0);
        if (enemyCard == null) eLaunchPos = clashCenter; // Dummy target

        // === PHASE 0.5: GATHERING (0.25s) ===
        // Fly to Launch Positions (Center Stage)
        float gatherTime = 0.25f;
        float elapsed = 0f;
        while(elapsed < gatherTime)
        {
             float t = elapsed / gatherTime;
             t = t * t * (3f - 2f * t); // SmoothStep

             playerCard.transform.position = Vector3.Lerp(pStartPos, pLaunchPos, t);
             // Also reset rotation if it was crazy
             playerCard.transform.rotation = Quaternion.Lerp(playerCard.transform.rotation, Quaternion.identity, t);

             if (enemyCard != null)
             {
                 enemyCard.transform.position = Vector3.Lerp(eStartPos, eLaunchPos, t);
                 enemyCard.transform.rotation = Quaternion.Lerp(enemyCard.transform.rotation, Quaternion.identity, t);
             }
             elapsed += Time.deltaTime;
             yield return null;
        }
        playerCard.transform.position = pLaunchPos;
        if (enemyCard != null) enemyCard.transform.position = eLaunchPos;


        // DYNAMIC HEIGHT CALCULATION:
        float actualHeight = 200f; // Default fallback
        RectTransform pRect = playerCard.GetComponent<RectTransform>();
        if (pRect != null) actualHeight = pRect.rect.height * playCardScale;
        
        float cardHalfHeight = actualHeight / 2f; 
        float collisionGap = 20f;    // Gap between cards at collision

        // === PHASE 1: LEVITATE (0.3s) ===
        // Cards lift slightly from Launch Position
        float levitateHeight = 40f;
        Vector3 pLevitatePos = pLaunchPos + new Vector3(0, levitateHeight, 0);
        Vector3 eLevitatePos = eLaunchPos + new Vector3(0, -levitateHeight, 0);

        float levitateTime = 0.3f;
        elapsed = 0f;
        while (elapsed < levitateTime)
        {
            float t = elapsed / levitateTime;
            t = 1f - (1f - t) * (1f - t); // Ease Out

            playerCard.transform.position = Vector3.Lerp(pLaunchPos, pLevitatePos, t);
            
            float scalePulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.05f;
            playerCard.transform.localScale = new Vector3(playCardScale * scalePulse, playCardScale * scalePulse, playCardScale);

            if (enemyCard != null)
            {
                enemyCard.transform.position = Vector3.Lerp(eLaunchPos, eLevitatePos, t);
                enemyCard.transform.localScale = new Vector3(playCardScale * scalePulse, playCardScale * scalePulse, playCardScale);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // === PHASE 2: ANTICIPATION PAUSE (0.2s) ===
        yield return new WaitForSeconds(0.2f);

        // === PHASE 3: LUNGE TO COLLISION (0.15s) ===
        // Player moves UP, Enemy moves DOWN, meeting at center with gap
        Vector3 pCollisionPos = clashCenter + new Vector3(0, -(cardHalfHeight + collisionGap / 2f), 0);
        Vector3 eCollisionPos = clashCenter + new Vector3(0, cardHalfHeight + collisionGap / 2f, 0);

        if (enemyCard == null) pCollisionPos = clashCenter; // Direct attack

        // Add tilt for dramatic effect
        Quaternion pTilt = Quaternion.Euler(0, 0, 8);
        Quaternion eTilt = Quaternion.Euler(0, 0, -8);

        float lungeTime = 0.15f;
        elapsed = 0f;
        while (elapsed < lungeTime)
        {
            float t = elapsed / lungeTime;
            // Aggressive ease-in (cubic) for building momentum
            t = t * t * t;

            playerCard.transform.position = Vector3.Lerp(pLevitatePos, pCollisionPos, t);
            playerCard.transform.rotation = Quaternion.Lerp(pOriginalRot, pTilt, t);

            if (enemyCard != null)
            {
                enemyCard.transform.position = Vector3.Lerp(eLevitatePos, eCollisionPos, t);
                enemyCard.transform.rotation = Quaternion.Lerp(eOriginalRot, eTilt, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final collision position
        playerCard.transform.position = pCollisionPos;
        playerCard.transform.rotation = pTilt;
        if (enemyCard != null)
        {
            enemyCard.transform.position = eCollisionPos;
            enemyCard.transform.rotation = eTilt;
        }

        // === PHASE 4: IMPACT - SCREEN SHAKE (0.15s) ===
        Vector3 camOriginalPos = Camera.main.transform.position;
        float shakeDuration = 0.15f;
        float shakeMagnitude = 8f;

        float shakeElapsed = 0f;
        while (shakeElapsed < shakeDuration)
        {
            // Decreasing shake intensity
            float intensity = 1f - (shakeElapsed / shakeDuration);
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude * intensity;
            shakeOffset.z = 0;
            Camera.main.transform.position = camOriginalPos + shakeOffset;

            shakeElapsed += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.position = camOriginalPos;

        // Set Broken State
        playerCard.SetBroken(true);
        if (enemyCard != null) enemyCard.SetBroken(true);

        // === PHASE 5: BOUNCE BACK (0.2s) ===
        // Cards recoil from impact
        float bounceDistance = 80f;
        Vector3 pBouncePos = pCollisionPos + new Vector3(0, -bounceDistance, 0);
        Vector3 eBouncePos = (enemyCard != null) ? eCollisionPos + new Vector3(0, bounceDistance, 0) : Vector3.zero;

        float bounceTime = 0.2f;
        elapsed = 0f;
        while (elapsed < bounceTime)
        {
            float t = elapsed / bounceTime;
            // Ease out for natural deceleration
            t = 1f - (1f - t) * (1f - t);

            playerCard.transform.position = Vector3.Lerp(pCollisionPos, pBouncePos, t);
            playerCard.transform.rotation = Quaternion.Lerp(pTilt, pOriginalRot, t);

            if (enemyCard != null)
            {
                enemyCard.transform.position = Vector3.Lerp(eCollisionPos, eBouncePos, t);
                enemyCard.transform.rotation = Quaternion.Lerp(eTilt, eOriginalRot, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // === PHASE 6: RETURN TO BOARD (0.3s) ===
        float returnTime = 0.3f;
        elapsed = 0f;
        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            // Smooth ease in-out
            t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            playerCard.transform.position = Vector3.Lerp(pBouncePos, pSlotPos, t);

            if (enemyCard != null)
            {
                enemyCard.transform.position = Vector3.Lerp(eBouncePos, eSlotPos, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // === CLEANUP ===
        // Restore final positions and re-enable layout
        playerCard.transform.position = pSlotPos;
        playerCard.transform.rotation = Quaternion.identity;
        playerCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);

        if (enemyCard != null)
        {
            enemyCard.transform.position = eSlotPos;
            enemyCard.transform.rotation = Quaternion.identity;
            enemyCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        }

        if (pLe != null) pLe.ignoreLayout = false;
        if (eLe != null) eLe.ignoreLayout = false;
    }

    void Start()
    {
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
            
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
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
        }
        else
        {
            // HIDDEN: Using Drag instead
            playCardButton.gameObject.SetActive(false);
            playCardButton.interactable = false;
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
            playCardButton.interactable = true;
        }
        else
        {
            playCardButton.interactable = true;
        }
    }

    IEnumerator BakunawaResponseSequence(CardUI playerCard)
    {
        yield return new WaitForSeconds(1.0f);

        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
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
            playCardButton.interactable = true;
        }
        else
        {
            if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
                StartCoroutine(BakunawaSoloPlaySequence());
            else
                StartCoroutine(EndRoundSequence());
        }
    }

    IEnumerator BakunawaSoloPlaySequence()
    {
        while (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            yield return new WaitForSeconds(1.0f);
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
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
}