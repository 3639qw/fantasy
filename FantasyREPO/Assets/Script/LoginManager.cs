using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    // 로그인 패널 Root 
    public GameObject LoginView;

    public TMP_InputField inputField_ID;
    public TMP_InputField inputField_PW;
    public Button Button_Login;

    private string apiUrl = "http://localhost:5106/api/login";
    /// <summary>
    /// 로그인 버튼 클릭시 실행
    /// </summary>
    public void LoginButtonClick()
    {
        string userId = inputField_ID.text;
        string password = inputField_PW.text;

        StartCoroutine(LoginRequest(userId, password));
    }


    private IEnumerator LoginRequest(string userId, string password)
    {
        // 로그인 요청을 위한 JSON 객체
        LoginRequest loginRequest = new LoginRequest
        {
            ID = userId,
            Password = password
        };

        // 로그인 요청을 JSON 형식으로 직렬화
        string json = JsonUtility.ToJson(loginRequest);

        // HTTP POST 요청 보내기
        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            // 요청 본문에 JSON 추가
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("로그인 성공: " + www.downloadHandler.text);
                SceneManager.LoadScene("Overworld");
            }
            else
            {
                Debug.Log("로그인 실패: " + www.responseCode);
                Debug.Log("에러 메시지: " + www.error);
                Debug.Log("서버 응답: " + www.downloadHandler.text);
            }
        }

    }
}

[System.Serializable]
public class LoginRequest
{
    public string ID;
    public string Password;
}
