using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// --- 변경 ---
// 보스 전용 상태 추가 (Attack, Skill)
public enum SlimeKingState
{
    Idle,
    Wander,
    Chase,
    Attack, // --- 추가 ---
    Skill,  // --- 추가 ---
    Resting,
    Attacked,
    Die
}

[RequireComponent(typeof(Rigidbody2D))]
// --- 변경 ---
// 클래스 이름을 SlimeScript -> SlimeKing로 변경
public class SlimeKing : MonoBehaviour, IDamageable
{
    [Header("기본 능력치")]
    public float idleTime = 2f;
    public float wanderRadius = 3f;
    public float chaseRange = 8f; // 보스라서 추적 범위를 조금 늘렸습니다 (조정 가능)
    public float moveSpeed = 2f;
    public float damage = 15f; // 보스 데미지
    public float MonsterHP = 150f; // 보스 체력

    // --- 추가 ---
    [Header("보스 전용 설정")]
    public float attackChargeTime = 0.25f; // 공격 전 충전 시간
    public float attackRange = 30f; // 공격 사거리
    public float attackDamage = 50f;
    public float attackAnimationTime = 2.09f; // 공격 애니메이션 시간
    public float attackJumpDuration = 1.84f; // 공격 점프 지속 시간

    public float actionCooldown = 5f; // 공격/스킬 통합 쿨타임
    private float actionTimer; // 통합 쿨타임 타이머

    public float skillRange = 7f; // 스킬 사거리
    public float skillAnimationTime = 0.3f; // 스킬 애니메이션 시간


    public float attackedAnimationTime = 0.3f; // 피격 애니메이션 시간
    public float dieAnimationTime = 2.07f; // 사망 애니메이션 시간
    
    [Header("잔상 효과")]
    [SerializeField] private GameObject _chargeGhostPrefab; // 'ChargeGhost' 프리팹
    [SerializeField] private GameObject _trailGhostPrefab; // 'SlimeKing_TrailGhost' 프리팹
    [SerializeField] private Material _ghostMaterial; // 잔상에 적용할 Material
    [SerializeField] private float _attackGhostInterval = 0.05f; // 공격 중 잔상 생성 간격
    [SerializeField] private float _chargeGhostInterval = 0.08f;

    [Header("스킬: 벌 소환")]
    [SerializeField] private GameObject _beePrefab; // 벌 프리팹
    [SerializeField] private int _beeCount = 8; // 소환할 벌의 수
    [SerializeField] private float _beeSpawnRadius = 1.5f; // 보스 기준 소환 반경

    [Header("스킬: 독버섯 소환")]
    [SerializeField] public GameObject Bombschroom;

    private GameObject MonsterGenerator;

    private SpriteRenderer _spriteRenderer; // 몬스터의 SpriteRenderer
    private Coroutine _actionRoutine = null; // 공격/스킬 코루틴 참조

    private SlimeKingState currentState; // --- 변경 ---
    private float stateTimer;
    private Animator animator;
    private Transform player;
    private PlayerMove playerState;

    private NavMeshPath path;
    private int pathIndex;
    private Rigidbody2D rb;

    private float pathUpdateInterval = 0.5f;
    private float pathUpdateTimer;

    // private GameManager _playerHP; // 원본 코드에 있었지만 사용되지 않아 주석 처리 (필요시 해제)

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        path = new NavMeshPath();
        playerState = player.GetComponent<PlayerMove>();
        _spriteRenderer = GetComponent<SpriteRenderer>(); // <-- ★★★ 매우 중요! 이 라인이 꼭 있어야 합니다.
    }

    private void Start()
    {
        // 쿨타임 초기화
        actionTimer = actionCooldown;
        ChangeState(SlimeKingState.Idle); // --- 변경 ---
    }

    private void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // --- 추가 ---
        // 쿨타임 감소
        if (actionTimer > 0) actionTimer -= Time.deltaTime;
        // --- 추가 끝 ---

        // 사망 또는 피격 시 다른 로직 중지
        if (currentState == SlimeKingState.Die || currentState == SlimeKingState.Attacked ||
            currentState == SlimeKingState.Attack || currentState == SlimeKingState.Skill ||
            currentState == SlimeKingState.Resting)
        {
            // 피격(Attacked) 또는 사망(Die) 시, 진행 중인 액션(공격/스킬) 코루틴 강제 중단
            if (currentState == SlimeKingState.Attacked || currentState == SlimeKingState.Die)
            {
                if (_actionRoutine != null)
                {
                    StopCoroutine(_actionRoutine);
                    _actionRoutine = null;
                    rb.linearVelocity = Vector2.zero; // 코루틴 강제 종료 시 속도 초기화
                }
            }
            
            // 상태 타이머가 0이 되면 다음 상태로 전환
            if (stateTimer <= 0f)
            {
                if (currentState == SlimeKingState.Die)
                {
                    Destroy(gameObject); 
                }
                else if (currentState == SlimeKingState.Skill) // 스킬이 끝나면 휴식
                {
                    ChangeState(SlimeKingState.Resting); 
                }
                else if (currentState == SlimeKingState.Attack) // 공격(코루틴)이 끝나면 휴식
                {
                    // (참고: Attack 코루틴이 스스로 stateTimer를 0으로 만들어 이 조건문을 발동시킴)
                    ChangeState(SlimeKingState.Resting);
                }
                else // Attacked, Resting 상태가 끝났을 때
                {
                    ChangeState(distanceToPlayer > chaseRange ? SlimeKingState.Idle : SlimeKingState.Chase);
                }
            }
            stateTimer -= Time.deltaTime;
            return; 
        }
        
        // --- 변경: 보스 AI 로직 ---
       // 1. (공격/스킬) 통합 쿨타임이 돌았는가?
        if (actionTimer <= 0)
        {
            // 2. 쿨타임이 돌았다면, 사거리 상관없이 50% 확률로 Attack 또는 Skill 사용
            if (Random.value > 0.5f) // 50% 확률 (0.0 ~ 1.0 사이의 난수)
            {
                ChangeState(SlimeKingState.Attack);
            }
            else
            {
                ChangeState(SlimeKingState.Skill);
            }
            return; // 상태 변경했으니 Update 종료
        }

        // 3. (공격/스킬을 안 썼다면) 추적 범위인가?
        if (distanceToPlayer < chaseRange)
        {
            // (참고: Chase 상태로 변경되어도 실제 이동은 FixedUpdate에서 처리)
            if (currentState != SlimeKingState.Chase) // 불필요한 상태 변경 방지
            {
                 ChangeState(SlimeKingState.Chase);
            }
        }
        // 4. (추적 범위도 아니라면) 배회/대기
        else
        {
            if (currentState == SlimeKingState.Chase) // 추적하다가 범위 밖으로 나감
            {
                ChangeState(SlimeKingState.Idle);
            }
            else if (currentState == SlimeKingState.Idle)
            {
                if (stateTimer <= 0f)
                {
                    ChangeState(SlimeKingState.Wander);
                }
            }
            else if (currentState == SlimeKingState.Wander)
            {
                if (ReachedDestination())
                {
                    ChangeState(SlimeKingState.Idle);
                }
            }
        }
        
        // --- 기존 로직 간소화 ---
        switch (currentState)
        {
            case SlimeKingState.Idle:
                stateTimer -= Time.deltaTime;
                break;
                
            case SlimeKingState.Wander:
                // 이동은 FixedUpdate에서 처리
                break;

            case SlimeKingState.Chase:
                // 경로 업데이트
                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    SetPathTo(player.position);
                    pathUpdateTimer = pathUpdateInterval;
                }
                break;
        }

        FixZ(); // 원본 코드 유지
    }

    private void FixedUpdate()
    {
        // --- 변경 ---
        // 이동 가능한 상태 (Wander, Chase)일 때만 경로 따라가기
        if (currentState == SlimeKingState.Wander || currentState == SlimeKingState.Chase)
        {
            FollowPath();
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // 그 외 모든 상태(Idle, Attack, Skill 등)에서 정지
        }
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        if (pos.z != 0)
        {
            pos.z = 0;
            transform.position = pos;
        }
    }

    // --- 변경 ---
    private void ChangeState(SlimeKingState newState)
    {
        // --- 6. 상태 변경 시 코루틴 중단 처리 (Attacked, Die 제외) ---
        // (이동/추적 상태로 변경될 때, 공격/스킬 코루틴이 남아있으면 중지)
        if (newState != SlimeKingState.Attacked && newState != SlimeKingState.Die)
        {
            if (_actionRoutine != null)
            {
                StopCoroutine(_actionRoutine);
                _actionRoutine = null;
                rb.linearVelocity = Vector2.zero;
            }
        }

        // 같은 상태로 중복 변경 방지 (선택 사항)
        if (currentState == newState) return;

        currentState = newState;
        stateTimer = idleTime; // 기본값
        pathIndex = 0;

        switch (newState)
        {
            case SlimeKingState.Idle:
                animator.Play("SlimeKing_Idle");
                path.ClearCorners();
                break;

            case SlimeKingState.Wander:
                animator.Play("SlimeKing_Move");
                Vector2 wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
                if (NavMesh.SamplePosition(wanderTarget, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                {
                    SetPathTo(hit.position);
                }
                break;

            case SlimeKingState.Chase:
                animator.Play("SlimeKing_Move");
                SetPathTo(player.position);
                pathUpdateTimer = pathUpdateInterval;
                break;

            // --- 7. Attack 상태 변경: 코루틴 시작 ---
            case SlimeKingState.Attack:
                actionTimer = actionCooldown;
                path.ClearCorners();
                rb.linearVelocity = Vector2.zero;
                // 코루틴 시작
                _actionRoutine = StartCoroutine(AttackRoutine());
                break;

            // --- 8. Skill 상태 변경: 코루틴 시작 ---
            case SlimeKingState.Skill:
                actionTimer = actionCooldown;
                path.ClearCorners();
                rb.linearVelocity = Vector2.zero;
                // 코루틴 시작
                _actionRoutine = StartCoroutine(SkillRoutine());
                break;

            case SlimeKingState.Resting:
                animator.Play("SlimeKing_Idle"); // Idle 애니메이션 재생
                stateTimer = 2.0f; // 2초간 휴식
                path.ClearCorners();
                rb.linearVelocity = Vector2.zero;
                break;

            case SlimeKingState.Attacked:
                animator.Play("SlimeKing_Attacked");
                stateTimer = attackedAnimationTime; // 피격 애니메이션 시간
                path.ClearCorners(); // 피격 시 잠시 멈춤
                rb.linearVelocity = Vector2.zero;
                break;

            case SlimeKingState.Die:
                animator.Play("SlimeKing_Dead");
                stateTimer = dieAnimationTime; // 사망 애니메이션 시간
                path.ClearCorners();
                rb.linearVelocity = Vector2.zero;
                GetComponent<Collider2D>().enabled = false; // 사망 시 충돌 비활성화
                break;
        }
    }
    
    // --- 9. 공격/스킬 코루틴 추가 ---
    private IEnumerator AttackRoutine()
    {
        rb.linearVelocity = Vector2.zero;


        stateTimer = attackChargeTime + attackJumpDuration + 0.1f; // (총 시간 + 여유시간)

        // 2. 점프 공격 (Jump Attack)
        animator.Play("SlimeKing_Attack"); // 실제 공격(점프) 모션
        SoundManage.instance.PlaySFX("Slime_Jump_Ready");

        stateTimer = attackChargeTime + attackJumpDuration + 0.1f; 
        
        float chargeTimer = 0f;
        float ghostSpawnTimer = 0f; // 잔상 생성 간격을 위한 타이머

        while (chargeTimer < attackChargeTime) // 0.25초 동안 반복
        {
            chargeTimer += Time.deltaTime;
            ghostSpawnTimer -= Time.deltaTime;

            // 잔상 생성 간격이 되면 "커지는 잔상" 생성
            if (ghostSpawnTimer <= 0f)
            {
                SpawnGhost(_chargeGhostPrefab);
                ghostSpawnTimer = _chargeGhostInterval; // 타이머 초기화
            }
            
            yield return null; // 다음 프레임까지 대기
        }

        float jumpTimer = 0f;
        float ghostTimer = 0f;

        while (jumpTimer < attackJumpDuration)
        {
            // 물리 프레임마다 대기 (이동 처리를 위해)
            yield return new WaitForFixedUpdate();

            jumpTimer += Time.fixedDeltaTime;
            ghostTimer += Time.fixedDeltaTime;

            // 잔상 생성 간격 체크
            if (ghostTimer >= _attackGhostInterval)
            {
                //SpawnGhost(_trailGhostPrefab); // 사라지는 잔상(Trail) 생성 더 좋은 방법이 있을까?
                ghostTimer = 0f;
            }
        }
        
        if (MonsterSpawner.Instance != null)
        {
            MonsterSpawner.Instance.SpawnBossMinions(4);
        }

        // 3. 종료
        rb.linearVelocity = Vector2.zero;
        _actionRoutine = null;
        
        // Update() 루프가 즉시 다음 상태(Resting)로 넘어가도록 타이머를 0에 가깝게 설정
        stateTimer = 0.01f; 
    }

    private IEnumerator SkillRoutine()
    {
        animator.Play("SlimeKing_Skill");
        rb.linearVelocity = Vector2.zero;

        stateTimer = attackChargeTime + skillAnimationTime; 

        float chargeTimer = 0f;
        float ghostSpawnTimer = 0f; // 잔상 생성 간격을 위한 타이머
        SoundManage.instance.PlaySFX("Boss_Skill_Ready");
        
        while (chargeTimer < attackChargeTime) // 0.25초 동안 반복
        {
            chargeTimer += Time.deltaTime;
            ghostSpawnTimer -= Time.deltaTime;
            

            // 잔상 생성 간격이 되면 "커지는 잔상" 생성
            if (ghostSpawnTimer <= 0f)
            {
                SpawnGhost(_chargeGhostPrefab);
                ghostSpawnTimer = _chargeGhostInterval; // 타이머 초기화
            }

            yield return null; // 다음 프레임까지 대기
        }


        // Update()가 이 상태를 건너뛰지 않도록 타이머 설정
        stateTimer = attackChargeTime + skillAnimationTime; // (충전 시간 + 스킬 시전 시간)

        yield return new WaitForSeconds(attackChargeTime); // 0.25초 대기

        if (_beePrefab != null && player != null)
        {
            float angleStep = 360f / _beeCount; // 8마리 기준 45도

            for (int i = 0; i < _beeCount; i++)
            {
                // 원형 위치 계산 (Trigonometry)
                float currentAngleRad = (angleStep * i) * Mathf.Deg2Rad; // 라디안으로 변환
                Vector2 offset = new Vector2(Mathf.Cos(currentAngleRad), Mathf.Sin(currentAngleRad)) * _beeSpawnRadius;
                Vector2 spawnPosition = (Vector2)transform.position + offset;

                // 벌 생성
                GameObject beeObj = Instantiate(_beePrefab, spawnPosition, Quaternion.identity);
                SoundManage.instance.PlaySFX("Boss_Skill_Bee");

                // 벌 스크립트에 플레이어(타겟) 정보 전달
                Bee beeScript = beeObj.GetComponent<Bee>();
                if (beeScript != null)
                {
                    beeScript.Initialize(player);
                }
                yield return new WaitForSeconds(0.05f);
            }
        }
        else
        {
            Debug.LogWarning("Bee Prefab 또는 Player가 할당되지 않았습니다!");
        }
        
        _actionRoutine = null;
    }

    // --- 10. 잔상 생성 헬퍼 함수 추가 ---
    private void SpawnGhost(GameObject ghostPrefab)
    {
        if (ghostPrefab == null || _spriteRenderer == null)
        {
            Debug.LogWarning("Ghost Prefab 또는 SpriteRenderer가 할당되지 않았습니다.");
            return;
        }

        GameObject ghostObj = Instantiate(ghostPrefab, transform.position, transform.rotation);
        
        // FadingGhost.cs 스크립트가 있는지 확인
        FadingGhost fadingGhost = ghostObj.GetComponent<FadingGhost>();
        if (fadingGhost != null)
        {
            fadingGhost.Setup(
                _spriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                _spriteRenderer.flipX,
                _spriteRenderer.sortingOrder,
                _ghostMaterial
            );
            return; // FadingGhost 처리 완료
        }

        // ChargeGhost.cs 스크립트가 있는지 확인
        ChargeGhost chargeGhost = ghostObj.GetComponent<ChargeGhost>();
        if (chargeGhost != null)
        {
            chargeGhost.Setup(
                _spriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                _spriteRenderer.flipX,
                _spriteRenderer.sortingOrder,
                _ghostMaterial
            );
        }
    }

    private void SetPathTo(Vector3 target)
    {
        if (NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path) &&
            path.status == NavMeshPathStatus.PathComplete)
        {
            pathIndex = 0;
        }
        else
        {
            path.ClearCorners();
        }
    }

    private void FollowPath()
    {
        if (path == null || path.corners.Length == 0 || pathIndex >= path.corners.Length)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector3 targetCorner = path.corners[pathIndex];
        targetCorner.z = transform.position.z;

        Vector2 direction = ((Vector2)targetCorner - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        float distance = Vector2.Distance(rb.position, targetCorner);

        if (distance < 0.1f)
        {
            pathIndex++;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = direction * moveSpeed;
        }
    }

    private bool ReachedDestination()
    {
        return pathIndex >= path.corners.Length;
    }

    private void FixZ()
    {
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealthController playerHealth = other.GetComponentInParent<PlayerHealthController>();
            if (GameManager.Instance != null && playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("GameManager instance or PlayerHealthController is null!");
            }
        }
    }
    
    public void PerformAttack()
    {
        PlayerHealthController playerHealth = player.GetComponentInParent<PlayerHealthController>();
        if (GameManager.Instance != null && playerHealth != null)
        {
            if (CameraShake.Instance != null)
            {
                SoundManage.instance.PlaySFX("Slime_Jump_Attack");
                Debug.Log("화면 흔들림!");
                // (0.25초 동안, 0.4f의 강도로 흔들기 - 값은 원하는대로 조절하세요)
                CameraShake.Instance.Shake(0.25f, 0.4f);
            }
            if (!playerState.isJump)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log("희피 실패!");
            }
                

            else
            {
                Debug.Log("플레이어가 점프해서 피했습니다.");
                return;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        // --- 변경 ---
        // 죽었거나, 이미 피격/공격/스킬 모션 중일 때는 데미지를 받지 않음 (무적 판정)
        if (currentState == SlimeKingState.Die || currentState == SlimeKingState.Attacked ||
            currentState == SlimeKingState.Attack || currentState == SlimeKingState.Skill)
        {
            return;
        }

        MonsterHP -= amount;
        Debug.Log($"{gameObject.name} 피격! 현재 체력: {MonsterHP}");

        if (MonsterHP <= 0f)
        {
            ChangeState(SlimeKingState.Die);
            Debug.Log($"{gameObject.name} 사망!");
        }
        else
        {
            ChangeState(SlimeKingState.Attacked);
        }
    }
}