using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    public Text popupText;
    public float lifetime = 1.0f;

    [Tooltip("How many UI pixels to travel upward over its lifetime")]
    public float moveDistance = 80f;

    public float popScale = 1.5f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(int amount, string label)
    {
        transform.localScale = Vector3.one;

        if (amount > 0)
        {
            popupText.text = "+" + amount + " " + label;
            popupText.color = Color.green;
        }
        else
        {
            popupText.text = amount + " " + label;
            popupText.color = Color.red;
        }

        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float timer = 0f;
        Color startColor = popupText.color;
        Vector2 startAnchoredPos = rectTransform.anchoredPosition;

        while (timer < lifetime)
        {
            timer += Time.unscaledDeltaTime;
            float percent = Mathf.Clamp01(timer / lifetime);

            // Move up in UI space using a smooth ease-out curve
            float movePercent = 1f - Mathf.Pow(1f - percent, 2f);
            rectTransform.anchoredPosition = startAnchoredPos + Vector2.up * moveDistance * movePercent;

            // Pop scale effect
            if (percent < 0.2f)
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * popScale, percent / 0.2f);
            else
                transform.localScale = Vector3.Lerp(Vector3.one * popScale, Vector3.one, (percent - 0.2f) / 0.8f);

            // Fade out in second half
            if (percent > 0.5f)
            {
                float fadeAlpha = Mathf.Lerp(1f, 0f, (percent - 0.5f) / 0.5f);
                popupText.color = new Color(startColor.r, startColor.g, startColor.b, fadeAlpha);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
