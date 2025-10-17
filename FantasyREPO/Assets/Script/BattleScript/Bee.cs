using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Bee : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 6f; // 벌의 비행 속도
    [SerializeField] private float _damage = 5f;    // 벌의 공격력
    [SerializeField] private float _lifeTime = 8f;  // 플레이어를 못 맞췄을 때 자동 파괴되는 시간

    [Header("벌 설정")]
    [SerializeField] private float _holdTime = 0.5f; // 소환 후 머무르는 시간
    private bool _canChase = false; // 추적 가능 상태

    private Transform _target;
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        
        // _lifeTime 뒤에 자동으로 파괴되도록 예약
        Destroy(gameObject, _lifeTime);
    }

    /// <summary>
    /// SlimeKing이 호출하여 벌의 타겟(플레이어)을 설정합니다.
    /// </summary>
    public void Initialize(Transform target)
    {
        _target = target;
        StartCoroutine(HoldAndChaseRoutine());
    }

    private IEnumerator HoldAndChaseRoutine()
    {
        // 1. _holdTime (0.5초) 만큼 그 자리에서 대기
        yield return new WaitForSeconds(_holdTime);

        // 2. 대기 시간이 끝나면 추적 시작
        _canChase = true;
    }

    void FixedUpdate()
    {
        // 타겟이 없으면(플레이어가 죽었거나) 움직이지 않음
        if (_target == null || !_canChase)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // 타겟(플레이어)을 향하는 방향 계산
        Vector2 direction = (_target.position - transform.position).normalized;
        
        // 해당 방향으로 속도 설정
        _rb.linearVelocity = direction * _moveSpeed;

        // (선택 사항) 플레이어를 바라보도록 방향 전환
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward); // 90도 보정 (스프라이트 방향에 따라 다름)
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 부딪혔을 때
        if (other.CompareTag("Player"))
        {
            // 데미지 처리 (SlimeKing과 동일한 로직)
            PlayerHealthController playerHealth = other.GetComponentInParent<PlayerHealthController>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(_damage);
            }
            
            // (TODO: 여기에 벌 파괴 이펙트/사운드 재생)
            
            // 플레이어와 부딪히면 즉시 파괴
            Destroy(gameObject);
        }
        // // (선택 사항) 벽이나 장애물과 부딪혔을 때
        // else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        // {
        //     // (TODO: 벌 파괴 이펙트/사운드 재생)
        //     Destroy(gameObject);
        // }
    }
}