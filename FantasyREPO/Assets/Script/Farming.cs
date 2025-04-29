using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Farming 시스템 (Seed·Tomato 예시) v3 – 전방 감지 수확 & 1×1 개간
///  • Space:
///     1) Stage3 작물   → 1s 후 cropIcon 지급
///     2) Seed 수확(전방 거리) – 물뿌리개·괭이 제외
///     3) 씨앗 심기(빈 젖은 밭)
///     4) 1×1 개간 / 급수
/// </summary>
public class Farming : MonoBehaviour
{
    public static Farming instance;

    /* ───────── 레퍼런스 ───────── */
    private GameManager gm;
    private Inventory inv;

    /* ───────── Harvest 설정 ───────── */
    [Header("수확 감지 범위")]
    [SerializeField] private float harvestDistance = 1.5f;   // 플레이어 전방 감지 거리

    /* ──────── 농지 타일맵 & 타일 ──────── */
    [Header("농지 타일맵 & 타일")]
    [SerializeField] private Tilemap farmLand;
    [SerializeField] private TileBase grassTile;
    [SerializeField] private TileBase tilledTile;
    [SerializeField] private TileBase farmTile;
    [SerializeField] private TileBase wetfarmTile;

    /* ──────── Seed & Crop 데이터 ──────── */
    [System.Serializable]
    public class CropData
    {
        public string name;
        public Sprite seedIcon;
        public Sprite cropIcon;
        public TileBase seedRemnant;
        public TileBase[] stages = new TileBase[4];
    }
    [SerializeField] private CropData[] crops;

    [Header("타일맵 (씨앗·작물 레이어)")]
    [SerializeField] private Tilemap seedLand;

    /* ───────── 초기화 ───────── */
    private void Awake() { if (instance == null) instance = this; else Destroy(gameObject); }
    private void Start() { gm = GameManager.Instance; inv = Inventory.Instance; }

    /* ───────── 입력 ───────── */
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        /* 1) Stage3 작물 수확 */
        if (TryHarvestCrop()) return;

        /* 2) 씨앗 수확 – 물뿌리개(1), 괭이(2) 제외 */
        if (inv.states != 1 && inv.states != 2 && TryHarvestSeedForward()) return;

        /* 3) 씨앗 심기 */
        CropData cd = FindCropBySeed(inv.GetSelectedSprite());
        if (cd != null && TryPlantSeed(cd)) return;

        /* 4) 1×1 개간/급수 */
        BuildFarm(5f);
    }

    /* ───────── Stage3 수확 (발밑) ───────── */
    private bool TryHarvestCrop()
    {
        Vector3Int pos = seedLand.WorldToCell(gm.player.transform.position);
        foreach (var c in crops)
        {
            if (seedLand.GetTile(pos) == c.stages[3])
            {
                StartCoroutine(HarvestCropRoutine(pos, c));
                return true;
            }
        }
        return false;
    }
    private IEnumerator HarvestCropRoutine(Vector3Int pos, CropData cd)
    {
        yield return new WaitForSeconds(1f);
        seedLand.SetTile(pos, null);
        inv.AddItem(cd.cropIcon, 1);
    }

    /* ───────── 전방 씨앗 수확 ───────── */
    private bool TryHarvestSeedForward()
    {
        Vector3 dir = GetFacingDir();
        Vector3 origin = gm.player.transform.position + new Vector3(0, -0.25f, 0);
        int steps = Mathf.CeilToInt(harvestDistance / 0.5f);
        for (int i = 0; i <= steps; i++)
        {
            Vector3 probe = origin + dir * (i * 0.5f);
            Vector3Int cell = seedLand.WorldToCell(probe);
            TileBase t = seedLand.GetTile(cell);
            if (t == null) continue;
            foreach (var c in crops)
            {
                if (t == c.seedRemnant)
                {
                    StartCoroutine(HarvestSeedRoutine(cell, c));
                    return true;
                }
            }
        }
        return false;
    }
    private Vector3 GetFacingDir()
    {
        // 간단히 localScale.x 로 좌우 판정 (2D)
        float sign = Mathf.Sign(gm.player.transform.localScale.x);
        return new Vector3(sign, 0f, 0f);
    }
    private IEnumerator HarvestSeedRoutine(Vector3Int pos, CropData cd)
    {
        yield return new WaitForSeconds(1f);
        seedLand.SetTile(pos, null);
        inv.AddItem(cd.seedIcon, 1);
    }

    /* ───────── 씨앗 심기 & 성장 ───────── */
    private bool TryPlantSeed(CropData cd)
    {
        Vector3Int farmCell = farmLand.WorldToCell(gm.player.transform.position);
        if (farmLand.GetTile(farmCell) != wetfarmTile) return false;

        Vector3 worldCenter = farmLand.GetCellCenterWorld(farmCell);
        Vector3Int seedCell = seedLand.WorldToCell(worldCenter);
        if (seedLand.GetTile(seedCell) != null) return false;

        seedLand.SetTile(seedCell, cd.stages[0]);
        StartCoroutine(GrowRoutine(seedCell, cd));
        inv.ConsumeSelectedItem(1);
        return true;
    }
    private IEnumerator GrowRoutine(Vector3Int cell, CropData cd)
    {
        yield return new WaitForSeconds(5f);
        seedLand.SetTile(cell, cd.stages[1]);
        yield return new WaitForSeconds(5f);
        seedLand.SetTile(cell, cd.stages[2]);
        yield return new WaitForSeconds(5f);
        seedLand.SetTile(cell, cd.stages[3]);
    }

    private CropData FindCropBySeed(Sprite icon)
    {
        foreach (var c in crops)
            if (c.seedIcon == icon) return c;
        return null;
    }

    /* ───────── 1×1 개간 & 급수 ───────── */
    private void BuildFarm(float reqST)
    {
        if (gm.ST < reqST) { Debug.Log("힘 부족"); return; }

        Vector3Int pos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(pos);

        if (cur == grassTile && inv.states == 2)
        {
            farmLand.SetTile(pos, tilledTile);
            gm.ConsumeSkill(3, reqST);
            return;
        }
        if (cur == tilledTile && inv.states == 2)
        {
            farmLand.SetTile(pos, farmTile);
            gm.ConsumeSkill(3, reqST);
            return;
        }
        if (cur == farmTile && inv.states == 1)
        {
            farmLand.SetTile(pos, wetfarmTile);
            gm.ConsumeSkill(3, reqST);
        }
    }
}
