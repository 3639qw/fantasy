using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    /* ──────────── 싱글톤 ──────────── */
    public static Inventory instance = null;
    public static Inventory Instance => instance;

    /* ──────────── 슬롯 & 설정 ──────────── */
    [Header("인벤토리 아이템(슬롯 1~5)")]
    public Image hand1;   // 1번 슬롯
    public Image hand2;   // 2번 슬롯
    public Image hand3;   // 3번 슬롯
    public Image hand4;   // 4번 슬롯
    public Image hand5;   // 5번 슬롯

    [Header("빈 슬롯 플레이스홀더")]
    [SerializeField] private Sprite emptySprite;   // Source Image = 빈 칸 배경

    /* 현재 선택된 슬롯 번호 (1~5) */
    protected internal int states = 1;

    /* ──────────── 초기화 ──────────── */
    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    /* ──────────── 입력 처리 ──────────── */
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { states = 1; SetHandAlpha(1); }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) { states = 2; SetHandAlpha(2); }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) { states = 3; SetHandAlpha(3); }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) { states = 4; SetHandAlpha(4); }
        else if (Input.GetKeyDown(KeyCode.Alpha5)) { states = 5; SetHandAlpha(5); }
    }

    /* ──────────── 아이템 자동 배치 ──────────── */
    /// <summary>
    /// hand1 → hand5 순으로 sprite == null 또는 emptySprite 인 첫 슬롯에 아이콘을 배치
    /// 성공:true, 인벤토리 가득:false
    /// </summary>
    public bool AddItemToFirstEmptySlot(Sprite icon)
    {
        Image[] slots = { hand1, hand2, hand3, hand4, hand5 };

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            bool isEmpty = slot.sprite == null || slot.sprite == emptySprite;
            if (!isEmpty) continue;

            slot.sprite = icon;
            slot.color = Color.white;   // 투명도 복구
            return true;
        }
        return false;
    }

    /* ──────────── 슬롯 UI 알파 조정 ──────────── */
    private void SetHandAlpha(int handIndex)
    {
        SetAlpha(hand1, handIndex == 1);
        SetAlpha(hand2, handIndex == 2);
        SetAlpha(hand3, handIndex == 3);
        SetAlpha(hand4, handIndex == 4);
        SetAlpha(hand5, handIndex == 5);
    }

    private void SetAlpha(Image img, bool selected)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = selected ? 1f : 60f / 255f;
        img.color = c;
    }
}
