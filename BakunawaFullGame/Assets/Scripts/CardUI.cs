using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI References")]
    public Image cardFrameImage;     // The White Background Image
    public Image artworkImage;       // The Character Art
    public Text nameText;
    public Text costText;
    public Text attackText;
    public GameObject selectionBorder;

    [Header("Card States")]
    public GameObject cardBackObject;
    public GameObject lockedArtObject;

    [Header("Locked Info References")]
    public Text lockedCostObject;
    public Text lockedAttackObject;

    [Header("Settings")]
    public bool isEnemy = false;

    private CardData data;
    private bool isPressed = false;
    private float pressTimer = 0f;
    private bool detailsShown = false;
    private float holdTimeNeeded = 0.5f; // Time in seconds to hold before showing details

    public void Setup(CardData cardData)
    {
        data = cardData;

        // Setup Texts
        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        if (attackText != null) attackText.text = data.attackValue.ToString();

        if (data.cardArt != null && artworkImage != null) artworkImage.sprite = data.cardArt;

        // Setup Locked Info Texts (Find the text components inside the objects)
        if (lockedCostObject != null)
        {
            Text txt = lockedCostObject.GetComponent<Text>();
            if (txt != null) txt.text = data.energyCost.ToString();
        }
        if (lockedAttackObject != null)
        {
            Text txt = lockedAttackObject.GetComponent<Text>();
            if (txt != null) txt.text = data.attackValue.ToString();
        }

        // Reset States (Hide everything special)
        if (selectionBorder != null) selectionBorder.SetActive(false);
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        // Start with everything VISIBLE (Normal Card)
        SetVisualsVisible(true);
        if (cardFrameImage != null) cardFrameImage.color = Color.white;
    }

    // --- MAIN FUNCTION: LOCKING ---
    public void SetLockedState(bool isLocked)
    {
        // 1. Show the Black Star if locked
        if (lockedArtObject != null) lockedArtObject.SetActive(isLocked);

        // 2. Hide the White Frame/Text if locked
        // If isLocked is TRUE -> SetVisualsVisible(FALSE) -> Hides Frame
        SetVisualsVisible(!isLocked);
    }

    // Helper to toggle the main card parts (Frame, Art, Text)
    void SetVisualsVisible(bool isVisible)
    {
        if (cardFrameImage != null) cardFrameImage.enabled = isVisible;
        if (artworkImage != null) artworkImage.enabled = isVisible;
        if (nameText != null) nameText.enabled = isVisible;
        if (costText != null) costText.enabled = isVisible;
        if (attackText != null) attackText.enabled = isVisible;
    }

    public void SwitchToDeckMode(bool showBack)
    {
        if (cardBackObject != null) cardBackObject.SetActive(showBack);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        // Ensure the frame is ON so the card exists physically in the pile
        SetVisualsVisible(true);

        this.enabled = false; // Disable interactions
    }

    public void ResetToHandMode()
    {
        this.enabled = true; // Re-enable interactions
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);
        if (selectionBorder != null) selectionBorder.SetActive(false);

        // Ensure everything is visible again for the hand
        SetVisualsVisible(true);
    }

    // --- Input Handling ---
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        pressTimer = 0f;
        detailsShown = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (detailsShown)
        {
            if (HandManager.Instance != null) HandManager.Instance.HideCardDetails();
        }
        else
        {
            if (HandManager.Instance == null) return;

            if (isEnemy) return; // Don't select enemy cards

            if (HandManager.Instance.isPlanningPhase)
            {
                if (selectionBorder != null)
                {
                    HandManager.Instance.ToggleCardSelection(this, !selectionBorder.activeSelf);
                    selectionBorder.SetActive(!selectionBorder.activeSelf);
                }
            }
            else
            {
                HandManager.Instance.SelectCardForBattle(this);
            }
        }
    }

    void Update()
    {
        // Logic to detect "Holding Down" the click
        if (isPressed && !detailsShown)
        {
            pressTimer += Time.deltaTime;

            if (pressTimer >= holdTimeNeeded)
            {
                detailsShown = true;
                if (HandManager.Instance != null)
                {
                    HandManager.Instance.ShowCardDetails(data);
                }
            }
        }
    }
}