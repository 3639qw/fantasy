using UnityEngine;
using System.Collections;
public class ChoppableTree : MonoBehaviour
{
    [Header("What to swap to when chopped")]
    public Sprite stumpSprite;
    public GameObject stumpPrefab;
    public bool destroyTreeObject = false;

    [Header("Loot")]
    public GameObject itemWorldPrefab;
    public ItemData logItemData;
    [Min(1)] public int logAmount = 1;

    [Header("HP (필요 타수)")]
    [Tooltip("기본 4로 두면 Copper(1)는 4타, Iron(2)는 2타")]
    [Min(1)] public int baseHitsRequired = 4;
    private int _hp;

    private bool _chopped, _lootGiven;
    private SpriteRenderer _sr;
    private Collider2D _col;

    [Tooltip("타격 시 흔들리는 시간 (예: 0.1초)")]
    public float shakeDuration = 0.1f;
    [Tooltip("타격 시 흔들리는 강도 (예: 0.05)")]
    public float shakeMagnitude = 0.05f;

    private Vector3 _originalPosition; // 바위의 원래 위치
    private Coroutine _shakeCoroutine; // 현재 실행 중인 쉐이크 코루틴

    void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        _hp = baseHitsRequired;
        
        _originalPosition = transform.position;
    }

    void OnEnable()
    {
        // 풀링 대비 초기화
        _hp = baseHitsRequired;
        _chopped = false;
        _lootGiven = false;

        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }
        transform.position = _originalPosition;
        // 필요 시 원복:
        // if (_col) _col.enabled = true;
        // if (_sr)  _sr.enabled = true;
    }

    /// <summary>
    /// 도끼 타격: Attack Power만큼 HP 감소
    /// CopperAxe=1 → 4타, IronAxe=2 → 2타
    /// </summary>
    public void Hit(int toolPower)
    {
        if (_chopped) return;
        if (toolPower <= 0) toolPower = 1;

        _hp -= toolPower;

        // (이전 쉐이크가 실행 중이면 중지하고 즉시 원위치)
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            transform.position = _originalPosition; 
        }
        // (새 쉐이크 코루틴 시작)
        _shakeCoroutine = StartCoroutine(ShakeEffect());

        if (_hp <= 0) ChopOnce();
    }

    /// <summary>
    /// 베기 완료(한 번만 실행)
    /// </summary>
    public void ChopOnce()
    {
        if (_chopped) return;
        _chopped = true;

        GiveLootOnce();

        if (stumpPrefab)
        {
            var stump = Instantiate(stumpPrefab, transform.position, Quaternion.identity, transform.parent);
            var pr = stump.GetComponent<SpriteRenderer>();
            if (pr && _sr) { pr.sortingLayerID = _sr.sortingLayerID; pr.sortingOrder = _sr.sortingOrder; }

            if (destroyTreeObject) Destroy(gameObject);
            else HideSpriteAndDisableCollider();
        }
        else
        {
            if (_sr && stumpSprite) _sr.sprite = stumpSprite;
            DisableColliderOnly();
        }

        gameObject.tag = "Untagged";
    }

    void GiveLootOnce()
    {
        if (itemWorldPrefab != null && logItemData != null)
        {
            GameObject droppedItemObj = Instantiate(itemWorldPrefab, transform.position, Quaternion.identity);

            ItemWorld itemScript = droppedItemObj.GetComponent<ItemWorld>();

            if (itemScript != null)
            {
                itemScript.Initialize(logItemData, logAmount);
            }
            else
            {
                Debug.LogError($"[ChoppableTree] 'ItemWorld_Prefab'에 ItemWorld.cs 스크립트가 없습니다!");
            }
        }
    }

    void HideSpriteAndDisableCollider()
    {
        if (_col) _col.enabled = false;
        if (_sr)  _sr.enabled = false;
    }

    void DisableColliderOnly()
    {
        if (_col) _col.enabled = false;
    }

        // ▼▼▼▼▼ 3. [추가] 쉐이크 코루틴 함수 ▼▼▼▼▼
    private IEnumerator ShakeEffect()
    {
        float timer = 0f;

        // 'shakeDuration' (예: 0.1초) 동안 반복
        while (timer < shakeDuration)
        {
            // 'shakeMagnitude' (예: 0.05) 만큼 X, Y 위치를 랜덤하게 흔듦
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            
            // (2D 게임이므로 z축은 원래 값으로 고정)
            transform.position = _originalPosition + new Vector3(x, y, 0);

            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // [중요] 루프가 끝나면 정확히 원래 위치로 복구
        transform.position = _originalPosition;
        _shakeCoroutine = null; // 코루틴 참조 제거
    }
}
