using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
    private Dictionary<string, List<Transform>> _uiNameIndex;

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

    [Header("플레이어")]
    [Tooltip("아이템 효과를 적용받을 플레이어 오브젝트")]
    [SerializeField] private GameObject playerObject;

    [SerializeField, Range(1, 10)] private int current = 1;
    public int states => current;

    [Header("Auto-Bind Override (Optional)")]
    [SerializeField] private Transform quickRootOverride; // 비워두면 Canvas 전체에서 검색
    [SerializeField] private Transform bagRootOverride;   // 비워두면 Canvas 전체에서 검색
    [SerializeField] private string bagPanelAutoName = "bag";

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

        TryAutoBindUIFromScene();
        BindBagPanelByName(null); 
        AutoAttachDragScripts();
        InitSlotVisuals();
    }

    void Start()
    {
        for (int i = 0; i < 30; i++)
        {
            AddItemByID("Arrow_001");
            AddItemByID("CopperPiece");
            AddItemByID("CopperOre");
            AddItemByID("IronOre");
            AddItemByID("Wood");
            AddItemByID("SlimePiece");
            AddItemByID("Bone");
        }
        AddItemByID("WoodenBow_001");
        AddItemByID("Bandage_001");
        AddItemByID("Soap_001");
        AddItemByID("DetoxPotion_001");
        AddItemByID("CopperPick");
        AddItemByID("IronPick_001");
        AddItemByID("IronSword_001");
        AddItemByID("IronAxe_001");
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
        {
            bagPanel.SetActive(!bagPanel.activeSelf);
            SoundManage.instance.PlaySFX("Inventory_Open");
        }
            

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

        if (Input.GetMouseButtonDown(0)) // 0 = 마우스 왼쪽 버튼
        {
            UseSelectedQuickSlotItem();
        }

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
        if (s.count <= 0) s.itemData = null;

        if (s.itemData != null)
        {
            s.icon.sprite = s.itemData.itemIcon;
            s.icon.color = Color.white;
            if (s.countLabel != null)
                s.countLabel.text = s.count > 1 ? $"×{s.count}" : string.Empty;
        }
        else
        {
            s.icon.sprite = emptySprite;
            s.icon.color = new Color(1, 1, 1, (emptySprite == null ? 0 : 1));
            if (s.countLabel != null)
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

    /// <param name="itemData">수량을 확인할 ItemData</param>
    /// <returns>총 아이템 개수</returns>
    public int GetItemQuantity(ItemData itemData)
    {
        if (itemData == null) return 0;

        int totalCount = 0;

        // 퀵슬롯에서 개수 확인
        if (quickSlots != null)
        {
            foreach (var s in quickSlots)
            {
                if (s.itemData == itemData)
                {
                    totalCount += s.count;
                }
            }
        }

        // 가방 슬롯에서 개수 확인
        if (bagSlots != null)
        {
            foreach (var s in bagSlots)
            {
                if (s.itemData == itemData)
                {
                    totalCount += s.count;
                }
            }
        }

        return totalCount;
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

    void OnEnable()  { SceneManager.sceneLoaded += _OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= _OnSceneLoaded; }
    private void _OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryAutoBindUIFromScene();
        BindBagPanelByName(null);
        AutoAttachDragScripts();
        InitSlotVisuals();
    }

    private bool _rebindDeferred = false;

    private void TryAutoBindUIFromScene()
    {
         var scn = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scn.IndexOf("Main", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // 필요하면 제작/가방 패널도 바로 끄기
            if (bagPanel) bagPanel.SetActive(false);
            Debug.Log("[Inventory] Main 씬 감지 → UI 자동 바인딩 스킵");
            return;
        }
        
        EnsureSlotsAllocated(quickSlots);
        EnsureSlotsAllocated(bagSlots);

        // 전역 UI 인덱스 새로 구성 (씬 로드시 UI가 바뀔 수 있으니 매번 빌드 권장)
        BuildUINameIndex();

        int qIconBound = 0, qCountBound = 0;
        int bIconBound = 0, bCountBound = 0;

        // (옵션) 인스펙터 오버라이드 루트
        Transform quickRoot = quickRootOverride ? quickRootOverride : null;
        Transform bagRoot   = bagRootOverride   ? bagRootOverride   : null;

        // ── Quick: Item1..Item10 + ItemCount1..ItemCount10 ──
        for (int i = 0; i < quickSlots.Length; i++)
        {
            string iconName  = $"Item{i + 1}";
            string countName = $"ItemCount{i + 1}";

            var iconTr  = FindByNameFromIndex(iconName,  quickRoot);
            var countTr = FindByNameFromIndex(countName, quickRoot);

            var img = iconTr  ? (iconTr.GetComponent<Image>() ?? iconTr.GetComponentInChildren<Image>(true)) : null;
            var txt = countTr ? (countTr.GetComponent<TMP_Text>() ?? countTr.GetComponentInChildren<TMP_Text>(true)) : null;
            if (txt == null && iconTr) txt = iconTr.GetComponentInChildren<TMP_Text>(true);

            if (quickSlots[i] == null) quickSlots[i] = new ItemSlot();
            quickSlots[i].icon = img;        if (img) qIconBound++;
            quickSlots[i].countLabel = txt;  if (txt) qCountBound++;
        }

        // ── Bag: slot1..slot50 + slotCount1..slotCount50 ──
        for (int i = 0; i < bagSlots.Length; i++)
        {
            string iconName  = $"slot{i + 1}";
            string countName = $"slotCount{i + 1}";

            var iconTr  = FindByNameFromIndex(iconName,  bagRoot);
            var countTr = FindByNameFromIndex(countName, bagRoot);

            var img = iconTr  ? (iconTr.GetComponent<Image>() ?? iconTr.GetComponentInChildren<Image>(true)) : null;
            var txt = countTr ? (countTr.GetComponent<TMP_Text>() ?? countTr.GetComponentInChildren<TMP_Text>(true)) : null;
            if (txt == null && iconTr) txt = iconTr.GetComponentInChildren<TMP_Text>(true);

            if (bagSlots[i] == null) bagSlots[i] = new ItemSlot();
            bagSlots[i].icon = img;          if (img) bIconBound++;
            bagSlots[i].countLabel = txt;    if (txt) bCountBound++;
        }
        BindBagPanelByName(bagRoot);
        
        Debug.Log($"[Inventory][AutoBindByName-GLOBAL] Quick icons {qIconBound}/10, counts {qCountBound}/10 | " +
                $"Bag icons {bIconBound}/50, counts {bCountBound}/50");

        // UI가 런타임에 늦게 생성될 경우 한 프레임 뒤 재시도
        if ((qIconBound + bIconBound) == 0 && !_rebindDeferred)
        {
            _rebindDeferred = true;
            StartCoroutine(_RebindNextFrame());
        }
    }
    private System.Collections.IEnumerator _RebindNextFrame()
    {
        yield return null; // 한 프레임 대기
        TryAutoBindUIFromScene();
        AutoAttachDragScripts();
        InitSlotVisuals();
    }

    // 이름(대소문자 무시)으로 전체 하위에서 1개 찾기
    // 이름(대소문자 무시)으로 한 루트 하위 재귀 탐색 (보조용)
    private Transform DeepFindByName(Transform root, string targetName)
    {
        if (!root || string.IsNullOrEmpty(targetName)) return null;
        string tn = targetName.ToLowerInvariant();

        foreach (Transform c in root)
            if (c && c.name.ToLowerInvariant() == tn)
                return c;

        foreach (Transform c in root)
        {
            var hit = DeepFindByName(c, targetName);
            if (hit) return hit;
        }
        return null;
    }

    private Transform DeepFindByNameAnyRoot(string targetName, Transform preferredRoot = null)
    {
        if (preferredRoot)
        {
            var hit = DeepFindByName(preferredRoot, targetName);
            if (hit) return hit;
        }

        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            var hit = DeepFindByName(go.transform, targetName);
            if (hit) return hit;
        }
        return null;
    }

    private Transform FindByNameFromIndex(string name, Transform preferRoot = null)
    {
        if (_uiNameIndex == null) BuildUINameIndex();

        var key = name.ToLowerInvariant();

        // 우선: 오버라이드 루트가 있으면 그 안에서 먼저 시도
        if (preferRoot != null)
        {
            var tr = DeepFindByName(preferRoot, name);
            if (tr) return tr;
        }

        // 전역 인덱스에서 첫 번째 항목 리턴
        if (_uiNameIndex.TryGetValue(key, out var list) && list != null && list.Count > 0)
            return list[0];

        return null;
    }

    private void BuildUINameIndex()
    {
        _uiNameIndex = new Dictionary<string, List<Transform>>(256);
        // 비활성 포함 + DontDestroyOnLoad 포함
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (var cv in canvases)
        {
            if (cv == null) continue;
            var go = cv.gameObject;
            if (!IsSceneObject(go)) continue; // 에셋 제외

            // 캔버스 하위 전체 수집(비활성 포함)
            var transforms = cv.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t == null) continue;
                var key = t.name.ToLowerInvariant();
                if (!_uiNameIndex.TryGetValue(key, out var list))
                {
                    list = new List<Transform>(2);
                    _uiNameIndex[key] = list;
                }
                list.Add(t);
            }
        }
    }

    private bool IsSceneObject(GameObject go)
    {
        // Prefab Asset 같은 에셋 제외, 씬 객체 또는 DontDestroyOnLoad만 포함
        return go != null && go.scene.IsValid();
    }

    private void EnsureSlotsAllocated(ItemSlot[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == null) arr[i] = new ItemSlot();
    }

    private Canvas FindActiveCanvas()
    {
    #if UNITY_2023_1_OR_NEWER
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
    #else
        var canvases = GameObject.FindObjectsOfType<Canvas>();
    #endif
        return canvases.FirstOrDefault(c => c.isActiveAndEnabled);
    }

    private Transform DeepFind(Transform root, string path)
    {
        // "A/B/C" 경로 지원 + 계층 전체 재귀 탐색
        if (root == null) return null;
        var parts = path.Split('/');
        Transform cur = root;
        foreach (var p in parts)
        {
            cur = cur.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t != null && string.Equals(t.name, p, System.StringComparison.OrdinalIgnoreCase));
            if (cur == null) return null;
        }
        return cur;
    }

    // prefixRegex: "^item\\s*(\\d+)" 또는 "^slot\\s*(\\d+)"
    private GameObject[] CollectSlotObjects(Transform root, string prefixRegex, int max)
    {
        if (root == null) return new GameObject[0];
        var re = new Regex(prefixRegex, RegexOptions.IgnoreCase);

        var list = root.GetComponentsInChildren<Transform>(true)
            .Select(t => new {
                tr = t,
                m = re.Match(t.name)
            })
            .Where(x => x.m.Success)
            .Select(x => new {
                go = x.tr.gameObject,
                num = int.TryParse(x.m.Groups[1].Value, out var n) ? n : int.MaxValue,
                sib = x.tr.GetSiblingIndex()
            })
            .OrderBy(x => x.num)       // Item1, Item2, ... / slot1, slot2, ...
            .ThenBy(x => x.sib)
            .Take(max)
            .Select(x => x.go)
            .ToArray();

        return list;
    }

    private void BindSlots(ItemSlot[] slots, GameObject[] gos)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (i < gos.Length && gos[i] != null)
            {
                // Image: 자기 자신 우선, 없으면 자식 중 첫번째
                var img = gos[i].GetComponent<Image>() ?? gos[i].GetComponentInChildren<Image>(true);
                // TMP_Text: 이름에 count/num/label 포함한 텍스트 우선
                var texts = gos[i].GetComponentsInChildren<TMP_Text>(true);
                var txt = texts.FirstOrDefault(t =>
                    t.name.ToLower().Contains("count") || t.name.ToLower().Contains("num") || t.name.ToLower().Contains("label"))
                    ?? texts.FirstOrDefault();

                s.icon = img;
                s.countLabel = txt;
            }
            else
            {
                s.icon = null;
                s.countLabel = null;
            }
        }
    }

    private void BindBagPanelByName(Transform bagRootPref = null)
    {
        if (bagPanel != null) return;                     // 이미 연결돼 있으면 패스
        if (_uiNameIndex == null) BuildUINameIndex();     // 전역 인덱스 없으면 생성

        // 대소문자 무시로 "Bag" (또는 오버라이드 이름)만 찾는다
        string key = string.IsNullOrEmpty(bagPanelAutoName) ? "Bag" : bagPanelAutoName;
        var t = FindByNameFromIndex(key, bagRootPref);
        if (t != null)
        {
            bagPanel = t.gameObject;
            Debug.Log($"[Inventory][BagBind] bagPanel = '{bagPanel.name}'");
        }
        else
        {
            Debug.LogWarning($"[Inventory][BagBind] '{key}' 오브젝트를 찾지 못했습니다.");
        }
    }
    /// <summary>
    /// 현재 선택된 퀵슬롯의 아이템을 사용합니다. (소모품 전용)
    /// </summary>
    public void UseSelectedQuickSlotItem()
    {
        // 1. 현재 선택된 퀵슬롯 정보 가져오기
        if (quickSlots == null || quickSlots.Length == 0) return;
        var slot = quickSlots[Mathf.Clamp(current - 1, 0, quickSlots.Length - 1)];

        if (slot == null || slot.itemData == null || slot.count <= 0)
        {
            // 빈 슬롯이거나 아이템이 없음
            return;
        }

        ItemData itemToUse = slot.itemData;


        if (itemToUse.itemType != "consumable")
        {
            // 소모품이 아니면(예: 무기, 도구) 사용 로직을 실행하지 않음
            return;
        }

        // 3. 플레이어 오브젝트가 할당되었는지 확인
        if (playerObject == null)
        {
            // playerObject 변수는 1단계에서 추가했습니다.
            // 유니티 인스펙터에서 플레이어 오브젝트를 끌어다 놓아야 합니다.
            Debug.LogError("[Inventory] 'Player Object' 참조가 비어있습니다! 인스펙터에서 할당해주세요.");
            return;
        }

        // 4. 플레이어의 StatusCondition 스크립트 가져오기
        StatusCondition playerStatus = playerObject.GetComponent<StatusCondition>();
        if (playerStatus == null)
        {
            Debug.LogError("아이템을 사용하려 했으나 플레이어에 StatusCondition 스크립트가 없습니다.");
            return;
        }

        // 5. [핵심] 아이템 효과 적용 (AntidoteItem.cs의 로직)
        bool itemSuccessfullyUsed = true; // 아이템을 소모해도 되는지 여부

        switch (itemToUse.curesStatusEffect)
        {
            case CuresStatusEffect.None:
                // 예: 여기서 체력 회복 포션 로직을 처리할 수 있습니다.
                // playerObject.GetComponent<PlayerHealth>().Heal(itemToUse.healAmount);
                Debug.Log($"{itemToUse.itemName}을 사용했지만 아무 효과가 없었습니다.");
                itemSuccessfullyUsed = false; // (실제 효과가 없으므로 소모 안 함)
                break;

            case CuresStatusEffect.Poison:
                playerStatus.CurePoison();
                break;

            case CuresStatusEffect.Bleeding:
                playerStatus.CureBleeding();
                break;

            case CuresStatusEffect.Slow:
                playerStatus.CureSlow();
                break;

            case CuresStatusEffect.All:
                playerStatus.CureAll();
                break;

            default:
                itemSuccessfullyUsed = false; // 정의되지 않은 효과
                break;
        }

        // 6. 효과가 성공적으로 적용되었다면 아이템 1개 소모
        if (itemSuccessfullyUsed)
        {
            // Inventory.cs에 이미 있는 '현재 선택 아이템 소모' 함수를 호출
            ConsumeSelectedItem(1);
            SoundManage.instance.PlaySFX("Use_Consumable");
            Debug.Log($"{itemToUse.itemName}을(를) 사용했습니다.");
        }
    }
}