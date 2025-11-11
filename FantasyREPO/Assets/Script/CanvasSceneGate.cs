using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CanvasSceneGate : MonoBehaviour
{
    [Tooltip("이 문자열들 중 하나라도 씬 이름에 포함되면 캔버스를 막습니다.")]
    [SerializeField] string[] blockIfSceneNameContains = { "Main" };

    [Tooltip("GameObject.SetActive(false)로 끌지, Canvas.enabled=false로 끌지 선택")]
    [SerializeField] bool disableGameObject = true;

    [Tooltip("씬 전환을 감지해서 자동으로 토글합니다.")]
    [SerializeField] bool listenSceneChanges = true;

    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();  // 없으면 disableGameObject 모드로만 동작
        Apply();
        if (listenSceneChanges) SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (listenSceneChanges) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => Apply();

    void Apply()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool shouldBlock = false;
        foreach (var kw in blockIfSceneNameContains)
        {
            if (!string.IsNullOrEmpty(kw) &&
                sceneName.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                shouldBlock = true; break;
            }
        }

        if (disableGameObject)
        {
            gameObject.SetActive(!shouldBlock);
        }
        else
        {
            if (_canvas) _canvas.enabled = !shouldBlock;
        }
    }
}
