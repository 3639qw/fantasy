// Assets/Scripts/Quest/TutorialBootstrapper.cs
using UnityEngine;

public class TutorialBootstrapper : MonoBehaviour
{
    [Header("Start Quest (choose one)")]
    [SerializeField] private Quest firstQuestSO;      // SO 직접 참조
    [SerializeField] private string firstQuestId = ""; // 또는 ID로 시작("T001_Move")

    [Header("On Start Tip")]
    [TextArea][SerializeField] private string startHint = "W=앞  A=좌  S=뒤  D=우 로 이동하세요 그 후 튜토리얼 NPC에게 F 키로 말을 걸고 퀘스트를 받으세요";

    private void Start()
    {
        // 1) 퀘스트 시작
        if (QuestManager.Instance != null)
        {
            if (firstQuestSO != null)
                QuestManager.Instance.StartQuest(firstQuestSO);
            else if (!string.IsNullOrWhiteSpace(firstQuestId))
                QuestManager.Instance.StartQuest(firstQuestId);
        }
        else
        {
            Debug.LogWarning("[TutorialBootstrapper] QuestManager.Instance가 없음");
        }

        // 2) 시작 안내 텍스트
        if (!string.IsNullOrWhiteSpace(startHint))
            TutorialUI.Instance?.Show(startHint);
    }
}
