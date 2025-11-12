using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SlimeSFXAuto : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;   // 속도 체크용(없어도 동작)

    [Header("Animator State Names (Base Layer)")]
    [SerializeField] private string moveStateName = "Slime_Move";
    [SerializeField] private string dieStateName = "Slime_die";
    [SerializeField] private int layerIndex = 0;

    [Header("Clips")]
    public AudioClip hopClip;   // 점프/이동 시 통통
    public AudioClip dieClip;   // 죽음

    [Header("Hop (Move) Options")]
    [Tooltip("이동 상태에서 홉 사운드 주기(초)")]
    public float hopInterval = 0.42f;
    [Tooltip("속도가 이 값 이상일 때만 홉 사운드(0이면 항상 재생)")]
    public float velocityThreshold = 0.05f;
    [Range(0f, 1f)] public float hopVolume = 1f;
    [Range(0f, 0.3f)] public float hopPitchJitter = 0.03f;

    [Header("Die Options")]
    [Range(0f, 1f)] public float dieVolume = 1f;
    [Range(0f, 0.3f)] public float diePitchJitter = 0.02f;

    int _moveHash, _dieHash, _lastHash;
    Coroutine _moveCo;

    void Reset()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        _moveHash = Animator.StringToHash(moveStateName);
        _dieHash = Animator.StringToHash(dieStateName);
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
        if (_moveCo != null) { StopCoroutine(_moveCo); _moveCo = null; }
    }

    void OnStateChanged(int prev, int cur)
    {
        // 이동 루틴 관리
        if (_moveCo != null) { StopCoroutine(_moveCo); _moveCo = null; }

        if (cur == _moveHash)
        {
            _moveCo = StartCoroutine(CoHopLoop());
        }
        else if (cur == _dieHash)
        {
            // 죽음 사운드 1회
            PlayOne(dieClip, dieVolume, diePitchJitter);
        }
    }

    IEnumerator CoHopLoop()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            if (t >= hopInterval)
            {
                t = 0f;
                if (ShouldPlayHop())
                    PlayOne(hopClip, hopVolume, hopPitchJitter);
            }
            yield return null;
        }
    }

    bool ShouldPlayHop()
    {
        if (!hopClip) return false;
        if (!rb || velocityThreshold <= 0f) return true;
        return rb.linearVelocity.sqrMagnitude >= velocityThreshold * velocityThreshold;
    }

    void PlayOne(AudioClip clip, float vol, float jitter)
    {
        if (!clip) return;

        // 1) SoundManager 우선 사용
        var sm = SoundManager.Instance;
        if (sm != null)
        {
            sm.PlaySFX(clip, vol, jitter);
            return;
        }

        // 2) 로컬 임시 오디오(파괴돼도 끝까지 재생)
        using (var _ = new TempOneShot(transform.position, clip, vol, jitter)) { }
    }

    // 임시 원샷 유틸
    struct TempOneShot : System.IDisposable
    {
        GameObject go; AudioSource src;
        public TempOneShot(Vector3 pos, AudioClip clip, float vol, float jitter)
        {
            go = new GameObject("[Slime OneShot]");
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
