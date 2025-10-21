// Assets/Scripts/Quest/QuestRuntimeReset.cs
using UnityEngine;

public class QuestRuntimeReset : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ResetQuestsAndManager()
    {
        // 1) 모든 Quest SO의 진행/완료 상태 초기화
        var all = Resources.LoadAll<Quest>("");
        foreach (var q in all)
        {
            if (q == null) continue;
            if (q.goal != null) q.goal.currentAmount = 0;
            q.isCompleted = false;
        }

        // 2) 씬에 있는 QuestManager 상태 비우기
        var qm = FindFirstObjectByType<QuestManager>(); // 2022+ 권장, 구버전이면 FindObjectOfType
        if (qm != null)
        {
            qm.activeQuests.Clear();
            qm.completedQuests.Clear();
        }

        Debug.Log("[Quest] 모든 퀘스트 런타임 값 & 매니저 리스트 초기화 완료");
    }
}
