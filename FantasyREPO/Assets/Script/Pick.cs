using UnityEngine;
using System.Collections.Generic;

// 광물 채굴용 Pick(곡괭이) 스크립트 - 애니메이션 출력 포함
// 요구사항:
// 1) Animator Float 파라미터: AttackX, AttackY
// 2) Animator Trigger: Pick     (도구별로 분리할 때 'Axe', 'Hoe' 등과 동일 컨벤션)
// 3) Inventory에 현재 선택 아이템이 pickSprite인지 검사(무기 게이트)
// 4) PlayerMove.isAttacking 게이트로 중복 동작 방지 (Melee와 동일 패턴)

[RequireComponent(typeof(Animator))]
public class Pick : MonoBehaviour
{
    [Header("탐지/상호작용")]
    public float interactRange = 1.8f;
    public string rockTag = "Rock";

    [Header("쿨타임")]
    public float coolTime = 0.5f;
    private float curTime;

    [Header("도구 선택 게이트(선택된 아이콘)")]
    [SerializeField] private Sprite pickSprite; // 현재 선택 슬롯 아이콘이 이 스프라이트여야 작동

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

        // 좌클릭 & 쿨타임 OK & 현재 공격 중 아님 & 선택한 도구가 '곡괭이'일 때만
        if (Input.GetMouseButtonDown(0) && curTime <= 0f && _playerMove != null && !_playerMove.isAttacking && IsPickSelected())
        {
            TryMine();
        }
    }

    private void TryMine()
    {
        // 주변 콜라이더 스캔
        var hits = Physics2D.OverlapCircleAll(transform.position, interactRange);
        if (hits == null || hits.Length == 0) return;

        var mined = false;
        var visited = new HashSet<GameObject>();

        foreach (var col in hits)
        {
            if (!col || !col.CompareTag(rockTag)) continue;

            // 같은 루트에 중복 타격 방지
            var root = col.transform.root.gameObject;
            if (visited.Contains(root)) continue;

            // 마우스 방향 계산 → AttackX/Y 세팅 (Melee와 동일 패턴)
            var dir = GetMouseWorldDir();
            _anim.SetFloat("AttackX", dir.x);
            _anim.SetFloat("AttackY", dir.y);
            _anim.SetTrigger("Pick");  // ★ Animator에 'Pick' 트리거 필요

            // 쿨타임/공격 상태
            curTime = coolTime;
            if (_playerMove) _playerMove.isAttacking = true;  // 이동 잠깐 막음 (Melee와 동일) :contentReference[oaicite:2]{index=2}

            // 채굴 처리 (타이밍을 이벤트로 빼고 싶으면 아래 줄을 주석 처리하고 애니메이션 이벤트에서 호출)
            var mineable = col.GetComponentInParent<MineableRock>();
            if (mineable) mineable.MineOnce();

            mined = true;
            visited.Add(root);
            break; // 한 번만 채굴
        }

        // 주변에 Rock이 없는데 클릭했다면 애니메이션만 재생하지 않도록 mined로 분기할 수도 있음
        if (!mined)
        {
            // 필요하면 여기서 무시
        }
    }

    /// <summary>애니메이션 끝에서 호출(클립 Event로 연결) — Melee의 EndAttack과 동일 역할</summary>
    public void EndPick()
    {
        _anim.ResetTrigger("Pick");
        _anim.SetFloat("AttackX", 0f);
        _anim.SetFloat("AttackY", 0f);
        if (_playerMove) _playerMove.isAttacking = false;  // 이동 가능 복원  :contentReference[oaicite:3]{index=3}
    }

    // 혹시 기존 클립이 EndAttack 이벤트를 쓰면 겸용할 수 있게 alias 제공
    public void EndAttack() => EndPick();

    private bool IsPickSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null || inv.IsSelectedEmpty()) return false;
        var sel = inv.GetSelectedSprite();
        return sel && sel == pickSprite; // Melee의 검 선택 검사와 동일 패턴  :contentReference[oaicite:4]{index=4}
    }

    private Vector2 GetMouseWorldDir()
    {
        var mouse = Input.mousePosition;
        if (_cam == null) _cam = Camera.main;
        if (_cam)
        {
            // 2D에서 정확한 평면 거리로 변환
            mouse.z = Mathf.Abs(_cam.transform.position.z - transform.position.z);
            var world = _cam.ScreenToWorldPoint(mouse);
            var v = (Vector2)world - (Vector2)transform.position;
            if (v.sqrMagnitude < 0.0001f) v = Vector2.right; // 제로 보호
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
