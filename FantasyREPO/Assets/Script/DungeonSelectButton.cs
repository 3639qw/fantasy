using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonSelectButton : MonoBehaviour
{
    
    public string sceneName;

    public void OnClickEnterDungeon()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
