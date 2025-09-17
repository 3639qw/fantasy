using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonSelectButton : MonoBehaviour
{
    // �ν����Ϳ��� �� �̸� �Է�
    public string sceneName;

    public void OnClickEnterDungeon()
    {
        Time.timeScale = 1f; // �ð� �ٽ� ��������
        SceneManager.LoadScene(sceneName);
    }
}
