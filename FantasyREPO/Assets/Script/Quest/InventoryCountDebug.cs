using UnityEngine;

public class InventoryCountDebug : MonoBehaviour
{
    public string key = "Tree";
    public float every = 1f;
    float t;

    void Update()
    {
        t += Time.deltaTime;
        if (t < every) return;
        t = 0f;
        int n = 0;
        try { n = InventoryBridge.Count(key); } catch { }
        Debug.Log($"[INV] {key} = {n}");
    }
}
