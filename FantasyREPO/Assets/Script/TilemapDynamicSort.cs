using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// �÷��̾ ���� ���� Z ���� ��¦ ������ �� SortingOrder ȿ���� �ش�.
/// </summary>
public class TilemapDynamicSort : MonoBehaviour
{
    [Header("����")]
    [SerializeField] Transform player;          // �÷��̾� Ʈ������
    [SerializeField] Tilemap targetMap;       // ����/���� Tilemap

    [Header("Z ������ (�÷��̾�=50 ���� ��)")]
    [SerializeField] float zOffset = -0.1f;     // ���� �� ī�޶� ����

    Vector3Int prevCell = Vector3Int.one * int.MaxValue;

    void LateUpdate()
    {
        Vector3Int cell = targetMap.WorldToCell(player.position);

        if (cell == prevCell) return;                       // ���� �� �� ����

        // 1) ���� �� ����
        if (targetMap.HasTile(prevCell))
            targetMap.SetTransformMatrix(prevCell, Matrix4x4.identity);

        // 2) �� �� ������
        if (targetMap.HasTile(cell))
        {
            Matrix4x4 m = Matrix4x4.TRS(
                new Vector3(0, 0, zOffset),   // z ������
                Quaternion.identity,
                Vector3.one);
            targetMap.SetTransformMatrix(cell, m);
            prevCell = cell;
        }
        else prevCell = Vector3Int.one * int.MaxValue;     // Ÿ���� ������ �ʱ�ȭ
    }
}
