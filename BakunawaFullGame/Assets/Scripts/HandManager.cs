using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Game Settings")]
    public int cardsPerHand = 5;
    public bool spawnEntireDatabase = true;
    public float playCardScale = 1.2f;
    public float discardScale = 0.8f;
    public float planningTime = 60f;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public Text winnerText;
    public Button restartButton;
    public Button mainMenuButton;
    public Text timerText;
    public Text roundCounterText;
    public Button lockInButton;
    public Button playCardButton;

    [Header("Areas")]
    public GameObject cardPrefab;
    public Transform handArea;
    public Transform lockedHandArea;
    public Transform deckPileArea;
    public Transform battleZone;
    public Transform discardPileArea;

    [Header("Dice & Turn")]
    public GameObject dicePanel;
    public Image playerDiceImg;
    public Image enemyDiceImg;
    public Button rollButton;
    public List<Sprite> diceSprites;
    public GameObject turnChoicePanel;
    public Button goFirstButton;
    public Button goSecondButton;
    public GameObject combatBanner;
    public Text combatBannerText;

    // --- MISSING VARIABLES RESTORED HERE ---
    [Header("Planning Banner & Round Info")]
    public GameObject planningBanner;
    public Text planningBannerText;
    public float planningBannerDuration = 2.0f;
    // ---------------------------------------

    [Header("Energy")]
    public Slider energySlider;
    public Text energyText;
    public Text warningText;
    public int maxEnergy = 10;
    public int currentEnergy = 10;

    [Header("Details Panel")]
    public GameObject detailsPanel;
    public Text detailName;
    public Text detailDesc;
    public Image detailImage;
    public Text detailCost;
    public Text detailAttack;

    [Header("Data")]
    public List<CardData> allCardsDatabase;

    // --- Compatibility Vars ---
    public bool alayBuffActive = false;
    public bool alayDebuffActive = false;
    public bool agongPlayedThisRound = false;
    public bool isGameOver = false;
    // --------------------------

    private List<CardUI> selectedCardsUI = new List<CardUI>();
    public bool isPlanningPhase = true;
    private bool inputLocked = false;
    public int roundNumber = 1;
    public bool playerGoesFirst = true;
    private float currentTimerValue;
    private CardUI currentBattleSelection;

    // AI State
    private bool enemyHasPlayed = false;
    private CardUI pendingEnemyCard = null;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // UI Cleanup
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (dicePanel) dicePanel.SetActive(false);
        if (turnChoicePanel) turnChoicePanel.SetActive(false);
        if (combatBanner) combatBanner.SetActive(false);
        if (detailsPanel) detailsPanel.SetActive(false);
        if (warningText) warningText.gameObject.SetActive(false);

        // Ensure Planning Banner is hidden at start
        if (planningBanner) planningBanner.SetActive(false);

        restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        lockInButton.onClick.AddListener(OnLockInPressed);
        playCardButton.onClick.AddListener(OnPlayButtonPressed);
        rollButton.onClick.AddListener(OnRollDicePressed);
        goFirstButton.onClick.AddListener(() => FinalizeTurnOrder(true));
        goSecondButton.onClick.AddListener(() => FinalizeTurnOrder(false));

        StartNewRound();
    }

    void Update()
    {
        if (isPlanningPhase && !inputLocked)
        {
            currentTimerValue -= Time.deltaTime;
            UpdateTimerUI(currentTimerValue);

            if (currentTimerValue <= 0)
            {
                currentTimerValue = 0;
                OnLockInPressed();
            }
        }
    }

    void StartNewRound()
    {
        UpdateRoundUI(); // Update Visuals

        // Reset State
        isPlanningPhase = true;
        inputLocked = false;
        currentEnergy = maxEnergy;
        currentTimerValue = planningTime;

        // Reset UI
        UpdateEnergyUI();
        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);
        if (timerText) timerText.gameObject.SetActive(true);

        SpawnDeck();

        StartCoroutine(StartPlanningPhaseSequence());
    }

    void SpawnDeck()
    {
        foreach (Transform t in handArea) Destroy(t.gameObject);

        List<CardData> source = new List<CardData>(allCardsDatabase);
        int countToSpawn = spawnEntireDatabase ? source.Count : cardsPerHand;

        for (int i = 0; i < countToSpawn; i++)
        {
            if (source.Count == 0) break;
            CardData data = source[i];
            GameObject cardObj = Instantiate(cardPrefab, handArea);
            CardUI ui = cardObj.GetComponent<CardUI>();
            ui.Setup(data);

            CardDisplay d = cardObj.GetComponent<CardDisplay>();
            if (d) { d.cardData = data; d.currentAttack = data.attackValue; }
        }
    }

    public void ToggleCardSelection(CardUI card, bool isSelected)
    {
        if (!isPlanningPhase) return;
        if (isSelected) selectedCardsUI.Add(card);
        else selectedCardsUI.Remove(card);
        UpdateEnergyUI();
    }

    void OnLockInPressed()
    {
        if (inputLocked) return;

        int cost = 0;
        foreach (CardUI c in selectedCardsUI) cost += c.cardData.energyCost;

        if (cost > currentEnergy)
        {
            StartCoroutine(ShowWarningSequence());
            return;
        }

        // Lock Logic
        currentEnergy -= cost;
        UpdateEnergyUI();
        isPlanningPhase = false;
        inputLocked = true;
        lockInButton.gameObject.SetActive(false);
        if (timerText) timerText.gameObject.SetActive(false);

        // Move SELECTED cards to Locked Area
        foreach (CardUI c in selectedCardsUI)
        {
            c.transform.SetParent(lockedHandArea);
            c.selectionBorder.SetActive(false);
            c.SetLockedState(true);
        }
        selectedCardsUI.Clear();

        // Clear Hand (Move unused to deck pile)
        List<Transform> unusedCards = new List<Transform>();
        foreach (Transform t in handArea) unusedCards.Add(t);

        foreach (Transform t in unusedCards)
        {
            CardUI c = t.GetComponent<CardUI>();
            if (c != null)
            {
                c.transform.SetParent(deckPileArea);
                c.SwitchToDeckMode(false);
                c.transform.localPosition = Vector3.zero;
                c.transform.localScale = Vector3.zero;
            }
        }

        // Trigger AI
        if (BakunawaAI.Instance) BakunawaAI.Instance.SinglePlayerLockIn();

        StartDicePhase();
    }

    // --- DICE & BANNERS ---
    void StartDicePhase()
    {
        if (dicePanel)
        {
            dicePanel.SetActive(true);
            rollButton.interactable = true;
        }
        else
        {
            FinalizeTurnOrder(true);
        }
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
        int pRoll = 1, eRoll = 1;

        while (elapsed < duration)
        {
            pRoll = Random.Range(1, 7);
            eRoll = Random.Range(1, 7);
            if (diceSprites.Count >= 6)
            {
                playerDiceImg.sprite = diceSprites[pRoll - 1];
                enemyDiceImg.sprite = diceSprites[eRoll - 1];
            }
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.5f);

        if (pRoll > eRoll)
        {
            if (turnChoicePanel) { turnChoicePanel.SetActive(true); dicePanel.SetActive(false); }
            else FinalizeTurnOrder(true);
        }
        else if (eRoll > pRoll)
        {
            dicePanel.SetActive(false);
            FinalizeTurnOrder(false);
        }
        else
        {
            rollButton.interactable = true;
        }
    }

    void FinalizeTurnOrder(bool pFirst)
    {
        playerGoesFirst = pFirst;
        if (turnChoicePanel) turnChoicePanel.SetActive(false);
        if (dicePanel) dicePanel.SetActive(false);
        StartCoroutine(CombatBannerSequence());
    }

    IEnumerator CombatBannerSequence()
    {
        if (combatBanner)
        {
            combatBanner.SetActive(true);
            if (combatBannerText) combatBannerText.text = playerGoesFirst ? "TRIBESMEN TURN" : "BAKUNAWA TURN";
            yield return new WaitForSeconds(2.0f);
            combatBanner.SetActive(false);
        }
        StartBattlePhase();
    }

    // --- BATTLE ---
    void StartBattlePhase()
    {
        inputLocked = false;
        if (BakunawaAI.Instance) BakunawaAI.Instance.RevealCards();

        if (playerGoesFirst)
        {
            playCardButton.interactable = true;
            playCardButton.gameObject.SetActive(true);
        }
        else
        {
            playCardButton.interactable = false;
            playCardButton.gameObject.SetActive(true);
            StartCoroutine(AiTurnRoutine());
        }
    }

    public void SelectCardForBattle(CardUI card)
    {
        if (inputLocked) return;
        if (currentBattleSelection) currentBattleSelection.selectionBorder.SetActive(false);
        currentBattleSelection = card;
        if (currentBattleSelection) currentBattleSelection.selectionBorder.SetActive(true);
    }

    void OnPlayButtonPressed()
    {
        if (currentBattleSelection == null) return;
        StartCoroutine(PlayerPlayRoutine(currentBattleSelection));
    }

    IEnumerator PlayerPlayRoutine(CardUI card)
    {
        playCardButton.interactable = false;
        inputLocked = true;

        MoveCardToBattle(card, false); // Player Left
        currentBattleSelection = null;

        if (playerGoesFirst)
        {
            // Player attacks -> AI defends
            yield return new WaitForSeconds(1.0f);
            if (BakunawaAI.Instance.HasCards())
            {
                CardUI aiCard = BakunawaAI.Instance.PlayCard();
                MoveCardToBattle(aiCard, true); // AI Right
                ResolveClash(card, aiCard);
            }
            else
            {
                ResolveClash(card, null); // Unopposed
            }
        }
        else
        {
            // AI already attacked -> Player defending
            ResolveClash(card, pendingEnemyCard);
            pendingEnemyCard = null;
            enemyHasPlayed = false;
        }

        yield return new WaitForSeconds(1.0f);
        CleanBattleZone();
        CheckRoundEndOrNextTurn();
    }

    IEnumerator AiTurnRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        if (BakunawaAI.Instance.HasCards())
        {
            CardUI aiCard = BakunawaAI.Instance.PlayCard();
            MoveCardToBattle(aiCard, true); // AI Right
            pendingEnemyCard = aiCard;
            enemyHasPlayed = true;

            inputLocked = false;
            playCardButton.interactable = true;
        }
        else
        {
            // AI Empty, pass to player
            inputLocked = false;
            playCardButton.interactable = true;
        }
    }

    void CheckRoundEndOrNextTurn()
    {
        bool playerEmpty = lockedHandArea.childCount == 0;
        bool aiEmpty = !BakunawaAI.Instance.HasCards();

        if (playerEmpty && aiEmpty)
        {
            if (ScoreManager.Instance) ScoreManager.Instance.ResolveRound();
            CheckFinalScoreWin();

            if (!isGameOver)
            {
                roundNumber++;
                StartNewRound();
            }
        }
        else
        {
            if (playerGoesFirst)
            {
                inputLocked = false;
                playCardButton.interactable = true;
            }
            else
            {
                StartCoroutine(AiTurnRoutine());
            }
        }
    }

    void MoveCardToBattle(CardUI card, bool isEnemy)
    {
        card.transform.SetParent(battleZone);
        card.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        card.transform.localRotation = Quaternion.identity;

        float xOffset = isEnemy ? 150f : -150f;
        card.transform.localPosition = new Vector3(xOffset, 0, 0);

        card.SetLockedState(false);
    }

    void ResolveClash(CardUI p, CardUI e)
    {
        int pAtk = p.cardData.attackValue;
        int eAtk = (e != null) ? e.cardData.attackValue : 0;
        Debug.Log($"Clash: Player {pAtk} vs AI {eAtk}");
        if (ScoreManager.Instance) ScoreManager.Instance.ResolveClash(pAtk, eAtk);
    }

    void CleanBattleZone()
    {
        foreach (Transform t in battleZone)
        {
            t.SetParent(discardPileArea);
            t.localPosition = Vector3.zero;
            t.localScale = Vector3.one * discardScale;
        }
    }

    // --- UI HELPERS ---
    public void UpdateEnergyUI()
    {
        int cost = 0;
        foreach (CardUI c in selectedCardsUI) cost += c.cardData.energyCost;
        int displayEnergy = currentEnergy - cost;

        if (energySlider) energySlider.value = displayEnergy;
        if (energyText) energyText.text = displayEnergy + "/" + maxEnergy;
    }

    public void UpdateTimerUI(float time)
    {
        if (timerText)
        {
            int min = Mathf.FloorToInt(time / 60F);
            int sec = Mathf.FloorToInt(time % 60F);
            timerText.text = string.Format("{0}:{1:00}", min, sec);
            timerText.color = time <= 10 ? Color.red : Color.white;
        }
    }

    public void UpdateRoundUI()
    {
        if (roundCounterText != null) roundCounterText.text = roundNumber.ToString();
    }

    IEnumerator ShowWarningSequence()
    {
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        warningText.gameObject.SetActive(false);
    }

    void CheckFinalScoreWin()
    {
        if (!ScoreManager.Instance) return;
        int s = ScoreManager.Instance.currentTowerScore;
        if (s >= 5) TriggerGameOver("Bakunawa");
        else if (s <= -5) TriggerGameOver("Tribesmen");
        else if (roundNumber >= 10) TriggerGameOver("Draw");
    }

    public void TriggerGameOver(string w)
    {
        if (isGameOver) return;
        isGameOver = true;
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            if (winnerText) winnerText.text = w + " WINS!";
        }
    }

    public void ShowCardDetails(CardData d) { detailsPanel.SetActive(true); detailName.text = d.cardName; detailDesc.text = d.description; detailImage.sprite = d.cardArt; detailCost.text = d.energyCost.ToString(); detailAttack.text = d.attackValue.ToString(); }
    public void HideCardDetails() { detailsPanel.SetActive(false); }

    // --- PLANNING BANNER SEQUENCE (Fixed) ---
    IEnumerator StartPlanningPhaseSequence()
    {
        inputLocked = true;
        isPlanningPhase = true;

        if (planningBanner != null)
        {
            planningBanner.SetActive(true);

            if (planningBannerText != null)
            {
                planningBannerText.text = "ROUND " + roundNumber;
                yield return new WaitForSeconds(1.5f);
                planningBannerText.text = "PLANNING PHASE";
                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }
            planningBanner.SetActive(false);
        }
        else yield return new WaitForSeconds(1.0f);

        inputLocked = false;
        UpdateEnergyUI();
    }

    // --- Helper for Fade (Optional, kept for compatibility if referenced elsewhere) ---
    IEnumerator FadeTextInAndOut(Text t, float d) { yield return new WaitForSeconds(d); }
}