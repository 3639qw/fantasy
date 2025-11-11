using UnityEngine;

public class PoisonScript : MonoBehaviour
{
    [Header("독 설정")]
    public float poisonLifetime = 5f; // 자동으로 사라지는 시간 (장면이 너무 복잡해지는 것을 방지)

    [Header("독 상태이상 설정")]
    public float poisonTickDamage = 5f;
    public float poisonChance = 0.3f;
    public float poisontickInterval = 0.1f;

    private float damageAmount;
    void Start()
    {
        Destroy(gameObject, poisonLifetime);
    }

    void Update()
    {

    }
    public void SetDamage(float damage) // float 타입으로 변경
    {
        damageAmount = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerCollider"))
        {
            PlayerHealthController playerHealth = other.GetComponentInParent<PlayerHealthController>();
            StatusCondition playerStatus = other.gameObject.GetComponentInParent<StatusCondition>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                if (Random.Range(0f, 1f) <= poisonChance)
                {
                    playerStatus.ApplyPoison(poisonTickDamage, poisontickInterval);
                }
                Debug.Log($"플레이어가 독에 피해를 입음! 데미지 : {damageAmount}");
            }
        }
    }
}