using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 시스템
///  • 퀵슬롯 5 + 가방 슬롯 50 (총 55) 스택 & 수량 표시
///  • 퀵슬롯이 가득 차면 가방으로 자동 이동
///  • E : Bag 패널 토글, 숫자 1‑5 : 퀵슬롯 선택
///  • AddItem(icon, amount) → 먼저 퀵슬롯, 없으면 가방 슬롯
///  • ConsumeSelectedItem() : 퀵슬롯 아이템 사용 및 0이면 아이콘 제거
/// </summary>
public class Inventory : MonoBehaviour
{
    /* ────────── 싱글톤 ────────── */
    public static Inventory instance;
    public static Inventory Instance => instance;

    /* ────────── 슬롯 구조 ────────── */
    [System.Serializable]
    public class ItemSlot
    {
        public Image icon;
        [HideInInspector] public int count = 0;
    }

    /* ────────── Quick (1~5) & Bag (50) ────────── */
    [Header("퀵슬롯 1~5 (UI 순서대로)")]
    public ItemSlot[] quickSlots = new ItemSlot[5];

    [Header("가방 슬롯 50 (UI 순서대로)")]
    public ItemSlot[] bagSlots = new ItemSlot[50];

    private ItemSlot[] AllSlots => CombineArrays(quickSlots, bagSlots);

    [Header("빈 슬롯 플레이스홀더")][SerializeField] private Sprite emptySprite;

    [Header("Bag UI 패널")][SerializeField] private GameObject bagPanel;

    /* 현재 선택 퀵슬롯 번호 (1~5) */
    protected internal int states = 1;

    /* ────────── 초기화 ────────── */
    private void Awake()
    {
        if (instance == null) instance = this; else { Destroy(gameObject); return; }
    }

    /* ────────── 입력 처리 ────────── */
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && bagPanel != null)
            bagPanel.SetActive(!bagPanel.activeSelf);

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuick(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuick(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuick(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuick(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuick(5);
    }

    private void SelectQuick(int idx)
    {
        states = idx;
        // 알파 갱신
        for (int i = 0; i < quickSlots.Length; i++)
            SetAlpha(quickSlots[i].icon, i == idx - 1);
    }

    /* ────────── 아이템 추가 / 소비 ────────── */
    public void AddItem(Sprite icon, int amount)
    {
        if (TryStackOrFill(quickSlots, icon, amount)) return;   // 퀵슬롯 우선
        if (TryStackOrFill(bagSlots, icon, amount)) return;   // 가방 슬롯
        Debug.LogWarning("인벤토리(퀵+가방) 모두 가득 찼습니다");
    }

    private bool TryStackOrFill(ItemSlot[] target, Sprite icon, int amount)
    {
        // 1) 스택 찾기
        foreach (var s in target)
        {
            if (s.icon.sprite == icon)
            {
                s.count += amount; UpdateSlotVisual(s); return true;
            }
        }
        // 2) 빈 칸 찾기
        foreach (var s in target)
        {
            if (s.icon.sprite == null || s.icon.sprite == emptySprite)
            {
                s.icon.sprite = icon; s.count = amount; s.icon.color = Color.white; UpdateSlotVisual(s); return true;
            }
        }
        return false;
    }

    /// <summary>현재 선택 퀵슬롯에서 amount 소비, 성공:true</summary>
    public bool ConsumeSelectedItem(int amount)
    {
        var slot = quickSlots[states - 1];
        if (slot.count < amount) return false;
        slot.count -= amount;
        if (slot.count == 0)
        {
            slot.icon.sprite = emptySprite; slot.icon.color = new Color(1, 1, 1, 0.3f);
        }
        UpdateSlotVisual(slot); return true;
    }

    /* ────────── 시각 갱신 ────────── */
    private void UpdateSlotVisual(ItemSlot s)
    {
        s.icon.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = s.count > 1 ? $"×{s.count}" : "";
    }
    private void SetAlpha(Image img, bool selected)
    {
        if (img == null) return; var c = img.color; c.a = selected ? 1f : 60f / 255f; img.color = c;
    }

    /* ────────── 유틸 ────────── */
    public Sprite GetSelectedSprite() => quickSlots[states - 1].icon.sprite;
    public bool IsSelectedEmpty()
        => GetSelectedSprite() == null || GetSelectedSprite() == emptySprite;

    private ItemSlot[] CombineArrays(ItemSlot[] a, ItemSlot[] b)
    {
        ItemSlot[] res = new ItemSlot[a.Length + b.Length];
        a.CopyTo(res, 0); b.CopyTo(res, a.Length); return res;
    }
}
