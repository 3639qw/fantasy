using UnityEngine;

public class Arrow : MonoBehaviour
{
    // 이 데미지 값은 화살을 발사하는 'PlayerBow.cs' 스크립트가
    // 화살을 생성(Instantiate)할 때 직접 설정해줘야 합니다.
    public float damage; 

    [Tooltip("화살이 아무것에도 부딪히지 않았을 때 자동으로 파괴되기까지 걸리는 시간")]
    [SerializeField]
    private float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("플레이어의 화살이 충돌함!");

        if (other.CompareTag("Player"))
        {
            return;
        }

        // 1. 부딪힌 대상이 "Enemy" 태그를 가지고 있는지 확인합니다.
        if (other.CompareTag("Enemy"))
        {
            // 2. 적의 IDamageable 컴포넌트를 찾습니다 (근접 공격 예시와 동일한 방식).
            //    GetComponentInParent를 사용하면 자식 오브젝트에 맞아도 부모의 IDamageable을 찾을 수 있습니다.
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // 3. 데미지를 입힙니다. (damage 값은 PlayerBow에서 설정해줍니다)
                damageable.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}