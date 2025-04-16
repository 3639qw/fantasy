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

    [Header("룰타일")]
    [SerializeField] private Tilemap tilemap;                    
    [SerializeField] private TileBase grassTile;                 // Grass_1_Middle_0
    [SerializeField] private TileBase tilledTile;                // Grass_Tiles_1_41
    [SerializeField] private TileBase farmTile;
    [SerializeField] private TileBase wetfarmTile;

    private Vector2 _lastDirection = Vector2.down;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
    
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryTillGround();
        }
    }

    void TryTillGround()
{
    Vector3 targetWorldPos = transform.position;
    Vector3Int tilePos = tilemap.WorldToCell(targetWorldPos);

    TileBase currentTile = tilemap.GetTile(tilePos);

    if (currentTile == grassTile)
    {
        // 1단계: 풀 → 밭으로
        tilemap.SetTile(tilePos, tilledTile);
    }
    else if (currentTile == tilledTile)
    {
        // 2단계: 밭 상태에서 주변 3x3이 전부 밭이면 → 농장으로
        Vector3Int centerPos = tilePos; // 중심 타일로 간주
        if (Is3x3Tilled(centerPos))
        {
            Replace3x3WithFarm(centerPos,farmTile);
        }
    }
    else
    {
        Vector3Int centerPos = tilePos;
        Replace3x3WithFarm(centerPos,wetfarmTile);
        
        Debug.Log("abc");
    }
}

bool Is3x3Tilled(Vector3Int center)
{
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            Vector3Int checkPos = center + new Vector3Int(x, y, 0);
            if (tilemap.GetTile(checkPos) != tilledTile)
                return false;
        }
    }
    return true;
}

void Replace3x3WithFarm(Vector3Int center, TileBase tile)
{
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            Vector3Int changePos = center + new Vector3Int(x, y, 0);
            tilemap.SetTile(changePos, tile);
        }
    }
}

}