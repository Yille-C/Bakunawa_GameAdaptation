using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Combat Banner")]
    public GameObject combatBanner;   // Drag your Panel (with the image attached) here
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
    private float currentTimer;
    private CardUI currentBattleSelection;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        detailsPanel.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);
        if (combatBanner != null) combatBanner.SetActive(false); // Ensure banner starts hidden

        lockInButton.onClick.AddListener(OnLockInPressed);
        playCardButton.onClick.AddListener(OnPlayButtonPressed);

        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);

        currentTimer = planningTime;
        SpawnDeck();
        SetEnergyUIActive(true);
        UpdateEnergyUI();
    }

    void Update()
    {
        if (isPlanningPhase)
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

    // --- ENERGY SYSTEM ---
    void UpdateEnergyUI()
    {
        int currentUsed = 0;
        foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card);
        int remaining = maxEnergy - currentUsed;

        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = Mathf.Max(0, remaining);
        }
        if (energyText != null)
        {
            energyText.text = remaining.ToString();
            if (remaining < 0) energyText.color = Color.red;
            else energyText.color = Color.white;
        }
    }

    void SetEnergyUIActive(bool isActive)
    {
        if (energySlider != null) energySlider.gameObject.SetActive(isActive);
        if (energyText != null) energyText.gameObject.SetActive(isActive);
    }

    public bool ToggleCardSelection(CardUI cardUI, bool isSelected)
    {
        if (!isPlanningPhase) return false;

        if (isSelected) selectedCardsUI.Add(cardUI);
        else selectedCardsUI.Remove(cardUI);

        UpdateEnergyUI();
        return true;
    }

    IEnumerator ShowWarningSequence()
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1);
            yield return new WaitForSeconds(0.5f);
            float duration = 1.0f;
            float currentTime = 0f;
            while (currentTime < duration)
            {
                float alpha = Mathf.Lerp(1f, 0f, currentTime / duration);
                warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, alpha);
                currentTime += Time.deltaTime;
                yield return null;
            }
            warningText.gameObject.SetActive(false);
        }
    }

    int GetCardCost(CardUI card)
    {
        if (card == null) return 0;
        CardDisplay display = card.GetComponent<CardDisplay>();
        if (display != null && display.cardData != null) return display.cardData.energyCost;
        if (card.costText != null && int.TryParse(card.costText.text, out int val)) return val;
        return 0;
    }

    // --- MAIN GAME LOOP ---

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
    }

    void OnLockInPressed()
    {
        // 1. Validate Energy
        int currentUsed = 0;
        foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card);

        if (currentUsed > maxEnergy)
        {
            Debug.Log("Cannot Lock In: Not Enough Energy!");
            StartCoroutine(ShowWarningSequence());
            return;
        }

        // 2. Lock Everything
        isPlanningPhase = false;
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

        if (BakunawaAI.Instance != null)
        {
            BakunawaAI.Instance.LockInPlan();
        }

        // 3. START BANNER SEQUENCE (Animation)
        StartCoroutine(CombatBannerSequence());
    }

    // Handles the flashy banner animation
    IEnumerator CombatBannerSequence()
    {
        if (combatBanner != null)
        {
            combatBanner.SetActive(true);

            // Get the CanvasGroup for fading. If it's missing, it will just pop in/out without fade.
            CanvasGroup group = combatBanner.GetComponent<CanvasGroup>();
            if (group != null)
            {
                // Fade In
                group.alpha = 0;
                float fadeSpeed = 2f;
                while (group.alpha < 1)
                {
                    group.alpha += Time.deltaTime * fadeSpeed;
                    yield return null;
                }
            }

            // Wait duration
            yield return new WaitForSeconds(bannerDuration);

            // Fade Out
            if (group != null)
            {
                float fadeSpeed = 2f;
                while (group.alpha > 0)
                {
                    group.alpha -= Time.deltaTime * fadeSpeed;
                    yield return null;
                }
            }

            combatBanner.SetActive(false);
        }
        else
        {
            // Fallback delay if no banner assigned
            yield return new WaitForSeconds(1.0f);
        }

        // 4. Start the Actual Battle Logic
        StartBattlePhase();
    }

    void StartBattlePhase()
    {
        if (lockedHandArea.childCount > 0)
        {
            playCardButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Player has 0 cards! Proceeding to Bakunawa Turn...");
            playCardButton.gameObject.SetActive(false);
            StartCoroutine(BakunawaSoloPlaySequence());
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

        currentBattleSelection.transform.SetParent(battleZone);
        currentBattleSelection.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        currentBattleSelection.transform.localRotation = Quaternion.identity;
        currentBattleSelection.SetLockedState(false);
        currentBattleSelection.selectionBorder.SetActive(false);

        CardUI playerCard = currentBattleSelection;
        currentBattleSelection = null;
        playCardButton.interactable = false;

        StartCoroutine(BakunawaResponseSequence(playerCard));
    }

    IEnumerator BakunawaResponseSequence(CardUI playerCard)
    {
        yield return new WaitForSeconds(1.0f);

        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
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

        if (lockedHandArea.childCount > 0)
        {
            playCardButton.interactable = true;
        }
        else
        {
            if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
            {
                StartCoroutine(BakunawaSoloPlaySequence());
            }
            else
            {
                StartCoroutine(EndRoundSequence());
            }
        }
    }

    IEnumerator BakunawaSoloPlaySequence()
    {
        while (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            yield return new WaitForSeconds(1.0f);
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();
            if (ScoreManager.Instance != null)
            {
                int eAtk = GetCardAttack(enemyCard);
                ScoreManager.Instance.ResolveClash(0, eAtk);
            }
            yield return new WaitForSeconds(0.5f);
        }
        StartCoroutine(EndRoundSequence());
    }

    int GetCardAttack(CardUI card)
    {
        if (card == null) return 0;
        CardDisplay display = card.GetComponent<CardDisplay>();
        if (display != null) return display.currentAttack;
        if (card.attackText != null && int.TryParse(card.attackText.text, out int val)) return val;
        return 0;
    }

    IEnumerator EndRoundSequence()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResolveRound();

        yield return new WaitForSeconds(2.0f);

        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform child in battleZone)
        {
            CardUI card = child.GetComponent<CardUI>();
            if (card != null) playedCards.Add(card);
        }
        foreach (CardUI card in playedCards) MoveToPile(card, discardPileArea, true);

        if (BakunawaAI.Instance != null) BakunawaAI.Instance.CleanupRound();

        yield return new WaitForSeconds(1.0f);
        StartNextRound();
    }

    void StartNextRound()
    {
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
        currentTimer = planningTime;
        if (timerText != null) timerText.color = Color.white;

        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);
        playCardButton.interactable = true;

        selectedCardsUI.Clear();
        SetEnergyUIActive(true);
        UpdateEnergyUI();
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