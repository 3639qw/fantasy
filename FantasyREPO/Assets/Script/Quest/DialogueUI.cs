// Assets/Scripts/Quest/DialogueUI.cs
using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;       // 말풍선 패널 루트
    [SerializeField] private TMP_Text textLabel;     // 본문
    [SerializeField] private TMP_Text hintLabel;     // 하단 힌트

    private string[] lines;
    private int index;
    private System.Action onFinished;
    private System.Action onAccept;
    private System.Action onDecline;
    private bool waitingChoice;

    public bool IsOpen => panel && panel.activeSelf;

    void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    public void ShowLines(string[] lines, System.Action finishedCallback = null)
    {
        this.lines = lines ?? new string[0];
        index = 0;
        waitingChoice = false;
        onFinished = finishedCallback;
        onAccept = null;
        onDecline = null;

        panel.SetActive(true);
        Render();
    }

    public void ShowQuestChoice(string[] preLines, System.Action accept, System.Action decline)
    {
        ShowLines(preLines, () =>
        {
            waitingChoice = true;
            onAccept = accept;
            onDecline = decline;
            textLabel.text = "이 퀘스트를 수락하시겠습니까?";
            if (hintLabel) hintLabel.text = "[Z] 수락   [X] 거절";
        });
    }

    void Update()
    {
        if (!IsOpen) return;

        if (!waitingChoice)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                index++;
                if (index >= (lines?.Length ?? 0))
                {
                    onFinished?.Invoke();
                    if (!waitingChoice) Close();
                }
                else
                {
                    Render();
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Z)) { onAccept?.Invoke(); Close(); }
            if (Input.GetKeyDown(KeyCode.X)) { onDecline?.Invoke(); Close(); }
        }
    }

    private void Render()
    {
        if (lines == null || lines.Length == 0) { Close(); return; }
        textLabel.text = lines[index];
        if (hintLabel) hintLabel.text = "[F] 다음";
    }

    public void Close()
    {
        waitingChoice = false;
        if (panel) panel.SetActive(false);
    }
}
