using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 필요합니다.

// IDamageable 인터페이스 구현
public class SkeletonAI : MonoBehaviour, IDamageable
{
    // Unity 에디터에서 설정할 수 있는 공개 변수들
    [Header("움직임")]
    public float moveSpeed = 2f; // 몬스터의 이동 속도
    public float wonderDurationMin = 2f; // Wonder 상태에서 이동하는 최소 시간
    public float wonderDurationMax = 5f; // Wonder 상태에서 이동하는 최대 시간
    public float wonderStopDurationMin = 1f; // Wonder 상태에서 멈춰있는 최소 시간
    public float wonderStopDurationMax = 3f; // Wonder 상태에서 멈춰있는 최대 시간

    [Header("감지 및 공격")]
    public float chaseRange = 10f; // 플레이어를 추적하기 시작하는 거리
    public float attackRange = 5f;  // 플레이어를 공격하기 위해 멈추는 거리 (사거리)
    public float attackCooldown = 2f; // 공격 사이의 재사용 대기 시간
    public GameObject arrowPrefab; // 발사할 화살 프리팹
    public Transform arrowSpawnPoint; // 화살이 생성될 위치
    public float attackDamage = 10f; // 이 스켈레톤의 공격 데미지 (ArrowScript로 전달될 값, float으로 변경)

    [Header("체력")]
    public float maxHealth = 100f; // 몬스터의 최대 체력 (float으로 변경)
    private float currentHealth; // 몬스터의 현재 체력 (float으로 변경)

    [Header("드랍테이블 설정")]
    public GameObject itemWorldPrefab;
    public ItemData SkeletonDropItem;
    public ItemData SkeletonDropItem2;
    [Min(1)] public int Amount = 1;
    public float dropChance = 0.4f;

    // 비공개 참조 변수
    private Animator animator; // Animator 컴포넌트 참조
    private Rigidbody2D rb; // Rigidbody2D 컴포넌트 참조
    private Transform playerTransform; // 플레이어의 Transform 참조

    // FSM (Finite State Machine) 변수
    public enum MonsterState { Idle, Wonder, Chase, Attack, Hit, Dead }
    public MonsterState currentState; // 현재 몬스터 상태

    // 상태별 변수
    private Vector2 wonderDirection; // Wonder 상태에서 이동할 방향
    private float attackTimer; // 공격 재사용 대기 시간 타이머
    private bool isAttacking = false; // 현재 공격 중인지 여부
    private bool isHit = false; // 현재 피격 중인지 여부
    private bool isDead = false; // 현재 죽은 상태인지 여부

    // 성능 향상을 위한 Animator 파라미터 해시
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    private static readonly int LastHorizontal = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVertical = Animator.StringToHash("LastVertical");
    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int AttackedTrigger = Animator.StringToHash("Attacked");
    private static readonly int DieTrigger = Animator.StringToHash("Die");

    void Awake()
    {
        // 컴포넌트 참조 초기화
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // 중력 영향 제거
        currentHealth = maxHealth; // 현재 체력을 최대 체력으로 초기화
    }

    void Start()
    {
        // "Player" 태그를 가진 게임 오브젝트를 찾아 플레이어 Transform 설정
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("Player 태그를 가진 플레이어 게임 오브젝트를 찾을 수 없습니다. 스켈레톤 AI가 추적하지 않습니다.");
        }

        ChangeState(MonsterState.Idle); // 시작 시 Idle 상태로 변경하여 WonderRoutine 시작
    }

    void Update()
    {
        if (isDead) return; // 죽은 상태면 아무것도 하지 않습니다.

        // 공격 재사용 대기 시간 업데이트 (공격 상태가 아니어도 계속 업데이트)
        if (attackTimer < attackCooldown)
        {
            attackTimer += Time.deltaTime;
        }

        // 상태 머신 로직
        float distanceToPlayer = (playerTransform != null) ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;

        switch (currentState)
        {
            case MonsterState.Idle:
                // Idle 상태에서는 WonderAndIdleCycle 코루틴이 상태 전환을 관리합니다.
                // 플레이어 감지 시 Chase로 즉시 전환
                if (playerTransform != null && distanceToPlayer <= chaseRange)
                {
                    ChangeState(MonsterState.Chase);
                }
                break;

            case MonsterState.Wonder:
                // Wonder 상태에서는 WonderAndIdleCycle 코루틴이 움직임과 Idle 전환을 관리합니다.
                // 플레이어 감지 시 Chase로 즉시 전환
                if (playerTransform != null && distanceToPlayer <= chaseRange)
                {
                    ChangeState(MonsterState.Chase);
                }
                break;

            case MonsterState.Chase:
                if (playerTransform == null || distanceToPlayer > chaseRange)
                {
                    ChangeState(MonsterState.Idle); // 플레이어가 범위를 벗어나면 Idle로 복귀
                }
                else if (distanceToPlayer <= attackRange)
                {
                    // 공격 범위 내에 들어오면 공격 시도
                    if (attackTimer >= attackCooldown && !isAttacking)
                    {
                        Debug.Log("Entering Attack State from Chase."); // Debug log: Chase에서 Attack 상태로 진입 시도
                        ChangeState(MonsterState.Attack);
                    }
                    else
                    {
                        // 공격 대기 중에는 멈춤
                        rb.linearVelocity = Vector2.zero;
                        SetAnimationParameters(Vector2.zero);
                    }
                }
                else
                {
                    // 플레이어를 향해 이동
                    Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                    rb.linearVelocity = directionToPlayer * moveSpeed;
                    SetAnimationParameters(directionToPlayer);
                }
                break;

            case MonsterState.Attack:
                // Attack 상태에서는 PerformAttack 코루틴이 로직을 관리합니다.
                // 공격 중에는 움직임 멈춤
                rb.linearVelocity = Vector2.zero;
                SetAnimationParameters(Vector2.zero); // 공격 애니메이션 중에도 Idle/마지막 방향 유지
                break;

            case MonsterState.Hit:
                // Hit 상태에서는 HitRecovery 코루틴이 로직을 관리합니다.
                // 피격 중에는 움직임 멈춤
                rb.linearVelocity = Vector2.zero;
                break;

            case MonsterState.Dead:
                // Dead 상태에서는 HandleDeadState가 파괴를 관리합니다.
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    void LateUpdate()
    {
        FixZ(); // Z축 고정
    }

    // --- 상태 전환 메서드 ---
    private void ChangeState(MonsterState newState)
    {
        // 현재 상태와 같으면 아무것도 하지 않음
        if (currentState == newState) return;

        Debug.Log($"Changing state from {currentState} to {newState}"); // Debug log: 상태 전환 시작

        // 이전 상태의 코루틴 중지 (특히 WonderAndIdleCycle, PerformAttack, HitRecovery)
        StopAllCoroutines();

        // 상태 플래그 초기화 (새로운 상태 진입 시)
        isAttacking = false;
        isHit = false;
        isDead = false;

        currentState = newState; // 상태 변경

        switch (newState)
        {
            case MonsterState.Idle:
                // Idle 상태가 되면 WonderAndIdleCycle을 시작하여 Idle과 Wander 사이를 오가도록 합니다.
                StartCoroutine(WonderAndIdleCycle());
                // Idle 상태 진입 시 즉시 멈춤
                rb.linearVelocity = Vector2.zero;
                SetAnimationParameters(Vector2.zero);
                break;

            case MonsterState.Wonder:
                // 이 상태는 WonderAndIdleCycle 코루틴 내부에서만 직접 설정됩니다.
                // 외부에서 Wonder로 직접 ChangeState 호출 시에는 Idle로 시작하여 사이클을 시작합니다.
                // (만약 외부에서 직접 Wonder로 시작해야 한다면, 이 부분을 조정해야 할 수 있습니다)
                break;

            case MonsterState.Chase:
                // Chase 상태 진입 시 특별한 초기 애니메이션 트리거는 필요 없음.
                // Update에서 지속적으로 SetAnimationParameters가 호출될 것임.
                break;

            case MonsterState.Attack:
                isAttacking = true;
                // 공격 시작 시 즉시 멈춤
                rb.linearVelocity = Vector2.zero;
                // 공격 방향으로 바라보도록 애니메이터 파라미터 설정
                Vector2 directionToPlayer = (playerTransform != null) ? (playerTransform.position - transform.position).normalized : Vector2.down;
                animator.SetFloat(AttackX, directionToPlayer.x);
                animator.SetFloat(AttackY, directionToPlayer.y);
                animator.SetTrigger(AttackTrigger); // 공격 애니메이션 트리거
                StartCoroutine(PerformAttack(directionToPlayer));
                break;

            case MonsterState.Hit:
                isHit = true;
                rb.linearVelocity = Vector2.zero; // 피격 시 멈춤
                animator.SetTrigger(AttackedTrigger); // 피격 애니메이션 트리거
                StartCoroutine(HitRecovery()); // 피격 회복 코루틴 시작
                break;

            case MonsterState.Dead:
                isDead = true;
                rb.linearVelocity = Vector2.zero; // 사망 시 멈춤
                GetComponent<Collider2D>().enabled = false; // 몬스터의 콜라이더 비활성화
                animator.SetTrigger(DieTrigger); // 죽음 애니메이션 트리거
                Destroy(gameObject, 1f); // 일정 시간 후 게임 오브젝트 파괴

                if (Random.Range(0f, 1f) <= dropChance)
                {
                    GiveLootOnce();
                }
                break;
        }
    }

    // --- 상태별 코루틴 및 로직 ---

    IEnumerator WonderAndIdleCycle()
    {
        while (true) // 이 코루틴은 ChangeState에 의해 중지될 때까지 계속 실행됩니다.
        {
            // Idle Phase
            currentState = MonsterState.Idle; // 상태를 명시적으로 Idle로 설정
            rb.linearVelocity = Vector2.zero;
            SetAnimationParameters(Vector2.zero); // Idle 애니메이션 설정 (마지막 방향 유지)
            float stopDuration = Random.Range(wonderStopDurationMin, wonderStopDurationMax);
            yield return new WaitForSeconds(stopDuration);

            // Player detection check during Idle phase
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= chaseRange)
            {
                ChangeState(MonsterState.Chase);
                yield break; // 코루틴 종료
            }

            // Wander Phase
            currentState = MonsterState.Wonder; // 상태를 명시적으로 Wonder로 설정
            wonderDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            rb.linearVelocity = wonderDirection * moveSpeed;
            SetAnimationParameters(wonderDirection); // 이동 애니메이션 설정

            float moveDuration = Random.Range(wonderDurationMin, wonderDurationMax);
            yield return new WaitForSeconds(moveDuration);

            // Player detection check during Wander phase
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= chaseRange)
            {
                ChangeState(MonsterState.Chase);
                yield break; // 코루틴 종료
            }
        }
    }

    IEnumerator PerformAttack(Vector2 direction)
    {
        Debug.Log("PerformAttack Coroutine Started."); // Debug log: PerformAttack 코루틴 시작
        // 애니메이션과 동기화하기 위한 짧은 지연 시간 (필요에 따라 조정)
        yield return new WaitForSeconds(0.3f); // 예시 지연 시간

        if (arrowPrefab != null && arrowSpawnPoint != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            angle += 90f; // 남쪽(Z=0)을 기준으로 반시계 방향 회전 보정

            Quaternion arrowRotation = Quaternion.Euler(0, 0, angle);

            GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowRotation);
            SoundManage.instance.PlaySFX("Arrow_Shot");
            
            ArrowScript arrowScript = arrow.GetComponent<ArrowScript>();
            if (arrowScript != null)
            {
                arrowScript.SetDirection(direction);
                arrowScript.SetDamage(attackDamage); // float 타입으로 전달
            }
            else
            {
                Debug.LogWarning("Arrow prefab에 ArrowScript가 없습니다! 화살이 움직이거나 데미지를 줄 수 없습니다.");
            }
        }

        attackTimer = 0f; // 재사용 대기 시간 초기화
        isAttacking = false;
        animator.SetFloat(LastHorizontal, direction.x);
        animator.SetFloat(LastVertical, direction.y);
        animator.SetFloat(AttackX, 0f);
        animator.SetFloat(AttackY, 0f);

        // 공격 후 다시 추적 또는 Idle 상태로 돌아감
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= chaseRange)
        {
            ChangeState(MonsterState.Chase);
        }
        else
        {
            ChangeState(MonsterState.Idle); // 플레이어가 범위 밖이면 Idle로 돌아감
        }
    }

    IEnumerator HitRecovery()
    {
        Debug.Log("HitRecovery Coroutine Started."); // Debug log: HitRecovery 코루틴 시작
        // 피격 애니메이션 지속 시간 또는 짧은 스턴 시간 동안 대기
        yield return new WaitForSeconds(0.5f);
        isHit = false;
        // 피격 후 이전 상태 또는 기본 상태로 돌아갈지 결정
        if (!isDead)
        {
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= chaseRange)
            {
                ChangeState(MonsterState.Chase);
            }
            else
            {
                ChangeState(MonsterState.Idle);
            }
        }
    }

    // --- 외부 콜백 (IDamageable 구현) ---

    // 몬스터가 피해를 입을 때 이 메서드를 호출합니다.
    public void TakeDamage(float amount) // float 타입으로 변경
    {
        if (isDead) return; // 죽은 몬스터는 피해를 입지 않습니다.

        currentHealth -= amount; // 체력 감소
        Debug.Log($"{gameObject.name} 피격! 현재 체력: {currentHealth}");

        if (currentHealth <= 0f) // float 비교
        {
            currentHealth = 0f;
            ChangeState(MonsterState.Dead); // 체력이 0이 되면 Dead 상태로 전환
        }
        else if (currentState != MonsterState.Hit) // 이미 Hit 상태가 아니면 Hit 상태로 전환
        {
            ChangeState(MonsterState.Hit); // 피해를 입으면 Hit 상태로 전환
        }
    }

    // --- 헬퍼 메서드 ---

    void SetAnimationParameters(Vector2 moveDirection)
    {
        // 블렌드 트리를 위한 이동 파라미터 설정
        animator.SetFloat(Horizontal, moveDirection.x);
        animator.SetFloat(Vertical, moveDirection.y);

        // 움직일 때만 LastHorizontal/LastVertical 업데이트 (Idle 시 마지막 방향 유지)
        if (moveDirection.magnitude > 0.1f) // 부동 소수점 문제 방지를 위한 작은 임계값 사용
        {
            animator.SetFloat(LastHorizontal, moveDirection.x);
            animator.SetFloat(LastVertical, moveDirection.y);
        }
    }

    private void FixZ()
    {
        Vector3 pos = transform.position;
        if (pos.z != 0f)
        {
            pos.z = 0f;
            transform.position = pos;
        }
    }

    // 선택 사항: 에디터에서 시각화를 위해 기즈모 그리기
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange); // 추적 범위 표시
        Gizmos.color = Color.red; // 공격 범위 색상 변경
        Gizmos.DrawWireSphere(transform.position, attackRange); // 공격 범위 표시
    }

        private void GiveLootOnce()
    {
        // 1. 프리팹과 데이터가 둘 다 설정되었는지 확인
        if (itemWorldPrefab != null && SkeletonDropItem != null)
        {
            // 2. 프리팹을 월드에 생성 (바위의 현재 위치에)
            GameObject droppedItemObj = Instantiate(itemWorldPrefab, transform.position, Quaternion.identity);

            // 3. 생성된 오브젝트에서 ItemWorld 스크립트를 가져옴
            ItemWorld itemScript = droppedItemObj.GetComponent<ItemWorld>();

            // 4. 스크립트에 아이템 정보와 수량을 전달
            if (itemScript != null)
            {
                if (Random.Range(0f, 1f) <= 0.5f)
                {
                    itemScript.Initialize(SkeletonDropItem, Amount);
                }
                else
                {
                    itemScript.Initialize(SkeletonDropItem2, Amount);
                }
                
            }
            else
            {
                Debug.LogError($"[SlimeScript] 'ItemWorld_Prefab'에 ItemWorld.cs 스크립트가 없습니다!");
            }
        }
    }
}
