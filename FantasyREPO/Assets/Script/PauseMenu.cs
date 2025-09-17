// Assets/Scripts/PauseMenu.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI ������Ʈ")]
    [SerializeField] private GameObject pausePanel;        // ����(���/����/������) �г�
    [SerializeField] private GameObject soundSettingsPanel; // ���� ���� �г� (BGM/SFX �����̴�)
    [SerializeField] private Image dimPanel;               // ��Ӱ� ó���� �г� (������, ���� 0.5~0.7)

    private bool isPaused = false;

    private void Start()
    {
        // ���� �� ��� ����
        if (pausePanel) pausePanel.SetActive(false);
        if (soundSettingsPanel) soundSettingsPanel.SetActive(false);
        if (dimPanel) dimPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ���� ������ ���� ������ �� ���� �ݰ� ����� ����
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

    /* ---------- ��ư���� ȣ�� ---------- */
    public void OnClickContinue() => TogglePause();

    public void OnClickQuit()
    {
        Time.timeScale = 1f; // �� �̵� �� �ð� ����ȭ
        SceneManager.LoadScene("Main1");
    }

    // ���� ��ư: ��� ����� ���� ������ ǥ��
    public void OnClickSettings()
    {
        if (!soundSettingsPanel) { Debug.LogWarning("[PauseMenu] soundSettingsPanel ������"); return; }

        // �Ͻ����� ���� ����
        EnsurePaused(true);

        // UI ��ȯ
        if (pausePanel) pausePanel.SetActive(false);
        soundSettingsPanel.SetActive(true);

        // �ֻ������(�ٸ� UI�� ������ �ʵ���)
        soundSettingsPanel.transform.SetAsLastSibling();

        Debug.Log("[PauseMenu] Open Sound Settings");
    }

    // ���� �������� �ڷΰ��� ��ư
    public void OnClickBackFromSound()
    {
        CloseSettingsToPause();
    }

    /* ---------- ���� ���� ---------- */
    private void TogglePause()
    {
        isPaused = !isPaused;
        EnsurePaused(isPaused);

        // ���� �гθ� ���(���� �г��� �׻� ���α�)
        if (pausePanel) pausePanel.SetActive(isPaused);
        if (soundSettingsPanel) soundSettingsPanel.SetActive(false);
    }

    // �Ͻ����� ���� ó�� + ���� UI ���
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

        // ������ �Ͻ����� ���� ����
        EnsurePaused(true);
        Debug.Log("[PauseMenu] Back to Pause from Settings");
    }
}
