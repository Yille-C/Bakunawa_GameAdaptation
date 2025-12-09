using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class UIButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float duration = 0.1f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;

    private Vector3 originalScale;
    private Coroutine activeCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
        
        // Try to find audio source on this object or parent
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
        
        // If main manager has one, maybe use that?
        if (audioSource == null)
        {
            var manager = FindFirstObjectByType<MainMenuManager>(); // Inefficient if many buttons, but okay for menu
            // We won't tightly couple, just rely on local or null
        }
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateScale(originalScale * hoverScale);
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateScale(originalScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateScale(originalScale * clickScale);
        PlaySound(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateScale(originalScale * hoverScale); // Return to hover scale while still hovering
    }

    private void AnimateScale(Vector3 target)
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        if (gameObject.activeInHierarchy)
            activeCoroutine = StartCoroutine(ScaleTo(target));
        else
            transform.localScale = target;
    }

    private IEnumerator ScaleTo(Vector3 target)
    {
        float timer = 0f;
        Vector3 start = transform.localScale;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, target, timer / duration);
            yield return null;
        }
        transform.localScale = target;
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
