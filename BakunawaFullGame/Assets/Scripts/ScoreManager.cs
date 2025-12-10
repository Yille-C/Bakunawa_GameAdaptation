using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Tower Slider")]
    public Slider towerSlider;
    public int currentTowerScore = 0;

    [Header("UI Text")]
    public Text playerScoreText;
    public Text bakunawaScoreText;

    [Header("Popup System")]
    public GameObject popupPrefab;
    public Transform playerPopupSpot;
    public Transform bakunawaPopupSpot;

    [Header("Zones")]
    public Transform playerZone;
    public Transform bakunawaZone;

    public int playerTotal;
    public int bakunawaTotal;

    public int enemyDebuffValue = 0;
    public int playerDebuffValue = 0;

    private int lastEnemyDebuff = 0;
    private int lastPlayerDebuff = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (towerSlider != null)
        {
            towerSlider.minValue = -5;
            towerSlider.maxValue = 5;
            towerSlider.wholeNumbers = true;
            towerSlider.value = 0;
        }
        currentTowerScore = 0;
        enemyDebuffValue = 0;
        playerDebuffValue = 0;
        lastEnemyDebuff = 0;
        lastPlayerDebuff = 0;
    }

    void Update()
    {
        CalculateBoardTotals();
        CheckForDebuffPopups();
    }

    void CheckForDebuffPopups()
    {
        if (playerDebuffValue > lastPlayerDebuff)
        {
            int diff = playerDebuffValue - lastPlayerDebuff;
            Debug.Log($"Player Debuffed! Spawning popup for -{diff}");
            CreatePopup(playerPopupSpot, -diff, "Atk");
        }
        lastPlayerDebuff = playerDebuffValue;

        if (enemyDebuffValue > lastEnemyDebuff)
        {
            int diff = enemyDebuffValue - lastEnemyDebuff;
            Debug.Log($"Bakunawa Debuffed! Spawning popup for -{diff}");
            CreatePopup(bakunawaPopupSpot, -diff, "Atk");
        }
        lastEnemyDebuff = enemyDebuffValue;
    }

    void CreatePopup(Transform spot, int amount, string label)
    {
        if (popupPrefab != null && spot != null)
        {
            GameObject popup = Instantiate(popupPrefab, spot.position, Quaternion.identity, spot);
            DamagePopup dp = popup.GetComponent<DamagePopup>();
            if (dp != null) dp.Setup(amount, label);
        }
    }

    public void ResolveClash(int pAtk, int eAtk)
    {
        Debug.Log($"Clash! P:{pAtk} vs B:{eAtk}");
    }

    public void ResolveRound()
    {
        Debug.Log($"ROUND END! Player: {playerTotal} vs Bakunawa: {bakunawaTotal}");

        if (playerTotal > bakunawaTotal) UpdateTowerScore(-1);
        else if (bakunawaTotal > playerTotal) UpdateTowerScore(1);
    }

    public void UpdateTowerScore(int change)
    {
        int previousScore = currentTowerScore;
        int nextScore = currentTowerScore + change;

        if (nextScore == 0)
        {
            if (previousScore > 0 && change < 0) nextScore = -1;
            else if (previousScore < 0 && change > 0) nextScore = 1;
        }

        currentTowerScore = Mathf.Clamp(nextScore, -5, 5);
        if (towerSlider != null) towerSlider.value = currentTowerScore;

        // --- NEW: INSTANT WIN CHECK ---
        if (currentTowerScore <= -5)
        {
            if (HandManager.Instance != null) HandManager.Instance.TriggerGameOver("Tribesmen");
        }
        else if (currentTowerScore >= 5)
        {
            if (HandManager.Instance != null) HandManager.Instance.TriggerGameOver("Bakunawa");
        }
        // ------------------------------
    }

    void CalculateBoardTotals()
    {
        int tempPlayerTotal = 0;
        int tempBakunawaTotal = 0;

        if (playerZone != null)
        {
            foreach (Transform card in playerZone)
            {
                CardDisplay display = card.GetComponent<CardDisplay>();
                if (display != null) tempPlayerTotal += display.currentAttack;
            }
        }

        if (bakunawaZone != null)
        {
            foreach (Transform card in bakunawaZone)
            {
                CardDisplay display = card.GetComponent<CardDisplay>();
                if (display != null) tempBakunawaTotal += display.currentAttack;
            }
        }

        tempPlayerTotal -= playerDebuffValue;
        tempBakunawaTotal -= enemyDebuffValue;

        playerTotal = Mathf.Max(0, tempPlayerTotal);
        bakunawaTotal = Mathf.Max(0, tempBakunawaTotal);

        if (playerScoreText != null) playerScoreText.text = playerTotal.ToString();
        if (bakunawaScoreText != null) bakunawaScoreText.text = bakunawaTotal.ToString();
    }
}