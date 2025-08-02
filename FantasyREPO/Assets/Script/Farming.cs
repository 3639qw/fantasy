using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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
    GameManager gm; Inventory inv;

    /* ───────── UI (optional) ───────── */
    [Header("채집 진행바 (없어도 동작)")]
    [SerializeField] Slider harvestBar;
    RectTransform barRoot; Canvas barCanvas; Camera cam;

    /* ───────── 취소 거리 ───────── */
    [Header("Cancel Distance (이상 멀어지면 취소)")]
    [SerializeField] float cancelDistance = 1.2f;

    /* ───────── Harvest 설정 ───────── */
    [Header("수확 감지 범위")]
    [SerializeField] float harvestDistance = 1.5f;

    /* ───────── 중복 수확 보호 ───────── */
    HashSet<Vector3Int> harvesting = new HashSet<Vector3Int>();

    /* ──────── 농지 타일맵 & 타일 ──────── */
    [Header("Farm Tilemap & Tiles")]
    [SerializeField] protected internal Tilemap farmLand;  // 외부(Serializer) 접근 허용
    [SerializeField] TileBase grassTile, tilledTile, farmTile, wetfarmTile;

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
    [SerializeField] CropData[] crops;

    [Header("Seed/Crop Tilemap")]
    [SerializeField] protected internal Tilemap seedLand;  // 외부(Serializer) 접근 허용

    /* ───────── init ───────── */
    void Awake()
    {
        if (instance == null) instance = this; else Destroy(gameObject);
    }

    void Start()
    {
        gm = GameManager.Instance;
        inv = Inventory.Instance;

        // 슬라이더 초기화 (있으면)
        if (harvestBar)
        {
            barRoot   = harvestBar.GetComponent<RectTransform>();
            barCanvas = harvestBar.GetComponentInParent<Canvas>();
            cam       = barCanvas ? (barCanvas.worldCamera ?? Camera.main) : Camera.main;

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
        if (!seedLand || !farmLand) { Debug.LogWarning("Tilemap reference missing"); return; }

        if (TryHarvestCrop()) return;                                        // 1
        if (inv.states != 1 && inv.states != 2 && TryHarvestSeedForward()) return; // 2

        CropData cd = FindCropBySeed(inv.GetSelectedSprite());               // 3
        if (cd != null && TryPlantSeed(cd)) return;

        BuildFarm(5f);                                                       // 4
    }

    /* ───────── 1) Stage‑3 작물 수확 ───────── */
    bool TryHarvestCrop()
    {
        Vector3Int pos = seedLand.WorldToCell(gm.player.transform.position);
        if (harvesting.Contains(pos)) return false;          // 중복 보호

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

    /* ───────── 2) 씨앗 잔재 수확 (전방 감지) ───────── */
    bool TryHarvestSeedForward()
    {
        Vector3 dir    = GetFacingDir();
        Vector3 origin = gm.player.transform.position + new Vector3(0, -0.25f);
        int steps      = Mathf.CeilToInt(harvestDistance / 0.5f);

        for (int i = 0; i <= steps; i++)
        {
            Vector3Int cell = seedLand.WorldToCell(origin + dir * (i * 0.5f));
            if (harvesting.Contains(cell)) continue;         // 중복 보호

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

    /* ───────── 수확 코루틴 (취소 가능) ───────── */
    IEnumerator HarvestRoutine(Vector3Int pos, CropData cd, bool isCrop)
    {
        bool success = false;
        yield return StartCoroutine(ShowProgressCancelable(pos, v => success = v));
        harvesting.Remove(pos);
        if (!success) yield break;

        seedLand.SetTile(pos, null);
        inv.AddItem(isCrop ? cd.cropIcon : cd.seedIcon, 1);
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

    /* ───────── 3) 씨앗 심기 & 성장 ───────── */
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

    /* ───────── 4) 1×1 개간 / 급수 ───────── */
    void BuildFarm(float reqST)
    {
        if (!farmLand) { Debug.LogWarning("farmLand null"); return; }
        if (gm.ST < reqST) { Debug.Log("힘 부족"); return; }

        Vector3Int pos = farmLand.WorldToCell(gm.player.transform.position);
        TileBase cur = farmLand.GetTile(pos);        // 잔디 → 흙 단계는 제거 (grassTile → tilledTile 생략)
        if (cur == tilledTile && inv.states == 2)
        {
            farmLand.SetTile(pos, farmTile);
            gm.ConsumeSkill(2, reqST);
            return;
        }
        if (cur == farmTile && inv.states == 1)
        {
            farmLand.SetTile(pos, wetfarmTile);
            gm.ConsumeSkill(2, reqST);
        }
    }

        /* ───────── Tilemap 복구 (호환용) ───────── */
    /// <summary>
    /// GameManager 등 기존 코드 호환을 위해 남겨둔 메서드.
    /// Scene 내 태그 "SeedLand", "Farm" 오브젝트를 찾아 seedLand / farmLand 에 할당한다.
    /// PLUS 버전에서는 에디터에서 직접 연결하는 것을 권장하지만, 없어도 Null 오류가 나지 않도록 유지.
    /// </summary>
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

    /* ───────── helper ───────── */
    Vector3 GetFacingDir()
    {
        float sign = Mathf.Sign(gm.player.transform.localScale.x);
        return new Vector3(sign, 0f, 0f);
    }

    CropData FindCropBySeed(Sprite icon)
    {
        foreach (var c in crops) if (c.seedIcon == icon) return c;
        return null;
    }
}