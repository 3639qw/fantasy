// Assets/Scripts/Quest/NPCQuestGiver.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class NPCQuestGiver : MonoBehaviour
{
    [Header("Data")]
    public Quest quest;
    [Tooltip("씬의 DialogueUI 참조(없으면 자동 탐색)")]
    public DialogueUI dialogueUI;

    [Header("Head Icons (UI Images)")]
    public Image iconSpeech;   // 수락 가능
    public Image iconCheck;    // 완료 확인

    [Header("Dialogues")]
    [TextArea] public string[] offerLines = { "마을 촌장에게 인사를 전해줄래?", "부탁할 수 있을까?" };
    [TextArea] public string[] acceptedLines = { "고마워! 촌장은 마을 중앙에 있어." };
    [TextArea] public string[] declineLines = { "괜찮아. 마음이 바뀌면 다시 와줘." };
    [TextArea] public string[] inProgressLines = { "진행 중인 퀘스트가 있어. 촌장에게 다녀와줘." };
    [TextArea] public string[] completeLines = { "수고했어! 보상은 바로 지급할게." };
    [TextArea] public string[] greetLines = { "안녕! 오늘도 좋은 하루야." };

    [Header("Popup (Speech)")]
    public bool usePopup = true;
    public string offerPopup = "마을에 온 걸 환영해!";
    public string acceptedPopup = "고마워, 큰 도움이 돼!";
    public string progressPopup = "촌장에게 다녀와줘!";
    public string completePopup = "오! 벌써 다녀왔구나!";
    public float popupDuration = 1.4f;
    public Vector2 popupOffset = new Vector2(0f, 1.6f);

    [Header("Starter Tools (ItemData)")]
    public ItemData axeItem;
    public ItemData pickaxeItem;
    public ItemData shovelItem;

    [Header("After Tutorial")]
    public string nextMainQuestId = "Q101_MainStart";

    // ⚠ Inventory는 정적이 아니라 인스턴스 참조로 사용
    [SerializeField] private Inventory inventory;

    bool playerInRange;

    void Awake()
    {
        if (!iconSpeech) iconSpeech = transform.Find("HeadCanvas/Icon1")?.GetComponent<Image>();
        if (!iconCheck) iconCheck = transform.Find("HeadCanvas/Icon2")?.GetComponent<Image>();
        if (!dialogueUI) dialogueUI = FindObjectOfType<DialogueUI>(true);
        if (!inventory) inventory = FindObjectOfType<Inventory>(true);

        SetupCanvas(iconSpeech);
        SetupCanvas(iconCheck);
        SetupHeadIconsLayout();

        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnEnable()
    {
        SetIcon(iconSpeech, false);
        SetIcon(iconCheck, false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;

        Invoke(nameof(UpdateIconState), 0.05f);
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;

        CancelInvoke();
        SafeHidePopups();
    }

    void Update()
    {
        if (dialogueUI && dialogueUI.IsOpen) return;
        if (!playerInRange || !Input.GetKeyDown(KeyCode.F)) return;

        if (!dialogueUI) dialogueUI = FindObjectOfType<DialogueUI>(true);
        if (!inventory) inventory = FindObjectOfType<Inventory>(true);

        var qm = QuestManager.Instance;
        if (qm == null || quest == null || dialogueUI == null) return;

        SafeHidePopups();

        // 1) 수락 전
        if (!qm.HasQuest(quest) && !qm.IsCompleted(quest))
        {
            Popup(offerPopup);
            dialogueUI.ShowQuestChoice(
                offerLines,
                accept: () =>
                {
                    qm.AddQuest(quest);
                    SafeHidePopups();
                    Popup(acceptedPopup);
                    dialogueUI.ShowLines(acceptedLines);
                    UpdateIconState();
                    Debug.Log($"[Angel] AddQuest: {quest.questId} ({quest.title})");


                },
                decline: () =>
                {
                    SafeHidePopups();
                    if (declineLines != null && declineLines.Length > 0)
                        dialogueUI.ShowLines(declineLines);
                    UpdateIconState();
                }
            );
            return;
        }

        // 2) 완료 후 확인 대기(V) 상태
        if (qm.ShouldShowCompleteIcon(quest))
        {
            Popup(completePopup);
            dialogueUI.ShowLines(completeLines, () =>
            {
                qm.AcknowledgeCompletion(quest);
                SafeHidePopups();
                UpdateIconState();
                OnComplete_StartMainQuest();
            });
            return;
        }

        // 3) 진행 중
        if (qm.HasQuest(quest) && !qm.IsCompleted(quest))
        {
            Popup(progressPopup);
            dialogueUI.ShowLines(inProgressLines);
            return;
        }

        // 4) 그 외
        dialogueUI.ShowLines(greetLines);
    }

    /* ---------- Trigger ---------- */
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other))
            playerInRange = true;
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInRange = false;

        // 플레이어가 NPC의 trigger에서 나가면 대화를 종료함
        if (dialogueUI && dialogueUI.IsOpen)
        {
            dialogueUI.Close(); // 대화창 닫기
        }

        // 팝업도 끈다
        SafeHidePopups();

        // 아이콘 상태 갱신
        UpdateIconState();
    }

    bool IsPlayer(Collider2D c)
    {
        if (!c) return false;
        if (c.CompareTag("Player") || c.CompareTag("PlayerCollider")) return true;
        var root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root?.gameObject;
        return root && (root.CompareTag("Player") || root.CompareTag("PlayerCollider"));
    }

    void OnQuestCompleted(Quest completedQuest)
    {
        if (quest == null || completedQuest != quest) return;
        UpdateIconState();
    }

    // QuestManager가 호출
    public void RefreshIcons() => UpdateIconState();

    void UpdateIconState()
    {
        var qm = QuestManager.Instance;
        if (qm == null || quest == null)
        {
            SetIcon(iconSpeech, false);
            SetIcon(iconCheck, false);
            return;
        }

        bool canAccept = !qm.HasQuest(quest) && !qm.IsCompleted(quest);
        bool showV = qm.ShouldShowCompleteIcon(quest);

        if (showV) { SetIcon(iconSpeech, false); SetIcon(iconCheck, true); }
        else if (canAccept) { SetIcon(iconCheck, false); SetIcon(iconSpeech, true); }
        else { SetIcon(iconSpeech, false); SetIcon(iconCheck, false); }
    }

    void SetIcon(Image img, bool on)
    {
        if (!img) return;
        img.raycastTarget = false;
        img.color = Color.white;
        img.enabled = on;
        img.gameObject.SetActive(on);
    }

    void SetupCanvas(Image img)
    {
        if (!img) return;
        var canvas = img.GetComponentInParent<Canvas>(true);
        if (!canvas) return;

        canvas.gameObject.SetActive(true);
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "Default";
        // canvas.sortingOrder = 5000; // 이걸 설정하면 플레이어의 인벤토리가 퀘스트 말풍선에 가려짐

        var rt = canvas.GetComponent<RectTransform>();
        if (rt)
        {
            rt.localScale = Vector3.one * 0.03f;
            rt.sizeDelta = new Vector2(96, 96);
        }
    }

    void SetupHeadIconsLayout()
    {
        if (iconSpeech)
        {
            var r = iconSpeech.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(32, 32);
            r.anchoredPosition = new Vector2(+16f, 12f);
            r.localScale = Vector3.one * 0.4f;
        }
        if (iconCheck)
        {
            var r = iconCheck.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(32, 32);
            r.anchoredPosition = new Vector2(-16f, 12f);
            r.localScale = Vector3.one * 0.4f;
        }
    }

    // 팝업 유틸
    void Popup(string msg)
    {
        if (!usePopup || string.IsNullOrWhiteSpace(msg)) return;
        SpeechPopupService.I?.Show(transform, msg, popupDuration, popupOffset);
    }

    void SafeHidePopups() => SpeechPopupService.I?.HideAllActive();

    /* ===== 튜토리얼 훅 ===== */
    public void OnAccepted_GiveStarterTools()
    {
        if (inventory)
        {
            if (axeItem) inventory.AddItem(axeItem, 1);
            if (pickaxeItem) inventory.AddItem(pickaxeItem, 1);
            if (shovelItem) inventory.AddItem(shovelItem, 1);
        }
        else
        {
            Debug.LogWarning("[NPCQuestGiver] Inventory 참조 없음: 도구 지급 생략");
        }

        // DialogueUI.Hint가 없으므로 TutorialUI 이용
        TutorialUI.Instance?.Show("도구 지급 완료! 스페이스바로 채집/채광/수집");
    }

    public void OnComplete_StartMainQuest()
    {
        if (!string.IsNullOrEmpty(nextMainQuestId))
            QuestManager.Instance?.StartQuest(nextMainQuestId);

        TutorialUI.Instance?.Show("튜토리얼 완료! 메인 퀘스트가 시작됩니다.");
    }
}
