using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 플레이어가 밟은 셀의 Z 값을 살짝 앞으로 빼 SortingOrder 효과를 준다.
/// </summary>
public class TilemapDynamicSort : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] Transform player;          // 플레이어 트랜스폼
    [SerializeField] Tilemap targetMap;       // 씨앗/농지 Tilemap

    [Header("Z 오프셋 (플레이어=50 보다 앞)")]
    [SerializeField] float zOffset = -0.1f;     // 음수 → 카메라 앞쪽

    Vector3Int prevCell = Vector3Int.one * int.MaxValue;

    void LateUpdate()
    {
        Vector3Int cell = targetMap.WorldToCell(player.position);

        if (cell == prevCell) return;                       // 같은 셀 → 무시

        // 1) 이전 셀 원복
        if (targetMap.HasTile(prevCell))
            targetMap.SetTransformMatrix(prevCell, Matrix4x4.identity);

        // 2) 새 셀 앞으로
        if (targetMap.HasTile(cell))
        {
            Matrix4x4 m = Matrix4x4.TRS(
                new Vector3(0, 0, zOffset),   // z 오프셋
                Quaternion.identity,
                Vector3.one);
            targetMap.SetTransformMatrix(cell, m);
            prevCell = cell;
        }
        else prevCell = Vector3Int.one * int.MaxValue;     // 타일이 없으면 초기화
    }
}
