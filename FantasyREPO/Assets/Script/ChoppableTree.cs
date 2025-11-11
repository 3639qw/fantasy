using UnityEngine;

public class ChoppableTree : MonoBehaviour
{
    [Header("What to swap to when chopped")]
    public Sprite stumpSprite;
    public GameObject stumpPrefab;
    public bool destroyTreeObject = false;

    [Header("Loot")]
    public ItemData logItemData;
    [Min(1)] public int logAmount = 1;

    [Header("HP (필요 타수)")]
    [Tooltip("기본 4로 두면 Copper(1)는 4타, Iron(2)는 2타")]
    [Min(1)] public int baseHitsRequired = 4;
    private int _hp;

    private bool _chopped, _lootGiven;
    private SpriteRenderer _sr;
    private Collider2D _col;

    void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        _hp  = baseHitsRequired;
    }

    void OnEnable()
    {
        // 풀링 대비 초기화
        _hp = baseHitsRequired;
        _chopped = false;
        _lootGiven = false;
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
        if (_lootGiven) return;
        _lootGiven = true;

        var inv = Inventory.Instance ?? FindObjectOfType<Inventory>(true);
        if (inv && logItemData != null)
            inv.AddItem(logItemData, logAmount);
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
}
