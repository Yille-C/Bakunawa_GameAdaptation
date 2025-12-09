using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public class RoundResult
    {
        public int finalTotalAttack;
        public int damageReductionToEnemy;
    }

    public RoundResult CalculateRoundStats(List<CardUI> cardsPlayed, List<CardUI> enemyCards)
    {
        RoundResult result = new RoundResult();
        int totalAttack = 0;
        int enemyDebuff = 0;

        // 1. SNAPSHOT: Remember old stats
        Dictionary<CardDisplay, int> oldAttacks = new Dictionary<CardDisplay, int>();
        foreach (CardUI card in cardsPlayed)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null)
            {
                oldAttacks[display] = display.lastFrameAttack;
                display.ResetStats();
            }
        }

        // 2. CALCULATION LOOP
        for (int i = 0; i < cardsPlayed.Count; i++)
        {
            CardUI currentCard = cardsPlayed[i];
            CardDisplay display = currentCard.GetComponent<CardDisplay>();
            if (display == null || display.cardData == null) continue;

            CardData data = display.cardData;
            int currentBuff = 0;

            switch (data.effectID)
            {
                // ==========================
                //      PLAYER CARDS
                // ==========================
                case "atk_bayanihan":
                    int uniqueTypes = cardsPlayed.Select(c => c.GetComponent<CardDisplay>().cardData.type).Distinct().Count();
                    currentBuff = uniqueTypes * 2;
                    break;
                case "atk_mayari":
                    if (cardsPlayed.Any(c => c.GetComponent<CardDisplay>().cardData.type == CardType.Support)) currentBuff = 3;
                    break;
                case "atk_sandugo":
                    int tribesmenCount = cardsPlayed.Count(c => c.GetComponent<CardDisplay>().cardData.subtype == CardSubtype.Tribesmen);
                    currentBuff = tribesmenCount * 1;
                    break;
                case "atk_datu":
                    if (i == 0) currentBuff = 2;
                    break;
                case "def_palayok": enemyDebuff += 5; break;
                case "def_agong": enemyDebuff += 3; break;
                case "def_sigaw": enemyDebuff += cardsPlayed.Count; break;
                case "def_kalasag": enemyDebuff += 2; break;
                case "def_anito": enemyDebuff += 2; break;
                case "sup_blessing": currentBuff = cardsPlayed.Count * 2; break;
                case "sup_alay":
                    foreach (CardUI c in cardsPlayed) c.GetComponent<CardDisplay>().ModifyAttack(4);
                    break;
                case "sup_kudyapi":
                    if (cardsPlayed.Count >= 3) currentBuff = cardsPlayed.Count;
                    break;
                case "sup_elder": currentBuff = 2; break;

                // ==========================
                //      BAKUNAWA CARDS
                // ==========================

                // --- ATTACK ---
                case "atk_lunar":
                    break;

                case "atk_daluyon":
                    int defenseCount = cardsPlayed.Count(c => c.GetComponent<CardDisplay>().cardData.type == CardType.Defense);
                    currentBuff = defenseCount;
                    break;

                case "atk_deepsea":
                    // Updated to use the public roundNumber from HandManager
                    if (HandManager.Instance != null && HandManager.Instance.roundNumber > 5)
                    {
                        currentBuff = 2;
                    }
                    break;

                case "atk_serpent":
                    if (enemyCards != null)
                    {
                        currentBuff = enemyCards.Count;
                    }
                    break;

                case "atk_primal":
                    break;

                // --- DEFENSE ---
                case "def_eclipse": enemyDebuff += 4; break;
                case "def_crushing": enemyDebuff += 3; break;
                case "def_dragon": enemyDebuff += 1; break;
                case "def_evasive": enemyDebuff += 2; break;

                case "def_armored":
                    enemyDebuff += 2;
                    if (enemyCards != null && enemyCards.Count > cardsPlayed.Count)
                    {
                        currentBuff = 1;
                    }
                    break;

                // --- SUPPORT ---
                case "sup_ancient":
                    for (int j = i + 1; j < cardsPlayed.Count; j++)
                    {
                        cardsPlayed[j].GetComponent<CardDisplay>().ModifyAttack(2);
                    }
                    break;

                case "sup_ocean":
                    int buffCount = 0;
                    for (int j = i + 1; j < cardsPlayed.Count; j++)
                    {
                        if (buffCount < 2)
                        {
                            cardsPlayed[j].GetComponent<CardDisplay>().ModifyAttack(2);
                            buffCount++;
                        }
                    }
                    break;

                case "sup_predator":
                    break;

                case "sup_tidal":
                    break;

                case "sup_draconic":
                    break;
            }

            if (currentBuff > 0) display.ModifyAttack(currentBuff);
        }

        // 3. COMPARE & POPUP
        foreach (CardUI card in cardsPlayed)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null)
            {
                int newValue = display.currentAttack;
                int oldValue = oldAttacks.ContainsKey(display) ? oldAttacks[display] : display.cardData.attackValue;
                int diff = newValue - oldValue;

                if (diff != 0)
                {
                    string label = GetLabelForEffect(display.cardData.effectID);
                    display.TriggerPopup(diff, label);
                }

                display.lastFrameAttack = newValue;
                totalAttack += newValue;
            }
        }

        result.finalTotalAttack = totalAttack;
        result.damageReductionToEnemy = enemyDebuff;
        return result;
    }

    string GetLabelForEffect(string id)
    {
        if (id.Contains("bayanihan")) return "Unity";
        if (id.Contains("sandugo")) return "Tribesmen";
        if (id.Contains("daluyon")) return "Wrath";
        if (id.Contains("deepsea")) return "Fury";
        if (id.Contains("serpent")) return "Strike";
        if (id.Contains("armored")) return "Scales";
        if (id.Contains("ancient")) return "Rage";
        if (id.Contains("ocean")) return "Blessing";

        return "Buff"; // Default
    }
}