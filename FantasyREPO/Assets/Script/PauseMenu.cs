// Assets/Scripts/PauseMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI ������Ʈ")]
    [SerializeField] private GameObject pausePanel;   // ������ UI�� ��Ʈ �г�
    [SerializeField] private Image dimPanel;     // ȭ�� �帲�� Image (������, ���� 0.6)

    bool isPaused = false;

    void Start()
    {
        // ���ʿ� ����
        if (pausePanel != null) pausePanel.SetActive(false);
        if (dimPanel != null) dimPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    /* ---------- ��ư���� ȣ���� public �޼��� ---------- */
    public void OnClickContinue() => TogglePause();

    public void OnClickQuit()
    {
        Time.timeScale = 1;          // �� �̵� �� �ð� ����
        SceneManager.LoadScene("Main1");
    }

    public void OnClickSettings()
    {
        // TODO: ���� UI ����
        Debug.Log("Settings: TBD");
    }

    /* ---------- �ٽ� ��� ---------- */
    void TogglePause()
    {
        isPaused = !isPaused;

        // UI ���
        pausePanel?.SetActive(isPaused);
        dimPanel?.gameObject.SetActive(isPaused);

        // �ð� ���� / �簳
        Time.timeScale = isPaused ? 0f : 1f;

        // ���콺 Ŀ�� (�ʿ� ��)
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None
                                    : CursorLockMode.Locked;
    }
}
