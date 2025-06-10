using UnityEngine;
using UnityEngine.AI;

public enum SlimeState
{
    Idle,
    Wander,
    Chase,
    Attacked,
    Die
}

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeScript : MonoBehaviour, IDamageable
{
    public float idleTime = 2f;
    public float wanderRadius = 3f;
    public float chaseRange = 5f;
    public float moveSpeed = 2f;
    public float damage = 5f;
    public float MonsterHP = 30f;

    private SlimeState currentState;
    private float stateTimer;
    private Animator animator;
    private Transform player;

    private NavMeshPath path;
    private int pathIndex;
    private Rigidbody2D rb;

    private float pathUpdateInterval = 0.5f;
    private float pathUpdateTimer;

    private GameManager _playerHP;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // 중력 영향 제거
        player = GameObject.FindGameObjectWithTag("Player").transform;
        _playerHP = FindObjectOfType<GameManager>();
        path = new NavMeshPath();
    }

    private void Start()
    {
        ChangeState(SlimeState.Idle);
    }

    private void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case SlimeState.Idle:
                if (distanceToPlayer < chaseRange)
                {
                    ChangeState(SlimeState.Chase);
                }
                else if (stateTimer <= 0f)
                {
                    ChangeState(SlimeState.Wander);
                }
                break;

            case SlimeState.Wander:
                if (distanceToPlayer < chaseRange)
                {
                    ChangeState(SlimeState.Chase);
                }
                else if (ReachedDestination())
                {
                    ChangeState(SlimeState.Idle);
                }
                break;

            case SlimeState.Chase:
                if (distanceToPlayer > chaseRange)
                {
                    ChangeState(SlimeState.Idle);
                }
                else
                {
                    pathUpdateTimer -= Time.deltaTime;
                    if (pathUpdateTimer <= 0f)
                    {
                        SetPathTo(player.position);
                        pathUpdateTimer = pathUpdateInterval;
                    }
                }
                break;

            case SlimeState.Attacked:
                if (stateTimer <= 0f)
                {
                    ChangeState(distanceToPlayer > chaseRange ? SlimeState.Idle : SlimeState.Chase);
                }
                break;

            case SlimeState.Die:
                if (stateTimer <= 0f)
                {
                    Destroy(gameObject);
                }
                break;
        }

        stateTimer -= Time.deltaTime;
        FixZ();
    }

    private void FixedUpdate()
    {
        FollowPath(); // 물리 이동은 FixedUpdate에서 실행
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

    private void ChangeState(SlimeState newState)
    {
        currentState = newState;
        stateTimer = idleTime;
        pathIndex = 0;

        switch (newState)
        {
            case SlimeState.Idle:
                animator.Play("Slime_Idle");
                path.ClearCorners();
                break;

            case SlimeState.Wander:
                animator.Play("Slime_Move");
                Vector2 wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
                if (NavMesh.SamplePosition(wanderTarget, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                {
                    SetPathTo(hit.position);
                }
                break;

            case SlimeState.Chase:
                animator.Play("Slime_Move");
                SetPathTo(player.position);
                pathUpdateTimer = pathUpdateInterval;
                break;

            case SlimeState.Attacked:
                animator.Play("Slime_attacked");
                stateTimer = 0.3f;
                break;

            case SlimeState.Die:
                animator.Play("Slime_die");
                stateTimer = 0.5f;
                path.ClearCorners();
                rb.linearVelocity = Vector2.zero;
                break;
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
            rb.linearVelocity = Vector2.zero;  // 경로 없으면 멈춤
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
            PlayerHealthController playerHealth = other.GetComponent<PlayerHealthController>();
            if (GameManager.Instance != null)
            {
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("GameManager instance is null!");
            }
        }
    }

    public void TakeDamage(float amount)
    {
        MonsterHP -= amount;
        Debug.Log($"{gameObject.name} 피격! 현재 체력: {MonsterHP}");

        if (MonsterHP <= 0f)
        {
            ChangeState(SlimeState.Die);
            Debug.Log($"{gameObject.name} 사망!");
        }
        else if (currentState != SlimeState.Attacked)
        {
            ChangeState(SlimeState.Attacked);
        }
    }
}
