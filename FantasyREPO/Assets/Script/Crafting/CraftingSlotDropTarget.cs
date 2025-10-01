using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingSlotDropTarget : MonoBehaviour, IDropHandler
{
    [Tooltip("이 슬롯이 몇 번째 재료 슬롯인지 설정 (0부터 시작)")]
    public int slotIndex;

    // CraftingUI를 쉽게 찾기 위한 참조
    private CraftingUI craftingUI;

    private void Awake()
    {
        // 부모 오브젝트에서 CraftingUI 컴포넌트를 찾아 저장해 둡니다.
        craftingUI = GetComponentInParent<CraftingUI>();
    }

    // 아이템이 이 슬롯에 드롭되었을 때 호출됩니다.
    public void OnDrop(PointerEventData eventData)
    {
        // 드래그된 오브젝트에서 SlotDrag 컴포넌트를 가져옵니다.
        SlotDrag sourceSlotDrag = eventData.pointerDrag.GetComponent<SlotDrag>();

        // CraftingUI가 있고, 드래그된 슬롯이 유효하다면 CraftingUI에게 알립니다.
        if (craftingUI != null && sourceSlotDrag != null)
        {
            // "CraftingUI야, 내(slotIndex) 위에 sourceSlotDrag가 드롭됐어!"
            craftingUI.OnItemDroppedToCraftingSlot(sourceSlotDrag, slotIndex);
        }
    }
}