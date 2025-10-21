using UnityEngine;
using System;
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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /* ---------- 상태 확인 ---------- */
    public bool HasQuest(Quest q) => q && activeQuests.Contains(q);
    public bool IsCompleted(Quest q) => q && (completedQuests.Contains(q) || q.isCompleted);
    public bool ShouldShowCompleteIcon(Quest q) => q && IsCompleted(q) && pendingCompleteAcks.Contains(q);

    /* ---------- 수락 ---------- */
    public void AddQuest(Quest quest)
    {
        if (quest == null) return;
        if (HasQuest(quest) || IsCompleted(quest)) return;

        activeQuests.Add(quest);
        Debug.Log($"[Quest] 수락: {quest.title}");
    }

    /* ---------- 목표 진행/완료 체크 ---------- */
    public void UpdateGoal(string key)
    {
        for (int i = 0; i < activeQuests.Count; i++)
        {
            Quest q = activeQuests[i];
            if (q == null || q.goal == null) continue;

            q.goal.AddProgress(key);

            if (q.goal.IsCompleted())
            {
                q.isCompleted = true;

                activeQuests.RemoveAt(i);
                completedQuests.Add(q);
                i--;

                Debug.Log($"[Quest 완료] {q.title}");

                // (필요하면 보상) GiveReward(q);

                // 완료 직후에는 확인 대기 상태로 등록 → V 유지
                pendingCompleteAcks.Add(q);

                OnQuestCompleted?.Invoke(q);
            }
        }
    }

    /* ---------- 완료 확인(F로 V 끄기) ---------- */
    public void AcknowledgeCompletion(Quest q)
    {
        if (q == null) return;
        if (!IsCompleted(q)) return;

        if (pendingCompleteAcks.Remove(q))
        {
            Debug.Log($"[Quest 완료확인] {q.title} (V 아이콘 OFF)");
        }
    }

    /* ---------- 보상 (원하면 구현) ---------- */
    // private void GiveReward(Quest q)
    // {
    //     if (q == null) return;
    //     // InventorySystem.Instance.AddItem(q.rewardItem, q.rewardAmount);
    // }
}
