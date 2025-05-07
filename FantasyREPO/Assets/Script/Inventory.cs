using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리: 5 슬롯(Quick Bar 포함) + 스택 개수(TMP) + 숫자 1‑5 선택 + Bag (E) 토글 + 아이템 소비
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
        public Image icon;       // 아이콘 이미지 UI
        public TextMeshProUGUI countLabel;// 수량 표시 TMP
        [HideInInspector] public int count = 0;
    }

    [Header("슬롯 1~5 (순서대로)")]
    public ItemSlot[] slots = new ItemSlot[5];

    [Header("빈 슬롯 플레이스홀더")]
    [SerializeField] private Sprite emptySprite;

    [Header("Bag UI 패널")]
    [SerializeField] private GameObject bagPanel;

    /* 현재 선택 슬롯 번호 (1~5) */
    protected internal int states = 1;

    /* ────────── 초기화 ────────── */
    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    /* ────────── 입력 처리 ────────── */
    private void Update()
    {
        // Bag 열기/닫기
        if (Input.GetKeyDown(KeyCode.E) && bagPanel != null)
            bagPanel.SetActive(!bagPanel.activeSelf);

        // 숫자 키 선택
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(5);
    }

    private void SelectSlot(int idx)
    {
        states = idx;
        for (int i = 0; i < slots.Length; i++)
            SetAlpha(slots[i].icon, i == idx - 1);
    }

    /* ────────── 아이템 추가 / 소비 ────────── */
    public void AddItem(Sprite icon, int amount)
    {
        // 1) 동일 아이콘 스택 찾기
        foreach (var s in slots)
        {
            if (s.icon.sprite == icon)
            {
                s.count += amount;
                UpdateSlotVisual(s);
                return;
            }
        }
        // 2) 빈 칸 찾기
        foreach (var s in slots)
        {
            if (s.icon.sprite == null || s.icon.sprite == emptySprite)
            {
                s.icon.sprite = icon;
                s.count = amount;
                s.icon.color = Color.white;
                UpdateSlotVisual(s);
                return;
            }
        }
        Debug.LogWarning("인벤토리가 가득 찼습니다");
    }

    /// <summary>현재 선택 슬롯에서 amount 만큼 소비. 성공:true/실패:false</summary>
    public bool ConsumeSelectedItem(int amount)
    {
        var slot = slots[states - 1];
        if (slot.count < amount) return false;

        slot.count -= amount;
        if (slot.count == 0)
        {
            slot.icon.sprite = emptySprite;
            slot.icon.color = new Color(1, 1, 1, 0.3f);
        }
        UpdateSlotVisual(slot);
        return true;
    }

    /* ────────── 헬퍼 ────────── */
    private void UpdateSlotVisual(ItemSlot s)
    {
        s.countLabel.text = s.count > 1 ? $"×{s.count}" : "";
    }
    private void SetAlpha(Image img, bool selected)
    {
        if (img == null) return;
        var c = img.color;
        c.a = selected ? 1f : 60f / 255f;
        img.color = c;
    }

    public Sprite GetSelectedSprite() => slots[states - 1].icon.sprite;
    public bool IsSelectedEmpty() => GetSelectedSprite() == null || GetSelectedSprite() == emptySprite;
}
