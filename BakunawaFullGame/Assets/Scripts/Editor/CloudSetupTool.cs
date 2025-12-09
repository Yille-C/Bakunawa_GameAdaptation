using UnityEngine;
using UnityEditor;

public class CloudSetupTool : EditorWindow
{
    [MenuItem("Tools/Animate Existing Clouds")]
    public static void AnimateClouds()
    {
        // Find specifically "Cloud 1" or any object with "Cloud" in the name that isn't the container
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        int count = 0;
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Cloud") && !obj.name.Contains("Container") && !obj.name.Contains("Spawner"))
            {
                // Check if it already has movement
                if (obj.GetComponent<CloudMovement>() == null)
                {
                    var move = obj.AddComponent<CloudMovement>();
                    move.speed = Random.Range(15f, 35f);
                    
                    // Setup bounds based on screen width approx
                    move.resetX = 1200f;
                    move.startX = -1200f;
                    
                    count++;
                }
            }
        }
        
        Debug.Log($"Added CloudMovement to {count} existing cloud objects (like 'Cloud 1').");
    }
}
