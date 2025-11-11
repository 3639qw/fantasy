using UnityEngine;

public class MineableRock : MonoBehaviour
{
    [Header("Loot (optional)")]
    public GameObject itemWorldPrefab; // 1. ItemWorld 프리팹 (필수)
    public ItemData oreItemData;       // 2. 드랍할 광석 ItemData (필수)
    [Min(1)] public int oreAmount = 1;

    [Header("HP (필요 타수)")]
    [Tooltip("기본 4. 곡괭이 파워 1이면 4타, 파워 2면 2타")]
    [Min(1)] public int baseHitsRequired = 4;
    private int _hp;

    [Header("Deactivate Options")]
    public bool destroyInstead = false;

    private bool _mined; // '_lootGiven'은 _mined로 대체 가능하므로 삭제
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
        // 풀링(재활용)을 고려해 HP/플래그 리셋
        _hp = baseHitsRequired;
        _mined = false;
        
        // 재활성화 시 콜라이더/스프라이트도 원복
        if (_col) _col.enabled = true;
        if (_sr)  _sr.enabled = true;
    }

    /// <summary>
    /// 곡괭이로 타격: Attack Power만큼 HP 감소
    /// </summary>
    public void Hit(int toolPower)
    {
        if (_mined) return; // 이미 채굴됐으면 무시
        if (toolPower <= 0) toolPower = 1;

        _hp -= toolPower;
        // Debug.Log($"[Rock] Hit power={toolPower}, hp={_hp}");

        // (파티클/사운드 재생은 여기서)

        if (_hp <= 0)
        {
            MineOnce();
        }
    }

    /// <summary>
    /// HP가 0이 되어 채굴이 완료될 때 딱 한 번 호출됨
    /// </summary>
    public void MineOnce()
    {
        if (_mined) return; // 중복 실행 방지 (가장 중요)
        _mined = true;

        // 바위 비활성화
        if (_col) _col.enabled = false;
        if (_sr)  _sr.enabled = false;
        gameObject.tag = "Untagged";

        // 아이템 스폰
        GiveLootOnce();

        // 오브젝트 제거 또는 비활성화
        if (destroyInstead) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    private void GiveLootOnce()
    {
        // 1. 프리팹과 데이터가 둘 다 설정되었는지 확인
        if (itemWorldPrefab != null && oreItemData != null)
        {
            // 2. 프리팹을 월드에 생성 (바위의 현재 위치에)
            GameObject droppedItemObj = Instantiate(itemWorldPrefab, transform.position, Quaternion.identity);

            // 3. 생성된 오브젝트에서 ItemWorld 스크립트를 가져옴
            ItemWorld itemScript = droppedItemObj.GetComponent<ItemWorld>();

            // 4. 스크립트에 아이템 정보와 수량을 전달
            if (itemScript != null)
            {
                itemScript.Initialize(oreItemData, oreAmount);
            }
            else
            {
                Debug.LogError($"[MineableRock] 'ItemWorld_Prefab'에 ItemWorld.cs 스크립트가 없습니다!");
            }
        }
    }
}