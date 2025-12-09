using UnityEngine;
using UnityEngine.UI;

public class CloudSpawner : MonoBehaviour
{
    [SerializeField] private Sprite cloudSprite;
    [SerializeField] private int cloudCount = 5;
    [SerializeField] private float spawnYMin = -400f;
    [SerializeField] private float spawnYMax = 400f;
    [SerializeField] private Color cloudColor = new Color(1, 1, 1, 0.6f);

    private RectTransform canvasRect;

    private void Start()
    {
        canvasRect = GetComponent<RectTransform>();
        
        // Use a default sprite if none provided (optional: creates a simple circle texture in memory if needed, but standard sprite usually exists)
        if (cloudSprite == null)
        {
            // Just try to load any standard UI sprite resource or leave null (will show white square)
            // Ideally we should assign one in Editor or find one.
        }

        for (int i = 0; i < cloudCount; i++)
        {
            SpawnCloud(true);
        }
    }

    private void SpawnCloud(bool randomX)
    {
        GameObject cloud = new GameObject("Cloud");
        cloud.transform.SetParent(transform, false);
        
        // Put behind everything else usually, so set sibling index 1 (after BG)
        cloud.transform.SetSiblingIndex(1);

        Image img = cloud.AddComponent<Image>();
        img.sprite = cloudSprite;
        img.color = cloudColor;
        
        // Random size
        float scale = Random.Range(1.5f, 3.5f);
        cloud.transform.localScale = new Vector3(scale, scale, 1f);

        RectTransform rt = cloud.GetComponent<RectTransform>();
        
        float x = randomX ? Random.Range(-960f, 960f) : -1100f; // Start off screen left if not random
        if (randomX) x = Random.Range(-Screen.width/2f, Screen.width/2f);
        
        float y = Random.Range(spawnYMin, spawnYMax);
        
        rt.anchoredPosition = new Vector2(x, y);

        // Add movement
        CloudMovement mov = cloud.AddComponent<CloudMovement>();
        mov.speed = Random.Range(10f, 40f);
        mov.resetX = Screen.width / 2f + 300f; // Right side
        mov.startX = -Screen.width / 2f - 300f; // Left side
    }
}


