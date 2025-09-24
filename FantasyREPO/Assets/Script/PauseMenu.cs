// Assets/Scripts/PauseMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI 오브젝트")]
    [SerializeField] private GameObject pausePanel;         // 퍼즈(계속/설정/나가기) 패널
    [SerializeField] private GameObject soundSettingsPanel; // 사운드 설정 패널 (BGM/SFX 슬라이더)
    [SerializeField] private Image dimPanel;                // 어둡게 처리용 패널 (알파 0.5~0.7)

    private bool isPaused = false;

    private void Start()
    {
        // 시작 시 모두 숨김
        if (pausePanel) pausePanel.SetActive(false);
        if (soundSettingsPanel) soundSettingsPanel.SetActive(false);
        if (dimPanel) dimPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 사운드 설정이 열려 있으면 → 설정 닫고 퍼즈로 복귀
            if (soundSettingsPanel && soundSettingsPanel.activeSelf)
            {
                CloseSettingsToPause();
            }
            else
            {
                TogglePause();
            }
        }
    }

    /* ---------- 버튼에서 호출 ---------- */
    public void OnClickContinue() => TogglePause();

    public void OnClickQuit()
    {
        Time.timeScale = 1f; // 씬 이동 전 시간 정상화
        SceneManager.LoadScene("Main1");
    }

    // 설정 버튼: 퍼즈를 숨기고 사운드 설정을 표시
    public void OnClickSettings()
    {
        if (!soundSettingsPanel) { Debug.LogWarning("[PauseMenu] soundSettingsPanel 미지정"); return; }

        // 일시정지 상태 유지
        EnsurePaused(true);

        // UI 전환
        if (pausePanel) pausePanel.SetActive(false);
        soundSettingsPanel.SetActive(true);

        // 최상단으로(다른 UI에 가리지 않도록)
        soundSettingsPanel.transform.SetAsLastSibling();

        Debug.Log("[PauseMenu] Open Sound Settings");
    }

    // 사운드 설정에서 뒤로가기 버튼
    public void OnClickBackFromSound()
    {
        CloseSettingsToPause();
    }

    /* ---------- 내부 로직 ---------- */
    private void TogglePause()
    {
        isPaused = !isPaused;
        EnsurePaused(isPaused);

        // 퍼즈 패널만 토글(설정 패널은 항상 꺼두기)
        if (pausePanel) pausePanel.SetActive(isPaused);
        if (soundSettingsPanel) soundSettingsPanel.SetActive(false);
    }

    // 일시정지 상태 처리 + 공통 UI 토글
    private void EnsurePaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (dimPanel) dimPanel.gameObject.SetActive(paused);
    }

    private void CloseSettingsToPause()
    {
        if (soundSettingsPanel) soundSettingsPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);

        // 여전히 일시정지 상태 유지
        EnsurePaused(true);
        Debug.Log("[PauseMenu] Back to Pause from Settings");
    }
}
