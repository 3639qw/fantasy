// Assets/Scripts/Quest/QuestRuntimeReset.cs
using UnityEngine;
using System.Reflection;   // 리플렉션 사용을 위해 추가

public class QuestRuntimeReset : MonoBehaviour
{
    // ❶ 도메인 리로드가 꺼진 경우에도 호출되는 초기화 지점
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticsEarly()
    {
        // 정적 싱글톤/캐시를 비움
        QuestManager.Instance = null;
    }

    // ❷ 씬 로드 직후 실제 SO & 매니저 리스트 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ResetQuestsAndManager()
    {
        // 1) 모든 Quest ScriptableObject의 런타임 진행도 초기화
        var all = Resources.LoadAll<Quest>("");
        foreach (var q in all)
        {
            if (q == null) continue;
            if (q.goal != null)
                q.goal.currentAmount = 0;
            q.isCompleted = false;
        }

        // 2) 씬 안의 QuestManager 상태 초기화
        var qm = Object.FindFirstObjectByType<QuestManager>(FindObjectsInactive.Include);
        if (qm != null)
        {
            qm.activeQuests.Clear();
            qm.completedQuests.Clear();

            // 3) private HashSet<Quest> pendingCompleteAcks 비우기 (리플렉션으로 접근)
            var fi = typeof(QuestManager).GetField("pendingCompleteAcks",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null)
            {
                var obj = fi.GetValue(qm); // HashSet<Quest> 객체
                var clearMI = fi.FieldType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
                clearMI?.Invoke(obj, null); // 안전하게 Clear() 호출
            }
        }

        Debug.Log("[Quest] 런타임 값 초기화 완료 (SO + 매니저 리스트)");
    }
}
