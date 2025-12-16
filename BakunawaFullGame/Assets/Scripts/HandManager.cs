using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

public class HandManager : MonoBehaviourPunCallbacks
{
    public static HandManager Instance;

    [Header("Game Mode")]
    public bool isMultiplayer = false;
    [Tooltip("For MP Testing: How many players must lock in before battle starts?")]
    public int playersNeededToStart = 2;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Image gameOverImage;
    public Sprite victorySprite;
    public Sprite defeatSprite;

    [Header("Extra Game Over Icons")]
    public Image extraIconDisplay;
    public Sprite bakunawaIconSprite;
    public Sprite tribesmenIconSprite;

    public Text winnerText;
    public Button restartButton;
    public Button mainMenuButton;
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
    public int currentEnergy;

    [Header("Areas")]
    public GameObject cardPrefab;
    public Transform handArea;
    [UnityEngine.Serialization.FormerlySerializedAs("lockedHandArea")]
    public Transform tribeSelectedPanel;
    public Transform tribeLockedPanel;
    public Transform bakunawaLockedPanel;
    public Transform deckPileArea;
    public Transform battleZone;
    public Transform discardPileArea;

    public Transform lockedArea;
    public Transform centerStage;
    public Text statusText;

    [Header("UI Controls")]
    public Button lockInButton;
    public Button playCardButton;
    public Text timerText;

    [Header("Settings")]
    public float playCardScale = 1.2f;
    public float lockedScale = 0.6f;
    public float discardScale = 0.8f;
    public float planningTime = 60f;
    public float tribePanelSpacing = -90f;
    public float clashDuration = 0.5f;

    [Header("Details UI")]
    public GameObject detailsPanel;
    public Text detailName;
    public Text detailDesc;
    public Image detailImage;
    public Text detailCost;
    public Text detailAttack;

    [Header("Hand Pagination")]
    public int cardsPerPage = 5;
    public Button prevPageBtn;
    public Button nextPageBtn;
    public Text pageIndicatorText;
    private int currentPage = 0;

    [Header("Card Visuals")]
    public Sprite tribesmenLockedCardBackSprite;
    public Sprite bakunawaLockedCardBackSprite;
    public GameObject tribesmenLockedCardBackPrefab;
    public GameObject bakunawaLockedCardBackPrefab;



    [Header("Data")]
    public List<CardData> myDeck;

    private List<CardUI> selectedCardsUI = new List<CardUI>();

    public bool isPlanningPhase = true;
    public bool IsInputLocked => inputLocked;
    private bool inputLocked = true;
    private float currentTimer;
    private CardUI currentBattleSelection;

    public int roundNumber = 1;
    private bool playerGoesFirst = true;
    private bool enemyHasPlayedPendingCard = false;
    private CardUI pendingEnemyCard = null;

    public bool alayBuffActive = false;
    public bool alayDebuffActive = false;
    public bool agongPlayedThisRound = false;

    private Image clashDimmer;

    private string myRole;
    private bool isTribesman = false;
    private List<int> executionQueue = new List<int>();
    private Dictionary<int, List<string>> pendingCardsMap = new Dictionary<int, List<string>>();
    private int readyPlayersCount = 0;

    private List<int> tribesmenTurnOrder = new List<int>();
    private List<int> tribesmenLockInOrder = new List<int>();
    private int bakunawaPlayerID = -1;
    private int currentPlannerIndex = 0;
    private int battleTurnIndex = 0;

    private CardUI pendingTribesmanCard = null;
    private CardUI pendingBakunawaCard = null;
    private bool waitingForBakunawaCard = false;
    private bool waitingForTribesmanCard = false;


    Color notificationTribeColor = new Color(0.8f, 0.3f, 0.1f, 1f);
    Color notificationBakunawaColor = new Color(0.1f, 0.5f, 1f, 1f);

    float shakingTimeNorm(float ct, float dur)
    {
        float t = ct / dur;
        return Mathf.Clamp01(t);
    }

    IEnumerator ShowTurnNotificationRoutine(bool isPlayerTurn)
    {
        string text = isPlayerTurn ? "YOUR TURN" : "BAKUNAWA'S TURN";
        Color color = isPlayerTurn ? notificationTribeColor : notificationBakunawaColor;

        if (TurnNotificationUI.Instance == null)
        {
            GameObject obj = new GameObject("TurnNotificationUI");
            obj.AddComponent<TurnNotificationUI>();
        }

        yield return StartCoroutine(TurnNotificationUI.Instance.PlayTurnNotification(text, color));
    }

    void Awake()
    {
        Instance = this;
        EnsureDimmer();
        if (TurnNotificationUI.Instance == null)
        {
            GameObject obj = new GameObject("TurnNotificationUI");
            obj.AddComponent<TurnNotificationUI>();
        }
    }

    void EnsureDimmer()
    {
        if (clashDimmer != null) return;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;

        if (rootCanvas != null)
        {
            GameObject dimObj = new GameObject("ClashDimmer");
            dimObj.transform.SetParent(rootCanvas.transform, false);
            dimObj.transform.SetAsFirstSibling();

            clashDimmer = dimObj.AddComponent<Image>();
            clashDimmer.color = new Color(0, 0, 0, 0f);
            clashDimmer.raycastTarget = false;

            Canvas dimmerCanvas = dimObj.AddComponent<Canvas>();
            dimmerCanvas.overrideSorting = true;
            dimmerCanvas.sortingOrder = 1999;

            dimObj.AddComponent<GraphicRaycaster>();

            RectTransform rt = dimObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    IEnumerator FadeDimmer(bool fadeIn, float duration = 0.3f)
    {
        if (clashDimmer == null) EnsureDimmer();
        if (clashDimmer == null) yield break;

        if (fadeIn) clashDimmer.transform.SetAsLastSibling();

        float startAlpha = clashDimmer.color.a;
        float targetAlpha = fadeIn ? 0.75f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            clashDimmer.color = new Color(0, 0, 0, a);
            elapsed += Time.deltaTime;
            yield return null;
        }
        clashDimmer.color = new Color(0, 0, 0, targetAlpha);
    }

    IEnumerator AnimateCardClash(CardUI playerCard, CardUI enemyCard)
    {
        StartCoroutine(FadeDimmer(true, 0.4f));

        Transform pOriginalParent = playerCard.transform.parent;
        Transform eOriginalParent = (enemyCard != null) ? enemyCard.transform.parent : null;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        Transform rootT = rootCanvas.transform;

        playerCard.transform.SetParent(rootT, true);
        if (enemyCard != null) enemyCard.transform.SetParent(rootT, true);

        playerCard.enabled = false;
        if (enemyCard != null) enemyCard.enabled = false;

        string rootSortingLayer = rootCanvas.sortingLayerName;

        SetupCardCanvas(playerCard, rootSortingLayer);
        if (enemyCard != null) SetupCardCanvas(enemyCard, rootSortingLayer);

        playerCard.transform.SetAsLastSibling();
        if (enemyCard != null) enemyCard.transform.SetAsLastSibling();

        playerCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        if (enemyCard != null) enemyCard.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);

        Vector3 pStartLocalPos = playerCard.transform.localPosition;
        Vector3 eStartLocalPos = (enemyCard != null) ? enemyCard.transform.localPosition : Vector3.zero;

        Vector3 clashPointLocal = Vector3.zero;
        float verticalOffset = 350f;

        Vector3 pReadyLocalPos = clashPointLocal + new Vector3(0, -verticalOffset, 0f);
        Vector3 eReadyLocalPos = clashPointLocal + new Vector3(0, verticalOffset, 0f);
        if (enemyCard == null) eReadyLocalPos = clashPointLocal;

        float windupTime = 0.4f;
        float elapsed = 0f;

        Quaternion pStartRot = playerCard.transform.localRotation;
        Quaternion eStartRot = (enemyCard != null) ? enemyCard.transform.localRotation : Quaternion.identity;

        Vector3 normalScale = new Vector3(playCardScale, playCardScale, playCardScale);
        Vector3 clashScaleVec = normalScale * 1.3f;

        while (elapsed < windupTime)
        {
            float t = elapsed / windupTime;
            t = t * t * (3f - 2f * t);

            playerCard.transform.localPosition = Vector3.Lerp(pStartLocalPos, pReadyLocalPos, t);
            playerCard.transform.localRotation = Quaternion.Lerp(pStartRot, Quaternion.identity, t);
            playerCard.transform.localScale = Vector3.Lerp(normalScale, clashScaleVec, t);

            if (enemyCard != null)
            {
                enemyCard.transform.localPosition = Vector3.Lerp(eStartLocalPos, eReadyLocalPos, t);
                enemyCard.transform.localRotation = Quaternion.Lerp(eStartRot, Quaternion.identity, t);
                enemyCard.transform.localScale = Vector3.Lerp(normalScale, clashScaleVec, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCard.transform.localScale = clashScaleVec;
        if (enemyCard != null) enemyCard.transform.localScale = clashScaleVec;

        yield return new WaitForSeconds(0.1f);

        float lungeTime = 0.15f;
        elapsed = 0f;

        float actualHeight = 250f;
        RectTransform pRect = playerCard.GetComponent<RectTransform>();
        if (pRect != null) actualHeight = pRect.rect.height * clashScaleVec.y;

        float cardHalfHeight = actualHeight / 2f;
        float offset = cardHalfHeight;

        Vector3 pImpactLocalPos = clashPointLocal + new Vector3(0, -offset, 0f);
        Vector3 eImpactLocalPos = clashPointLocal + new Vector3(0, offset, 0f);

        Vector3 stretchScale = new Vector3(clashScaleVec.x * 0.8f, clashScaleVec.y * 1.2f, clashScaleVec.z);

        while (elapsed < lungeTime)
        {
            float t = elapsed / lungeTime;
            t = t * t * t;

            playerCard.transform.localPosition = Vector3.Lerp(pReadyLocalPos, pImpactLocalPos, t);
            playerCard.transform.localScale = Vector3.Lerp(clashScaleVec, stretchScale, t);

            if (enemyCard != null)
            {
                enemyCard.transform.localPosition = Vector3.Lerp(eReadyLocalPos, eImpactLocalPos, t);
                enemyCard.transform.localScale = Vector3.Lerp(clashScaleVec, stretchScale, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCard.transform.localPosition = pImpactLocalPos;
        if (enemyCard != null) enemyCard.transform.localPosition = eImpactLocalPos;

        playerCard.SetBroken(true);
        if (enemyCard != null) enemyCard.SetBroken(true);

        Vector3 sparkPos = rootT.position;
        if (enemyCard != null) sparkPos = Vector3.Lerp(playerCard.transform.position, enemyCard.transform.position, 0.5f);
        CreateImpactSparks(sparkPos);

        float recoilTime = 0.3f;
        float shakeTimer = 0f;
        Vector3 pRecoilLocalPos = pImpactLocalPos + new Vector3(0, -50f, 0);
        Vector3 eRecoilLocalPos = eImpactLocalPos + new Vector3(0, 50f, 0);

        while (shakeTimer < recoilTime)
        {
            float t = shakeTimer / recoilTime;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            Vector3 currentRecoilP = Vector3.Lerp(pImpactLocalPos, pRecoilLocalPos, t);
            Vector3 currentRecoilE = Vector3.Lerp(eImpactLocalPos, eRecoilLocalPos, t);

            if (shakeTimer < 0.25f)
            {
                float strength = 1f - (shakeTimer / 0.25f);
                Vector3 cardJitter = (Vector3)(Random.insideUnitCircle * 15f * strength);

                playerCard.transform.localPosition = currentRecoilP + cardJitter;
                if (enemyCard != null) enemyCard.transform.localPosition = currentRecoilE + cardJitter;
            }
            else
            {
                playerCard.transform.localPosition = currentRecoilP;
                if (enemyCard != null) enemyCard.transform.localPosition = currentRecoilE;
            }
            playerCard.transform.localScale = clashScaleVec;
            if (enemyCard != null) enemyCard.transform.localScale = clashScaleVec;

            shakeTimer += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(FadeDimmer(false, 0.4f));

        if (pOriginalParent != null) playerCard.transform.SetParent(pOriginalParent, true);
        else playerCard.transform.SetParent(battleZone, true);

        if (enemyCard != null)
        {
            if (eOriginalParent != null) enemyCard.transform.SetParent(eOriginalParent, true);
            else enemyCard.transform.SetParent(battleZone, true);
        }

        CleanupCardCanvas(playerCard);
        if (enemyCard != null) CleanupCardCanvas(enemyCard);

        playerCard.enabled = true;
        if (enemyCard != null) enemyCard.enabled = true;

        if (playerCard.transform.parent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerCard.transform.parent as RectTransform);
    }

    void SetupCardCanvas(CardUI card, string sortLayer)
    {
        Canvas c = card.GetComponent<Canvas>();
        if (c == null) c = card.gameObject.AddComponent<Canvas>();
        c.enabled = true;
        c.overrideSorting = true;
        c.sortingLayerName = sortLayer;
        c.sortingOrder = 2000;

        if (card.GetComponent<GraphicRaycaster>() == null) card.gameObject.AddComponent<GraphicRaycaster>();
    }

    void CleanupCardCanvas(CardUI card)
    {
        Canvas c = card.GetComponent<Canvas>();
        if (c != null) c.overrideSorting = false;
    }

    void Start()
    {
        tribePanelSpacing = -90f;
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

        if (prevPageBtn != null)
        {
            prevPageBtn.onClick.AddListener(PrevHandPage);
            EnsureButtonAnimation(prevPageBtn);
        }
        if (nextPageBtn != null)
        {
            nextPageBtn.onClick.AddListener(NextHandPage);
            EnsureButtonAnimation(nextPageBtn);
        }

        lockInButton.gameObject.SetActive(true);
        playCardButton.gameObject.SetActive(false);
        roundNumber = 1;
        UpdateRoundUI();
        SetupLayoutGroups();

        currentEnergy = maxEnergy;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            isMultiplayer = true;
            SetupMultiplayer();
        }
        else
        {
            isMultiplayer = false;
            SetupSinglePlayer();
        }
    }

    void SetupLayoutGroups()
    {
        if (tribeSelectedPanel != null) EnsureHorizontalLayout(tribeSelectedPanel);
        if (tribeLockedPanel != null) EnsureHorizontalLayout(tribeLockedPanel);
        if (battleZone != null)
        {
            HorizontalLayoutGroup hlg = battleZone.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = battleZone.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(50, 0, 0, 0);
            hlg.spacing = 20;
        }
    }

    void EnsureHorizontalLayout(Transform t)
    {
        HorizontalLayoutGroup hlg = t.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = t.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = tribePanelSpacing;
    }

    void SetupSinglePlayer()
    {
        Debug.Log("Starting Single Player Mode...");
        SpawnDeck();
        StartCoroutine(StartPlanningPhaseSequence());
    }

    void SetupMultiplayer()
    {
        Debug.Log("Starting Multiplayer Mode...");

        if (handArea == null) { Debug.LogError("HandArea is NULL!"); return; }

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
            myRole = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
        else
            myRole = "Mandirigma";

        if (myRole == "Spectator") myRole = "Mandirigma";

        if (myRole == "Tank") myRole = "Tagapangalaga";
        if (myRole == "Attacker") myRole = "Mandirigma";
        if (myRole == "Support") myRole = "Albularyo";

        if (statusText) statusText.text = "Role: " + myRole;

        if (myRole == "Bakunawa") isTribesman = false;
        else isTribesman = true;

        tribesmenTurnOrder.Clear();
        bakunawaPlayerID = -1;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            string pRole = p.CustomProperties.ContainsKey("Role") ? (string)p.CustomProperties["Role"] : "Spectator";

            if (pRole == "Bakunawa")
            {
                bakunawaPlayerID = p.ActorNumber;
            }
            else if (pRole != "Spectator")
            {
                tribesmenTurnOrder.Add(p.ActorNumber);
            }
        }
        tribesmenTurnOrder.Sort();

        if (tribesmenTurnOrder.Count == 0 && isTribesman)
            tribesmenTurnOrder.Add(PhotonNetwork.LocalPlayer.ActorNumber);

        foreach (Transform child in handArea) Destroy(child.gameObject);

        if (DeckManager.Instance != null)
        {
            List<CardData> roleDeck = DeckManager.Instance.GetDeckByRole(myRole);
            if (roleDeck != null && roleDeck.Count > 0)
            {
                foreach (CardData data in roleDeck)
                {
                    if (data == null) continue;
                    GameObject cardObj = Instantiate(cardPrefab, handArea);
                    CardUI ui = cardObj.GetComponent<CardUI>();
                    if (ui) ui.Setup(data);

                    CardDisplay display = cardObj.GetComponent<CardDisplay>();
                    if (display != null) { display.cardData = data; display.currentAttack = data.attackValue; }
                }
            }
        }
        UpdateHandPagination();

        StartCoroutine(StartPlanningPhaseSequenceMP(true));
    }

    IEnumerator StartPlanningPhaseSequenceMP(bool isMyTurn)
    {
        inputLocked = true;
        isPlanningPhase = true;
        currentTimer = planningTime;
        SetEnergyUIActive(false);

        if (planningBanner != null)
        {
            planningBanner.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            planningBanner.SetActive(false);
        }
        else yield return new WaitForSeconds(0.5f);

        SetEnergyUIActive(true);
        UpdateEnergyUI();
        UpdateHandPagination();

        inputLocked = false;
        if (lockInButton) lockInButton.interactable = true;

        if (isTribesman)
        {
            if (statusText) statusText.text = "PLANNING PHASE: Select cards and Lock In!";
        }
        else
        {
            if (statusText) statusText.text = "BAKUNAWA'S PLANNING: Select cards and Lock In!";
        }

        if (timerText) timerText.color = Color.white;
    }

    [PunRPC]


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
        if (roundCounterText != null) roundCounterText.text = roundNumber.ToString();
    }

    public void ShowCardDetails(CardData data)
    {
        detailsPanel.SetActive(true);
        if (detailName != null) detailName.text = data.cardName;
        if (detailDesc != null) detailDesc.text = data.description;
        if (detailImage != null && data.cardArt != null) detailImage.sprite = data.cardArt;
        if (detailCost != null) detailCost.text = data.energyCost.ToString();
        if (detailAttack != null) detailAttack.text = data.attackValue.ToString();
    }

    public void HideCardDetails()
    {
        detailsPanel.SetActive(false);
    }

    public void TriggerGameOver(string winner)
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (winnerText != null)
            {
                winnerText.text = winner == "Tribesmen" ? "VICTORY" : "DEFEAT";
                winnerText.color = winner == "Tribesmen" ? new Color(0f, 1f, 1f) : Color.red;
            }
            if (gameOverImage != null)
            {
                gameOverImage.color = Color.white;
                if (winner == "Tribesmen" && victorySprite != null) gameOverImage.sprite = victorySprite;
                else if (defeatSprite != null) gameOverImage.sprite = defeatSprite;
            }
            if (extraIconDisplay != null)
            {
                extraIconDisplay.gameObject.SetActive(true);
                if (winner == "Tribesmen")
                {
                    if (tribesmenIconSprite != null) extraIconDisplay.sprite = tribesmenIconSprite;
                    else extraIconDisplay.gameObject.SetActive(false);
                }
                else
                {
                    if (bakunawaIconSprite != null) extraIconDisplay.sprite = bakunawaIconSprite;
                    else extraIconDisplay.gameObject.SetActive(false);
                }
            }
        }
    }

    void RestartGame()
    {
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator ShowAlayChoice()
    {
        if (alayChoicePanel != null)
        {
            alayChoicePanel.SetActive(true);
            alayChoiceMade = false;
            while (!alayChoiceMade) { yield return null; }
            alayChoicePanel.SetActive(false);
        }
    }

    void ResolveAlayChoice(bool isBuff)
    {
        if (isBuff) { alayBuffActive = true; alayDebuffActive = false; }
        else { alayBuffActive = false; alayDebuffActive = true; }
        alayChoiceMade = true;
    }

    public void SelectCardForBattle(CardUI card)
    {
        if (inputLocked || !isMultiplayer) return;

        if (playCardButton != null && !playCardButton.interactable) return;

        if (currentBattleSelection != null)
        {
            if (currentBattleSelection.glowOverlay != null)
                currentBattleSelection.glowOverlay.SetGlowEnabled(false);
            if (currentBattleSelection.selectionBorder != null)
                currentBattleSelection.selectionBorder.SetActive(false);
        }

        currentBattleSelection = card;

        if (currentBattleSelection.glowOverlay != null)
        {
            currentBattleSelection.glowOverlay.SetGlowEnabled(true);
        }
        if (currentBattleSelection.selectionBorder != null)
        {
            currentBattleSelection.selectionBorder.SetActive(true);
        }
    }


    void OnPlayButtonPressed()
    {
        if (isMultiplayer)
        {
            if (currentBattleSelection != null) TryPlayCard(currentBattleSelection);
            return;
        }

        if (currentBattleSelection == null) return;
        StartCoroutine(PlayPlayerCardSequence(currentBattleSelection));
    }

    IEnumerator PlayPlayerCardSequence(CardUI cardToPlay)
    {
        playCardButton.interactable = false;
        inputLocked = true;

        CardDisplay display = cardToPlay.GetComponent<CardDisplay>();
        if (display != null && display.cardData != null && display.cardData.effectID == "sup_alay")
        {
            yield return StartCoroutine(ShowAlayChoice());
        }

        if (display != null && display.cardData != null && display.cardData.effectID == "def_agong")
        {
            agongPlayedThisRound = true;
        }

        cardToPlay.transform.SetParent(battleZone);
        cardToPlay.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);
        cardToPlay.transform.localRotation = Quaternion.identity;

        LayoutElement le = cardToPlay.GetComponent<LayoutElement>();
        if (le == null) le = cardToPlay.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        cardToPlay.transform.localPosition = Vector3.zero;
        cardToPlay.SetLockedState(false);

        if (cardToPlay.glowOverlay != null) cardToPlay.glowOverlay.SetGlowEnabledImmediate(false);
        if (cardToPlay.selectionBorder != null) cardToPlay.selectionBorder.SetActive(false);

        RecalculateBattleEffects();

        currentBattleSelection = null;

        if (playerGoesFirst)
        {
            StartCoroutine(BakunawaResponseSequence(cardToPlay));
        }
        else
        {
            if (enemyHasPlayedPendingCard && pendingEnemyCard != null)
            {
                StartCoroutine(ResolveImmediateClash(cardToPlay, pendingEnemyCard));
            }
            else
            {
                StartCoroutine(ResolveImmediateClash(cardToPlay, null));
            }
        }
    }

    IEnumerator DiscardPlayerCardSequence(string sourceEffectName)
    {
        Transform target = null;
        if (deckPileArea.childCount > 0) target = deckPileArea.GetChild(0);
        else if (handArea.childCount > 0) target = handArea.GetChild(0);

        if (target != null)
        {
            CardUI cardUI = target.GetComponent<CardUI>();
            CardDisplay display = target.GetComponent<CardDisplay>();

            if (discardNotifyPanel != null && display != null)
            {
                discardNotifyPanel.SetActive(true);
                if (discardNotifyImage != null) discardNotifyImage.sprite = display.cardData.cardArt;
                if (discardNotifyText != null) discardNotifyText.text = sourceEffectName + " forced you to discard:\n" + display.cardData.cardName;

                CanvasGroup group = discardNotifyPanel.GetComponent<CanvasGroup>();
                if (group != null) { group.alpha = 0; while (group.alpha < 1) { group.alpha += Time.deltaTime * 3f; yield return null; } }
                yield return new WaitForSeconds(discardNotifyDuration);
                if (group != null) { while (group.alpha > 0) { group.alpha -= Time.deltaTime * 3f; yield return null; } }
                discardNotifyPanel.SetActive(false);
            }
            MoveToPile(cardUI, discardPileArea, true);
        }
    }

    IEnumerator ShowAgongRetrieval()
    {
        if (discardPileArea.childCount > 0)
        {
            Transform recoveredCardObj = discardPileArea.GetChild(discardPileArea.childCount - 1);
            CardUI recoveredCardUI = recoveredCardObj.GetComponent<CardUI>();
            CardDisplay recoveredData = recoveredCardObj.GetComponent<CardDisplay>();

            if (recoveredCardUI != null && recoveredData != null)
            {
                if (agongCardImage != null) agongCardImage.sprite = recoveredData.cardData.cardArt;
                if (agongCardName != null) agongCardName.text = recoveredData.cardData.cardName;

                if (agongPanel != null)
                {
                    agongPanel.SetActive(true);
                    CanvasGroup group = agongPanel.GetComponent<CanvasGroup>();
                    if (group != null) { group.alpha = 0; while (group.alpha < 1) { group.alpha += Time.deltaTime * 3f; yield return null; } }
                    yield return new WaitForSeconds(agongDuration);
                    if (group != null) { while (group.alpha > 0) { group.alpha -= Time.deltaTime * 3f; yield return null; } }
                    agongPanel.SetActive(false);
                }
                ReturnCardToHand(recoveredCardUI);
            }
        }
    }

    IEnumerator EndRoundSequence()
    {
        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.ClearTurnIndicators();

        int pScore = 0;
        int bScore = 0;
        bool playerLost = false;
        bool bakunawaWon = false;

        if (ScoreManager.Instance != null)
        {
            pScore = ScoreManager.Instance.playerTotal;
            bScore = ScoreManager.Instance.bakunawaTotal;
            ScoreManager.Instance.ResolveRound();
            if (bScore > pScore) { playerLost = true; bakunawaWon = true; }
        }

        yield return StartCoroutine(ShowResultBanner(pScore, bScore));

        if (playerLost && agongPlayedThisRound)
        {
            yield return StartCoroutine(ShowAgongRetrieval());
        }

        yield return new WaitForSeconds(1.0f);

        List<CardUI> playedCards = new List<CardUI>();
        foreach (Transform child in battleZone) { CardUI card = child.GetComponent<CardUI>(); if (card != null) playedCards.Add(card); }
        foreach (CardUI card in playedCards) MoveToPile(card, discardPileArea, true);
        if (BakunawaAI.Instance != null) BakunawaAI.Instance.CleanupRound();

        yield return new WaitForSeconds(1.0f);
        StartNextRound();
    }

    IEnumerator StartPlanningPhaseSequence()
    {
        inputLocked = true;
        isPlanningPhase = true;
        currentTimer = planningTime;
        SetEnergyUIActive(false);

        alayBuffActive = false;
        alayDebuffActive = false;
        agongPlayedThisRound = false;

        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.ClearTurnIndicators();

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
        UpdateHandPagination();
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

    void OnLockInPressed()
    {
        if (inputLocked) return;

        int currentUsed = 0;
        foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card);

        if (currentUsed > currentEnergy)
        {
            Debug.Log("Cannot Lock In: Not Enough Energy!");
            StartCoroutine(ShowWarningSequence());
            return;
        }

        if (isMultiplayer)
        {
            MultiplayerLockIn(selectedCardsUI, currentUsed);
        }
        else
        {
            SinglePlayerLockIn();
        }
    }

    void SinglePlayerLockIn()
    {
        isPlanningPhase = false;
        inputLocked = true;
        lockInButton.gameObject.SetActive(false);
        if (timerText != null) timerText.text = "";
        SetEnergyUIActive(false);
        UpdateHandPagination();

        foreach (CardUI card in selectedCardsUI)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null && display.cardData.effectID == "def_agong")
                agongPlayedThisRound = true;

            card.transform.SetParent(tribeLockedPanel);
            if (card.glowOverlay != null) card.glowOverlay.SetGlowEnabledImmediate(false);
            if (card.selectionBorder != null) card.selectionBorder.SetActive(false);
            card.SetLockedState(true);
        }

        UpdateContainerSpacing(tribeLockedPanel as RectTransform);

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

    void MultiplayerLockIn(List<CardUI> selectedCards, int totalCost)
    {
        currentEnergy -= totalCost;

        isPlanningPhase = false;
        inputLocked = true;
        if (lockInButton) lockInButton.interactable = false;
        if (statusText) statusText.text = "Waiting for others...";

        List<string> cardNames = new List<string>();
        if (selectedCards == null) selectedCards = new List<CardUI>();

        foreach (CardUI c in selectedCards)
        {
            if (c != null)
            {
                CardDisplay cd = c.GetComponent<CardDisplay>();
                if (cd != null && cd.cardData != null) cardNames.Add(cd.cardData.cardName);
            }
        }

        if (photonView)
            photonView.RPC("RPC_PlayerLockedIn", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, cardNames.ToArray(), totalCost, isTribesman);

        Transform targetPanel = isTribesman ? tribeLockedPanel : bakunawaLockedPanel;

        foreach (CardUI c in selectedCards)
        {
            if (c != null)
            {
                c.transform.SetParent(targetPanel);
                c.SetLockedState(true);
            }
        }

        if (targetPanel != null)
        {
            UpdateContainerSpacing(targetPanel as RectTransform);
        }

        List<Transform> remainingCards = new List<Transform>();
        foreach (Transform child in handArea) remainingCards.Add(child);
        foreach (Transform child in remainingCards) { if (child) child.gameObject.SetActive(false); }

        selectedCardsUI.Clear();
        UpdateEnergyUI();
    }




    [PunRPC]
    void RPC_PlayerLockedIn(int actorNumber, string[] cardNames, int cost, bool isTribesmanPlayer)
    {
        if (!executionQueue.Contains(actorNumber)) executionQueue.Add(actorNumber);
        if (!pendingCardsMap.ContainsKey(actorNumber)) pendingCardsMap[actorNumber] = new List<string>();
        pendingCardsMap[actorNumber].AddRange(cardNames);

        if (isTribesmanPlayer && !tribesmenLockInOrder.Contains(actorNumber))
        {
            tribesmenLockInOrder.Add(actorNumber);
        }

        if (this.isTribesman && isTribesmanPlayer)
        {
            if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                currentEnergy -= cost;
                UpdateEnergyUI();
            }
        }

        if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Transform targetLockedArea = null;
            bool isOpposingTeam = (this.isTribesman != isTribesmanPlayer);

            if (isTribesmanPlayer)
            {
                targetLockedArea = tribeLockedPanel;
            }
            else
            {
                targetLockedArea = bakunawaLockedPanel;
            }

            if (targetLockedArea != null)
            {
                foreach (string s in cardNames)
                {
                    GameObject lockedObj = null;

                    if (isOpposingTeam)
                    {
                        GameObject cardBackPrefab = isTribesmanPlayer ? tribesmenLockedCardBackPrefab : bakunawaLockedCardBackPrefab;
                        Sprite cardBackSprite = isTribesmanPlayer ? tribesmenLockedCardBackSprite : bakunawaLockedCardBackSprite;

                        if (cardBackPrefab != null)
                        {
                            lockedObj = Instantiate(cardBackPrefab, targetLockedArea);
                        }
                        else
                        {
                            lockedObj = Instantiate(cardPrefab, targetLockedArea);

                            Image cardImage = lockedObj.GetComponent<Image>();
                            if (cardImage != null && cardBackSprite != null)
                            {
                                cardImage.sprite = cardBackSprite;
                            }

                            foreach (Transform child in lockedObj.transform)
                            {
                                child.gameObject.SetActive(false);
                            }

                            Text[] texts = lockedObj.GetComponentsInChildren<Text>(true);
                            foreach (Text t in texts) t.gameObject.SetActive(false);

                            Image[] images = lockedObj.GetComponentsInChildren<Image>(true);
                            foreach (Image img in images)
                            {
                                if (img.gameObject != lockedObj) img.gameObject.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        CardData data = null;
                        if (DeckManager.Instance != null)
                        {
                            var all = new List<CardData>();
                            if (DeckManager.Instance.mandirigmaDeck != null) all.AddRange(DeckManager.Instance.mandirigmaDeck);
                            if (DeckManager.Instance.tagapangalagaDeck != null) all.AddRange(DeckManager.Instance.tagapangalagaDeck);
                            if (DeckManager.Instance.albularyoDeck != null) all.AddRange(DeckManager.Instance.albularyoDeck);
                            if (DeckManager.Instance.bakunawaDeck != null) all.AddRange(DeckManager.Instance.bakunawaDeck);
                            data = all.Find(c => c.cardName == s);
                        }

                        lockedObj = Instantiate(cardPrefab, targetLockedArea);

                        CardUI ui = lockedObj.GetComponent<CardUI>();
                        if (ui && data)
                        {
                            ui.Setup(data);
                            ui.SetLockedState(true);
                        }

                        CardDisplay display = lockedObj.GetComponent<CardDisplay>();
                        if (display != null)
                        {
                            display.cardData = data;
                            display.currentAttack = data.attackValue;
                        }
                    }

                    if (lockedObj != null)
                    {
                        Destroy(lockedObj.GetComponent<Button>());
                        lockedObj.transform.localScale = new Vector3(lockedScale, lockedScale, lockedScale);
                    }
                }

                UpdateContainerSpacing(targetLockedArea as RectTransform);
            }
        }

        int totalPlayersNeeded = tribesmenTurnOrder.Count;
        if (bakunawaPlayerID != -1) totalPlayersNeeded++;

        if (executionQueue.Count >= totalPlayersNeeded)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_OpenDicePanel", RpcTarget.All);
            }
        }
        else
        {
            if (statusText)
            {
                statusText.text = $"Waiting for other players... ({executionQueue.Count}/{totalPlayersNeeded})";
            }
        }
    }






    [PunRPC]
    void RPC_OpenDicePanel()
    {
        if (dicePanel != null)
        {
            dicePanel.SetActive(true);
            if (PhotonNetwork.IsMasterClient)
            {
                if (rollButton)
                {
                    rollButton.interactable = true;
                    rollButton.onClick.RemoveAllListeners();
                    rollButton.onClick.AddListener(OnHostRollPressed);
                }
                if (statusText) statusText.text = "You are the Host. Roll the Dice!";
            }
            else
            {
                if (rollButton) rollButton.interactable = false;
                if (statusText) statusText.text = "Waiting for Host to roll...";
            }
        }
    }

    void OnHostRollPressed()
    {
        if (rollButton) rollButton.interactable = false;
        int pRoll = Random.Range(1, 7);
        int eRoll = Random.Range(1, 7);
        while (pRoll == eRoll) { pRoll = Random.Range(1, 7); }

        photonView.RPC("RPC_PerformDiceAnimation", RpcTarget.All, pRoll, eRoll);
    }

    [PunRPC]
    void RPC_PerformDiceAnimation(int finalPRoll, int finalERoll)
    {
        StartCoroutine(MultiplayerDiceRoutine(finalPRoll, finalERoll));
    }

    IEnumerator MultiplayerDiceRoutine(int pRoll, int eRoll)
    {
        float duration = 2.0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            int rP = Random.Range(0, 6);
            int rE = Random.Range(0, 6);
            if (diceSprites != null && diceSprites.Count >= 6)
            {
                playerDiceImg.sprite = diceSprites[rP];
                enemyDiceImg.sprite = diceSprites[rE];
            }
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (diceSprites != null && diceSprites.Count >= 6)
        {
            playerDiceImg.sprite = diceSprites[pRoll - 1];
            enemyDiceImg.sprite = diceSprites[eRoll - 1];
        }

        yield return new WaitForSeconds(1.0f);

        if (PhotonNetwork.IsMasterClient)
        {
            bool tribesmenWonRoll = (pRoll > eRoll);
            photonView.RPC("RPC_ShowTurnChoicePanel", RpcTarget.All, tribesmenWonRoll);
        }
    }

    [PunRPC]
    void RPC_ShowTurnChoicePanel(bool tribesmenWonRoll)
    {
        if (dicePanel) dicePanel.SetActive(false);

        bool shouldShowChoice = (this.isTribesman && tribesmenWonRoll) || (!this.isTribesman && !tribesmenWonRoll);

        if (shouldShowChoice)
        {
            if (turnChoicePanel != null)
            {
                turnChoicePanel.SetActive(true);

                if (goFirstButton != null)
                {
                    goFirstButton.onClick.RemoveAllListeners();
                    goFirstButton.onClick.AddListener(() => OnTurnChoiceMade(true, tribesmenWonRoll));
                }

                if (goSecondButton != null)
                {
                    goSecondButton.onClick.RemoveAllListeners();
                    goSecondButton.onClick.AddListener(() => OnTurnChoiceMade(false, tribesmenWonRoll));
                }
            }

            if (statusText) statusText.text = "Your team won the roll! Choose turn order.";
        }
        else
        {
            if (statusText) statusText.text = "Waiting for opposing team to choose turn order...";
        }
    }

    void OnTurnChoiceMade(bool chooseToGoFirst, bool tribesmenWonRoll)
    {
        if (turnChoicePanel != null) turnChoicePanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient || isTribesman)
        {
            photonView.RPC("RPC_StartBattlePhase", RpcTarget.All, chooseToGoFirst, tribesmenWonRoll);
        }
    }

    [PunRPC]
    void RPC_StartBattlePhase(bool winningTeamGoesFirst, bool tribesmenWonRoll)
    {
        if (turnChoicePanel != null) turnChoicePanel.SetActive(false);
        if (dicePanel) dicePanel.SetActive(false);

        bool tribesmenGoFirst = (tribesmenWonRoll && winningTeamGoesFirst) || (!tribesmenWonRoll && !winningTeamGoesFirst);

        battleTurnIndex = 0;

        StartCoroutine(StartBattlePhaseSequence(tribesmenGoFirst));
    }

    IEnumerator StartBattlePhaseSequence(bool tribesmenGoFirst)
    {
        if (combatBanner != null)
        {
            combatBanner.SetActive(true);

            if (combatBannerText != null)
            {
                combatBannerText.text = "BATTLE PHASE!";
                yield return StartCoroutine(FadeTextInAndOut(combatBannerText, 1.5f));
            }

            combatBanner.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.IsMasterClient)
        {
            int firstPlayerID = GetNextBattlePlayer(tribesmenGoFirst);

            if (firstPlayerID != -1)
            {
                photonView.RPC("RPC_StartPlayerBattleTurn", RpcTarget.All, firstPlayerID);
            }
        }
    }

    int GetNextBattlePlayer(bool tribesmenTurn)
    {
        if (tribesmenTurn)
        {
            if (tribesmenLockInOrder.Count == 0) return -1;

            int attempts = 0;
            while (attempts < tribesmenLockInOrder.Count)
            {
                int actorID = tribesmenLockInOrder[battleTurnIndex % tribesmenLockInOrder.Count];

                if (pendingCardsMap.ContainsKey(actorID) && pendingCardsMap[actorID].Count > 0)
                {
                    battleTurnIndex++;
                    return actorID;
                }

                battleTurnIndex++;
                attempts++;
            }
        }
        else
        {
            if (bakunawaPlayerID != -1)
            {
                if (pendingCardsMap.ContainsKey(bakunawaPlayerID) && pendingCardsMap[bakunawaPlayerID].Count > 0)
                {
                    return bakunawaPlayerID;
                }
            }
        }

        return -1;
    }





    IEnumerator HostStartManualBattle(bool tribesmenFirst)
    {
        photonView.RPC("RPC_UpdateStatus", RpcTarget.All, "Battle Phase Starting!");
        yield return new WaitForSeconds(1.0f);

        battleTurnIndex = 0;

        int firstActor = GetNextValidPlayer();

        if (firstActor != -1)
        {
            photonView.RPC("RPC_StartPlayerBattleTurn", RpcTarget.All, firstActor);
        }
        else
        {
            photonView.RPC("RPC_EndTurnMP", RpcTarget.All);
        }
    }

    int GetNextValidPlayer()
    {
        List<int> allPlayers = new List<int>(tribesmenTurnOrder);
        if (bakunawaPlayerID != -1) allPlayers.Add(bakunawaPlayerID);

        int attempts = 0;
        int max = allPlayers.Count;

        while (attempts < max)
        {
            int actorID = allPlayers[battleTurnIndex % allPlayers.Count];

            if (pendingCardsMap.ContainsKey(actorID) && pendingCardsMap[actorID].Count > 0)
            {
                return actorID;
            }

            battleTurnIndex++;
            attempts++;
        }
        return -1;
    }


    [PunRPC]
    void RPC_StartPlayerBattleTurn(int actorID)
    {
        Debug.Log($"RPC_StartPlayerBattleTurn called. actorID: {actorID}, LocalPlayer: {PhotonNetwork.LocalPlayer.ActorNumber}, isMyTurn: {PhotonNetwork.LocalPlayer.ActorNumber == actorID}");

        if (dicePanel) dicePanel.SetActive(false);
        if (turnChoicePanel) turnChoicePanel.SetActive(false);

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorID)
        {
            inputLocked = false;
            isPlanningPhase = false;

            Debug.Log("It's my turn! Enabling interaction...");

            if (playCardButton)
            {
                playCardButton.gameObject.SetActive(true);
                playCardButton.interactable = true;
                Debug.Log("Play button activated");
            }

            Transform myLockedPanel = isTribesman ? tribeLockedPanel : bakunawaLockedPanel;
            Debug.Log($"My locked panel: {myLockedPanel.name}, card count: {myLockedPanel.childCount}");

            foreach (Transform t in myLockedPanel)
            {
                CardUI c = t.GetComponent<CardUI>();
                if (c)
                {
                    Debug.Log($"Unlocking card: {c.name}");
                    c.SetLockedState(false);

                    Button btn = c.GetComponent<Button>();
                    if (btn == null)
                    {
                        btn = c.gameObject.AddComponent<Button>();
                        btn.transition = Selectable.Transition.None;
                        Debug.Log("Added button component");
                    }
                    btn.interactable = true;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectCardForBattle(c));
                    Debug.Log("Button listener added");
                }
            }

            if (statusText) statusText.text = "YOUR BATTLE TURN: Select and play a card!";
        }
        else
        {
            inputLocked = true;
            isPlanningPhase = false;

            if (playCardButton) playCardButton.gameObject.SetActive(false);

            if (statusText) statusText.text = $"Waiting for Player {actorID} to play...";
            Debug.Log($"Not my turn, waiting for player {actorID}");
        }
    }


    [PunRPC]
    void RPC_UpdateStatus(string message)
    {
        if (statusText) statusText.text = message;
    }

    public bool TryPlayCard(CardUI card)
    {
        if (inputLocked) return false;

        if (isMultiplayer)
        {
            StartCoroutine(MultiplayerPlayCardSequence(card));
            return true;
        }

        if (!playerGoesFirst && !playCardButton.interactable) return false;
        StartCoroutine(PlayPlayerCardSequence(card));
        return true;
    }

    IEnumerator MultiplayerPlayCardSequence(CardUI card)
    {
        inputLocked = true;
        if (playCardButton) playCardButton.interactable = false;

        CardDisplay d = card.GetComponent<CardDisplay>();
        string cardName = (d && d.cardData) ? d.cardData.cardName : "Unknown";

        if (pendingCardsMap.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            pendingCardsMap[PhotonNetwork.LocalPlayer.ActorNumber].Remove(cardName);
        }

        bool isTribesmanCard = isTribesman;

        photonView.RPC("RPC_ExecuteBattleMove", RpcTarget.All, cardName, PhotonNetwork.LocalPlayer.ActorNumber, isTribesmanCard);

        yield return new WaitForSeconds(0.1f);

        currentBattleSelection = null;
    }

    [PunRPC]
    void RPC_ExecuteBattleMove(string cardName, int ownerID, bool isTribesmanCard)
    {
        if (pendingCardsMap.ContainsKey(ownerID))
        {
            pendingCardsMap[ownerID].Remove(cardName);
        }

        CardData cardData = null;
        if (DeckManager.Instance)
        {
            var all = new List<CardData>();
            if (DeckManager.Instance.mandirigmaDeck != null) all.AddRange(DeckManager.Instance.mandirigmaDeck);
            if (DeckManager.Instance.tagapangalagaDeck != null) all.AddRange(DeckManager.Instance.tagapangalagaDeck);
            if (DeckManager.Instance.albularyoDeck != null) all.AddRange(DeckManager.Instance.albularyoDeck);
            if (DeckManager.Instance.bakunawaDeck != null) all.AddRange(DeckManager.Instance.bakunawaDeck);
            cardData = all.Find(c => c.cardName == cardName);
        }

        Transform sourcePanel = isTribesmanCard ? tribeLockedPanel : bakunawaLockedPanel;

        if (ownerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            foreach (Transform t in sourcePanel)
            {
                CardDisplay cd = t.GetComponent<CardDisplay>();
                if (cd != null && cd.cardData != null && cd.cardData.cardName == cardName)
                {
                    Destroy(t.gameObject);
                    break;
                }
            }
        }
        else
        {
            bool isOpposingTeam = (this.isTribesman != isTribesmanCard);

            if (!isOpposingTeam)
            {
                foreach (Transform t in sourcePanel)
                {
                    CardDisplay cd = t.GetComponent<CardDisplay>();
                    if (cd != null && cd.cardData != null && cd.cardData.cardName == cardName)
                    {
                        Destroy(t.gameObject);
                        break;
                    }
                }
            }
            else
            {
                if (sourcePanel.childCount > 0)
                {
                    Destroy(sourcePanel.GetChild(0).gameObject);
                }
            }
        }

        GameObject cardObj = Instantiate(cardPrefab, battleZone);
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardData && cardUI) cardUI.Setup(cardData);

        CardDisplay display = cardObj.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.cardData = cardData;
            display.currentAttack = cardData.attackValue;
        }

        cardUI.isEnemy = !isTribesmanCard;
        cardObj.transform.localScale = new Vector3(playCardScale, playCardScale, playCardScale);

        if (isTribesmanCard)
        {
            pendingTribesmanCard = cardUI;
            waitingForBakunawaCard = true;

            if (statusText) statusText.text = $"Tribesman played {cardName}. Waiting for Bakunawa...";
        }
        else
        {
            pendingBakunawaCard = cardUI;
            waitingForTribesmanCard = true;

            if (statusText) statusText.text = $"Bakunawa played {cardName}. Waiting for Tribesman...";
        }

        if (pendingTribesmanCard != null && pendingBakunawaCard != null)
        {
            StartCoroutine(ExecuteClashSequence());
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(DetermineNextTurn());
            }
        }
    }


    IEnumerator ExecuteClashSequence()
    {
        waitingForBakunawaCard = false;
        waitingForTribesmanCard = false;

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(AnimateCardClash(pendingTribesmanCard, pendingBakunawaCard));

        int tribesmanAttack = GetCardAttack(pendingTribesmanCard);
        int bakunawaAttack = GetCardAttack(pendingBakunawaCard);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResolveClash(tribesmanAttack, bakunawaAttack);
        }

        pendingTribesmanCard = null;
        pendingBakunawaCard = null;

        yield return new WaitForSeconds(1.0f);

        foreach (Transform t in battleZone)
        {
            Destroy(t.gameObject);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DetermineNextTurn());
        }
    }

    IEnumerator DetermineNextTurn()
    {
        yield return new WaitForSeconds(0.3f);

        bool anyTribesmenHasCards = false;
        foreach (int tribeID in tribesmenLockInOrder)
        {
            if (pendingCardsMap.ContainsKey(tribeID) && pendingCardsMap[tribeID].Count > 0)
            {
                anyTribesmenHasCards = true;
                break;
            }
        }

        bool bakunawaHasCards = (bakunawaPlayerID != -1 && pendingCardsMap.ContainsKey(bakunawaPlayerID) && pendingCardsMap[bakunawaPlayerID].Count > 0);

        if (!anyTribesmenHasCards && !bakunawaHasCards)
        {
            photonView.RPC("RPC_EndTurnMP", RpcTarget.All);
            yield break;
        }

        if (pendingTribesmanCard == null && pendingBakunawaCard == null)
        {
            int nextPlayer = -1;

            if (anyTribesmenHasCards)
            {
                nextPlayer = GetNextBattlePlayer(true);
            }

            if (nextPlayer == -1 && bakunawaHasCards)
            {
                nextPlayer = bakunawaPlayerID;
            }

            if (nextPlayer != -1)
            {
                photonView.RPC("RPC_StartPlayerBattleTurn", RpcTarget.All, nextPlayer);
            }
            else
            {
                photonView.RPC("RPC_EndTurnMP", RpcTarget.All);
            }
        }
        else if (pendingTribesmanCard != null && pendingBakunawaCard == null)
        {
            if (bakunawaHasCards)
            {
                photonView.RPC("RPC_StartPlayerBattleTurn", RpcTarget.All, bakunawaPlayerID);
            }
            else
            {
                yield return StartCoroutine(ExecuteClashSequence());
            }
        }
        else if (pendingBakunawaCard != null && pendingTribesmanCard == null)
        {
            if (anyTribesmenHasCards)
            {
                int nextTribesman = GetNextBattlePlayer(true);
                if (nextTribesman != -1)
                {
                    photonView.RPC("RPC_StartPlayerBattleTurn", RpcTarget.All, nextTribesman);
                }
            }
            else
            {
                yield return StartCoroutine(ExecuteClashSequence());
            }
        }
    }



    IEnumerator ResolveMultiplayerClash(CardUI p, CardUI e)
    {
        yield return StartCoroutine(AnimateCardClash(p, e));
    }



    [PunRPC]
    void RPC_EndTurnMP()
    {
        pendingTribesmanCard = null;
        pendingBakunawaCard = null;
        waitingForBakunawaCard = false;
        waitingForTribesmanCard = false;

        if (centerStage) foreach (Transform t in centerStage) Destroy(t.gameObject);
        if (lockedArea) foreach (Transform t in lockedArea) Destroy(t.gameObject);
        if (battleZone) foreach (Transform t in battleZone) Destroy(t.gameObject);
        if (tribeLockedPanel) foreach (Transform t in tribeLockedPanel) Destroy(t.gameObject);
        if (bakunawaLockedPanel) foreach (Transform t in bakunawaLockedPanel) Destroy(t.gameObject);
        if (dicePanel) dicePanel.SetActive(false);
        if (lockInButton) lockInButton.interactable = true;

        currentEnergy = maxEnergy;
        UpdateEnergyUI();

        if (PhotonNetwork.IsMasterClient)
        {
            executionQueue.Clear();
            pendingCardsMap.Clear();
            tribesmenLockInOrder.Clear();
            readyPlayersCount = 0;
            battleTurnIndex = 0;
        }

        roundNumber++;
        UpdateRoundUI();

        SetupMultiplayer();
    }



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
        if (isMultiplayer) return;
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
        int playerCards = tribeLockedPanel.childCount;
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

        StartCoroutine(StartBattlePhase());
    }

    IEnumerator StartBattlePhase()
    {
        inputLocked = true;

        if (tribeLockedPanel.childCount == 0)
        {
            playCardButton.gameObject.SetActive(false);
            StartCoroutine(BakunawaSoloPlaySequence());
            yield break;
        }

        if (playerGoesFirst)
        {
            enemyHasPlayedPendingCard = false;
            pendingEnemyCard = null;
            playCardButton.gameObject.SetActive(false);

            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();

            yield return StartCoroutine(ShowTurnNotificationRoutine(true));

            inputLocked = false;
            playCardButton.interactable = true;
        }
        else
        {
            inputLocked = true;
            playCardButton.gameObject.SetActive(false);
            playCardButton.interactable = false;

            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetBakunawaTurn();

            yield return StartCoroutine(ShowTurnNotificationRoutine(false));

            StartCoroutine(EnemyPlaysFirstRoutine());
        }
    }

    IEnumerator EnemyPlaysFirstRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            pendingEnemyCard = BakunawaAI.Instance.PlayCard();

            yield return StartCoroutine(BakunawaAI.Instance.AnimateCurveToBoard(pendingEnemyCard));

            enemyHasPlayedPendingCard = true;
            RecalculateBattleEffects();

            inputLocked = false;
            playCardButton.interactable = true;

            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();

            yield return StartCoroutine(ShowTurnNotificationRoutine(true));
        }
        else
        {
            inputLocked = false;
            playCardButton.interactable = true;

            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();

            yield return StartCoroutine(ShowTurnNotificationRoutine(true));
        }
    }

    IEnumerator BakunawaResponseSequence(CardUI playerCard)
    {
        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetBakunawaTurn();

        yield return StartCoroutine(ShowTurnNotificationRoutine(false));

        yield return new WaitForSeconds(0.5f);

        if (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();

            yield return StartCoroutine(BakunawaAI.Instance.AnimateCurveToBoard(enemyCard));

            RecalculateBattleEffects();

            yield return StartCoroutine(AnimateCardClash(playerCard, enemyCard));

            if (ScoreManager.Instance != null)
            {
                int pAtk = GetCardAttack(playerCard);
                int eAtk = GetCardAttack(enemyCard);
                ScoreManager.Instance.ResolveClash(pAtk, eAtk);
            }
        }
        else
        {
            yield return StartCoroutine(AnimateCardClash(playerCard, null));

            if (ScoreManager.Instance != null)
            {
                int pAtk = GetCardAttack(playerCard);
                ScoreManager.Instance.ResolveClash(pAtk, 0);
            }
        }

        yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ContinueBattleLoop());
    }

    IEnumerator ResolveImmediateClash(CardUI playerCard, CardUI enemyCard)
    {
        yield return StartCoroutine(AnimateCardClash(playerCard, enemyCard));

        int pAtk = GetCardAttack(playerCard);
        int eAtk = (enemyCard != null) ? GetCardAttack(enemyCard) : 0;

        if (ScoreManager.Instance != null) ScoreManager.Instance.ResolveClash(pAtk, eAtk);

        enemyHasPlayedPendingCard = false;
        pendingEnemyCard = null;

        yield return new WaitForSeconds(0.5f);

        if (!playerGoesFirst)
        {
            if (tribeLockedPanel.childCount > 0) StartCoroutine(EnemyPlaysFirstRoutine());
            else StartCoroutine(ContinueBattleLoop());
        }
        else
        {
            StartCoroutine(ContinueBattleLoop());
        }
    }

    IEnumerator ContinueBattleLoop()
    {
        if (tribeLockedPanel.childCount > 0)
        {
            if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetTribeTurn();

            yield return StartCoroutine(ShowTurnNotificationRoutine(true));

            inputLocked = false;
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
        if (TurnIndicatorEffect.Instance != null) TurnIndicatorEffect.Instance.SetBakunawaTurn();

        yield return StartCoroutine(ShowTurnNotificationRoutine(false));

        while (BakunawaAI.Instance != null && BakunawaAI.Instance.HasLockedCards())
        {
            yield return new WaitForSeconds(1.0f);
            CardUI enemyCard = BakunawaAI.Instance.PlayCard();

            yield return StartCoroutine(BakunawaAI.Instance.AnimateCurveToBoard(enemyCard));

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

        List<CardUI> playerBattleCards = new List<CardUI>();
        List<CardUI> enemyBattleCards = new List<CardUI>();

        foreach (Transform t in battleZone)
        {
            CardUI c = t.GetComponent<CardUI>();
            if (c != null)
            {
                if (c.isEnemy) enemyBattleCards.Add(c);
                else playerBattleCards.Add(c);
            }
        }
        if (BakunawaAI.Instance != null && BakunawaAI.Instance.battleZone != null && BakunawaAI.Instance.battleZone != battleZone)
        {
            foreach (Transform t in BakunawaAI.Instance.battleZone)
            {
                CardUI c = t.GetComponent<CardUI>();
                if (c != null)
                {
                    if (c.isEnemy) enemyBattleCards.Add(c);
                    else playerBattleCards.Add(c);
                }
            }
        }

        var playerResult = CardEffectManager.Instance.CalculateRoundStats(playerBattleCards, enemyBattleCards);
        var enemyResult = CardEffectManager.Instance.CalculateRoundStats(enemyBattleCards, playerBattleCards);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.enemyDebuffValue = playerResult.damageReductionToEnemy;
            ScoreManager.Instance.playerDebuffValue = enemyResult.damageReductionToEnemy;
        }
    }

    void StartNextRound()
    {
        if (roundNumber >= 10)
        {
            CheckFinalScoreWin();
            return;
        }

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
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.enemyDebuffValue = 0;
            ScoreManager.Instance.playerDebuffValue = 0;
        }

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

    void MoveToPile(CardUI card, Transform pile, bool faceDown) { card.transform.SetParent(pile); card.transform.localPosition = Vector3.zero; card.transform.localScale = new Vector3(discardScale, discardScale, discardScale); card.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f)); card.SwitchToDeckMode(faceDown); }
    void ShuffleList(List<CardUI> list) { for (int i = 0; i < list.Count; i++) { CardUI temp = list[i]; int randomIndex = Random.Range(i, list.Count); list[i] = list[randomIndex]; list[randomIndex] = temp; } }
    void UpdateEnergyUI() { int currentUsed = 0; foreach (CardUI card in selectedCardsUI) currentUsed += GetCardCost(card); int remaining = currentEnergy - currentUsed; if (energySlider != null) { energySlider.maxValue = maxEnergy; energySlider.value = Mathf.Max(0, remaining); } if (energyText != null) { energyText.text = remaining.ToString() + "/" + maxEnergy.ToString(); if (remaining < 0) energyText.color = Color.red; else energyText.color = Color.white; } }
    void SetEnergyUIActive(bool isActive) { if (energySlider != null) energySlider.gameObject.SetActive(isActive); if (energyText != null) energyText.gameObject.SetActive(isActive); }

    public bool ToggleCardSelection(CardUI cardUI, bool isSelected)
    {
        if (!isPlanningPhase) return false;

        if (isSelected)
        {
            int currentUsed = 0;
            foreach (CardUI c in selectedCardsUI) currentUsed += GetCardCost(c);
            if (currentUsed + GetCardCost(cardUI) > currentEnergy)
            {
                StartCoroutine(ShowWarningSequence());
                return false;
            }

            selectedCardsUI.Add(cardUI);
            cardUI.transform.SetParent(tribeSelectedPanel);
            cardUI.transform.localRotation = Quaternion.identity;
            cardUI.UpdateLockedLayout();
        }
        else
        {
            selectedCardsUI.Remove(cardUI);
            ReturnCardToHand(cardUI);
        }

        UpdateEnergyUI();

        UpdateContainerSpacing(tribeSelectedPanel as RectTransform);
        if (CurvedHandLayout.Instance != null) CurvedHandLayout.Instance.ForceLayoutUpdate();

        return true;
    }

    IEnumerator ShowWarningSequence() { if (warningText != null) { warningText.gameObject.SetActive(true); warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1); yield return new WaitForSeconds(0.5f); float duration = 1.0f; float currentTime = 0f; while (currentTime < duration) { float alpha = Mathf.Lerp(1f, 0f, currentTime / duration); warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, alpha); currentTime += Time.deltaTime; yield return null; } warningText.gameObject.SetActive(false); } }
    int GetCardCost(CardUI card) { if (card == null) return 0; CardDisplay display = card.GetComponent<CardDisplay>(); if (display != null && display.cardData != null) return display.cardData.energyCost; if (card.costText != null && int.TryParse(card.costText.text, out int val)) return val; return 0; }
    int GetCardAttack(CardUI card) { if (card == null) return 0; CardDisplay display = card.GetComponent<CardDisplay>(); if (display != null) return display.currentAttack; if (card.attackText != null && int.TryParse(card.attackText.text, out int val)) return val; return 0; }

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
        UpdateHandPagination();
    }

    public void UpdateHandPagination()
    {
        if (handArea == null) return;
        int totalCards = handArea.childCount;
        int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)totalCards / cardsPerPage) - 1);
        if (currentPage > maxPage) currentPage = maxPage;
        if (currentPage < 0) currentPage = 0;
        int startIndex = currentPage * cardsPerPage;
        int endIndex = startIndex + cardsPerPage;

        for (int i = 0; i < totalCards; i++)
        {
            Transform child = handArea.GetChild(i);
            bool shouldBeVisible = (i >= startIndex && i < endIndex);
            child.gameObject.SetActive(shouldBeVisible);
        }
        bool showPagination = isPlanningPhase;

        if (prevPageBtn != null) { prevPageBtn.gameObject.SetActive(showPagination); if (showPagination) prevPageBtn.interactable = (currentPage > 0); }
        if (nextPageBtn != null) { nextPageBtn.gameObject.SetActive(showPagination); if (showPagination) nextPageBtn.interactable = (currentPage < maxPage); }
        if (pageIndicatorText != null) { pageIndicatorText.gameObject.SetActive(showPagination); if (showPagination) pageIndicatorText.text = $"Page {currentPage + 1}/{maxPage + 1}"; }
        if (CurvedHandLayout.Instance != null) CurvedHandLayout.Instance.ForceLayoutUpdate();
    }

    void NextHandPage() { int totalCards = handArea.childCount; int maxPage = Mathf.Max(0, Mathf.CeilToInt((float)totalCards / cardsPerPage) - 1); if (currentPage < maxPage) { currentPage++; UpdateHandPagination(); } }
    void PrevHandPage() { if (currentPage > 0) { currentPage--; UpdateHandPagination(); } }
    void ReturnCardToHand(CardUI card) { card.transform.SetParent(handArea); card.transform.localScale = Vector3.one; card.ResetToHandMode(); UpdateHandPagination(); }
    void EnsureButtonAnimation(Button btn) { if (btn != null) { if (btn.gameObject.GetComponent<UIButtonAnimation>() == null) { btn.gameObject.AddComponent<UIButtonAnimation>(); } } }

    void CreateImpactSparks(Vector3 pos)
    {
        if (ClashBloomController.Instance != null) ClashBloomController.Instance.TriggerClashBloom();
        GameObject container = new GameObject("SparkContainer");
        container.transform.position = pos;
        Canvas root = GetComponentInParent<Canvas>();
        if (root != null && root.rootCanvas != null) root = root.rootCanvas;
        if (root != null) container.transform.SetParent(root.transform); else container.transform.SetParent(transform);
        container.transform.localScale = Vector3.one; container.transform.SetAsLastSibling();
        Canvas sparkCanvas = container.AddComponent<Canvas>();
        sparkCanvas.overrideSorting = true;
        sparkCanvas.sortingLayerName = root != null ? root.sortingLayerName : "Default";
        sparkCanvas.sortingOrder = 2001;
        container.AddComponent<GraphicRaycaster>();
        Material hdrGlowMat = null;
        Shader hdrShader = Shader.Find("UI/HDRGlow");
        if (hdrShader != null) { hdrGlowMat = new Material(hdrShader); hdrGlowMat.SetFloat("_GlowIntensity", 4f); hdrGlowMat.SetFloat("_GlowFalloff", 1.5f); }
        Texture2D fallbackGlowTex = null;
        if (hdrGlowMat == null) fallbackGlowTex = CreateGlowTexture(64);

        int sparkCount = 24;
        List<RectTransform> sparks = new List<RectTransform>();
        List<Image> sparkImages = new List<Image>();
        List<Vector2> velocities = new List<Vector2>();
        List<Image> glowHalos = new List<Image>();

        for (int i = 0; i < sparkCount; i++)
        {
            float rVal = Random.value;
            Color sparkColor;
            if (rVal > 0.6f) sparkColor = new Color(1f, 1f, 0.7f);
            else if (rVal > 0.3f) sparkColor = new Color(1f, 0.85f, 0.2f);
            else sparkColor = new Color(1f, 0.5f, 0.1f);

            GameObject halo = new GameObject("Halo");
            halo.transform.SetParent(container.transform);
            halo.transform.position = pos;
            halo.transform.localScale = Vector3.one;
            Image haloImg = halo.AddComponent<Image>();

            if (hdrGlowMat != null) { Material haloMat = new Material(hdrGlowMat); haloMat.SetColor("_Color", sparkColor); haloMat.SetFloat("_GlowIntensity", 3f); haloImg.material = haloMat; haloImg.color = new Color(1f, 1f, 1f, 0.6f); }
            else { haloImg.sprite = Sprite.Create(fallbackGlowTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f)); haloImg.color = new Color(sparkColor.r, sparkColor.g, sparkColor.b, 0.4f); }

            GameObject s = new GameObject("Spark");
            s.transform.SetParent(container.transform);
            s.transform.position = pos;
            s.transform.localScale = Vector3.one;
            Image img = s.AddComponent<Image>();

            if (hdrGlowMat != null) { Material sparkMat = new Material(hdrGlowMat); sparkMat.SetColor("_Color", sparkColor); sparkMat.SetFloat("_GlowIntensity", 5f); sparkMat.SetFloat("_GlowFalloff", 2f); img.material = sparkMat; img.color = Color.white; }
            else { img.color = sparkColor; }

            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(400f, 1000f);
            velocities.Add(dir * speed);

            RectTransform rt = s.GetComponent<RectTransform>();
            RectTransform haloRT = halo.GetComponent<RectTransform>();
            float size = Random.Range(8f, 24f);
            rt.sizeDelta = new Vector2(size, size);
            haloRT.sizeDelta = new Vector2(size * 3f, size * 3f);

            sparks.Add(rt); sparkImages.Add(img); glowHalos.Add(haloImg);
        }
        StartCoroutine(AnimateSparksExplosion(container, sparks, sparkImages, glowHalos, velocities));
    }

    Texture2D CreateGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        for (int y = 0; y < size; y++) { for (int x = 0; x < size; x++) { float dx = (x - center) / center; float dy = (y - center) / center; float dist = Mathf.Sqrt(dx * dx + dy * dy); float alpha = Mathf.Clamp01(1f - dist); alpha = Mathf.Pow(alpha, 1.5f); pixels[y * size + x] = new Color(1f, 1f, 1f, alpha); } }
        tex.SetPixels(pixels); tex.Apply(); tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    IEnumerator AnimateSparksExplosion(GameObject container, List<RectTransform> sparks, List<Image> sparkImages, List<Image> glowHalos, List<Vector2> velocities)
    {
        float duration = 0.5f; float elapsed = 0f;
        while (elapsed < duration && container != null)
        {
            float t = elapsed / duration;
            for (int i = 0; i < sparks.Count; i++)
            {
                if (sparks[i] == null) continue;
                sparks[i].anchoredPosition += velocities[i] * Time.deltaTime;
                if (i < glowHalos.Count && glowHalos[i] != null) { RectTransform haloRT = glowHalos[i].GetComponent<RectTransform>(); if (haloRT != null) haloRT.anchoredPosition = sparks[i].anchoredPosition; }
                velocities[i] = Vector2.Lerp(velocities[i], Vector2.zero, Time.deltaTime * 5f);
                float fadeT = t * t;
                if (sparkImages[i] != null) { Color c = sparkImages[i].color; c.a = Mathf.Lerp(1f, 0f, fadeT); sparkImages[i].color = c; }
                if (i < glowHalos.Count && glowHalos[i] != null) { Color c = glowHalos[i].color; c.a = Mathf.Lerp(0.5f, 0f, fadeT); glowHalos[i].color = c; }
                sparks[i].localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                if (i < glowHalos.Count && glowHalos[i] != null) { RectTransform haloRT = glowHalos[i].GetComponent<RectTransform>(); if (haloRT != null) haloRT.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t); }
            }
            elapsed += Time.deltaTime; yield return null;
        }
        if (container != null) Destroy(container);
    }

    public void UpdateContainerSpacing(RectTransform container)
    {
        if (container == null) return;
        HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) return;
        hlg.childControlWidth = false; hlg.childForceExpandWidth = false;
        int count = container.childCount;
        if (count <= 1) { hlg.spacing = 20; return; }
        Canvas.ForceUpdateCanvases();
        float cardWidth = 0f;
        RectTransform child = container.GetChild(0) as RectTransform;
        float contentScale = 1f;
        if (child != null) contentScale = child.localScale.x;
        if (container == tribeLockedPanel || container == tribeSelectedPanel) { contentScale = lockedScale; }
        if (child != null) cardWidth = child.rect.width * contentScale;
        if (cardWidth <= 10f) cardWidth = 150f;
        float maxVisualWidth = 950f;
        float totalCardWidth = count * cardWidth;
        float maxSpacing = tribePanelSpacing;
        float dynamicSpacing = (maxVisualWidth - totalCardWidth) / (float)(count - 1);
        hlg.spacing = Mathf.Min(maxSpacing, dynamicSpacing);
    }
}

