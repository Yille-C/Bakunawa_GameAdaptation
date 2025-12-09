using UnityEngine;

public enum CardType
{
    Attack,
    Defense,
    Support
}

public enum CardSubtype
{
    None,
    Tribesmen,
}

[CreateAssetMenu(fileName = "New Card", menuName = "Echoes/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName;
    public CardType type;
    public CardSubtype subtype; // Added for Sandugo Assault
    public int energyCost;
    public int attackValue;

    [Header("Visuals")]
    public Sprite cardArt;
    [TextArea] public string description;

    [Header("Logic")]
    public string effectID;     // KEY: e.g., "atk_bayanihan", "def_palayok"
}