// ResultSlot.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResultSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 플레이어가 결과 슬롯에 드롭 시도 → 무시
        Debug.Log("ResultSlot에는 드롭 불가");
    }
}
