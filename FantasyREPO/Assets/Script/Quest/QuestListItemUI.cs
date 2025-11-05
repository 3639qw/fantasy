// Assets/Scripts/Quest/UI/QuestListItemUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[DisallowMultipleComponent]
public class QuestListItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text title;

    [Header("Optional Visuals")]
    [SerializeField] private GameObject selectedHighlight; // 선택 시 켜질 오브젝트(선택)

    private Quest data;
    private Action<Quest> onClick;

    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!title) title = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnDestroy()
    {
        if (button) button.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 리스트 항목에 데이터 바인딩
    /// </summary>
    public void Bind(Quest q, Action<Quest> click, string suffix = "")
    {
        data = q;
        onClick = click;

        if (selectedHighlight) selectedHighlight.SetActive(false);

        if (title)
            title.text = (q != null ? q.title : "(null)") + suffix;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = (q != null);

            button.onClick.AddListener(() =>
            {
                if (data == null) return;

                // 선택 시각효과(있으면)
                if (selectedHighlight) selectedHighlight.SetActive(true);

                onClick?.Invoke(data);
            });
        }
    }

    /// <summary>
    /// 외부에서 이 항목의 선택 표시를 제어하고 싶을 때(선택)
    /// </summary>
    public void SetSelected(bool on)
    {
        if (selectedHighlight) selectedHighlight.SetActive(on);
    }

    /// <summary>
    /// 현재 바인딩된 퀘스트 반환(필요 시)
    /// </summary>
    public Quest GetQuest() => data;
}
