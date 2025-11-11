using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NewCraftingUI : MonoBehaviour
{
    // ==== 열기/닫기 ====
    [Header("Open/Close")]
    [SerializeField] private GameObject craftingPanelRoot; // 비워두면 자동탐색
    [SerializeField] private KeyCode toggleKey = KeyCode.R;
    [SerializeField] private KeyCode closeKey  = KeyCode.Escape;
    [SerializeField] private bool pauseOnOpen = false;

    // ==== Inventory ====
    [Header("Inventory")]
    [SerializeField] private Inventory inventory;

    // ==== 탐색 옵션 ====
    [Header("Search Options")]
    [SerializeField] private string itemResourcesPath = "Item";
    [SerializeField] private string[] rootNameCandidates = { "CraftingPanel", "CraftingUI", "Crafting" };
    [SerializeField] private string[] blockIfSceneNameContains = { "Main" }; // Main, Main1 등

    // ==== 내부 참조 ====
    private Transform _root;             // CraftingPanel
    private Transform _cat;              // CraftingPanel/Category
    private Button _tabConv, _tabTools, _tabPotion;

    private GameObject _pageConv;        // CraftingPanel/Conversion
    private GameObject _pageTools;       // CraftingPanel/Tools
    private GameObject _pagePotion;      // CraftingPanel/Potion

    // Tools 하위 페이지
    private GameObject _toolsCopper;     // Tools/Copper
    private GameObject _toolsIron;       // Tools/Iron
    private Button _btnNext;             // Tools/Copper/NextButton
    private Button _btnPrev;             // Tools/Iron/PreviousButton

    // ==== 레시피 ====
    [Serializable] public struct ItemStack { public string id; public int count; public ItemStack(string id, int c){ this.id=id; this.count=c; } }
    [Serializable] public class Recipe { public List<ItemStack> cost = new(); public List<ItemStack> reward = new(); }
    private readonly Dictionary<string, Recipe> recipes = new();

    // ItemID -> ItemData 캐시
    private Dictionary<string, ItemData> _itemDb;

    // 바인딩 상태 플래그
    private bool _boundOnce;

    // ───────────────────────────────────────

    private void Awake()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (ShouldBlockInThisScene()) { SafeClose(); return; }

        TryResolveAndBindUI();     // 초기 1회
        SafeClose();               // 시작은 닫아두기
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 새 씬 진입 시 재바인딩 준비
        _boundOnce = false;
        craftingPanelRoot = null;
        _root = null;

        if (ShouldBlockInThisScene()) { SafeClose(); return; }

        TryResolveAndBindUI(); // 새 씬에서 다시 찾고 다시 바인딩
        SafeClose();
    }

    private void Update()
    {
        if (ShouldBlockInThisScene()) { SafeClose(); return; }

        if (Input.GetKeyDown(toggleKey))
        {
            // 첫 열기 전에 한 번 더 바인딩 시도 (지연 바인딩)
            if (!_boundOnce || craftingPanelRoot == null) TryResolveAndBindUI();
            ToggleCrafting();
        }

        if (craftingPanelRoot != null && craftingPanelRoot.activeSelf && Input.GetKeyDown(closeKey))
            CloseCrafting();
    }

    // ───────────────────────────────────────
    // 바인딩 핵심
    // ───────────────────────────────────────
    private void TryResolveAndBindUI()
    {
        // 루트 찾기
        if (craftingPanelRoot == null)
        {
            craftingPanelRoot = FindCraftingRootByNames(rootNameCandidates);
            if (craftingPanelRoot == null)
                craftingPanelRoot = FindCraftingRootHeuristics(); // 구조 휴리스틱
        }

        if (craftingPanelRoot == null)
        {
            Debug.LogWarning("[CraftingUI] 루트를 찾지 못했습니다. (이름 후보/구조 확인)");
            return;
        }

        _root = craftingPanelRoot.transform;

        // 카테고리/탭
        _cat = _root.Find("Category");
        _tabConv   = EnsureButton(_cat?.Find("Conversion"));
        _tabTools  = EnsureButton(_cat?.Find("Tools"));
        _tabPotion = EnsureButton(_cat?.Find("Potion"));

        // 콘텐츠 패널
        _pageConv   = _root.Find("Conversion")?.gameObject;
        _pageTools  = _root.Find("Tools")?.gameObject;
        _pagePotion = _root.Find("Potion")?.gameObject;

        // Tools 내부 페이지와 넘김 버튼
        _toolsCopper = _pageTools ? _pageTools.transform.Find("Copper")?.gameObject : null;
        _toolsIron   = _pageTools ? _pageTools.transform.Find("Iron")?.gameObject   : null;

        _btnNext = FindButtonRecursive(_toolsCopper?.transform, "NextButton");
        _btnPrev = FindButtonRecursive(_toolsIron?.transform,   "PreviousButton");

        // 리스너 중복 방지
        if (_btnNext) { _btnNext.onClick.RemoveAllListeners(); _btnNext.onClick.AddListener(()=> ShowToolsPage(_toolsIron)); }
        if (_btnPrev) { _btnPrev.onClick.RemoveAllListeners(); _btnPrev.onClick.AddListener(()=> ShowToolsPage(_toolsCopper)); }

        if (_tabConv)   { _tabConv.onClick.RemoveAllListeners();   _tabConv.onClick.AddListener(() => ShowOnly(_pageConv)); }
        if (_tabTools)  { _tabTools.onClick.RemoveAllListeners();  _tabTools.onClick.AddListener(() => { ShowOnly(_pageTools); ShowToolsPage(_toolsCopper); }); }
        if (_tabPotion) { _tabPotion.onClick.RemoveAllListeners(); _tabPotion.onClick.AddListener(() => ShowOnly(_pagePotion)); }

        // 레시피 & 버튼 바인딩
        BuildRecipeDB();
        BindRecipeButtons();

        _boundOnce = true;
    }

    // 이름 후보로 찾기
    private GameObject FindCraftingRootByNames(string[] candidates)
    {
        if (candidates == null) return null;
        foreach (var name in candidates)
        {
            if (string.IsNullOrEmpty(name)) continue;
            var go = FindGODeep(name);
            if (go) return go;
        }
        // 마지막으로 기본값 시도
        return FindGODeep("CraftingPanel");
    }

    // 구조 휴리스틱으로 찾기 (Category / Conversion / Tools / Potion)
    private GameObject FindCraftingRootHeuristics()
    {
        foreach (var cv in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (cv == null || !cv.gameObject.scene.IsValid()) continue;
            foreach (var t in cv.GetComponentsInChildren<Transform>(true))
            {
                if (!t || !t.gameObject.scene.IsValid()) continue;
                var hasCategory = t.Find("Category");
                var hasConv     = t.Find("Conversion");
                var hasTools    = t.Find("Tools");
                var hasPotion   = t.Find("Potion");
                if (hasCategory && (hasConv || hasTools || hasPotion))
                    return t.gameObject;
            }
        }
        return null;
    }

    // ───────────────────────────────────────
    // 씬 이름 차단
    // ───────────────────────────────────────
    private bool ShouldBlockInThisScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        foreach (var kw in blockIfSceneNameContains)
        {
            if (!string.IsNullOrEmpty(kw) &&
                sceneName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private void SafeClose()
    {
        if (craftingPanelRoot && craftingPanelRoot.activeSelf) CloseCrafting();
    }

    // ───────────────────────────────────────
    // 열기/닫기
    // ───────────────────────────────────────
    public void ToggleCrafting()
    {
        if (craftingPanelRoot == null) return;
        if (craftingPanelRoot.activeSelf) CloseCrafting();
        else OpenCrafting();
    }

    public void OpenCrafting()
    {
        craftingPanelRoot.SetActive(true);
        ShowOnly(_pageConv);
        if (_pageTools != null) ShowToolsPage(_toolsCopper);
        if (pauseOnOpen) Time.timeScale = 0f;
    }

    public void CloseCrafting()
    {
        craftingPanelRoot.SetActive(false);
        if (pauseOnOpen) Time.timeScale = 1f;
    }

    // ───────────────────────────────────────
    // 레시피
    // ───────────────────────────────────────
    private void BuildRecipeDB()
    {
        recipes.Clear();

        // 전환
        recipes["ConversionIron"]        = new Recipe { cost = new(){ new("IronOre", 1) },        reward = new(){ new("Iron", 1) } };
        recipes["ConversionCopperPiece"] = new Recipe { cost = new(){ new("CopperPiece", 3) },    reward = new(){ new("CopperOre", 1) } };
        recipes["ConversionCopper"]      = new Recipe { cost = new(){ new("CopperOre", 1) },      reward = new(){ new("Copper", 1) } };

        // 구리 도구
        recipes["CopperSword"]    = MakeTool("Copper", "Wood", 1, 1, "CopperSword");
        recipes["CopperAxe"]      = MakeTool("Copper", "Wood", 2, 2, "CopperAxe");
        recipes["CopperPick"]     = MakeTool("Copper", "Wood", 3, 2, "CopperPick");
        recipes["CopperHoe"]      = MakeTool("Copper", "Wood", 1, 2, "CopperHoe");
        recipes["CopperWatering"] = new Recipe { cost = new(){ new("Copper", 3) }, reward = new(){ new("Watering_Can_002", 1) } };
        recipes["WoodenBow"]      = new Recipe { cost = new(){ new("Wood", 3) },   reward = new(){ new("WoodenBow_001", 1) } };

        // 철 도구
        recipes["IronSword"]      = MakeTool("Iron", "Wood", 1, 1, "IronSword_001");
        recipes["IronAxe"]        = MakeTool("Iron", "Wood", 2, 2, "IronAxe_001");
        recipes["IronPick"]       = MakeTool("Iron", "Wood", 3, 2, "IronPick_001");
        recipes["IronHoe"]        = MakeTool("Iron", "Wood", 1, 2, "IronHoe_001");
        recipes["IronWatering"]   = new Recipe { cost = new(){ new("Iron", 3) },   reward = new(){ new("Watering_Can_001", 1) } };
        recipes["IronBow"]        = new Recipe { cost = new(){ new("Iron", 3) },   reward = new(){ new("IronBow", 1) } };

        // 포션
        recipes["WhitePotion"]    = new Recipe { cost = new(){ new("Bone", 1) },       reward = new(){ new("WhitePotion", 1) } };
        recipes["GoldPotion"]     = new Recipe { cost = new(){ new("SlimePiece", 1) }, reward = new(){ new("GoldPotion", 1) } };
    }

    private Recipe MakeTool(string metal, string wood, int metalCnt, int woodCnt, string resultId)
        => new Recipe { cost = new(){ new(metal, metalCnt), new(wood, woodCnt) }, reward = new(){ new(resultId, 1) } };

    // 버튼 ↔ 레시피 자동 바인딩
    private void BindRecipeButtons()
    {
        foreach (var btn in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (btn == null) continue;
            if (!btn.gameObject.scene.IsValid()) continue; // 프리팹 제외
            var key = btn.name;
            if (!recipes.ContainsKey(key)) continue;       // 탭/Next/Prev 제외
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => TryCraft(key));
        }
    }

    private void TryCraft(string recipeKey)
    {
        if (!recipes.TryGetValue(recipeKey, out var recipe)) return;

        // 1) 보유 확인
        foreach (var c in recipe.cost)
            if (!Has(c.id, c.count)) { Debug.Log($"[Crafting] 재료 부족: {c.id} x{c.count}"); return; }

        // 2) 차감
        foreach (var c in recipe.cost)
            if (!Remove(c.id, c.count)) { Debug.LogWarning($"[Crafting] 차감 실패: {c.id} x{c.count}"); return; }

        // 3) 지급
        foreach (var r in recipe.reward)
            Give(r.id, r.count);

        Debug.Log($"[Crafting] 완료: {recipeKey}");
    }

    // 표시 제어
    private void ShowOnly(GameObject only)
    {
        var arr = new[] { _pageConv, _pageTools, _pagePotion };
        foreach (var p in arr) if (p != null) p.SetActive(p == only);
    }

    private void ShowToolsPage(GameObject page)
    {
        if (_pageTools == null) return;
        if (_toolsCopper != null) _toolsCopper.SetActive(false);
        if (_toolsIron   != null) _toolsIron.SetActive(false);
        if (page != null) page.SetActive(true);
    }

    // ───────────────────────────────────────
    // 인벤토리 호출 (ID→ItemData 변환은 이 스크립트가 담당)
    // ───────────────────────────────────────
    private bool Has(string id, int cnt)
    {
        if (inventory == null) return false;
        var data = ResolveItem(id);
        return data != null && inventory.HasItem(data, cnt);
    }

    private bool Remove(string id, int cnt)
    {
        if (inventory == null) return false;
        var data = ResolveItem(id);
        if (data == null) return false;
        if (!inventory.HasItem(data, cnt)) return false;
        inventory.RemoveItem(data, cnt);
        return true;
    }

    private void Give(string id, int cnt)
    {
        if (inventory == null) return;
        inventory.AddItemByID(id, cnt);
    }

    private void BuildItemDbIfNeeded()
    {
        if (_itemDb != null) return;
        _itemDb = new Dictionary<string, ItemData>(StringComparer.Ordinal);

        var all = Resources.LoadAll<ItemData>(itemResourcesPath);
        foreach (var it in all)
        {
            if (it == null) continue;
            string key = TryReadItemId(it);
            if (string.IsNullOrEmpty(key)) key = it.name; // 없으면 에셋 이름
            if (!_itemDb.ContainsKey(key)) _itemDb.Add(key, it);
        }
    }

    private ItemData ResolveItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        BuildItemDbIfNeeded();
        if (_itemDb.TryGetValue(id, out var d)) return d;
        // 에셋 이름으로도 한 번 더 시도
        return _itemDb.Values.FirstOrDefault(x => x && x.name == id);
    }

    private static string TryReadItemId(object it)
    {
        if (it == null) return null;
        var t = it.GetType();
        string[] names = { "ItemID","ItemId","itemID","itemId","ID","Id","id","ItemCode","itemCode","code","Code" };
        foreach (var n in names)
        {
            var p = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(string))
            {
                var v = p.GetValue(it) as string;
                if (!string.IsNullOrEmpty(v)) return v;
            }
        }
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
            {
                var v = f.GetValue(it) as string;
                if (!string.IsNullOrEmpty(v)) return v;
            }
        }
        return null;
    }

    // ───────────────────────────────────────
    // 유틸
    // ───────────────────────────────────────
    private static GameObject FindGODeep(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == name && t.gameObject.scene.IsValid())
                return t.gameObject;
        return null;
    }

    private static Button EnsureButton(Transform tr)
    {
        if (tr == null) return null;
        var btn = tr.GetComponent<Button>();
        if (btn != null) return btn;

        var img = tr.GetComponent<Image>();
        if (!img) img = tr.gameObject.AddComponent<Image>();
        img.raycastTarget = true;

        btn = tr.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    private static Button FindButtonRecursive(Transform root, string name)
    {
        if (root == null) return null;
        var direct = root.Find(name);
        if (direct) return EnsureButton(direct);

        foreach (Transform c in root)
        {
            var r = FindButtonRecursive(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
