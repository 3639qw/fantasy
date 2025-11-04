using UnityEngine;

public class TutorialQuestWire : MonoBehaviour
{
    void OnEnable()
    {
        // 이동하면 → T002 시작
        TutorialEvent.On("Move", () => Chain("T001_Move", "T002_TalkChief",
            "마을장에게 말을 걸어보세요 (F)"));

        // 나무 조사하면 → T004(다음 준비)
        TutorialEvent.On("InspectTree", () => Chain("T003_InspectTree", "T004_CollectLogs",
            "스페이스바로 채집하여 자원을 모으세요"));
    }

    private void Chain(string doneId, string nextId, string guide = null)
    {
        QuestManager.Instance?.CompleteQuest(doneId);
        QuestManager.Instance?.StartQuest(nextId);
        if (!string.IsNullOrEmpty(guide))
            TutorialUI.Instance?.Show(guide);
    }
}
