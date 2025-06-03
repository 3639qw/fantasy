using UnityEngine;  // UnityEngine 네임스페이스 추가

public class MeleeAttackScript : MonoBehaviour
{
    public float attackRange = 2f;  // 공격 범위 (원 크기)
    public float attackAngle = 180f;  // 공격 각도 (기본값 180도)
    public Sprite attackRangeSprite;  // 공격 범위에 해당하는 원 스프라이트

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
        if (Input.GetMouseButtonDown(0) && curTime <= 0)
        {
            PerformAttack();
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log("Current State: " + state.fullPathHash);
        }

        if (curTime > 0)
        {
            curTime -= Time.deltaTime;
        }

    }

    private void PerformAttack()
    {
        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D obj in objectsInRange)
        {
            if (obj.CompareTag("Enemy"))
            {
                Vector2 directionToMonster = (obj.transform.position - transform.position).normalized;
                Vector2 directionToMouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
                float angle = Vector2.Angle(directionToMonster, directionToMouse);

                if (angle <= attackAngle / 2)
                {
                    // 애니메이션에 방향 정보 전달
                    Vector2 lastDir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;;

                    _animator.SetFloat("AttackX", lastDir.x);
                    _animator.SetFloat("AttackY", lastDir.y);
                    _animator.SetTrigger("Attack");

                    Debug.Log("몬스터 공격 성공: " + obj.name);
                    curTime = coolTime;

                    // 여기에 데미지 처리도 추가 가능
                }
            }
        }
    }

    void EndAttack()
    {
        _animator.SetFloat("AttackX", 0f);
        _animator.SetFloat("AttackY", 0f);
        _animator.ResetTrigger("Attack");
    }
}