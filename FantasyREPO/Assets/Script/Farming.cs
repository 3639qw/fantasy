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
    [SerializeField] private Tilemap farmLand;       // 모든 농지 타일맵
    [SerializeField] private TileBase grassTile;
    [SerializeField] private TileBase tilledTile;
    [SerializeField] private TileBase farmTile;
    [SerializeField] private TileBase wetfarmTile;

    /* ──────────── Seed 수확 설정 ──────────── */
    [Header("Seed 수확 설정")]
    [SerializeField] private Tilemap seedLand;
    [SerializeField] private TileBase seedTile;
    [SerializeField] private Sprite foodIcon;      // Food_Icons_NO_Outline_27

    /* ──────────── 작물 심기 설정 ──────────── */
    [Header("Crop 심기 설정")]
    [SerializeField] private TileBase cropsTile;     // Crops_2

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

    /* ──────────── 입력 처리 ──────────── */
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        // 1) Food 아이콘 슬롯이 선택돼 있으면 작물 심기
        if (IsFoodIconSelected() && TryPlantCrop()) return;

        // 2) Seed 수확 (선택된 슬롯이 빈 손일 때)
        if (IsSelectedSlotEmpty() && TryHarvestSeed()) return;

        // 3) 농지 개간 / 급수
        BuildFarm(5f);
    }

    /* ──────────── 작물 심기 ──────────── */
    private bool TryPlantCrop()
    {
        Vector3 worldPos = gm.player.transform.position + new Vector3(0f, -0.25f, 0f);
        Vector3Int farmPos = farmLand.WorldToCell(worldPos);

        // 젖은 밭 확인
        TileBase cur = farmLand.GetTile(farmPos);
        bool isWet = cur == wetfarmTile || (cur && wetfarmTile && cur.name == wetfarmTile.name);
        if (!isWet) return false;

        // 동일 월드 기준 Seed 셀 좌표 계산
        Vector3 worldCenter = farmLand.GetCellCenterWorld(farmPos);
        Vector3Int seedPos = seedLand.WorldToCell(worldCenter);

        seedLand.SetTile(seedPos, cropsTile);
        seedLand.RefreshTile(seedPos);
        return true;
    }

    private bool IsFoodIconSelected()
    {
        Sprite sel = GetSelectedSlotSprite();
        return sel != null && (sel == foodIcon || sel.name == foodIcon.name);
    }

    private bool IsSelectedSlotEmpty()
    {
        return GetSelectedSlotSprite() == null;
    }

    private Sprite GetSelectedSlotSprite()
    {
        return inv.states switch
        {
            1 => inv.hand1?.sprite,
            2 => inv.hand2?.sprite,
            3 => inv.hand3?.sprite,
            4 => inv.hand4?.sprite,
            5 => inv.hand5?.sprite,
            _ => null
        };
    }

    private int GetFoodIconSlotIndex()
    {
        if (inv.hand1 && inv.hand1.sprite == foodIcon) return 1;
        if (inv.hand2 && inv.hand2.sprite == foodIcon) return 2;
        if (inv.hand3 && inv.hand3.sprite == foodIcon) return 3;
        if (inv.hand4 && inv.hand4.sprite == foodIcon) return 4;
        if (inv.hand5 && inv.hand5.sprite == foodIcon) return 5;
        return 0;
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

        bool ok = inv.AddItemToFirstEmptySlot(foodIcon);
        if (!ok)
        {
            Debug.Log("슬롯이 가득 차 Seed 아이템을 얻지 못했습니다");
            yield break;
        }

        // Food 아이콘이 들어간 슬롯을 자동 선택
        int idx = GetFoodIconSlotIndex();
        if (idx > 0)
        {
            inv.states = idx;
            HighlightSlot(idx);
        }
    }

    private void HighlightSlot(int idx)
    {
        SetAlpha(inv.hand1, idx == 1);
        SetAlpha(inv.hand2, idx == 2);
        SetAlpha(inv.hand3, idx == 3);
        SetAlpha(inv.hand4, idx == 4);
        SetAlpha(inv.hand5, idx == 5);
    }

    private void SetAlpha(UnityEngine.UI.Image img, bool selected)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = selected ? 1f : 60f / 255f;
        img.color = c;
    }

    /* ──────────── 농지 개간 & 업그레이드 ──────────── */
    private void BuildFarm(float reqST)
    {
        Vector3Int tilePos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(tilePos);

        if (gm.ST < reqST) { Debug.Log("힘이 부족합니다"); return; }

        // 잔디 → 1차 농지  (슬롯2: 괭이)
        if (cur == grassTile && inv.states == 2)
        {
            farmLand.SetTile(tilePos, tilledTile);
            gm.ConsumeSkill(3, reqST);
            Debug.Log("1차 농지화 완료");
            return;
        }

        // 1차 농지 (3x3) → 2차 농지  (슬롯2: 괭이)
        if (cur == tilledTile && inv.states == 2)
        {
            if (Is3x3All(tilePos, tilledTile))
            {
                Replace3x3(tilePos, farmTile);
                gm.ConsumeSkill(3, reqST);
                Debug.Log("2차 농지화 완료");
            }
            return;
        }

        // 2차 농지 → 급수  (슬롯1: 물뿌리개)
        if (cur == farmTile && inv.states == 1)
        {
            Replace3x3(tilePos, wetfarmTile);
            gm.ConsumeSkill(3, reqST);
            Debug.Log("급수 완료");
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
