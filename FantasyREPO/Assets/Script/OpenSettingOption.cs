using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuPanel;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        pauseMenuPanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1;
    }

    public void OnClickSettings()
    {
        Debug.Log("설정 버튼 클릭됨");
        // 설정 창 띄우는 로직은 나중에 추가
    }

    public void OnClickExit()
    {
        Debug.Log("종료하기 버튼 클릭됨");
        Time.timeScale = 1;
        SceneManager.LoadScene("TitleScene"); // 혹은 Application.Quit();
    }
}
