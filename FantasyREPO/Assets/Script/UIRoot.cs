using UnityEngine;
using UnityEngine.EventSystems;

public class UIRoot : MonoBehaviour
{
    private static UIRoot _inst;

    private static T FindOne<T>() where T : Object
    {
    #if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
    #else
        return Object.FindObjectOfType<T>();
    #endif
    }

    private void Awake()
    {
        // 싱글턴 보장
        if (_inst && _inst != this) { Destroy(gameObject); return; }
        _inst = this;

        // 전역 유지
        DontDestroyOnLoad(gameObject);

        // EventSystem 보장
        if (!FindOne<EventSystem>())
        {
            var es = new GameObject("EventSystem");
        #if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        #else
            es.AddComponent<StandaloneInputModule>();
        #endif
            DontDestroyOnLoad(es);
        }
    }
}
