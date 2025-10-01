using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class Pick : MonoBehaviour
{
    [Header("탐지/상호작용")]
    public float interactRange = 1.8f;
    public string rockTag = "Rock";

    [Header("쿨타임")]
    public float coolTime = 0.5f;
    private float curTime;

    // <<-- 변경: Sprite 대신 ItemData로 곡괭이를 식별합니다.
    [Header("도구 선택 게이트(선택된 아이템 데이터)")]
    [SerializeField] private ItemData pickItemData;

    private Animator _anim;
    private PlayerMove _playerMove;
    private Camera _cam;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _playerMove = GetComponent<PlayerMove>();
        _cam = Camera.main;
    }

    void Update()
    {
        if (curTime > 0f) curTime -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0) && curTime <= 0f && _playerMove != null && !_playerMove.isAttacking && IsPickSelected())
        {
            TryMine();
        }
    }

    private void TryMine()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, interactRange);
        if (hits == null || hits.Length == 0) return;

        var visited = new HashSet<GameObject>();

        foreach (var col in hits)
        {
            if (!col || !col.CompareTag(rockTag)) continue;

            var root = col.transform.root.gameObject;
            if (visited.Contains(root)) continue;

            var dir = GetMouseWorldDir();
            _anim.SetFloat("AttackX", dir.x);
            _anim.SetFloat("AttackY", dir.y);
            _anim.SetTrigger("Pick");

            curTime = coolTime;
            if (_playerMove) _playerMove.isAttacking = true;

            var mineable = col.GetComponentInParent<MineableRock>();
            if (mineable) mineable.MineOnce();

            visited.Add(root);
            break; 
        }
    }

    public void EndPick()
    {
        _anim.ResetTrigger("Pick");
        _anim.SetFloat("AttackX", 0f);
        _anim.SetFloat("AttackY", 0f);
        if (_playerMove) _playerMove.isAttacking = false;
    }

    public void EndAttack() => EndPick();

    // <<-- 변경: ItemData를 가져와서 비교하도록 로직을 수정합니다.
    private bool IsPickSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null || inv.IsSelectedEmpty()) return false;
        
        // 인벤토리에서 현재 선택된 '아이템 데이터'를 가져옵니다.
        var selectedItem = inv.GetSelectedItemData();
        
        // 선택된 아이템이 있고, 그것이 우리가 지정한 'pickItemData'와 일치하는지 확인합니다.
        return selectedItem != null && selectedItem == pickItemData;
    }

    private Vector2 GetMouseWorldDir()
    {
        var mouse = Input.mousePosition;
        if (_cam == null) _cam = Camera.main;
        if (_cam)
        {
            mouse.z = Mathf.Abs(_cam.transform.position.z - transform.position.z);
            var world = _cam.ScreenToWorldPoint(mouse);
            var v = (Vector2)world - (Vector2)transform.position;
            if (v.sqrMagnitude < 0.0001f) v = Vector2.right;
            return v.normalized;
        }
        return Vector2.right;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}