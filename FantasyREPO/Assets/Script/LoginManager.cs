using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    //�α��� ȭ�� Root 
    public GameObject LoginView;

    public TMP_InputField inputField_ID;
    public TMP_InputField inputField_PW;
    
    
    [Header("Register Form")]
    public TMP_InputField registerID; // id
    public TMP_InputField registerPW; // pw
    public TMP_InputField registerMail; // email
    public TMP_InputField registerName; // 성명
    public TMP_InputField registerNick; // 닉네임

    private string apiUrl = "http://localhost:5106/api/login";

    /// <summary>
    /// �α��� ��ư Ŭ���� ����
    /// </summary>
    public void LoginButtonClick()
    {
        string userId = inputField_ID.text;
        string password = inputField_PW.text;

        StartCoroutine(LoginRequest(userId, password));
    }

    public void RegisterButtonClick()
    {
        string userId = registerID.text;
        string password = registerPW.text;
        string mail = registerMail.text;
        string name = registerName.text;
        string nickname = registerNick.text;

        Debug.Log("회원가입 요청");
        // StartCoroutine(RegisterRequest(userId, password, mail, name, nickname));
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

    /// <summary>
    /// 회원가입요청
    /// </summary>
    /// <param name="userId">로그인 ID</param>
    /// <param name="password">로그인 PW</param>
    /// <param name="mail">이메일주소</param>
    /// <param name="name">성명</param>
    /// <param name="nickname">닉네임</param>
    /// <returns></returns>
    // private IEnumerator RegisterRequest(string userId, string password, string mail, string name, string nickname)
    // {
    //     RegisterRequest regRequest = new RegisterRequest
    //     {
    //         ID = userId,
    //         Password = password,
    //         Mail = mail,
    //         Name = name,
    //         Nickname = nickname
    //     };
    //     
    //     string json = JsonUtility.ToJson(regRequest);
    //
    // }
}

[System.Serializable]
public class LoginRequest
{
    public string ID;
    public string Password;
}

[System.Serializable]
public class RegisterRequest
{
    public string ID;
    public string Password;
    public string Mail;
    public string Name;
    public string Nickname;
}