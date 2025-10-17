using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Security.Cryptography;

public class PlayerMove : MonoBehaviour
{
    [Header("달리기 발동되는 키")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _runSpeed = 8f;
    [SerializeField] private Tilemap tilemap; // 이동 경계 타일맵

    [Header("대쉬 (더블클릭)")]
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashTime = 0.2f;
    [SerializeField] private float _dashCooldown = 1f;
    [SerializeField] private float _doubleTapTimeWindow = 0.3f;
    [SerializeField] private float _useDashST = 5f;

    [Header("점프 키")]
    [SerializeField] private KeyCode jumpKey = KeyCode.C;
    [SerializeField] private float _useJumpST = 10f;

    // --- 잔상 효과 추가 ---
    [Header("잔상 효과")]
    [SerializeField] private GameObject _playerGhostPrefab; // 잔상 프리팹
    [SerializeField] private float _ghostSpawnInterval = 0.05f; // 잔상 생성 간격
    [SerializeField] private Material _ghostMaterial; // 잔상에 적용할 Material (없으면 SpriteRenderer에 설정된 기본 Material 사용)

    private SpriteRenderer _playerSpriteRenderer; // 플레이어의 SpriteRenderer
    
    private Coroutine _ghostRoutine; // 잔상 생성 코루틴 참조
    // --- 잔상 효과 추가 끝 ---

    private bool _isDashing = false;
    private float _dashTimer = 0f;
    private float _dashCooldownTimer = 0f;
    private Vector2 _dashDirection;

    private float _lastTapTime = -1f;
    private KeyCode _lastTapKey = KeyCode.None;

    private Vector2 _input;
    private Rigidbody2D _rb;
    private Animator _animator;

    public bool isAttacking = false;
    public bool isDie = false;
    public bool isDamaged = false;
    public bool isJump = false;

    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";
    private const string _lastHorizontal = "LastHorizontal";
    private const string _lastVertical = "LastVertical";

    private Vector2 _lastDirection = Vector2.down;
    public Vector2 LastDirection => _lastDirection;

    private Bounds _tilemapBounds;
    private float _halfWidth = 0.25f;
    private float _halfHeight = 0.25f;

    private GameManager _playerST;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer 참조 추가
        _playerST = FindObjectOfType<GameManager>();

        if (tilemap != null)
        {
            tilemap.CompressBounds();
            _tilemapBounds = tilemap.localBounds;
        }
        else
        {
            Debug.LogWarning("Tilemap이 지정되지 않았습니다.");
        }
    }

    void Update()
    {
        if (_dashCooldownTimer > 0) _dashCooldownTimer -= Time.deltaTime;

        // 대쉬 중일 때
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0)
            {
                _isDashing = false;
                if (_ghostRoutine != null) // 대쉬 종료 시 잔상 생성 코루틴 중지
                {
                    StopCoroutine(_ghostRoutine);
                    _ghostRoutine = null;
                }
            }
            return;

        }

        // 1. 점프 입력 감지
        // 점프, 공격, 사망, 피격 중이 아닐 때만 점프 가능
        if (Input.GetKeyDown(jumpKey) && !isJump && !isAttacking && !isDie && !isDamaged)
        {
            if (_playerST.ST > _useJumpST)
            {
                StartJump();
                _playerST.ST -= _useJumpST;
                Debug.Log("점프 스태미나 소모됨.");
            }
            
        }

        // 2. 점프 중일 때의 처리
        if (isJump)
        {
            // 점프 중에는 이동 및 대쉬 입력을 받지 않음
            _input = Vector2.zero;
            _animator.SetFloat(_horizontal, 0);
            _animator.SetFloat(_vertical, 0);
            return; // Update의 나머지 부분(이동/대쉬 입력)을 건너뜀
        }

        // 일반 이동 입력 (기존 로직)
        _input.Set(InputManager.Movement.x, InputManager.Movement.y);
        _animator.SetFloat(_horizontal, _input.x);
        _animator.SetFloat(_vertical, _input.y);

        if (_input != Vector2.zero)
        {
            _animator.SetFloat(_lastHorizontal, _input.x);
            _animator.SetFloat(_lastVertical, _input.y);
            _lastDirection = _input.normalized;
        }

        // 더블 탭 대쉬 입력 감지
        
        if (_dashCooldownTimer <= 0 && !isAttacking && !isDie && !isDamaged)
        {
            if (_playerST.ST > _useDashST)
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) && _playerST.ST > _useDashST)
                {
                    HandleDoubleTap(KeyCode.W, Vector2.up);
                    
                }
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) && _playerST.ST > _useDashST)
                {
                    HandleDoubleTap(KeyCode.S, Vector2.down);
                }
                else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) && _playerST.ST > _useDashST)
                {
                    HandleDoubleTap(KeyCode.A, Vector2.left);
                }
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) && _playerST.ST > _useDashST)
                {
                    HandleDoubleTap(KeyCode.D, Vector2.right);
                }    
            }
        }
        
    }

    private void HandleDoubleTap(KeyCode key, Vector2 direction)
    {
        if (Time.time - _lastTapTime < _doubleTapTimeWindow && _lastTapKey == key)
        {
            StartDash(direction);
        }
        else
        {
            _lastTapTime = Time.time;
            _lastTapKey = key;
        }
    }

    private void StartDash(Vector2 direction)
    {
        _isDashing = true;
        _dashDirection = direction;
        _dashTimer = _dashTime;
        _dashCooldownTimer = _dashCooldown;

        _lastDirection = direction;
        _animator.SetFloat(_lastHorizontal, direction.x);
        _animator.SetFloat(_lastVertical, direction.y);

        _playerST.ST -= _useDashST;
        Debug.Log("대시 스테미나 소모됨.");

        // 잔상 생성 코루틴 시작
        if (_playerGhostPrefab != null && _ghostRoutine == null)
        {
            _ghostRoutine = StartCoroutine(SpawnGhostsRoutine());
        }
    }
    private void StartJump()
    {
        isJump = true;
        _animator.SetTrigger("Jump"); // 요청하신 "Jump" 트리거 발동

        // 점프 시 속도를 0으로 만듭니다 (FixedUpdate에서도 처리하지만, 즉각적인 반응을 위해 여기서도 호출)
        SetVelocity(Vector2.zero);
    }
    public void OnJumpAnimationEnd()
    {
        isJump = false;
    }
    // 잔상 생성 코루틴
    private IEnumerator SpawnGhostsRoutine()
    {
        while (_isDashing) // 대쉬 중일 때만 반복
        {
            SpawnGhost();
            yield return new WaitForSeconds(_ghostSpawnInterval);
        }
    }

    // 잔상 생성 함수
    private void SpawnGhost()
    {
        if (_playerGhostPrefab == null || _playerSpriteRenderer == null)
        {
            Debug.LogWarning("PlayerGhostPrefab 또는 PlayerSpriteRenderer가 할당되지 않았습니다.");
            return;
        }

        GameObject ghostObj = Instantiate(_playerGhostPrefab);
        PlayerGhost ghost = ghostObj.GetComponent<PlayerGhost>();

        if (ghost != null)
        {
            // 현재 플레이어의 시각적 정보를 잔상에 복사
            ghost.SetupGhost(
                _playerSpriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                _playerSpriteRenderer.sortingOrder,
                _ghostMaterial,
                _playerSpriteRenderer.flipX // --- 변경: flipX 값 추가 ---
            );
        }
        else
        {
            Debug.LogWarning("PlayerGhostPrefab에 PlayerGhost 스크립트가 없습니다.");
            Destroy(ghostObj);
        }
    }

    void FixedUpdate()
    {
        if (_isDashing)
        {
            ApplyClampedMovement(_dashDirection, _dashSpeed);
            return;
        }

        // --- 변경 ---
        // 점프, 전투, 피격, 사망 중에는 이동 정지
        if (isAttacking || isDie || isDamaged || isJump)
        {
            SetVelocity(Vector2.zero);
            return;
        }
        // --- 변경 끝 ---

        float speed = (Input.GetKey(runKey) && GameManager.Instance.ST > 0f) ? _runSpeed : _moveSpeed;

        Vector2 dir = _input;
        if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
        else dir = Vector2.zero;

        ApplyClampedMovement(dir, speed);
    }

    private void ApplyClampedMovement(Vector2 direction, float speed)
    {
        float dt = Time.fixedDeltaTime;
        Vector2 pos = _rb.position;
        Vector2 nextPos = pos + direction * speed * dt;

        Vector2 clamped = nextPos;
        if (tilemap != null)
        {
            clamped.x = Mathf.Clamp(nextPos.x, _tilemapBounds.min.x + _halfWidth, _tilemapBounds.max.x - _halfWidth);
            clamped.y = Mathf.Clamp(nextPos.y, _tilemapBounds.min.y + _halfHeight, _tilemapBounds.max.y - _halfHeight);
        }

        Vector2 vel = (dt > 1e-6f) ? (clamped - pos) / dt : Vector2.zero;

        if (!IsFinite(vel.x) || !IsFinite(vel.y))
        {
            Debug.LogWarning($"[PlayerMove] invalid vel {vel}; direction={direction}, speed={speed}, dt={dt}");
            vel = Vector2.zero;
        }

        SetVelocity(vel);
    }

    private void SetVelocity(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = v;
#else
        _rb.velocity = v;
#endif
        _rb.angularVelocity = 0f;
    }

    private static bool IsFinite(float f) => !(float.IsNaN(f) || float.IsInfinity(f));
}