using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger2 : MonoBehaviour
{
    public string sceneToLoad2 = "OverWorld"; // 이동할 씬 이름
    public Vector2 spawnPositionInNextScene = new Vector2(5, 1); // 다음 씬에서 플레이어가 위치할 곳

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerPositionManager.NextPlayerPosition = spawnPositionInNextScene;
            SceneManager.LoadScene(sceneToLoad2);
        }
    }
}
