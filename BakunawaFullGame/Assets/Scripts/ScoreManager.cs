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

    [Header("Zones")]
    public Transform playerZone;
    public Transform bakunawaZone;

    public int playerTotal;
    public int bakunawaTotal;

    // NEW: Stores the total reduction from Player's Defense cards
    public int enemyDebuffValue = 0;

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
        enemyDebuffValue = 0; // Reset
    }

    void Update()
    {
        CalculateBoardTotals();
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

    void UpdateTowerScore(int change)
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
    }

    void CalculateBoardTotals()
    {
        playerTotal = 0;
        bakunawaTotal = 0;

        if (playerZone != null)
        {
            foreach (Transform card in playerZone)
            {
                CardDisplay display = card.GetComponent<CardDisplay>();
                if (display != null) playerTotal += display.currentAttack;
            }
        }

        if (bakunawaZone != null)
        {
            foreach (Transform card in bakunawaZone)
            {
                CardDisplay display = card.GetComponent<CardDisplay>();
                if (display != null) bakunawaTotal += display.currentAttack;
            }
        }

        // --- APPLY DEBUFFS HERE ---
        // Subtract the Defense Card effects from Bakunawa's total
        bakunawaTotal -= enemyDebuffValue;
        // Prevent negative score (optional)
        if (bakunawaTotal < 0) bakunawaTotal = 0;

        if (playerScoreText != null) playerScoreText.text = playerTotal.ToString();
        if (bakunawaScoreText != null) bakunawaScoreText.text = bakunawaTotal.ToString();
    }
}