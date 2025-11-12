using UnityEngine;

[DisallowMultipleComponent]
public class ArrowSFX : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip flyClip;   // 발사 순간/비행 휘잉
    public AudioClip hitClip;   // 명중/꽂힘

    [Header("Volumes")]
    [Range(0f, 1f)] public float flyVol = 0.9f;
    [Range(0f, 1f)] public float hitVol = 1f;

    [Header("Pitch Jitter")]
    [Range(0f, 0.3f)] public float flyJitter = 0.01f;
    [Range(0f, 0.3f)] public float hitJitter = 0.02f;

    bool _hitPlayed;

    void OnEnable()
    {
        // 생성 즉시 발사/비행음
        PlayOne(flyClip, flyVol, flyJitter);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 화살이 무언가에 닿았을 때(플레이어/지형 등) 명중음만 재생
        if (_hitPlayed) return;

        // 화살끼리/적과의 충돌은 무시하고 싶으면 아래 주석 해제
        // if (other.CompareTag("Enemy") || other.CompareTag("Arrow")) return;

        PlayOne(hitClip, hitVol, hitJitter);
        _hitPlayed = true;
        // 파괴는 ArrowScript가 처리함. 우리는 소리만 냄.
    }

    void OnDisable()
    {
        // 혹시 명중 이벤트 못 받았더라도 파괴 직전에 한 번 더 보장하고 싶으면 필요 시 구현
        // (지금은 과재생 방지를 위해 생략)
    }

    void PlayOne(AudioClip clip, float vol, float jitter)
    {
        if (!clip) return;
        var sm = SoundManager.Instance;
        if (sm != null)
        {
            sm.PlaySFX(clip, vol, jitter);
            return;
        }
        using (var _ = new TempOneShot(transform.position, clip, vol, jitter)) { }
    }

    struct TempOneShot : System.IDisposable
    {
        GameObject go; AudioSource src;
        public TempOneShot(Vector3 pos, AudioClip clip, float vol, float jitter)
        {
            go = new GameObject("[Arrow OneShot]");
            go.transform.position = pos;
            src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f; src.playOnAwake = false;
            src.pitch = 1f + (jitter > 0f ? Random.Range(-jitter, jitter) : 0f);
            src.PlayOneShot(clip, vol);
            Object.Destroy(go, clip.length + 0.1f);
        }
        public void Dispose() { }
    }
}
