using UnityEngine;

[System.Serializable]
public class QuestGoal
{
    public enum GoalType { Collect, Kill, Talk }
    public GoalType goalType;
    public string targetKey;      // Talk 대상 키(예: "VillageChief")
    public int requiredAmount = 1;
    public int currentAmount = 0;

    public bool IsCompleted() => currentAmount >= requiredAmount;

    public void AddProgress(string key)
    {
        if (goalType != GoalType.Talk) return;
        if (IsCompleted()) return;
        if (key == targetKey) currentAmount++;
    }
}
