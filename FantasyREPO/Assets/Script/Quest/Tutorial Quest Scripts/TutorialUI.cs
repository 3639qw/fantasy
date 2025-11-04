// Assets/Scripts/Quest/TutorialUI.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance;

    [Header("Refs")]
    [SerializeField] private GameObject panel; // ← 이걸 껐다 켜서 표시/숨김
    [SerializeField] private TMP_Text text;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 2.5f;

    private Coroutine _showRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 루트는 항상 활성 상태 유지, 패널만 끄기
        if (panel) panel.SetActive(false);
    }

    public void Show(string msg, float duration = -1f)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;

        // ❗ 루트가 꺼져 있으면 강제로 켜줌 (코루틴 실행 가능)
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }
        _showRoutine = StartCoroutine(ShowRoutine(msg, duration > 0f ? duration : defaultDuration));
    }

    IEnumerator ShowRoutine(string msg, float duration)
    {
        if (text) text.text = msg;
        if (panel) panel.SetActive(true);

        yield return new WaitForSeconds(duration);

        Hide();
    }

    public void Hide()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }
        if (panel) panel.SetActive(false);
        // 루트는 비활성화하지 말 것! (코루틴 못 돌림)
    }
}
