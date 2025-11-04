// Assets/Scripts/Quest/QuestManager.cs
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Quests")]
    public List<Quest> activeQuests = new();
    public List<Quest> completedQuests = new();

    // 완료 후, F로 확인하기 전까지 V 아이콘을 유지해야 하는 퀘스트
    private readonly HashSet<Quest> pendingCompleteAcks = new();

    // 완료 순간 알림(옵션)
    public event Action<Quest> OnQuestCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[QuestManager] 중복 인스턴스 발견 → 삭제됨");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  // << 반드시 유지
    }

    /* =========================
     * 상태 확인 / 아이콘 헬퍼
     * ========================= */
    public bool HasQuest(Quest q) => q && activeQuests.Contains(q);
    public bool IsCompleted(Quest q) => q && (completedQuests.Contains(q) || q.isCompleted);
    public bool ShouldShowCompleteIcon(Quest q) => q && IsCompleted(q) && pendingCompleteAcks.Contains(q);

    // (아이콘 컨트롤러에서 쓰기 좋게 노출)
    public bool CanOffer(Quest q) => q && !HasQuest(q) && !IsCompleted(q);
    public bool CanTurnIn(Quest q) => q && IsCompleted(q) && pendingCompleteAcks.Contains(q);

    // ID 기준 조회/상태 확인
    public bool HasActive(string questId)
        => !string.IsNullOrEmpty(questId) &&
           activeQuests.Any(a => a != null && a.questId == questId && !a.isCompleted);

    /* =========================
     * 수락 (SO 직접 / ID로 시작)
     * ========================= */
    public void AddQuest(Quest quest)
    {
        if (quest == null) return;
        if (HasQuest(quest) || IsCompleted(quest)) return;

        activeQuests.Add(quest);
        Debug.Log($"[Quest] 수락: {quest.title}");
        RefreshAllGiverIcons();
    }

    // 튜토리얼 진입 시 사용: ID로 시작
    public void StartQuest(string questId)
    {
        Quest q = FindQuestAssetById(questId);
        if (q == null)
        {
            Debug.LogWarning($"[Quest] StartQuest 실패 - ID '{questId}'를 찾을 수 없음");
            return;
        }
        AddQuest(q);
    }

    public void StartQuest(Quest quest) => AddQuest(quest);

    /* =========================
     * 목표 진행/완료 체크
     * ========================= */
    public void UpdateGoal(string key)
    {
        for (int i = 0; i < activeQuests.Count; i++)
        {
            Quest q = activeQuests[i];
            if (q == null || q.goal == null) continue;

            q.goal.AddProgress(key);

            if (q.goal.IsCompleted())
            {
                HandleCompleted(q, i);
                i--; // 리스트 축소 보정
            }
        }
    }

    // 촌장 등 특정 대상에게 "말 걸기" 보고 → 내부적으로 UpdateGoal에 key전달
    public void ReportTalk(string targetKey)
    {
        if (string.IsNullOrEmpty(targetKey)) return;
        UpdateGoal(targetKey);
    }

    // (편의) 특정 퀘스트 진행 중일 때만 talk 보고
    public void ReportTalkForQuest(string questId, string targetKey)
    {
        if (HasActive(questId))
            UpdateGoal(targetKey);
    }

    // (옵션) 특정 퀘스트에 누적 추가가 필요할 때 사용
    public void AddProgress(string questId, int amount = 1, string subKey = null)
    {
        Quest q = activeQuests.FirstOrDefault(a => a != null && a.questId == questId);
        if (q == null || q.goal == null) return;

        for (int n = 0; n < Mathf.Max(1, amount); n++)
            q.goal.AddProgress(subKey);

        if (q.goal.IsCompleted())
        {
            int idx = activeQuests.IndexOf(q);
            if (idx >= 0) HandleCompleted(q, idx);
        }
    }

    /* =========================
     * 강제 완료(대화/이동 등에서 사용)
     * ========================= */
    public void CompleteQuest(string questId)
    {
        int idx = activeQuests.FindIndex(q => q != null && q.questId == questId);
        Quest q = idx >= 0 ? activeQuests[idx] : FindQuestAssetById(questId);
        if (q == null) return;

        if (idx >= 0)
        {
            HandleCompleted(q, idx); // 내부에서 아이콘 갱신 호출
        }
        else
        {
            if (!completedQuests.Contains(q)) completedQuests.Add(q);
            q.isCompleted = true;
            pendingCompleteAcks.Add(q);
            OnQuestCompleted?.Invoke(q);
            Debug.Log($"[Quest 완료] {q.title} (강제)");
            RefreshAllGiverIcons();
        }
    }

    /* =========================
     * 완료 확인(F로 V 끄기)
     * ========================= */
    public void AcknowledgeCompletion(Quest q)
    {
        if (q == null) return;
        if (!IsCompleted(q)) return;

        if (pendingCompleteAcks.Remove(q))
        {
            Debug.Log($"[Quest 완료확인] {q.title} (V 아이콘 OFF)");
            RefreshAllGiverIcons();
        }
    }

    /* =========================
     * 내부 유틸
     * ========================= */
    private void HandleCompleted(Quest q, int activeIndex)
    {
        q.isCompleted = true;

        if (activeIndex >= 0 && activeIndex < activeQuests.Count)
            activeQuests.RemoveAt(activeIndex);

        if (!completedQuests.Contains(q))
            completedQuests.Add(q);

        // 완료 직후에는 확인 대기 상태로 등록 → V 유지
        pendingCompleteAcks.Add(q);

        Debug.Log($"[Quest 완료] {q.title}");
        OnQuestCompleted?.Invoke(q);

        // 필요 시 보상 처리 자리
        // GiveReward(q);

        RefreshAllGiverIcons();
    }

    private Quest FindQuestAssetById(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;

        // 1) Resources에서 이름으로 먼저 시도
        var direct = Resources.Load<Quest>(questId);
        if (direct != null) return direct;

        // 2) 전체 스캔하여 Quest.questId 비교
        var all = Resources.LoadAll<Quest>("");
        foreach (var q in all)
        {
            if (q != null && q.questId == questId)
                return q;
        }
        return null;
    }

    // 아이콘 갱신 (버전별 안전 처리)
    private void RefreshAllGiverIcons()
    {
#if UNITY_2023_1_OR_NEWER
        var givers = UnityEngine.Object.FindObjectsByType<NPCQuestGiver>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var givers = UnityEngine.Object.FindObjectsOfType<NPCQuestGiver>(true);
#endif
        foreach (var g in givers) g.RefreshIcons();
    }

    /* ---------- 보상 (원하면 구현) ----------
    private void GiveReward(Quest q)
    {
        if (q == null) return;
        // InventorySystem.Instance.AddItem(q.rewardItem, q.rewardAmount);
    }
    */
}
