// Assets/Scripts/PauseMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI 오브젝트")]
    [SerializeField] private GameObject pausePanel;   // “정지 UI” 루트 패널
    [SerializeField] private Image dimPanel;     // 화면 흐림용 Image (검정색, 알파 0.6)

    bool isPaused = false;

    void Start()
    {
        // 최초엔 숨김
        if (pausePanel != null) pausePanel.SetActive(false);
        if (dimPanel != null) dimPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    /* ---------- 버튼에서 호출할 public 메서드 ---------- */
    public void OnClickContinue() => TogglePause();

    public void OnClickQuit()
    {
        Time.timeScale = 1;          // 씬 이동 전 시간 복구
        SceneManager.LoadScene("Main1");
    }

    public void OnClickSettings()
    {
        // TODO: 설정 UI 구현
        Debug.Log("Settings: TBD");
    }

    /* ---------- 핵심 토글 ---------- */
    void TogglePause()
    {
        isPaused = !isPaused;

        // UI 토글
        pausePanel?.SetActive(isPaused);
        dimPanel?.gameObject.SetActive(isPaused);

        // 시간 정지 / 재개
        Time.timeScale = isPaused ? 0f : 1f;

        // 마우스 커서 (필요 시)
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None
                                    : CursorLockMode.Locked;
    }
}
