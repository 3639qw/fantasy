using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// T003: 구리 광석 3개 수집 → 천사에게 상호작용(F) 시 완료.
/// - 기존 시스템(퀘스트 매니저/UI)은 건드리지 않음
/// - 인벤토리는 InventoryBridge.Count(...) 만 사용(읽기 전용)
/// - V표시(완료 마커) 토글, 대화/진행 마커 토글 지원
/// </summary>
public class AngelCopperQuestT003 : MonoBehaviour
{
    public enum State { NotStarted, InProgress, ReadyToTurnIn, Completed }

    [Header("Basic")]
    [Tooltip("퀘스트 식별자(원하면 UI에서 참고용)")]
    public string questId = "T003_Copper";
    [Tooltip("인벤토리에서 개수를 셀 아이템 ID 또는 Prefix (예: Copper, Copper_Ore 등)")]
    public string itemIdOrPrefix = "Copper";
    [Tooltip("필요 개수")]
    public int requiredCount = 3;

    [Header("Interaction")]
    public Transform player;                    // 플레이어 Transform
    public float interactRange = 1.8f;          // 상호작용 거리
    public KeyCode interactKey = KeyCode.F;     // 상호작용 키
    [Tooltip("완료 시 아이템을 소모할지 여부(기본: 소모 안함)")]
    public bool consumeOnComplete = false;

    [Header("Markers (optional)")]
    [Tooltip("완료 가능(V표시) 마커 오브젝트")]
    public GameObject readyMark;                // V표시
    [Tooltip("진행 중(?) 마커 오브젝트")]
    public GameObject progressMark;             // 물음표/말풍선 등 (있으면 연결)

    [Header("Events")]
    public UnityEvent onQuestStarted;
    public UnityEvent onQuestCompleted;
    public UnityEvent onTurnIn;                 // F키로 완료 순간

    public State state { get; private set; } = State.NotStarted;

    void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        ApplyMarker();
    }

    void Update()
    {
        // 아직 시작 안 했으면 대기
        if (state == State.NotStarted) return;

        // 진행 중이면 인벤토리 개수 체크
        if (state == State.InProgress)
        {
            int have = SafeCount(itemIdOrPrefix);
            if (have >= requiredCount)
            {
                state = State.ReadyToTurnIn;
                ApplyMarker();
            }
        }

        // 반납 조건: 완료 가능 + 플레이어가 근처 + F키
        if (state == State.ReadyToTurnIn && player != null)
        {
            if (Vector3.Distance(player.position, transform.position) <= interactRange)
            {
                if (Input.GetKeyDown(interactKey))
                {
                    if (consumeOnComplete)
                        TryConsume(itemIdOrPrefix, requiredCount);

                    state = State.Completed;
                    ApplyMarker();
                    onTurnIn?.Invoke();
                    onQuestCompleted?.Invoke();
                }
            }
        }
    }

    /// <summary> 외부(예: T002 완료 시 트리거) 또는 인스펙터 버튼으로 호출해도 됨. </summary>
    public void StartQuest()
    {
        if (state != State.NotStarted) return;
        state = State.InProgress;
        onQuestStarted?.Invoke();

        // 이미 조건을 만족하고 있었다면 즉시 전환
        if (SafeCount(itemIdOrPrefix) >= requiredCount)
            state = State.ReadyToTurnIn;

        ApplyMarker();
    }

    // -------- Helpers --------

    int SafeCount(string keyOrPrefix)
    {
        // 네 프로젝트의 InventoryBridge.Count(...) 사용
        // (없다면 0 반환)
        try { return InventoryBridge.Count(keyOrPrefix); }
        catch { return 0; }
    }

    void ApplyMarker()
    {
        if (readyMark) readyMark.SetActive(state == State.ReadyToTurnIn);
        if (progressMark) progressMark.SetActive(state == State.InProgress);
    }

    /// <summary>
    /// 아이템 소모가 필요할 때 시도.
    /// 네 인벤토리에 Remove 계열 API가 있으면 여기서 호출하도록 OPTIONAL 구현.
    /// (없으면 그냥 패스 → “소모 안함”으로 쓰면 됨)
    /// </summary>
    bool TryConsume(string keyOrPrefix, int amount)
    {
        // 1) InventoryBridge.Remove(...) 가 있다면 그걸 호출
        // 2) 또는 Inventory.Instance.RemoveById / Consume 등 네이밍에 맞춰 반영
        // 지금은 샘플로 false만 리턴(프로젝트에 맞게 구현 가능)
        return false;
    }
}
