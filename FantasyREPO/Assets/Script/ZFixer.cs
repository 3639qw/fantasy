using UnityEngine;

public class ZFixer : MonoBehaviour
{

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
