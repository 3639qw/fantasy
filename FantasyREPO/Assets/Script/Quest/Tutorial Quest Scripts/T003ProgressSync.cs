using UnityEngine;

public class T003ProgressSync : MonoBehaviour
{
    [Header("T003 Quest SO")]
    public Quest t003;                 // T003_Copper.asset drag&drop
    [Header("Count 기준 (인벤토리 itemID 또는 Prefix)")]
    public string targetKey = "Copper";
    [Header("갱신 주기(초)")]
    public float interval = 0.3f;

    float _t;

    void Update()
    {
        if (t003 == null) return;

        // T003이 active일 때만 밀어넣고 싶으면, 여기에 조건 추가(예: QuestManager에 물어보기)
        _t += Time.deltaTime;
        if (_t < interval) return;
        _t = 0f;

        int have = 0;
        try { have = InventoryBridge.Count(targetKey); } catch { have = 0; }

        var goal = t003.goal;
        if (goal == null) return;

        int clamped = Mathf.Clamp(have, 0, Mathf.Max(1, goal.requiredAmount));
        if (goal.currentAmount != clamped)
        {
            goal.currentAmount = clamped;         // ← SO 진행도 업데이트
            // 완료 체크(선택)
            if (goal.currentAmount >= goal.requiredAmount)
                t003.isCompleted = true;
        }
        Debug.Log(InventoryBridge.Count("Copper"));
    }
}
