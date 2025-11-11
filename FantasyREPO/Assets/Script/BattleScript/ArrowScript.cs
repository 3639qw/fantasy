using UnityEngine;

public class ArrowScript : MonoBehaviour
{
    [Header("화살 설정")]
    public float arrowSpeed = 10f; 
    public float arrowLifetime = 5f;
    private Vector2 moveDirection; 

    [Header("출혈관리")]
    public float bleedingChance = 0.3f;
    public float bleedingDuration = 15f;
    public float bleedingTickDamage = 5f;
    public float bleedingInterval = 1f;

    private float damageAmount; // 화살이 플레이어에게 입힐 데미지 (float으로 변경)


    void Start()
    {
        // 일정 시간 후 화살 게임 오브젝트를 파괴하여 메모리 누수를 방지합니다.
        Destroy(gameObject, arrowLifetime);
    }

    // SkeletonAI 스크립트에서 호출하여 화살의 초기 이동 방향을 설정합니다.
    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized; // 방향을 정규화하여 일정한 속도를 유지합니다.
    }

    // SkeletonScript에서 데미지 값을 설정할 수 있도록 public 메서드 제공
    public void SetDamage(float damage) // float 타입으로 변경
    {
        damageAmount = damage;
    }

    void Update()
    {
        // 화살을 설정된 방향과 속도로 이동시킵니다.
        // Time.deltaTime을 곱하여 프레임 속도에 독립적인 움직임을 만듭니다.
        transform.Translate(moveDirection * arrowSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 오브젝트의 태그가 "Player"인지 확인합니다.
        if (other.CompareTag("PlayerCollider"))
        {
            PlayerHealthController playerHealth = other.GetComponentInParent<PlayerHealthController>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount); // 플레이어에게 데미지 적용
                Debug.Log($"플레이어가 화살에 맞음! 데미지: {damageAmount}");
                StatusCondition playerStatus = other.gameObject.GetComponentInParent<StatusCondition>();

                if (playerStatus != null)
                {
                    if (Random.Range(0f, 1f) <= bleedingChance)
                    {
                        playerStatus.ApplyBleeding(bleedingDuration, bleedingTickDamage, bleedingInterval);
                    }
                }
            }
            // 화살은 플레이어에게 데미지를 입힌 후 파괴됩니다.
            Destroy(gameObject);
        }
        // 플레이어 외의 다른 오브젝트(예: 벽, 다른 몬스터)와 충돌했을 때도 파괴
        // 필요에 따라 특정 레이어나 태그를 가진 오브젝트에만 반응하도록 수정 가능
        else if (!other.CompareTag("Enemy") && !other.CompareTag("Arrow")) // 몬스터나 다른 화살과는 충돌하지 않도록
        {
            Destroy(gameObject);
        }
    }
}
