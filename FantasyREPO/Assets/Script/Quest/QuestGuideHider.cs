using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 기존 코드를 수정하지 않고, 특정 퀘스트(T001 등)일 때
/// 오른쪽 패널의 진행도/보상을 숨기는 보조 컴포넌트.
/// 기준은 '제목 텍스트' 또는 '퀘스트ID 문자열' 중 편한 쪽을 사용.
/// </summary>
public class QuestGuideHider : MonoBehaviour
{
    [Header("바인딩(오른쪽 패널)")]
    [SerializeField] TMP_Text titleText;          // Title (TMP_Text)
    [SerializeField] GameObject progressRow;      // 진행도 묶음(부모 오브젝트)
    [SerializeField] GameObject rewardRow;        // 보상 묶음(부모 오브젝트)

    [Header("가이드로만 보여줄 퀘스트 식별")]
    [Tooltip("제목으로 판별 (예: \"이동하기\")")]
    [SerializeField] string[] guideQuestTitles;
    [Tooltip("퀘스트ID로 판별 (예: \"T001_Move\") - 제목 대신 이걸로 써도 됨")]
    [SerializeField] string[] guideQuestIds;

    [Header("제목 대신 ID 텍스트를 어디서 읽을지(없으면 비워두기)")]
    [Tooltip("UI 어딘가에 현재 퀘스트ID를 찍어두는 TMP가 있다면 연결(없으면 비움)")]
    [SerializeField] TMP_Text currentQuestIdText;

    string _lastKey;

    void OnEnable()
    {
        // 가벼운 폴링(0.2초)로 제목/ID가 바뀔 때만 반응
        StartCoroutine(CoWatch());
    }

    IEnumerator CoWatch()
    {
        var wait = new WaitForSeconds(0.2f);
        while (isActiveAndEnabled)
        {
            ApplyVisibility();
            yield return wait;
        }
    }

    void ApplyVisibility()
    {
        string key = GetCurrentKey();
        if (key == _lastKey) return;
        _lastKey = key;

        bool isGuide = IsGuideKey(key);

        if (progressRow != null) progressRow.SetActive(!isGuide);
        if (rewardRow != null) rewardRow.SetActive(!isGuide);

        // 레이아웃 정리
        var rt = GetComponent<RectTransform>();
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    string GetCurrentKey()
    {
        // 1) ID 텍스트가 연결돼 있으면 우선 사용
        if (currentQuestIdText != null && !string.IsNullOrEmpty(currentQuestIdText.text))
            return currentQuestIdText.text.Trim();

        // 2) 아니면 제목으로 판별
        if (titleText != null && !string.IsNullOrEmpty(titleText.text))
            return titleText.text.Trim();

        return string.Empty;
    }

    bool IsGuideKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        // ID 우선 체크
        if (guideQuestIds != null)
            foreach (var id in guideQuestIds)
                if (!string.IsNullOrEmpty(id) && key.Equals(id.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    return true;

        // 제목 체크
        if (guideQuestTitles != null)
            foreach (var t in guideQuestTitles)
                if (!string.IsNullOrEmpty(t) && key.Equals(t.Trim(), System.StringComparison.CurrentCulture))
                    return true;

        return false;
    }
}
