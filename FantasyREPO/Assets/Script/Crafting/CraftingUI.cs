using UnityEngine;
using UnityEngine.UI;

public class CraftingUI : MonoBehaviour
{
    [Header("Recipe (참조 비교)")]
    [SerializeField] private Sprite woodSprite;
    [SerializeField] private Sprite stoneSprite;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;   // 제작창 루트
    [SerializeField] private Button craftBtn;        // 제작 버튼

    [Header("Slots")]
    [SerializeField] private CraftingSlotDropTarget ingredientSlotA;
    [SerializeField] private CraftingSlotDropTarget ingredientSlotB;
    [SerializeField] private Image resultSlot;
    [SerializeField] private Sprite emptySprite;

    [Header("Product")]
    [SerializeField] private Sprite productIcon;     // 결과 (예: 곡괭이)
    [SerializeField] private int productGive = 1;

    [Header("Refs (선택)")]
    [SerializeField] private Inventory inventory;

    private void Awake()
    {
        if (!inventory) inventory = Inventory.Instance;
        if (!inventory) inventory = FindObjectOfType<Inventory>();

        if (panelRoot) panelRoot.SetActive(false);
        if (craftBtn) craftBtn.onClick.AddListener(Craft);

        if (resultSlot) SetResultEmpty();
    }

    private void OnDestroy()
    {
        if (craftBtn) craftBtn.onClick.RemoveListener(Craft);
    }

    private void Update()
    {
        if (panelRoot && panelRoot.activeSelf)
            TryCraft();

        if (craftBtn) craftBtn.interactable = CanCraft();

        if (panelRoot && panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();
    }

    private bool CanCraft()
    {
        if (!inventory) return false;
        if (!resultSlot || resultSlot.sprite == null) return false;
        if (resultSlot.sprite == emptySprite) return false;
        return true;
    }

    private void TryCraft()
    {
        if (!ingredientSlotA || !ingredientSlotB || !resultSlot) return;

        if (ingredientSlotA.IsEmpty() || ingredientSlotB.IsEmpty())
        {
            SetResultEmpty();
            return;
        }

        var iconA = ingredientSlotA.CurrentSprite;
        var iconB = ingredientSlotB.CurrentSprite;

        bool match =
            (iconA == woodSprite && iconB == stoneSprite) ||
            (iconA == stoneSprite && iconB == woodSprite);

        if (match && productIcon)
        {
            resultSlot.sprite = productIcon;
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

        // 인벤토리에 결과 지급
        inventory.AddItem(productIcon, productGive);

        Debug.Log("[Crafting] 제작 완료!");

        // 슬롯 초기화
        ingredientSlotA.Clear();
        ingredientSlotB.Clear();
        SetResultEmpty();
    }

    private void SetResultEmpty()
    {
        if (!resultSlot) return;
        resultSlot.sprite = emptySprite;
        var c = resultSlot.color;
        c.a = 0.6f;
        resultSlot.color = c;
    }

    public void TogglePanel()
    {
        if (!panelRoot) return;
        panelRoot.SetActive(!panelRoot.activeSelf);
        Debug.Log($"[CraftingUI] Panel {(panelRoot.activeSelf ? "OPEN" : "CLOSE")}");
    }

    public void OpenPanel()
    {
        if (panelRoot && !panelRoot.activeSelf) TogglePanel();
    }

    public void ClosePanel()
    {
        if (panelRoot && panelRoot.activeSelf) TogglePanel();
    }
}
