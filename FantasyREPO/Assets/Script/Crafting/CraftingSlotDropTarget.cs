using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingSlotDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image icon;          // 이 슬롯의 이미지
    [SerializeField] private Sprite emptySprite;  // 빈 슬롯 스프라이트

    public Sprite CurrentSprite => icon ? icon.sprite : null;

    private void Reset()
    {
        icon = GetComponent<Image>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag?.GetComponent<SlotDrag>();
        if (drag == null) return;

        // 드랍된 아이템 아이콘을 이 슬롯에 표시
        if (icon != null && drag.IconSprite != null)
        {
            icon.sprite = drag.IconSprite;
            icon.color = Color.white;
            Debug.Log($"[CraftingSlot] {gameObject.name} ← {drag.IconSprite.name}");
        }
    }

    public void Clear()
    {
        if (icon != null && emptySprite != null)
        {
            icon.sprite = emptySprite;
            icon.color = new Color(1, 1, 1, 0.3f);
        }
    }

    public bool IsEmpty()
    {
        return icon == null || icon.sprite == null || icon.sprite == emptySprite;
    }
}
