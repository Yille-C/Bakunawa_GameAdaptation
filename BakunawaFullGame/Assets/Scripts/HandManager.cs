using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Areas")]
    public GameObject cardPrefab;
    public Transform handArea;         // Start/Hand
    public Transform lockedHandArea;   // Locked (Center)
    public Transform deckPileArea;     // Unused Pile (Right)
    public Transform battleZone;       // Play Zone (Center - NEEDS LAYOUT GROUP)
    public Transform discardPileArea;  // Discard Pile (Left)

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
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.Setup(card);
        }
    }

    void OnLockInPressed()
    {
        if (selectedCardsUI.Count == 0) return;

        isPlanningPhase = false;
        lockInButton.gameObject.SetActive(false);
        playCardButton.gameObject.SetActive(true);

        // 1. Move Selected Cards to Locked Area
        foreach (CardUI card in selectedCardsUI)
        {
            card.transform.SetParent(lockedHandArea);
            card.selectionBorder.SetActive(false);
            // Turn ON Locked Art
            card.SetLockedState(true);
        }

        // 2. Move Remaining Cards to Right Pile
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

        // 1. Move Card to Battle Zone
        currentBattleSelection.transform.SetParent(battleZone);

        // NOTE: We do NOT reset localPosition to zero here anymore.
        // The Horizontal Layout Group on "BattleZone" handles the position!

        currentBattleSelection.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        currentBattleSelection.transform.localRotation = Quaternion.identity;

        // 2. Reveal Card (Turn off Locked Art)
        currentBattleSelection.SetLockedState(false);

        currentBattleSelection.selectionBorder.SetActive(false);
        currentBattleSelection = null;

        // Check for End of Round
        if (lockedHandArea.childCount == 0)
        {
            Debug.Log("All cards played! Ending round...");
            playCardButton.interactable = false;
            StartCoroutine(EndRoundSequence());
        }
    }

    IEnumerator EndRoundSequence()
    {
        yield return new WaitForSeconds(2.0f);

        // Move played cards to Discard Pile
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
            Debug.Log("Deck Empty! Reshuffling Discard Pile...");
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