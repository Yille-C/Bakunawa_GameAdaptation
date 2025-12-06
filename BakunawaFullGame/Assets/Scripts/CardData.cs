using UnityEngine;

// This defines the 3 types of cards
public enum CardType
{
    Attack,
    Defense,
    Support
}

[CreateAssetMenu(fileName = "New Card", menuName = "Echoes/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName;
    public CardType type;       // Dropdown for Red/Blue/Green
    public int energyCost;
    public int attackValue;

    [Header("Visuals")]
    public Sprite cardArt;
    [TextArea] public string description; // Big box for text

    [Header("Logic")]
    public string effectID;     // Use this later for special powers (e.g., "atk_bayanihan")
}