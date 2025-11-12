using UnityEngine;
using System.Collections.Generic;

public class MeleeAttackScript : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackAngle = 180f;
    public float coolTime = 0.5f;

    // <<-- 변경: Sprite 참조 대신, 어떤 '아이템 데이터'가 검인지 명시합니다.
    [Header("Weapon Gate (Selected ItemData)")]
    [SerializeField] private ItemData swordItemData;

    // <<-- 삭제: 공격력은 이제 ItemData에서 가져오므로 이 변수는 필요 없습니다.
    // public float AttackDamage = 5f;

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
        if (curTime > 0)
        {
            curTime -= Time.deltaTime;
        }

        // IsSwordSelected()가 ItemData를 확인하도록 수정되었습니다.
        if (Input.GetMouseButtonDown(0) && curTime <= 0 && _playerMove != null && !_playerMove.isAttacking && IsSwordSelected())
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        // <<-- 추가: 현재 선택된 아이템의 데이터를 가져옵니다.
        ItemData currentWeapon = Inventory.Instance.GetSelectedItemData();
        if (currentWeapon == null) return; // 혹시 모를 null 체크

        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(transform.position, attackRange);
        bool attacked = false;
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

        foreach (Collider2D obj in objectsInRange)
        {
            if (obj.CompareTag("Enemy"))
            {
                GameObject rootObj = obj.transform.root.gameObject;
                if (damagedObjects.Contains(rootObj))
                    continue;

                Vector2 directionToMonster = (obj.transform.position - transform.position).normalized;
                Vector2 directionToMouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
                float angle = Vector2.Angle(directionToMonster, directionToMouse);

                if (angle <= attackAngle / 2)
                {
                    if (!attacked)
                    {
                        Vector2 lastDir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
                        _animator.SetFloat("AttackX", lastDir.x);
                        _animator.SetFloat("AttackY", lastDir.y);
                        _animator.SetTrigger("Attack");
                        SoundManage.instance.PlaySFX("Melee_Attack");

                        curTime = coolTime;
                        _playerMove.isAttacking = true;
                        attacked = true;
                    }

                    IDamageable damageable = obj.GetComponentInParent<IDamageable>(); // GetComponentInParent로 변경하여 더 안정적으로 찾기
                    if (damageable != null)
                    {
                        // <<-- 변경: 고정된 공격력이 아닌, 현재 무기(ItemData)의 공격력을 사용합니다.
                        // (ItemData 스크립트에 public float attackPower; 와 같은 변수가 필요합니다.)
                        damageable.TakeDamage(currentWeapon.attackPower); 
                        // 위 코드를 사용하려면 ItemData.cs에 public float attackPower; 를 추가해야 합니다.
                        
                        damagedObjects.Add(rootObj);
                    }
                }
            }
        }
    }

    void EndAttack()
    {
        _animator.SetFloat("AttackX", 0f);
        _animator.SetFloat("AttackY", 0f);
        _animator.ResetTrigger("Attack");
        if(_playerMove != null) _playerMove.isAttacking = false;
    }

    // <<-- 변경: Sprite 대신 ItemData를 가져와서 비교합니다.
    private bool IsSwordSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null || inv.IsSelectedEmpty()) return false;

        // 인벤토리에서 현재 선택된 '아이템 데이터'를 가져옵니다.
        ItemData selectedItem = inv.GetSelectedItemData(); 
        
        // 선택된 아이템이 있고, 그것이 우리가 지정한 'swordItemData'와 일치하는지 확인합니다.
        return selectedItem != null && selectedItem == swordItemData;
    }
}