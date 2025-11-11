// MushroomExplosionSFX.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MushroomExplosionSFX : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip explodeClip;        // 펑! 한 번
    public AudioClip poisonLoopClip;     // 지이잉~ 루프(선택)

    [Header("Volumes")]
    [Range(0f, 1f)] public float explodeVolume = 1f;
    [Range(0f, 1f)] public float loopVolume = 0.8f;

    [Header("Options")]
    [Tooltip("활성화될 때 자동 재생(폭발 즉시 소리 나게)")]
    public bool playOnEnable = true;
    [Tooltip("PoisonScript의 poisonLifetime에 맞춰 자동 페이드아웃")]
    public bool bindToPoisonLifetime = true;
    [Tooltip("루프 페이드아웃 시간(초)")]
    public float loopFadeOutTime = 0.25f;

    [Header("Routing")]
    [Tooltip("원샷(폭발)은 SoundManager를 우회하고 로컬로 재생")]
    public bool bypassSoundManagerForExplode = false;

    [Tooltip("원샷 재생에 피치 랜덤(가벼운 변주)")]
    [Range(0f, 0.5f)] public float explodePitchJitter = 0.03f;

    AudioSource _src;           // 루프 전용(로컬)
    AudioSource _oneshotSrc;    // 원샷용(필요시 임시)
    Coroutine _loopCo;

    void Awake()
    {
        // 루프용 소스 (이 오브젝트 수명과 함께 함)
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = false;            // 루프는 코루틴으로 컨트롤
        _src.spatialBlend = 0f;       // 2D 믹스 (원하면 3D로 변경)

        // 원샷 전용 임시 소스(필요할 때 생성)
        _oneshotSrc = gameObject.AddComponent<AudioSource>();
        _oneshotSrc.playOnAwake = false;
        _oneshotSrc.loop = false;
        _oneshotSrc.spatialBlend = 0f;
        _oneshotSrc.volume = 1f;
    }

    void OnEnable()
    {
        if (playOnEnable)
            PlayNow();
    }

    public void PlayNow()
    {
        // 1) 폭발 원샷
        if (explodeClip != null)
        {
            var sm = (bypassSoundManagerForExplode) ? null : SoundManager.Instance;
            if (sm != null)
            {
                sm.PlaySFX(explodeClip, explodeVolume, explodePitchJitter);
            }
            else
            {
                float p0 = _oneshotSrc.pitch;
                if (explodePitchJitter > 0f)
                    _oneshotSrc.pitch = 1f + Random.Range(-explodePitchJitter, explodePitchJitter);
                _oneshotSrc.PlayOneShot(explodeClip, explodeVolume);
                _oneshotSrc.pitch = p0;
            }
        }

        // 2) 독 구름 루프 (선택)
        if (poisonLoopClip != null)
        {
            if (_loopCo != null) StopCoroutine(_loopCo);
            _loopCo = StartCoroutine(CoPlayLoopWithOptionalAutoStop());
        }
    }

    IEnumerator CoPlayLoopWithOptionalAutoStop()
    {
        // 루프용 별도 오디오소스로 시작
        _src.clip = poisonLoopClip;
        _src.volume = loopVolume;
        _src.loop = true;
        _src.Play();

        if (!bindToPoisonLifetime)
            yield break;

        // PoisonScript에서 수명 가져오기(없으면 패스)
        float lifetime = 0f;
        var poison = GetComponent<PoisonScript>();
        if (poison != null) lifetime = Mathf.Max(0f, poison.poisonLifetime);

        // lifetime이 0이면 자동 정지 안 함
        if (lifetime <= 0f) yield break;

        // 수명 동안 대기(페이드아웃 고려해서 약간 앞당겨 정지)
        float wait = Mathf.Max(0f, lifetime - loopFadeOutTime);
        yield return new WaitForSeconds(wait);

        // 페이드아웃
        if (loopFadeOutTime > 0f)
        {
            float t = 0f;
            float v0 = _src.volume;
            while (t < loopFadeOutTime)
            {
                t += Time.deltaTime;
                _src.volume = Mathf.Lerp(v0, 0f, t / loopFadeOutTime);
                yield return null;
            }
        }

        _src.Stop();
        _src.volume = loopVolume;
        _loopCo = null;
    }

#if UNITY_EDITOR
    [ContextMenu("TEST ► Play Now")]
    void __TEST() => PlayNow();
#endif
}
