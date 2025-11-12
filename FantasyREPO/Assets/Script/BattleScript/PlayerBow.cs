using UnityEngine;

public class PlayerBow : MonoBehaviour
{
    [Header("Damage & Cooldown")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Inventory Gate")]
    [Tooltip("비워두면 타입(Bow)만 검사하고, 채우면 이 아이템과 타입 둘 중 하나라도 만족하면 공격 허용")]
    [SerializeField] private ItemData bowItemData;     // 선택Bow(옵션)
    [SerializeField] private ItemData arrowItemData;   // 소모 화살(권장)

    [Header("Arrow")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float arrowSpeed = 15f;

    private float curTime;
    private PlayerMove _playerMove;
    private Rigidbody2D _rb;
    private Animator _animator;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerMove = GetComponent<PlayerMove>();
    }

    void Update()
    {
        if (curTime > 0f) curTime -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0) &&
            curTime <= 0f &&
            _playerMove != null && !_playerMove.isAttacking &&
            IsBowSelected() &&
            HasArrow())
        {
            PerformAttack();
        }
    }

    // === 타입 게이트: 선택 아이템의 itemType == "Bow" 이거나 bowItemData와 동일 ===
    private bool IsBowSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null || inv.IsSelectedEmpty()) return false;

        var sel = inv.GetSelectedItemData();
        if (sel == null) return false;

        // 1) 지정된 Bow 아이템과 동일하면 OK
        if (bowItemData && sel == bowItemData) return true;

        // 2) 타입이 Bow면 OK
        var type = ReadItemType(sel);
        return !string.IsNullOrEmpty(type) &&
               type.Equals("Bow", System.StringComparison.OrdinalIgnoreCase);
    }

    // 화살 보유 확인 (arrowItemData를 기준으로 체크)
    private bool HasArrow()
    {
        var inv = Inventory.Instance;
        if (inv == null) return false;
        if (!arrowItemData) return true;                // 화살 아이템을 지정하지 않았다면 소모 안함
        return inv.HasItem(arrowItemData, 1);
    }

    void PerformAttack()
    {
        curTime = attackCooldown;
        _playerMove.isAttacking = true;

        // 공격 방향
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;

        // 애니메이션
        _animator.SetFloat("AttackX", dir.x);
        _animator.SetFloat("AttackY", dir.y);
        _animator.SetTrigger("Bow");

        // 화살 생성/발사
        Vector3 spawn = arrowSpawnPoint ? arrowSpawnPoint.position : transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        var arrow = Instantiate(arrowPrefab, spawn, Quaternion.Euler(0, 0, angle));

        var rb = arrow.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = dir * arrowSpeed;         // Rigidbody2D는 velocity!

        // (선택) 화살 스크립트에 데미지 넘기기
        // var a = arrow.GetComponent<Arrow>(); if (a) a.damage = attackDamage;

        // 화살 소모
        if (arrowItemData) Inventory.Instance.RemoveItem(arrowItemData, 1);
    }

    // 애니메이션 이벤트에서 호출
    public void OnAttackAnimationFinished()
    {
        if (_playerMove) _playerMove.isAttacking = false;
        _animator.ResetTrigger("Bow");
        _animator.SetFloat("AttackX", 0f);
        _animator.SetFloat("AttackY", 0f);
    }

    // === ItemData.itemType 읽기(필드/프로퍼티 둘 다 지원) ===
    private static string ReadItemType(object it)
    {
        if (it == null) return null;
        var t = it.GetType();

        var f = t.GetField("itemType") ?? t.GetField("ItemType");
        if (f != null) { var v = f.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }

        var p = t.GetProperty("itemType") ?? t.GetProperty("ItemType");
        if (p != null) { var v = p.GetValue(it, null) as string; if (!string.IsNullOrEmpty(v)) return v; }

        return null;
    }
}
