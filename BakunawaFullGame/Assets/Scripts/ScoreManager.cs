using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    // Necessary for HandManager to find this script
    public static ScoreManager Instance;

    [Header("UI Text")]
    public Text playerScoreText;
    public Text bakunawaScoreText;

    [Header("Zones")]
    // Drag the "Player Board/Row" object here
    public Transform playerZone;
    // Drag the "Bakunawa Board/Row" object here
    public Transform bakunawaZone;

    // Public so you can see totals in Inspector
    public int playerTotal;
    public int bakunawaTotal;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Calculate and Update UI every frame
        CalculateBoardTotals();
    }

    // HandManager calls this to handle the battle logic (e.g., Log who won)
    public void ResolveClash(int pAtk, int eAtk)
    {
        // We do NOT update the text here. We let CalculateBoardTotals() handle the UI.
        Debug.Log($"<color=yellow>CLASH!</color> Player Card: {pAtk} vs Bakunawa Card: {eAtk}");

        // Add specific damage logic here if needed (e.g. Tower health)
    }

    void CalculateBoardTotals()
    {
        playerTotal = 0;
        bakunawaTotal = 0;

        // 1. Calculate Player Total
        if (playerZone != null)
        {
            foreach (Transform card in playerZone)
            {
                // We look for the component that HandManager just set up
                CardDisplay display = card.GetComponent<CardDisplay>();
                if (display != null)
                {
                    playerTotal += display.currentAttack;
                }
            }
        }

        // 2. Calculate Bakunawa Total
        if (bakunawaZone != null)
        {
            foreach (Transform card in bakunawaZone)
            {
                CardDisplay display = card.GetComponent<CardDisplay>();
                if (display != null)
                {
                    bakunawaTotal += display.currentAttack;
                }
            }
        }

        // 3. Update the UI Text to show the SUM of all cards
        if (playerScoreText != null) playerScoreText.text = playerTotal.ToString();
        if (bakunawaScoreText != null) bakunawaScoreText.text = bakunawaTotal.ToString();
    }
}