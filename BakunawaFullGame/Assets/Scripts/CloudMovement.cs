using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public float speed = 20f;
    public float resetX = 1200f;
    public float startX = -1200f;
    private RectTransform rt;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (rt == null) return;
        rt.anchoredPosition += new Vector2(speed * Time.deltaTime, 0);
        if (rt.anchoredPosition.x > resetX)
        {
            rt.anchoredPosition = new Vector2(startX, Random.Range(rt.anchoredPosition.y - 50, rt.anchoredPosition.y + 50));
        }
    }
}
