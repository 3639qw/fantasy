using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerBowSFXAuto : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Clips")]
    public AudioClip bowDrawClip;      // 시위 당기는 소리
    public AudioClip bowReleaseClip;   // 발사/휘잉

    [Header("Timings")]
    [Tooltip("발사 타이밍까지의 지연 (Bow 트리거 후 화살 나가는 시점). 애니메이션에 맞춰 조절")]
    public float releaseDelay = 0.0f;  // Bow 애니메이션이 즉시 발사면 0, 약간 뒤면 0.1~0.2

    [Header("Animator State Names (Base Layer)")]
    [Tooltip("플레이어 활 애니메이션 상태 이름 (Animator의 State 이름)")]
    public string bowStateName = "Bow";  // 프로젝트 상태명에 맞게 바꿔도 됨
    public int layerIndex = 0;

    [Header("Volumes / Jitter")]
    [Range(0f, 1f)] public float volDraw = 1f;
    [Range(0f, 1f)] public float volRelease = 1f;
    [Range(0f, 0.3f)] public float jitterDraw = 0.01f;
    [Range(0f, 0.3f)] public float jitterRelease = 0.02f;

    int _bowHash, _lastHash;
    Coroutine _co;

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        _bowHash = Animator.StringToHash(bowStateName);
    }

    void Update()
    {
        if (!animator) return;
        var info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        int cur = info.shortNameHash;
        if (cur != _lastHash)
        {
            OnStateChanged(_lastHash, cur);
            _lastHash = cur;
        }
    }

    void OnDisable()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
    }

    void OnStateChanged(int prev, int cur)
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
        if (cur == _bowHash)
            _co = StartCoroutine(CoBowTimeline());
    }

    IEnumerator CoBowTimeline()
    {
        PlayOne(bowDrawClip, volDraw, jitterDraw);         // 장전
        if (releaseDelay > 0f) yield return new WaitForSeconds(releaseDelay);
        PlayOne(bowReleaseClip, volRelease, jitterRelease); // 발사
        _co = null;
    }

    void PlayOne(AudioClip clip, float vol, float jitter)
    {
        if (!clip) return;

        var sm = SoundManager.Instance;
        if (sm != null) { sm.PlaySFX(clip, vol, jitter); return; }

        using (var _ = new TempOneShot(transform.position, clip, vol, jitter)) { }
    }

    struct TempOneShot : System.IDisposable
    {
        GameObject go; AudioSource src;
        public TempOneShot(Vector3 pos, AudioClip clip, float vol, float jitter)
        {
            go = new GameObject("[PlayerBow OneShot]");
            go.transform.position = pos;
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f;
            src.pitch = 1f + (jitter > 0f ? Random.Range(-jitter, jitter) : 0f);
            src.PlayOneShot(clip, vol);
            Object.Destroy(go, clip.length + 0.1f);
        }
        public void Dispose() { }
    }
}
