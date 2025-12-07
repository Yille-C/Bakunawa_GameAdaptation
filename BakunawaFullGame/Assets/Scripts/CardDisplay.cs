using UnityEngine;

public class CardDisplay : MonoBehaviour
{
    // Drag your ScriptableObject (Serpent, Primal Bite) here in the Inspector
    public CardData cardData;

    // The ScoreManager will read this number
    [HideInInspector]
    public int currentAttack;

    void Start()
    {
        // When the game starts, grab the attack value from the data file
        if (cardData != null)
        {
            currentAttack = cardData.attackValue;
        }
        else
        {
            Debug.LogError("CardData is missing on " + gameObject.name);
        }
    }
}