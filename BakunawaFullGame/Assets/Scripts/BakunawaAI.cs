using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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
        SetupBattleZone();
        Invoke("SpawnHand", 0.5f);
    }

    void SetupBattleZone()
    {
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
            hlg.spacing = 20; 
        }
    }

    void SpawnHand()
    {
        foreach (CardData card in aiDeck)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.isEnemy = true;
            ui.Setup(card);
            ui.SwitchToDeckMode(true);

            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.cardData = card;
                display.currentAttack = card.attackValue;
            }
        }
    }

    // --- NEW: REVEAL LOCKED CARDS (For Gabayan ng Ninuno) ---
    public void RevealLockedCards()
    {
        foreach (Transform child in lockedArea)
        {
            CardUI card = child.GetComponent<CardUI>();
            if (card != null)
            {
                // Flip face up
                card.SwitchToDeckMode(false);
                Debug.Log("Bakunawa locked card revealed!");
            }
        }
    }
    // --------------------------------------------------------

    public void LockInPlan()
    {
        myHand.Clear();
        foreach (Transform child in handArea)
        {
            CardUI c = child.GetComponent<CardUI>();
            if (c != null) myHand.Add(c);
        }

        int strategy = Random.Range(0, 3);
        if (strategy == 0) myHand.Sort((a, b) => GetCardCost(b).CompareTo(GetCardCost(a)));
        else if (strategy == 1) myHand.Sort((a, b) => GetCardCost(a).CompareTo(GetCardCost(b)));
        else ShuffleList(myHand);

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

        // Capture Start Position (World Space)
        Vector3 startPos = cardToPlay.transform.position;

        cardToPlay.transform.SetParent(battleZone);
        
        // Setup for Animation: maintain position, ignore layout for now
        LayoutElement le = cardToPlay.GetComponent<LayoutElement>();
        if (le == null) le = cardToPlay.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        // Force position to start (overriding any auto-snap)
        cardToPlay.transform.position = startPos;
        cardToPlay.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        // Keep original rotation for now, animate later
        // cardToPlay.transform.rotation = Quaternion.identity; 

        cardToPlay.SetLockedState(false);
        cardToPlay.ResetToHandMode();

        // Note: The caller (HandManager) handles triggering the animation coroutine
        // to coordinate with other events (like waiting for player), 
        // OR we can trigger a default one if needed. 
        // For now, we leave it 'floating' at startPos with ignoreLayout=true.

        return cardToPlay;
    }

    public IEnumerator AnimateCurveToBoard(CardUI card, float duration = 0.5f)
    {
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        
        // Target is CENTER of battle zone
        Vector3 targetPos = battleZone.position;
        Quaternion targetRot = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Smooth step
            t = t * t * (3f - 2f * t);

            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            card.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        card.transform.position = targetPos;
        card.transform.rotation = targetRot;
        card.transform.localPosition = Vector3.zero; // Ensure centered locally

        // Keep ignoreLayout TRUE - clash animation will handle final positioning
        LayoutElement le = card.GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;
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