using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Inventory : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // 싱글턴 인벤토리
    //  - 어디서든 Inventory.Instance 로 접근
    //  - 같은 오브젝트가 중복 생성되면 자신을 파괴 (씬 중복 방지)
    //  - 씬 전환 후에도 유지하려면 DontDestroyOnLoad(gameObject) 추가 검토
    // ─────────────────────────────────────────────────────────────────────────────
    public static Inventory Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────────
    // 슬롯 단위 데이터 구조
    //  - icon      : 슬롯에 표시될 이미지(UI Image)
    //  - countLabel: 수량 텍스트(TMP_Text)
    //  - count     : 보유 개수(0 이하면 빈 슬롯으로 간주)
    // ─────────────────────────────────────────────────────────────────────────────
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
    [SerializeField] private Sprite emptySprite;     // 빈 슬롯일 때 표시용 스프라이트(없으면 투명만 적용)
    public Sprite EmptySprite => emptySprite;

    [Header("Bag UI 패널")]
    [SerializeField] private GameObject bagPanel;    // E키로 열고 닫는 가방 UI

    [SerializeField, Range(1, 10)] private int current = 1;  // 현재 선택된 퀵슬롯(1~10)
    public int states => current;                               // 외부용 읽기 프로퍼티

    // ─────────────────────────────────────────────────────────────────────────────
    // 라이프사이클: 초기화
    //  1) 싱글턴 세팅
    //  2) 드래그/드롭 보조 컴포넌트 자동 부착
    //  3) 슬롯 카운트 정규화(아이콘 있는데 count=0 이면 1로 보정)
    //  4) 초기 시각화(모든 슬롯 리프레시 + 현재 선택 하이라이트)
    // ─────────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AutoAttachDragScripts();
        NormalizeCounts();
        InitSlotVisuals();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 초기 시각화: 모든 슬롯을 Refresh 후, 현재 선택 슬롯 하이라이트 반영
    // ─────────────────────────────────────────────────────────────────────────────
    void InitSlotVisuals()
    {
        foreach (var s in quickSlots) RefreshSlot(s);
        foreach (var s in bagSlots) RefreshSlot(s);

        // 첫 선택 상태 반영 (current가 범위 밖이면 보정)
        SelectQuick(Mathf.Clamp(current, 1, Mathf.Max(1, quickSlots.Length)));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 데이터 정규화: 아이콘이 있는데 count==0 이면 1로 보정(초기 세팅 오류 방지)
    // ─────────────────────────────────────────────────────────────────────────────
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

            // 아이콘이 '있고'(null 아님), 그 아이콘이 emptySprite가 아니며, count == 0 이면 1로
            if (s.count == 0 && s.icon.sprite != null && s.icon.sprite != emptySprite)
                s.count = 1; // 아이콘이 실아이템인데 count만 0인 초기 실수를 보정
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 입력 처리
    //  - E: 가방 패널 토글
    //  - 숫자키 1~0: 퀵슬롯 선택(1~10)
    // ─────────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && bagPanel != null)
            bagPanel.SetActive(!bagPanel.activeSelf);

        // 숫자키 → 퀵슬롯 선택
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 퀵슬롯 선택/하이라이트
    //  - 선택 슬롯: 알파 1.0
    //  - 비선택 슬롯: 알파 0.6
    //  - 빈 슬롯: 알파 0.0(완전 투명)
    // ─────────────────────────────────────────────────────────────────────────────
    private void SelectQuick(int idx)
    {
        if (quickSlots == null || quickSlots.Length == 0) return;

        current = Mathf.Clamp(idx, 1, quickSlots.Length);

        for (int i = 0; i < quickSlots.Length; i++)
        {
            var s = quickSlots[i];
            if (s == null || s.icon == null) continue;

            if (s.count > 0)
                SetAlpha(s.icon, i == current - 1);   // 선택된 슬롯만 1f
            else
                s.icon.color = new Color(1f, 1f, 1f, 0f); // 빈 슬롯은 투명
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 아이템 획득
    //  - 먼저 동일 Sprite 스택 시도
    //  - 실패 시 빈 슬롯 채움
    //  - 퀵슬롯 → 가방 순으로 시도
    // ─────────────────────────────────────────────────────────────────────────────
    public void AddItem(Sprite icon, int amount = 1)
    {
        if (icon == null || amount <= 0) return;

        if (TryStackOrFill(quickSlots, icon, amount)) return;
        if (TryStackOrFill(bagSlots, icon, amount)) return;

        Debug.LogWarning("인벤토리(퀵+가방) 모두 가득 찼습니다");
    }

    // 동일 아이콘 스택 → 빈 슬롯 채우기 순
    private bool TryStackOrFill(ItemSlot[] arr, Sprite icon, int amount)
    {
        if (arr == null) return false;

        // 1) 같은 아이콘(동일 Sprite 레퍼런스) 스택
        foreach (var s in arr)
        {
            if (s == null || s.icon == null) continue;
            if (s.count > 0 && s.icon.sprite == icon)
            {
                s.count += amount;
                RefreshSlot(s);
                return true;
            }
        }

        // 2) 빈 슬롯 채우기
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 선택 슬롯 아이템 소비
    //  - 현재 퀵슬롯에서 amount 만큼 감소
    //  - 부족하면 false
    // ─────────────────────────────────────────────────────────────────────────────
    public bool ConsumeSelectedItem(int amount = 1)
    {
        if (quickSlots == null || quickSlots.Length == 0) return false;

        var slot = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];
        if (slot == null || slot.count < amount) return false;

        slot.count -= amount;
        RefreshSlot(slot);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 슬롯 시각 갱신
    //  - countLabel: 2개 이상이면 "×N", 아니면 빈 문자열
    //  - 빈 슬롯: emptySprite(있으면) + 알파 0
    //  - 보유 슬롯: 알파 1 (선택/비선택 알파는 SelectQuick에서 다시 조절)
    // ─────────────────────────────────────────────────────────────────────────────
    public void RefreshSlot(ItemSlot s)
    {
        if (s == null || s.icon == null) return;

        // 수량 표시 업데이트
        if (s.countLabel != null)
            s.countLabel.text = s.count > 1 ? $"×{s.count}" : string.Empty;

        if (s.count <= 0)
        {
            // 빈 슬롯 처리
            if (emptySprite != null) s.icon.sprite = emptySprite;
            s.icon.color = new Color(1f, 1f, 1f, 0f); // 완전 투명
            s.count = 0; // 음수 방지
        }
        else
        {
            // 보유 슬롯 표시
            if (s.icon.sprite == null && emptySprite != null)
                s.icon.sprite = emptySprite; 

            s.icon.color = Color.white; // 완전 불투명
        }
    }

    // 선택/비선택 시 알파값 조절(선택: 1.0, 비선택: 0.6)
    private void SetAlpha(Image img, bool selected)
    {
        if (img == null) return;
        var c = img.color;
        c.a = selected ? 1f : 0.6f;
        img.color = c;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 드래그&드롭 보조 세팅
    //  - 각 슬롯 아이콘 GameObject에 CanvasGroup/SlotDrag/SlotDropTarget 자동 부착
    //  - 실제 드래그 중 blocksRaycasts on/off 전환은 SlotDrag 구현체에서 처리해야 함
    // ─────────────────────────────────────────────────────────────────────────────
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

            // 드래그/드롭용 레이캐스트 제어를 위한 CanvasGroup 보장
            if (!go.TryGetComponent(out CanvasGroup cg))
                cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;

            // 드래그/드롭 스크립트가 없으면 자동 추가
            if (!go.GetComponent<SlotDrag>()) go.AddComponent<SlotDrag>();
            if (!go.GetComponent<SlotDropTarget>()) go.AddComponent<SlotDropTarget>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 유틸성 API
    // ─────────────────────────────────────────────────────────────────────────────

    // 현재 선택된 퀵슬롯의 스프라이트 반환 (없으면 null)
    public Sprite GetSelectedSprite()
    {
        if (quickSlots == null || quickSlots.Length == 0) return null;

        var s = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];
        return (s != null && s.icon != null) ? s.icon.sprite : null;
    }

    // 현재 선택된 퀵슬롯이 비었는지 여부
    public bool IsSelectedEmpty()
    {
        if (quickSlots == null || quickSlots.Length == 0) return true;

        var s = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];
        return (s == null) || s.count == 0;
    }

    // 지정 Sprite 아이템이 need 수량 이상 있는지 확인(퀵+가방 합산)
    public bool HasItem(Sprite icon, int need)
    {
        if (need <= 0 || icon == null) return true;

        int sum = 0;

        if (quickSlots != null)
        {
            foreach (var s in quickSlots)
                if (s != null && s.icon != null && s.icon.sprite == icon)
                    sum += s.count;
        }

        if (bagSlots != null)
        {
            foreach (var s in bagSlots)
                if (s != null && s.icon != null && s.icon.sprite == icon)
                    sum += s.count;
        }

        return sum >= need;
    }

    // 지정 Sprite 아이템을 count 만큼 제거(퀵 → 가방 순으로 차감)
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

    // 주어진 Image가 어느 슬롯의 icon인지 탐색(참조 비교)
    public ItemSlot FindSlotByIcon(Image icon)
    {
        if (icon == null) return null;

        if (quickSlots != null)
            foreach (var q in quickSlots)
                if (q != null && q.icon == icon) return q;

        if (bagSlots != null)
            foreach (var b in bagSlots)
                if (b != null && b.icon == icon) return b;

        return null;
    }
}
