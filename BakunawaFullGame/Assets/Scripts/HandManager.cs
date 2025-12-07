using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Areas")]
    public GameObject cardPrefab;
    public Transform handArea;
    public Transform lockedHandArea;
    public Transform deckPileArea;
    public Transform battleZone;
    public Transform discardPileArea;

    [Header("UI Buttons")]
    public Button lockInButton;
    public Button playCardButton;

    [Header("Settings")]
    public float playCardScale = 1.2f;
    public float discardScale = 0.8f;

    [Header("Details UI")]
    public GameObject detailsPanel;
    public Text detailName;
    public Text detailDesc;

    [Header("Data")]
    public List<CardData> myDeck;

    private List<CardUI> selectedCardsUI = new List<CardUI>();

    public bool isPlanningPhase = true;
    private CardUI currentBattleSelection;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        detailsPanel.SetActive(false);
        lockInButton.onClick.AddListener(OnLockInPressed);
        playCardButton.onClick.AddListener(OnPlayButtonPressed);

        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);

        SpawnDeck();
    }

    void SpawnDeck()
    {
        foreach (CardData card in myDeck)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);

            // 1. Setup Visuals (Your existing UI script)
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.Setup(card);

            // 2. Setup Logic (The script for ScoreManager)
            // MAKE SURE 'CardDisplay' IS ADDED TO YOUR CARD PREFAB!
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.cardData = card;
                display.currentAttack = card.attackValue;
            }
        }
    }

    void OnLockInPressed()
    {
        if (selectedCardsUI.Count == 0) return;

        isPlanningPhase = false;
        lockInButton.gameObject.SetActive(false);
        playCardButton.gameObject.SetActive(true);

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

        if (BakunawaAI.Instance != null)
        {
            BakunawaAI.Instance.LockInPlan();
        }
    }

    public void SelectCardForBattle(CardUI card)
    {
        if (currentBattleSelection != null)
        {
            currentBattleSelection.selectionBorder.SetActive(false);
        }
        currentBattleSelection = card;
        currentBattleSelection.selectionBorder.SetActive(true);
    }

    void OnPlayButtonPressed()
    {
        if (currentBattleSelection == null) return;

        // 1. Move Player Card
        currentBattleSelection.transform.SetParent(battleZone);
        currentBattleSelection.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        currentBattleSelection.transform.localRotation = Quaternion.identity;
        currentBattleSelection.SetLockedState(false);
        currentBattleSelection.selectionBorder.SetActive(false);

        // 2. Cache the card so we can use it for scoring later
        CardUI playerCard = currentBattleSelection;
        currentBattleSelection = null;

        playCardButton.interactable = false;

        // 3. Pass the player's card to the sequence
        StartCoroutine(BakunawaResponseSequence(playerCard));
    }

    IEnumerator BakunawaResponseSequence(CardUI playerCard)
    {
        yield return new WaitForSeconds(1.0f);

        // 1. Bakunawa attempts to play
        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            // --- Get the enemy card returned by PlayCard() ---
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();

            // --- SCORING LOGIC ---
            if (ScoreManager.Instance != null)
            {
                int pAtk = GetCardAttack(playerCard);
                int eAtk = GetCardAttack(enemyCard);
                ScoreManager.Instance.ResolveClash(pAtk, eAtk);
            }
        }
        else
        {
            // Bakunawa had no cards? Player gets a free hit!
            if (ScoreManager.Instance != null)
            {
                int pAtk = GetCardAttack(playerCard);
                ScoreManager.Instance.ResolveClash(pAtk, 0); // Enemy attack is 0
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 2. Check Player Status
        if (lockedHandArea.childCount > 0)
        {
            playCardButton.interactable = true;
        }
        else
        {
            if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
            {
                Debug.Log("Player finished, Bakunawa finishing hand...");
                StartCoroutine(BakunawaSoloPlaySequence());
            }
            else
            {
                Debug.Log("All cards played! Ending round...");
                StartCoroutine(EndRoundSequence());
            }
        }
    }

    // --- LOOP for Bakunawa to finish playing (Free Hits) ---
    IEnumerator BakunawaSoloPlaySequence()
    {
        while (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            yield return new WaitForSeconds(1.0f);

            CardUI enemyCard = BakunawaAI.Instance.PlayCard();

            // --- SCORE LOGIC (Player has 0 defense here) ---
            if (ScoreManager.Instance != null)
            {
                int eAtk = GetCardAttack(enemyCard);
                ScoreManager.Instance.ResolveClash(0, eAtk);
            }

            yield return new WaitForSeconds(0.5f);
        }

        StartCoroutine(EndRoundSequence());
    }

    // Helper to get attack value safely
    int GetCardAttack(CardUI card)
    {
        if (card == null) return 0;

        // 1. Try to get it from CardDisplay first (Logic)
        CardDisplay display = card.GetComponent<CardDisplay>();
        if (display != null)
        {
            return display.currentAttack;
        }

        // 2. Fallback to reading the text (Visual)
        if (card.attackText != null && int.TryParse(card.attackText.text, out int val))
        {
            return val;
        }
        return 0;
    }

    IEnumerator EndRoundSequence()
    {
        yield return new WaitForSeconds(2.0f);

        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform child in battleZone)
        {
            CardUI card = child.GetComponent<CardUI>();
            if (card != null) playedCards.Add(card);
        }

        foreach (CardUI card in playedCards)
        {
            MoveToPile(card, discardPileArea, true);
        }

        if (BakunawaAI.Instance != null)
        {
            BakunawaAI.Instance.CleanupRound();
        }

        yield return new WaitForSeconds(1.0f);
        StartNextRound();
    }

    void StartNextRound()
    {
        Debug.Log("Starting Next Round...");

        if (deckPileArea.childCount > 0)
        {
            List<CardUI> unusedCards = new List<CardUI>();
            foreach (Transform child in deckPileArea)
            {
                CardUI card = child.GetComponent<CardUI>();
                if (card != null) unusedCards.Add(card);
            }
            foreach (CardUI card in unusedCards) ReturnCardToHand(card);
        }
        else
        {
            List<CardUI> discardedCards = new List<CardUI>();
            foreach (Transform child in discardPileArea)
            {
                CardUI card = child.GetComponent<CardUI>();
                if (card != null) discardedCards.Add(card);
            }
            ShuffleList(discardedCards);
            foreach (CardUI card in discardedCards) ReturnCardToHand(card);
        }

        isPlanningPhase = true;
        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);
        playCardButton.interactable = true;
    }

    void ReturnCardToHand(CardUI card)
    {
        card.transform.SetParent(handArea);
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;
        card.ResetToHandMode();
    }

    void MoveToPile(CardUI card, Transform pile, bool faceDown)
    {
        card.transform.SetParent(pile);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = new Vector3(discardScale, discardScale, discardScale);
        card.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
        card.SwitchToDeckMode(faceDown);
    }

    void ShuffleList(List<CardUI> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            CardUI temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void ToggleCardSelection(CardUI cardUI, bool isSelected)
    {
        if (!isPlanningPhase) return;
        if (isSelected) selectedCardsUI.Add(cardUI);
        else selectedCardsUI.Remove(cardUI);
    }

    public void ShowCardDetails(CardData data)
    {
        detailsPanel.SetActive(true);
        detailName.text = data.cardName;
        detailDesc.text = data.description;
    }

    public void HideCardDetails()
    {
        detailsPanel.SetActive(false);
    }
}