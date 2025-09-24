using UnityEngine;

public class DungeonPortal : MonoBehaviour
{
    public GameObject dungeonSelectUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            dungeonSelectUI.SetActive(true); // UI 열기
            Time.timeScale = 0f;              // 게임 일시정지
        }
    }
}
