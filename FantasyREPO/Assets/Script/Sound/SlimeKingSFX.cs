// SlimeKingSFX.cs
using UnityEngine;

[DisallowMultipleComponent]
public class SlimeKingSFX : MonoBehaviour
{
    [Header("이동/발자국")]
    public AudioClip[] footstepClips;
    [Range(0f, 1f)] public float footstepVol = 1f;
    [Range(0f, 0.3f)] public float footstepPitchJitter = 0.05f;

    [Header("스킬/행동")]
    public AudioClip beeSummon;
    public AudioClip jumpCharge;
    public AudioClip jumpLaunch;
    public AudioClip jumpImpact;
    public AudioClip roar;

    [Header("재생 공통 옵션")]
    [Tooltip("같은 프레임/중복 재생 방지 간격(초)")]
    public float minInterval = 0.04f;

    [Tooltip("체크하면 SoundManager를 우회하고 이 오브젝트의 AudioSource로 재생")]
    public bool bypassSoundManager = false;

    [Tooltip("SoundManager가 없거나 우회 시 로컬 AudioSource 사용")]
    public bool useLocalSourceFallback = true;

    AudioSource _src;
    float _lastPlay;

    void Awake()
    {
        if (useLocalSourceFallback)
        {
            _src = GetComponent<AudioSource>();
            if (_src == null) _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f; // 2D 게임이면 0
        }
    }

    void OnValidate()
    {
        footstepVol = Mathf.Clamp01(footstepVol);
        footstepPitchJitter = Mathf.Clamp(footstepPitchJitter, 0f, 0.3f);
        if (minInterval < 0f) minInterval = 0f;
    }

    // === 애니메이션 이벤트용 ===
    public void Footstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        Play(clip, footstepVol, footstepPitchJitter, "[Footstep]");
    }

    public void PlayBeeSummon() => Play(beeSummon, 1f, 0.02f, "[BeeSummon]");
    public void PlayJumpCharge() => Play(jumpCharge, 1f, 0.02f, "[JumpCharge]");
    public void PlayJumpLaunch() => Play(jumpLaunch, 1f, 0.02f, "[JumpLaunch]");
    public void PlayJumpImpact() => Play(jumpImpact, 1f, 0.02f, "[JumpImpact]");
    public void PlayRoar() => Play(roar, 1f, 0.02f, "[Roar]");

    public void PlayByName(string name)
    {
        switch ((name ?? "").ToLowerInvariant())
        {
            case "bee":
            case "beesummon": PlayBeeSummon(); break;
            case "charge":
            case "jumpcharge": PlayJumpCharge(); break;
            case "launch":
            case "jumplaunch": PlayJumpLaunch(); break;
            case "impact":
            case "jumpimpact": PlayJumpImpact(); break;
            case "roar": PlayRoar(); break;
        }
    }

    // 내부 공통
    void Play(AudioClip clip, float volume, float pitchJitter, string tag = "")
    {
        if (clip == null) { Debug.LogWarning($"{tag} clip=null"); return; }
        if (Time.time - _lastPlay < minInterval) return;
        _lastPlay = Time.time;

        // 1) SoundManager 경로(우회 옵션이 꺼져 있을 때만)
        var sm = (!bypassSoundManager) ? SoundManager.Instance : null;
        if (sm != null)
        {
            sm.PlaySFX(clip, volume, pitchJitter);
            // Debug.Log($"{tag} via SoundManager: {clip.name}");
            return;
        }

        // 2) 로컬 폴백
        if (!useLocalSourceFallback)
        {
            Debug.LogWarning($"{tag} no playback path (bypass:{bypassSoundManager}, useLocal:false), clip:{clip.name}");
            return;
        }

        if (_src == null)
        {
            _src = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
        }

        float p0 = _src.pitch;
        if (pitchJitter > 0f) _src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        _src.PlayOneShot(clip, Mathf.Clamp01(volume));
        _src.pitch = p0;
        // Debug.Log($"{tag} via LocalSource: {clip.name}");
    }

#if UNITY_EDITOR
    // 에디터에서 바로 재생 테스트(컴포넌트 우클릭 → ContextMenu)
    [ContextMenu("TEST ► Bee Summon")]
    void __TEST_Bee() => PlayBeeSummon();
#endif
}
