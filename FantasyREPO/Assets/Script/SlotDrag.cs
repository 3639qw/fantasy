using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image icon;
    private CanvasGroup cg;
    private Vector3 startPos;

    private Inventory.ItemSlot mySlot;
    public Inventory.ItemSlot Slot => mySlot;

    public void Initialize(Inventory.ItemSlot slot)
    {
        mySlot = slot;
    }

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        icon = GetComponent<Image>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!HasItem()) return;

        startPos = transform.position;
        cg.blocksRaycasts = false;
        
        // <<-- 변경: Show 메서드에 Sprite가 아닌 Image 컴포넌트(icon)를 전달합니다.
        DragGhost.Instance.Show(icon);
    }
    
    public void OnDrag(PointerEventData e) { /* DragGhost가 커서 추적 */ }

    public void OnEndDrag(PointerEventData e)
    {
        cg.blocksRaycasts = true;
        DragGhost.Instance.Hide();
    }
    
    public void OnDrop(PointerEventData e) { } 

    public bool HasItem() => mySlot != null && mySlot.itemData != null;

    public void SwapWith(SlotDrag other)
    {
        if (mySlot == null || other.mySlot == null) return;

        ItemData tmpData = mySlot.itemData;
        int tmpCount = mySlot.count;

        mySlot.itemData = other.mySlot.itemData;
        mySlot.count = other.mySlot.count;

        other.mySlot.itemData = tmpData;
        other.mySlot.count = tmpCount;

        var inv = Inventory.Instance;
        inv.RefreshSlot(mySlot);
        inv.RefreshSlot(other.mySlot);
    }
}