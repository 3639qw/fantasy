using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 재료 슬롯에서 우클릭하면 아이템을 인벤토리로 돌려보내고 슬롯을 비운다.
/// IngredientSlotA / IngredientSlotB에 붙여서 사용.
/// </summary>
[RequireComponent(typeof(CraftingSlotDropTarget))]
public class CraftingSlotRightClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs (선택)")]
    [SerializeField] private Inventory inventory; // 비워두면 자동 탐색

    private CraftingSlotDropTarget slot;

    private void Awake()
    {
        slot = GetComponent<CraftingSlotDropTarget>();

        if (!inventory) inventory = Inventory.Instance;
        if (!inventory) inventory = FindObjectOfType<Inventory>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (slot == null || slot.IsEmpty()) return;
        if (!inventory)
        {
            Debug.LogWarning("[Crafting] Inventory 참조 없음");
            return;
        }

        // 현재 슬롯 아이콘 1개를 인벤토리로 되돌림 (필요시 수량 처리 확장)
        var spr = slot.CurrentSprite;
        if (spr != null)
        {
            inventory.AddItem(spr, 1);   // 인벤토리에 1개 지급 (프로젝트 규칙에 맞게 조정)
        }

        slot.Clear(); // 슬롯 비우기
        // CraftingUI는 Update에서 TryCraft()를 돌리므로 자동으로 결과 갱신됨
        Debug.Log("[Crafting] 우클릭으로 재료를 인벤토리로 반환");
    }
}
