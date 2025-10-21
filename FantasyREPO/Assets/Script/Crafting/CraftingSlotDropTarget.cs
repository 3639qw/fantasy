// Assets/Scripts/Crafting/CraftingSlotDropTarget.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CraftingSlotDropTarget : MonoBehaviour, IDropHandler
{
    [Tooltip("이 슬롯이 몇 번째 재료 슬롯인지 설정 (0부터 시작)")]
    public int slotIndex;

    [Header("UI")]
    [SerializeField] private Image icon;                 // 재료 아이콘
    [SerializeField] private TMP_Text countLabel;        // 수량 라벨(옵션)
    [SerializeField] private Sprite emptySprite;         // 비었을 때 아이콘
    [Range(0f, 1f)][SerializeField] private float emptyAlpha = 0.6f;

    // 내부 상태
    private int count = 0;
    private string currentKey = null;

    // 외부에서 읽는 API (다른 스크립트가 기대)
    public Sprite CurrentSprite => icon ? icon.sprite : null;
    public int CurrentCount => count;

    // CraftingUI 참조(기존 동작 유지)
    private CraftingUI craftingUI;

    private void Awake()
    {
        craftingUI = GetComponentInParent<CraftingUI>();
        // 시작 상태를 비어있게 정리
        if (icon && icon.sprite == null) icon.sprite = emptySprite;
        ApplyVisual();
    }

    /* =======================
     *  IDropHandler (기존 동작 유지)
     * ======================= */
    public void OnDrop(PointerEventData eventData)
    {
        // 드래그 출처
        var sourceSlotDrag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<SlotDrag>() : null;

        // UI/로직은 CraftingUI가 결정 (기존 설계 유지)
        if (craftingUI != null && sourceSlotDrag != null)
        {
            craftingUI.OnItemDroppedToCraftingSlot(sourceSlotDrag, slotIndex);
        }
    }

    /* =======================
     *  외부에서 요구하는 슬롯 API
     * ======================= */

    /// <summary>이 슬롯이 비었는가?</summary>
    public bool IsEmpty()
    {
        // count가 0이거나 아이콘이 empty인 경우 비었다고 간주
        return count <= 0 || (icon && icon.sprite == emptySprite);
    }

    /// <summary>amount만큼 소비. 0 이하가 되면 Clear()</summary>
    public void Consume(int amount)
    {
        if (amount <= 0 || IsEmpty()) return;
        count -= amount;
        if (count <= 0)
        {
            Clear();
        }
        else
        {
            ApplyVisual();
        }
    }

    /// <summary>슬롯을 완전히 비움</summary>
    public void Clear()
    {
        count = 0;
        currentKey = null;
        if (icon) icon.sprite = emptySprite;
        ApplyVisual();
    }

    /* =======================
     *  CraftingUI에서 채워 넣기용 헬퍼
     *  (OnItemDroppedToCraftingSlot 이후에 CraftingUI가 호출)
     * ======================= */

    /// <summary>
    /// 이 슬롯에 아이템/수량을 세팅한다.
    /// key는 선택(레시피 매칭용). amount &lt;= 0이면 비움.
    /// </summary>
    public void Set(Sprite sprite, int amount, string key = null)
    {
        if (sprite == null || amount <= 0)
        {
            Clear();
            return;
        }

        if (icon) icon.sprite = sprite;
        count = amount;
        currentKey = key;
        ApplyVisual();
    }

    /// <summary>현재 보이는 스프라이트의 (선택) 키를 얻고 싶을 때</summary>
    public string GetItemKey() => currentKey;

    /* =======================
     *  내부: UI 갱신
     * ======================= */
    private void ApplyVisual()
    {
        if (icon)
        {
            bool empty = (icon.sprite == null) || (icon.sprite == emptySprite) || count <= 0;
            var c = icon.color;
            c.a = empty ? emptyAlpha : 1f;
            icon.color = c;
        }

        if (countLabel)
        {
            countLabel.text = (count > 1) ? count.ToString() : (count == 1 ? "1" : "");
        }
    }
}
