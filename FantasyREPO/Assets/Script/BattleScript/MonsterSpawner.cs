using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnArea
    {
        public Transform top;
        public Transform bottom;
        public Transform left;
        public Transform right;

        public Rect GetRect()
        {
            float xMin = left.position.x;
            float xMax = right.position.x;
            float yMin = bottom.position.y;
            float yMax = top.position.y;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }

    public SpawnArea spawnArea;
    public GameObject[] monsterPrefabs;
    public float spawnInterval = 10f;
    public int maxSpawnAttempts = 10;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        InvokeRepeating(nameof(SpawnMonster), spawnInterval, spawnInterval);
    }

    void SpawnMonster()
    {
        Rect area = spawnArea.GetRect();
        Rect cameraView = GetCameraViewRect();

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(area.xMin, area.xMax),
                Random.Range(area.yMin, area.yMax)
            );

            if (!cameraView.Contains(randomPos))
            {
                GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
                Instantiate(prefab, randomPos, Quaternion.identity);
                return;
            }
        }

        Debug.LogWarning("몬스터 생성 실패");
    }

    Rect GetCameraViewRect()
    {
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }
}