using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMove : MonoBehaviour
{
    [Header("달리기 발동되는 키")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _runSpeed = 8f;
    [SerializeField] private Tilemap tilemap; // 이동 경계 타일맵

    private Vector2 _input;           // 입력 누적(업데이트에서 읽고 고정업데이트에서 사용)
    private Rigidbody2D _rb;
    private Animator _animator;

    public bool isAttacking = false;
    public bool isDie = false;
    public bool isDamaged = false;

    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";
    private const string _lastHorizontal = "LastHorizontal";
    private const string _lastVertical = "LastVertical";

    private Vector2 _lastDirection = Vector2.down;
    public Vector2 LastDirection => _lastDirection;

    private Bounds _tilemapBounds;
    private float _halfWidth = 0.25f;
    private float _halfHeight = 0.25f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        if (tilemap != null)
        {
            // 경계 최신화 (타일이 변경될 때 생기는 빈칸/여백 제거)
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
        // 입력만 읽기
        _input.Set(InputManager.Movement.x, InputManager.Movement.y);

        // 애니메이션 파라미터
        _animator.SetFloat(_horizontal, _input.x);
        _animator.SetFloat(_vertical, _input.y);

        if (_input != Vector2.zero)
        {
            _animator.SetFloat(_lastHorizontal, _input.x);
            _animator.SetFloat(_lastVertical, _input.y);
            _lastDirection = _input.normalized;
        }
    }

    void FixedUpdate()
    {
        // 전투/피격/사망 중에는 이동 정지
        if (isAttacking || isDie || isDamaged)
        {
            SetVelocity(Vector2.zero);
            return;
        }

        float speed = (Input.GetKey(runKey) && GameManager.Instance.ST > 0f) ? _runSpeed : _moveSpeed;

        // 0벡터 정규화 금지 (NaN 예방)
        Vector2 dir = _input;
        if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
        else dir = Vector2.zero;

        float dt = Time.fixedDeltaTime;               // 물리 프레임 델타
        Vector2 pos = _rb.position;
        Vector2 nextPos = pos + dir * speed * dt;     // 예측 위치

        // 경계 클램프
        Vector2 clamped = nextPos;
        if (tilemap != null)
        {
            clamped.x = Mathf.Clamp(nextPos.x, _tilemapBounds.min.x + _halfWidth, _tilemapBounds.max.x - _halfWidth);
            clamped.y = Mathf.Clamp(nextPos.y, _tilemapBounds.min.y + _halfHeight, _tilemapBounds.max.y - _halfHeight);
        }

        // 클램프된 목표를 향한 안전한 속도 계산
        Vector2 vel = (dt > 1e-6f) ? (clamped - pos) / dt : Vector2.zero;

        // NaN / Infinity 가드
        if (!IsFinite(vel.x) || !IsFinite(vel.y))
        {
            Debug.LogWarning($"[PlayerMove] invalid vel {vel}; input={_input}, speed={speed}, dt={dt}");
            vel = Vector2.zero;
        }

        SetVelocity(vel);
    }

    // Unity 버전에 따라 velocity/linearVelocity 명칭이 다를 수 있어 래퍼로 설정
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
