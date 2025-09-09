using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class SoundSaveButton : MonoBehaviour
{
    [Header("UI References")]
    public GameObject saveMessageUI; // 메시지 띄울 Text UI
    public GameObject pausePanel;    // Pause 패널

    [Header("Settings")]
    public float messageDuration = 1.5f; // 메시지 표시 시간 (초)
    public string nextSceneName;         // 필요시 씬 전환용 (현재 사용 안함)

    public void OnClickSave()
    {
        Debug.Log("[SoundSaveButton] Save 버튼 클릭됨");
        StartCoroutine(SaveAndMove());
    }

    private IEnumerator SaveAndMove()
    {
        Debug.Log("[SoundSaveButton] 코루틴 시작");

        if (saveMessageUI != null)
        {
            saveMessageUI.SetActive(true);
            Debug.Log("[SoundSaveButton] Save 메시지 활성화");
        }
        else
        {
            Debug.LogWarning("[SoundSaveButton] saveMessageUI가 할당되어 있지 않음");
        }

        PlayerPrefs.Save();
        Debug.Log("[SoundSaveButton] PlayerPrefs 저장 완료");

        // 시간 정지 영향을 받지 않도록 수정
        yield return new WaitForSecondsRealtime(messageDuration);

        if (saveMessageUI != null)
        {
            saveMessageUI.SetActive(false);
            Debug.Log("[SoundSaveButton] Save 메시지 비활성화 완료");
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Debug.Log("[SoundSaveButton] pausePanel 활성화");
        }
        else
        {
            Debug.LogWarning("[SoundSaveButton] pausePanel이 할당되어 있지 않음");
        }

        Debug.Log("[SoundSaveButton] 코루틴 종료");
    }
}
