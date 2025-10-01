using UnityEngine;

public class ChoppableTree : MonoBehaviour
{
    [Header("What to swap to when chopped")]
    public Sprite stumpSprite;
    public GameObject stumpPrefab;
    public bool destroyTreeObject = false;

    // <<-- 변경: Sprite 대신 ItemData를 사용하여 어떤 아이템을 줄지 결정합니다.
    [Header("Loot")]
    public ItemData logItemData; 
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

        if (stumpPrefab)
        {
            var stump = Instantiate(stumpPrefab, transform.position, Quaternion.identity, transform.parent);
            var pr = stump.GetComponent<SpriteRenderer>();
            if (pr && _sr) { pr.sortingLayerID = _sr.sortingLayerID; pr.sortingOrder = _sr.sortingOrder; }

            if (destroyTreeObject)
            {
                Destroy(gameObject);
            }
            else
            {
                HideSpriteAndDisableCollider();
            }
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

        // <<-- 변경: AddItem 메서드에 logIcon(Sprite) 대신 logItemData(ItemData)를 전달합니다.
        if (inv && logItemData != null) 
        {
            inv.AddItem(logItemData, logAmount);
        }
    }

    void HideSpriteAndDisableCollider()
    {
        if (_col) _col.enabled = false;
        if (_sr) _sr.enabled = false;
    }

    void DisableColliderOnly()
    {
        if (_col) _col.enabled = false;
    }
}