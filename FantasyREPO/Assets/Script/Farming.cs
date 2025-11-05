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

    [Header("Farming Tool Gate (Selected ItemData)")]
    [SerializeField] private ItemData wateringCanItemData;
    [SerializeField] private ItemData hoeItemData;
    [SerializeField] private bool debugToolGate = false;

    // ================================
    // Pick.cs 구조 적용 (개간/급수 전용)
    // ================================
    [Header("개간/급수 애니메이션 & 쿨타임")]
    [SerializeField] private string hoeTriggerName = "Hoe";           // 개간 트리거
    [SerializeField] private string wateringTriggerName = "Watering"; // 급수 트리거
    [SerializeField] private float farmCoolTime = 0.5f;               // 좌클릭 쿨타임
    private float farmCurTime;

    private Animator _anim;
    private PlayerMove _playerMove;
    // ================================

    void Awake()
    {
        if (instance == null) instance = this; else Destroy(gameObject);
    }

    void Start()
    {
        gm = GameManager.Instance;
        inv = Inventory.Instance;
        TryBindHarvestBar();

        // 애니메이터/플레이어 상태 참조
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

        if (harvestBar)
        {
            barRoot = harvestBar.GetComponent<RectTransform>();
            barCanvas = harvestBar.GetComponentInParent<Canvas>();
            cam = barCanvas ? (barCanvas.worldCamera ?? Camera.main) : Camera.main;
            harvestBar.interactable = false;
            harvestBar.minValue = 0f;
            harvestBar.maxValue = 1f;
            harvestBar.value = 0f;
            barRoot.gameObject.SetActive(false);
        }
        else
        {
            cam = Camera.main;
        }
    }

    void Update()
    {
        // 개간/급수 쿨타임 감소
        if (farmCurTime > 0f) farmCurTime -= Time.deltaTime;

        // 좌클릭: 개간/급수만 처리 (Pick.cs 구조)
        if (Input.GetMouseButtonDown(0) &&
            farmCurTime <= 0f &&
            _playerMove != null && !_playerMove.isAttacking &&
            (IsHoeSelected() || IsWateringCanSelected()))
        {
            if (TryFarmAction(5f))   // 필요 ST는 기존 BuildFarm과 동일하게 5f 사용
                return;
        }

        // Space: 기존 수확/심기/앞수확/BuildFarm 유지
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (!seedLand || !farmLand) { Debug.LogWarning("Tilemap reference missing"); return; }

        if (TryHarvestCrop()) return;
        if (!(IsWateringCanSelected() || IsHoeSelected()) && TryHarvestSeedForward()) return;

        CropData cd = FindCropBySeedData(inv.GetSelectedItemData());
        if (cd != null && TryPlantSeed(cd)) return;

        // 스페이스로 하는 기존 개간/급수 로직도 유지
        BuildFarm(5f);
    }

    // Pick.cs 스타일 개간/급수 실행부
    private bool TryFarmAction(float reqST)
    {
        if (!farmLand || gm == null || gm.player == null) return false;
        if (gm.ST < reqST) return false;

        Vector3Int pos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(pos);

        // 개간: tilled -> farm (호미 선택 + "Hoe" 트리거)
        if (cur == tilledTile && IsHoeSelected())
        {
            TriggerFarmAnimTowardMouse(hoeTriggerName);
            farmLand.SetTile(pos, farmTile);
            gm.ConsumeSkill(2, reqST);

            farmCurTime = farmCoolTime;
            _playerMove.isAttacking = true;
            return true;
        }

        // 급수: farm -> wetfarm (물뿌리개 선택 + "Watering" 트리거)
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

    // 애니메이션 이벤트(EndAttack)에서 호출될 함수
    public void EndFarm()
    {
        if (_anim)
        {
            // 두 트리거 모두 리셋 (안전)
            _anim.ResetTrigger(hoeTriggerName);
            _anim.ResetTrigger(wateringTriggerName);
            _anim.SetFloat("AttackX", 0f);
            _anim.SetFloat("AttackY", 0f);
        }
        if (_playerMove) _playerMove.isAttacking = false;
    }

    // Pick.cs와 동일하게 애니메이션 이벤트명이 EndAttack이라면 이것도 잡아줌
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

    // ====== 아래부터는 기존 로직 그대로 ======

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
            yield break;
        }

        barRoot.gameObject.SetActive(true);
        harvestBar.value = 0f;

        Vector3 world = seedLand.GetCellCenterWorld(cell) + Vector3.up * 0.3f;
        float t = 0f; bool cancel = false;
        while (t < 1f)
        {
            t += Time.deltaTime; harvestBar.value = t;

            Vector3 scr = cam.WorldToScreenPoint(world);
            if (barCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                barRoot.position = scr;
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)barCanvas.transform, scr, cam, out var local);
                barRoot.anchoredPosition = local;
            }

            if (Vector2.Distance(gm.player.transform.position, world) > cancelDistance) { cancel = true; break; }
            yield return null;
        }
        barRoot.gameObject.SetActive(false);
        done?.Invoke(!cancel);
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
        {
            if (c.seedItemData == data) return c;
        }
        return null;
    }

    private bool IsToolSelected(ItemData toolData)
    {
        if (inv == null || inv.IsSelectedEmpty()) return false;

        var selectedItem = inv.GetSelectedItemData();
        bool ok = (selectedItem != null && selectedItem == toolData);

        if (debugToolGate)
            Debug.Log($"[ToolGate] selectedItem={(selectedItem?.name)}, tool={(toolData?.name)}, match={ok}");

        return ok;
    }

    // 씬/Don’tDestroyOnLoad 객체만 허용(프리팹 에셋 제외)
    private static bool IsSceneObject(Component c) => c && c.gameObject.scene.IsValid();

    // 전역에서 이름으로 Slider 찾기(활/비활성 + DDOL 포함)
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

    // 이름으로 HarvestBar 자동 바인딩 + 초기화
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
            if (!cam) cam = Camera.main; // 진행바 없어도 동작하도록 카메라 확보
            Debug.LogWarning($"[Farming][AutoBind] '{harvestBarAutoName}' Slider를 찾지 못했습니다. (진행바 없이 진행)");
        }
    }

    void OnEnable()  { SceneManager.sceneLoaded += _OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= _OnSceneLoaded; }
    private void _OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryBindHarvestBar();
    }
    
    private bool IsWateringCanSelected() => IsToolSelected(wateringCanItemData);
    private bool IsHoeSelected() => IsToolSelected(hoeItemData);
}
