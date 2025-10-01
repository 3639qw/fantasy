using UnityEngine;
using UnityEngine.UI;
using System.Linq; // Linq 사용을 위해 추가

public class CraftingUI : MonoBehaviour
{
    [Header("Recipe")]
    [SerializeField] private ItemData woodItemData;
    [SerializeField] private ItemData stoneItemData;

    [Header("Product")]
    [SerializeField] private ItemData productItemData;
    [SerializeField] private int productGive = 1;

    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button craftBtn;
    [SerializeField] private Image resultSlot;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Inventory.ItemSlot[] ingredientUISlots; // 재료 슬롯의 UI 컴포넌트들

    // 내부적으로 재료 아이템 데이터를 저장할 배열
    private Inventory.ItemSlot[] ingredientDataSlots = new Inventory.ItemSlot[2];

    private void Awake()
    {
        // 데이터 슬롯 초기화
        for (int i = 0; i < ingredientDataSlots.Length; i++)
        {
            ingredientDataSlots[i] = new Inventory.ItemSlot();
        }

        if (panelRoot) panelRoot.SetActive(false);
        if (craftBtn) craftBtn.onClick.AddListener(Craft);
        if (resultSlot) SetResultEmpty();
    }

    private void Update()
    {
        if (panelRoot && panelRoot.activeSelf)
        {
            TryUpdateRecipe(); // 레시피가 맞는지 계속 확인
            if (Input.GetKeyDown(KeyCode.Escape)) ClosePanel();
        }
        if (craftBtn) craftBtn.interactable = CanCraft();
    }

    // 다른 인벤토리 슬롯의 아이템이 제작 슬롯으로 드롭되었을 때 호출되는 메서드
    public void OnItemDroppedToCraftingSlot(SlotDrag sourceSlotDrag, int targetCraftingSlotIndex)
    {
        // 드롭된 아이템 데이터를 가져옴
        Inventory.ItemSlot sourceDataSlot = sourceSlotDrag.Slot;

        // 제작 슬롯과 인벤토리 슬롯의 데이터를 교환(Swap)
        var targetDataSlot = ingredientDataSlots[targetCraftingSlotIndex];

        // 데이터 교환
        ItemData tempData = targetDataSlot.itemData;
        int tempCount = targetDataSlot.count;

        targetDataSlot.itemData = sourceDataSlot.itemData;
        targetDataSlot.count = sourceDataSlot.count;

        sourceDataSlot.itemData = tempData;
        sourceDataSlot.count = tempCount;

        // 양쪽 슬롯 UI 갱신
        RefreshCraftingSlotUI(targetCraftingSlotIndex);
        Inventory.Instance.RefreshSlot(sourceDataSlot);
    }
    
    // 레시피가 맞는지 확인하고 결과물 UI를 업데이트하는 메서드
    private void TryUpdateRecipe()
    {
        var itemA = ingredientDataSlots[0].itemData;
        var itemB = ingredientDataSlots[1].itemData;

        bool match = (itemA != null && itemB != null) &&
            ((itemA == woodItemData && itemB == stoneItemData) ||
             (itemA == stoneItemData && itemB == woodItemData));

        if (match && productItemData != null)
        {
            resultSlot.sprite = productItemData.itemIcon;
            resultSlot.color = Color.white;
        }
        else
        {
            SetResultEmpty();
        }
    }
    
    private void Craft()
    {
        if (!CanCraft()) return;

        Inventory.Instance.AddItem(productItemData, productGive);
        Debug.Log($"[Crafting] {productItemData.itemName} 제작 완료!");

        // 재료 소모
        ingredientDataSlots[0].itemData = null;
        ingredientDataSlots[0].count = 0;
        ingredientDataSlots[1].itemData = null;
        ingredientDataSlots[1].count = 0;

        // UI 갱신
        RefreshCraftingSlotUI(0);
        RefreshCraftingSlotUI(1);
        SetResultEmpty();
    }

    // 특정 재료 슬롯의 UI를 데이터에 맞게 갱신
    private void RefreshCraftingSlotUI(int index)
    {
        var dataSlot = ingredientDataSlots[index];
        var uiSlot = ingredientUISlots[index];

        if (dataSlot.itemData != null)
        {
            uiSlot.icon.sprite = dataSlot.itemData.itemIcon;
            uiSlot.icon.color = Color.white;
            uiSlot.countLabel.text = dataSlot.count > 1 ? $"x{dataSlot.count}" : "";
        }
        else
        {
            uiSlot.icon.sprite = emptySprite;
            uiSlot.icon.color = new Color(1, 1, 1, 0.5f);
            uiSlot.countLabel.text = "";
        }
    }

    private bool CanCraft() => resultSlot.sprite != null && resultSlot.sprite != emptySprite;
    private void SetResultEmpty() { if (!resultSlot) return; resultSlot.sprite = emptySprite; resultSlot.color = new Color(1, 1, 1, 0.6f); }
    public void TogglePanel() { if (!panelRoot) return; panelRoot.SetActive(!panelRoot.activeSelf); }
    public void OpenPanel() { if (panelRoot && !panelRoot.activeSelf) TogglePanel(); }
    public void ClosePanel() { if (panelRoot && panelRoot.activeSelf) TogglePanel(); }
}