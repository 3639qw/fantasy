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
    public static Inventory Instance { get; private set; }

    /* ────────── 슬롯 구조 ────────── */
    [System.Serializable]
    public class ItemSlot
    {
        public Image icon;
        public TMP_Text countLabel;
        [HideInInspector] public int count;
    }

    /* ────────── Quick (1~10) & Bag (50) ────────── */
    [Header("퀵 슬롯 10 (UI 순서대로 1~0)")]
    public ItemSlot[] quickSlots = new ItemSlot[10];

    [Header("가방 50")]
    public ItemSlot[] bagSlots = new ItemSlot[50];

    [Header("빈 슬롯 플레이스홀더")]
    [SerializeField] private Sprite emptySprite;
    public Sprite EmptySprite => emptySprite;

    [Header("Bag UI 패널")]
    [SerializeField] private GameObject bagPanel;

    /* 현재 선택 퀵슬롯 번호 (1~10) */
    [SerializeField, Range(1, 10)] private int current = 1;
    public int states => current;

    /* ────────── 초기화 ────────── */
    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        AutoAttachDragScripts();   // 드래그/드롭 스크립트 부착
        NormalizeCounts();         // 슬롯 카운트 정리
        InitSlotVisuals();         // 슬롯 시각화 초기화
    }

    void InitSlotVisuals()
    {
        foreach (var s in quickSlots) RefreshSlot(s);
        foreach (var s in bagSlots) RefreshSlot(s);
    }

    void NormalizeCounts()
    {
        FixArray(quickSlots);
        FixArray(bagSlots);
    }

    void FixArray(ItemSlot[] arr)
    {
        foreach (var s in arr)
            if (s.count == 0 && s.icon && s.icon.sprite && s.icon.sprite != emptySprite)
                s.count = 1;
    }

    /* ────────── 입력 처리 ────────── */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && bagPanel)
            bagPanel.SetActive(!bagPanel.activeSelf);

        /* 1~9, 0 → 퀵슬롯 1~10 */
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuick(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuick(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuick(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuick(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuick(5);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) SelectQuick(6);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) SelectQuick(7);
        else if (Input.GetKeyDown(KeyCode.Alpha8)) SelectQuick(8);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) SelectQuick(9);
        else if (Input.GetKeyDown(KeyCode.Alpha0)) SelectQuick(10);
    }

    private void SelectQuick(int idx)
    {
        current = Mathf.Clamp(idx, 1, quickSlots.Length);
        for (int i = 0; i < quickSlots.Length; i++)
        {
            ItemSlot s = quickSlots[i];
            if (s.count > 0)
                SetAlpha(s.icon, i == current - 1);   // 선택된 슬롯은 1f, 나머지는 0.6f
            else
                s.icon.color = new Color(1, 1, 1, 0); // 비어있으면 투명
        }
    }

    /* ────────── 아이템 추가 / 소비 ────────── */
    public void AddItem(Sprite icon, int amount = 1)
    {
        if (TryStackOrFill(quickSlots, icon, amount)) return;
        if (TryStackOrFill(bagSlots, icon, amount)) return;
        Debug.LogWarning("인벤토리(퀵+가방) 모두 가득 찼습니다");
    }

    private bool TryStackOrFill(ItemSlot[] arr, Sprite icon, int amount)
    {
        // 같은 아이콘 스택
        foreach (var s in arr)
            if (s.count > 0 && s.icon.sprite == icon)
            { s.count += amount; RefreshSlot(s); return true; }

        // 빈 슬롯 찾기
        foreach (var s in arr)
            if (s.count == 0)
            { s.icon.sprite = icon; s.count = amount; RefreshSlot(s); return true; }

        return false;
    }

    public bool ConsumeSelectedItem(int amount = 1)
    {
        ItemSlot slot = quickSlots[current - 1];
        if (slot.count < amount) return false;
        slot.count -= amount;
        RefreshSlot(slot);
        return true;
    }

    /* ────────── 시각 갱신 ────────── */
    public void RefreshSlot(ItemSlot s)
    {
        if (s.countLabel != null)
        s.countLabel.text = s.count > 1 ? $"×{s.count}" : "";
        if (s.count == 0)
        {
            s.icon.sprite = emptySprite;
            s.icon.color = new Color(1, 1, 1, 0);
        }
        else s.icon.color = Color.white;
    }

    private void SetAlpha(Image img, bool selected)
    {
        if (!img) return;
        Color c = img.color;
        c.a = selected ? 1f : 0.6f;
        img.color = c;
    }

    /* ────────── 드래그&드롭 자동 부착 ────────── */
    private void AutoAttachDragScripts()
    {
        Attach(quickSlots);
        Attach(bagSlots);
    }

    private void Attach(ItemSlot[] arr)
    {
        foreach (var s in arr)
        {
            if (!s.icon) continue;
            GameObject go = s.icon.gameObject;
            if (!go.TryGetComponent(out CanvasGroup cg)) cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            if (!go.GetComponent<SlotDrag>()) go.AddComponent<SlotDrag>();
            if (!go.GetComponent<SlotDropTarget>()) go.AddComponent<SlotDropTarget>();
        }
    }

    /* ────────── 유틸 ────────── */
    public Sprite GetSelectedSprite() => quickSlots[current - 1].icon.sprite;
    public bool IsSelectedEmpty() => quickSlots[current - 1].count == 0;

    /// <summary>특정 아이콘이 need 개 이상 있는가?</summary>
    public bool HasItem(Sprite icon, int need)
    {
        int sum = 0;
        foreach (var s in quickSlots) if (s.icon.sprite == icon) sum += s.count;
        foreach (var s in bagSlots) if (s.icon.sprite == icon) sum += s.count;
        return sum >= need;
    }

    /// <summary>아이콘을 amount 만큼 제거. 부족하면 false</summary>
    public void RemoveItem(Sprite icon, int count)
    {
        CountDown(quickSlots);
        CountDown(bagSlots);

        void CountDown(ItemSlot[] arr)
        {
            foreach (var s in arr)
            {
                if (s.icon.sprite != icon) continue;
                int take = Mathf.Min(s.count, count);
                s.count -= take;
                count -= take;
                RefreshSlot(s);
                if (count == 0) return;
            }
        }
    }

    /// <summary>icon으로 ItemSlot 찾기</summary>
    public ItemSlot FindSlotByIcon(Image icon)
    {
        foreach (var q in quickSlots) if (q.icon == icon) return q;
        foreach (var b in bagSlots) if (b.icon == icon) return b;
        return null;
    }
}
