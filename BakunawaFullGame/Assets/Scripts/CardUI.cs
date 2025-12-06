using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI References")]
    public Image cardFrameImage;     // The Big White Frame
    public Image artworkImage;       // The Character Art
    public Text nameText;
    public Text costText;
    public Text attackText;
    public GameObject selectionBorder;

    [Header("Card States")]
    public GameObject cardBackObject;
    public GameObject lockedArtObject;

    [Header("Locked Info References")]
    public Text lockedCostText;
    public Text lockedAttackText;

    private CardData data;
    private bool isSelected = false;

    // Hold Variables
    private bool isPressed = false;
    private float pressTimer = 0f;
    private bool detailsShown = false;

    public void Setup(CardData cardData)
    {
        data = cardData;

        // 1. Setup Normal Data
        nameText.text = data.cardName;
        costText.text = data.energyCost.ToString();
        attackText.text = data.attackValue.ToString();
        if (data.cardArt != null) artworkImage.sprite = data.cardArt;

        // 2. Setup Locked Data
        if (lockedCostText != null) lockedCostText.text = data.energyCost.ToString();
        if (lockedAttackText != null) lockedAttackText.text = data.attackValue.ToString();

        // 3. Reset to "Normal" State (Everything Visible)
        selectionBorder.SetActive(false);
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        // Make sure normal stuff is ON
        SetVisualsVisible(true);

        cardFrameImage.color = Color.white;
    }

    // --- UPDATED FUNCTION: Hides the white frame when Locked! ---
    public void SetLockedState(bool isLocked)
    {
        // 1. Toggle the Locked Art
        if (lockedArtObject != null) lockedArtObject.SetActive(isLocked);

        // 2. Toggle the Normal Visuals (Frame, Art, Text)
        // If Locked is TRUE, Normal Visuals should be FALSE (Hidden)
        SetVisualsVisible(!isLocked);
    }

    // Helper to turn on/off the normal card parts
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

        // If showing front, ensure frame is visible. If back, hide frame/art? 
        // Usually Deck Mode just covers everything with the Back object, 
        // so we can leave the frame on behind it.
        SetVisualsVisible(true);

        this.enabled = false;
    }

    public void ResetToHandMode()
    {
        this.enabled = true;
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);
        selectionBorder.SetActive(false);

        // Make sure everything is visible again!
        SetVisualsVisible(true);
    }

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
            HandManager.Instance.HideCardDetails();
        }
        else
        {
            if (HandManager.Instance.isPlanningPhase)
            {
                HandManager.Instance.ToggleCardSelection(this, !selectionBorder.activeSelf);
                selectionBorder.SetActive(!selectionBorder.activeSelf);
            }
            else
            {
                HandManager.Instance.SelectCardForBattle(this);
            }
        }
    }

    void Update()
    {
        if (isPressed && !detailsShown)
        {
            pressTimer += Time.deltaTime;
            if (pressTimer >= 0.5f)
            {
                HandManager.Instance.ShowCardDetails(data);
                detailsShown = true;
            }
        }
    }
}