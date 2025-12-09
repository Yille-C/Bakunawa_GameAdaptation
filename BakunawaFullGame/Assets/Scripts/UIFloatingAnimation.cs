using UnityEngine;

public class UIFloatingAnimation : MonoBehaviour
{
    [Header("Position Animation (Bobbing)")]
    [SerializeField] private bool animatePosition = true;
    [SerializeField] private Vector2 moveAmount = new Vector2(0f, 10f); // Move up/down 10 px
    [SerializeField] private float moveSpeed = 1f;

    [Header("Rotation Animation (Wobble)")]
    [SerializeField] private bool animateRotation = false;
    [SerializeField] private float rotationAngle = 2f;
    [SerializeField] private float rotationSpeed = 1f;

    [Header("Scale Animation (Pulse)")]
    [SerializeField] private bool animateScale = false;
    [SerializeField] private Vector2 scaleAmount = new Vector2(0.05f, 0.05f);
    [SerializeField] private float scaleSpeed = 1f;

    [Header("Settings")]
    [SerializeField] private bool randomOffset = true;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private float timeOffset;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition3D;
        originalRotation = rectTransform.localRotation;
        originalScale = rectTransform.localScale;
    }

    private void Start()
    {
        if (randomOffset)
        {
            timeOffset = Random.Range(0f, 10f);
        }
    }

    private void Update()
    {
        float time = Time.time + timeOffset;

        // Position
        if (animatePosition)
        {
            Vector3 offset = new Vector3(
                Mathf.Sin(time * moveSpeed) * moveAmount.x,
                Mathf.Sin(time * moveSpeed) * moveAmount.y,
                0f
            );
            rectTransform.anchoredPosition3D = originalPosition + offset;
        }

        // Rotation
        if (animateRotation)
        {
            float zRotation = Mathf.Sin(time * rotationSpeed) * rotationAngle;
            rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, zRotation);
        }

        // Scale
        if (animateScale)
        {
            Vector3 scaleOffset = new Vector3(
                Mathf.Sin(time * scaleSpeed) * scaleAmount.x,
                Mathf.Sin(time * scaleSpeed) * scaleAmount.y,
                0f
            );
            rectTransform.localScale = originalScale + scaleOffset;
        }
    }
}
