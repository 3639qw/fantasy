using UnityEngine;

public class ChargeGhost : MonoBehaviour
{
    [Header("효과 설정")]
    [SerializeField] private float _duration = 0.25f; // 효과 지속 시간
    [SerializeField] private float _endScale = 1.5f; // 목표 크기 (원본의 1.5배)
    [SerializeField] private float _startAlpha = 0.1f; // 시작 투명도
    [SerializeField] private float _endAlpha = 0.5f; // 최대 투명도
    [SerializeField] private Color _ghostColor = Color.yellow; // 강조 색상 (노란색 등)

    private SpriteRenderer _spriteRenderer;
    private Vector3 _startScale;
    private float _timer = 0f;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _startScale = transform.localScale; // Setup에서 설정된 스케일을 시작 스케일로 사용
    }

    void Update()
    {
        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / _duration); // 0.0 ~ 1.0 사이의 진행률

        // 시간에 따라 스케일과 알파 값을 Lerp (선형 보간)
        transform.localScale = Vector3.Lerp(_startScale, _startScale * _endScale, t);
        float currentAlpha = Mathf.Lerp(_startAlpha, _endAlpha, t);
        _spriteRenderer.color = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, currentAlpha);

        // 지속 시간이 끝나면 파괴
        if (_timer >= _duration)
        {
            Destroy(gameObject);
        }
    }

    // 이 스크립트도 원본의 정보를 복사해야 함
    public void Setup(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, bool flipX, int sortingOrder, Material material)
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

        _spriteRenderer.sprite = sprite;
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale; // 이 스케일이 Awake에서 _startScale이 됨
        _spriteRenderer.flipX = flipX;
        _spriteRenderer.sortingOrder = sortingOrder - 1;
        
        if (material != null)
        {
            _spriteRenderer.material = material;
        }

        // Awake에서 설정된 초기 알파 값을 덮어씀
        _spriteRenderer.color = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, _startAlpha);
    }
}