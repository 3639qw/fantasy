using UnityEngine;
using System.Collections.Generic;

public class MeleeAttackScript : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackAngle = 180f;
    public float coolTime = 0.5f;

    private float curTime;
    private PlayerMove _playerMove;
    private Rigidbody2D _rb;
    private Animator _animator;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerMove = GetComponent<PlayerMove>();
    }

    private void Update()
    {
        if (curTime > 0) curTime -= Time.deltaTime;

        // 선택 아이템이 "Sword" 타입일 때만 공격
        if (Input.GetMouseButtonDown(0) &&
            curTime <= 0 &&
            _playerMove != null && !_playerMove.isAttacking &&
            IsSwordSelected())
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        var inv = Inventory.Instance;
        var currentWeapon = inv ? inv.GetSelectedItemData() : null;
        if (currentWeapon == null) return;

        // 공격력은 ItemData.attackPower(필드/프로퍼티)에서 읽음
        int power = ReadAttackPower(currentWeapon);
        if (power <= 0) power = 1;

        var objectsInRange = Physics2D.OverlapCircleAll(transform.position, attackRange);
        bool attacked = false;
        var damagedObjects = new HashSet<GameObject>();

        foreach (var obj in objectsInRange)
        {
            if (!obj || !obj.CompareTag("Enemy")) continue;

            var rootObj = obj.transform.root.gameObject;
            if (damagedObjects.Contains(rootObj)) continue;

            Vector2 dirToTarget = ((Vector2)obj.transform.position - (Vector2)transform.position).normalized;
            Vector2 dirToMouse  = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(dirToTarget, dirToMouse);

            if (angle <= attackAngle * 0.5f)
            {
                if (!attacked)
                {
                    Vector2 lastDir = dirToMouse.sqrMagnitude < 0.0001f ? Vector2.right : dirToMouse;
                    _animator.SetFloat("AttackX", lastDir.x);
                    _animator.SetFloat("AttackY", lastDir.y);
                    _animator.SetTrigger("Attack");

                    curTime = coolTime;
                    _playerMove.isAttacking = true;
                    attacked = true;
                }

                var damageable = obj.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(power);
                    damagedObjects.Add(rootObj);
                }
            }
        }
    }

    // 애니메이션 이벤트에서 호출
    void EndAttack()
    {
        _animator.SetFloat("AttackX", 0f);
        _animator.SetFloat("AttackY", 0f);
        _animator.ResetTrigger("Attack");
        if (_playerMove) _playerMove.isAttacking = false;
    }

    // === 타입 게이트: 선택 아이템의 itemType이 "Sword"인가 ===
    private bool IsSwordSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null || inv.IsSelectedEmpty()) return false;

        var it   = inv.GetSelectedItemData();
        var type = ReadItemType(it);
        return !string.IsNullOrEmpty(type) &&
               type.Equals("Sword", System.StringComparison.OrdinalIgnoreCase);
    }

    // === 리플렉션 헬퍼: ItemData.itemType / attackPower 읽기 ===
    private static string ReadItemType(object it)
    {
        if (it == null) return null;
        var t = it.GetType();

        var f = t.GetField("itemType") ?? t.GetField("ItemType");
        if (f != null) { var v = f.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }

        var p = t.GetProperty("itemType") ?? t.GetProperty("ItemType");
        if (p != null) { var v = p.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }

        return null;
    }

    private static int ReadAttackPower(object it)
    {
        if (it == null) return 1;
        var t = it.GetType();

        var f = t.GetField("attackPower") ?? t.GetField("AttackPower");
        if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(it);

        var p = t.GetProperty("attackPower") ?? t.GetProperty("AttackPower");
        if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(it, null);

        return 1;
    }
}
