using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

public class MultiplayerGameManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerGameManager Instance;

    [Header("Game Settings")]
    [Tooltip("Set this to 4 for real game, or 1-2 for testing")]
    public int playersNeededToStart = 2;

    [Header("UI References")]
    public Transform handArea;
    public Transform lockedArea;
    public Transform centerStage;
    public Text statusText;
    public Text energyText;
    public Slider energySlider;
    public Button lockInButton;
    public GameObject cardPrefab;

    [Header("Game State")]
    public int maxTeamEnergy = 10;
    public int currentTeamEnergy;
    public bool isPlanningPhase = true;

    // --- DATA ---
    private string myRole;
    private bool isTribesman = false;
    private List<CardUI> selectedCards = new List<CardUI>();

    // --- HOST TRACKING ---
    private List<int> executionQueue = new List<int>();
    private Dictionary<int, List<string>> playerPendingCards = new Dictionary<int, List<string>>();
    private int readyPlayersCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 1. Identify Role
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            myRole = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
        }
        else
        {
            myRole = "Spectator";
        }

        if (statusText != null) statusText.text = "Role: " + myRole;

        // 2. Identify Team
        if (myRole == "Bakunawa") isTribesman = false;
        else isTribesman = true;

        // 3. Setup Energy
        currentTeamEnergy = maxTeamEnergy;
        UpdateEnergyUI();

        // 4. Spawn My Cards
        SpawnMyHand();
    }

    void SpawnMyHand()
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager Missing! Make sure _NetworkController exists.");
            return;
        }

        // DEBUG: Print what we are trying to load
        Debug.Log("Attempting to load deck for role: " + myRole);

        List<CardData> myDeck = DeckManager.Instance.GetDeckByRole(myRole);

        if (myDeck.Count == 0)
        {
            Debug.LogWarning("Deck is empty for role: " + myRole + ". Check Inspector in DeckManager!");
        }

        foreach (CardData data in myDeck)
        {
            GameObject cardObj = Instantiate(cardPrefab, handArea);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            cardUI.Setup(data);
        }
    }

    // --- CARD SELECTION ---

    public void OnCardClicked(CardUI card)
    {
        if (!isPlanningPhase) return;

        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
            card.glowOverlay.SetGlowEnabled(false);
        }
        else
        {
            // Simple check: Do we have enough energy?
            int cost = card.GetComponent<CardDisplay>().cardData.energyCost;
            if (currentTeamEnergy >= cost)
            {
                selectedCards.Add(card);
                card.glowOverlay.SetGlowEnabled(true);
            }
        }
    }

    public void OnLockInPressed()
    {
        if (selectedCards.Count == 0 || !isPlanningPhase) return;

        // Calculate Cost
        int totalCost = 0;
        List<string> cardNames = new List<string>();
        foreach (CardUI c in selectedCards)
        {
            totalCost += c.GetComponent<CardDisplay>().cardData.energyCost;
            cardNames.Add(c.GetComponent<CardDisplay>().cardData.cardName);
        }

        if (currentTeamEnergy >= totalCost)
        {
            // LOCK IN
            isPlanningPhase = false;
            if (lockInButton) lockInButton.interactable = false;
            if (statusText) statusText.text = "Waiting for other players...";

            // Send RPC
            photonView.RPC("RPC_PlayerLockedIn", RpcTarget.All,
                PhotonNetwork.LocalPlayer.ActorNumber,
                cardNames.ToArray(),
                totalCost,
                isTribesman);

            // Hide local cards
            foreach (CardUI c in selectedCards) c.gameObject.SetActive(false);
            selectedCards.Clear();
        }
    }

    // --- NETWORK LOGIC ---

    [PunRPC]
    void RPC_PlayerLockedIn(int actorNumber, string[] cardNames, int cost, bool isTribesman)
    {
        // Visuals
        if (isTribesman)
        {
            currentTeamEnergy -= cost;
            UpdateEnergyUI();
            foreach (string s in cardNames) SpawnLockedVisual(s);
        }

        // HOST LOGIC: Add to Queue
        if (PhotonNetwork.IsMasterClient)
        {
            // Add to execution order (First Come First Serve)
            if (!executionQueue.Contains(actorNumber)) executionQueue.Add(actorNumber);

            // Store cards
            if (!playerPendingCards.ContainsKey(actorNumber))
                playerPendingCards[actorNumber] = new List<string>();

            playerPendingCards[actorNumber].AddRange(cardNames);

            // Count Ready Players
            readyPlayersCount++;

            Debug.Log("Players Ready: " + readyPlayersCount + "/" + playersNeededToStart);

            // CHECK IF EVERYONE IS READY
            if (readyPlayersCount >= playersNeededToStart)
            {
                StartCoroutine(HostExecutionLoop());
            }
        }
    }

    // --- EXECUTION LOOP (The "Turn" logic) ---

    IEnumerator HostExecutionLoop()
    {
        photonView.RPC("RPC_UpdateStatus", RpcTarget.All, "Battle Phase Starting!");
        yield return new WaitForSeconds(1.5f);

        bool cardsRemaining = true;
        int roundIndex = 0; // 1st card, then 2nd card...

        while (cardsRemaining)
        {
            cardsRemaining = false;

            // Loop through players in the ORDER they locked in
            foreach (int actorNum in executionQueue)
            {
                // Check if this player has a card for this round index
                if (playerPendingCards.ContainsKey(actorNum) && playerPendingCards[actorNum].Count > roundIndex)
                {
                    string cardToPlay = playerPendingCards[actorNum][roundIndex];

                    // HOST ROLLS DICE
                    int diceResult = Random.Range(1, 7);

                    // Execute RPC
                    photonView.RPC("RPC_ExecuteCard", RpcTarget.All, cardToPlay, actorNum, diceResult);

                    // Wait for Animation (3 seconds)
                    yield return new WaitForSeconds(3.0f);

                    cardsRemaining = true;
                }
            }

            roundIndex++;
        }

        photonView.RPC("RPC_EndTurn", RpcTarget.All);
    }

    [PunRPC]
    void RPC_ExecuteCard(string cardName, int ownerActor, int diceRoll)
    {
        // Clear previous center card
        foreach (Transform child in centerStage) Destroy(child.gameObject);

        // Find Card Data
        CardData cardData = FindCardDataByName(cardName);

        if (cardData != null)
        {
            GameObject visual = Instantiate(cardPrefab, centerStage);
            visual.GetComponent<CardUI>().Setup(cardData);

            if (statusText)
                statusText.text = "Player " + ownerActor + " plays " + cardName + "\nRoll: " + diceRoll;
        }
    }

    [PunRPC]
    void RPC_UpdateStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    [PunRPC]
    void RPC_EndTurn()
    {
        if (statusText) statusText.text = "Planning Phase";

        isPlanningPhase = true;
        if (lockInButton) lockInButton.interactable = true;

        currentTeamEnergy = maxTeamEnergy;
        UpdateEnergyUI();

        foreach (Transform child in centerStage) Destroy(child.gameObject);
        foreach (Transform child in lockedArea) Destroy(child.gameObject);

        if (PhotonNetwork.IsMasterClient)
        {
            executionQueue.Clear();
            playerPendingCards.Clear();
            readyPlayersCount = 0;
        }
    }

    // --- HELPERS ---

    void SpawnLockedVisual(string name)
    {
        GameObject icon = new GameObject("LockedCard");
        icon.transform.SetParent(lockedArea);
        Image img = icon.AddComponent<Image>();
        img.color = Color.cyan;
        LayoutElement le = icon.AddComponent<LayoutElement>();
        le.preferredWidth = 50; le.preferredHeight = 70;
    }

    void UpdateEnergyUI()
    {
        if (energyText) energyText.text = currentTeamEnergy + "/" + maxTeamEnergy;
        if (energySlider) energySlider.value = currentTeamEnergy;
    }

    CardData FindCardDataByName(string name)
    {
        if (DeckManager.Instance == null) return null;

        var allCards = new List<CardData>();
        allCards.AddRange(DeckManager.Instance.mandirigmaDeck);
        allCards.AddRange(DeckManager.Instance.tagapangalagaDeck);
        allCards.AddRange(DeckManager.Instance.albularyoDeck);
        allCards.AddRange(DeckManager.Instance.bakunawaDeck);

        return allCards.Find(c => c.cardName == name);
    }
}