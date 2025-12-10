using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI References")]
    public Image cardFrameImage;
    public Image artworkImage;
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
    public CardData cardData;

    private bool isPressed = false;
    private float pressTimer = 0f;
    private bool detailsShown = false;
    private float holdTimeNeeded = 0.5f;
    private bool isDeckMode = false;

    public void Setup(CardData data)
    {
        cardData = data;
        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        if (attackText != null) attackText.text = data.attackValue.ToString();
        if (data.cardArt != null && artworkImage != null) artworkImage.sprite = data.cardArt;

        // Setup Locked Visuals
        if (lockedCostObject != null) lockedCostObject.text = data.energyCost.ToString();
        if (lockedAttackObject != null) lockedAttackObject.text = data.attackValue.ToString();

        if (selectionBorder != null) selectionBorder.SetActive(false);
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        SetVisualsVisible(true);
        this.enabled = true; // Ensure script is on
        isDeckMode = false;
    }

    public void SetLockedState(bool locked)
    {
        if (lockedArtObject != null) lockedArtObject.SetActive(locked);
        // We do NOT disable the script here anymore
        // Visuals can be toggled if you have separate 'locked' vs 'unlocked' graphics
        SetVisualsVisible(!locked);
    }

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
        isDeckMode = true;
        if (cardBackObject != null) cardBackObject.SetActive(showBack);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);
        SetVisualsVisible(true);
    }

    public void ResetToHandMode()
    {
        isDeckMode = false;
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);
        if (selectionBorder != null) selectionBorder.SetActive(false);
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
            if (HandManager.Instance != null) HandManager.Instance.HideCardDetails();
            detailsShown = false;
            return;
        }

        // --- CLICK LOGIC ---
        if (pressTimer < holdTimeNeeded && !isDeckMode && !isEnemy)
        {
            if (HandManager.Instance == null) return;

            // Phase 1: Planning (Select to Lock)
            if (HandManager.Instance.isPlanningPhase)
            {
                if (selectionBorder != null)
                {
                    bool wasSelected = !selectionBorder.activeSelf;
                    HandManager.Instance.ToggleCardSelection(this, wasSelected);
                    selectionBorder.SetActive(wasSelected);
                }
            }
            // Phase 2: Battle (Select to Play)
            else
            {
                // This is the part that was failing because the script was disabled!
                HandManager.Instance.SelectCardForBattle(this);
            }
        }
    }

    void Update()
    {
        if (isPressed && !detailsShown)
        {
            pressTimer += Time.deltaTime;
            if (pressTimer >= holdTimeNeeded)
            {
                detailsShown = true;
                if (HandManager.Instance != null && cardData != null)
                {
                    HandManager.Instance.ShowCardDetails(cardData);
                }
            }
        }
    }
}