using UnityEngine;
using System; // [System.Serializable]을 사용하기 위해 필요

// 1. 인스펙터 창에서 사운드를 관리하기 편하도록 별도의 클래스를 정의합니다.
//    [System.Serializable] 어트리뷰트가 있어야 인스펙터에 노출됩니다.
[System.Serializable]
public class Sound
{
    public string name;      // 사운드를 구분할 이름 (예: "Jump", "Coin", "MainTheme")
    public AudioClip clip; // 실제 오디오 파일 (AudioClip)
}

public class SoundManage : MonoBehaviour
{
    // 2. 싱글톤 인스턴스: 씬 내 어디서든 이 SoundManager에 접근할 수 있게 합니다.
    public static SoundManage instance;

    // 3. 사운드 재생기 컴포넌트
    // BGM은 하나만, 루프 재생 / SFX는 여러 개가, 겹쳐서 재생되어야 합니다.
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    // 4. 관리할 사운드 목록 (인스펙터에서 채워줄 배열)
    public Sound[] bgmSounds; // BGM 목록
    public Sound[] sfxSounds; // SFX 목록

    void Awake()
    {
        // 5. 싱글톤 설정
        if (instance == null)
        {
            instance = this;
            // 씬이 변경되어도 이 SoundManager 오브젝트가 파괴되지 않도록 설정
            // (BGM이 씬 전환 시 끊기지 않게 하기 위함)
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 씬에 이미 SoundManager가 존재한다면 새로 생긴 것을 파괴
            Destroy(gameObject);
        }
    }

    // 6. BGM 재생 함수
    public void PlayBGM(string name)
    {
        // 이름에 맞는 BGM 클립을 찾습니다.
        AudioClip clip = FindClip(name, bgmSounds);
        if (clip != null)
        {
            // 만약 현재 재생 중인 BGM과 같은 것이라면 다시 재생하지 않습니다.
            if (bgmSource.clip == clip && bgmSource.isPlaying)
                return;

            bgmSource.clip = clip;
            bgmSource.loop = true; // BGM은 항상 루프
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning("SoundManager: BGM을 찾을 수 없습니다 - " + name);
        }
    }

    // 7. SFX (효과음) 재생 함수
    public void PlaySFX(string name)
    {
        // 이름에 맞는 SFX 클립을 찾습니다.
        AudioClip clip = FindClip(name, sfxSounds);
        if (clip != null)
        {
            // PlayOneShot: 현재 재생 중인 소리를 멈추지 않고, 겹쳐서 재생합니다.
            // (점프 중에 코인 먹는 소리가 같이 날 수 있음)
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("SoundManager: SFX를 찾을 수 없습니다 - " + name);
        }
    }

    // 8. BGM 정지 함수
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // 9. 이름으로 오디오 클립을 찾는 내부 헬퍼 함수
    private AudioClip FindClip(string name, Sound[] soundArray)
    {
        foreach (Sound s in soundArray)
        {
            if (s.name == name)
            {
                return s.clip;
            }
        }
        return null;
    }
}