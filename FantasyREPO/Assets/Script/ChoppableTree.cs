using UnityEngine;

public class ChoppableTree : MonoBehaviour
{
    [Header("What to swap to when chopped")]
    public Sprite stumpSprite;
    public GameObject stumpPrefab;
    public bool destroyTreeObject = false;

    [Header("Loot")]
    public Sprite logIcon;
    [Min(1)] public int logAmount = 1;

    bool _chopped, _lootGiven;
    SpriteRenderer _sr;
    Collider2D _col;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    public void ChopOnce()
    {
        if (_chopped) return;
        _chopped = true;

        GiveLootOnce();

        if (stumpPrefab) // ✅ 프리팹을 쓰는 경우
        {
            var stump = Instantiate(stumpPrefab, transform.position, Quaternion.identity, transform.parent);

            // 새 스텀프가 같은 정렬 레이어/순서를 쓰도록 맞춰주면 겹침 문제 방지
            var pr = stump.GetComponent<SpriteRenderer>();
            if (pr && _sr) { pr.sortingLayerID = _sr.sortingLayerID; pr.sortingOrder = _sr.sortingOrder; }

            if (destroyTreeObject)
            {
                Destroy(gameObject);             // 완전히 교체
            }
            else
            {
                HideSpriteAndDisableCollider();  // 원래 트리의 시각은 숨기고 충돌만 끔
            }
        }
        else // ✅ 스프라이트 교체만 하는 경우
        {
            if (_sr && stumpSprite) _sr.sprite = stumpSprite; // 바꿔 끼우기
            DisableColliderOnly();                             // 시각은 유지!
        }

        // 더는 상호작용되지 않게 태그 제거
        gameObject.tag = "Untagged";
    }

    void GiveLootOnce()
    {
        if (_lootGiven) return;
        _lootGiven = true;
        var inv = Inventory.Instance ?? FindObjectOfType<Inventory>(true);
        if (inv && logIcon) inv.AddItem(logIcon, logAmount);
    }

    // 프리팹 경로에서 사용: 시각까지 숨김
    void HideSpriteAndDisableCollider()
    {
        if (_col) _col.enabled = false;
        if (_sr) _sr.enabled = false; // ← 이건 프리팹일 때만!
    }

    // 스프라이트 교체 경로에서 사용: 콜라이더만 끔
    void DisableColliderOnly()
    {
        if (_col) _col.enabled = false;
        // _sr.enabled 는 그대로 둔다
    }
}
