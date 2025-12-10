using UnityEngine;

public class BreathingEffect : MonoBehaviour
{
    [Header("Breathing Settings")]
    [Tooltip("How fast the breathing cycle is.")]
    public float speed = 2.0f;

    [Tooltip("How much it expands. 0.05 means 5% bigger/smaller.")]
    public float strength = 0.05f;

    private Vector3 originalScale;

    void Start()
    {
        // Remember the starting size so we don't drift away
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Calculate a wave that goes up and down smoothly between -1 and 1
        float wave = Mathf.Sin(Time.time * speed);

        // Convert that wave into a scale factor (e.g., oscillating between 0.95 and 1.05)
        float scaleFactor = 1.0f + (wave * strength);

        // Apply the new scale
        transform.localScale = originalScale * scaleFactor;
    }
}