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

    // CHANGED TO PUBLIC TO FIX HANDMANAGER ERROR
    public CardData cardData;

    private bool isPressed = false;
    private float pressTimer = 0f;
    private bool detailsShown = false;
    private float holdTimeNeeded = 0.5f;

    private bool isDeckMode = false;

    public void Setup(CardData data)
    {
        cardData = data;

        if (nameText != null) nameText.text = cardData.cardName;
        if (costText != null) costText.text = cardData.energyCost.ToString();
        if (attackText != null) attackText.text = cardData.attackValue.ToString();

        if (cardData.cardArt != null && artworkImage != null) artworkImage.sprite = cardData.cardArt;

        if (lockedCostObject != null)
        {
            Text txt = lockedCostObject.GetComponent<Text>();
            if (txt != null) txt.text = cardData.energyCost.ToString();
        }
        if (lockedAttackObject != null)
        {
            Text txt = lockedAttackObject.GetComponent<Text>();
            if (txt != null) txt.text = cardData.attackValue.ToString();
        }

        if (selectionBorder != null) selectionBorder.SetActive(false);
        if (cardBackObject != null) cardBackObject.SetActive(false);
        if (lockedArtObject != null) lockedArtObject.SetActive(false);

        SetVisualsVisible(true);
        if (cardFrameImage != null) cardFrameImage.color = Color.white;

        this.enabled = true;
        isDeckMode = false;
    }

    public void SetLockedState(bool locked)
    {
        if (lockedArtObject != null) lockedArtObject.SetActive(locked);
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

        if (pressTimer < holdTimeNeeded && !isDeckMode)
        {
            if (HandManager.Instance == null) return;

            if (isEnemy) return;

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
        if (isPressed && !detailsShown)
        {
            pressTimer += Time.deltaTime;

            if (pressTimer >= holdTimeNeeded)
            {
                if (cardBackObject != null && cardBackObject.activeSelf) return;

                detailsShown = true;
                if (HandManager.Instance != null && cardData != null)
                {
                    HandManager.Instance.ShowCardDetails(cardData);
                }
            }
        }
    }
}