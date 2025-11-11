// Assets/Scripts/PlayerFSM/PlayerMove.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Security.Cryptography;

public class PlayerMove : MonoBehaviour
{
    [Header("달리기 발동되는 키")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [SerializeField] public float _moveSpeed = 5f;
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

    [Header("잔상 효과")]
    [SerializeField] private GameObject _playerGhostPrefab;
    [SerializeField] private float _ghostSpawnInterval = 0.05f;
    [SerializeField] private Material _ghostMaterial;

    private SpriteRenderer _playerSpriteRenderer;
    private Coroutine _ghostRoutine;

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

    // ★ T001_Move 진행 신호를 한 번만 쏘기 위한 플래그
    private bool _sentMoveGoal = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
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

        // 대쉬 중
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0)
            {
                _isDashing = false;
                if (_ghostRoutine != null)
                {
                    StopCoroutine(_ghostRoutine);
                    _ghostRoutine = null;
                }
            }
            return;
        }

        // 점프 입력
        if (Input.GetKeyDown(jumpKey) && !isJump && !isAttacking && !isDie && !isDamaged)
        {
            if (_playerST.ST > _useJumpST)
            {
                StartJump();
                _playerST.ST -= _useJumpST;
                Debug.Log("점프 스태미나 소모됨.");
            }
        }

        // 점프 중에는 이동/대쉬 입력 받지 않음
        if (isJump)
        {
            _input = Vector2.zero;
            _animator.SetFloat(_horizontal, 0);
            _animator.SetFloat(_vertical, 0);
            return;
        }

        // ─────────────────────────────────────────────
        // 이동 입력
        _input.Set(InputManager.Movement.x, InputManager.Movement.y);
        _animator.SetFloat(_horizontal, _input.x);
        _animator.SetFloat(_vertical, _input.y);

        // ★ 첫 이동 순간에만 퀘스트 진행(Goal Key = "PlayerMove")
        if (!_sentMoveGoal && _input.sqrMagnitude > 0.0001f)
        {
            _sentMoveGoal = true;
            QuestManager.Instance?.UpdateGoal("PlayerMove");
            // TutorialUI.Instance?.Show("마을장에게 말을 걸어보자 (F)"); // 필요 시 다음 힌트
        }
        // ─────────────────────────────────────────────

        if (_input != Vector2.zero)
        {
            _animator.SetFloat(_lastHorizontal, _input.x);
            _animator.SetFloat(_lastVertical, _input.y);
            _lastDirection = _input.normalized;
        }

        // 더블 탭 대쉬
        if (_dashCooldownTimer <= 0 && !isAttacking && !isDie && !isDamaged)
        {
            if (_playerST.ST > _useDashST)
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    HandleDoubleTap(KeyCode.W, Vector2.up);
                }
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    HandleDoubleTap(KeyCode.S, Vector2.down);
                }
                else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    HandleDoubleTap(KeyCode.A, Vector2.left);
                }
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
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

        if (_playerGhostPrefab != null && _ghostRoutine == null)
            _ghostRoutine = StartCoroutine(SpawnGhostsRoutine());
    }

    private void StartJump()
    {
        isJump = true;
        _animator.SetTrigger("Jump");
        SetVelocity(Vector2.zero);
    }

    public void OnJumpAnimationEnd() => isJump = false;

    private IEnumerator SpawnGhostsRoutine()
    {
        while (_isDashing)
        {
            SpawnGhost();
            yield return new WaitForSeconds(_ghostSpawnInterval);
        }
    }

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
            ghost.SetupGhost(
                _playerSpriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                _playerSpriteRenderer.sortingOrder,
                _ghostMaterial,
                _playerSpriteRenderer.flipX
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

        // 점프/전투/피격/사망 시 이동 정지
        if (isAttacking || isDie || isDamaged || isJump)
        {
            SetVelocity(Vector2.zero);
            return;
        }

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
