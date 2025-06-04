using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop‑handler for a slot.
///
/// • self 빈칸 → <i>MoveStack</i><br/>
/// • self 아이템 & origin 아이템:
///     ‑ <b>같은 스프라이트</b> ⇒ 더 많은 스택으로 <b>병합</b><br/>
///     ‑ <b>다른 스프라이트</b> ⇒ <b>Swap</b>
/// </summary>
[RequireComponent(typeof(SlotDrag))]
public class SlotDropTarget : MonoBehaviour, IDropHandler
{
    private SlotDrag self;

    void Awake() => self = GetComponent<SlotDrag>();

    public void OnDrop(PointerEventData eventData)
    {
        var origin = eventData.pointerDrag?.GetComponent<SlotDrag>();
        if (origin == null || origin == self) return;   // null or same slot
        if (!origin.HasItem()) return;                  // origin empty → ignore

        // ─── 타깃(self)이 빈칸이면 그대로 이동 ──────────────────────────────
        if (!self.HasItem()) { MoveStack(origin, self); return; }

        // ─── 두 슬롯 모두 아이템 있을 때 ────────────────────────────────────
        bool sameSprite = origin.Slot.icon.sprite == self.Slot.icon.sprite;

        if (sameSprite)
        {
            // 병합 대상 결정 : 스택이 많은 쪽(동일하면 target/self)
            if (self.Slot.count >= origin.Slot.count)
                Merge(origin, self);   // origin → self
            else
                Merge(self, origin);   // self   → origin (마우스가 놓인 곳보다 큰 스택으로 흡수)
        }
        else
        {
            // 다른 아이콘이면 그냥 Swap
            origin.SwapWith(self);
        }
    }

    /* origin 스택 전체를 target 슬롯에 더한다. origin 은 빈칸 처리 */
    void Merge(SlotDrag origin, SlotDrag target)
    {
        target.Slot.count += origin.Slot.count;
        origin.Slot.count = 0;
        origin.Slot.icon.sprite = Inventory.Instance.EmptySprite;

        var inv = Inventory.Instance;
        inv.RefreshSlot(target.Slot);
        inv.RefreshSlot(origin.Slot);
    }

    /* origin 스택을 비어 있는 target 슬롯으로 이동 */
    void MoveStack(SlotDrag origin, SlotDrag target)
    {
        target.Slot.icon.sprite = origin.Slot.icon.sprite;
        target.Slot.count = origin.Slot.count;

        origin.Slot.count = 0;
        origin.Slot.icon.sprite = Inventory.Instance.EmptySprite;

        var inv = Inventory.Instance;
        inv.RefreshSlot(target.Slot);
        inv.RefreshSlot(origin.Slot);
    }
}
