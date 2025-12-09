using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    public Text popupText;
    public float lifetime = 1.0f;
    public float moveSpeed = 30f;
    public float popScale = 1.5f;

    public void Setup(int amount, string label)
    {
        transform.localScale = Vector3.one;

        // 1. Set Text & Color (Green for Buff, Red for Debuff)
        if (amount > 0)
        {
            popupText.text = "+" + amount + " " + label;
            popupText.color = Color.green;
        }
        else
        {
            popupText.text = amount + " " + label; // e.g. "-2 Enemy"
            popupText.color = Color.red;
        }

        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float timer = 0;
        Color startColor = popupText.color;

        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            float percent = timer / lifetime;

            // Move Up
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            // Pop Effect
            if (percent < 0.2f)
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * popScale, percent / 0.2f);
            else
                transform.localScale = Vector3.Lerp(Vector3.one * popScale, Vector3.one, (percent - 0.2f) / 0.8f);

            // Fade Out
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
