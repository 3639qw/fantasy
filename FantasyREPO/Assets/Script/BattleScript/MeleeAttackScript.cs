using UnityEngine;  // UnityEngine 네임스페이스 추가

public class MeleeAttackScript : MonoBehaviour
{
    public float attackRange = 2f;  // 공격 범위 (원 크기)
    public float attackAngle = 180f;  // 공격 각도 (기본값 180도)
    public Sprite attackRangeSprite;  // 공격 범위에 해당하는 원 스프라이트

    void Update()
    {
        if (Input.GetMouseButtonDown(0))  // 마우스 왼쪽 클릭
        {
            PerformAttack();
        }
    }

  

    private void PerformAttack()
    {

        // 1. 플레이어 주변의 오브젝트를 찾기 (범위 내 모든 오브젝트 감지)
        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(transform.position, attackRange);

        // 디버그 로그: 감지된 오브젝트 출력
        if (objectsInRange.Length == 0)
        {
            Debug.Log("범위 내에 몬스터가 없습니다.");
        }
        else
        {
            foreach (var obj in objectsInRange)
            {
                // 태그가 "Enemy"인 오브젝트만 감지
                if (obj.CompareTag("Enemy"))
                {
                    Debug.Log("감지된 몬스터: " + obj.name);
                }
            }
        }

        // 범위 내에 몬스터가 있다면 공격 각도 계산
        foreach (Collider2D obj in objectsInRange)
        {
            // 태그가 "Enemy"인 오브젝트만 공격
            if (obj.CompareTag("Enemy"))
            {
                Debug.Log("공격 감지됨.");
                // 몬스터의 위치와 플레이어의 위치를 기준으로 방향을 계산
                Vector2 directionToMonster = (obj.transform.position - transform.position).normalized;

                // 마우스 위치 계산
                Vector2 directionToMouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;

                // 각도 계산
                float angle = Vector2.Angle(directionToMonster, directionToMouse);

                // 각도가 범위 내에 있을 경우 공격
                if (angle <= attackAngle / 2)
                {
                    Debug.Log("몬스터 공격 성공: " + obj.name);
                    // 공격 처리 로직 추가
                }
                else
                {
                    Debug.Log("각도 외 공격 실패");
                }
            }
        }

    }
}
