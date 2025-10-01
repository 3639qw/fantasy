using UnityEngine;

public class MineableRock : MonoBehaviour
{
    // <<-- 변경: Sprite 대신 ItemData를 사용하여 어떤 아이템을 줄지 결정합니다.
    [Header("Loot (optional)")]
    public ItemData oreItemData;
    [Min(1)] public int oreAmount = 1;

    [Header("Deactivate Options")]
    public bool destroyInstead = false;

    bool _mined, _lootGiven;
    Collider2D _col;
    SpriteRenderer _sr;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>곡괭이 1회 타격 시 호출</summary>
    public void MineOnce()
    {
        if (_mined) return;
        _mined = true;

        if (_col) _col.enabled = false;
        if (_sr) _sr.enabled = false;
        gameObject.tag = "Untagged";

        GiveLootOnce();

        if (destroyInstead) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    void GiveLootOnce()
    {
        if (_lootGiven) return;
        _lootGiven = true;

        // <<-- 변경: oreIcon 대신 oreItemData를 확인합니다.
        if (oreItemData == null || oreAmount <= 0) return;

        var inv = Inventory.Instance ?? FindObjectOfType<Inventory>(true);
        
        // <<-- 변경: AddItem 메서드에 oreItemData(ItemData)를 전달합니다.
        if (inv) inv.AddItem(oreItemData, oreAmount);
    }
}