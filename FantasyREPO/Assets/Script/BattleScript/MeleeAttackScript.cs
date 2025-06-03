using UnityEngine;  // UnityEngine ���ӽ����̽� �߰�

public class MeleeAttackScript : MonoBehaviour
{
    public float attackRange = 2f;  // ���� ���� (�� ũ��)
    public float attackAngle = 180f;  // ���� ���� (�⺻�� 180��)
    public Sprite attackRangeSprite;  // ���� ������ �ش��ϴ� �� ��������Ʈ

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
                    // �ִϸ��̼ǿ� ���� ���� ����
                    Vector2 lastDir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;;

                    _animator.SetFloat("AttackX", lastDir.x);
                    _animator.SetFloat("AttackY", lastDir.y);
                    _animator.SetTrigger("Attack");

                    Debug.Log("���� ���� ����: " + obj.name);
                    curTime = coolTime;

                    // ���⿡ ������ ó���� �߰� ����
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