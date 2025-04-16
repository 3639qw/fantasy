
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum INDirection
{
    Right,
    Left,
    Down,
    Up
}

public class IN : MonoBehaviour
{
    public string sceneName = "HouseTest";
    public int doorNumber = 0;
    public ExitDirection direction = ExitDirection.Up;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 {gameObject.name}와 충돌 - 씬 이동: {sceneName}, 문 번호: {doorNumber}");
            RoomManager.ChangeScene("HouseTest", 0);
        }
    }
}

