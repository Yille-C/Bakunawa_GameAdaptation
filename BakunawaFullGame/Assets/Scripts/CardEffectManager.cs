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

    // Returns a class containing the results of the calculation
    public class RoundResult
    {
        public int finalTotalAttack;
        public int damageReductionToEnemy; // How much to minus from enemy
    }

    public RoundResult CalculateRoundStats(List<CardUI> cardsPlayed, List<CardUI> enemyCards)
    {
        RoundResult result = new RoundResult();
        int totalAttack = 0;
        int enemyDebuff = 0;

        // 1. Reset all cards to base attack first
        foreach (CardUI card in cardsPlayed)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null) display.ResetStats();
        }

        // 2. Iterate through cards and apply logic
        for (int i = 0; i < cardsPlayed.Count; i++)
        {
            CardUI currentCard = cardsPlayed[i];
            CardDisplay display = currentCard.GetComponent<CardDisplay>();
            if (display == null || display.cardData == null) continue;

            CardData data = display.cardData;
            int currentBuff = 0;

            switch (data.effectID)
            {
                // --- ATTACK CARDS ---
                case "atk_bayanihan":
                    // +2 Attack for each different card type played
                    int uniqueTypes = cardsPlayed.Select(c => c.GetComponent<CardDisplay>().cardData.type).Distinct().Count();
                    currentBuff = uniqueTypes * 2;
                    break;

                case "atk_mayari":
                    // +3 Attack if any Support card was played
                    bool hasSupport = cardsPlayed.Any(c => c.GetComponent<CardDisplay>().cardData.type == CardType.Support);
                    if (hasSupport) currentBuff = 3;
                    break;

                case "atk_sandugo":
                    // +1 Attack for each Tribesmen card
                    int tribesmenCount = cardsPlayed.Count(c => c.GetComponent<CardDisplay>().cardData.subtype == CardSubtype.Tribesmen);
                    currentBuff = tribesmenCount * 1;
                    break;

                case "atk_datu":
                    // +2 Attack if played as your first card
                    if (i == 0) currentBuff = 2;
                    break;

                case "atk_matulis":
                    // No effect
                    break;

                // --- DEFENSE CARDS (Debuffs) ---
                case "def_palayok":
                    enemyDebuff += 5;
                    break;

                case "def_agong":
                    enemyDebuff += 3;
                    // Note: "Draw card on loss" logic needs to be handled in EndRoundSequence
                    break;

                case "def_sigaw":
                    // Reduce enemy Atk by 1 for each card you played
                    enemyDebuff += cardsPlayed.Count;
                    break;

                case "def_kalasag":
                    enemyDebuff += 2;
                    break;

                case "def_anito":
                    enemyDebuff += 2;
                    break;

                // --- SUPPORT CARDS ---
                case "sup_blessing":
                    // +2 Attack for every card played
                    currentBuff = cardsPlayed.Count * 2;
                    break;

                case "sup_alay":
                    // Choose: +4 to ALL or Reduce Enemy by 6
                    // TODO: Add Choice UI. For now, defaulting to +4 Buff All.
                    // We handle "Buff All" by adding +4 to THIS card's calculation loop? 
                    // No, "Buff All" implies affecting others.
                    // Simple hack: We loop through ALL cards right now and add 4.
                    foreach (CardUI c in cardsPlayed)
                    {
                        c.GetComponent<CardDisplay>().ModifyAttack(4);
                    }
                    break;

                case "sup_kudyapi":
                    // If played 3+ cards, +1 Atk for every card played
                    if (cardsPlayed.Count >= 3)
                    {
                        currentBuff = cardsPlayed.Count;
                    }
                    break;

                case "sup_gabayan":
                    // Reveal opponent hand (Visual only)
                    // Logic would go here: BakunawaAI.Instance.RevealHand();
                    break;

                case "sup_elder":
                    // Gain +2 Attack
                    currentBuff = 2;
                    break;
            }

            // Apply Buffs to the specific card
            if (currentBuff > 0)
            {
                display.ModifyAttack(currentBuff);
            }
        }

        // 3. Sum up the modified attacks
        foreach (CardUI card in cardsPlayed)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null) totalAttack += display.currentAttack;
        }

        result.finalTotalAttack = totalAttack;
        result.damageReductionToEnemy = enemyDebuff;
        return result;
    }
}