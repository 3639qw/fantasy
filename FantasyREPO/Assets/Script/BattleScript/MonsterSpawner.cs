using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterSpawner : MonoBehaviour
{
    // 1. 싱글톤 인스턴스: 다른 스크립트가 MonsterSpawner.Instance로 접근하게 함
    public static MonsterSpawner Instance { get; private set; }

    [Header("스폰 영역 설정")]
    [Tooltip("몬스터가 스폰될 땅(Tile)의 콜라이더. 'SpawnHere' 레이어를 가져야 함.")]
    public Collider2D spawnZoneCollider; 

    [Header("몬스터 설정")]
    public GameObject[] monsterPrefabs; // (슬라임 킹이 소환할 몬스터도 이 배열에서 랜덤으로 뽑힘)
    public float spawnInterval = 10f;
    [Tooltip("유효한 스폰 지점을 찾기 위한 최대 시도 횟수")]
    public int maxSpawnAttempts = 50; 

    [Header("스폰 예외 처리 (Layer)")]
    [Tooltip("몬스터가 스폰 *불가능한* 레이어 (예: Wall, Rock, Water)")]
    public LayerMask obstacleLayer; // "DontSpawnHere" (금지) 레이어

    [Tooltip("스폰 지점의 여유 공간 (몬스터의 반지름 크기)")]
    public float clearanceRadius = 0.5f;

    private Camera mainCamera;
    private Bounds _spawnBounds; // 스폰 영역의 Bounds

    public bool startSpawningOnLoad = true;

    void Awake()
    {
        // 2. 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[MonsterSpawner] 씬에 'MainCamera' 태그가 달린 카메라가 없습니다!");
            return;
        }

        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogError("[MonsterSpawner] 'Monster Prefabs' 배열이 비어있습니다!");
            return;
        }

        if (spawnZoneCollider == null)
        {
            Debug.LogError("[MonsterSpawner] 'Spawn Zone Collider'가 할당되지 않았습니다!");
            return;
        }
        _spawnBounds = spawnZoneCollider.bounds; // 스폰 영역의 실제 Bounds를 미리 계산

        if (startSpawningOnLoad)
        {
            Debug.Log("몬스터 생성 시작!");
            InvokeRepeating(nameof(SpawnMonster), spawnInterval, spawnInterval); 
        }
    }

    // 3. 주기적인 스폰 함수 (기존 로직)
    void SpawnMonster()
    {
        // 핵심 로직을 호출 (실패 시 로그는 띄우지 않음)
        TrySpawnSingleMonster(false);
    }

    // 4. [신규] 슬라임 킹이 호출할 공용 함수
    /// <summary>
    /// 지정된 수량만큼 몬스터 소환을 요청합니다. (0.1초 간격으로 순차적 생성)
    /// </summary>
    public void SpawnBossMinions(int count)
    {
        StartCoroutine(SpawnBossMinionsRoutine(count));
    }

    private IEnumerator SpawnBossMinionsRoutine(int count)
    {
        int spawnedCount = 0;
        for (int i = 0; i < count; i++)
        {
            // 핵심 로직을 호출 (실패 시 로그를 띄움)
            bool success = TrySpawnSingleMonster(true); 
            if (success)
            {
                spawnedCount++;
                yield return new WaitForSeconds(0.1f); // 0.1초 간격으로 '타다닥' 소환
            }
        }
        Debug.Log($"[MonsterSpawner] 보스 요청: {spawnedCount} / {count} 마리 소환 완료.");
    }


    // 5. [수정됨] 모든 스폰 로직이 통합된 '핵심 함수'
    /// <summary>
    /// 몬스터 1마리를 유효한 위치에 스폰하려고 시도합니다.
    /// </summary>
    /// <param name="logFailure">실패 시 콘솔에 에러 로그를 남길지 여부</param>
    /// <returns>스폰 성공 시 true, 실패 시 false</returns>
    public bool TrySpawnSingleMonster(bool logFailure)
    {
        Rect cameraView = GetCameraViewRect();

        // [버그 수정] spawnZoneCollider의 레이어 번호(int)를 LayerMask로 올바르게 변환
        int spawnLayerInt = spawnZoneCollider.gameObject.layer;
        LayerMask spawnLayerMask = 1 << spawnLayerInt; // (예: 9 -> 1000000000)

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // 1. 랜덤 위치 생성 (Bounds 내부)
            Vector2 randomPos = new Vector2(
                Random.Range(_spawnBounds.min.x, _spawnBounds.max.x),
                Random.Range(_spawnBounds.min.y, _spawnBounds.max.y)
            );

            // 2. 예외 1: 카메라 뷰
            if (cameraView.Contains(randomPos)) continue;

            // 3. 예외 2: 'SpawnHere' 레이어 위인가? (수정된 spawnLayerMask 사용)
            Collider2D spawnGround = Physics2D.OverlapPoint(randomPos, spawnLayerMask);
            if (spawnGround == null) continue;

            // 4. 예외 3: 'DontSpawnHere' 레이어와 겹치는가?
            Collider2D obstacle = Physics2D.OverlapCircle(randomPos, clearanceRadius, obstacleLayer);
            if (obstacle != null) continue;
            
            // 5. 모든 예외 통과! 몬스터 생성
            GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
            Instantiate(prefab, randomPos, Quaternion.identity);
            return true; // 생성 성공!
        }

        // for 루프(150번)를 다 돌고도 실패
        if (logFailure)
            Debug.LogError($"[MonsterSpawner] 1마리 생성 실패: {maxSpawnAttempts}번 모두 실패.");
        
        return false; // 생성 실패
    }
    
    Rect GetCameraViewRect()
    {
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }
}