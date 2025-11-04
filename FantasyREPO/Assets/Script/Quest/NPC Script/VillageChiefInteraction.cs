// Assets/Scripts/Quest/NPC/VillageChiefInteraction.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VillageChiefInteraction : MonoBehaviour
{
    [Header("Dialogue UI (없으면 자동 탐색)")]
    public DialogueUI dialogueUI;

    [Header("Quest/Keys")]
    [Tooltip("퀘스트 SO의 Target Key와 동일해야 합니다.")]
    public string talkTargetKey = "VillageChief";
    [Tooltip("천사에게서 받은 퀘스트 ID")]
    public string questId = "T002_TalkChief";

    [Header("Dialogues")]
    [TextArea] public string[] greetLines = { "반갑네, 여행자." };
    [TextArea]
    public string[] introLines = {
        "여긴 ‘푸른숨 마을’. 중앙 광장, 우물, 대장간, 밭이 있지.",
        "사람들과 인사 나누고 필요한 게 있으면 내게 오게."
    };
    [TextArea] public string[] afterGiveLines = { "이 도구들로 여행에 도움이 되길 바라네." };
    [TextArea] public string[] defaultLinesNoQuest = { "어서오게. 먼저 안내를 받으려면 천사에게 가 보게." };
    [TextArea] public string[] alreadyClearedLines = { "이미 도구를 줬지. 마을 생활에 익숙해졌나?" };

    [System.Serializable]
    public struct ItemStack
    {
        public ItemData item;
        public int amount;
    }

    [Header("Reward Tools")]
    public ItemStack[] rewardTools; // 도끼/곡괭이/괭이/물뿌리개 등

    private bool playerInRange;
    private bool rewardGiven; // 재지급 방지

    private void Awake()
    {
        if (!dialogueUI) dialogueUI = FindObjectOfType<DialogueUI>(true);

        // 트리거로 동작
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other)) playerInRange = true;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other)) playerInRange = false;
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
        if (!playerInRange) return;
        if (dialogueUI && dialogueUI.IsOpen) return;

        if (Input.GetKeyDown(KeyCode.F))
            Interact();
    }

    public void Interact()
    {
        Debug.Log($"[Chief] HasActive({questId}) = {QuestManager.Instance.HasActive(questId)}");
        var qm = QuestManager.Instance;
        if (!qm)
        {
            Debug.LogWarning("[Chief] QuestManager가 필요합니다.");
            return;
        }

        // 이미 보상 지급했으면 완료 후 멘트만
        if (rewardGiven)
        {
            Show(dialogueUI, alreadyClearedLines, null);
            return;
        }

        // 퀘스트 미보유: 안내만
        if (!qm.HasActive(questId))
        {
            Show(dialogueUI, defaultLinesNoQuest, null);
            return;
        }

        // 진행 중이면: 인사 -> 소개 -> 도구 지급 -> 완료 보고
        Show(dialogueUI, greetLines, () =>
        Show(dialogueUI, introLines, () =>
        {
            GiveRewards();
            Show(dialogueUI, afterGiveLines, () =>
            {
                // 목표 달성 보고
                qm.ReportTalkForQuest(questId, talkTargetKey);
                rewardGiven = true;
            });
        }));
    }

    // DialogueUI.ShowLines 체인 헬퍼
    private void Show(DialogueUI ui, string[] lines, System.Action onFinish)
    {
        if (!ui) ui = FindObjectOfType<DialogueUI>(true);
        if (!ui)
        {
            Debug.LogWarning("[Chief] DialogueUI가 필요합니다.");
            onFinish?.Invoke();
            return;
        }
        ui.ShowLines(lines, onFinish);
    }

    private void GiveRewards()
    {
        var inv = FindObjectOfType<Inventory>(true);
        if (!inv)
        {
            Debug.LogWarning("[Chief] Inventory가 필요합니다. (Assets/Scripts/.../Inventory.cs)");
            return;
        }

        foreach (var s in rewardTools)
        {
            if (!s.item || s.amount <= 0) continue;
            inv.AddItem(s.item, s.amount); // ✅ Inventory.AddItem(ItemData,int) 시그니처에 맞춤
        }

        Debug.Log("[Chief] 도구 지급 완료");
        TutorialUI.Instance?.Show("도구 지급 완료! 스페이스바로 채집/채광/수집");
    }
}
