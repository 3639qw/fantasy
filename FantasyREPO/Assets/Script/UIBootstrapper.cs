using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public static class UIBootstrapper
{
    // Resources 상대 경로 (확장자 X)
    private const string PREFAB_PATH = "Canvas"; // => Assets/Resources/Canvas.prefab

    private static bool _booted;

    // UI를 제외할 씬 목록
    private static readonly string[] EXCLUDED_SCENES = { "Main", "Main1" };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetFlag() => _booted = false;

    // 씬 로드 전에 UIRoot 보장
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // 씬 로드시마다 검사 이벤트 연결
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 첫 씬 로드 전에 한 번 실행
        EnsureUIRoot(SceneManager.GetActiveScene());
    }

    // 씬이 로드될 때마다 호출됨
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureUIRoot(scene);
    }

    private static void EnsureUIRoot(Scene scene)
    {
        string sceneName = scene.name;

        // 예외 씬(로그인/타이틀 등)은 스킵
        if (EXCLUDED_SCENES.Contains(sceneName))
        {
            Debug.Log($"[UIBootstrapper] {sceneName}은(는) UI 자동생성 제외대상");
            return;
        }

        // 이미 UIRoot가 있으면 중복 생성 방지
    #if UNITY_2023_1_OR_NEWER
        var existing = Object.FindFirstObjectByType<UIRoot>();
    #else
        var existing = Object.FindObjectOfType<UIRoot>();
    #endif
        if (existing)
        {
            Debug.Log($"[UIBootstrapper] {sceneName}에 이미 UIRoot 존재 → 패스");
            _booted = true;
            return;
        }

        // ✅ Canvas.prefab 로드 및 인스턴스 생성
        var prefab = Resources.Load<GameObject>(PREFAB_PATH);
        if (!prefab)
        {
            Debug.LogError($"[UIBootstrapper] Resources/{PREFAB_PATH}.prefab 을(를) 찾지 못했습니다.");
            return;
        }

        var go = Object.Instantiate(prefab);
        var root = go.GetComponent<UIRoot>() ?? go.AddComponent<UIRoot>();

        // 씬 이동 시에도 유지되도록 DDOL 적용
        if (go.scene.IsValid()) Object.DontDestroyOnLoad(go);

        _booted = true;
        Debug.Log($"[UIBootstrapper] {sceneName}에 UIRoot 자동 생성 완료!");
    }
}
