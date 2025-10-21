// Quest.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Quest System/Quest")]
public class Quest : ScriptableObject
{
    [Header("Info")]
    public string questId;
    public string title;
    [TextArea] public string description;

    [Header("Goal")]
    public QuestGoal goal = new QuestGoal(); // ✅ 기본 생성

    [Header("Rewards")]
    public int rewardExp;
    public int rewardGold;

    [HideInInspector] public bool isCompleted;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (goal == null) goal = new QuestGoal(); // ✅ 에디터에서 null 방지
    }
#endif
}
