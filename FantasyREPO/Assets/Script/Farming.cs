using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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

    // <<-- 변경: CropData가 이제 Sprite 대신 ItemData를 가집니다.
    [System.Serializable]
    public class CropData
    {
        public string name;
        public ItemData seedItemData;  // 씨앗 아이템의 정보
        public ItemData cropItemData;  // 수확물 아이템의 정보
        public TileBase seedRemnant;
        public TileBase[] stages = new TileBase[4];
    }
    [SerializeField] CropData[] crops;

    [Header("Seed/Crop Tilemap")]
    [SerializeField] protected internal Tilemap seedLand;

    // <<-- 변경: 농기구도 Sprite 대신 ItemData로 구분합니다.
    [Header("Farming Tool Gate (Selected ItemData)")]
    [SerializeField] private ItemData wateringCanItemData;
    [SerializeField] private ItemData hoeItemData;
    [SerializeField] private bool debugToolGate = false;

    void Awake()
    {
        if (instance == null) instance = this; else Destroy(gameObject);
    }

    void Start()
    {
        gm = GameManager.Instance;
        inv = Inventory.Instance;

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
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (!seedLand || !farmLand) { Debug.LogWarning("Tilemap reference missing"); return; }

        if (TryHarvestCrop()) return;
        if (!(IsWateringCanSelected() || IsHoeSelected()) && TryHarvestSeedForward()) return;
        
        // <<-- 변경: 현재 선택된 '아이템 데이터'로 심을 작물을 찾습니다.
        CropData cd = FindCropBySeedData(inv.GetSelectedItemData());
        if (cd != null && TryPlantSeed(cd)) return;

        BuildFarm(5f);
    }

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
        
        // <<-- 변경: 인벤토리에 Sprite가 아닌 ItemData를 추가합니다.
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

    // <<-- 변경: Sprite 대신 ItemData를 받아 해당 씨앗 정보를 찾습니다.
    CropData FindCropBySeedData(ItemData data)
    {
        if (data == null) return null;
        foreach (var c in crops)
        {
            if (c.seedItemData == data) return c;
        }
        return null;
    }

    // <<-- 변경: Sprite 대신 ItemData를 받아 현재 선택된 도구와 비교합니다.
    private bool IsToolSelected(ItemData toolData)
    {
        if (inv == null || inv.IsSelectedEmpty()) return false;

        var selectedItem = inv.GetSelectedItemData(); // 현재 선택된 '아이템 데이터'
        bool ok = (selectedItem != null && selectedItem == toolData);

        if (debugToolGate)
            Debug.Log($"[ToolGate] selectedItem={(selectedItem?.name)}, tool={(toolData?.name)}, match={ok}");
        
        return ok;
    }

    private bool IsWateringCanSelected() => IsToolSelected(wateringCanItemData);
    private bool IsHoeSelected() => IsToolSelected(hoeItemData);
}