using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static int doorNumber = 1;

    void Start()
    {
        GameObject[] exits = GameObject.FindGameObjectsWithTag("Exit");
        foreach (GameObject doorObj in exits)
        {
            Exit exit = doorObj.GetComponent<Exit>();
            if (doorNumber == exit.doorNumber)
            {
                Vector3 playerPos = doorObj.transform.position;

                switch (exit.direction)
                {
                    case ExitDirection.Up:
                        playerPos.y += 1;
                        break;
                    case ExitDirection.Right:
                        playerPos.x += 1;
                        break;
                    case ExitDirection.Down:
                        playerPos.y -= 1;
                        break;
                    case ExitDirection.Left:
                        playerPos.x -= 1;
                        break;
                }

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = playerPos;
                }
                break;
            }
        }
    }

    public static void ChangeScene(string sceneName, int doorNum)
    {
        doorNumber = doorNum;
        SceneManager.LoadScene(sceneName);
    }
}
