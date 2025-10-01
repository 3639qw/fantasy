using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// 저장을 위한 데이터 구조 (ItemID를 저장하도록 변경)
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class SerializableSlotData
{
    public string itemID; // spriteName 대신 itemID를 저장
    public int count;

    public SerializableSlotData(string id, int num)
    {
        itemID = id;
        count = num;
    }
}

[System.Serializable]
public class InventoryData
{
    public List<SerializableSlotData> quickSlotsData = new List<SerializableSlotData>();
    public List<SerializableSlotData> bagSlotsData = new List<SerializableSlotData>();
}

// ─────────────────────────────────────────────────────────────────────────────
// 인벤토리 메인 클래스
// ─────────────────────────────────────────────────────────────────────────────
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [System.Serializable]
    public class ItemSlot
    {
        public ItemData itemData; // 슬롯이 이제 Sprite가 아닌 ItemData를 직접 가짐
        public Image icon;
        public TMP_Text countLabel;
        [HideInInspector] public int count;
    }

    [Header("슬롯 설정")]
    public ItemSlot[] quickSlots = new ItemSlot[10];
    public ItemSlot[] bagSlots = new ItemSlot[50];

    [Header("UI 및 기타")]
    [SerializeField] private Sprite emptySprite;
    public Sprite EmptySprite => emptySprite;
    [SerializeField] private GameObject bagPanel;

    [SerializeField, Range(1, 10)] private int current = 1;
    public int states => current;

    private string savePath;
    // 아이템 ID를 키로 사용하는 아이템 데이터베이스
    private Dictionary<string, ItemData> _itemDatabase = new Dictionary<string, ItemData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        savePath = Path.Combine(Application.persistentDataPath, "inventory.json");
        
        // 게임 시작 시 모든 ItemData를 불러와 데이터베이스를 구축
        LoadAllItemDataToDatabase();

        AutoAttachDragScripts();
        InitSlotVisuals();
    }

    void Start()
    {
        // ==========================================================
        // ✨ 테스트용 초기 아이템 5개 추가
        // ==========================================================
        AddItemByID("Watering_Can_001");
        AddItemByID("IronHoe_001");
        AddItemByID("IronSword_001");
        AddItemByID("IronAxe_001");
        AddItemByID("IronPick_001");
    }

    private void LoadAllItemDataToDatabase()
    {
        _itemDatabase.Clear();
        // "Assets/Resources/Item" 폴더에 있는 모든 ItemData 애셋을 불러옴
        ItemData[] allItems = Resources.LoadAll<ItemData>("Item"); 
        foreach (ItemData item in allItems)
        {
            if (!_itemDatabase.ContainsKey(item.itemID))
            {
                _itemDatabase.Add(item.itemID, item);
            }
            else
            {
                Debug.LogWarning($"[Inventory] 중복된 아이템 ID가 발견되었습니다: {item.itemID}");
            }
        }
        Debug.Log($"<color=green>[Inventory] {_itemDatabase.Count}개의 아이템 데이터를 데이터베이스에 로드했습니다.</color>");
    }
    
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

        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveInventory();
            Debug.Log("<color=cyan>인벤토리 저장 완료!</color>");
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            LoadInventory();
            Debug.Log("<color=yellow>인벤토리 불러오기 시도...</color>");
        }
    }

    public void SaveInventory()
    {
        InventoryData data = new InventoryData();
        foreach (var slot in quickSlots)
        {
            if (slot.count > 0 && slot.itemData != null)
                data.quickSlotsData.Add(new SerializableSlotData(slot.itemData.itemID, slot.count));
            else
                data.quickSlotsData.Add(new SerializableSlotData("", 0));
        }
        foreach (var slot in bagSlots)
        {
            if (slot.count > 0 && slot.itemData != null)
                data.bagSlotsData.Add(new SerializableSlotData(slot.itemData.itemID, slot.count));
            else
                data.bagSlotsData.Add(new SerializableSlotData("", 0));
        }
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadInventory()
    {
        if (!File.Exists(savePath)) return;
        string json = File.ReadAllText(savePath);
        InventoryData data = JsonUtility.FromJson<InventoryData>(json);

        for (int i = 0; i < quickSlots.Length; i++)
        {
            var slotData = (i < data.quickSlotsData.Count) ? data.quickSlotsData[i] : null;
            if (slotData != null && !string.IsNullOrEmpty(slotData.itemID) && _itemDatabase.TryGetValue(slotData.itemID, out ItemData itemData))
            {
                quickSlots[i].itemData = itemData;
                quickSlots[i].count = slotData.count;
            }
            else
            {
                quickSlots[i].itemData = null;
                quickSlots[i].count = 0;
            }
        }
        for (int i = 0; i < bagSlots.Length; i++)
        {
            var slotData = (i < data.bagSlotsData.Count) ? data.bagSlotsData[i] : null;
            if (slotData != null && !string.IsNullOrEmpty(slotData.itemID) && _itemDatabase.TryGetValue(slotData.itemID, out ItemData itemData))
            {
                bagSlots[i].itemData = itemData;
                bagSlots[i].count = slotData.count;
            }
            else
            {
                bagSlots[i].itemData = null;
                bagSlots[i].count = 0;
            }
        }
        InitSlotVisuals();
    }
    
    void InitSlotVisuals()
    {
        foreach (var s in quickSlots) RefreshSlot(s);
        foreach (var s in bagSlots) RefreshSlot(s);
        SelectQuick(current);
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
                SetAlpha(s.icon, i == current - 1);
            else
                s.icon.color = new Color(1f, 1f, 1f, 0f);
        }
    }
    
    // 아이템 ID로 아이템을 추가하는 헬퍼 메서드
    public void AddItemByID(string itemID, int amount = 1)
    {
        if (_itemDatabase.TryGetValue(itemID, out ItemData data))
        {
            AddItem(data, amount);
        }
        else
        {
            Debug.LogWarning($"[Inventory] 데이터베이스에 '{itemID}' ID를 가진 아이템이 없습니다.");
        }
    }

    // ItemData를 직접 받아 아이템을 추가하는 메인 메서드
    public void AddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return;
        if (TryStackOrFill(quickSlots, itemData, amount)) return;
        if (TryStackOrFill(bagSlots, itemData, amount)) return;
        Debug.LogWarning("인벤토리가 가득 찼습니다.");
    }

    private bool TryStackOrFill(ItemSlot[] arr, ItemData itemData, int amount)
    {
        if (arr == null) return false;

        // 1) 같은 아이템(동일 ItemData) 스택
        foreach (var s in arr)
        {
            if (s.count > 0 && s.itemData == itemData)
            {
                s.count += amount;
                RefreshSlot(s);
                return true;
            }
        }

        // 2) 빈 슬롯 채우기
        foreach (var s in arr)
        {
            if (s.count == 0 || s.itemData == null)
            {
                s.itemData = itemData;
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
        if (slot == null || slot.itemData == null || slot.count < amount) return false;
        
        slot.count -= amount;
        RefreshSlot(slot);
        return true;
    }
    
    public void RefreshSlot(ItemSlot s)
    {
        if (s == null || s.icon == null) return;
        if (s.count <= 0) s.itemData = null; // 수량이 0 이하면 데이터도 비움

        if (s.itemData != null) // 데이터가 있는 슬롯
        {
            s.icon.sprite = s.itemData.itemIcon;
            s.icon.color = Color.white;
            s.countLabel.text = s.count > 1 ? $"×{s.count}" : string.Empty;
        }
        else // 빈 슬롯
        {
            s.icon.sprite = emptySprite;
            s.icon.color = new Color(1, 1, 1, (emptySprite == null ? 0 : 1));
            s.countLabel.text = string.Empty;
            s.count = 0;
        }
    }

    private void SetAlpha(Image img, bool selected)
    {
        if (img == null) return;
        var c = img.color;
        c.a = selected ? 1f : 0.6f;
        img.color = c;
    }
    
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

        if (!go.TryGetComponent(out CanvasGroup cg)) 
            go.AddComponent<CanvasGroup>().blocksRaycasts = true;

        // <<-- 변경: SlotDrag 스크립트를 가져오거나 추가한 후, Initialize 메서드를 호출해줍니다.
        if (!go.TryGetComponent(out SlotDrag dragScript))
            dragScript = go.AddComponent<SlotDrag>();
        
        dragScript.Initialize(s); // <<-- 중요! 이 슬롯의 데이터(s)를 SlotDrag 스크립트에게 알려줍니다.

        if (!go.GetComponent<SlotDropTarget>()) 
            go.AddComponent<SlotDropTarget>();
    }
}

    public ItemData GetSelectedItemData()
    {
        if (quickSlots == null || quickSlots.Length == 0) return null;
        var slot = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];
        return (slot != null && slot.count > 0) ? slot.itemData : null;
    }

    public bool IsSelectedEmpty()
    {
        return GetSelectedItemData() == null;
    }

    public bool HasItem(ItemData itemData, int need)
    {
        if (need <= 0 || itemData == null) return true;
        int sum = 0;
        foreach (var s in quickSlots) if (s.itemData == itemData) sum += s.count;
        foreach (var s in bagSlots) if (s.itemData == itemData) sum += s.count;
        return sum >= need;
    }
    
    public void RemoveItem(ItemData itemData, int count)
    {
        if (itemData == null || count <= 0) return;
        void CountDown(ItemSlot[] arr)
        {
            if (arr == null) return;
            foreach (var s in arr)
            {
                if (count == 0) return;
                if (s.itemData != itemData) continue;
                int take = Mathf.Min(s.count, count);
                s.count -= take;
                count -= take;
                RefreshSlot(s);
            }
        }
        CountDown(quickSlots);
        CountDown(bagSlots);
    }
}