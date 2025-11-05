using UnityEngine;

public static class UIBootstrapper
{
    // Resources 상대 경로 (확장자 X)
    private const string PREFAB_PATH = "Canvas"; // => Assets/Resources/Canvas.prefab

    private static bool _booted;

    // Domain Reload OFF 대비 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetFlag() => _booted = false;

    // 씬 로드 전에 UIRoot 보장
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureUIRoot()
    {
        if (_booted) return;

        // 이미 살아있으면 패스
    #if UNITY_2023_1_OR_NEWER
        var already = Object.FindFirstObjectByType<UIRoot>();
    #else
        var already = Object.FindObjectOfType<UIRoot>();
    #endif
        if (already) { _booted = true; return; }

        // Resources에서 Canvas.prefab 로드
        var prefab = Resources.Load<GameObject>(PREFAB_PATH);
        if (!prefab)
        {
            Debug.LogError($"[UIBootstrapper] Resources/{PREFAB_PATH}.prefab 를 찾지 못했습니다.");
            return;
        }

        // 인스턴스 생성
        var go = Object.Instantiate(prefab);

        // 프리팹에 UIRoot가 없다면 자동으로 붙여서 DDOL/이벤트시스템까지 보장
        var root = go.GetComponent<UIRoot>();
        if (!root) root = go.AddComponent<UIRoot>();

        // 혹시라도 루트가 DDOL을 못 걸었을 경우 대비(중복 안전)
        if (go.scene.IsValid()) Object.DontDestroyOnLoad(go);

        _booted = true;
        Debug.Log("[UIBootstrapper] UIRoot auto-instantiated.");
    }
}
