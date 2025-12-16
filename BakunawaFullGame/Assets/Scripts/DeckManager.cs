using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Role Decks")]
    public List<CardData> bakunawaDeck;
    public List<CardData> mandirigmaDeck;
    public List<CardData> tagapangalagaDeck;
    public List<CardData> albularyoDeck;

    void Awake()
    {
        // Persist this object so decks survive scene load
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<CardData> GetDeckByRole(string role)
    {
        switch (role)
        {
            case "Bakunawa": return bakunawaDeck;
            case "Mandirigma": return mandirigmaDeck;
            case "Tagapangalaga": return tagapangalagaDeck;
            case "Albularyo": return albularyoDeck;
            default: return new List<CardData>();
        }
    }
}