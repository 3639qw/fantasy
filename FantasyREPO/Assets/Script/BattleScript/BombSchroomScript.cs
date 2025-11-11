using UnityEngine;
using System.Collections;

public class BombSchroomScript : MonoBehaviour, IDamageable
{
    [Header("움직임")]
    public float moveSpeed = 2f;
    public float wonderDurationMin = 2f;
    public float wonderDurationMax = 5f;
    public float wonderStopDurationMin = 1f;
    public float wonderStopDurationMax = 3f;

    [Header("감지 및 자폭")]
    public float chaseRange = 10f;
    public float attackRange = 1.5f;
    public float selfDestructDelay = 0.5f;
    public float attackDamage = 50f;
    public float encounterAnimationTime = 1f; // [추가] Encounter 애니메이션 시간

    [Header("프리팹 설정")]
    public GameObject poisonPrefab;

    [Header("드랍테이블 설정")]
    public GameObject itemWorldPrefab;
    public ItemData BombschroomDropItem;
    [Min(1)] public int Amount = 1;
    public float dropChance = 0.4f;

    [Header("체력")]
    public float maxHealth = 10f;
    private float currentHealth;

    private Animator animator;
    private Rigidbody2D rb;
    private Transform playerTransform;

    // [수정] Encounter 상태 추가
    public enum MonsterState { Idle, Wonder, Encounter, Chase, Attack, Hit, Dead }
    public MonsterState currentState;

    private bool isDead = false;
    
    // 애니메이션 파라미터 해시
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    private static readonly int LastHorizontal = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVertical = Animator.StringToHash("LastVertical");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int AttackedTrigger = Animator.StringToHash("Attacked");
    private static readonly int DieTrigger = Animator.StringToHash("Die");
    private static readonly int EncounterTrigger = Animator.StringToHash("Encounter"); // Encounter 트리거 추가

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        currentHealth = maxHealth;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        ChangeState(MonsterState.Idle);
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case MonsterState.Idle:
            case MonsterState.Wonder:
                // [수정] 플레이어 발견 시 Chase가 아닌 Encounter 상태로 전환
                if (distanceToPlayer <= chaseRange)
                {
                    ChangeState(MonsterState.Encounter);
                }
                break;

            case MonsterState.Chase:
                if (distanceToPlayer > chaseRange)
                {
                    ChangeState(MonsterState.Idle);
                }
                else if (distanceToPlayer <= attackRange)
                {
                    ChangeState(MonsterState.Attack);
                }
                else
                {
                    Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                    rb.linearVelocity = directionToPlayer * moveSpeed;
                    SetAnimationParameters(directionToPlayer);
                }
                break;

            // 다른 상태들은 움직임이 없으므로 velocity를 0으로 설정
            case MonsterState.Encounter:
            case MonsterState.Attack:
            case MonsterState.Hit:
            case MonsterState.Dead:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void ChangeState(MonsterState newState)
    {
        if (currentState == newState || isDead) return;

        StopAllCoroutines();
        currentState = newState;

        switch (newState)
        {
            case MonsterState.Idle:
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(WonderAndIdleCycle());
                break;
            
            // [추가] Encounter 상태 진입 시 로직
            case MonsterState.Encounter:
                rb.linearVelocity = Vector2.zero;
                SetAnimationParameters(Vector2.zero);
                animator.SetTrigger(EncounterTrigger);
                StartCoroutine(EncounterRoutine());
                break;

            case MonsterState.Chase:
                break;

            case MonsterState.Attack:
                rb.linearVelocity = Vector2.zero;
                SetAnimationParameters(Vector2.zero);
                animator.SetTrigger(AttackTrigger);
                StartCoroutine(SelfDestructSequence());
                break;

            case MonsterState.Hit:
                rb.linearVelocity = Vector2.zero;
                animator.SetTrigger(AttackedTrigger);
                StartCoroutine(HitRecovery());
                break;

            case MonsterState.Dead:
                isDead = true;
                rb.linearVelocity = Vector2.zero;
                GetComponent<Collider2D>().enabled = false;
                animator.SetTrigger(DieTrigger);
                Destroy(gameObject, 0.5f);
                break;
        }
    }

    // [추가] Encounter 애니메이션 재생 후 Chase 상태로 전환하는 코루틴
    private IEnumerator EncounterRoutine()
    {
        // 설정된 애니메이션 시간만큼 대기
        yield return new WaitForSeconds(encounterAnimationTime);
        // Chase 상태로 전환
        ChangeState(MonsterState.Chase);
    }

    private IEnumerator SelfDestructSequence()
    {
        yield return new WaitForSeconds(selfDestructDelay);

        if (poisonPrefab != null)
        {
            GameObject poisonInstance = Instantiate(poisonPrefab, transform.position, Quaternion.identity);
            PoisonScript poisonScript = poisonInstance.GetComponent<PoisonScript>();
            if (poisonScript != null)
            {
                poisonScript.SetDamage(attackDamage);
            }
        }
        
        Destroy(gameObject);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            ChangeState(MonsterState.Dead);
        }
        else
        {
            ChangeState(MonsterState.Hit);
        }
    }

    private IEnumerator HitRecovery()
    {
        yield return new WaitForSeconds(0.5f);
        ChangeState(MonsterState.Idle);
    }

    private IEnumerator WonderAndIdleCycle()
    {
        while (currentState == MonsterState.Idle || currentState == MonsterState.Wonder)
        {
            currentState = MonsterState.Idle;
            rb.linearVelocity = Vector2.zero;
            SetAnimationParameters(Vector2.zero);
            yield return new WaitForSeconds(Random.Range(wonderStopDurationMin, wonderStopDurationMax));

            currentState = MonsterState.Wonder;
            Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            rb.linearVelocity = randomDirection * moveSpeed;
            SetAnimationParameters(randomDirection);
            yield return new WaitForSeconds(Random.Range(wonderDurationMin, wonderDurationMax));
        }
    }

    void SetAnimationParameters(Vector2 direction)
    {
        animator.SetFloat(Horizontal, direction.x);
        animator.SetFloat(Vertical, direction.y);

        if (direction.sqrMagnitude > 0)
        {
            animator.SetFloat(LastHorizontal, direction.x);
            animator.SetFloat(LastVertical, direction.y);
        }
    }

    private void GiveLootOnce()
    {
        // 1. 프리팹과 데이터가 둘 다 설정되었는지 확인
        if (itemWorldPrefab != null && BombschroomDropItem != null)
        {
            // 2. 프리팹을 월드에 생성 (바위의 현재 위치에)
            GameObject droppedItemObj = Instantiate(itemWorldPrefab, transform.position, Quaternion.identity);

            // 3. 생성된 오브젝트에서 ItemWorld 스크립트를 가져옴
            ItemWorld itemScript = droppedItemObj.GetComponent<ItemWorld>();

            // 4. 스크립트에 아이템 정보와 수량을 전달
            if (itemScript != null)
            {
                itemScript.Initialize(BombschroomDropItem, Amount);
            }
            else
            {
                Debug.LogError($"[SlimeScript] 'ItemWorld_Prefab'에 ItemWorld.cs 스크립트가 없습니다!");
            }
        }
    }
}