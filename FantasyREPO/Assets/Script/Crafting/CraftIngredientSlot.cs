// Assets/Scripts/Crafting/CraftIngredientSlot.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftIngredientSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image icon;             // 이 슬롯의 이미지
    [SerializeField] private Sprite emptySprite;     // 빈칸 스프라이트 (Inventory.EmptySprite와 동일 권장)

    public Sprite CurrentIcon => icon ? icon.sprite : null;
    public bool IsEmpty => !icon || icon.sprite == null || icon.sprite == emptySprite;

    private void Reset()
    {
        icon = GetComponent<Image>();
    }

    public void Clear()
    {
        if (!icon) return;
        icon.sprite = emptySprite;
        var c = icon.color; c.a = 0.6f; icon.color = c;
    }

    public void SetIcon(Sprite spr)
    {
        if (!icon) return;
        icon.sprite = spr;
        icon.color = Color.white;
    }

    public void OnDrop(PointerEventData e)
    {
        // 인벤토리 쪽에서 끌어온 것만 받기
        var origin = e.pointerDrag ? e.pointerDrag.GetComponent<SlotDrag>() : null;
        if (origin == null) return;
        if (!origin.HasItem()) return;

        // 인벤토리에서는 아직 차감하지 않음(제작 확정될 때 제거)
        SetIcon(origin.GetComponent<Image>().sprite);
    }
}
