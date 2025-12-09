using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public CardData cardData;
    [HideInInspector] public int currentAttack;

    [Header("UI References")]
    public Text attackText;
    public Image artImage; // Optional

    void Start()
    {
        if (cardData != null)
        {
            ResetStats();
            if (artImage != null) artImage.sprite = cardData.cardArt;
        }
    }

    public void ResetStats()
    {
        if (cardData != null)
        {
            currentAttack = cardData.attackValue;
            UpdateStatText();
        }
    }

    public void ModifyAttack(int amount)
    {
        currentAttack += amount;
        UpdateStatText();
    }

    void UpdateStatText()
    {
        if (attackText != null)
        {
            attackText.text = currentAttack.ToString();

            // Visual Feedback: Green if buffed, White if normal
            if (cardData != null && currentAttack > cardData.attackValue)
                attackText.color = Color.green;
            else if (cardData != null && currentAttack < cardData.attackValue)
                attackText.color = Color.red;
            else
                attackText.color = Color.white;
        }
    }
}