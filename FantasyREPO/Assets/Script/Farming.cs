using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// 파밍 시스템 v4-C (취소 가능한 수확)
/// ● Space 키 동작  
///   1) 성장 완료 작물(Stage-3) → 1 초 진행바 후 crop 아이템 지급  
///   2) 씨앗 잔재(전방 감지) 수확 – 물뿌리개(1) & 괭이(2) 사용 시 제외  
///   3) 젖은 밭에 선택된 씨앗 심기 → 3단계 성장  
///   4) 1×1 개간 / 물주기 / 잔디→1차→2차→젖은밭 업그레이드  
/// ● 진행 중 플레이어가 <c>cancelDistance</c> 이상 멀어지면 즉시 취소  
/// </summary>
public class Farming : MonoBehaviour
{
    public static Farming instance;

    /* ───────── refs ───────── */
    GameManager gm; Inventory inv;

    /* ───────── UI ───────── */
    [Header("채집 슬라이더")]
    [SerializeField] Slider harvestBar;
    RectTransform barRoot; Canvas barCanvas; Camera cam;

    /* ───────── cancel range ───────── */
    [Header("Cancel Distance (이 거리 이상 멀어지면 취소)")]
    [SerializeField] float cancelDistance = 1.2f;

    /* ───────── 중복 수확 보호 ───────── */
    HashSet<Vector3Int> harvesting = new HashSet<Vector3Int>();

    /* ───────── seed forward range ───────── */
    [Header("채집 감지 범위")]
    [SerializeField] float harvestDistance = 1.5f;

    /* ───────── farm tiles ───────── */
    [Header("Farm Tilemap & Tiles")]
    [SerializeField] Tilemap farmLand;
    [SerializeField] TileBase grassTile, tilledTile, farmTile, wetfarmTile;

    /* ───────── seed/crop data ───────── */
    [System.Serializable]
    public class CropData
    {
        public string name;
        public Sprite seedIcon;
        public Sprite cropIcon;
        public TileBase seedRemnant;
        public TileBase[] stages = new TileBase[4]; // 0-3
    }
    [SerializeField] CropData[] crops;

    [Header("Seed/Crop Tilemap")]
    [SerializeField] Tilemap seedLand;

    /* ───────── init ───────── */
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        gm = GameManager.Instance;
        inv = Inventory.Instance;

        if (harvestBar)
        {
            barRoot = harvestBar.GetComponent<RectTransform>();
            barCanvas = harvestBar.GetComponentInParent<Canvas>();
            cam = barCanvas ? (barCanvas.worldCamera ?? Camera.main)
                                  : Camera.main;

            harvestBar.interactable = false;
            harvestBar.minValue = 0f;
            harvestBar.maxValue = 1f;
            harvestBar.value = 0f;
            barRoot.gameObject.SetActive(false);
        }
    }

    /* ───────── 입력 ───────── */
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        if (TryHarvestCrop()) return; // 1
        if (inv.states != 1 && inv.states != 2                // 2
            && TryHarvestSeedForward()) return;

        CropData cd = FindCropBySeed(inv.GetSelectedSprite()); // 3
        if (cd != null && TryPlantSeed(cd)) return;

        BuildFarm(5f);                                         // 4
    }

    /* ───────── 1) 작물 수확 ───────── */
    bool TryHarvestCrop()
    {
        Vector3Int pos = seedLand.WorldToCell(gm.player.transform.position);
        if (harvesting.Contains(pos)) return false;

        foreach (var c in crops)
        {
            if (seedLand.GetTile(pos) == c.stages[3])
            {
                harvesting.Add(pos);
                StartCoroutine(HarvestRoutine(pos, c, isCrop: true));
                return true;
            }
        }
        return false;
    }

    /* ───────── 2) 씨앗 잔재 수확 ───────── */
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
                    StartCoroutine(HarvestRoutine(cell, c, isCrop: false));
                    return true;
                }
            }
        }
        return false;
    }

    /* ───────── 수확 코루틴 ───────── */
    IEnumerator HarvestRoutine(Vector3Int pos, CropData cd, bool isCrop)
    {
        bool success = false;

        // 진행바 코루틴 실행 → 콜백으로 결과 전달
        yield return StartCoroutine(ShowProgressCancelable(pos, v => success = v));

        harvesting.Remove(pos);              // 중복 수확 해제

        if (!success) yield break;           // 취소되면 아무것도 하지 않음

        // 성공: 타일 제거 + 아이템 지급
        seedLand.SetTile(pos, null);
        inv.AddItem(isCrop ? cd.cropIcon : cd.seedIcon, 1);
    }

    /* ───────── 3) 씨앗 심기 & 성장 ───────── */
    bool TryPlantSeed(CropData cd)
    {
        Vector3Int farmCell = farmLand.WorldToCell(gm.player.transform.position);
        if (farmLand.GetTile(farmCell) != wetfarmTile) return false;

        Vector3Int seedCell = seedLand.WorldToCell(
            farmLand.GetCellCenterWorld(farmCell));
        if (seedLand.GetTile(seedCell) != null) return false;

        seedLand.SetTile(seedCell, cd.stages[0]);
        StartCoroutine(GrowRoutine(seedCell, cd));
        inv.ConsumeSelectedItem(1);
        return true;
    }

    IEnumerator GrowRoutine(Vector3Int cell, CropData cd)
    {
        yield return new WaitForSeconds(5f); seedLand.SetTile(cell, cd.stages[1]);
        yield return new WaitForSeconds(5f); seedLand.SetTile(cell, cd.stages[2]);
        yield return new WaitForSeconds(5f); seedLand.SetTile(cell, cd.stages[3]);
    }

    CropData FindCropBySeed(Sprite icon)
    {
        foreach (var c in crops) if (c.seedIcon == icon) return c;
        return null;
    }

    /* ───────── 4) 1×1 개간 / 물주기 ───────── */
    void BuildFarm(float reqST)
    {
        if (gm.ST < reqST) { Debug.Log("힘 부족"); return; }

        Vector3Int pos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(pos);

        if (cur == grassTile && inv.states == 2)
        { farmLand.SetTile(pos, tilledTile); gm.ConsumeSkill(3, reqST); return; }

        if (cur == tilledTile && inv.states == 2)
        { farmLand.SetTile(pos, farmTile); gm.ConsumeSkill(3, reqST); return; }

        if (cur == farmTile && inv.states == 1)
        { farmLand.SetTile(pos, wetfarmTile); gm.ConsumeSkill(3, reqST); }
    }

    /* ───────── helper ───────── */
    Vector3 GetFacingDir()
    {
        float s = Mathf.Sign(gm.player.transform.localScale.x);
        return new Vector3(s, 0);
    }

    /* ───────── 진행바 코루틴 (취소 가능) ───────── */
    IEnumerator ShowProgressCancelable(Vector3Int targetCell, System.Action<bool> done)
    {
        if (!harvestBar)
        {
            yield return new WaitForSeconds(1f);
            done?.Invoke(true);
            yield break;
        }

        barRoot.gameObject.SetActive(true);
        harvestBar.value = 0f;

        Vector3 world = seedLand.GetCellCenterWorld(targetCell);

        float t = 0f;
        bool cancelled = false;
        while (t < 1f)
        {
            t += Time.deltaTime;
            harvestBar.value = t;

            // 위치 갱신
            Vector3 scr = cam.WorldToScreenPoint(world + Vector3.up * 0.3f);
            if (barCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                barRoot.position = scr;
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    barCanvas.transform as RectTransform, scr, cam, out var local);
                barRoot.anchoredPosition = local;
            }

            // 거리 체크 → 취소
            float dist = Vector2.Distance(
                (Vector2)gm.player.transform.position,
                new Vector2(world.x, world.y));

            if (dist > cancelDistance) { cancelled = true; break; }
            yield return null;
        }

        barRoot.gameObject.SetActive(false);
        done?.Invoke(!cancelled);
    }
}
