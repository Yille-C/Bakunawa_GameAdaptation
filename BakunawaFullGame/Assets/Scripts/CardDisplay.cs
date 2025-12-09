using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public CardData cardData;
    [HideInInspector] public int currentAttack;
    [HideInInspector] public int lastFrameAttack;

    [Header("UI References")]
    public Text attackText;
    public Image artImage;

    [Header("Effects")]
    public GameObject popupPrefab;
    // DRAG AN EMPTY GAMEOBJECT HERE TO CONTROL EXACT POSITION
    public Transform popupSpawnPoint;

    void Start()
    {
        if (cardData != null)
        {
            currentAttack = cardData.attackValue;
            lastFrameAttack = currentAttack;

            UpdateStatText();
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

            // --- COLOR CHANGE LOGIC RESTORED ---
            if (cardData != null)
            {
                if (currentAttack > cardData.attackValue)
                {
                    attackText.color = Color.green; // Buffed
                }
                else if (currentAttack < cardData.attackValue)
                {
                    attackText.color = Color.red;   // Debuffed
                }
                else
                {
                    attackText.color = Color.white; // Normal
                }
            }
            // -----------------------------------
        }
    }

    public void TriggerPopup(int changeAmount, string label)
    {
        if (popupPrefab != null)
        {
            // Use the specific point you set in Unity, or default to center
            Vector3 spawnPos = transform.position;
            if (popupSpawnPoint != null)
            {
                spawnPos = popupSpawnPoint.position;
            }

            // Spawn directly in the Canvas hierarchy
            GameObject popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity, transform.root);

            DamagePopup dp = popup.GetComponent<DamagePopup>();
            if (dp != null)
            {
                dp.Setup(changeAmount, label);
            }
        }
    }
}