using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score Tracking")]
    public int playerTotal = 0;
    public int bakunawaTotal = 0;
    public int enemyDebuffValue = 0;
    public int playerDebuffValue = 0;
    public int lastEnemyDebuff = 0;
    public int lastPlayerDebuff = 0;

    [Header("UI References")]
    public Text playerScoreText;
    public Text bakunawaScoreText;
    public Slider towerSlider;
    public int currentTowerScore = 0;

    [Header("Debuff Popup")]
    public GameObject debuffPopupPrefab;
    public Transform playerDebuffSpawnPoint;
    public Transform bakunawaDebuffSpawnPoint;

    [Header("Card Zones")]
    public Transform playerZone;
    public Transform bakunawaZone;

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
        UpdateScoreUI();
    }

    public void AddScore(int tribesmanAttack, int bakunawaAttack)
    {
        playerTotal += tribesmanAttack;
        bakunawaTotal += bakunawaAttack;

        Debug.Log($"Scores Updated! Tribesmen: {playerTotal} | Bakunawa: {bakunawaTotal}");
    }

    public void ResolveClash(int pAtk, int eAtk)
    {
        Debug.Log($"Clash! Tribesmen: {pAtk} vs Bakunawa: {eAtk}");
    }

    public void ResolveRound()
    {
        int finalPlayerScore = playerTotal - enemyDebuffValue;
        int finalBakunawaScore = bakunawaTotal - playerDebuffValue;

        finalPlayerScore = Mathf.Max(0, finalPlayerScore);
        finalBakunawaScore = Mathf.Max(0, finalBakunawaScore);

        Debug.Log($"Round Resolved! Tribesmen: {finalPlayerScore} | Bakunawa: {finalBakunawaScore}");

        int difference = finalPlayerScore - finalBakunawaScore;

        if (difference > 0)
        {
            UpdateTowerScore(1);
            Debug.Log("Tribesmen win this round!");
        }
        else if (difference < 0)
        {
            UpdateTowerScore(-1);
            Debug.Log("Bakunawa wins this round!");
        }
        else
        {
            Debug.Log("Round is a tie!");
        }
    }

    public void UpdateTowerScore(int change)
    {
        currentTowerScore = Mathf.Clamp(currentTowerScore + change, -5, 5);

        if (towerSlider != null)
        {
            towerSlider.value = currentTowerScore;
        }

        if (currentTowerScore <= -5)
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.TriggerGameOver("Tribesmen");
            }
        }

        if (currentTowerScore >= 5)
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.TriggerGameOver("Bakunawa");
            }
        }
    }

    public void ResetScores()
    {
        playerTotal = 0;
        bakunawaTotal = 0;
        enemyDebuffValue = 0;
        playerDebuffValue = 0;
        lastEnemyDebuff = 0;
        lastPlayerDebuff = 0;

        Debug.Log("Scores reset for new round");
    }

    void UpdateScoreUI()
    {
        int playerScore = 0;
        int bakunawaScore = 0;

        if (playerZone != null)
        {
            foreach (Transform t in playerZone)
            {
                CardDisplay cd = t.GetComponent<CardDisplay>();
                if (cd != null) playerScore += cd.currentAttack;
            }
        }

        if (bakunawaZone != null)
        {
            foreach (Transform t in bakunawaZone)
            {
                CardDisplay cd = t.GetComponent<CardDisplay>();
                if (cd != null) bakunawaScore += cd.currentAttack;
            }
        }

        playerTotal = playerScore;
        bakunawaTotal = bakunawaScore;

        if (playerScoreText != null)
        {
            playerScoreText.text = playerTotal.ToString();
        }

        if (bakunawaScoreText != null)
        {
            bakunawaScoreText.text = bakunawaTotal.ToString();
        }
    }

    public void ApplyDebuffToEnemy(int amount)
    {
        enemyDebuffValue += amount;
        lastEnemyDebuff = amount;

        if (debuffPopupPrefab != null && bakunawaDebuffSpawnPoint != null)
        {
            GameObject popup = Instantiate(debuffPopupPrefab, bakunawaDebuffSpawnPoint.position, Quaternion.identity, bakunawaDebuffSpawnPoint.root);
            DamagePopup dp = popup.GetComponent<DamagePopup>();
            if (dp != null)
            {
                dp.Setup(-amount, "Defense");
            }
        }
    }

    public void ApplyDebuffToPlayer(int amount)
    {
        playerDebuffValue += amount;
        lastPlayerDebuff = amount;

        if (debuffPopupPrefab != null && playerDebuffSpawnPoint != null)
        {
            GameObject popup = Instantiate(debuffPopupPrefab, playerDebuffSpawnPoint.position, Quaternion.identity, playerDebuffSpawnPoint.root);
            DamagePopup dp = popup.GetComponent<DamagePopup>();
            if (dp != null)
            {
                dp.Setup(-amount, "Defense");
            }
        }
    }
}
