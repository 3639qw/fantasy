using UnityEngine;

public class MineableRock : MonoBehaviour
{
    // 어떤 아이템을 드랍할지 (기존 유지)
    [Header("Loot (optional)")]
    public ItemData oreItemData;
    [Min(1)] public int oreAmount = 1;

    [Header("HP (필요 타수)")]
    [Tooltip("기본 4로 두면 Copper(1)는 4타, Iron(2)는 2타")]
    [Min(1)] public int baseHitsRequired = 4;
    private int _hp;

    [Header("Deactivate Options")]
    public bool destroyInstead = false;

    private bool _mined, _lootGiven;
    private Collider2D _col;
    private SpriteRenderer _sr;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr  = GetComponent<SpriteRenderer>();
        _hp  = baseHitsRequired;
    }

    void OnEnable()
    {
        // 풀링을 고려해 HP/플래그 리셋
        _hp = baseHitsRequired;
        _mined = false;
        _lootGiven = false;
        // 필요 시 아래 두 줄을 풀면 재활성화 때 콜라이더/스프라이트도 원복됩니다.
        // if (_col) _col.enabled = true;
        // if (_sr)  _sr.enabled = true;
    }

    /// <summary>
    /// 곡괭이로 타격: Attack Power만큼 HP 감소
    /// CopperPick=1 → 4타, IronPick=2 → 2타
    /// </summary>
    public void Hit(int toolPower)
    {
        if (_mined) return;
        if (toolPower <= 0) toolPower = 1;

        _hp -= toolPower;
        // Debug.Log($"[Rock] Hit power={toolPower}, hp={_hp}");

        if (_hp <= 0)
            MineOnce();
    }

    /// <summary>
    /// 기존 즉시 채굴 함수(마무리 처리). HP가 0 이하일 때만 호출되도록 유지.
    /// </summary>
    public void MineOnce()
    {
        if (_mined) return;
        _mined = true;

        if (_col) _col.enabled = false;
        if (_sr)  _sr.enabled = false;
        gameObject.tag = "Untagged";

        GiveLootOnce();

        if (destroyInstead) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    private void GiveLootOnce()
    {
        if (_lootGiven) return;
        _lootGiven = true;

        if (oreItemData == null || oreAmount <= 0) return;

        var inv = Inventory.Instance ?? FindObjectOfType<Inventory>(true);
        if (inv) inv.AddItem(oreItemData, oreAmount);
    }
}
