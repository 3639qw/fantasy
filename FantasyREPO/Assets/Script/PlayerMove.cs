using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMove : MonoBehaviour
{
    [Header("달리기 발동되는 키")] [SerializeField]
    private KeyCode runKey;
    
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _runSpeed = 8f;
    [SerializeField] private Tilemap tilemap; // 이동 경계 타일맵

    private Vector2 vector;
    private Rigidbody2D _rb;
    private Animator _animator;
    public bool isAttacking = false;
    public bool isDie = false;

    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";
    private const string _lastHorizontal = "LastHorizontal";
    private const string _lastVertical = "LastVertical";

    private Vector2 _lastDirection = Vector2.down;
    public Vector2 LastDirection => _lastDirection;

    private Bounds _tilemapBounds;
    private float _halfWidth = 0.25f;   // 캐릭터 크기의 절반 (조정 가능)
    private float _halfHeight = 0.25f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        // tilemap = GameObject.FindWithTag("FullBackground").GetComponent<Tilemap>();

        // ✅ 타일맵 경계 캐싱
        if (tilemap != null)
        {
            _tilemapBounds = tilemap.localBounds;
        }
        else
        {
            Debug.LogWarning("Tilemap이 지정되지 않았습니다.");
        }
    }

    private void Update()
    {
        vector.Set(InputManager.Movement.x, InputManager.Movement.y);
        
        float currentSpeed = (Input.GetKey(runKey) && GameManager.Instance.ST > 0)
            ? _runSpeed
            : _moveSpeed;


        Vector2 nextPos = (Vector2)transform.position + (vector * (currentSpeed * Time.deltaTime));

        Vector2 clampedPosition;

        if (tilemap != null)
        {
            // 경계 체크: Clamp
            float clampedX = Mathf.Clamp(
                nextPos.x,
                _tilemapBounds.min.x + _halfWidth,
                _tilemapBounds.max.x - _halfWidth
            );

            float clampedY = Mathf.Clamp(
                nextPos.y,
                _tilemapBounds.min.y + _halfHeight,
                _tilemapBounds.max.y - _halfHeight
            );

            clampedPosition = new Vector2(clampedX, clampedY);
            
        }
        else
        {
            // 타일맵 지정 안됐으면 제한 없이 이동
            clampedPosition = nextPos;
        }

        if (isAttacking || isDie)
        {
            // 공격 중일 땐 움직이지 않음
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;

            // 정지 애니메이션 처리 (선택사항)
            _animator.SetFloat(_horizontal, 0f);
            _animator.SetFloat(_vertical, 0f);
            return; // 여기서 함수 종료
        }
        
         // 이동 적용
        Vector2 velocity = (clampedPosition - (Vector2)transform.position) / Time.deltaTime;
        _rb.linearVelocity = velocity;

        // 애니메이션
        _animator.SetFloat(_horizontal, vector.x);
        _animator.SetFloat(_vertical, vector.y);
        
        

        if (vector != Vector2.zero)
        {
            _animator.SetFloat(_lastHorizontal, vector.x);
            _animator.SetFloat(_lastVertical, vector.y);
            _lastDirection = vector.normalized;
        }
    }

}
