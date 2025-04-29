using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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

    [Header("룰타일 설정")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase grassTile;                 // Grass_1_Middle_0
    [SerializeField] private TileBase tilledTile;                // Grass_Tiles_1_41
    [SerializeField] private TileBase farmTile;
    [SerializeField] private TileBase wetfarmTile;

    private Vector2 _lastDirection = Vector2.down;

    [Header("스태미나 설정")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRecoverySpeed = 10f;
    [SerializeField] private float staminaConsumption = 5f;
    [SerializeField] private float staminaConsumptionFor3x3 = 15f;

    private float stamina;

    [Header("스태미나 UI (Image Fill)")]
    [SerializeField] private Image staminaFillImage;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        stamina = maxStamina;

        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = 1f;
        }
    }

    private void Update()
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

        RecoverStamina();

        // 스태미나 UI 업데이트
        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = stamina / maxStamina;
        }

        // 스페이스 키 입력
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 targetWorldPos = transform.position;
            Vector3Int tilePos = tilemap.WorldToCell(targetWorldPos);
            TileBase currentTile = tilemap.GetTile(tilePos);

            float requiredStamina = 0f;

            if (currentTile == grassTile)
                requiredStamina = staminaConsumption;
            else if (currentTile == tilledTile || currentTile == farmTile || currentTile == wetfarmTile)
                requiredStamina = staminaConsumptionFor3x3;

            if (stamina >= requiredStamina)
            {
                TryTillGround(); // 여기서 실제 타일 변경 + ConsumeStamina()
            }
            else
            {
                Debug.Log("스태미나가 부족해서 작업할 수 없습니다!");
            }
        }
    }

    void RecoverStamina()
    {
        if (stamina < maxStamina)
        {
            stamina += staminaRecoverySpeed * Time.deltaTime;
            if (stamina > maxStamina)
                stamina = maxStamina;
        }
    }

    void ConsumeStamina(float amount)
    {
        stamina -= amount;
        if (stamina < 0f)
        {
            stamina = 0f;
        }
    }

    void TryTillGround()
    {

        Vector3 targetWorldPos = transform.position;
        Vector3Int tilePos = tilemap.WorldToCell(targetWorldPos);

        TileBase currentTile = tilemap.GetTile(tilePos);

        if (currentTile == grassTile)
        {
            tilemap.SetTile(tilePos, tilledTile);
            ConsumeStamina(staminaConsumption); // 기본 소모
        }
        else if (currentTile == tilledTile)
        {
            Vector3Int centerPos = tilePos;
            if (Is3x3Tilled(centerPos))
            {
                Replace3x3WithFarm(centerPos, farmTile);
                ConsumeStamina(staminaConsumptionFor3x3); // 3x3 소모
            }
        }
        else
        {
            Vector3Int centerPos = tilePos;
            Replace3x3WithFarm(centerPos, wetfarmTile);
            ConsumeStamina(staminaConsumptionFor3x3); // 3x3 소모
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
