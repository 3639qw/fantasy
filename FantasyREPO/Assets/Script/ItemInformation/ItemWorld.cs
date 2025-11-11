using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ItemWorld : MonoBehaviour
{
    // --- 기본 정보 ---
    private ItemData _itemData;
    private int _amount = 1;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;

    // --- 1. 자석 기능 변수 (수정) ---
    [Header("자석 효과 (Magnet)")]
    [Tooltip("이 거리(반지름) 안으로 플레이어가 들어오면 아이템이 끌려갑니다.")]
    public float magnetRange = 5f;
    
    // [기존] public float magnetSpeed = 8f; // <-- 이 줄은 삭제합니다.

    // ▼▼▼▼▼ [신규] 아래 두 줄을 추가합니다. ▼▼▼▼▼
    [Tooltip("끌려오기 시작할 때의 최소 속도")]
    public float minMagnetSpeed = 2f;
    [Tooltip("플레이어에 거의 닿을 때의 최대 속도")]
    public float maxMagnetSpeed = 20f;
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    private Transform _playerTransform;
    private bool _isCollected = false;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("ItemWorld가 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
    }
    
    public void Initialize(ItemData itemData, int amount)
    {
        this._itemData = itemData;
        this._amount = amount;
        if (_spriteRenderer != null && itemData != null)
        {
            _spriteRenderer.sprite = itemData.itemIcon;
        }
    }

    // --- 3. Update() 로직 (수정) ---
    void Update()
    {
        if (_playerTransform == null || _isCollected || magnetRange <= 0)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        if (distance <= magnetRange)
        {
            if (_rb != null && !_rb.isKinematic)
            {
                _rb.linearVelocity = Vector2.zero;
            }

            // ▼▼▼▼▼ [핵심] 속도 계산 로직 (수정) ▼▼▼▼▼

            // 1. 거리 비율 계산 (t: 0.0 ~ 1.0)
            //    (멀리 있을수록 0.0, 플레이어에게 닿으면 1.0)
            float t = (magnetRange - distance) / magnetRange;

            // 2. 가속 곡선 적용 (Ease-In Quadratic: t * t)
            //    t가 0.1일 때(멈) 0.01, t가 0.9일 때(가까움) 0.81
            float easedT = t * t; 

            // 3. 최소 속도와 최대 속도 사이에서 '보간'
            float currentSpeed = Mathf.Lerp(minMagnetSpeed, maxMagnetSpeed, easedT);

            // 4. 아이템 이동
            Vector2 direction = (_playerTransform.position - transform.position).normalized;
            transform.position += (Vector3)direction * currentSpeed * Time.deltaTime;
            
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            // 픽업 판정 (기존과 동일)
            if (distance < 0.5f) 
            {
                CollectItem();
            }
        }
        else if (_rb != null && _rb.isKinematic)
        {
            _rb.isKinematic = false;
        }
    }
    
    private void CollectItem()
    {
        if (_isCollected || _itemData == null) return;
        _isCollected = true; 

        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(_itemData, _amount);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectItem();
        }
    }
}