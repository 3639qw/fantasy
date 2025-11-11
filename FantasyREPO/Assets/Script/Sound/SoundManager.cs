using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public enum ToolType { None, Pickaxe, Axe, Hoe, Sword, WateringCan, Hammer }

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("슬라이더")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("오디오 소스")]
    public AudioSource bgmSource;
    [Tooltip("동시재생용 SFX 소스들(2~6개 추천)")]
    public AudioSource[] sfxSources;

    [Header("공통 SFX")]
    public AudioClip attackClip;

    // === 발자국(지형 무시) ===
    [Header("발자국 클립(지형 무시)")]
    public AudioClip[] footstepWalk;
    public AudioClip[] footstepRun;
    [Tooltip("걷기 간격(초)")]
    public float footstepIntervalWalk = 0.35f;
    [Tooltip("달리기 간격(초)")]
    public float footstepIntervalRun = 0.22f;
    [Tooltip("발자국 최소 속도(이 값보다 느리면 무음)")]
    public float speedThreshold = 0.1f;
    [Range(0f, 1f)] public float footstepVolume = 1f;
    [Tooltip("발자국마다 피치 랜덤(±값)")]
    [Range(0f, 0.5f)] public float footstepRandomPitch = 0.05f;

    private float _lastFootstepTime;

    // === 도구 사운드 ===
    [Serializable]
    public class ToolClips
    {
        public ToolType tool;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volumeScale = 1f;
        [Range(0f, 0.5f)] public float randomPitch = 0.05f;
    }
    [Header("도구 사운드 매핑")]
    public ToolClips[] toolClips;
    private Dictionary<ToolType, ToolClips> _toolMap;

    private int sfxIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _toolMap = new Dictionary<ToolType, ToolClips>();
        foreach (var t in toolClips) if (t != null) _toolMap[t.tool] = t;
    }

    private void Start()
    {
        float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (bgmSlider) bgmSlider.value = savedBGM;
        if (sfxSlider) sfxSlider.value = savedSFX;

        ApplyBGMVolume(savedBGM);
        ApplySFXVolume(savedSFX);

        if (bgmSlider) bgmSlider.onValueChanged.AddListener(ApplyBGMVolume);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(ApplySFXVolume);
    }

    private void OnApplicationQuit() => PlayerPrefs.Save();

    // === 볼륨 ===
    public void ApplyBGMVolume(float value)
    {
        if (bgmSource) bgmSource.volume = value;
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void ApplySFXVolume(float value)
    {
        foreach (var sfx in sfxSources) if (sfx) sfx.volume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    // === 공통 SFX ===
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchJitter = 0f)
    {
        if (!clip || sfxSources == null || sfxSources.Length == 0) return;

        var src = sfxSources[sfxIndex];
        if (!src) return;

        // 피치 랜덤
        float oldPitch = src.pitch;
        if (pitchJitter > 0f)
        {
            float delta = UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            src.pitch = Mathf.Clamp(oldPitch + delta, 0.5f, 2f);
        }

        src.PlayOneShot(clip, volumeScale);

        // 원복
        src.pitch = oldPitch;
        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
    }

    public void PlayAttackSFX() => PlaySFX(attackClip);

    // === 발자국(간단) ===
    /// <summary>
    /// 이동 속도와 러닝 여부만 받아서 발자국을 플레이(지형 무시).
    /// PlayerMovement 수정 없이 외부 스크립트에서 호출하면 됩니다.
    /// </summary>
    public void TryPlayFootstepSimple(float speed, bool isRunning)
    {
        if (speed < speedThreshold) return;

        float interval = isRunning ? footstepIntervalRun : footstepIntervalWalk;
        if (Time.time - _lastFootstepTime < interval) return;

        var bank = isRunning && footstepRun != null && footstepRun.Length > 0
            ? footstepRun
            : footstepWalk;

        var clip = RandomClip(bank);
        if (clip) PlaySFX(clip, footstepVolume, footstepRandomPitch);

        _lastFootstepTime = Time.time;
    }

    // 애니메이션 이벤트용(쿨다운만 씀)
    public void PlayFootstepByAnim(bool isRunning)
    {
        float interval = isRunning ? footstepIntervalRun : footstepIntervalWalk;
        if (Time.time - _lastFootstepTime < interval) return;

        var bank = isRunning && footstepRun != null && footstepRun.Length > 0
            ? footstepRun
            : footstepWalk;

        var clip = RandomClip(bank);
        if (clip) PlaySFX(clip, footstepVolume, footstepRandomPitch);

        _lastFootstepTime = Time.time;
    }

    private AudioClip RandomClip(AudioClip[] bank)
    {
        if (bank == null || bank.Length == 0) return null;
        int idx = UnityEngine.Random.Range(0, bank.Length);
        return bank[idx];
    }

    // === 도구 ===
    public void PlayToolSFX(ToolType tool, float intensity01 = 1f)
    {
        if (!_toolMap.TryGetValue(tool, out var cfg) || cfg.clips == null || cfg.clips.Length == 0) return;
        var clip = RandomClip(cfg.clips);
        if (!clip) return;

        float vol = Mathf.Clamp01(cfg.volumeScale * Mathf.Lerp(0.75f, 1.2f, Mathf.Clamp01(intensity01)));
        PlaySFX(clip, vol, cfg.randomPitch);
    }
}
