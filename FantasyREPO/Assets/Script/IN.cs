using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExitDirection
{
    Right,
    Left,
    Down,
    Up
}

public class Exit : MonoBehaviour
{
    public string sceneName = "IN House";
    public int doorNumber = 1;
    public ExitDirection direction = ExitDirection.Up;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 {gameObject.name}와 충돌 - 씬 이동: {sceneName}, 문 번호: {doorNumber}");
            RoomManager.ChangeScene("IN House", 1);
        }
    }
}