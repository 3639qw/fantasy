using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Farming : MonoBehaviour
{
    public static Farming instance;

    GameManager gm;
    Inventory inv;

    [Header("채집 진행바 (없어도 동작)")]
    [SerializeField] Slider harvestBar;
    RectTransform barRoot; Canvas barCanvas; Camera cam;

    [Header("Cancel Distance (이상 멀어지면 취소)")]
    [SerializeField] float cancelDistance = 1.2f;

    [Header("수확 감지 범위")]
    [SerializeField] float harvestDistance = 1.5f;

    HashSet<Vector3Int> harvesting = new HashSet<Vector3Int>();

    [Header("Farm Tilemap & Tiles")]
    [SerializeField] protected internal Tilemap farmLand;
    [SerializeField] TileBase grassTile, tilledTile, farmTile, wetfarmTile;

    [Header("Auto-Bind UI (names)")]
    [SerializeField] private string harvestBarAutoName = "HarvestBar";

    [System.Serializable]
    public class CropData
    {
        public string name;
        public ItemData seedItemData;
        public ItemData cropItemData;
        public TileBase seedRemnant;
        public TileBase[] stages = new TileBase[4];
    }
    [SerializeField] CropData[] crops;

    [Header("Seed/Crop Tilemap")]
    [SerializeField] protected internal Tilemap seedLand;

    [Header("Farming Tool Gate (by ItemData.itemType)")]
    [Tooltip("콘솔에 게이트 결과를 출력할지")]
    [SerializeField] private bool debugToolGate = false;

    // Pick.cs 스타일
    [Header("개간/급수 애니메이션 & 쿨타임")]
    [SerializeField] private string hoeTriggerName = "Hoe";
    [SerializeField] private string wateringTriggerName = "Watering";
    [SerializeField] private float farmCoolTime = 0.5f;
    private float farmCurTime;

    private Animator _anim;
    private PlayerMove _playerMove;

    void Awake()
    {
        if (instance == null) instance = this; else Destroy(gameObject);
    }

    void Start()
    {
        gm = GameManager.Instance;
        inv = Inventory.Instance;
        TryBindHarvestBar();

        if (gm && gm.player)
        {
            _anim = gm.player.GetComponent<Animator>();
            _playerMove = gm.player.GetComponent<PlayerMove>();
        }
        else
        {
            _anim = GetComponent<Animator>();
            _playerMove = GetComponent<PlayerMove>();
        }

        if (!harvestBar)
        {
            cam = Camera.main;
        }
    }

    void Update()
    {
        if (farmCurTime > 0f) farmCurTime -= Time.deltaTime;

        // 좌클릭: 개간/급수 (타입 게이트)
        if (Input.GetMouseButtonDown(0) &&
            farmCurTime <= 0f &&
            _playerMove != null && !_playerMove.isAttacking &&
            (IsHoeSelected() || IsWateringCanSelected()))
        {
            if (TryFarmAction(5f)) return;
        }

        // Space: 기존 수확/심기/앞수확/BuildFarm 유지
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (!seedLand || !farmLand) { Debug.LogWarning("Tilemap reference missing"); return; }

        if (TryHarvestCrop()) return;
        if (!(IsWateringCanSelected() || IsHoeSelected()) && TryHarvestSeedForward()) return;

        CropData cd = FindCropBySeedData(inv?.GetSelectedItemData());
        if (cd != null && TryPlantSeed(cd)) return;

        // 스페이스로 하던 기본 개간/급수(호환유지)
        BuildFarm(5f);
    }

    // Pick.cs 스타일 개간/급수 실행부
    private bool TryFarmAction(float reqST)
    {
        if (!farmLand || gm == null || gm.player == null) return false;
        if (gm.ST < reqST) return false;

        Vector3Int pos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(pos);

        // 개간: tilled -> farm
        if (cur == tilledTile && IsHoeSelected())
        {
            TriggerFarmAnimTowardMouse(hoeTriggerName);
            farmLand.SetTile(pos, farmTile);
            gm.ConsumeSkill(2, reqST);

            farmCurTime = farmCoolTime;
            _playerMove.isAttacking = true;
            return true;
        }

        // 급수: farm -> wetfarm
        if (cur == farmTile && IsWateringCanSelected())
        {
            TriggerFarmAnimTowardMouse(wateringTriggerName);
            farmLand.SetTile(pos, wetfarmTile);
            gm.ConsumeSkill(2, reqST);

            farmCurTime = farmCoolTime;
            _playerMove.isAttacking = true;
            return true;
        }

        return false;
    }

    private void TriggerFarmAnimTowardMouse(string triggerName)
    {
        if (_anim == null) return;

        Vector2 dir = GetMouseWorldDir();
        _anim.SetFloat("AttackX", dir.x);
        _anim.SetFloat("AttackY", dir.y);
        _anim.SetTrigger(triggerName);
    }

    public void EndFarm()
    {
        if (_anim)
        {
            _anim.ResetTrigger(hoeTriggerName);
            _anim.ResetTrigger(wateringTriggerName);
            _anim.SetFloat("AttackX", 0f);
            _anim.SetFloat("AttackY", 0f);
        }
        if (_playerMove) _playerMove.isAttacking = false;
    }
    public void EndAttack() => EndFarm();

    private Vector2 GetMouseWorldDir()
    {
        var cameraToUse = cam != null ? cam : Camera.main;
        if (cameraToUse == null || gm == null || gm.player == null) return Vector2.right;

        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(cameraToUse.transform.position.z - gm.player.transform.position.z);
        Vector3 world = cameraToUse.ScreenToWorldPoint(mouse);
        Vector2 v = (Vector2)world - (Vector2)gm.player.transform.position;
        if (v.sqrMagnitude < 0.0001f) v = Vector2.right;
        return v.normalized;
    }

    // ====== 기존 로직 유지 (수확/심기 등) ======
    bool TryHarvestCrop()
    {
        Vector3Int pos = seedLand.WorldToCell(gm.player.transform.position);
        if (harvesting.Contains(pos)) return false;
        foreach (var c in crops)
        {
            if (seedLand.GetTile(pos) == c.stages[3])
            {
                harvesting.Add(pos);
                StartCoroutine(HarvestRoutine(pos, c, true));
                return true;
            }
        }
        return false;
    }

    bool TryHarvestSeedForward()
    {
        Vector3 dir = GetFacingDir();
        Vector3 origin = gm.player.transform.position + new Vector3(0, -0.25f);
        int steps = Mathf.CeilToInt(harvestDistance / 0.5f);

        for (int i = 0; i <= steps; i++)
        {
            Vector3Int cell = seedLand.WorldToCell(origin + dir * (i * 0.5f));
            if (harvesting.Contains(cell)) continue;

            TileBase t = seedLand.GetTile(cell);
            if (!t) continue;

            foreach (var c in crops)
            {
                if (t == c.seedRemnant)
                {
                    harvesting.Add(cell);
                    StartCoroutine(HarvestRoutine(cell, c, false));
                    return true;
                }
            }
        }
        return false;
    }

    IEnumerator HarvestRoutine(Vector3Int pos, CropData cd, bool isCrop)
    {
        bool success = false;
        yield return StartCoroutine(ShowProgressCancelable(pos, v => success = v));
        harvesting.Remove(pos);
        if (!success) yield break;

        seedLand.SetTile(pos, null);
        inv.AddItem(isCrop ? cd.cropItemData : cd.seedItemData, 1);
    }

    IEnumerator ShowProgressCancelable(Vector3Int cell, System.Action<bool> done)
    {
        if (!harvestBar)
        {
            yield return new WaitForSeconds(1f);
            done?.Invoke(true);
            yield break;               // ← return true; 금지
        }

        barRoot.gameObject.SetActive(true);
        harvestBar.value = 0f;

        Vector3 world = seedLand.GetCellCenterWorld(cell) + Vector3.up * 0.3f;
        float t = 0f; bool cancel = false;
        while (t < 1f)
        {
            t += Time.deltaTime; 
            harvestBar.value = t;

            Vector3 scr = cam.WorldToScreenPoint(world);
            if (barCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                barRoot.position = scr;
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)barCanvas.transform, scr, cam, out var local);
                barRoot.anchoredPosition = local;
            }

            if (Vector2.Distance(gm.player.transform.position, world) > cancelDistance)
            {
                cancel = true;
                break;
            }
            yield return null;
        }

        barRoot.gameObject.SetActive(false);
        done?.Invoke(!cancel);
        yield break;                    // 안전하게 종료
    }

    bool TryPlantSeed(CropData cd)
    {
        Vector3Int farmCell = farmLand.WorldToCell(gm.player.transform.position);
        if (farmLand.GetTile(farmCell) != wetfarmTile) return false;

        Vector3Int seedCell = seedLand.WorldToCell(farmLand.GetCellCenterWorld(farmCell));
        if (seedLand.GetTile(seedCell) != null) return false;

        seedLand.SetTile(seedCell, cd.stages[0]);
        StartCoroutine(GrowRoutine(seedCell, cd));
        inv.ConsumeSelectedItem(1);
        return true;
    }

    IEnumerator GrowRoutine(Vector3Int cell, CropData cd)
    {
        yield return new WaitForSeconds(5f); if (seedLand) seedLand.SetTile(cell, cd.stages[1]);
        yield return new WaitForSeconds(5f); if (seedLand) seedLand.SetTile(cell, cd.stages[2]);
        yield return new WaitForSeconds(5f); if (seedLand) seedLand.SetTile(cell, cd.stages[3]);
    }

    void BuildFarm(float reqST)
    {
        if (!farmLand) { Debug.LogWarning("farmLand null"); return; }
        if (gm.ST < reqST) { return; }

        Vector3Int pos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(pos);

        if (cur == tilledTile)
        {
            if (IsHoeSelected())
            {
                farmLand.SetTile(pos, farmTile);
                gm.ConsumeSkill(2, reqST);
            }
            return;
        }

        if (cur == farmTile)
        {
            if (IsWateringCanSelected())
            {
                farmLand.SetTile(pos, wetfarmTile);
                gm.ConsumeSkill(2, reqST);
            }
            return;
        }
    }

    public void TryRecoverTilemaps()
    {
        if (!seedLand)
        {
            GameObject go = GameObject.FindGameObjectWithTag("SeedLand");
            if (go) seedLand = go.GetComponent<Tilemap>();
        }
        if (!farmLand)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Farm");
            if (go) farmLand = go.GetComponent<Tilemap>();
        }
    }

    Vector3 GetFacingDir()
    {
        float sign = Mathf.Sign(gm.player.transform.localScale.x);
        return new Vector3(sign, 0f, 0f);
    }

    CropData FindCropBySeedData(ItemData data)
    {
        if (data == null) return null;
        foreach (var c in crops)
            if (c.seedItemData == data) return c;
        return null;
    }

    // === 타입 게이트: 선택 아이템의 itemType 비교 ===
    private bool IsToolTypeSelected(string wantType)
    {
        var it   = inv ? inv.GetSelectedItemData() : null;
        var type = ReadItemType(it);
        bool ok  = !string.IsNullOrEmpty(type) &&
                type.Equals(wantType, System.StringComparison.OrdinalIgnoreCase);

        if (debugToolGate)
        {
            // 👇 문제 줄을 안전하게 분리
            var selName = (it != null ? it.name : "null");
            Debug.Log($"[ToolGate] selected={selName}, type={type}, want={wantType}, ok={ok}");
        }

        return ok;
    }
    private bool IsWateringCanSelected() => IsToolTypeSelected("WateringCan");
    private bool IsHoeSelected()          => IsToolTypeSelected("Hoe");

    // === 리플렉션 헬퍼: ItemData.itemType 읽기 ===
    private static string ReadItemType(object it)
    {
        if (it == null) return null;
        var t = it.GetType();

        var f = t.GetField("itemType") ?? t.GetField("ItemType");
        if (f != null) { var v = f.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }

        var p = t.GetProperty("itemType") ?? t.GetProperty("ItemType");
        if (p != null) { var v = p.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }

        return null;
    }

    // ====== 진행바/바인딩 유틸 (기존) ======
    private static bool IsSceneObject(Component c) => c && c.gameObject.scene.IsValid();

    private Slider FindSliderByNameGlobal(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var sliders = Resources.FindObjectsOfTypeAll<Slider>();
        foreach (var s in sliders)
        {
            if (!IsSceneObject(s)) continue;
            if (string.Equals(s.name, name, System.StringComparison.OrdinalIgnoreCase))
                return s;
        }
        return null;
    }

    private string GetHierarchyPath(Transform t)
    {
        if (!t) return "null";
        var path = t.name;
        while (t.parent) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    private void TryBindHarvestBar()
    {
        if (harvestBar == null)
            harvestBar = FindSliderByNameGlobal(harvestBarAutoName);

        if (harvestBar != null)
        {
            barRoot   = harvestBar.GetComponent<RectTransform>();
            barCanvas = harvestBar.GetComponentInParent<Canvas>();
            cam       = barCanvas ? (barCanvas.worldCamera ?? Camera.main) : Camera.main;

            harvestBar.interactable = false;
            harvestBar.minValue = 0f;
            harvestBar.maxValue = 1f;
            harvestBar.value    = 0f;

            if (barRoot) barRoot.gameObject.SetActive(false);

            Debug.Log($"[Farming][AutoBind] harvestBar = '{harvestBar.name}' ({GetHierarchyPath(harvestBar.transform)})");
        }
        else
        {
            if (!cam) cam = Camera.main;
            Debug.LogWarning($"[Farming][AutoBind] '{harvestBarAutoName}' Slider를 찾지 못했습니다. (진행바 없이 진행)");
        }
    }

    void OnEnable()  { SceneManager.sceneLoaded += _OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= _OnSceneLoaded; }
    private void _OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryBindHarvestBar();
    }
}
