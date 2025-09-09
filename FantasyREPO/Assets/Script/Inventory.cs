using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [System.Serializable]
    public class ItemSlot
    {
        public Image icon;
        public TMP_Text countLabel;
        [HideInInspector] public int count;
    }

    [Header("퀵 슬롯 10 (UI 순서대로 1~0)")]
    public ItemSlot[] quickSlots = new ItemSlot[10];

    [Header("가방 50")]
    public ItemSlot[] bagSlots = new ItemSlot[50];

    [Header("빈 슬롯 플레이스홀더")]
    [SerializeField] private Sprite emptySprite;
    public Sprite EmptySprite => emptySprite;

    [Header("Bag UI 패널")]
    [SerializeField] private GameObject bagPanel;

    [SerializeField, Range(1, 10)] private int current = 1;
    public int states => current;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        AutoAttachDragScripts();
        NormalizeCounts();
        InitSlotVisuals();
    }

    /* ---------- 초기 시각화 ---------- */
    void InitSlotVisuals()
    {
        foreach (var s in quickSlots) RefreshSlot(s);
        foreach (var s in bagSlots) RefreshSlot(s);
        // 첫 선택 상태 반영
        SelectQuick(Mathf.Clamp(current, 1, Mathf.Max(1, quickSlots.Length)));
    }

    void NormalizeCounts()
    {
        FixArray(quickSlots);
        FixArray(bagSlots);
    }

    void FixArray(ItemSlot[] arr)
    {
        if (arr == null) return;
        foreach (var s in arr)
        {
            if (s == null || s.icon == null) continue;
            if (s.count == 0 && s.icon.sprite != null && s.icon.sprite != emptySprite)
                s.count = 1; // 아이콘이 있는데 count가 0이면 1로 보정
        }
    }

    /* ---------- 입력 ---------- */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && bagPanel != null)
            bagPanel.SetActive(!bagPanel.activeSelf);

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
        if (quickSlots == null || quickSlots.Length == 0) return;

        current = Mathf.Clamp(idx, 1, quickSlots.Length);

        for (int i = 0; i < quickSlots.Length; i++)
        {
            var s = quickSlots[i];
            if (s == null || s.icon == null) continue;

            if (s.count > 0)
                SetAlpha(s.icon, i == current - 1);  // 선택된 슬롯만 1f
            else
                s.icon.color = new Color(1f, 1f, 1f, 0f); // 빈 슬롯은 투명
        }
    }

    /* ---------- 아이템 추가/소비 ---------- */
    public void AddItem(Sprite icon, int amount = 1)
    {
        if (icon == null || amount <= 0) return;

        if (TryStackOrFill(quickSlots, icon, amount)) return;
        if (TryStackOrFill(bagSlots, icon, amount)) return;

        Debug.LogWarning("인벤토리(퀵+가방) 모두 가득 찼습니다");
    }

    private bool TryStackOrFill(ItemSlot[] arr, Sprite icon, int amount)
    {
        if (arr == null) return false;

        // 같은 아이콘 스택
        foreach (var s in arr)
        {
            if (s == null || s.icon == null) continue;
            if (s.count > 0 && s.icon.sprite == icon)
            { s.count += amount; RefreshSlot(s); return true; }
        }

        // 빈 슬롯 채우기
        foreach (var s in arr)
        {
            if (s == null || s.icon == null) continue;
            if (s.count == 0)
            {
                s.icon.sprite = icon;
                s.count = amount;
                RefreshSlot(s);
                return true;
            }
        }
        return false;
    }

    public bool ConsumeSelectedItem(int amount = 1)
    {
        if (quickSlots == null || quickSlots.Length == 0) return false;
        var slot = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];
        if (slot == null || slot.count < amount) return false;

        slot.count -= amount;
        RefreshSlot(slot);
        return true;
    }

    /* ---------- 시각 갱신 ---------- */
    public void RefreshSlot(ItemSlot s)
    {
        if (s == null || s.icon == null) return;

        if (s.countLabel != null)
            s.countLabel.text = s.count > 1 ? $"×{s.count}" : string.Empty;

        if (s.count <= 0)
        {
            // 빈 슬롯 처리
            if (emptySprite != null) s.icon.sprite = emptySprite;
            s.icon.color = new Color(1f, 1f, 1f, 0f);
            s.count = 0; // 음수 방지
        }
        else
        {
            // 정상 슬롯 표시
            if (s.icon.sprite == null && emptySprite != null)
                s.icon.sprite = emptySprite;
            s.icon.color = Color.white;
        }
    }

    private void SetAlpha(Image img, bool selected)
    {
        if (img == null) return;
        var c = img.color;
        c.a = selected ? 1f : 0.6f;
        img.color = c;
    }

    /* ---------- 드래그&드롭 보조 ---------- */
    private void AutoAttachDragScripts()
    {
        Attach(quickSlots);
        Attach(bagSlots);
    }

    private void Attach(ItemSlot[] arr)
    {
        if (arr == null) return;
        foreach (var s in arr)
        {
            if (s == null || s.icon == null) continue;
            GameObject go = s.icon.gameObject;
            if (!go.TryGetComponent(out CanvasGroup cg)) cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            if (!go.GetComponent<SlotDrag>()) go.AddComponent<SlotDrag>();
            if (!go.GetComponent<SlotDropTarget>()) go.AddComponent<SlotDropTarget>();
        }
    }

    /* ---------- 유틸 ---------- */
    public Sprite GetSelectedSprite()
    {
        if (quickSlots == null || quickSlots.Length == 0) return null;
        var s = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];
        return (s != null && s.icon != null) ? s.icon.sprite : null;
    }

    public bool IsSelectedEmpty()
    {
        if (quickSlots == null || quickSlots.Length == 0) return true;
        var s = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];
        return (s == null) || s.count == 0;
    }

    public bool HasItem(Sprite icon, int need)
    {
        if (need <= 0 || icon == null) return true;
        int sum = 0;

        if (quickSlots != null)
            foreach (var s in quickSlots)
                if (s != null && s.icon != null && s.icon.sprite == icon) sum += s.count;

        if (bagSlots != null)
            foreach (var s in bagSlots)
                if (s != null && s.icon != null && s.icon.sprite == icon) sum += s.count;

        return sum >= need;
    }

    public void RemoveItem(Sprite icon, int count)
    {
        if (icon == null || count <= 0) return;

        void CountDown(ItemSlot[] arr)
        {
            if (arr == null) return;
            foreach (var s in arr)
            {
                if (count == 0) return;
                if (s == null || s.icon == null || s.icon.sprite != icon) continue;

                int take = Mathf.Min(s.count, count);
                s.count -= take;
                count -= take;
                RefreshSlot(s);
            }
        }

        CountDown(quickSlots);
        CountDown(bagSlots);
    }

    public ItemSlot FindSlotByIcon(Image icon)
    {
        if (icon == null) return null;
        if (quickSlots != null) foreach (var q in quickSlots) if (q != null && q.icon == icon) return q;
        if (bagSlots != null) foreach (var b in bagSlots) if (b != null && b.icon == icon) return b;
        return null;
    }
}
