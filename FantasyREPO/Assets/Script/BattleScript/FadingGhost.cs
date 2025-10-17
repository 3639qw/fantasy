using UnityEngine;

public class FadingGhost : MonoBehaviour
{
    [SerializeField] private float _fadeTime = 0.5f; // 잔상이 사라지는 시간
    [SerializeField] private float _startAlpha = 0.5f; // 시작 투명도
    [SerializeField] private Color _ghostColor = Color.white; // 잔상 색상

    private SpriteRenderer _spriteRenderer;
    private float _currentAlpha;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _currentAlpha = _startAlpha;
        _spriteRenderer.color = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, _currentAlpha);
    }

    void Update()
    {
        // 알파 값을 시간에 따라 감소
        _currentAlpha -= _startAlpha * (Time.deltaTime / _fadeTime);
        _spriteRenderer.color = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, _currentAlpha);

        // 알파 값이 0 이하가 되면 소멸
        if (_currentAlpha <= 0)
        {
            Destroy(gameObject);
        }
    }

    // 잔상이 될 때 원본의 시각적 정보를 복사하는 함수
    public void Setup(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, bool flipX, int sortingOrder, Material material)
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

        _spriteRenderer.sprite = sprite;
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
        _spriteRenderer.flipX = flipX;
        _spriteRenderer.sortingOrder = sortingOrder - 1; // 원본보다 뒤에
        
        if (material != null)
        {
            _spriteRenderer.material = material;
        }
    }
}