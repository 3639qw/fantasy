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

    // 🔵 Popup 옵션
    [Header("Popup (Speech)")]
    public bool usePopup = true;
    [Tooltip("수락 제안 직전에 한번 띄움")]
    public string offerPopup = "마을에 온 걸 환영해!";
    public string acceptedPopup = "고마워, 큰 도움이 돼!";
    public string progressPopup = "촌장에게 다녀와줘!";
    public string completePopup = "오! 벌써 다녀왔구나!";
    public float popupDuration = 1.4f;
    public Vector2 popupOffset = new Vector2(0f, 1.6f);

    bool playerInRange;

    void Awake()
    {
        if (!iconSpeech) iconSpeech = transform.Find("HeadCanvas/Icon1")?.GetComponent<Image>();
        if (!iconCheck) iconCheck = transform.Find("HeadCanvas/Icon2")?.GetComponent<Image>();
        if (!dialogueUI) dialogueUI = FindObjectOfType<DialogueUI>(true);

        SetupCanvas(iconSpeech);
        SetupCanvas(iconCheck);
        SetupHeadIconsLayout();
    }

    void OnEnable()
    {
        SetIcon(iconSpeech, false);
        SetIcon(iconCheck, false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;

        // QuestManager 초기화/로드가 끝난 뒤 아이콘 반영 (하단 한 번 딜레이)
        Invoke(nameof(UpdateIconState), 0.1f);
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;

        CancelInvoke();                 // 남은 Invoke 정리
        SafeHidePopups();               // 혹시 남아있을 팝업 정리
    }

    void Update()
    {
        // 대화창이 열려 있을 땐 입력은 DialogueUI가 처리
        if (dialogueUI && dialogueUI.IsOpen) return;

        if (!playerInRange || !Input.GetKeyDown(KeyCode.F)) return;

        if (!dialogueUI) dialogueUI = FindObjectOfType<DialogueUI>(true);
        var qm = QuestManager.Instance;
        if (qm == null || quest == null || dialogueUI == null) return;

        // 대화 시작 직전: 떠 있는 팝업은 전부 정리 (하얀 화면/겹침 방지)
        SafeHidePopups();

        // 1) 아직 수락 전 → 안내문 → 수락/거절
        if (!qm.HasQuest(quest) && !qm.IsCompleted(quest))
        {
            Popup(offerPopup); // ✅ 제안 직전 한 번
            dialogueUI.ShowQuestChoice(
                offerLines,
                accept: () =>
                {
                    qm.AddQuest(quest);
                    SafeHidePopups();             // 수락 시 팝업 정리
                    Popup(acceptedPopup);         // ✅ 수락 직후
                    dialogueUI.ShowLines(acceptedLines);
                    UpdateIconState();
                },
                decline: () =>
                {
                    SafeHidePopups();             // 거절 시 팝업 정리
                    if (declineLines != null && declineLines.Length > 0)
                        dialogueUI.ShowLines(declineLines);
                    UpdateIconState();
                }
            );
            return;
        }

        // 2) 완료되어 체크아이콘 보여줄 상태 → 완료 대사 후 확인 처리
        if (qm.ShouldShowCompleteIcon(quest))
        {
            Popup(completePopup); // ✅ 완료로 돌아왔을 때
            dialogueUI.ShowLines(completeLines, () =>
            {
                qm.AcknowledgeCompletion(quest);   // 체크 끄기/보상 마무리 등
                SafeHidePopups();
                UpdateIconState();
            });
            return;
        }

        // 3) 진행 중(아직 조건 미달)
        if (qm.HasQuest(quest) && !qm.IsCompleted(quest))
        {
            Popup(progressPopup); // ✅ 진행 안내
            dialogueUI.ShowLines(inProgressLines);
            return;
        }

        // 4) 이미 영구 완료 상태 or 그 외
        dialogueUI.ShowLines(greetLines);
    }

    /* ---------- Trigger ---------- */
    void OnTriggerEnter2D(Collider2D other) { if (IsPlayer(other)) playerInRange = true; }
    void OnTriggerExit2D(Collider2D other) { if (IsPlayer(other)) playerInRange = false; }

    bool IsPlayer(Collider2D c)
    {
        if (!c) return false;
        if (c.CompareTag("Player") || c.CompareTag("PlayerCollider")) return true;
        var root = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.transform.root?.gameObject;
        return root && (root.CompareTag("Player") || root.CompareTag("PlayerCollider"));
    }

    /* ---------- Quest Event ---------- */
    void OnQuestCompleted(Quest completedQuest)
    {
        if (quest == null || completedQuest != quest) return;
        UpdateIconState(); // 완료 직후 체크가 켜지도록
    }

    /* ---------- UI State ---------- */
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
        img.color = new Color(1, 1, 1, 1);
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
        canvas.sortingOrder = 5000;

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

    // 🔵 팝업 유틸
    void Popup(string msg)
    {
        if (!usePopup || string.IsNullOrWhiteSpace(msg)) return;
        SpeechPopupService.I?.Show(transform, msg, popupDuration, popupOffset);
    }

    void SafeHidePopups()
    {
        SpeechPopupService.I?.HideAllActive();
    }
}
