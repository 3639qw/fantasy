using UnityEngine;

public class Pick : MonoBehaviour
{
    [Header("Interact")]
    public KeyCode interactKey = KeyCode.Space;
    public float interactRange = 2f;
    public string rockTag = "Rock";
    public float cooldown = 0.35f;

    [Header("Inventory Gate (optional)")]
    public bool requirePickaxeSelected = false;
    public Sprite pickaxeSprite;

    float _cool;

    void Update()
    {
        if (_cool > 0f) _cool -= Time.deltaTime;
        if (_cool <= 0f && Input.GetKeyDown(interactKey))
            TryMineNearestRock();
    }

    void TryMineNearestRock()
    {
        if (requirePickaxeSelected && !IsPickaxeSelected()) return;

        // 근처 바위 찾기 (가장 가까운 것 하나)
        var cols = Physics2D.OverlapCircleAll(transform.position, interactRange);
        Collider2D nearest = null; float best = float.MaxValue;

        foreach (var c in cols)
        {
            if (!c || (rockTag.Length > 0 && !c.CompareTag(rockTag))) continue;
            float sq = (c.transform.position - transform.position).sqrMagnitude;
            if (sq < best) { best = sq; nearest = c; }
        }
        if (!nearest) return;

        var rock = nearest.GetComponentInParent<MineableRock>();
        if (!rock) return;

        // 바로 1회 채굴 (애니 없음)
        rock.MineOnce();
        _cool = cooldown;
    }

    bool IsPickaxeSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null) return true; // 인벤토리 시스템 없으면 그냥 허용
        var sel = inv.GetSelectedSprite();
        if (pickaxeSprite && sel) return sel == pickaxeSprite;
        return !inv.IsSelectedEmpty();
    }
}
