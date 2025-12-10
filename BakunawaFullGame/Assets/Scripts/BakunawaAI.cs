using UnityEngine;
using System.Collections.Generic;

public class BakunawaAI : MonoBehaviour
{
    public static BakunawaAI Instance;

    [Header("Setup")]
    public GameObject cardPrefab;
    public Transform handArea;
    public Transform lockedArea;
    public Transform deckPileArea;
    public List<CardData> aiDeck;

    private List<CardUI> myHand = new List<CardUI>();
    private List<CardUI> myLockedCards = new List<CardUI>();

    void Awake() { Instance = this; }

    public void SinglePlayerLockIn()
    {
        foreach (Transform t in handArea) Destroy(t.gameObject);
        foreach (Transform t in lockedArea) Destroy(t.gameObject);
        myHand.Clear();
        myLockedCards.Clear();

        foreach (CardData d in aiDeck)
        {
            GameObject g = Instantiate(cardPrefab, handArea);
            CardUI ui = g.GetComponent<CardUI>();
            ui.Setup(d);
            ui.isEnemy = true;
            ui.SwitchToDeckMode(true);
            myHand.Add(ui);
        }

        int energy = 10;

        // Shuffle
        for (int i = 0; i < myHand.Count; i++)
        {
            CardUI temp = myHand[i];
            int r = Random.Range(i, myHand.Count);
            myHand[i] = myHand[r];
            myHand[r] = temp;
        }

        foreach (CardUI c in myHand)
        {
            if (c.cardData.energyCost <= energy)
            {
                c.transform.SetParent(lockedArea);
                c.transform.localPosition = Vector3.zero;
                c.transform.localRotation = Quaternion.identity;
                c.transform.localScale = Vector3.one;
                myLockedCards.Add(c);
                energy -= c.cardData.energyCost;
            }
            else
            {
                c.transform.SetParent(deckPileArea);
            }
        }
    }

    public void RevealCards()
    {
        foreach (CardUI c in myLockedCards)
        {
            c.SwitchToDeckMode(false);
        }
    }

    public bool HasCards()
    {
        return myLockedCards.Count > 0;
    }

    public CardUI PlayCard()
    {
        if (myLockedCards.Count == 0) return null;
        CardUI c = myLockedCards[0];
        myLockedCards.RemoveAt(0);
        return c;
    }
}