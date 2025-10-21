using UnityEngine;
using System.Collections;

public class PlayerBow : MonoBehaviour
{
    // [기존 변수]
    public float attackDamage = 10f;

    [Header("Weapon Gate (Selected ItemData)")]
    [SerializeField] private ItemData bowItemData;
    [SerializeField] private ItemData arrowItemData;

    private float curTime;
    private PlayerMove _playerMove;
    private Rigidbody2D _rb;
    private Animator _animator; // Awake에서 할당된 Animator 변수

    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float attackCooldown = 1f;

    [Header("Bow Settings")]
    public float arrowSpeed = 15f; // 화살 속도 변수 추가


    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerMove = GetComponent<PlayerMove>();
    }

    void Update()
    {
        // 쿨다운 타이머
        if (curTime > 0)
        {
            curTime -= Time.deltaTime;
        }

        // 공격 입력 확인 (화살 보유 여부 체크 추가)
        if (Input.GetMouseButtonDown(0) && curTime <= 0 && _playerMove != null && !_playerMove.isAttacking && IsBowSelected() && IsHavingArrow())
        {
            PerformAttack();
            Debug.Log("화살 발사됨!");
        }
    }

    private bool IsBowSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null || inv.IsSelectedEmpty())
            return false;

        // [수정] 'bowItemData' 타입을 'ItemData'로 변경
        ItemData selectedItem = inv.GetSelectedItemData();

        return selectedItem != null && selectedItem == bowItemData;
    }

    private bool IsHavingArrow()
    {
        var inv = Inventory.Instance;
        if (inv == null) // 인스턴스 존재 여부만 확인
            return false;
        
        // 새로 추가한 GetItemQuantity 메서드를 호출하여
        // arrowItemData의 수량이 0보다 큰지 확인합니다.
        return inv.GetItemQuantity(arrowItemData) > 0;
    }

    void PerformAttack()
    {
        // 쿨다운 설정
        curTime = attackCooldown;

        // 플레이어 상태를 '공격 중'으로 변경 (애니메이션 이벤트로 해제)
        _playerMove.isAttacking = true;

        // --- 공격 방향 계산 (기존 코드) ---
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 attackDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;
        attackDirection.Normalize();

        // --- 애니메이터 설정 (변수명 수정) ---
        _animator.SetFloat("AttackX", attackDirection.x);
        _animator.SetFloat("AttackY", attackDirection.y);
        _animator.SetTrigger("Bow");

        // --- [추가] 화살 생성 및 발사 로직 ---

        // 화살 생성 위치 (arrowSpawnPoint가 없으면 플레이어 위치)
        Vector3 spawnPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;

        // 화살 회전값 계산
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        angle += 90f;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // 화살 생성
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, rotation);
        
        // 화살 Rigidbody2D를 가져와서 속도 적용
        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        if (arrowRb != null)
        {
            arrowRb.linearVelocity = attackDirection * arrowSpeed;
        }
        
        // (선택 사항) 화살 프리팹에 'Arrow.cs' 같은 스크립트가 있다면
        // 데미지 값을 넘겨줄 수 있습니다.
        // Arrow arrowScript = arrow.GetComponent<Arrow>();
        // if (arrowScript != null)
        // {
        //     arrowScript.damage = attackDamage;
        // }

        // --- [추가] 화살 소모 ---
        // Inventory.Instance에 아이템을 제거하는 메서드가 있다고 가정합니다.
        Inventory.Instance.RemoveItem(arrowItemData, 1);
    }

    // --- [추가] 애니메이션 이벤트용 메서드 ---
    // 'Bow' 애니메이션 클립의 마지막 프레임에
    // 'OnAttackAnimationFinished' 이름으로 Animation Event를 추가하세요.
    public void OnAttackAnimationFinished()
    {
        if (_playerMove != null)
        {
            _playerMove.isAttacking = false;
        }
    }
}