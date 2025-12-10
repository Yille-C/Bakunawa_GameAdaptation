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

    private CardData data;
    private bool isPressed = false;
    private float pressTimer = 0f;
    private bool detailsShown = false;
    private float holdTimeNeeded = 0.5f;

    // STATE FLAGS
    private bool isLocked = false;
    private bool isDeckMode = false; // Used to prevent clicking without disabling script

    public void Setup(CardData cardData)
    {
        data = cardData;

        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        if (attackText != null) attackText.text = data.attackValue.ToString();

        if (data.cardArt != null && artworkImage != null) artworkImage.sprite = data.cardArt;

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

        if (selectionBorder != null) selectionBorder.SetActive(false);
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        SetVisualsVisible(true);
        if (cardFrameImage != null) cardFrameImage.color = Color.white;

        // Ensure script is ON so we can detect holds
        this.enabled = true;
    }

    public void SetLockedState(bool locked)
    {
        isLocked = locked;
        if (lockedArtObject != null) lockedArtObject.SetActive(isLocked);
        SetVisualsVisible(!isLocked);
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

        // CHANGED: We KEEP the script enabled so logic works, 
        // but we block clicks using the 'isDeckMode' flag instead.
        this.enabled = true;
    }

    public void ResetToHandMode()
    {
        isDeckMode = false;
        this.enabled = true;

        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);
        if (selectionBorder != null) selectionBorder.SetActive(false);

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
            // Close details if they were open
            if (HandManager.Instance != null) HandManager.Instance.HideCardDetails();
            detailsShown = false;
            return; // Don't process click logic
        }

        // Only process click if it wasn't a long hold
        if (pressTimer < holdTimeNeeded)
        {
            if (HandManager.Instance == null) return;

            // 1. Prevent clicking Enemy Cards
            if (isEnemy) return;

            // 2. Prevent clicking Deck/Pile Cards
            if (isDeckMode) return;

            // 3. Normal Logic
            if (HandManager.Instance.isPlanningPhase)
            {
                if (!isLocked && selectionBorder != null)
                {
                    HandManager.Instance.ToggleCardSelection(this, !selectionBorder.activeSelf);
                    selectionBorder.SetActive(!selectionBorder.activeSelf);
                }
            }
            else
            {
                if (isLocked)
                {
                    HandManager.Instance.SelectCardForBattle(this);
                }
            }
        }
    }

    void Update()
    {
        // HOLD LOGIC
        if (isPressed && !detailsShown)
        {
            pressTimer += Time.deltaTime;

            if (pressTimer >= holdTimeNeeded)
            {
                // SAFETY: Don't show details if card is Face Down (Cheating protection)
                if (cardBackObject != null && cardBackObject.activeSelf) return;

                detailsShown = true;
                if (HandManager.Instance != null && data != null)
                {
                    HandManager.Instance.ShowCardDetails(data);
                }
            }
        }
    }
}