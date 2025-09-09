using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonSelectButton : MonoBehaviour
{
    // 인스펙터에서 씬 이름 입력
    public string sceneName;

    public void OnClickEnterDungeon()
    {
        Time.timeScale = 1f; // 시간 다시 정상으로
        SceneManager.LoadScene(sceneName);
    }
}
