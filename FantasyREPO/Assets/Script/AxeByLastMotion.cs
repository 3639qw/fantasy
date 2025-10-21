using UnityEngine;

public class AxeByLastMotion : MonoBehaviour
{
    [Header("Chop Settings")]
    public float interactRange = 2f;       // 플레이어 → 나무 콜라이더까지의 '표면' 거리 허용치
    public float searchPadding = 1f;       // 탐색 여유 반경 (큰 나무도 찾기 쉽게)
    public string treeTag = "Tree";
    public float cooldown = 0.35f;

    [Header("Inventory Gate (optional)")]
    public bool requireAxeSelected = false;
    public ItemData axeItemData;

    [Header("Targeting")]
    public bool preferTreeUnderCursor = true; // 커서가 콜라이더 위면 그 나무 우선

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

        // 좌클릭 전용
        if (Input.GetMouseButtonDown(0) &&
            _cool <= 0f &&
            (_playerMove == null || !_playerMove.isAttacking) &&
            (!requireAxeSelected || IsAxeSelected()))
        {
            var target = FindReachableTree();
            if (!target) return;

            // 방향 계산 (타겟 중심 기준, 너무 가까우면 마우스 방향)
            Vector2 dir = (target.transform.position - transform.position);
            if (dir.sqrMagnitude < 0.0001f) dir = GetMouseWorldDir();
            else dir.Normalize();

            // 애니메이션 트리거
            TriggerAxeAnim(dir);

            // 기능 실행
            var tree = target.GetComponentInParent<ChoppableTree>();
            if (tree) tree.ChopOnce();

            // 락 & 쿨타임
            if (_playerMove) _playerMove.isAttacking = true;
            _cool = cooldown;
        }
    }

    // == 콜라이더 기반 타깃팅 ==
    Collider2D FindReachableTree()
    {
        // 1) 커서가 콜라이더 위면 우선 선택
        if (preferTreeUnderCursor)
        {
            var underCursor = GetTreeUnderCursor();
            if (underCursor && IsReachable(underCursor)) return underCursor;
        }

        // 2) 주변에서 탐색 (searchPadding만큼 넓게)
        var cols = Physics2D.OverlapCircleAll(transform.position, interactRange + searchPadding);
        Collider2D nearest = null;
        float best = float.MaxValue;

        foreach (var c in cols)
        {
            if (!IsTreeCollider(c)) continue;
            if (!IsReachable(c)) continue;

            // 콜라이더 '표면'까지의 실제 거리로 비교
            float dist = DistanceToColliderSurface(c);
            if (dist < best) { best = dist; nearest = c; }
        }
        return nearest;
    }

    // 커서가 덮고 있는 나무 콜라이더 가져오기
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

    // 트리 태그 판정(자식 콜라이더도 허용)
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

    // 플레이어 위치 ↔ 나무 콜라이더 표면까지의 거리
    float DistanceToColliderSurface(Collider2D treeCol)
    {
        Vector2 closest = treeCol.ClosestPoint(transform.position);
        return Vector2.Distance(closest, transform.position);
    }

    // 도달 판정: 콜라이더 표면까지 거리가 interactRange 이내
    bool IsReachable(Collider2D treeCol)
    {
        return DistanceToColliderSurface(treeCol) <= interactRange;
    }

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

    bool IsAxeSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null) return true;
        var selectedItem = inv.GetSelectedItemData();
        if (axeItemData != null) return selectedItem == axeItemData;
        return !inv.IsSelectedEmpty();
    }
}
