using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("슬라이더")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("오디오 소스")]
    public AudioSource bgmSource;
    public AudioSource[] sfxSources; // 효과음 여러 개 재생 가능

    [Header("효과음 클립")]
    public AudioClip attackClip;

    private int sfxIndex = 0; // 효과음 순환 재생 인덱스

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (bgmSlider == null)
            bgmSlider = GameObject.Find("BGMSlider")?.GetComponent<Slider>();

        if (sfxSlider == null)
            sfxSlider = GameObject.Find("SFXSlider")?.GetComponent<Slider>();

        float savedBGM = PlayerPrefs.GetFloat("BGMSlider", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SFXSlider", 1f);

        if (bgmSlider != null)
        {
            bgmSlider.value = savedBGM;
            bgmSlider.onValueChanged.AddListener(ApplyBGMVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(ApplySFXVolume);
        }

        ApplyBGMVolume(savedBGM);
        ApplySFXVolume(savedSFX);
    }

    public void ApplyBGMVolume(float value)
    {
        if (bgmSource != null)
            bgmSource.volume = value;

        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void ApplySFXVolume(float value)
    {
        foreach (var sfx in sfxSources)
        {
            if (sfx != null)
                sfx.volume = value;
        }

        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    // 🔊 공격 사운드 등 효과음 재생 메서드
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSources.Length == 0) return;

        sfxSources[sfxIndex].PlayOneShot(clip);
        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
    }

    // 공격용으로 간단한 단축 메서드
    public void PlayAttackSFX()
    {
        PlaySFX(attackClip);
    }
}
