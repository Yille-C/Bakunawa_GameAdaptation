using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

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

    [Header("Data")]
    public List<CardData> myDeck;

    private List<CardUI> selectedCardsUI = new List<CardUI>();

    public bool isPlanningPhase = true;
    private bool inputLocked = true;
    private float currentTimer;
    private CardUI currentBattleSelection;

    // GAME STATE
    public int roundNumber = 1;        // CHANGED TO PUBLIC for card effects
    private bool playerGoesFirst = true;
    private bool enemyHasPlayedPendingCard = false;
    private CardUI pendingEnemyCard = null;

    void Awake()
    {
        Instance = this;
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

        // Button Listeners
        lockInButton.onClick.AddListener(OnLockInPressed);
        playCardButton.onClick.AddListener(OnPlayButtonPressed);

        if (rollButton != null) rollButton.onClick.AddListener(OnRollDicePressed);
        if (goFirstButton != null) goFirstButton.onClick.AddListener(() => FinalizeTurnOrder(true));
        if (goSecondButton != null) goSecondButton.onClick.AddListener(() => FinalizeTurnOrder(false));

        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);

        roundNumber = 1; // Start at Round 1
        UpdateRoundUI();

        SpawnDeck();
        StartCoroutine(StartPlanningPhaseSequence());
    }

    void Update()
    {
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
        if (roundCounterText != null)
        {
            roundCounterText.text = roundNumber.ToString();
        }
    }

    // --- CINEMATIC ROUND START ---
    IEnumerator StartPlanningPhaseSequence()
    {
        inputLocked = true;
        isPlanningPhase = true;
        currentTimer = planningTime;
        SetEnergyUIActive(false);

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

    // --- LOCK IN ---
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

        foreach (CardUI card in selectedCardsUI)
        {
            card.transform.SetParent(lockedHandArea);
            card.selectionBorder.SetActive(false);
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

    // --- DICE SYSTEM ---
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

    // --- BATTLE SEQUENCE ---
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
        else
        {
            playCardButton.interactable = true;
        }
    }

    public void SelectCardForBattle(CardUI card)
    {
        if (inputLocked || (playCardButton.interactable == false)) return;

        if (currentBattleSelection != null)
        {
            currentBattleSelection.selectionBorder.SetActive(false);
        }
        currentBattleSelection = card;
        if (currentBattleSelection.selectionBorder != null)
        {
            currentBattleSelection.selectionBorder.SetActive(true);
        }
    }

    void OnPlayButtonPressed()
    {
        if (currentBattleSelection == null) return;

        currentBattleSelection.transform.SetParent(battleZone);
        currentBattleSelection.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        currentBattleSelection.transform.localRotation = Quaternion.identity;
        currentBattleSelection.SetLockedState(false);
        currentBattleSelection.selectionBorder.SetActive(false);

        RecalculateBattleEffects();

        CardUI playerCard = currentBattleSelection;
        currentBattleSelection = null;
        playCardButton.interactable = false;

        if (playerGoesFirst)
        {
            StartCoroutine(BakunawaResponseSequence(playerCard));
        }
        else
        {
            if (enemyHasPlayedPendingCard && pendingEnemyCard != null)
            {
                StartCoroutine(ResolveImmediateClash(playerCard, pendingEnemyCard));
            }
            else
            {
                StartCoroutine(ResolveImmediateClash(playerCard, null));
            }
        }
    }

    IEnumerator BakunawaResponseSequence(CardUI playerCard)
    {
        yield return new WaitForSeconds(1.0f);

        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
            RecalculateBattleEffects();

            if (ScoreManager.Instance != null)
            {
                int pAtk = GetCardAttack(playerCard);
                int eAtk = GetCardAttack(enemyCard);
                ScoreManager.Instance.ResolveClash(pAtk, eAtk);
            }
        }
        else
        {
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
        else
        {
            ContinueBattleLoop();
        }
    }

    void ContinueBattleLoop()
    {
        if (lockedHandArea.childCount > 0)
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

    IEnumerator EndRoundSequence()
    {
        int pScore = 0;
        int bScore = 0;
        if (ScoreManager.Instance != null)
        {
            pScore = ScoreManager.Instance.playerTotal;
            bScore = ScoreManager.Instance.bakunawaTotal;
            ScoreManager.Instance.ResolveRound();
        }

        yield return StartCoroutine(ShowResultBanner(pScore, bScore));
        yield return new WaitForSeconds(1.0f);

        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform child in battleZone) { CardUI card = child.GetComponent<CardUI>(); if (card != null) playedCards.Add(card); }
        foreach (CardUI card in playedCards) MoveToPile(card, discardPileArea, true);
        if (BakunawaAI.Instance != null) BakunawaAI.Instance.CleanupRound();

        yield return new WaitForSeconds(1.0f);
        StartNextRound();
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
        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform t in battleZone) { CardUI c = t.GetComponent<CardUI>(); if (c != null) playedCards.Add(c); }
        List<CardUI> enemyCards = new List<CardUI>();
        if (BakunawaAI.Instance != null && BakunawaAI.Instance.battleZone != null) { foreach (Transform t in BakunawaAI.Instance.battleZone) { CardUI c = t.GetComponent<CardUI>(); if (c != null) enemyCards.Add(c); } }
        var result = CardEffectManager.Instance.CalculateRoundStats(playedCards, enemyCards);
        if (ScoreManager.Instance != null) ScoreManager.Instance.enemyDebuffValue = result.damageReductionToEnemy;
    }

    void StartNextRound()
    {
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
        if (ScoreManager.Instance != null) ScoreManager.Instance.enemyDebuffValue = 0;

        roundNumber++;
        UpdateRoundUI();
        StartCoroutine(StartPlanningPhaseSequence());
    }

    void ReturnCardToHand(CardUI card) { card.transform.SetParent(handArea); card.transform.localRotation = Quaternion.identity; card.transform.localScale = Vector3.one; card.ResetToHandMode(); }
    void MoveToPile(CardUI card, Transform pile, bool faceDown) { card.transform.SetParent(pile); card.transform.localPosition = Vector3.zero; card.transform.localScale = new Vector3(discardScale, discardScale, discardScale); card.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f)); card.SwitchToDeckMode(faceDown); }
    void ShuffleList(List<CardUI> list) { for (int i = 0; i < list.Count; i++) { CardUI temp = list[i]; int randomIndex = Random.Range(i, list.Count); list[i] = list[randomIndex]; list[randomIndex] = temp; } }
    public void ShowCardDetails(CardData data) { detailsPanel.SetActive(true); detailName.text = data.cardName; detailDesc.text = data.description; }
    public void HideCardDetails() { detailsPanel.SetActive(false); }

    void UpdateEnergyUI() { int currentUsed = 0; foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card); int remaining = maxEnergy - currentUsed; if (energySlider != null) { energySlider.maxValue = maxEnergy; energySlider.value = Mathf.Max(0, remaining); } if (energyText != null) { energyText.text = remaining.ToString() + "/" + maxEnergy.ToString(); if (remaining < 0) energyText.color = Color.red; else energyText.color = Color.white; } }
    void SetEnergyUIActive(bool isActive) { if (energySlider != null) energySlider.gameObject.SetActive(isActive); if (energyText != null) energyText.gameObject.SetActive(isActive); }
    public bool ToggleCardSelection(CardUI cardUI, bool isSelected) { if (!isPlanningPhase) return false; if (isSelected) selectedCardsUI.Add(cardUI); else selectedCardsUI.Remove(cardUI); UpdateEnergyUI(); return true; }
    IEnumerator ShowWarningSequence() { if (warningText != null) { warningText.gameObject.SetActive(true); warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1); yield return new WaitForSeconds(0.5f); float duration = 1.0f; float currentTime = 0f; while (currentTime < duration) { float alpha = Mathf.Lerp(1f, 0f, currentTime / duration); warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, alpha); currentTime += Time.deltaTime; yield return null; } warningText.gameObject.SetActive(false); } }
    int GetCardCost(CardUI card) { if (card == null) return 0; CardDisplay display = card.GetComponent<CardDisplay>(); if (display != null && display.cardData != null) return display.cardData.energyCost; if (card.costText != null && int.TryParse(card.costText.text, out int val)) return val; return 0; }
    int GetCardAttack(CardUI card) { if (card == null) return 0; CardDisplay display = card.GetComponent<CardDisplay>(); if (display != null) return display.currentAttack; if (card.attackText != null && int.TryParse(card.attackText.text, out int val)) return val; return 0; }
    void SpawnDeck() { foreach (CardData card in myDeck) { GameObject newCard = Instantiate(cardPrefab, handArea); CardUI ui = newCard.GetComponent<CardUI>(); ui.Setup(card); CardDisplay display = newCard.GetComponent<CardDisplay>(); if (display != null) { display.cardData = card; display.currentAttack = card.attackValue; } } }
}