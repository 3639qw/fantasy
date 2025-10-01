using UnityEngine;

public class AxeByLastMotion : MonoBehaviour
{
    [Header("Interact")]
    public KeyCode interactKey = KeyCode.Space;
    public float interactRange = 2f;
    public string treeTag = "Tree";
    public float cooldown = 0.35f;

    // <<-- 변경: Sprite 대신 ItemData로 도끼를 식별합니다.
    [Header("Inventory Gate (optional)")]
    public bool requireAxeSelected = false;
    public ItemData axeItemData;

    float _cool;

    void Update()
    {
        if (_cool > 0f) _cool -= Time.deltaTime;
        if (_cool <= 0f && Input.GetKeyDown(interactKey))
            TryChopNearestTree();
    }

    void TryChopNearestTree()
    {
        if (requireAxeSelected && !IsAxeSelected()) return;

        // 근처 나무 찾기 (가장 가까운 것 하나)
        var cols = Physics2D.OverlapCircleAll(transform.position, interactRange);
        Collider2D nearest = null; float best = float.MaxValue;

        foreach (var c in cols)
        {
            if (!c || (treeTag.Length > 0 && !c.CompareTag(treeTag))) continue;
            float sq = (c.transform.position - transform.position).sqrMagnitude;
            if (sq < best) { best = sq; nearest = c; }
        }
        if (!nearest) return;

        var tree = nearest.GetComponentInParent<ChoppableTree>();
        if (!tree) return;
        
        tree.ChopOnce();
        _cool = cooldown;
    }

    // <<-- 변경: ItemData를 가져와서 비교하도록 로직을 수정합니다.
    bool IsAxeSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null) return true; // 인벤토리 시스템 없으면 그냥 허용
        
        // 인벤토리에서 현재 선택된 '아이템 데이터'를 가져옵니다.
        var selectedItem = inv.GetSelectedItemData();
        
        // 지정된 도끼 데이터가 있고, 선택된 아이템이 그것과 일치하는지 확인합니다.
        if (axeItemData != null)
        {
            return selectedItem == axeItemData;
        }
        
        // 만약 axeItemData가 지정되지 않았다면, 그냥 빈 슬롯이 아닌지만 확인합니다.
        return !inv.IsSelectedEmpty();
    }
}