using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BakunawaAI : MonoBehaviour
{
    public static BakunawaAI Instance;

    [Header("Areas")]
    public GameObject cardPrefab;
    public Transform handArea;
    public Transform lockedArea;
    public Transform deckPileArea;
    public Transform battleZone;
    public Transform discardPile;

    [Header("Data")]
    public List<CardData> aiDeck;
    public int maxEnergy = 10;

    [Header("Settings")]
    public float playCardScale = 1.2f;
    public float discardScale = 0.8f;

    private List<CardUI> myHand = new List<CardUI>();
    private List<CardUI> myLockedCards = new List<CardUI>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Invoke("SpawnHand", 0.5f);
    }

    void SpawnHand()
    {
        foreach (CardData card in aiDeck)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);

            // 1. Setup Visuals
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.isEnemy = true;
            ui.Setup(card);
            ui.SwitchToDeckMode(true);

            // 2. Setup Logic (ADDED THIS FIX)
            // This ensures ScoreManager can read the enemy's attack!
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.cardData = card;
                display.currentAttack = card.attackValue;
            }
        }
    }

    public void LockInPlan()
    {
        myHand.Clear();
        foreach (Transform child in handArea)
        {
            CardUI c = child.GetComponent<CardUI>();
            if (c != null) myHand.Add(c);
        }

        // Strategy Randomizer
        int strategy = Random.Range(0, 3);
        if (strategy == 0) myHand.Sort((a, b) => GetCardCost(b).CompareTo(GetCardCost(a))); // Tall
        else if (strategy == 1) myHand.Sort((a, b) => GetCardCost(a).CompareTo(GetCardCost(b))); // Swarm
        else ShuffleList(myHand); // Random

        int currentEnergy = 0;
        myLockedCards.Clear();

        foreach (CardUI card in myHand)
        {
            int cost = GetCardCost(card);

            if (currentEnergy + cost <= maxEnergy)
            {
                myLockedCards.Add(card);
                currentEnergy += cost;
            }
            else
            {
                MoveToPile(card, deckPileArea, true);
            }
        }

        foreach (CardUI card in myLockedCards)
        {
            card.transform.SetParent(lockedArea);
            card.SetLockedState(true);
        }
    }

    int GetCardCost(CardUI card)
    {
        if (card.costText != null && int.TryParse(card.costText.text, out int parsedCost))
            return parsedCost;
        return 0;
    }

    public bool HasLockedCards()
    {
        return myLockedCards.Count > 0;
    }

    public CardUI PlayCard()
    {
        if (myLockedCards.Count == 0) return null;

        CardUI cardToPlay = myLockedCards[0];
        myLockedCards.RemoveAt(0);

        cardToPlay.transform.SetParent(battleZone);
        cardToPlay.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        cardToPlay.transform.localRotation = Quaternion.identity;

        cardToPlay.SetLockedState(false);
        cardToPlay.ResetToHandMode();

        return cardToPlay;
    }

    public void CleanupRound()
    {
        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform child in battleZone)
        {
            CardUI card = child.GetComponent<CardUI>();
            if (card != null) playedCards.Add(card);
        }

        foreach (CardUI card in playedCards)
        {
            MoveToPile(card, discardPile, true);
        }

        if (deckPileArea.childCount == 0)
        {
            Debug.Log("Bakunawa Deck Empty! Reshuffling...");
            List<CardUI> discarded = new List<CardUI>();
            foreach (Transform child in discardPile)
            {
                CardUI c = child.GetComponent<CardUI>();
                if (c != null) discarded.Add(c);
            }
            ShuffleList(discarded);
            foreach (CardUI c in discarded)
            {
                c.transform.SetParent(handArea);
                c.ResetToHandMode();
                c.SwitchToDeckMode(true);
            }
        }
        else
        {
            List<CardUI> unused = new List<CardUI>();
            foreach (Transform child in deckPileArea)
            {
                CardUI c = child.GetComponent<CardUI>();
                if (c != null) unused.Add(c);
            }
            foreach (CardUI c in unused)
            {
                c.transform.SetParent(handArea);
            }
        }
    }

    void MoveToPile(CardUI card, Transform pile, bool faceDown)
    {
        if (card == null) return;
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
            int r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = temp;
        }
    }
}