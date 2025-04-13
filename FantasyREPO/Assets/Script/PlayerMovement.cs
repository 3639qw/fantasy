using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private Vector2 vector;

    private Rigidbody2D _rb;
    private Animator _animator;

    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";
    private const string _lastHorizontal = "LastHorizontal";
    private const string _lastVertical = "LastVertical";

    // === Ÿ�� ���� ���� ===
    [Header("Ÿ�� ���� ����")]
    [SerializeField] private Tilemap tilemap;                    // Ÿ�ϸ�
    [SerializeField] private TileBase grassTile;                 // Grass_1_Middle_0
    [SerializeField] private TileBase tilledTile;                // Grass_Tiles_1_41

    private Vector2 _lastDirection = Vector2.down;  // ������ ���� ����

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        // === �̵� ó�� ===
        vector.Set(InputManager.Movement.x, InputManager.Movement.y);
        _rb.linearVelocity = vector * _moveSpeed;

        _animator.SetFloat(_horizontal, vector.x);
        _animator.SetFloat(_vertical, vector.y);

        if (vector != Vector2.zero)
        {
            _animator.SetFloat(_lastHorizontal, vector.x);
            _animator.SetFloat(_lastVertical, vector.y);
            _lastDirection = vector.normalized;
        }

        // === �� ���� (�����̽���) ===
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryTillGround();
        }
    }

    void TryTillGround()
    {
        Vector3 targetWorldPos = transform.position + (Vector3)_lastDirection;
        Vector3Int tilePos = tilemap.WorldToCell(targetWorldPos);

        TileBase currentTile = tilemap.GetTile(tilePos);

        if (currentTile == grassTile)
        {
            tilemap.SetTile(tilePos, tilledTile);
        }
    }
}
