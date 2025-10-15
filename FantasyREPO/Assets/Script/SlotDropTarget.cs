using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SlotDrag))]
public class SlotDropTarget : MonoBehaviour, IDropHandler
{
    private SlotDrag targetDrag;

    void Awake()
    {
        targetDrag = GetComponent<SlotDrag>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var sourceGO = eventData.pointerDrag;
        if (!sourceGO) return;

        var sourceDrag = sourceGO.GetComponent<SlotDrag>();
        if (sourceDrag == null || targetDrag == null) return;

        var inv = Inventory.Instance;
        if (inv == null) return;

        Inventory.ItemSlot src = sourceDrag.Slot;
        Inventory.ItemSlot dst = targetDrag.Slot;

        if (src == null || dst == null) return;
        if (src == dst) return;

        // 소스에 아이템 없으면 무시
        if (src.itemData == null || src.count <= 0) return;

        // 1) 타깃이 빈 칸 → 이동
        if (dst.itemData == null || dst.count <= 0)
        {
            Move(src, dst, inv);
            return;
        }

        // 2) 같은 아이템 → 병합
        if (src.itemData == dst.itemData)
        {
            Merge(src, dst, inv);
            return;
        }

        // 3) 다른 아이템 → 스왑
        sourceDrag.SwapWith(targetDrag);
    }

    private static void Move(Inventory.ItemSlot src, Inventory.ItemSlot dst, Inventory inv)
    {
        dst.itemData = src.itemData;
        dst.count = src.count;

        src.itemData = null;
        src.count = 0;

        inv.RefreshSlot(dst);
        inv.RefreshSlot(src);
    }

    private static void Merge(Inventory.ItemSlot src, Inventory.ItemSlot dst, Inventory inv)
    {
        dst.count += src.count;

        src.itemData = null;
        src.count = 0;

        inv.RefreshSlot(dst);
        inv.RefreshSlot(src);
    }
}
