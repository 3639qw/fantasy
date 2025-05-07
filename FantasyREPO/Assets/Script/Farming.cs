using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Farming : MonoBehaviour
{
    public static Farming instance;

    /* ──────────── 레퍼런스 ──────────── */
    private GameManager gm;
    private Inventory inv;

    /* ──────────── 농지 타일맵 & 타일 ──────────── */
    [Header("농지 타일맵 & 타일")]
    [SerializeField] private Tilemap farmLand;
    [SerializeField] private TileBase grassTile;
    [SerializeField] private TileBase tilledTile;
    [SerializeField] private TileBase farmTile;
    [SerializeField] private TileBase wetfarmTile;

    [Header("Seed & Crop 타일맵")]
    [SerializeField] private Tilemap seedLand;
    [SerializeField] private TileBase seedTile;
    [SerializeField] private TileBase cropsTile;
    [SerializeField] private Sprite foodIcon;

    /* ──────────── 초기화 ──────────── */
    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        gm = GameManager.Instance;
        inv = Inventory.Instance;
    }

    /* ──────────── 매 프레임 입력 처리 ──────────── */
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        // 1) Food 아이콘 슬롯 + 젖은 밭 → 씨앗 심기 & 수량 -1
        if (inv.GetSelectedSprite() == foodIcon && TryPlantCrop())
        {
            inv.ConsumeSelectedItem(1);
            return;
        }

        // 2) 빈 손이면 Seed 수확
        if (inv.IsSelectedEmpty() && TryHarvestSeed()) return;

        // 3) 그 외 개간/급수 로직
        BuildFarm(5f);
    }

    /* ──────────── 씨앗 심기 ──────────── */
    private bool TryPlantCrop()
    {
        Vector3 worldPos = gm.player.transform.position + new Vector3(0f, -0.25f, 0f);
        Vector3Int farmCell = farmLand.WorldToCell(worldPos);

        // 젖은 밭 여부 확인
        TileBase cur = farmLand.GetTile(farmCell);
        bool isWet = cur == wetfarmTile || (cur && wetfarmTile && cur.name == wetfarmTile.name);
        if (!isWet) return false;

        // farm 셀 → worldCenter → seed 셀 변환
        Vector3 worldCenter = farmLand.GetCellCenterWorld(farmCell);
        Vector3Int seedCell = seedLand.WorldToCell(worldCenter);

        seedLand.SetTile(seedCell, cropsTile);
        seedLand.RefreshTile(seedCell);
        seedLand.CompressBounds();
        Debug.Log("✅ 씨앗 심기 & 수량 -1");
        return true;
    }

    /* ──────────── Seed 수확 ──────────── */
    private bool TryHarvestSeed()
    {
        Vector3Int pos = seedLand.WorldToCell(gm.player.transform.position);
        if (seedLand.GetTile(pos) != seedTile) return false;
        StartCoroutine(HarvestSeedCoroutine(pos));
        return true;
    }

    private IEnumerator HarvestSeedCoroutine(Vector3Int pos)
    {
        yield return new WaitForSeconds(1f);
        seedLand.SetTile(pos, null);
        inv.AddItem(foodIcon, 1);
    }

    /* ──────────── 농지: 개간 / 업그레이드 / 급수 ──────────── */
    private void BuildFarm(float reqST)
    {
        Vector3Int pos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(pos);

        if (gm.ST < reqST) { Debug.Log("힘 부족"); return; }

        // 1) 잔디 → 1차 농지 (슬롯2)
        if (cur == grassTile && inv.states == 2)
        {
            farmLand.SetTile(pos, tilledTile);
            gm.ConsumeSkill(3, reqST);
            return;
        }

        // 2) 1차 농지 3x3 → 2차 농지 (슬롯2)
        if (cur == tilledTile && inv.states == 2 && Is3x3All(pos, tilledTile))
        {
            Replace3x3(pos, farmTile);
            gm.ConsumeSkill(3, reqST);
            return;
        }

        // 3) 2차 농지 3x3 → 젖은 밭 (슬롯1)
        if (cur == farmTile && inv.states == 1)
        {
            Replace3x3(pos, wetfarmTile);
            gm.ConsumeSkill(3, reqST);
        }
    }

    /* ──────────── 보조 메서드 ──────────── */
    private bool Is3x3All(Vector3Int center, TileBase target)
    {
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (farmLand.GetTile(center + new Vector3Int(x, y, 0)) != target)
                    return false;
        return true;
    }

    private void Replace3x3(Vector3Int center, TileBase tile)
    {
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                farmLand.SetTile(center + new Vector3Int(x, y, 0), tile);
    }
}
