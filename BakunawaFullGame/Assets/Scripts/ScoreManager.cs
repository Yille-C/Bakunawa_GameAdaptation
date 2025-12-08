using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Tower Slider")]
    public Slider towerSlider; // Set Min: -5, Max: 5, Value: 0 in Inspector
    public int currentTowerScore = 0;

    [Header("UI Text")]
    public Text playerScoreText;
    public Text bakunawaScoreText;

    [Header("Zones")]
    public Transform playerZone;
    public Transform bakunawaZone;

    // These hold the total score for the current round
    public int playerTotal;
    public int bakunawaTotal;

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
    }

    void Update()
    {
        CalculateBoardTotals();
    }

    // Called during the card fight - purely visual log now, NO MARKER MOVEMENT
    public void ResolveClash(int pAtk, int eAtk)
    {
        Debug.Log($"Clash! P:{pAtk} vs B:{eAtk}");
    }

    // --- NEW FUNCTION: Called ONLY when the round ends ---
    public void ResolveRound()
    {
        Debug.Log($"<color=green>ROUND END!</color> Final Score - Player: {playerTotal} vs Bakunawa: {bakunawaTotal}");

        if (playerTotal > bakunawaTotal)
        {
            Debug.Log("Player Wins Round! Moving Marker Down.");
            UpdateTowerScore(-1);
        }
        else if (bakunawaTotal > playerTotal)
        {
            Debug.Log("Bakunawa Wins Round! Moving Marker Up.");
            UpdateTowerScore(1);
        }
        else
        {
            Debug.Log("Round Draw! Marker stays put.");
        }
    }

    void UpdateTowerScore(int change)
    {
        int previousScore = currentTowerScore;
        int nextScore = currentTowerScore + change;

        // --- SKIP ZERO LOGIC ---
        if (nextScore == 0)
        {
            // If we were at 1 and go down, skip 0 -> go to -1
            if (previousScore > 0 && change < 0)
            {
                nextScore = -1;
            }
            // If we were at -1 and go up, skip 0 -> go to 1
            else if (previousScore < 0 && change > 0)
            {
                nextScore = 1;
            }
        }

        // Clamp values
        currentTowerScore = Mathf.Clamp(nextScore, -5, 5);

        // Update Slider
        if (towerSlider != null)
        {
            towerSlider.value = currentTowerScore;
        }

        Debug.Log($"Marker moved from {previousScore} to {currentTowerScore}");
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

        if (playerScoreText != null) playerScoreText.text = playerTotal.ToString();
        if (bakunawaScoreText != null) bakunawaScoreText.text = bakunawaTotal.ToString();
    }
}