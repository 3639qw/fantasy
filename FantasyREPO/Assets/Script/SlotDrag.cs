using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 아이콘(슬롯) 하나에 붙어서 드래그-앤-드롭을 처리.
/// • 빈칸은 끌어낼 수 없음
/// • DragGhost로 유령 아이콘 표시
/// • SlotDropTarget 의 OnDrop 에서 다시 호출돼 스왑/이동 수행
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SlotDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private Image icon;          // 내 아이콘 Image
    private CanvasGroup cg;
    private Vector3 startPos;
    private Sprite empty;         // Inventory.EmptySprite

    /* ===== 프로퍼티 – 외부(SlotDropTarget)가 접근 ===== */
    private Inventory.ItemSlot mySlot;
    public Inventory.ItemSlot Slot => mySlot;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        icon = GetComponent<Image>();

        empty = Inventory.Instance.EmptySprite;
        mySlot = Inventory.Instance.FindSlotByIcon(icon);
        if (mySlot == null) { Debug.LogError($"ItemSlot 매칭 실패: {name}"); enabled = false; }
    }

    /* ───── Drag 시작 ───── */
    public void OnBeginDrag(PointerEventData e)
    {
        Debug.Log($"BeginDrag  slot={name}");
        if (!HasItem()) return;

        startPos = transform.position;
        cg.blocksRaycasts = false;
        DragGhost.Instance.Show(icon);
    }
    public void OnDrag(PointerEventData e) { /* DragGhost가 커서 추적 */ }
    public void OnEndDrag(PointerEventData e)
    {
        cg.blocksRaycasts = true;
        DragGhost.Instance.Hide();
        transform.position = startPos;
    }

    /* ───── Drop 처리(슬롯→슬롯) ───── */
    public void OnDrop(PointerEventData e)
    {
        var target = e.pointerDrag?.GetComponent<SlotDrag>(); // 내 쪽에서 이벤트 재호출 안 함
    }

    /* ───── 헬퍼 ───── */
    public bool HasItem() => icon.sprite != empty;

    public void SwapWith(SlotDrag other)
    {
        // 아이콘 + 수량 전부 교환
        Sprite tmpSpr = icon.sprite;
        int tmpCount = mySlot.count;

        icon.sprite = other.icon.sprite;
        mySlot.count = other.mySlot.count;

        other.icon.sprite = tmpSpr;
        other.mySlot.count = tmpCount;

        var inv = Inventory.Instance;
        inv.RefreshSlot(mySlot);
        inv.RefreshSlot(other.mySlot);
    }
}
