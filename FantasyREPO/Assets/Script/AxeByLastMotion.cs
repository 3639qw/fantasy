using UnityEngine;

public class AxeByLastMotion : MonoBehaviour
{
    [Header("Chop Settings")]
    public float interactRange = 2f;       // 플레이어 → 나무 콜라이더 표면 거리 허용치
    public float searchPadding = 1f;       // 탐색 여유 반경
    public string treeTag = "Tree";
    public float cooldown = 0.35f;

    [Header("Inventory Gate (optional)")]
    public bool requireAxeSelected = false;
    public ItemData axeItemData;           // 특정 아이템 강제하고 싶을 때만 지정

    [Header("Targeting")]
    public bool preferTreeUnderCursor = true;

    [Header("Animation")]
    [SerializeField] private string axeTriggerName = "Axe";
    private Animator _anim;
    private PlayerMove _playerMove;
    private Camera _cam;
    float _cool;

    void Start()
    {
        var gm = GameManager.Instance;
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
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cool > 0f) _cool -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0) &&
            _cool <= 0f &&
            (_playerMove == null || !_playerMove.isAttacking) &&
            (!requireAxeSelected || IsAxeSelected()))
        {
            var target = FindReachableTree();
            if (!target) return;

            // ✅ 현재 선택이 Axe인지 확인 + Attack Power 읽기
            int toolPower = GetSelectedToolPower_Axe();
            if (toolPower <= 0) return;

            // 방향 계산 (타겟이 너무 가까우면 마우스 방향)
            Vector2 dir = (target.transform.position - transform.position);
            if (dir.sqrMagnitude < 0.0001f) dir = GetMouseWorldDir();
            else dir.Normalize();

            // 애니메이션 트리거 (유지)
            TriggerAxeAnim(dir);
            SoundManage.instance.PlaySFX("Melee_Attack");

            // ✅ HP 깎기 (ChopOnce 직접 호출 금지)
            var tree = target.GetComponentInParent<ChoppableTree>();
            if (tree) tree.Hit(toolPower);

            // 락 & 쿨타임
            if (_playerMove) _playerMove.isAttacking = true;
            _cool = cooldown;
        }
    }

    // == 콜라이더 기반 타깃팅 ==
    Collider2D FindReachableTree()
    {
        if (preferTreeUnderCursor)
        {
            var underCursor = GetTreeUnderCursor();
            if (underCursor && IsReachable(underCursor)) return underCursor;
        }

        var cols = Physics2D.OverlapCircleAll(transform.position, interactRange + searchPadding);
        Collider2D nearest = null;
        float best = float.MaxValue;

        foreach (var c in cols)
        {
            if (!IsTreeCollider(c)) continue;
            if (!IsReachable(c)) continue;

            float dist = DistanceToColliderSurface(c);
            if (dist < best) { best = dist; nearest = c; }
        }
        return nearest;
    }

    Collider2D GetTreeUnderCursor()
    {
        var cam = _cam ? _cam : Camera.main;
        if (!cam) return null;

        Vector2 p = cam.ScreenToWorldPoint(Input.mousePosition);
        var hits = Physics2D.OverlapPointAll(p);
        foreach (var h in hits)
            if (IsTreeCollider(h)) return h;
        return null;
    }

    bool IsTreeCollider(Collider2D c)
    {
        if (!c) return false;
        var t = c.transform;
        while (t != null)
        {
            if (t.CompareTag(treeTag)) return true;
            t = t.parent;
        }
        return false;
    }

    float DistanceToColliderSurface(Collider2D treeCol)
    {
        Vector2 closest = treeCol.ClosestPoint(transform.position);
        return Vector2.Distance(closest, transform.position);
    }

    bool IsReachable(Collider2D treeCol) => DistanceToColliderSurface(treeCol) <= interactRange;

    void TriggerAxeAnim(Vector2 dir)
    {
        if (_anim == null) return;
        _anim.SetFloat("AttackX", dir.x);
        _anim.SetFloat("AttackY", dir.y);
        _anim.ResetTrigger(axeTriggerName);
        _anim.SetTrigger(axeTriggerName);
    }

    Vector2 GetMouseWorldDir()
    {
        var cam = _cam ? _cam : Camera.main;
        if (!cam) return Vector2.right;

        Vector3 mouse = Input.mousePosition;
        mouse.z = 10f;
        Vector3 world = cam.ScreenToWorldPoint(mouse);
        Vector2 v = (Vector2)world - (Vector2)transform.position;
        if (v.sqrMagnitude < 0.0001f) v = Vector2.right;
        return v.normalized;
    }

    // 애니메이션 이벤트
    public void EndAxe()
    {
        if (_anim)
        {
            _anim.ResetTrigger(axeTriggerName);
            _anim.SetFloat("AttackX", 0f);
            _anim.SetFloat("AttackY", 0f);
        }
        if (_playerMove) _playerMove.isAttacking = false;
    }
    public void EndAttack() => EndAxe();

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 1, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif

    // ───────── 선택 아이템이 Axe인지 판정 + Attack Power 읽기 ─────────

    bool IsAxeSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null) return true; // 게이트 사용 안 하면 통과

        var selected = inv.GetSelectedItemData();
        if (axeItemData != null) return selected == axeItemData; // 특정 아이템 강제 모드

        var type = ReadItemType(selected);
        return !string.IsNullOrEmpty(type) &&
               type.Equals("Axe", System.StringComparison.OrdinalIgnoreCase);
    }

    int GetSelectedToolPower_Axe()
    {
        var inv = Inventory.Instance;
        var it  = inv ? inv.GetSelectedItemData() : null;
        if (it == null) return 0;

        var type = ReadItemType(it);
        if (string.IsNullOrEmpty(type) || !type.Equals("Axe", System.StringComparison.OrdinalIgnoreCase))
            return 0;

        int ap = ReadAttackPower(it);
        return Mathf.Max(1, ap); // 최소 1
    }

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

    // 다양한 이름/타입 지원 + 이름 기반 폴백(Copper=1, Iron=2)
    static int ReadAttackPower(ItemData it)
    {
        if (it == null) return 1;
        var t = it.GetType();

        string[] names = {
            "attackPower","AttackPower",
            "chopPower","ChopPower",
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
}
