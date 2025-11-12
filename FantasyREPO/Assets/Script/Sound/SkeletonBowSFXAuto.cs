using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SkeletonBowSFXAuto : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Clips")]
    public AudioClip bowDrawClip;    // 시위 당기는 소리
    public AudioClip bowReleaseClip; // 발사/휘익

    [Header("Timings")]
    [Tooltip("SkeletonAI가 화살을 발사하기까지의 지연(기본 0.3s)")]
    public float releaseDelay = 0.3f;

    [Header("Animator State Names (Base Layer)")]
    [Tooltip("공격 애니메이션 상태 이름(애니메이터의 State 이름과 일치)")]
    public string attackStateName = "Attack"; // 필요 시 인스펙터에서 실제 이름으로 변경
    public int layerIndex = 0;

    [Header("Volumes")]
    [Range(0f, 1f)] public float volDraw = 1f;
    [Range(0f, 1f)] public float volRelease = 1f;

    [Header("Pitch Jitter")]
    [Range(0f, 0.3f)] public float jitterDraw = 0.01f;
    [Range(0f, 0.3f)] public float jitterRelease = 0.02f;

    int _attackHash, _lastHash;
    Coroutine _co;

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        _attackHash = Animator.StringToHash(attackStateName);
    }

    void Update()
    {
        if (!animator) return;
        var info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        int h = info.shortNameHash;
        if (h != _lastHash)
        {
            OnStateChanged(_lastHash, h);
            _lastHash = h;
        }
    }

    void OnDisable()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
    }

    void OnStateChanged(int prev, int cur)
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
        if (cur == _attackHash)
            _co = StartCoroutine(CoAttackTimeline());
    }

    IEnumerator CoAttackTimeline()
    {
        PlayOne(bowDrawClip, volDraw, jitterDraw);
        if (releaseDelay > 0f) yield return new WaitForSeconds(releaseDelay);
        PlayOne(bowReleaseClip, volRelease, jitterRelease);
        _co = null;
    }

    void PlayOne(AudioClip clip, float vol, float jitter)
    {
        if (!clip) return;

        // 1) SoundManager 우선
        var sm = SoundManager.Instance;
        if (sm != null)
        {
            sm.PlaySFX(clip, vol, jitter);
            return;
        }

        // 2) 로컬 OneShot (오브젝트 파괴와 무관하게 들리도록 임시 AudioSource 사용)
        using (var _ = new TempOneShot(transform.position, clip, vol, jitter)) { }
    }

    // 임시 원샷 유틸 (파괴 안전)
    struct TempOneShot : System.IDisposable
    {
        GameObject go; AudioSource src;
        public TempOneShot(Vector3 pos, AudioClip clip, float vol, float jitter)
        {
            go = new GameObject("[OneShot]");
            go.transform.position = pos;
            src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f; src.playOnAwake = false;
            float p0 = 1f + (jitter > 0f ? Random.Range(-jitter, jitter) : 0f);
            src.pitch = p0;
            src.PlayOneShot(clip, vol);
            Object.Destroy(go, clip.length + 0.1f);
        }
        public void Dispose() { /* no-op */ }
    }
}
