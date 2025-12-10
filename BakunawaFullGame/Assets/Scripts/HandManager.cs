using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Multiplayer & Roles")]
    public string currentRole = "Attacker";
    public int cardsPerHand = 5;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Image gameOverImage;
    public Sprite victorySprite;
    public Sprite defeatSprite;
    public Text winnerText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Extra Game Over Icons")]
    public Image extraIconDisplay;
    public Sprite bakunawaIconSprite;
    public Sprite tribesmenIconSprite;

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
    public Transform lockedHandArea;
    public Transform deckPileArea;
    public Transform battleZone;
    public Transform discardPileArea;

    [Header("UI Controls")]
    public Button lockInButton;
    public Button playCardButton;
    public Text timerText;

    [Header("Settings")]
    public float playCardScale = 1.2f;
    public float discardScale = 0.8f;
    public float planningTime = 60f;

    [Header("Details UI")]
    public GameObject detailsPanel;
    public Text detailName;
    public Text detailDesc;
    public Image detailImage;
    public Text detailCost;
    public Text detailAttack;

    [Header("Data")]
    public List<CardData> allCardsDatabase;

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

    void Start()
    {
        if (SimpleNetworkManager.Instance != null)
        {
            currentRole = SimpleNetworkManager.Instance.myRole;
        }

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

        lockInButton.onClick.AddListener(OnLockInPressed);
        playCardButton.onClick.AddListener(OnPlayButtonPressed);

        if (rollButton != null) rollButton.onClick.AddListener(OnRollDicePressed);
        if (goFirstButton != null) goFirstButton.onClick.AddListener(() => FinalizeTurnOrder(true));
        if (goSecondButton != null) goSecondButton.onClick.AddListener(() => FinalizeTurnOrder(false));

        if (alayBuffButton != null) alayBuffButton.onClick.AddListener(() => ResolveAlayChoice(true));
        if (alayDebuffButton != null) alayDebuffButton.onClick.AddListener(() => ResolveAlayChoice(false));

        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);

        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);

        roundNumber = 1;
        UpdateRoundUI();

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
                timerText.color = currentTimer <= 10f ? Color.red : Color.white;
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

    // --- ROLE BASED SPAWNING ---
    void SpawnDeck()
    {
        List<CardData> roleDeck = new List<CardData>();

        if (currentRole == "Attacker") roleDeck = allCardsDatabase.Where(c => c.type == CardType.Attack).ToList();
        else if (currentRole == "Tank") roleDeck = allCardsDatabase.Where(c => c.type == CardType.Defense).ToList();
        else if (currentRole == "Support") roleDeck = allCardsDatabase.Where(c => c.type == CardType.Support).ToList();
        else roleDeck = allCardsDatabase;

        for (int i = 0; i < cardsPerHand; i++)
        {
            if (roleDeck.Count == 0) break;
            int randIndex = Random.Range(0, roleDeck.Count);
            CardData cardToSpawn = roleDeck[randIndex];

            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.Setup(cardToSpawn);

            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.cardData = cardToSpawn;
                display.currentAttack = cardToSpawn.attackValue;
            }
        }
    }

    // --- LOCK IN ---
    void OnLockInPressed()
    {
        if (inputLocked) return;

        int currentUsed = 0;
        foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card);

        if (currentUsed > maxEnergy)
        {
            StartCoroutine(ShowWarningSequence());
            return;
        }

        isPlanningPhase = false;
        inputLocked = true;
        lockInButton.gameObject.SetActive(false);
        if (timerText != null) timerText.text = "";
        SetEnergyUIActive(false);

        foreach (CardUI card in selectedCardsUI)
        {
            card.transform.SetParent(lockedHandArea);
            card.selectionBorder.SetActive(false);
            card.SetLockedState(true);

            // Safely get card name for network using CardDisplay
            CardDisplay d = card.GetComponent<CardDisplay>();
            string cName = (d != null && d.cardData != null) ? d.cardData.cardName : "Unknown";

            if (SimpleNetworkManager.Instance != null)
                SimpleNetworkManager.Instance.SendMessageToServer("LOCK_CARD:" + cName);
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
                card.SwitchToDeckMode(false);
            }
        }
        selectedCardsUI.Clear();

        if (BakunawaAI.Instance != null) BakunawaAI.Instance.LockInPlan();

        StartDicePhase();
    }

    // --- DICE & TURN ---
    void StartDicePhase()
    {
        if (dicePanel != null) { dicePanel.SetActive(true); rollButton.interactable = true; }
        else FinalizeTurnOrder(true);
    }

    void OnRollDicePressed() { rollButton.interactable = false; StartCoroutine(RollDiceRoutine()); }

    IEnumerator RollDiceRoutine()
    {
        float duration = 1.0f;
        float elapsed = 0f;
        int pRoll = 1, eRoll = 1;

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
            if (turnChoicePanel != null) { turnChoicePanel.SetActive(true); dicePanel.SetActive(false); }
            else FinalizeTurnOrder(true);
        }
        else if (eRoll > pRoll)
        {
            dicePanel.SetActive(false);
            ProcessBakunawaTurnDecision();
        }
        else { rollButton.interactable = true; }
    }

    void ProcessBakunawaTurnDecision()
    {
        int playerCards = lockedHandArea.childCount;
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

    // --- BATTLE ---
    void StartBattlePhase()
    {
        inputLocked = false;
        if (lockedHandArea.childCount == 0)
        {
            playCardButton.gameObject.SetActive(false);
            StartCoroutine(BakunawaSoloPlaySequence());
            return;
        }

        if (playerGoesFirst)
        {
            enemyHasPlayedPendingCard = false;
            pendingEnemyCard = null;
            playCardButton.gameObject.SetActive(true);
            playCardButton.interactable = true;
        }
        else
        {
            playCardButton.gameObject.SetActive(true);
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
            enemyHasPlayedPendingCard = true;
            RecalculateBattleEffects();
            playCardButton.interactable = true;
        }
        else playCardButton.interactable = true;
    }

    public void SelectCardForBattle(CardUI card)
    {
        if (inputLocked || (playCardButton != null && !playCardButton.interactable)) return;
        if (currentBattleSelection != null && currentBattleSelection.selectionBorder != null)
            currentBattleSelection.selectionBorder.SetActive(false);
        currentBattleSelection = card;
        if (currentBattleSelection.selectionBorder != null)
            currentBattleSelection.selectionBorder.SetActive(true);
    }

    // --- SELECTION LOGIC ---
    public bool ToggleCardSelection(CardUI cardUI, bool isSelected)
    {
        if (!isPlanningPhase) return false;
        if (isSelected) selectedCardsUI.Add(cardUI);
        else selectedCardsUI.Remove(cardUI);
        UpdateEnergyUI();
        return true;
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

        if (display != null && display.cardData != null)
        {
            if (display.cardData.effectID == "sup_alay") yield return StartCoroutine(ShowAlayChoice());
            if (display.cardData.effectID == "def_agong") agongPlayedThisRound = true;
        }

        cardToPlay.transform.SetParent(battleZone);
        cardToPlay.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        cardToPlay.transform.localRotation = Quaternion.identity;
        cardToPlay.SetLockedState(false);
        cardToPlay.selectionBorder.SetActive(false);

        RecalculateBattleEffects();
        currentBattleSelection = null;

        if (playerGoesFirst) StartCoroutine(BakunawaResponseSequence(cardToPlay));
        else
        {
            if (enemyHasPlayedPendingCard && pendingEnemyCard != null) StartCoroutine(ResolveImmediateClash(cardToPlay, pendingEnemyCard));
            else StartCoroutine(ResolveImmediateClash(cardToPlay, null));
        }
    }

    IEnumerator BakunawaResponseSequence(CardUI playerCard)
    {
        yield return new WaitForSeconds(1.0f);
        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
            RecalculateBattleEffects();
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResolveClash(GetCardAttack(playerCard), GetCardAttack(enemyCard));
        }
        else if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResolveClash(GetCardAttack(playerCard), 0);
        }
        yield return new WaitForSeconds(0.5f);
        ContinueBattleLoop();
    }

    IEnumerator ResolveImmediateClash(CardUI playerCard, CardUI enemyCard)
    {
        yield return new WaitForSeconds(0.5f);
        int pAtk = GetCardAttack(playerCard);
        int eAtk = (enemyCard != null) ? GetCardAttack(enemyCard) : 0;
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResolveClash(pAtk, eAtk);
        enemyHasPlayedPendingCard = false;
        pendingEnemyCard = null;
        yield return new WaitForSeconds(0.5f);

        if (!playerGoesFirst)
        {
            if (lockedHandArea.childCount > 0) StartCoroutine(EnemyPlaysFirstRoutine());
            else ContinueBattleLoop();
        }
        else ContinueBattleLoop();
    }

    void ContinueBattleLoop()
    {
        if (lockedHandArea.childCount > 0) playCardButton.interactable = true;
        else
        {
            if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards()) StartCoroutine(BakunawaSoloPlaySequence());
            else StartCoroutine(EndRoundSequence());
        }
    }

    IEnumerator BakunawaSoloPlaySequence()
    {
        while (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            yield return new WaitForSeconds(1.0f);
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
            RecalculateBattleEffects();
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResolveClash(0, GetCardAttack(enemyCard));
            yield return new WaitForSeconds(0.5f);
        }
        StartCoroutine(EndRoundSequence());
    }

    // --- END ROUND ---
    IEnumerator EndRoundSequence()
    {
        int pScore = 0, bScore = 0;
        bool playerLost = false, bakunawaWon = false;

        if (ScoreManager.Instance != null)
        {
            pScore = ScoreManager.Instance.playerTotal;
            bScore = ScoreManager.Instance.bakunawaTotal;
            ScoreManager.Instance.ResolveRound();
            if (bScore > pScore) { playerLost = true; bakunawaWon = true; }
        }

        yield return StartCoroutine(ShowResultBanner(pScore, bScore));

        if (playerLost && agongPlayedThisRound) yield return StartCoroutine(ShowAgongRetrieval());

        if (bakunawaWon)
        {
            bool lunarActive = false, tidalActive = false;
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
            if (lunarActive && bakunawaCardCount == 1 && ScoreManager.Instance != null) ScoreManager.Instance.UpdateTowerScore(1);
            if (tidalActive) yield return StartCoroutine(DiscardPlayerCardSequence("Tidal Pull"));
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
                    if (d != null && d.cardData != null && d.cardData.effectID == "sup_draconic") draconicPlayed = true;
                }
            }
            if (draconicPlayed) yield return StartCoroutine(DiscardPlayerCardSequence("Draconic Patience"));
        }

        yield return new WaitForSeconds(1.0f);

        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform child in battleZone) { CardUI card = child.GetComponent<CardUI>(); if (card != null) playedCards.Add(card); }
        foreach (CardUI card in playedCards) MoveToPile(card, discardPileArea, true);
        if (BakunawaAI.Instance != null) BakunawaAI.Instance.CleanupRound();

        yield return new WaitForSeconds(1.0f);
        StartNextRound();
    }

    // --- HELPERS & UTILS ---
    IEnumerator ShowResultBanner(int pScore, int bScore)
    {
        if (resultBannerObject != null)
        {
            if (bannerDisplayImage != null)
            {
                bannerDisplayImage.color = Color.white;
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
                else if (fallbackText)
                {
                    fallbackText.gameObject.SetActive(true);
                    fallbackText.text = "DRAW!";
                }
            }
            resultBannerObject.SetActive(true);
            CanvasGroup group = resultBannerObject.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 0; while (group.alpha < 1) { group.alpha += Time.deltaTime * 3f; yield return null; } }
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

        List<CardUI> allCards = new List<CardUI>();
        foreach (Transform t in battleZone) { CardUI c = t.GetComponent<CardUI>(); if (c != null) allCards.Add(c); }
        if (BakunawaAI.Instance != null && BakunawaAI.Instance.battleZone != null && BakunawaAI.Instance.battleZone != battleZone)
        {
            foreach (Transform t in BakunawaAI.Instance.battleZone) { CardUI c = t.GetComponent<CardUI>(); if (c != null) allCards.Add(c); }
        }

        foreach (CardUI c in allCards) { if (c.isEnemy) enemyBattleCards.Add(c); else playerBattleCards.Add(c); }

        var pResult = CardEffectManager.Instance.CalculateRoundStats(playerBattleCards, enemyBattleCards);
        var eResult = CardEffectManager.Instance.CalculateRoundStats(enemyBattleCards, playerBattleCards);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.enemyDebuffValue = pResult.damageReductionToEnemy;
            ScoreManager.Instance.playerDebuffValue = eResult.damageReductionToEnemy;
        }
    }

    void StartNextRound()
    {
        if (roundNumber >= 10) { CheckFinalScoreWin(); return; }

        if (deckPileArea.childCount > 0) { List<CardUI> unused = new List<CardUI>(); foreach (Transform child in deckPileArea) { CardUI c = child.GetComponent<CardUI>(); if (c != null) unused.Add(c); } foreach (CardUI c in unused) ReturnCardToHand(c); }
        else { List<CardUI> discarded = new List<CardUI>(); foreach (Transform child in discardPileArea) { CardUI c = child.GetComponent<CardUI>(); if (c != null) discarded.Add(c); } ShuffleList(discarded); foreach (CardUI c in discarded) ReturnCardToHand(c); }

        isPlanningPhase = true;
        currentTimer = planningTime;
        if (timerText != null) timerText.color = Color.white;
        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);
        playCardButton.interactable = true;
        selectedCardsUI.Clear();
        SetEnergyUIActive(true);
        UpdateEnergyUI();
        if (ScoreManager.Instance != null) { ScoreManager.Instance.enemyDebuffValue = 0; ScoreManager.Instance.playerDebuffValue = 0; }

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

    void ReturnCardToHand(CardUI card) { card.transform.SetParent(handArea); card.transform.localRotation = Quaternion.identity; card.transform.localScale = Vector3.one; card.ResetToHandMode(); }
    void MoveToPile(CardUI card, Transform pile, bool faceDown) { card.transform.SetParent(pile); card.transform.localPosition = Vector3.zero; card.transform.localScale = new Vector3(discardScale, discardScale, discardScale); card.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f)); card.SwitchToDeckMode(faceDown); }
    void ShuffleList(List<CardUI> list) { for (int i = 0; i < list.Count; i++) { CardUI temp = list[i]; int randomIndex = Random.Range(i, list.Count); list[i] = list[randomIndex]; list[randomIndex] = temp; } }

    // --- DETAILS & UI UTILS ---
    public void ShowCardDetails(CardData data) { detailsPanel.SetActive(true); if (detailName != null) detailName.text = data.cardName; if (detailDesc != null) detailDesc.text = data.description; if (detailImage != null && data.cardArt != null) detailImage.sprite = data.cardArt; if (detailCost != null) detailCost.text = data.energyCost.ToString(); if (detailAttack != null) detailAttack.text = data.attackValue.ToString(); }
    public void HideCardDetails() { detailsPanel.SetActive(false); }
    void UpdateEnergyUI() { int used = 0; foreach (CardUI c in selectedCardsUI) used += GetCardCost(c); int remaining = maxEnergy - used; if (energySlider != null) { energySlider.maxValue = maxEnergy; energySlider.value = Mathf.Max(0, remaining); } if (energyText != null) { energyText.text = remaining + "/" + maxEnergy; energyText.color = remaining < 0 ? Color.red : Color.white; } }
    void SetEnergyUIActive(bool isActive) { if (energySlider != null) energySlider.gameObject.SetActive(isActive); if (energyText != null) energyText.gameObject.SetActive(isActive); }
    IEnumerator ShowWarningSequence() { if (warningText != null) { warningText.gameObject.SetActive(true); warningText.color = Color.white; yield return new WaitForSeconds(0.5f); float dur = 1.0f, cur = 0f; while (cur < dur) { warningText.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, cur / dur)); cur += Time.deltaTime; yield return null; } warningText.gameObject.SetActive(false); } }
    int GetCardCost(CardUI card) { if (card == null) return 0; CardDisplay d = card.GetComponent<CardDisplay>(); if (d != null && d.cardData != null) return d.cardData.energyCost; return 0; }
    int GetCardAttack(CardUI card) { if (card == null) return 0; CardDisplay d = card.GetComponent<CardDisplay>(); if (d != null) return d.currentAttack; return 0; }
    IEnumerator ShowAlayChoice() { if (alayChoicePanel != null) { alayChoicePanel.SetActive(true); alayChoiceMade = false; while (!alayChoiceMade) yield return null; alayChoicePanel.SetActive(false); } }
    void ResolveAlayChoice(bool isBuff) { if (isBuff) { alayBuffActive = true; alayDebuffActive = false; } else { alayBuffActive = false; alayDebuffActive = true; } alayChoiceMade = true; }
    IEnumerator DiscardPlayerCardSequence(string source) { Transform t = null; if (deckPileArea.childCount > 0) t = deckPileArea.GetChild(0); else if (handArea.childCount > 0) t = handArea.GetChild(0); if (t != null) { if (discardNotifyPanel != null) { discardNotifyPanel.SetActive(true); if (discardNotifyText != null) discardNotifyText.text = source + "\nDiscarded:\n" + t.GetComponent<CardDisplay>().cardData.cardName; yield return new WaitForSeconds(discardNotifyDuration); discardNotifyPanel.SetActive(false); } MoveToPile(t.GetComponent<CardUI>(), discardPileArea, true); } }
    IEnumerator ShowAgongRetrieval() { if (discardPileArea.childCount > 0) { Transform t = discardPileArea.GetChild(discardPileArea.childCount - 1); if (agongPanel != null) { agongPanel.SetActive(true); if (agongCardName != null) agongCardName.text = t.GetComponent<CardDisplay>().cardData.cardName; yield return new WaitForSeconds(agongDuration); agongPanel.SetActive(false); } ReturnCardToHand(t.GetComponent<CardUI>()); } }
    public void TriggerGameOver(string winner) { if (isGameOver) return; isGameOver = true; if (gameOverPanel != null) { gameOverPanel.SetActive(true); if (winnerText != null) { winnerText.text = winner.ToUpper() + " WINS!"; winnerText.color = winner == "Tribesmen" ? new Color(0f, 1f, 1f) : Color.red; } if (gameOverImage != null) { gameOverImage.color = Color.white; if (winner == "Tribesmen" && victorySprite != null) gameOverImage.sprite = victorySprite; else if (defeatSprite != null) gameOverImage.sprite = defeatSprite; } if (extraIconDisplay != null) { extraIconDisplay.gameObject.SetActive(true); extraIconDisplay.sprite = (winner == "Tribesmen") ? tribesmenIconSprite : bakunawaIconSprite; } } }
    void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    void GoToMainMenu() { SceneManager.LoadScene("MainMenu"); }
    IEnumerator StartPlanningPhaseSequence() { inputLocked = true; isPlanningPhase = true; currentTimer = planningTime; SetEnergyUIActive(false); alayBuffActive = false; alayDebuffActive = false; agongPlayedThisRound = false; if (planningBanner != null) { planningBanner.SetActive(true); CanvasGroup group = planningBanner.GetComponent<CanvasGroup>(); if (group != null) { group.alpha = 0; while (group.alpha < 1) { group.alpha += Time.deltaTime * 3f; yield return null; } } if (planningBannerText != null) { planningBannerText.text = "ROUND " + roundNumber; yield return StartCoroutine(FadeTextInAndOut(planningBannerText, 1.5f)); } if (planningBannerText != null) { planningBannerText.text = "PLANNING PHASE"; yield return StartCoroutine(FadeTextInAndOut(planningBannerText, 1.5f)); } if (group != null) { while (group.alpha > 0) { group.alpha -= Time.deltaTime * 3f; yield return null; } } planningBanner.SetActive(false); } else yield return new WaitForSeconds(1.0f); inputLocked = false; SetEnergyUIActive(true); UpdateEnergyUI(); }
    IEnumerator FadeTextInAndOut(Text textObj, float displayDuration) { Color c = textObj.color; float t = 0; while (t < 1) { t += Time.deltaTime * 3f; textObj.color = new Color(c.r, c.g, c.b, t); yield return null; } yield return new WaitForSeconds(displayDuration); t = 1; while (t > 0) { t -= Time.deltaTime * 3f; textObj.color = new Color(c.r, c.g, c.b, t); yield return null; } }
}