using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class Pick : MonoBehaviour
{
    [Header("탐지/상호작용")]
    public float interactRange = 1.8f;
    public string rockTag = "Rock";

    [Header("쿨타임")]
    public float coolTime = 0.5f;
    private float curTime;

    // (하위 호환용) 인스펙터에 남아있는 필드지만 현재 로직에서는 사용하지 않습니다.
    [Header("도구 선택 게이트(과거 버전 호환용, 현재 미사용)")]
    [SerializeField] private ItemData pickItemData;

    private Animator _anim;
    private PlayerMove _playerMove;
    private Camera _cam;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _playerMove = GetComponent<PlayerMove>();
        _cam = Camera.main;
    }

    void Update()
    {
        if (curTime > 0f) curTime -= Time.deltaTime;

        // _playerMove가 없으면 통과, 있으면 isAttacking일 때만 차단
        if (Input.GetMouseButtonDown(0) &&
            curTime <= 0f &&
            (_playerMove == null || !_playerMove.isAttacking) &&
            IsPickSelected())
        {
            TryMine();
        }
    }

    // ─────────────────────────────────────────────
    // 채광 시도 (애니메이션 유지 + Attack Power 반영)
    // ─────────────────────────────────────────────
    private void TryMine()
    {
        // 현재 선택된 아이템이 Pick인지 확인하고 Attack Power 가져오기
        int toolPower = GetSelectedToolPower_Pick();
        if (toolPower <= 0) return;

        var hits = Physics2D.OverlapCircleAll(transform.position, interactRange);
        if (hits == null || hits.Length == 0) return;

        var visited = new HashSet<GameObject>();

        foreach (var col in hits)
        {
            if (!col || !col.CompareTag(rockTag)) continue;

            var root = col.transform.root.gameObject;
            if (visited.Contains(root)) continue;

            // ★ 원래 애니메이션 코드 유지
            var dir = GetMouseWorldDir();
            _anim.SetFloat("AttackX", dir.x);
            _anim.SetFloat("AttackY", dir.y);
            _anim.SetTrigger("Pick");

            curTime = coolTime;
            if (_playerMove) _playerMove.isAttacking = true;

            // ✅ HP 차감: MineOnce() 직접 호출 금지, 반드시 Hit(power)
            var mineable = col.GetComponentInParent<MineableRock>();
            if (mineable) mineable.Hit(toolPower);

            visited.Add(root);
            break; // 한 번에 하나만
        }
    }

    // 애니메이션 이벤트용(기존 유지)
    public void EndPick()
    {
        _anim.ResetTrigger("Pick");
        _anim.SetFloat("AttackX", 0f);
        _anim.SetFloat("AttackY", 0f);
        if (_playerMove) _playerMove.isAttacking = false;
    }
    public void EndAttack() => EndPick();

    // ─────────────────────────────────────────────
    // 선택 아이템이 곡괭이인지 판정 (Item Type == "Pick")
    // ─────────────────────────────────────────────
    private bool IsPickSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null || inv.IsSelectedEmpty()) return false;

        var it = inv.GetSelectedItemData();
        var type = ReadItemType(it);
        return !string.IsNullOrEmpty(type) &&
               type.Equals("Pick", System.StringComparison.OrdinalIgnoreCase);
    }

    // 현재 마우스 방향 계산(기존 유지)
    private Vector2 GetMouseWorldDir()
    {
        var mouse = Input.mousePosition;
        if (_cam == null) _cam = Camera.main;
        if (_cam)
        {
            mouse.z = Mathf.Abs(_cam.transform.position.z - transform.position.z);
            var world = _cam.ScreenToWorldPoint(mouse);
            var v = (Vector2)world - (Vector2)transform.position;
            if (v.sqrMagnitude < 0.0001f) v = Vector2.right;
            return v.normalized;
        }
        return Vector2.right;
    }

    // ─────────────────────────────────────────────
    // 선택 아이템에서 Attack Power 읽기 (없으면 1)
    // ─────────────────────────────────────────────
    int GetSelectedToolPower_Pick()
    {
        var inv = Inventory.Instance;
        var it  = inv ? inv.GetSelectedItemData() : null;
        if (it == null) return 0;

        var type = ReadItemType(it);
        if (string.IsNullOrEmpty(type) || !type.Equals("Pick", System.StringComparison.OrdinalIgnoreCase))
            return 0;

        int ap = ReadAttackPower(it);
        return Mathf.Max(1, ap); // 최소 1
    }

    // ─────────────────────────────────────────────
    // ItemData 리플렉션 유틸
    // ─────────────────────────────────────────────
    static string ReadItemType(ItemData it)
    {
        if (it == null) return null;
        var t = it.GetType();

        var f = t.GetField("itemType") ?? t.GetField("ItemType");
        if (f != null) { var v = f.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }

        var p = t.GetProperty("itemType") ?? t.GetProperty("ItemType");
        if (p != null) { var v = p.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }

        return null;
    }

    // 다양한 이름/타입을 폭넓게 지원 + 이름 기반 폴백(Copper=1, Iron=2)
    static int ReadAttackPower(ItemData it)
    {
        if (it == null) return 1;
        var t = it.GetType();

        string[] names = {
            "attackPower","AttackPower",
            "miningPower","MiningPower",
            "toolPower","ToolPower",
            "power","Power",
            "atk","Atk","ATK",
            "damage","Damage"
        };

        object val = null;
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null) { val = f.GetValue(it); break; }
            var p = t.GetProperty(n);
            if (p != null) { val = p.GetValue(it, null); break; }
        }

        int result = 0;
        if (val is int i) result = i;
        else if (val is float f) result = Mathf.RoundToInt(f);
        else if (val is double d) result = Mathf.RoundToInt((float)d);
        else if (val is string s && int.TryParse(s, out var si)) result = si;

        if (result <= 0)
        {
            var nm = it.name ?? string.Empty;
            if (nm.IndexOf("iron", System.StringComparison.OrdinalIgnoreCase) >= 0) result = 2;
            else if (nm.IndexOf("copper", System.StringComparison.OrdinalIgnoreCase) >= 0) result = 1;
            else result = 1;
        }
        return result;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
