using UnityEngine;

public class PlayerGhost : MonoBehaviour
{
    [SerializeField] private float _fadeTime = 0.5f; // 잔상이 완전히 사라지는 시간
    [SerializeField] private float _startAlpha = 0.5f; // 시작 투명도 (0~1)
    [SerializeField] private Color _ghostColor = Color.white; // 잔상 색상 (선택 사항)

    private SpriteRenderer _spriteRenderer;
    private float _currentAlpha;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _currentAlpha = _startAlpha;

        // 초기 색상 설정
        _spriteRenderer.color = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, _currentAlpha);
    }

    void Update()
    {
        _currentAlpha -= _startAlpha * (Time.deltaTime / _fadeTime);
        _spriteRenderer.color = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, _currentAlpha);

        if (_currentAlpha <= 0)
        {
            Destroy(gameObject);
        }
    }

    // 잔상이 될 때 플레이어의 정보를 복사하는 함수
    // --- 변경: playerFlipX 매개변수 추가 ---
    public void SetupGhost(Sprite playerSprite, Vector3 playerPosition, Quaternion playerRotation, Vector3 playerScale, int playerSortingOrder, Material ghostMaterial, bool playerFlipX)
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

        _spriteRenderer.sprite = playerSprite;
        transform.position = playerPosition;
        transform.rotation = playerRotation;
        transform.localScale = playerScale;
        _spriteRenderer.sortingOrder = playerSortingOrder - 1; // 플레이어보다 뒤에 그려지도록

        // --- 변경: playerFlipX 값 적용 ---
        _spriteRenderer.flipX = playerFlipX;

        if (ghostMaterial != null)
        {
            _spriteRenderer.material = ghostMaterial;
        }
    }
}