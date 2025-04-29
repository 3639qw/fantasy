using UnityEngine;
using System.Collections.Generic;

public class DontDestroy : MonoBehaviour
{
    static List<string> dontDestroy = new List<string>();
    
    
    void Awake()
    {
        if (dontDestroy.Contains(gameObject.name))
        {
            Destroy(gameObject);
            return;
        }

        dontDestroy.Add(gameObject.name);
        DontDestroyOnLoad(gameObject);
    }
}
