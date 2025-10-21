using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCTalkTarget : MonoBehaviour
{
    [Header("이 NPC의 Talk Key (Quest.goal.targetKey와 일치)")]
    public string talkKey = "VillageChief";

    bool playerInRange;

    // ✅ Player / PlayerCollider 둘 다 허용
    bool IsPlayerTag(Collider2D col)
    {
        if (col == null) return false;

        // 자기 자신 태그
        if (col.CompareTag("Player") || col.CompareTag("PlayerCollider"))
            return true;

        // Rigidbody2D 붙은 부모 오브젝트 검사
        var rb = col.attachedRigidbody ? col.attachedRigidbody.gameObject : null;
        if (rb != null && (rb.CompareTag("Player") || rb.CompareTag("PlayerCollider")))
            return true;

        // 루트(최상위) 검사
        if (col.transform.root.CompareTag("Player") || col.transform.root.CompareTag("PlayerCollider"))
            return true;

        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerTag(other))
        {
            playerInRange = true;
            Debug.Log($"[TalkTarget] ENTER by {other.name} (tag={other.tag})");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerTag(other))
        {
            playerInRange = false;
            Debug.Log($"[TalkTarget] EXIT by {other.name} (tag={other.tag})");
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"[TalkTarget] F pressed, talkKey={talkKey}");
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("[TalkTarget] QuestManager 없음");
                return;
            }
            QuestManager.Instance.UpdateGoal(talkKey);
        }
    }
}
