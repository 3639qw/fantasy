// Assets/Scripts/Quest/NPCTalkTarget.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCTalkTarget : MonoBehaviour
{
    [Header("이 NPC의 Talk Key (Quest.goal.targetKey와 일치)")]
    public string talkKey = "VillageChief";

    [Header("대화 대상(촌장 상호작용 스크립트)")]
    public VillageChiefInteraction chief;   // 인스펙터에서 드래그(없으면 자동 탐색)

    private bool playerInRange;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        if (!chief) chief = GetComponent<VillageChiefInteraction>();
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        if (!chief) chief = GetComponent<VillageChiefInteraction>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other)) playerInRange = true;
        Debug.Log("[TalkTarget] ENTER by " + other.tag);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other)) playerInRange = false;
        Debug.Log("[TalkTarget] EXIT by " + other.tag);
    }

    private bool IsPlayer(Collider2D c)
    {
        if (!c) return false;
        if (c.CompareTag("Player") || c.CompareTag("PlayerCollider")) return true;
        var root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root?.gameObject;
        return root && (root.CompareTag("Player") || root.CompareTag("PlayerCollider"));
    }

    private void Update()
    {
        // ⛔ 대화창 열려 있으면 F 입력을 이 스크립트가 절대 처리하지 않게 함
        if (chief && chief.dialogueUI && chief.dialogueUI.IsOpen) return;

        // (거리 체크 쓰는 중이라면)
        var player = GameObject.FindWithTag("Player");
        if (!player) return;
        float dist = Vector2.Distance(player.transform.position, transform.position);
        bool canInteract = dist < 1.2f;

        if (canInteract && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"[TalkTarget] F pressed, dist={dist:F2}, talkKey={talkKey}");
            chief?.Interact();
        }
    }
}
