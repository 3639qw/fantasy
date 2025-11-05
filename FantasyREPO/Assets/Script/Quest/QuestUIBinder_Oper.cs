// Assets/Scripts/Quest/UI/QuestUIBinder_Oper.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class QuestUIBinder_Oper : MonoBehaviour
{
    [Header("Panels (Scene Objects)")]
    [SerializeField] private GameObject questwindow;     // 왼쪽 리스트 패널
    [SerializeField] private GameObject explainWindow;   // 오른쪽 상세 패널

    [Header("List")]
    [SerializeField] private Transform listContent;    // questwindow/Scroll View/Viewport/Content
    [SerializeField] private GameObject listItemPrefab; // QuestListItemUI 포함 프리팹
    [SerializeField] private ScrollRect scrollRect;     // (선택) 수동 연결 권장. 비면 런타임에 탐색

    [Header("Detail (Right)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text goldText;

    private Quest _current;
    private enum Tab { Active, Completed }
    private Tab _tab = Tab.Active;

    private void Awake()
    {
        // ScrollRect가 비어있으면 안전하게 찾기
        if (!scrollRect)
            scrollRect = GetComponentInChildren<ScrollRect>(true);
    }

    private void Start()
    {
        // 시작 시 모두 닫음
        SetPanels(false, false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            Toggle();
    }

    /// <summary>
    /// Q키: 창 열고 닫기. 열 때는 리스트만 열림(상세는 클릭 시에만).
    /// </summary>
    public void Toggle()
    {
        bool anyOpen =
            (questwindow && questwindow.activeSelf) ||
            (explainWindow && explainWindow.activeSelf);

        if (anyOpen)
        {
            SetPanels(false, false);
            return;
        }

        SetPanels(true, false);
        RebuildList();
        ScrollToTopNextFrame();   // 항상 맨 위부터 시작
    }

    /// <summary>
    /// 탭 버튼에 연결 (선택)
    /// </summary>
    public void ShowActiveTab() { _tab = Tab.Active; RebuildList(); ScrollToTopNextFrame(); }
    public void ShowCompletedTab() { _tab = Tab.Completed; RebuildList(); ScrollToTopNextFrame(); }

    /// <summary>
    /// 리스트 다시 그리기
    /// </summary>
    public void RebuildList()
    {
        if (!listContent)
        {
            Debug.LogError("[QuestUI] listContent가 설정되지 않았습니다.");
            return;
        }

        // 리스트 비우기
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        var qm = QuestManager.Instance;
        if (qm == null)
        {
            _current = null;
            SetDetail(null);        // 상세 비우기
            ShowDetailPanel(false); // 상세 숨김
            return;
        }

        List<Quest> src = (_tab == Tab.Active) ? qm.activeQuests : qm.completedQuests;
        src = src.Where(q => q != null).ToList();

        if (src.Count == 0)
        {
            _current = null;
            SetDetail(null);
            ShowDetailPanel(false);
            return;
        }

        foreach (var q in src)
        {
            var go = Instantiate(listItemPrefab, listContent);
            if (!go)
                continue;

            // 프리팹에 QuestListItemUI 반드시 붙어 있어야 함
            var ui = go.GetComponent<QuestListItemUI>();
            if (ui == null)
            {
                Debug.LogError("[QuestUI] listItemPrefab에 QuestListItemUI 컴포넌트가 필요합니다.");
                continue;
            }

            string extra = "";
            if (_tab == Tab.Active && qm.CanTurnIn(q)) extra = "  [수령 대기]";
            if (_tab == Tab.Completed) extra = "  [완료]";

            // 항목 클릭 → 상세 열기
            ui.Bind(q, OnClickListItem, extra);
        }

        // 자동선택/자동상세 오픈 없음(요구사항)
        _current = null;
        SetDetail(null);
        ShowDetailPanel(false);
    }

    /// <summary>
    /// 리스트 항목 클릭 콜백
    /// </summary>
    private void OnClickListItem(Quest q)
    {
        _current = q;
        SetDetail(q);
        ShowDetailPanel(true);   // 클릭 시에만 상세 패널 표시
    }

    /// <summary>
    /// 상세 패널 내용 채우기(데이터만)
    /// </summary>
    private void SetDetail(Quest q)
    {
        if (q == null)
        {
            if (titleText) titleText.text = "퀘스트 없음";
            if (descText) descText.text = "";
            if (progressText) progressText.text = "";
            if (goldText) goldText.text = "";
            return;
        }

        var qm = QuestManager.Instance;

        if (titleText) titleText.text = q.title;
        if (descText) descText.text = q.description;
        if (progressText) progressText.text = BuildProgressLine(q);

        // 보상 표기 (EXP + Gold)
        string reward = "";
        if (q.rewardExp > 0) reward += $"EXP {q.rewardExp}";
        if (q.rewardGold > 0) reward += (reward.Length > 0 ? "   " : "") + $"Gold {q.rewardGold}";
        if (goldText) goldText.text = reward;

        // 상태 꼬리표
        if (qm != null && goldText != null)
        {
            if (qm.CanTurnIn(q)) goldText.text += (goldText.text.Length > 0 ? "\n" : "") + "상태: 완료(수령 대기)";
            else if (qm.IsCompleted(q)) goldText.text += (goldText.text.Length > 0 ? "\n" : "") + "상태: 완료";
            else if (qm.HasQuest(q)) goldText.text += (goldText.text.Length > 0 ? "\n" : "") + "상태: 진행 중";
        }
    }

    // ---------- 내부 유틸 ----------
    private void SetPanels(bool listOn, bool detailOn)
    {
        if (questwindow) questwindow.SetActive(listOn);
        if (explainWindow) explainWindow.SetActive(detailOn);
    }

    private void ShowDetailPanel(bool on)
    {
        if (explainWindow) explainWindow.SetActive(on);
    }

    private void ScrollToTopNextFrame()
    {
        if (!scrollRect) return;
        // 레이아웃 갱신 후 한 프레임 뒤에 맨 위로
        StartCoroutine(CoScrollTop());
    }

    private IEnumerator CoScrollTop()
    {
        Canvas.ForceUpdateCanvases();          // 즉시 레이아웃 계산
        yield return null;                     // 다음 프레임까지 대기
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f; // 1=맨 위
    }

    // ---------- 진행도 문자열(Reflection 기반) ----------
    private string BuildProgressLine(Quest q)
    {
        if (q == null || q.goal == null) return "";

        object goal = q.goal;
        Type t = goal.GetType();

        int current = ReadIntAny(t, goal, "currentAmount", "current", "count", "progress");
        int required = ReadIntAny(t, goal, "requiredAmount", "targetAmount", "needAmount", "max", "goalCount");
        bool? completed = InvokeBoolIfExists(t, goal, "IsCompleted");

        if (completed == true && required == 0) return "진행도: 완료";

        if (required > 0)
            return $"진행도: {current}/{required}" + ((completed == true) ? " (완료)" : "");
        if (current > 0)
            return $"진행도: {current}" + ((completed == true) ? " (완료)" : "");

        return (completed == true) ? "진행도: 완료" : "진행도: -";
    }

    private int ReadIntAny(Type t, object obj, params string[] names)
    {
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(obj);

            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(obj, null);
        }
        return 0;
    }

    private bool? InvokeBoolIfExists(Type t, object obj, string method)
    {
        var m = t.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (m != null && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
            return (bool)m.Invoke(obj, null);
        return null;
    }

    // (선택) 상세창의 '완료 확인(F)' 버튼
    public void AckSelected()
    {
        if (_current == null || QuestManager.Instance == null) return;
        QuestManager.Instance.AcknowledgeCompletion(_current);
        RebuildList();             // 상태 새로고침
        ScrollToTopNextFrame();    // 리스트 변화 시에도 맨 위로
    }
}
