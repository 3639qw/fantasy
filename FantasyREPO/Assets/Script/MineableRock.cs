using UnityEngine;

public class MineableRock : MonoBehaviour
{
    [Header("Loot (optional)")]
    public Sprite oreIcon;            // 인벤토리 아이콘 (없으면 지급 생략)
    [Min(1)] public int oreAmount = 1;

    [Header("Deactivate Options")]
    public bool destroyInstead = false;   // true면 Destroy, false면 SetActive(false)

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

        // 더 못 건드리게 즉시 상호작용 차단
        if (_col) _col.enabled = false;
        // (원하면 시각도 즉시 숨기기)
        if (_sr) _sr.enabled = false;
        // 태그는 깔끔하게 지워둠(선택)
        gameObject.tag = "Untagged";

        GiveLootOnce();

        // 파괴 or 비활성화
        if (destroyInstead) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    void GiveLootOnce()
    {
        if (_lootGiven) return;
        _lootGiven = true;

        if (!oreIcon || oreAmount <= 0) return;

        var inv = Inventory.Instance ?? FindObjectOfType<Inventory>(true);
        if (inv) inv.AddItem(oreIcon, oreAmount);
    }
}
